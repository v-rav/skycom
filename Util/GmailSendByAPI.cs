//=============================================================================
// GmailSendByAPI
// 1通のメールを作成し、送信する
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using MimeKit;
using SKYCOM.DLManagement.Data;
using SKYCOM.DLManagement.Util;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SKYCOM.DLManagement.GmailAPI
{
    public class GmailSendByAPI
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string appName;
        private readonly string gmailAddress;
        private readonly string keyFilePath;
        private readonly string serviceAccountEmail;
        private readonly string secretPassword;
        private readonly string gmailScope;
        private readonly bool isServiceAccount;
        private readonly Message _message;
        private readonly string[] scopes = { GmailService.Scope.GmailSend };

        private const string GMAIL_TOKEN = "Google.Apis.Gmail.Token";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings"></param>
        public GmailSendByAPI(IOptions<Settings> settings, Message message)
        {
            LogUtil.Instance.Trace("start");
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (message == null) throw new ArgumentNullException(nameof(message));
            try
            {
                _message = message;
                // GmailAPIクライアントID
                clientId = settings.Value.Gmail.GmailClientId;
                // GmailAPIクライアントシークレット;
                clientSecret = settings.Value.Gmail.GmailClientSecret;
                // Google APIsで登録した アプリケーション名
                appName = settings.Value.Gmail.GmailAppName;
                // 送信元アドレス
                gmailAddress = settings.Value.ReleaseGuide.SendFromAddressValue;
                // 鍵ファルパス（P12）
                keyFilePath = settings.Value.Gmail.KeyFilePath;
                // サービスアカウント
                serviceAccountEmail = settings.Value.Gmail.ServiceAccountEmail;
                // 秘密鍵のパスワード
                secretPassword = settings.Value.Gmail.SecretPassword;
                // Gmailスコープ
                gmailScope = settings.Value.Gmail.GmailScope;
                // 認証方法
                isServiceAccount = settings.Value.Gmail.IsServiceAccount;
            }
            catch (Exception ex)
            {
                LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
                throw;
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// １メール送信
        /// </summary>
        /// <param name="mailInfo">メール情報am>
        /// <param name="_settings"></param>
        /// <returns></returns>
        public bool ExecGmailSend(MailInfo mailInfo)
        {
            if (mailInfo == null) throw new ArgumentNullException(nameof(mailInfo));

            LogUtil.Instance.Trace("start");
            try
            {
                GmailService service = null; ;
                try
                {
                    if (isServiceAccount)
                    {
                        LogUtil.Instance.Debug("Setting is ServiceAccount");
                        //サービスアカウントAPI認証
                        if (File.Exists(keyFilePath))
                        {
                            var certificate = new X509Certificate2(keyFilePath, secretPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                            var credential = new ServiceAccountCredential(
                                   new ServiceAccountCredential.Initializer(serviceAccountEmail)
                                   {
                                       User = gmailAddress,
                                       Scopes = new[] { gmailScope }
                                   }.FromCertificate(certificate)
                            );
                            if (credential.RequestAccessTokenAsync(CancellationToken.None).Result)
                            {
                                service = new GmailService(new BaseClientService.Initializer()
                                {
                                    ApplicationName = appName,
                                    HttpClientInitializer = credential
                                });
                            }
                        }
                        if (service == null)
                        {
                            LogUtil.Instance.Error(_message.MessageList["FailureGmailAPIAuth"] + keyFilePath);
                            return false;
                        }
                    }
                    else
                    {
                        LogUtil.Instance.Debug("Setting is Client authentication");
                        //GoogleAPI認証
                        var tokenFolderPath = Path.Combine(Path.GetTempPath(), GMAIL_TOKEN);
                        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                          new ClientSecrets
                          {
                              ClientId = clientId,
                              ClientSecret = clientSecret
                          },
                          scopes,
                          "user",
                          CancellationToken.None,
                          new FileDataStore(tokenFolderPath)
                        ).Result;

                        service = new GmailService(new BaseClientService.Initializer()
                        {
                            ApplicationName = appName,
                            HttpClientInitializer = credential
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Error(_message.MessageList["FailureGmailAPIAuth"], ex);
                    return false;
                }
                try
                {
                    //メール作成
                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress(appName, gmailAddress));
                    // Bcc送信先セット
                    mimeMessage.Bcc.Add(new MailboxAddress("", mailInfo.Address));
                    //件名
                    mimeMessage.Subject = mailInfo.Subject;
                    //本文
                    var textPart = new TextPart(MimeKit.Text.TextFormat.Plain);
                    //本文エンコード
                    textPart.SetText(Encoding.GetEncoding("iso-2022-jp"), mailInfo.Body);
                    mimeMessage.Body = textPart;
                    var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mimeMessage.ToString())).Replace('+', '-').Replace('/', '_').Replace("=", "");

                    //メール送信
                    var result = service.Users.Messages.Send(
                      new Google.Apis.Gmail.v1.Data.Message()
                      {
                          Raw = rawMessage
                      },
                      "me"
                    ).Execute();
                    if (result == null)
                    {
                        LogUtil.Instance.Error(string.Format(CultureInfo.GetCultureInfo("ja-JP"), _message.MessageList["FailureMailSend"], mailInfo.Address));
                        return false;
                    }
                    LogUtil.Instance.Info(string.Format(CultureInfo.GetCultureInfo("ja-JP"), _message.MessageList["SuccesMailSend"], result.Id));
                    Task.Delay(500);
                    return true;

                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Error(string.Format(CultureInfo.GetCultureInfo("ja-JP"), _message.MessageList["FailureMailSend"], mailInfo.Address), ex);
                    return false;
                }
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }
    }
}

