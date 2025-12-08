using System.IO;

namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// 定数クラス
    /// </summary>
    internal class ConstSystemPath
    {
        /// <summary>
        /// カレントディレクトリパス
        /// </summary>
        internal static string CurrentDirectory => Directory.GetCurrentDirectory();

        /// <summary>
        /// 要素追加設定ファイルパス
        /// </summary>
        internal static string SettingFilePath => Path.Combine(CurrentDirectory, "config", "ElementAddSettting.xml");

        /// <summary>
        /// 作業フォルダパス（CSV）
        /// </summary>
        internal static string WorkspaceCSVFolderPath => Path.Combine(CurrentDirectory, "workspace", "csv");

        /// <summary>
        /// 作業フォルダパス（GML）
        /// </summary>
        internal static string WorkspaceGMLFolderPath => Path.Combine(CurrentDirectory, "workspace", "gml");
    }
}
