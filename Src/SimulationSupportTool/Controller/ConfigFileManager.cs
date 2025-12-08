using System.Configuration;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// アプリケーション設定ファイルマネージャー
    /// </summary>
    internal class ConfigFileManager
    {
        /// <summary>
        /// 設定キー：シミュレーションデータフォルダパス
        /// </summary>
        internal static string InputFolderPath => "InputFolderPath";

        /// <summary>
        /// 設定キー：GISデータ出力フォルダパス
        /// </summary>
        internal static string OutputFolderPath => "OutputFolderPath";

        /// <summary>
        /// 値を取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>値</returns>
        internal static string GetValue(string key)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var setting = config.AppSettings.Settings[key];

            if (setting != null)
            {
                return setting.Value;
            }

            config.AppSettings.Settings.Add(key, string.Empty);
            config.Save();

            return string.Empty;
        }

        /// <summary>
        /// 値を設定します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        internal static void SetValue(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var setting = config.AppSettings.Settings[key];

            if (setting != null)
            {
                config.AppSettings.Settings[key].Value = value;
                config.Save();
                return;
            }

            config.AppSettings.Settings.Add(key, value);
            config.Save();
        }
    }
}
