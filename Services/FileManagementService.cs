//=============================================================================
// FIleManagementService
// ファイルディレクトリ情報を所得するためのファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Azure.Storage.Blobs.Models;
using MimeKit;
using SKYCOM.DLManagement.AzureHelper;
using SKYCOM.DLManagement.Data;
using SKYCOM.DLManagement.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SKYCOM.DLManagement.Services
{
    public enum DLState : int
    {
        /// <summary>
        /// ダウンロード未
        /// </summary>
        None = 0,
        /// <summary>
        /// ダウンロード済
        /// </summary>
        Downloaded = 1,
    }

    public class FileManagementService
    {
        private readonly Message _message;
        public FileManagementService(Message message)
        {
            _message = message;
        }

        /// <summary>
        /// 指定されたディレクトリ配下のディレクトリ及びファイルのリストを取得する
        /// </summary>
        /// <param name="serverFileInfo">ルートディレクトリ情報</param>
        /// <returns></returns>
        public Task<List<ServerFileInfo>> GetServerFileInfos_Old(ServerFileInfo serverFileInfo)
        {
            //Get Blobs List
          
            
            try
            {                
                if (serverFileInfo == null) throw new ArgumentNullException(nameof(serverFileInfo));
                LogUtil.Instance.Trace("start");
                if (!Directory.Exists(serverFileInfo.FullPath))
                {
                    //存在しないディレクトリパスの場合空のリストを返す
                    return Task.FromResult(new List<ServerFileInfo>());
                }
                if (File.GetAttributes(serverFileInfo.FullPath).HasFlag(FileAttributes.Directory))
                {
                    var dir = new DirectoryInfo(serverFileInfo.FullPath);
                    var serverFiles = dir.GetDirectories("*", SearchOption.TopDirectoryOnly).Select(x => new ServerFileInfo() { Date = x.LastWriteTime, FileSize = 0, FileName = x.Name, FullPath = x.FullName, Attributes = x.Attributes, IsFile = false }).OrderBy(FileName => FileName.FileName).ToList();
                    for (var i = serverFiles.Count - 1; i >= 0; i--)
                    {
                        if (!IsFolderAccess(serverFiles[i].FullPath))
                        {
                            serverFiles.Remove(serverFiles[i]);
                        }
                    }
                    serverFiles.AddRange(dir.GetFiles("*.*", SearchOption.TopDirectoryOnly).Select(x => new ServerFileInfo() { Date = x.LastWriteTime, FileSize = (int)x.Length, FileName = x.Name, FullPath = x.FullName, Attributes = x.Attributes, IsFile = true }).OrderBy(FileName => FileName.FileName).ToList());
                    return Task.FromResult(serverFiles.ToList());
                }
                return Task.FromResult(new List<ServerFileInfo>());
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }


        /// <summary>
        /// Return the list of blobs along with container name from the azure storage. CMF - Changes
        /// </summary>
        /// <param name="serverFileInfo"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task<List<ServerFileInfo>> GetServerFileInfos(ServerFileInfo serverFileInfo)
        {
            LogUtil.Instance.Trace("start");
            //Get Blobs List
            try
            {
                var blobsList = BlobHelperProvider.BlobHelper.GetBlobList(serverFileInfo.FullPath);
                //var serverfiles = new List<ServerFileInfo>();
                //foreach (var blob in blobsList) {
                //    serverfiles.Add(new ServerFileInfo { FileName = blob.FileName, FullPath = blob.ContainerName, FileSize = blob.FileSize, Date = blob.Date });
                //        }
                return Task.FromResult(blobsList);               
            }
            finally
            {
                LogUtil.Instance.Trace("end");
            }
        }

        /// <summary>
        /// フォルダへのアクセス権チェック
        /// </summary>
        /// <param name="folderPath">フォルダのフルパス</param>
        /// <returns>書き込み権限あり：true、それ以外：false</returns>
        private bool IsFolderAccess(string folderPath)
        {
            bool isWriteAccess = true;
            try
            {
                LogUtil.Instance.Debug(Path.GetFileName(folderPath));
                new DirectoryInfo(folderPath).GetFiles();
                //var collection = security.GetAccessRules(true, true, typeof(NTAccount));
                //var collection =
                //    FileSystemAclExtensions.GetAccessControl(new DirectoryInfo(folderPath))
                //        .GetAccessRules(true, true, typeof(NTAccount));
                //foreach (FileSystemAccessRule rule in collection)
                //{
                //    var access = rule.AccessControlType
                //        + "\t" + (rule.IdentityReference as NTAccount).Value
                //        + "\t" + rule.FileSystemRights.ToString()
                //        + "\t" + rule.IsInherited.ToString();
                //    LogUtil.Instance.Debug(access);

                //    if (rule.AccessControlType == AccessControlType.Allow)
                //    {
                //        isWriteAccess = true;
                //        break;
                //    }
                //}
            }
            catch (UnauthorizedAccessException ex)
            {
                isWriteAccess = false;
                LogUtil.Instance.Error(_message.MessageList["PermissionError"], ex);
            }
            return isWriteAccess;
        }
    }
}
