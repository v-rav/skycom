//=============================================================================
// ReleaseGuideService
// リリース案内で使用するサービス処理用ファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SKYCOM.DLManagement.Data;
using SKYCOM.DLManagement.Entity;
using SKYCOM.DLManagement.GmailAPI;
using SKYCOM.DLManagement.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using SKYCOM.DLManagement.AzureHelper;

namespace SKYCOM.DLManagement.Services
{
    public class ReleaseGuideService
    {
        private const string KEYWORD_PRODUCT = "%PRODUCT{0}%";
        private const string KEYWORD_VERSION = "%VERSION{0}%";
        private const string KEYWORD_URL = "%URL{0}%";

        private readonly DbAccess _context;
        private readonly IOptions<Settings> _settings;
        private readonly NavigationManager _navigationManager;
        private readonly Message _message;
        private IDbContextTransaction _transaction;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">DbAcecssインスタンス</param>
        /// <param name="settings">Settingsインスタンス</param>
        /// <param name="navigationManager">NavigationManager</param>
        /// <param name="message">Message</param>
        public ReleaseGuideService(DbAccess context, IOptions<Settings> settings, NavigationManager navigationManager, Message message)
        {
            _context = context;
            _settings = settings;
            _navigationManager = navigationManager;
            _message = message;
        }

        /// <summary>
        /// 送付状の初期値
        /// </summary>
        /// <returns>テンプレート</returns>
        public Task<string> InitMailText()
        {
            try
            {
                LogUtil.Instance.Trace("start");
                var templateList = new Dictionary<string, string>();
                var defaultTemplate = string.Empty;
                try
                {
                    templateList = _settings.Value.TemplateList;
                    defaultTemplate = _settings.Value.DefaultTemplate;
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
                    throw;
                }
                if (templateList.Keys.Contains(defaultTemplate))
                {
                    #region CMF-Changes
                    var blobClient = AzBlobStorageHelper.GetBlobClient(_settings.Value.BlobSettings.CommonContainerName, templateList[defaultTemplate]); // Get the BlobClient for the given blob

                    // Check if the blob exists
                    if (!AzBlobStorageHelper.BlobExists(blobClient))
                    {
                        LogUtil.Instance.Error(_message.MessageList["NonexistentFile"]);
                        return Task.FromResult(string.Empty); // Return empty if the blob does not exist
                    }
                    // Download and return the content of the blob
                    return Task.FromResult(AzBlobStorageHelper.DownloadBlobContent(blobClient));
                    #endregion

                    #region existing changes
                    //if (!File.Exists(templateList[defaultTemplate]))
                    //{
                    //    LogUtil.Instance.Error(_message.MessageList["NonexistentFile"]);
                    //    return Task.FromResult(string.Empty);
                    //}
                    //return Task.FromResult(File.ReadAllText(templateList[defaultTemplate]));
                    #endregion region
                }
                LogUtil.Instance.Warn(_message.MessageList["InitialValueSetting"]);
                return Task.FromResult(string.Empty);
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// 製品選択時の処理
        /// テンプレートの指定があればファイルを読み込む
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public Task<string> SelectProduct(string productName)
        {
            LogUtil.Instance.Trace("start");
            try
            {
                var templateList = new Dictionary<string, string>();
                var productList = new Dictionary<string, string>();
                try
                {
                    templateList = _settings.Value.TemplateList;
                    productList = _settings.Value.ProductList;
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
                    throw;
                }
                try
                {
                    if (productList.Keys.Contains(productName))
                    {
                        if (templateList.Keys.Contains(productList[productName]))
                        {
                            #region CMF-Changes
                            var blobClient = AzBlobStorageHelper.GetBlobClient(_settings.Value.BlobSettings.CommonContainerName, templateList[productList[productName]]); // Get the BlobClient for the given blob

                            // Check if the blob exists
                            if (!AzBlobStorageHelper.BlobExists(blobClient))
                            {
                                LogUtil.Instance.Error(_message.MessageList["NonexistentProductName"]);
                                return Task.FromResult(string.Empty); // Return empty if the blob does not exist
                            }
                            // Download and return the content of the blob
                            return Task.FromResult(AzBlobStorageHelper.DownloadBlobContent(blobClient));
                            #endregion

                            #region existing code
                            //if (!File.Exists(templateList[productList[productName]]))
                            //{
                            //    LogUtil.Instance.Error(_message.MessageList["NonexistentProductName"]);
                            //    return Task.FromResult(string.Empty);
                            //}
                            //return Task.FromResult(File.ReadAllText(templateList[productList[productName]]));
                            #endregion
                        }
                    }
                    return Task.FromResult(string.Empty);
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Error(_message.MessageList["NotChoiceProduct"], ex);
                    return Task.FromResult(string.Empty);
                }
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// 顧客CSVファイル選択処理
        /// </summary>
        /// <param name="csvFilePath">顧客CSVファイルパス</param>
        /// <returns>Userリスト</returns>
        public Task<List<User>> SelectCsv(string csvFilePath, out string message)
        {
            try
            {
                LogUtil.Instance.Trace("start");
                message = string.Empty;
                List<User> Csvlists = CsvUtil.ReadCsv(_message.MessageList, _settings.Value.Csv.CsCsvExclusion, csvFilePath, out message);
                return Task.FromResult(Csvlists);
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// ダウンロードURL作成処理
        /// 製品管理TBL、ユーザー管理TBL及びDL状態管理TBLへ追加
        /// </summary>
        /// <param name="users">ユーザー一覧</param>
        /// <param name="productInfos">製品一覧</param>
        /// <param name="fromAddress">送信元アドレス</param>
        /// <returns>ダウンロードURLリスト/returns>
        public async Task<List<DownloadUrl>> CreateDownloadUrl(List<User> users, List<ProductInfo> productInfos, string fromAddress)
        {
            LogUtil.Instance.Trace("start");
            if (users == null || users.Count == 0)
            {
                throw new ArgumentNullException(nameof(users));
            }
            if (productInfos == null || productInfos.Count == 0)
            {
                throw new ArgumentNullException(nameof(productInfos));
            }

            if(_transaction == null)
            {
                _transaction = _context.Database.CurrentTransaction;
            }
            if (_transaction != null)
            {
                await _transaction.DisposeAsync().ConfigureAwait(false);
                _transaction = null;
                _context.Database.SetCommandTimeout(null);
            }
            int? transactionTimeout = null;
            try
            {
                transactionTimeout = _settings.Value.TransactionTimeout;
            }
            catch (Exception ex)
            {
                LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
            }
            try
            {
                _context.Database.SetCommandTimeout(transactionTimeout);
                _transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted).ConfigureAwait(false);

                var sendGroupId = ((await _context.DLStatusManagements.MaxAsync(x => (long?)x.SendGroupId).ConfigureAwait(false)) ?? 0) + 1;
                var products = new List<Product>();
                foreach (var productInfo in productInfos.Where(x => !string.IsNullOrEmpty(x.ProductFilePath)))
                {
                    var product = await GetProduct(productInfo).ConfigureAwait(false);
                    if (product == null)
                    {
                        return await Task.FromResult<List<DownloadUrl>>(null).ConfigureAwait(false);
                    }
                    products.Add(product);
                }
                var urls = new List<DownloadUrl>();
                foreach (var user in users)
                {
                    var userDb = await _context.User.Where(x => x.MailAddress == user.MailAddress && x.NegotiationName == user.NegotiationName).FirstOrDefaultAsync().ConfigureAwait(false);
                    if (userDb == null)
                    {
                        _context.User.Add(user);
                    }
                    foreach (var product in products)
                    {
                        urls.Add(AddDLStatusManagements(fromAddress, sendGroupId, userDb ?? user, product));
                    }
                }
                var result = await _context.SaveChangesAsync().ConfigureAwait(false);
                if (result < 1)
                {
                    LogUtil.Instance.Error(_message.MessageList["SaveFailed"]);
                    return await Task.FromResult(new List<DownloadUrl>()).ConfigureAwait(false);
                }

                return await Task.FromResult(urls).ConfigureAwait(false);
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// 製品レコードを取得
        /// </summary>
        /// <param name="productInfo">製品情報</param>
        /// <returns>製品レコード</returns>
        private async Task<Product> GetProduct(ProductInfo productInfo)
        {
            try
            {
                LogUtil.Instance.Trace("start");
                var product = await _context.Product.Where(x => x.ProductFilePath == productInfo.ProductFilePath).FirstOrDefaultAsync().ConfigureAwait(false);
                if (product == null)
                {
                    product = new Product()
                    {
                        ProductName = productInfo.FileName,
                        ProductFilePath = productInfo.ProductFilePath
                    };
                    _context.Product.Add(product);
                    var res = await _context.SaveChangesAsync().ConfigureAwait(false);
                    if (res != 1)
                    {
                        LogUtil.Instance.Error(_message.MessageList["SaveFailed"]);
                        return null;
                    }
                }
                return product;
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// DL状態管理テーブルへレコード追加
        /// </summary>
        /// <param name="fromAddress">送信元アドレス</param>
        /// <param name="sendGroupId">送信グループID</param>
        /// <param name="user">ユーザー情報</param>
        /// <param name="product">製品情報</param>
        /// <returns>ダウンロードURL</returns>
        private DownloadUrl AddDLStatusManagements(string fromAddress, long sendGroupId, User user, Product product)
        {
            try
            {
                LogUtil.Instance.Trace("start");
                var dlStatusMgt = new DLStatusManagements()
                {
                    DLStatusManagementId = Guid.NewGuid().ToString("N"),
                    ProductId = product.ProductId,
                    SendGroupId = sendGroupId,
                    FormMailAddress = fromAddress,
                    DLStatus = 0,
                    DLCount = 0,
                    UserId = user.UserId,
                    TUser = user,
                    TProduct = product
                };
                dlStatusMgt.Urlstr = $"{_navigationManager.BaseUri}api/v1/file?id={dlStatusMgt.DLStatusManagementId}";
                var downloadUrl = new DownloadUrl()
                {
                    DLStatusManagementId = dlStatusMgt.DLStatusManagementId,
                    ProductName = product.ProductName,
                    Urlstr = dlStatusMgt.Urlstr,
                    NegotiationName = user.NegotiationName,
                    CustomerName = string.IsNullOrEmpty(user.CustomerName) ? user.SupportContact : user.CustomerName,
                    MailAddress = user.MailAddress
                };
                _context.DLStatusManagements.Add(dlStatusMgt);
                return downloadUrl;
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// 送信処理
        /// </summary>
        /// <param name="downloadUrls">ダウンロードURL一覧</param>
        /// <param name="reserveDateTime">予約時間</param>
        /// <param name="subjectValue">件名</param>
        /// <param name="text">本文</param>                
        /// <returns></returns>
        public async Task<bool> SendMail(List<DownloadUrl> downloadUrls, DateTime? reserveDateTime, string subjectValue, string text, List<ProductInfo> productInfos)
        {
            try
            {
                LogUtil.Instance.Trace("start");
                if (string.IsNullOrEmpty(text))
                {
                    throw new ArgumentNullException(nameof(text));
                }
                if (downloadUrls == null || downloadUrls.Count == 0)
                {
                    throw new ArgumentNullException(nameof(downloadUrls));
                }
                if (_transaction != null)
                {
                    var dlIdList = downloadUrls.Select(d => d.DLStatusManagementId).ToList();
                    var dLStatusManagements = await _context.DLStatusManagements.Where(x => dlIdList.Contains(x.DLStatusManagementId)).ToListAsync().ConfigureAwait(false);
                    var dateTime = DateTime.Now;
                    dLStatusManagements.ForEach(x =>
                    {
                        x.MailSendDateTime = dateTime;
                    });
                    if (reserveDateTime != null)
                    {
                        dLStatusManagements.ForEach(x =>
                        {
                            x.ReservedDateTime = reserveDateTime;
                        });
                    }
                    var result = await _context.SaveChangesAsync().ConfigureAwait(false);
                    if (result != dLStatusManagements.Count)
                    {
                        LogUtil.Instance.Error(_message.MessageList["NotChoiceProduct"]);
                        return await Task.FromResult(false).ConfigureAwait(false);
                    }
                    await _transaction.CommitAsync().ConfigureAwait(false);
                    _context.Database.SetCommandTimeout(null);
                    _transaction = null;
                }
                var addressList = downloadUrls.Select(x => x.MailAddress).Distinct().ToList();
                mailInfos.Clear();
                foreach (var address in addressList)
                {
                    var downloads = downloadUrls.Where(x => x.MailAddress == address).ToList();
                    var body = CreateBody(productInfos, downloads, text);
                    mailInfos.Add(new MailInfo()
                    {
                        DLStatusManagementIds = downloads.Select(x => x.DLStatusManagementId).ToList(),
                        Subject = subjectValue,
                        Body = body,
                        Address = address
                    });
                }

                TimeSpan timeSpan = TimeSpan.Zero;
                if (reserveDateTime != null)
                {
                    timeSpan = (TimeSpan)(reserveDateTime - DateTime.Now);
                    new Timer(ExecGmailSendAll, null, timeSpan, new TimeSpan(0, 0, 0));
                }
                else
                {
                    ExecGmailSendAll(null);
                    if(mailInfos.Count != successCnt)
                    {
                        return await Task.FromResult(false).ConfigureAwait(false);
                    }
                }
                
                return await Task.FromResult(true).ConfigureAwait(false);
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// メール本文作成
        /// </summary>
        /// <param name="productInfos">製品一覧</param>
        /// <param name="downloads">ダウンロードURL一覧</param>
        /// <param name="text">送付状</param>
        /// <returns>メール本文</returns>
        private static string CreateBody(List<ProductInfo> productInfos, List<DownloadUrl> downloads, string text)
        {
            try
            {
                LogUtil.Instance.Trace("start");
                var body = text;
                for (var i = 0; i < productInfos.Count; i++)
                {
                    var download = downloads.Where(x => x.ProductName == productInfos[i].FileName).FirstOrDefault();
                    if (download != null)
                    {
                        body = body.Replace(string.Format(CultureInfo.GetCultureInfo("ja-JP"), KEYWORD_PRODUCT, i + 1), productInfos[i].ProductName, StringComparison.OrdinalIgnoreCase);
                        body = body.Replace(string.Format(CultureInfo.GetCultureInfo("ja-JP"), KEYWORD_VERSION, i + 1), productInfos[i].Version, StringComparison.OrdinalIgnoreCase);
                        body = body.Replace(string.Format(CultureInfo.GetCultureInfo("ja-JP"), KEYWORD_URL, i + 1), download.Urlstr, StringComparison.OrdinalIgnoreCase);
                    }

                }

                return body;
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// メール情報
        /// </summary>
        private readonly List<MailInfo> mailInfos = new List<MailInfo>();
        private int successCnt;
        private int failureCnt;

        /// <summary>
        /// 全メール送信処理（[送信]ボタン押下単位）
        /// Timerで起動される
        /// maInfosコレクションに設定されているメール情報をExecGmailSendに渡す
        /// </summary>
        /// <param name="state"></param>
        public void ExecGmailSendAll(object state)
        {
            LogUtil.Instance.Trace("start");
            try
            {
                successCnt = 0;
                failureCnt = 0;
                var gmailSend = new GmailSendByAPI(_settings, _message);
                foreach (var mailinfo in mailInfos)
                {
                    if (gmailSend.ExecGmailSend(mailinfo))
                    {
                        ++successCnt;
                        try
                        {
                            // DB更新
                            using var tran = _context.Database.BeginTransaction();
                            var dlStatusManegements = _context.DLStatusManagements.Where(x => mailinfo.DLStatusManagementIds.Contains(x.DLStatusManagementId)).ToList();
                            var dateTime = DateTime.Now;
                            dlStatusManegements.ForEach(x => x.MailSendExecutedDateTime = dateTime);
                            var result = _context.SaveChanges();
                            if(result != dlStatusManegements.Count)
                            {
                                LogUtil.Instance.Error(_message.MessageList["FailureUpdateMailSendDateTime"]);
                            }
                            tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            LogUtil.Instance.Error(_message.MessageList["FailureUpdateMailSendDateTime"], ex);
                        }
                    }
                    else
                    {
                        ++failureCnt;
                    }
                }
                LogUtil.Instance.Info(string.Format(CultureInfo.GetCultureInfo("ja-JP"), _message.MessageList["MailSendComplate"], mailInfos.Count, successCnt, failureCnt));
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }
    }
}
