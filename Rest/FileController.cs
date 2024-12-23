//=============================================================================
// FileController
// アップロードとダウンロードの処理
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SKYCOM.DLManagement.Data;
using SKYCOM.DLManagement.Entity;
using SKYCOM.DLManagement.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;
using HttpGetAttribute = Microsoft.AspNetCore.Mvc.HttpGetAttribute;
using HttpPostAttribute = Microsoft.AspNetCore.Mvc.HttpPostAttribute;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using SKYCOM.DLManagement.AzureHelper; // CMF-Changes

namespace SKYCOM.DLManagement.Rest
{
    [ApiController]
    [RequestFormLimits(MultipartBodyLengthLimit = 3758096384)]
    [RequestSizeLimit(3758096384)]
    public class FileController : ControllerBase
    {
        private const string UPLOAD_PATH = "path";
        private const string FILE_SIZE = "fileSize";
        private const string HEADER_DISPOSITION = "content-disposition";
        private const string ATTACHMENT = "attachment;filename={0}";
        private const string ENCODING_SJIS = "Shift_JIS";
        private const string DATE_TIME = "yyyy/MM/dd HH:mm:ss";

        private readonly DbAccess _context;
        private readonly IOptions<Settings> _settings;
        private readonly Message _message;
        private readonly AzBlobStorageHelper _azBlobStorageHelper;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        /// <param name="message"></param>
        public FileController(DbAccess context, IOptions<Settings> settings, Message message,AzBlobStorageHelper azBlobStorageHelper)
        {
            _context = context;
            _settings = settings;
            _message = message;
            _azBlobStorageHelper = azBlobStorageHelper;
        }

        /// <summary>
        /// エラー返却
        /// </summary>
        /// <param name="statusCode">HTTPステータスコード</param>
        /// <param name="errorCode">内部エラーコード</param>
        /// <param name="message">メッセージ</param>
        /// <param name="ex">Exception</param>
        /// <returns>ステータスコード</returns>
        private IActionResult ErrorStatus(int statusCode, string errorCode, string message, Exception ex = null)
        {
            LogUtil.Instance.Error(message, ex);
            var err = new
            {
                ErrorCode = errorCode,
                Message = message
            };
            return StatusCode(statusCode, err);
        }

        #region Upload - Existing Code
        ///// <summary>
        ///// ファイルアップロード
        ///// </summary>
        ///// <param name="file">ファイルデータ</param>
        ///// <param name="message">Message</param>
        ///// <returns>処理結果</returns>
        //[HttpPost]
        //[Route("api/v1/file")]
        //public async Task<IActionResult> FileUpload(IFormFile file)
        //{
        //    LogUtil.Instance.Info("start");
        //    try
        //    {
        //        // リクエストのバリデーションチェック
        //        if (file == null || file.Length == 0)
        //        {
        //            return ErrorStatus(400, "DL001", _message.MessageList["NonexistentUploadFile"]);
        //        }
        //        if (!Request.Form.Keys.Contains(UPLOAD_PATH))
        //        {
        //            return ErrorStatus(400, "DL002", _message.MessageList["NonexistentUploadPathKey"]);
        //        }
        //        var uploadPath = Request.Form[UPLOAD_PATH].ToString();
        //        if (!Request.Form.Keys.Contains(FILE_SIZE))
        //        {
        //            return ErrorStatus(400, "DL003", _message.MessageList["NonexistentFileSizeKey"]);
        //        }
        //        if (!long.TryParse(Request.Form[FILE_SIZE].ToString(), out long fileSize))
        //        {
        //            return ErrorStatus(400, "DL004", _message.MessageList["IsNotCorrectFileSize"]);
        //        }
        //        if (!Directory.Exists(uploadPath))
        //        {
        //            return ErrorStatus(500, "DL005", _message.MessageList["FolderNotFound"]);
        //        }

        //        // ファイル保存
        //        var filePath = Path.Combine(uploadPath, file.FileName);
        //        try
        //        {
        //            using (var stream = System.IO.File.Create(filePath))
        //            {
        //                await file.CopyToAsync(stream).ConfigureAwait(false);
        //            }
        //            // ファイルサイズのチェック
        //            var saveFileSize = new FileInfo(filePath).Length;
        //            if (saveFileSize != fileSize)
        //            {
        //                try
        //                {
        //                    // ファイルを削除
        //                    System.IO.File.Delete(filePath);
        //                }
        //                catch (Exception ex)
        //                {
        //                    LogUtil.Instance.Error(_message.MessageList["DeleteFailed"] + $"{filePath}", ex);
        //                }
        //                return ErrorStatus(500, "DL006", _message.MessageList["DoNotMatchFileSize"] + $"[{fileSize}]→[{saveFileSize}]");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return ErrorStatus(500, "DL007", _message.MessageList["SaveFailed"] + $"{filePath}", ex);
        //        }
        //        // 正常終了
        //        var body = new
        //        {
        //            ErrorCode = string.Empty,
        //            Message = string.Empty
        //        };
        //        return StatusCode(201, body);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ErrorStatus(500, "DL008", _message.MessageList["UploadUnexpectedError"], ex);
        //    }
        //    finally
        //    {
        //        LogUtil.Instance.Info("end");
        //    }
        //}
        #endregion

        #region Upload - CMF-Changes
        /// <summary>
        /// ファイルアップロード
        /// </summary>
        /// <param name="file">ファイルデータ</param>
        /// <param name="message">Message</param>
        /// <returns>処理結果</returns>
        [HttpPost]
        [Route("api/v1/file")]
        public async Task<IActionResult> FileUpload(IFormFile file)
        {
            LogUtil.Instance.Info("start");
            try
            {
                // リクエストのバリデーションチェック
                if (file == null || file.Length == 0)
                {
                    return ErrorStatus(400, "DL001", _message.MessageList["NonexistentUploadFile"]);
                }
                //if (!Request.Form.Keys.Contains(UPLOAD_PATH)) //Upload path should be replaced by the blob container name  as the files are uploaded into blob storage
                //{
                //    return ErrorStatus(400, "DL002", _message.MessageList["NonexistentUploadPathKey"]);
                //}

                // Setting the upload path as blob container
                //Customer will replace this containerName by actual blob container name on production
                var BlobContainerName = _settings.Value.BlobSettings.CommonContainerName; 

                if (!Request.Form.Keys.Contains(FILE_SIZE))
                {
                    return ErrorStatus(400, "DL003", _message.MessageList["NonexistentFileSizeKey"]);
                }
                if (!long.TryParse(Request.Form[FILE_SIZE].ToString(), out long fileSize))
                {
                    return ErrorStatus(400, "DL004", _message.MessageList["IsNotCorrectFileSize"]);
                }
                //Container existance check done in the AzBlobhelper.cs file
                //if (!Directory.Exists(uploadPath))
                //{
                //    return ErrorStatus(500, "DL005", _message.MessageList["FolderNotFound"]);
                //}

                // ファイル保存
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream).ConfigureAwait(false);
                        _azBlobStorageHelper.UploadFileToAzure(stream, file.FileName, BlobContainerName);
                    }
                    //File size check is not needed here // so commented
                    // ファイルサイズのチェック                    
                    //var saveFileSize = new FileInfo(filePath).Length;
                    //if (saveFileSize != fileSize)
                    //{
                    //    try
                    //    {
                    //        // ファイルを削除
                    //        System.IO.File.Delete(filePath);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        LogUtil.Instance.Error(_message.MessageList["DeleteFailed"] + $"{filePath}", ex);
                    //    }
                    //    return ErrorStatus(500, "DL006", _message.MessageList["DoNotMatchFileSize"] + $"[{fileSize}]→[{saveFileSize}]");
                    //}
                }
                catch (Exception ex)
                {
                    return ErrorStatus(500, "DL007", _message.MessageList["SaveFailed"] + $"{file.FileName}", ex);
                }
                // 正常終了
                var body = new
                {
                    ErrorCode = string.Empty,
                    Message = string.Empty
                };
                return StatusCode(201, body);
            }
            catch (Exception ex)
            {
                return ErrorStatus(500, "DL008", _message.MessageList["UploadUnexpectedError"], ex);
            }
            finally
            {
                LogUtil.Instance.Info("end");
            }
        }
        #endregion


        #region Download - Old code
        ///// <summary>
        ///// ファイルダウンロード
        ///// </summary>
        ///// <param name="id">DL状態管理ID</param>
        ///// <returns>処理結果</returns>
        //[HttpGet]
        //[Route("api/v1/file")]
        //public async Task<IActionResult> FileDownloadAsync(string id)
        //{
        //    LogUtil.Instance.Info("start");
        //    try
        //    {
        //        // 設定ファイルの読み込み
        //        List<string> csIpAddress = null;
        //        int? transactionTimeout = null;
        //        try
        //        {
        //            csIpAddress = _settings.Value.CsIpAddress;
        //            transactionTimeout = _settings.Value.TransactionTimeout;
        //        }
        //        catch (Exception ex)
        //        {
        //            LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
        //        }
        //        var ipAddress = GetIpAddress(csIpAddress, out bool isCsFlag);

        //        // バリデーションチェック
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            return ErrorStatus(400, "DL009", _message.MessageList["NotGetDLManagementStatus"] + $"{id}");
        //        }

        //        // DBレコードチェック
        //        var filePath = string.Empty;
        //        _context.Database.SetCommandTimeout(transactionTimeout);
        //        using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted).ConfigureAwait(false))
        //        {
        //            var dlStatusManegement = await _context.DLStatusManagements.Where(x => x.DLStatusManagementId == id).FirstOrDefaultAsync().ConfigureAwait(false);
        //            if (dlStatusManegement == null)
        //            {
        //                return ErrorStatus(400, "DL009", string.Format(_message.MessageList["DBRecordNotFound"], id));
        //            }
        //            var product = await _context.Product.Where(x => x.ProductId == dlStatusManegement.ProductId).FirstOrDefaultAsync().ConfigureAwait(false);
        //            if (product == null)
        //            {
        //                return ErrorStatus(404, "DL010", string.Format(_message.MessageList["DBRecordNotFound"], dlStatusManegement.ProductId));
        //            }

        //            filePath = product.ProductFilePath;
        //            // ファイルの存在チェック
        //            if (!System.IO.File.Exists(filePath))
        //            {
        //                return ErrorStatus(404, "DL011", string.Format(_message.MessageList["FileNotFound"], filePath));
        //            }

        //            if (!isCsFlag)
        //            {
        //                try
        //                {
        //                    if (!await UpdateDLStatusmanagement(ipAddress, dlStatusManegement).ConfigureAwait(false))
        //                    {
        //                        return ErrorStatus(500, "DL012", _message.MessageList["DBUpdateFailed"]);
        //                    }
        //                    await transaction.CommitAsync().ConfigureAwait(false);
        //                }
        //                catch (Exception ex)
        //                {
        //                    return ErrorStatus(500, "DL012", _message.MessageList["DBUpdateFailed"], ex);
        //                }
        //            }
        //        }
        //        _context.Database.SetCommandTimeout(null);
        //        var fileName = Path.GetFileName(filePath);
        //        Response.Headers.Append(HEADER_DISPOSITION, string.Format(ATTACHMENT, fileName));
        //        var fs = new FileStream(filePath, FileMode.Open);
        //        return new FileStreamResult(fs, MimeKit.MimeTypes.GetMimeType(fileName));
        //    }
        //    catch (Exception ex)
        //    {
        //        return ErrorStatus(500, "DL013", _message.MessageList["FileDownloadFailed"], ex);
        //    }
        //    finally
        //    {
        //        LogUtil.Instance.Info("end");
        //    }
        //}


        /// CMF-Changes       
        /// <summary>
        /// ファイルダウンロード
        /// </summary>
        /// <param name="id">DL状態管理ID</param>
        /// <returns>処理結果</returns>
        [HttpGet]
        [Route("api/v1/file")]
        public async Task<IActionResult> FileDownloadAsync(string id)
        {
            LogUtil.Instance.Info("start");
            try
            {
                // 設定ファイルの読み込み
                List<string> csIpAddress = null;
                int? transactionTimeout = null;
                try
                {
                    csIpAddress = _settings.Value.CsIpAddress;
                    transactionTimeout = _settings.Value.TransactionTimeout;
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
                }
                var ipAddress = GetIpAddress(csIpAddress, out bool isCsFlag);

                // バリデーションチェック
                if (string.IsNullOrEmpty(id))
                {
                    return ErrorStatus(400, "DL009", _message.MessageList["NotGetDLManagementStatus"] + $"{id}");
                }

                // DBレコードチェック
                 var filePath = string.Empty;
                _context.Database.SetCommandTimeout(transactionTimeout);
                using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted).ConfigureAwait(false))
                {
                    var dlStatusManegement = await _context.DLStatusManagements.Where(x => x.DLStatusManagementId == id).FirstOrDefaultAsync().ConfigureAwait(false);
                    if (dlStatusManegement == null)
                    {
                        return ErrorStatus(400, "DL009", string.Format(_message.MessageList["DBRecordNotFound"], id));
                    }
                    var product = await _context.Product.Where(x => x.ProductId == dlStatusManegement.ProductId).FirstOrDefaultAsync().ConfigureAwait(false);
                    if (product == null)
                    {
                        return ErrorStatus(404, "DL010", string.Format(_message.MessageList["DBRecordNotFound"], dlStatusManegement.ProductId));
                    }


                    filePath = product.ProductFilePath; 

                    //Directory existance check done in AzBlobStorageHelper.cs page
                    //// ファイルの存在チェック
                    //if (!System.IO.File.Exists(filePath))
                    //{
                    //    return ErrorStatus(404, "DL011", string.Format(_message.MessageList["FileNotFound"], filePath));
                    //}

                    if (!isCsFlag)
                    {
                        try
                        {
                            if (!await UpdateDLStatusmanagement(ipAddress, dlStatusManegement).ConfigureAwait(false))
                            {
                                return ErrorStatus(500, "DL012", _message.MessageList["DBUpdateFailed"]);
                            }
                            await transaction.CommitAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            return ErrorStatus(500, "DL012", _message.MessageList["DBUpdateFailed"], ex);
                        }
                    }
                }
                _context.Database.SetCommandTimeout(null);
                var fileName = Path.GetFileName(filePath);
                var containerName = _settings.Value.BlobSettings.CommonContainerName;
                Response.Headers.Append(HEADER_DISPOSITION, string.Format(ATTACHMENT, fileName));
                //var fs = new FileStream(filePath, FileMode.Open);
                //Reading file from blob below
                var ms = _azBlobStorageHelper.DownloadBlobToMemoryStream(containerName, fileName);
                return new FileStreamResult(ms, MimeKit.MimeTypes.GetMimeType(fileName));
            }
            catch (Exception ex)
            {
                return ErrorStatus(500, "DL013", _message.MessageList["FileDownloadFailed"], ex);
            }
            finally
            {
                LogUtil.Instance.Info("end");
            }
        }

        /// <summary>
        /// IPアドレスの取得
        /// </summary>
        /// <param name="csIpAddress">カスタマーのアドレス一覧</param>
        /// <param name="isCsFlag">カスタマーのフラグ</param>
        /// <returns>IPアドレス</returns>
        private string GetIpAddress(List<string> csIpAddress, out bool isCsFlag)
        {
            try
            {
                LogUtil.Instance.Info("start");
                // 接続元IPアドレス
                isCsFlag = false;
                var ipAddress = string.Empty;
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    var ipAddressList = Request.Headers["X-Forwarded-For"].ToString().Split(",").ToList();
                    LogUtil.Instance.Debug($"Request.Headers[X-Forwarded-For]:{ipAddressList}");
                    ipAddress = ipAddressList.Where(x => csIpAddress.Contains(x)).FirstOrDefault();
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        LogUtil.Instance.Debug("Client is Custer User.");
                        isCsFlag = true;
                    }
                }
                return ipAddress;
            }
            finally
            {
                LogUtil.Instance.Info("end");
            }
        }

        /// <summary>
        /// DL状態管理テーブルを更新する
        /// </summary>
        /// <param name="ipAddress">接続元IPアドレス</param>
        /// <param name="dlStatusManegement">DL状態管理テーブル</param>
        /// <returns>更新結果</returns>
        private async Task<bool> UpdateDLStatusmanagement(string ipAddress, DLStatusManagements dlStatusManegement)
        {
            try
            {
                LogUtil.Instance.Info("start");
                // DL状態管理TBLを更新
                var dlStatusManegementList = new List<DLStatusManagements>();
                var user = await _context.User.Where(x => x.UserId == dlStatusManegement.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
                if (user != null)
                {
                    var userIds = await _context.User.Where(x => x.MailAddress == user.MailAddress).Select(x => x.UserId).ToListAsync().ConfigureAwait(false);
                    dlStatusManegementList.AddRange(await _context.DLStatusManagements.Where(
                        x => userIds.Contains(x.UserId) &&
                        x.SendGroupId == dlStatusManegement.SendGroupId &&
                        x.ProductId == dlStatusManegement.ProductId).ToListAsync().ConfigureAwait(false));
                }
                if (dlStatusManegementList.Count == 0)
                {
                    return false;
                }
                var dateTime = DateTime.Now;
                dlStatusManegementList.ForEach(x =>
                {
                    x.IpAddress = ipAddress;
                    x.DLStatus = (int)DLState.Downloaded;
                    x.DLDateTime = dateTime;
                    ++x.DLCount;
                });
                var result = await _context.SaveChangesAsync().ConfigureAwait(false);
                if (result != dlStatusManegementList.Count)
                {
                    return false;
                }
                return true;
            }
            finally
            {
                LogUtil.Instance.Info("end");
            }
        }
        #endregion

        #region CsvDownload
        ///// <summary>
        ///// DL状態管理CSVのダウンロード
        ///// </summary>
        ///// <returns>DL状態管理CSV</returns>
        //[HttpGet]
        //[Route("downloadcsv")]
        //public async Task<IActionResult> CsvDownloadAsync_Old()
        //{
        //    try
        //    {
        //        LogUtil.Instance.Info("start");
        //        var csvData = (await _context.DLStatusManagements
        //            .Join(_context.User, d => d.UserId, u => u.UserId, (d, u) => new { d, u })
        //            .Join(_context.Product, du => du.d.ProductId, p => p.ProductId, (du, p) => new
        //            {
        //                DLStateManagementId = du.d.DLStatusManagementId,
        //                du.d.MailSendDateTime,
        //                du.d.ReservedDateTime,
        //                du.d.MailSendExecutedDateTime,
        //                du.d.FormMailAddress,
        //                ToMailAddress = du.u.MailAddress,
        //                DLSystemName = p.ProductName,
        //                DLFilePath = p.ProductFilePath,
        //                du.u.NegotiationName,
        //                CompanyName = du.u.CustomerName,
        //                du.u.SupportContact,
        //                du.u.UpdateContact,
        //                DLIpAddress = du.d.IpAddress,
        //                DLUrlstr = du.d.Urlstr,
        //                du.d.DLStatus,
        //                du.d.DLCount,
        //                du.d.DLDateTime,
        //                du.d.InsertDateTime,
        //                du.d.UpdateDateTime
        //            }).OrderByDescending(x => x.UpdateDateTime).ToListAsync().ConfigureAwait(false)).Select(x => new DLStateManagementCsv()
        //            {
        //                DLStateManagementId = x.DLStateManagementId,
        //                MailSendDateTime = (x.MailSendDateTime == null) ? string.Empty : ((DateTime)x.MailSendDateTime).ToString(DATE_TIME),
        //                ReservedDateTime = (x.ReservedDateTime == null) ? string.Empty : ((DateTime)x.ReservedDateTime).ToString(DATE_TIME),
        //                MailSendExecutedDateTime = (x.MailSendExecutedDateTime == null) ? string.Empty : ((DateTime)x.MailSendExecutedDateTime).ToString(DATE_TIME),
        //                FormMailAddress = x.FormMailAddress,
        //                ToMailAddress = x.ToMailAddress,
        //                DLSystemName = x.DLSystemName,
        //                DLFilePath = x.DLFilePath,
        //                NegotiationName = x.NegotiationName,
        //                CompanyName = x.CompanyName,
        //                SupportContact = x.SupportContact,
        //                UpdateContact = x.UpdateContact,
        //                DLIpAddress = x.DLIpAddress,
        //                DLUrlstr = x.DLUrlstr,
        //                DLStatus = x.DLStatus,
        //                DLCount = x.DLCount,
        //                DLDateTime = (x.DLDateTime == null) ? string.Empty : ((DateTime)x.DLDateTime).ToString(DATE_TIME),
        //                InsertDateTime = x.InsertDateTime.ToString(DATE_TIME),
        //                UpdateDateTime = x.UpdateDateTime.ToString(DATE_TIME)
        //            });
        //        var fileName = string.Format("{0}_{1}.csv", "DLManagement", DateTime.Now.ToString("yyyyMMddHHmmss"));

        //        try
        //        {
        //            if (!string.IsNullOrEmpty(_settings.Value.Csv.CsvFileName))
        //            {
        //                fileName = string.Format("{0}_{1}.csv", _settings.Value.Csv.CsvFileName, DateTime.Now.ToString("yyyyMMddHHmmss"));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
        //        }

        //        Response.Headers.Append(HEADER_DISPOSITION, string.Format(ATTACHMENT, fileName));
        //        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //        using var ms = new MemoryStream();
        //        using TextWriter tw = new StreamWriter(ms, Encoding.GetEncoding(ENCODING_SJIS));
        //        using var csv = new CsvWriter(tw, new CsvConfiguration(CultureInfo.GetCultureInfo("ja-JP")){ NewLine = "\r\n", HasHeaderRecord = true});
        //        tw.NewLine = "\r\n";
        //        csv.Context.RegisterClassMap<DLStateManagementMapper>();
        //        csv.WriteRecords(csvData);
        //        tw.Flush();
        //        ms.Seek(0, SeekOrigin.Begin);
        //        return File(ms.ToArray(), "text/csv", fileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ErrorStatus(500, "DL013", _message.MessageList["FileDownloadFailed"], ex);
        //    }
        //    finally
        //    {
        //        LogUtil.Instance.Info("end");
        //    }
        //}

        #region CMF-Changes
        /// <summary>
        /// DL状態管理CSVのダウンロード
        /// </summary>
        /// <returns>DL状態管理CSV</returns>
        [HttpGet]
        [Route("downloadcsv")]
        public async Task<IActionResult> CsvDownloadAsync()
        {
            try
            {
                LogUtil.Instance.Info("start");
                var csvData = (await _context.DLStatusManagements
                    .Join(_context.User, d => d.UserId, u => u.UserId, (d, u) => new { d, u })
                    .Join(_context.Product, du => du.d.ProductId, p => p.ProductId, (du, p) => new
                    {
                        DLStateManagementId = du.d.DLStatusManagementId,
                        du.d.MailSendDateTime,
                        du.d.ReservedDateTime,
                        du.d.MailSendExecutedDateTime,
                        du.d.FormMailAddress,
                        ToMailAddress = du.u.MailAddress,
                        DLSystemName = p.ProductName,
                        DLFilePath = p.ProductFilePath,
                        du.u.NegotiationName,
                        CompanyName = du.u.CustomerName,
                        du.u.SupportContact,
                        du.u.UpdateContact,
                        DLIpAddress = du.d.IpAddress,
                        DLUrlstr = du.d.Urlstr,
                        du.d.DLStatus,
                        du.d.DLCount,
                        du.d.DLDateTime,
                        du.d.InsertDateTime,
                        du.d.UpdateDateTime
                    }).OrderByDescending(x => x.UpdateDateTime).ToListAsync().ConfigureAwait(false)).Select(x => new DLStateManagementCsv()
                    {
                        DLStateManagementId = x.DLStateManagementId,
                        MailSendDateTime = (x.MailSendDateTime == null) ? string.Empty : ((DateTime)x.MailSendDateTime).ToString(DATE_TIME),
                        ReservedDateTime = (x.ReservedDateTime == null) ? string.Empty : ((DateTime)x.ReservedDateTime).ToString(DATE_TIME),
                        MailSendExecutedDateTime = (x.MailSendExecutedDateTime == null) ? string.Empty : ((DateTime)x.MailSendExecutedDateTime).ToString(DATE_TIME),
                        FormMailAddress = x.FormMailAddress,
                        ToMailAddress = x.ToMailAddress,
                        DLSystemName = x.DLSystemName,
                        DLFilePath = x.DLFilePath,
                        NegotiationName = x.NegotiationName,
                        CompanyName = x.CompanyName,
                        SupportContact = x.SupportContact,
                        UpdateContact = x.UpdateContact,
                        DLIpAddress = x.DLIpAddress,
                        DLUrlstr = x.DLUrlstr,
                        DLStatus = x.DLStatus,
                        DLCount = x.DLCount,
                        DLDateTime = (x.DLDateTime == null) ? string.Empty : ((DateTime)x.DLDateTime).ToString(DATE_TIME),
                        InsertDateTime = x.InsertDateTime.ToString(DATE_TIME),
                        UpdateDateTime = x.UpdateDateTime.ToString(DATE_TIME)
                    });
                var fileName = string.Format("{0}_{1}.csv", "DLManagement", DateTime.Now.ToString("yyyyMMddHHmmss"));

                try
                {
                    if (!string.IsNullOrEmpty(_settings.Value.Csv.CsvFileName))
                    {
                        fileName = string.Format("{0}_{1}.csv", _settings.Value.Csv.CsvFileName, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Instance.Warn(_message.MessageList["FraudSettings"], ex);
                }

                Response.Headers.Append(HEADER_DISPOSITION, string.Format(ATTACHMENT, fileName));
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                //using var ms = new MemoryStream();
                //using TextWriter tw = new StreamWriter(ms, Encoding.GetEncoding(ENCODING_SJIS));
                //using var csv = new CsvWriter(tw, new CsvConfiguration(CultureInfo.GetCultureInfo("ja-JP")) { NewLine = "\r\n", HasHeaderRecord = true });
                //tw.NewLine = "\r\n";
                //csv.Context.RegisterClassMap<DLStateManagementMapper>();
                //csv.WriteRecords(csvData);
                //tw.Flush();
                //ms.Seek(0, SeekOrigin.Begin);
                //return File(ms.ToArray(), "text/csv", fileName);

                // Download the blob content as a memory stream
                var containerName = _settings.Value.BlobSettings.CommonContainerName; // This should be replaced by customer with actual container name
                Stream blobStream = _azBlobStorageHelper.DownloadBlobToMemoryStream(containerName, fileName);

                // Reset the position of the stream to the beginning
                blobStream.Seek(0, SeekOrigin.Begin);

                // Return the blob content as a downloadable file
                return File(blobStream, "text/csv", fileName);                
            }
            catch (Exception ex)
            {
                return ErrorStatus(500, "DL013", _message.MessageList["FileDownloadFailed"], ex);
            }
            finally
            {
                LogUtil.Instance.Info("end");
            }
        }
        #endregion
        #endregion


        #region CMF-Changes
        #endregion
    }
}
