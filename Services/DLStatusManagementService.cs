//=============================================================================
// DLStatusManagementService
// CSVファイルダウンロード用実行ファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Microsoft.EntityFrameworkCore;
using SKYCOM.DLManagement.Entity;
using SKYCOM.DLManagement.Util;
using System.Threading.Tasks;

namespace SKYCOM.DLManagement.Services
{
    public class DLStatusManagementService
    {
        private readonly DbAccess _context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context"></param>
        public DLStatusManagementService(DbAccess context) => _context = context;

        /// <summary>
        /// CSVを作成できるか確認する
        /// </summary>
        /// <returns>データが1件以上あればtrue、0件がfalse</returns>
        public async Task<bool> CanCreateCsvAsync()
        {
            try
            {
                LogUtil.Instance.Debug("start");
                return (await _context.DLStatusManagements.ToListAsync().ConfigureAwait(false)).Count != 0;
            }
            finally
            {
                LogUtil.Instance.Debug("end");
            }
        }
    }
}
