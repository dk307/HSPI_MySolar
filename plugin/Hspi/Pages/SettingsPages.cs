using System;
using System.Linq;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.Utils;
using Serilog.Events;

#nullable enable

namespace Hspi
{
    internal sealed class SettingsPages : IEnvoySettings
    {
        public SettingsPages(SettingsCollection collection)
        {
            var logLevelStr = collection[SettingPageId].GetViewById<SelectListView>(LoggingLevelId).GetSelectedOptionKey();
            if (!Enum.TryParse<LogEventLevel>(logLevelStr, out LogEventLevel logEventLevel))
            {
                LogLevel = LogEventLevel.Information;
            }
            else
            {
                LogLevel = logEventLevel;
            }

            LogtoFileEnabled = collection[SettingPageId].GetViewById<ToggleView>(LogToFileId).IsEnabled;
            EnvoyHost = collection[SettingPageId].GetViewById<InputView>(EnvoyHostId).Value;
            UserName = collection[SettingPageId].GetViewById<InputView>(UserNameId).Value;
            Password = collection[SettingPageId].GetViewById<InputView>(PasswordId).Value;
            RefreshInterval = collection[SettingPageId].GetViewById<TimeSpanView>(RefreshIntervalId).Value;
        }

        public bool DebugLoggingEnabled => LogLevel <= LogEventLevel.Debug;

        public string? EnvoyHost { get; private set; }
        public LogEventLevel LogLevel { get; private set; }

        public bool LogtoFileEnabled { get; private set; }

        public string? Password { get; private set; }
        public TimeSpan RefreshInterval { get; private set; }
        public string? UserName { get; private set; }

        public static Page CreateDefault(LogEventLevel logEventLevel = LogEventLevel.Information,
                                         bool logToFileDefault = false)
        {
            var settings = PageFactory.CreateSettingsPage(SettingPageId, "Settings");

            settings = settings.WithInput(EnvoyHostId, "Envoy Host", HomeSeer.Jui.Types.EInputType.Text)
                               .WithInput(UserNameId, "UserName", HomeSeer.Jui.Types.EInputType.Text)
                               .WithInput(PasswordId, "Password", HomeSeer.Jui.Types.EInputType.Password);

            var refreshIntervalView = new TimeSpanView(RefreshIntervalId, "Refresh Interval", TimeSpan.FromSeconds(5))
            {
                ShowSeconds = true,
                ShowDays = false
            };

            settings = settings.WithView(refreshIntervalView);

            var logOptions = EnumHelper.GetValues<LogEventLevel>().Select(x => x.ToString()).ToList();
            settings = settings.WithDropDownSelectList(LoggingLevelId, "Logging Level", logOptions, logOptions, (int)logEventLevel);

            settings = settings.WithToggle(LogToFileId, "Log to file", logToFileDefault);
            return settings.Page;
        }

        public bool OnSettingChange(AbstractView changedView)
        {
            if (changedView.Id == LoggingLevelId)
            {
                var value = ((SelectListView)changedView).GetSelectedOptionKey();
                if (Enum.TryParse<LogEventLevel>(value, out LogEventLevel logEventLevel))
                {
                    LogLevel = logEventLevel;
                    return true;
                }

                return false;
            }

            if (changedView.Id == LogToFileId)
            {
                LogtoFileEnabled = ((ToggleView)changedView).IsEnabled;
                return true;
            }

            if (changedView.Id == EnvoyHostId)
            {
                EnvoyHost = ((InputView)changedView).Value;
                return true;
            }

            if (changedView.Id == UserNameId)
            {
                UserName = ((InputView)changedView).Value;
                return true;
            }

            if (changedView.Id == PasswordId)
            {
                Password = ((InputView)changedView).Value;
                return true;
            }

            if (changedView.Id == RefreshIntervalId)
            {
                RefreshInterval = ((TimeSpanView)changedView).Value;
                return true;
            }

            return false;
        }

        internal const string EnvoyHostId = "EnvoyHost";
        internal const string LoggingLevelId = "LogLevel";
        internal const string LogToFileId = "LogToFile";
        internal const string PasswordId = "Password";
        internal const string RefreshIntervalId = "RefreshInterval";
        internal const string SettingPageId = "SettingPage";
        internal const string UserNameId = "Username";
    }
}