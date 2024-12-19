//=============================================================================
// Program
// プログラム起動時に１回だけ実行する（初期設定）用ファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SKYCOM.DLManagement.Util;

namespace SKYCOM.DLManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LogUtil.Instance.Info("start");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
