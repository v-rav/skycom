//=============================================================================
// Message
// メッセージ情報をJSONファイルから読み込み、他のPageでも使えるようにする
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using SKYCOM.DLManagement.AzureHelper;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace SKYCOM.DLManagement.Data
{
    public class Message
    {
        public Dictionary<string, string> MessageList { get; }
        public Message(string jsonPath,string containername)
        {
            #region CMF-Changes
            MemoryStream memoryStream = AzBlobStorageHelper.DownloadBlobToMemoryStream(containername, jsonPath);
            // Deserialize the JSON content from the memory stream
            MessageList = JsonSerializer.Deserialize<Dictionary<string, string>>(new StreamReader(memoryStream).ReadToEnd());
            #endregion

            #region existing code
            //  MessageList = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(jsonPath));
            #endregion 
        }
    }
}
