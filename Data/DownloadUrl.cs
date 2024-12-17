//=============================================================================
// DownloadUrl
// ダウンロード先URL作成クラス
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

namespace SKYCOM.DLManagement.Data
{
    public class DownloadUrl
    {
        public string DLStatusManagementId { get; set; }
        public string NegotiationName { get; set; }
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public string Urlstr { get; set; }
        public string MailAddress { get; set; }
    }
}
