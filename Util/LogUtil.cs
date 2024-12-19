//=============================================================================
// LogUtil
// log4netによるログ出力
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using log4net;
using log4net.Util;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using SKYCOM.DLManagement.AzureHelper;
using SKYCOM.DLManagement.Data;
using Microsoft.Extensions.Options;

namespace SKYCOM.DLManagement.Util
{
    public class LogUtil
    {
        #region
        private static readonly LogUtil _instance = new LogUtil();

        public static LogUtil Instance => _instance;
        private readonly IOptions<Settings> _settings;
        private const string LOG_CONFIG_FILE = "log4net.config";
        private const string LOG_MESSAGE_FORMART = "{0}.{1} - {2}";
        private static readonly CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ja-JP");

        private static readonly ILog _logger = LogManager.GetLogger(Assembly.GetCallingAssembly(), MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        #region コンストラクタ
        /// <summary>
        /// プライベートコンストラクタ
        /// </summary>
       
        public LogUtil(IOptions<Settings> settings)
        {
            _settings = settings;
        }
        private LogUtil()
        {
            #region existingcode
            //log4netConfig.Load(File.OpenRead(LOG_CONFIG_FILE));
            #endregion

            #region CMF-Changes
            MemoryStream memoryStream = AzBlobStorageHelper.DownloadBlobToMemoryStream(_settings.Value.BlobSettings.CommonContainerName, LOG_CONFIG_FILE);
            var log4netConfig = new XmlDocument();
            log4netConfig.Load(memoryStream);
            #endregion


            var repo = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        }
        #endregion
        #region Fatal
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Fatal(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Fatal(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

#pragma warning disable CA1822 // メンバーを static に設定します
        public void Fatal(string message, Exception exception)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.FatalExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }
        #endregion
        #region Error
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Error(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Error(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

#pragma warning disable CA1822 // メンバーを static に設定します
        public void Error(string message, Exception exception)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.ErrorExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }
        #endregion
        #region Warn
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Warn(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Warn(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

#pragma warning disable CA1822 // メンバーを static に設定します
        public void Warn(string message, Exception exception)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.WarnExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }
        #endregion
        #region Info
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Info(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Info(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

#pragma warning disable CA1822 // メンバーを static に設定します
        public void Info(string message, Exception exception)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.InfoExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }
        #endregion
        #region Debug
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Debug(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Debug(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

#pragma warning disable CA1822 // メンバーを static に設定します
        public void Debug(string message, Exception exception)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.DebugExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }
        #endregion
        #region Trace
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Trace(string message)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Trace(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), null);
        }
        #endregion
    }
}
