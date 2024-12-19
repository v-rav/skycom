//=============================================================================
// DLStateManagementCsv
// DL状態管理csvファイルデータへのアクセスクラス
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using CsvHelper.Configuration;
using System;

namespace SKYCOM.DLManagement.Data
{
    public class DLStateManagementCsv
    {
        // DL状態管理ID
        public string DLStateManagementId { get; set; }
        // リリース案内送信日時
        public string MailSendDateTime { get; set; }
        // 送信予約日時
        public string ReservedDateTime { get; set; }
        // 実送信日時
        public string MailSendExecutedDateTime { get; set; }
        // 送付元
        public string FormMailAddress { get; set; }
        // 送信先
        public string ToMailAddress { get; set; }
        // DL製品名
        public string DLSystemName { get; set; }
        // DLファイルフルパス
        public string DLFilePath { get; set; }
		// 商談名
        public string NegotiationName { get; set; }
        // 取引先名
        public string CompanyName { get; set; }
		// サポート窓口
        public string SupportContact { get; set; }
		// 更新窓口企業名
        public string UpdateContact { get; set; }
        // DL実施IPアドレス
        public string DLIpAddress { get; set; }
        // DL URL
        public string DLUrlstr { get; set; }
        // ライセンス番号
        public string LicenseNumber { get; set; }
        // 保守管理番号
        public string MaintenanceNumber { get; set; }
        // DL状況
        public int DLStatus { get; set; }
        // DL回数
        public int DLCount { get; set; }
        // DL日時
        public string DLDateTime { get; set; }
        // レコード作成日時
        public string InsertDateTime { get; set; }
        // レコード更新日時
        public string UpdateDateTime { get; set; }

    }

    public class DLStateManagementMapper : ClassMap<DLStateManagementCsv>
    {
        private const string HEADRE_NAME_INDEX0 = "DL状態管理ID";
        private const string HEADRE_NAME_INDEX1 = "リリース案内送信日時";
        private const string HEADRE_NAME_INDEX2 = "送信予約日時";
        private const string HEADRE_NAME_INDEX3 = "実送信日時";
        private const string HEADRE_NAME_INDEX4 = "送付元";
        private const string HEADRE_NAME_INDEX5 = "送信先";
        private const string HEADRE_NAME_INDEX6 = "DL製品名";
        private const string HEADRE_NAME_INDEX7 = "DLファイルフルパス";
        private const string HEADRE_NAME_INDEX8 = "商談名";
        private const string HEADRE_NAME_INDEX9 = "取引先名";
        private const string HEADRE_NAME_INDEX10 = "サポート窓口";
        private const string HEADRE_NAME_INDEX11 = "更新窓口企業名";
        private const string HEADRE_NAME_INDEX12 = "DL実施IPアドレス";
        private const string HEADRE_NAME_INDEX13 = "DL URL";
        private const string HEADRE_NAME_INDEX14 = "ライセンス番号";
        private const string HEADRE_NAME_INDEX15 = "保守管理番号";
        private const string HEADRE_NAME_INDEX16 = "DL状況";
        private const string HEADRE_NAME_INDEX17 = "DL回数";
        private const string HEADRE_NAME_INDEX18 = "DL日時";
        private const string HEADRE_NAME_INDEX19 = "レコード作成日時";
        private const string HEADRE_NAME_INDEX20 = "レコード更新日時";


        public DLStateManagementMapper()
        {
            Map(x => x.DLStateManagementId).Index(0).Name(HEADRE_NAME_INDEX0);
            Map(x => x.MailSendDateTime).Index(1).Name(HEADRE_NAME_INDEX1);
            Map(x => x.ReservedDateTime).Index(2).Name(HEADRE_NAME_INDEX2);
            Map(x => x.MailSendExecutedDateTime).Index(3).Name(HEADRE_NAME_INDEX3);
            Map(x => x.FormMailAddress).Index(4).Name(HEADRE_NAME_INDEX4);
            Map(x => x.ToMailAddress).Index(5).Name(HEADRE_NAME_INDEX5);
            Map(x => x.DLSystemName).Index(6).Name(HEADRE_NAME_INDEX6);
            Map(x => x.DLFilePath).Index(7).Name(HEADRE_NAME_INDEX7);
            Map(x => x.NegotiationName).Index(8).Name(HEADRE_NAME_INDEX8);
            Map(x => x.CompanyName).Index(9).Name(HEADRE_NAME_INDEX9);
            Map(x => x.SupportContact).Index(10).Name(HEADRE_NAME_INDEX10);
            Map(x => x.UpdateContact).Index(11).Name(HEADRE_NAME_INDEX11);
            Map(x => x.DLIpAddress).Index(12).Name(HEADRE_NAME_INDEX12);
            Map(x => x.DLUrlstr).Index(13).Name(HEADRE_NAME_INDEX13);
            Map(x => x.LicenseNumber).Index(14).Name(HEADRE_NAME_INDEX14);
            Map(x => x.MaintenanceNumber).Index(15).Name(HEADRE_NAME_INDEX15);
            Map(x => x.DLStatus).Index(16).Name(HEADRE_NAME_INDEX16);
            Map(x => x.DLCount).Index(17).Name(HEADRE_NAME_INDEX17);
            Map(x => x.DLDateTime).Index(18).Name(HEADRE_NAME_INDEX18);
            Map(x => x.InsertDateTime).Index(19).Name(HEADRE_NAME_INDEX19);
            Map(x => x.UpdateDateTime).Index(20).Name(HEADRE_NAME_INDEX20);
        }
    }
}
