#nullable enable

using System.IO;

namespace Hspi
{
    /// <summary>
    /// Class to store static data for plugin
    /// </summary>
    internal static class PlugInData
    {
        /// <summary>
        /// The plugin Id
        /// </summary>
        public const string PlugInId = @"MySolar";

        /// <summary>
        /// The plugin name
        /// </summary>
        public const string PlugInName = @"My Solar";

        /// <summary>
        /// Config file name
        /// </summary>
        public const string SettingFileName = @"MySolar.ini";

        public static readonly string HomeSeerDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    }
}