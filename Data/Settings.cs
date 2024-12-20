//=============================================================================
// Settings
// CS使用PCのIPアドレス、除外IPアドレス、CSVファイル名　プロパティ
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using System.Collections.Generic;

namespace SKYCOM.DLManagement.Data
{
    public class Settings
    {
        public Dictionary<string, string> TemplateList { get; set; }
        public string DefaultTemplate { get; set; }
        public ReleaseGuide ReleaseGuide { get; set; }
        public SGmail Gmail { get; set; }
        public SUpload Upload { get; set; }
        public Dictionary<string, string> ProductList { get; set; }
        public Csv Csv { get; set; }
        public List<string> CsIpAddress { get; set; }
        public int TransactionTimeout { get; set; }
    }

    public class ReleaseGuide
    {
        public string SignatureValue { get; set; }
        public string SendFromAddressValue { get; set; }
        public int MaxFileSelectSize { get; set; }
    }

    public class SGmail
    {
        public string KeyFilePath { get; set; }
        public string ServiceAccountEmail { get; set; }
        public string SecretPassword { get; set; }
        public string GmailClientId { get; set; }
        public string GmailClientSecret { get; set; }
        public string GmailAppName { get; set; }
        public string GmailScope { get; set; }
        public bool IsServiceAccount { get; set; }
    }

    public class SUpload
    {
        public string UploadFileRootPath { get; set; }
        public long MaxUploadFileSize { get; set; }
    }

    public class Csv
    {
        public List<string> CsCsvExclusion { get; set; }
        public string CsvFileName { get; set; }
    }
}
