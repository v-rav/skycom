//=============================================================================
// Product
// t_productテーブルへのアクセスクラス
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKYCOM.DLManagement.Entity
{
    [Table("t_product")]
    public class Product
    {
        [Key]
        [Column("product_id", TypeName = "bigint")]
        [Required]
        public long ProductId { get; set; }
        [Column("product_file_path")]
        [Required]
        public string ProductFilePath { get; set; }
        [Column("product_name")]
        [Required]
        public string ProductName { get; set; }
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
