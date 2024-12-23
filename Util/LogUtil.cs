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
        private static readonly Lazy<LogUtil> _lazyInstance = new Lazy<LogUtil>(() => new LogUtil());
        public static LogUtil Instance => _lazyInstance.Value;

        private readonly IOptions<Settings> _settings;

        private const string LOG_CONFIG_FILE = "log4net.config";
        private const string LOG_MESSAGE_FORMART = "{0}.{1} - {2}";
        private static readonly CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ja-JP");
        private static readonly ILog _logger = LogManager.GetLogger(Assembly.GetCallingAssembly(), MethodBase.GetCurrentMethod().DeclaringType);

        private LogUtil()
        {
            // Constructor logic can be delayed here
        }

        public LogUtil(IOptions<Settings> settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            try
            {
                MemoryStream memoryStream = BlobHelperProvider.BlobHelper.DownloadBlobToMemoryStream(_settings.Value.BlobSettings.CommonContainerName, LOG_CONFIG_FILE);
                var log4netConfig = new XmlDocument();
                log4netConfig.Load(memoryStream);
                var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
                log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
            }
            catch (Exception ex)
            {
                _logger.Error("Error initializing logger.", ex);
                throw; // Optionally rethrow to halt execution or handle differently
            }
        }

        #region Fatal

        public void Fatal(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Fatal(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

        public void Fatal(string message, Exception exception)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.FatalExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }

        #endregion

        #region Error

        public void Error(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Error(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

        public void Error(string message, Exception exception)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.ErrorExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }

        #endregion

        #region Warn

        public void Warn(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Warn(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

        public void Warn(string message, Exception exception)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.WarnExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }

        #endregion

        #region Info

        public void Info(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Info(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

        public void Info(string message, Exception exception)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.InfoExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }

        #endregion

        #region Debug

        public void Debug(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Debug(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message));
        }

        public void Debug(string message, Exception exception)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.DebugExt(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), exception);
        }

        #endregion

        #region Trace

        public void Trace(string message)
        {
            var caller = new System.Diagnostics.StackFrame(1);
            _logger.Trace(string.Format(cultureInfo, LOG_MESSAGE_FORMART, caller.GetMethod().ReflectedType.Name, caller.GetMethod().Name, message), null);
        }

        #endregion
    }
}
