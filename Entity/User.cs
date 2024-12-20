//=============================================================================
// User
// t_userテーブルへのアクセスクラス
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
    /// ユーザー管理
    /// </summary>
    [Table("t_user")]
    public class User
    {
        [Key]
        [Column("user_id", TypeName = "bigint")]
        [Required]
        public long UserId { get; set; }
        [Column("mail_address")]
        [Required]
        public string MailAddress { get; set; }
        [Column("negotiation_name")]
        [Required]
        public string NegotiationName { get; set; }
        [Column("customer_name")]
        public string CustomerName { get; set; }
        [Column("support_contact")]
        public string SupportContact { get; set; }
        [Column("update_contact")]
        public string UpdateContact { get; set; }
        [Column("insert_date_time", TypeName = "datetime")]
        [Required]
        [Timestamp]
        public DateTime InsertDateTime { get; set; }
        [Column("update_date_time", TypeName = "datetime")]
        [Required]
        [Timestamp]
        public DateTime UpdateDateTime { get; set; }
    }
}
