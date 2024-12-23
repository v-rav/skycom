//=============================================================================
// CsvUtil
// CSVファイルを配列に変換する。
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Microsoft.VisualBasic.FileIO;
using SKYCOM.DLManagement.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SKYCOM.DLManagement.AzureHelper;
using Microsoft.Extensions.Configuration;


namespace SKYCOM.DLManagement.Util
{
    public static class CsvUtil
    {
        private const string EXTENSION_CSV = ".csv";
        private const string ENCODING_SJIS = "Shift_JIS";
        private const string CSV_DELIMITER = ",";
        private const int MAX_COLUMN = 26;
        private static readonly List<int> MAIL_ADDERSS_COULUN_INDEX = new List<int>() { 8,9,10,11,12,13,14,15,16,17,18,19,20,22,23,24,25};
        private const int NEGOTIATION_NAME_INDEX = 1; // 商談名
        private const int CUSTOMER_INDEX = 7; // 取引先名
        private const int SUPPORT_CONTACT_INDEX = 2; // サポート窓口
        private const int UPDAT_CONTACT_INDEX = 21; // 更新窓口企業名

        private static Dictionary<string, string> _messageList;

        private static IConfiguration _configuration;

        // Static property to hold the container name (if needed)
        public static string CommonContainerName { get; private set; }

        // Method to configure the static class with IConfiguration
        public static void Configure(IConfiguration configuration)
        {
            _configuration = configuration;

            // Read the container name from the configuration
            CommonContainerName = _configuration["BlobSettings:CommonContainerName"];
        }


        #region CMF-Changes
        /// <summary>
        /// CSVファイルの読み込み
        /// </summary>
        /// <param name="messageList">メッセージ一覧</param>
        /// <param name="csCsvExclusion">appSettngs.jsonから取得したCsCsvExclusion</param>
        /// <param name="csvFilePath">CSVファイルのフルパス</param>
        /// <param name="message">エラー時のメッセージ</param>
        /// <returns>読み込みデータ</returns>
        public static List<User> ReadCsv(Dictionary<string, string> messageList,List<string> csCsvExclusion, string csvFilePath, out string message)
        {
            //get the filename from the filepath before downloading
            if (messageList == null) throw new ArgumentNullException(nameof(messageList));
            try
            {
                LogUtil.Instance.Debug("start");
                message = string.Empty;
                _messageList = messageList;
                var csvData = new List<User>();

                // 拡張子チェック
                if(Path.GetExtension(csvFilePath).ToLower() != EXTENSION_CSV)
                {
                    LogUtil.Instance.Error(_messageList["NoCSVFileSelecte"]);
                    message = _messageList["NoCSVFileSelecte"];
                    return null;
                }
                // ファイル存在チェック               
                var resultString = BlobHelperProvider.BlobHelper.DownloadBlobContent(CommonContainerName , csvFilePath); // Get the BlobClient for the given blob


                // Check if the blob exists
                if (string.IsNullOrEmpty(resultString)) 
                {
                    LogUtil.Instance.Error(_messageList["NonexistentFile"]);
                    message = _messageList["NonexistentFile"];
                    return null;
                }
                // CSVの解析
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (var parser = new TextFieldParser(csvFilePath, Encoding.GetEncoding(ENCODING_SJIS)))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(CSV_DELIMITER);
                    while (!parser.EndOfData)
                    {
                        var row = parser.ReadFields().ToList();
                     
                        if(csCsvExclusion.Where(x => row.First().StartsWith(x)).ToList().Count > 0)
                        {
                            // 読み取り対象外の行のためスキップ
                            continue;
                        }

                        var userList = CreateUserFromRow(row, out message);
                        if (userList == null)
                        {
                            // 不正な行があったため、エラー
                            // ログとメソッド内で出力 
                            LogUtil.Instance.Error(_messageList["CSVFormatError"]);
                            return null;
                        }
                        foreach(var user in userList)
                        {
                            if (csvData.Where(x => x.NegotiationName == user.NegotiationName && x.MailAddress == user.MailAddress).Any())
                            {
                                //商談名の重複
                                message = string.Format(CultureInfo.GetCultureInfo("ja-JP"), _messageList["SameNegotiationName"], user.NegotiationName);
                                LogUtil.Instance.Error(string.Format(CultureInfo.GetCultureInfo("ja-JP"), _messageList["SameNegotiationName"], user.NegotiationName));
                                return null;
                            }
                            if(!csvData.Contains(user))
                            {
                                csvData.Add(user);
                            }
                        }
                    }
                }
                if (csvData.Count == 0)
                {
                    // 有効行が1行もない場合
                    LogUtil.Instance.Error(_messageList["NoEffectiveLine"]);
                    message = _messageList["NoEffectiveLine"];
                    return null;
                }
                return csvData;
            }
            catch (Exception ex)
            {
                LogUtil.Instance.Error(_messageList["CSVFileAnalysisFail"], ex);
                message = _messageList["CSVFileAnalysisFail"];
                return null;
            }
            finally
            {
                LogUtil.Instance.Debug("end");
            }
        }
        #endregion


        /// <summary>
        /// CSVデータの１行分のデータのエラーチェックを行う
        /// </summary>
        /// <param name="row">１行分のデータ</param>
        /// <param name="message">エラー時のメッセージ</param>
        /// <returns>チェック結果</returns>
        private static List<User> CreateUserFromRow(List<string> row,out string message)
        {
            try
            {
                LogUtil.Instance.Debug("start");
                message = string.Empty;

                // 列数チェック
                if (row.Count != MAX_COLUMN)
                {
                    LogUtil.Instance.Error(_messageList["RowError"]);
                    message = _messageList["RowError"];
                    return null;
                }
                if (string.IsNullOrEmpty(row[NEGOTIATION_NAME_INDEX]))
                {
                    // 商談名が存在しない
                    LogUtil.Instance.Error(_messageList["NegotiationNameNoSetting"]);
                    message = _messageList["NegotiationNameNoSetting"];
                    return null;
                }
                if (string.IsNullOrEmpty(row[CUSTOMER_INDEX]) && string.IsNullOrEmpty(row[SUPPORT_CONTACT_INDEX]))
                {
                    // 会社名が存在しない
                    LogUtil.Instance.Error(_messageList["CompanyNameNoSetting"]);
                    message = _messageList["CompanyNameNoSetting"];
                    return null;
                }
                var mailAddressList = new List<string>();
                foreach (var index in MAIL_ADDERSS_COULUN_INDEX)
                {
                    if (!string.IsNullOrWhiteSpace(row[index]) && !mailAddressList.Contains(row[index]))
                    {
                        if (!IsValidMailAddress(row[index]))
                        {
                            LogUtil.Instance.Error(_messageList["MailAddressFormatError"]);
                            message = _messageList["MailAddressFormatError"];
                            return null;
                        }
                        mailAddressList.Add(row[index]);
                    }
                }
                if(mailAddressList.Count == 0)
                {
                    // アドレスが存在しない
                    LogUtil.Instance.Error(_messageList["MailAddressNoSetting"]);
                    message = _messageList["MailAddressNoSetting"];
                    return null;
                }

                return mailAddressList.Select(x => new User()
                {
                    MailAddress = x,
                    NegotiationName = row[NEGOTIATION_NAME_INDEX],
                    CustomerName = row[CUSTOMER_INDEX],
                    SupportContact = row[SUPPORT_CONTACT_INDEX],
                    UpdateContact = row[UPDAT_CONTACT_INDEX],

                }).ToList();
            }
            finally
            {
                LogUtil.Instance.Debug("end");
            }

        }

        /// <summary>
        /// メールアドレスの正規表現チェック
        /// https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        /// </summary>
        /// <param name="mailAddress">メールアドレス</param>
        /// <returns>チェック結果</returns>
        public static bool IsValidMailAddress(string mailAddress)
        {
            try
            {
                LogUtil.Instance.Debug("start");
                try
                {
                    mailAddress = Regex.Replace(mailAddress, @"(@)(.+)$", DomainMapper,
                                          RegexOptions.None, TimeSpan.FromMilliseconds(200));

                    static string DomainMapper(Match match)
                    {
                        var idn = new IdnMapping();
                        var domainName = idn.GetAscii(match.Groups[2].Value);
                        return match.Groups[1].Value + domainName;
                    }
                }
                catch (RegexMatchTimeoutException e)
                {
                    LogUtil.Instance.Error(_messageList["RegexMatchTimeoutException"], e);
                    return false;
                }
                catch (ArgumentException e)
                {
                    LogUtil.Instance.Error(_messageList["ArgumentException"], e);
                    return false;
                }

                try
                {
                    return Regex.IsMatch(mailAddress,
                        @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                        RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                }
                catch (RegexMatchTimeoutException e)
                {
                    LogUtil.Instance.Error(_messageList["RegexMatchTimeoutException"], e);
                    return false;
                }
            }
            finally
            {
                LogUtil.Instance.Debug("end");
            }
        }
    }
}
