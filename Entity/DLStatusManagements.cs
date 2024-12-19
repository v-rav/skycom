//=============================================================================
// DLStatusManagements
// t_dl_status_managementsテーブルへのアクセスクラス
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKYCOM.DLManagement.Entity
{

    /// <summary>
    /// DL状態
    /// </summary>
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

    /// <summary>
    /// DL状態管理
    /// </summary>
    [Table("t_dl_status_managements")]
    public class DLStatusManagements
    {
        [Key]
        [Column("dl_status_management_id")]
        [Required]
        public string DLStatusManagementId { get; set; }

        [Column("send_group_id", TypeName = "bigint")]
        [Required]
        public long SendGroupId { get; set; }

        [Column("user_id", TypeName = "bigint")]
        [Required]
        public long UserId { get; set; }

        [Column("product_id", TypeName = "bigint")]
        [Required]
        public long ProductId { get; set; }

        [Column("mail_send_date_time", TypeName = "datetime")]
        public DateTime? MailSendDateTime { get; set; }

        [Column("reserved_date_time", TypeName = "datetime")]
        public DateTime? ReservedDateTime { get; set; }

        [Column("mail_send_executed_date_time", TypeName = "datetime")]
        public DateTime? MailSendExecutedDateTime { get; set; }

        [Column("from_mail_address")]
        public string FormMailAddress { get; set; }

        [Column("url")]
        public string Urlstr { get; set; }

        [Column("ip_address")]
        public string IpAddress { get; set; }

        [Column("dl_status", TypeName = "int")]
        public int DLStatus { get; set; }

        [Column("dl_count", TypeName = "int")]
        public int DLCount { get; set; }

        [Column("dl_date_time", TypeName = "datetime")]
        public DateTime? DLDateTime { get; set; }

        [Column("insert_date_time", TypeName = "datetime")]
        [Required]
        [Timestamp]
        public DateTime InsertDateTime { get; set; }

        [Column("update_date_time", TypeName = "datetime")]
        [Required]
        [Timestamp]
        public DateTime UpdateDateTime { get; set; }

        public virtual User TUser { get; set; }
        public virtual Product TProduct { get; set; }
    }
}
