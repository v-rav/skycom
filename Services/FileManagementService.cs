//=============================================================================
// FIleManagementService
// ファイルディレクトリ情報を所得するためのファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
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
        private readonly AzBlobStorageHelper _blobStorageHelper;


        // Constructor that accepts AzBlobStorageHelper dependency
        public FileManagementService(Message message, AzBlobStorageHelper blobStorageHelper)
        {
            _message = message;
            _blobStorageHelper = blobStorageHelper;
        }

        /// <summary>
        /// 指定されたディレクトリ配下のディレクトリ及びファイルのリストを取得する
        /// </summary>
        /// <param name="serverFileInfo">ルートディレクトリ情報</param>
        /// <returns></returns>
        public Task<List<ServerFileInfo>> GetServerFileInfos(ServerFileInfo serverFileInfo)
        {
            if (serverFileInfo == null) throw new ArgumentNullException(nameof(serverFileInfo));

            try
            {
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
        

        public async Task<List<ServerFileInfo>> GetBlobFileInfos(string containerName)
        {
            
            

            try
            {
                LogUtil.Instance.Trace("start");

                // Get BlobContainerClient for the container

                var containerClient = _blobStorageHelper.GetBlobContainerClient(containerName); // Get container client

                if (!await containerClient.ExistsAsync())
                {
                    // If the container doesn't exist, return an empty list
                    LogUtil.Instance.Warn($"Blob container not found: {containerName}");
                    return new List<ServerFileInfo>();
                }

                var blobInfos = new List<ServerFileInfo>();

                // List all blobs (files and folders) in the container
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    // Check if the blob is a directory (virtual folder) or file
                    bool isFile = !blobItem.Name.EndsWith("/"); // Treat "folders" as blobs with a trailing "/"

                    var blobInfo = new ServerFileInfo
                    {
                        FileName = blobItem.Name,
                        FullPath = blobItem.Name, // Full path of the blob
                        IsFile = isFile,
                        FileSize = isFile ? (int)blobItem.Properties.ContentLength : 0, // Files have size, directories don't
                        Date = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue, // Convert DateTimeOffset to DateTime
                        Attributes = FileAttributes.Normal // No direct equivalent in Blob Storage
                    };


                    // Add to the list if it's a file or if you want to include folders
                    if (isFile || !isFile) // You can modify this condition if you want to include virtual directories as well
                    {
                        blobInfos.Add(blobInfo);
                    }
                }

                return blobInfos.OrderBy(file => file.FileName).ToList();
            }
            catch (Exception ex)
            {
                LogUtil.Instance.Warn($"Error occurred while fetching server file infos from Azure Blob Storage: {ex.Message}", ex);
                return new List<ServerFileInfo>();
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
