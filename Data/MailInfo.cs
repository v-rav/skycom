//=============================================================================
// MailInfo
// メール情報のプロパティ定義
//
// Copyright (C) SKYCOM Corporation
// EamdM
//=============================================================================

using System.Collections.Generic;

namespace SKYCOM.DLManagement.Data
{
    public class MailInfo
    {
        public List<string> DLStatusManagementIds { get; set; }
        public string Subject { get; set; }     // 件名
        public string Body { get; set; }        // 本文
        public string Address { get; set; }       // 送信アドレス
    }
}
