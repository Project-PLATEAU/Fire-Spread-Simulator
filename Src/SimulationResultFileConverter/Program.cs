using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using SimulationCommonLibrary.Model;
using SimulationResultFileConverter.Controller;

namespace SimulationResultFileConverter
{
    /// <summary>
    /// メインクラス
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private static log4net.ILog? logger = null;

        /// <summary>
        /// ロガー
        /// </summary>
        internal static log4net.ILog Logger
        {
            get
            {
                if (logger == null)
                {
                    log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
                    logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName ?? string.Empty);
                }

                return logger;
            }
        }

        /// <summary>
        /// メイン関数
        /// </summary>
        /// <param name="args">実行引数</param>
        /// <returns>
        /// 終了コード<br/>
        /// 0：成功<br/>
        /// 1：実行引数が不正<br/>
        /// 2：GISデータ変換ツール設定ファイルが無い、または不正<br/>
        /// 3：処理に失敗<br/>
        /// </returns>
        public static int Main(string[] args)
        {
            string settingFilePath;
            if (args.Length == 0)
            {
                Logger.Info("引数無しで起動");

                // 所定の場所のsettingファイルを読み込む
                var currentDir = Directory.GetCurrentDirectory();
                settingFilePath = Path.Combine(currentDir, "ResultFileConv.setting");
            }
            else if (args.Length == 1)
            {
                Logger.Info($"引数有りで起動 arg = {args[0]}");

                // 引数の場所のsettingファイルを読み込む
                settingFilePath = args[0];
            }
            else
            {
                Logger.Error($"引数有りで起動 引数の数が不正 args = {string.Join(' ', args)}");
                return 1;
            }

            var setting = CheckSettingFile(settingFilePath);
            if (setting == null)
            {
                return 2;
            }

            var conv = new ResultFileConverter();
            if (!conv.Execute(
                setting.InputSimulationSourceFolderPath,
                setting.InputSimulationResultFolderPath,
                setting.OutputGisDataFolderPath,
                setting.IsOutputBuilding,
                setting.IsOutputFirePath,
                setting.IsEllipsoidHeight))
            {
                return 3;
            }

            return 0;
        }

        /// <summary>
        /// GISデータ変換ツール設定ファイルのチェックを行います。
        /// </summary>
        /// <param name="settingFilePath">GISデータ変換ツール設定ファイルパス</param>
        /// <returns>GISデータ変換ツール設定</returns>
        private static ResultFileConvSetting? CheckSettingFile(string settingFilePath)
        {
            if (!File.Exists(settingFilePath))
            {
                Logger.Error($"settingファイルがない settingFilePath = {settingFilePath}");
                return null;
            }

            ResultFileConvSetting? setting = null;

            try
            {
                using (var sr = new StreamReader(settingFilePath, new UTF8Encoding(false)))
                {
                    setting = JsonConvert.DeserializeObject<ResultFileConvSetting>(sr.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"settingファイルの読み込みに失敗 settingFilePath = {settingFilePath}", ex);
                return null;
            }

            if (setting == null)
            {
                Logger.Error($"settingファイルの読み込みに失敗 settingFilePath = {settingFilePath}");
                return null;
            }

            var content = string.Empty;
            content += $"inputSimulationSourceFolderPath = {setting.InputSimulationSourceFolderPath}\r\n";
            content += $"inputSimulationResultFolderPath = {setting.InputSimulationResultFolderPath}\r\n";
            content += $"outputGisDataFolderPath = {setting.OutputGisDataFolderPath}\r\n";
            content += $"isOutputBuilding = {setting.IsOutputBuilding}\r\n";
            content += $"isOutputFirePath = {setting.IsOutputFirePath}\r\n";
            content += $"isEllipsoidHeight = {setting.IsEllipsoidHeight}\r\n";

            Logger.Info($"settingファイルの内容\r\n" +
                $"----\r\n" +
                $"settingFilePath = {settingFilePath}\r\n" +
                $"----\r\n" +
                $"{content}");

            var errors = new List<string>();
            if (string.IsNullOrEmpty(setting.InputSimulationSourceFolderPath))
            {
                errors.Add("シミュレーションデータフォルダが設定されていない");
            }

            if (string.IsNullOrEmpty(setting.InputSimulationResultFolderPath))
            {
                errors.Add("シミュレーション結果フォルダが設定されていない");
            }

            if (string.IsNullOrEmpty(setting.OutputGisDataFolderPath))
            {
                errors.Add("GISデータ出力フォルダが設定されていない");
            }

            if (setting.InputSimulationSourceFolderPath.Equals(setting.InputSimulationResultFolderPath, StringComparison.CurrentCultureIgnoreCase))
            {
                errors.Add("フォルダが同一（シミュレーションデータフォルダ = シミュレーション結果フォルダ）");
            }

            if (setting.InputSimulationSourceFolderPath.Equals(setting.OutputGisDataFolderPath, StringComparison.CurrentCultureIgnoreCase))
            {
                errors.Add("フォルダが同一（シミュレーションデータフォルダ = GISデータ出力フォルダ）");
            }

            if (setting.InputSimulationResultFolderPath.Equals(setting.OutputGisDataFolderPath, StringComparison.CurrentCultureIgnoreCase))
            {
                errors.Add("フォルダが同一（シミュレーション結果フォルダ = GISデータ出力フォルダ）");
            }

            if (0 < errors.Count)
            {
                Logger.Error($"settingファイルの設定内容不備 errors = \r\n・{string.Join("\r\n・", errors)}");
                return null;
            }

            return setting;
        }
    }
}
