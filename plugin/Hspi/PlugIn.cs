using System;
using System.Diagnostics.CodeAnalysis;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.Device;
using Hspi.Utils;
using Serilog;

#nullable enable

namespace Hspi
{
    internal partial class PlugIn : HspiBase
    {
        public PlugIn()
            : base(PlugInData.PlugInId, PlugInData.PlugInName)
        {
        }

        private SettingsPages SettingsPages
        {
            get
            {
                CheckNotNull(settingsPages);
                return settingsPages;
            }
        }

        protected override void BeforeReturnStatus()
        {
            this.Status = PluginStatus.Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                deviceManager?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            try
            {
                Log.Information("Plugin Starting");
                Settings.Add(SettingsPages.CreateDefault());
                LoadSettingsFromIni();
                settingsPages = new SettingsPages(Settings);
                UpdateDebugLevel();

                deviceManager = new DeviceManager(HomeSeerSystem, settingsPages, ShutdownCancellationToken);

                Log.Information("Plugin Started");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to initialize PlugIn with {error}", ex.GetFullMessage());
                throw;
            }
        }

        protected override bool OnSettingChange(string pageId, AbstractView currentView, AbstractView changedView)
        {
            Log.Information("Page:{pageId} has changed value of id:{id} to {value}", pageId, changedView.Id, changedView.GetStringValue());

            if (SettingsPages.OnSettingChange(changedView))
            {
                UpdateDebugLevel();
                deviceManager?.Dispose();
                deviceManager = new DeviceManager(HomeSeerSystem, SettingsPages, ShutdownCancellationToken);

                return true;
            }

            return base.OnSettingChange(pageId, currentView, changedView);
        }

        protected override void OnShutdown()
        {
            Log.Information("Shutting down");
            base.OnShutdown();
        }

        private static void CheckNotNull([NotNull] object? obj)
        {
            if (obj is null)
            {
                throw new InvalidOperationException("Plugin Not Initialized");
            }
        }

        private void UpdateDebugLevel()
        {
            bool debugLevel = SettingsPages.DebugLoggingEnabled;
            bool logToFile = SettingsPages.LogtoFileEnabled;
            this.LogDebug = debugLevel;
            Logger.ConfigureLogging(SettingsPages.LogLevel, logToFile, HomeSeerSystem);
        }

        private SettingsPages? settingsPages;
        private DeviceManager? deviceManager;
    }
}