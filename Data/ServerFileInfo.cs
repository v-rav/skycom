//=============================================================================
// ServerFileInfo
// ServerFileListService.csで使用するプロパティ定義
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


namespace SKYCOM.DLManagement.Data
{
    public class ServerFileInfo
    {
        public DateTime Date { get; set; }

        public long FileSize { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        //ファイル情報隠しファイル等
        public FileAttributes Attributes { get; set; }

        //ファイル　or　フォルダ　のどちらかであるかの情報
        // true:ファイル、false：フォルダ
        public bool IsFile { get; set; }
    }
}
