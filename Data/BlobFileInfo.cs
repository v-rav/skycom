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
    public class BlobFileInfo
    {
        public string ContainerName { get; set; }

        public DateTime Date { get; set; }

        public long FileSize { get; set; }

        public string FileName { get; set; }

        public bool IsFile { get; set; }
    }
}
