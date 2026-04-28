using System;
using System.IO;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace LogViewerApp.Services;

public class Log4NetConfigService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Log4NetConfigService));

    public string? CurrentConfigPath { get; private set; }
    public string? ValidationError { get; private set; }

    public bool LoadConfig(string xmlPath)
    {
        ValidationError = null;
        if (!File.Exists(xmlPath))
        {
            ValidationError = $"File not found: {xmlPath}";
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(xmlPath);
            ValidateLog4NetXml(doc);

            ILoggerRepository repo = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()!);
            XmlConfigurator.Configure(repo, new FileInfo(xmlPath));

            CurrentConfigPath = xmlPath;
            return true;
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            return false;
        }
    }

    public string GenerateDefaultConfig(string logOutputPath)
    {
        string path = logOutputPath.Replace("\\", "/");
        string pattern = "%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline";
        return $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <log4net>
              <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
                <file value="{path}" />
                <appendToFile value="true" />
                <rollingStyle value="Size" />
                <maxSizeRollBackups value="10" />
                <maximumFileSize value="10MB" />
                <staticLogFileName value="true" />
                <layout type="log4net.Layout.PatternLayout">
                  <conversionPattern value="{pattern}" />
                </layout>
              </appender>
              <root>
                <level value="DEBUG" />
                <appender-ref ref="RollingFileAppender" />
              </root>
            </log4net>
            """;
    }

    private static void ValidateLog4NetXml(XmlDocument doc)
    {
        var root = doc.DocumentElement;
        if (root == null || root.Name != "log4net")
            throw new InvalidOperationException("XML root element must be <log4net>.");
    }
}
