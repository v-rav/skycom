//=============================================================================
// Message
// メッセージ情報をJSONファイルから読み込み、他のPageでも使えるようにする
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SKYCOM.DLManagement.Data
{
    public class Message
    {
        public Dictionary<string, string> MessageList { get; }
        public Message(string jsonPath)
        {
            MessageList = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(jsonPath));
        }
    }
}
