using System.Diagnostics;
using System.IO;
using System.Text;
using SimulationCommonLibrary.Utility;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// データ変換ツール実行クラス
    /// </summary>
    internal class GeneFileExecutor
    {
        /// <summary>
        /// 実行プロセス
        /// </summary>
        private Process? process;

        /// <summary>
        /// 処理を中止したかどうか
        /// </summary>
        private bool isCalceled = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// private にして外部からのインスタンス化を禁止します。
        /// </remarks>
        /// <param name="workingFolderPath">作業フォルダパス</param>
        /// <param name="exeFilePath">実行exeファイルパス</param>
        /// <param name="inFolderPath">入力フォルダパス</param>
        /// <param name="outFolderPath">出力フォルダパス</param>
        private GeneFileExecutor(string workingFolderPath, string exeFilePath, string inFolderPath, string outFolderPath)
        {
            this.WorkingFolderPath = workingFolderPath;
            this.ExeFilePath = exeFilePath;
            this.InFolderPath = inFolderPath;
            this.OutFolderPath = outFolderPath;
        }

        /// <summary>
        /// 作業フォルダパス
        /// </summary>
        private string WorkingFolderPath { get; set; }

        /// <summary>
        /// 実行exeファイルパス
        /// </summary>
        private string ExeFilePath { get; set; }

        /// <summary>
        /// 入力フォルダパス
        /// </summary>
        private string InFolderPath { get; set; }

        /// <summary>
        /// 出力フォルダパス
        /// </summary>
        private string OutFolderPath { get; set; }

        /// <summary>
        /// インスタンスを作成します。
        /// </summary>
        /// <returns>インスタンス</returns>
        internal static GeneFileExecutor? CreateInstance()
        {
            var currentDir = Directory.GetCurrentDirectory();

            var workingFolderPath = Path.Combine(currentDir, "GeneFile");
            if (!Directory.Exists(workingFolderPath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（データ変換ツール実行フォルダ「GeneFile」がない）");
                return null;
            }

            var exeFilePath = Path.Combine(workingFolderPath, "plateau_conv.exe");
            if (!File.Exists(exeFilePath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（データ変換ツール実行exeファイル「GeneFile/plateau_conv.exe」がない）");
                return null;
            }

            // 出力ファイルフォルダ作成（既にあれば何もしない）
            // ※csvを配置する必要があるためフォルダはあるはず
            var outFolderPath = Path.Combine(workingFolderPath, "gene_out");
            Directory.CreateDirectory(outFolderPath);

            // 入力ファイルフォルダ作成（既にあれば何もしない）
            var inFolderPath = Path.Combine(workingFolderPath, "gene_in");
            Directory.CreateDirectory(inFolderPath);

            return new GeneFileExecutor(workingFolderPath, exeFilePath, inFolderPath, outFolderPath);
        }

        /// <summary>
        /// 処理を中止します。
        /// </summary>
        internal void Cancel()
        {
            // 強制終了
            this.process?.Kill();
            this.isCalceled = true;
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="gmlFilePath">入力ファイルパス（変換対象ファイルパス）</param>
        /// <param name="meshNumer">メッシュ番号</param>
        /// <param name="seriesNumber">平面直角座標系</param>
        /// <returns>成否</returns>
        internal bool Execute(string gmlFilePath, string meshNumer, int seriesNumber)
        {
            // cfgファイルの複製（座標系を変更）
            var orgFilePath = Path.Combine(this.WorkingFolderPath, "cfg", "plateau_conv_org.cfg");
            var newFilePath = Path.Combine(this.WorkingFolderPath, "cfg", "plateau_conv.cfg");

            var epsgCode = GisUtility.ConvertSeriesNumberToEpsgCode(seriesNumber);

            if (!FileUtility.CopyAndRewrite(
                orgFilePath,
                newFilePath,
                (line) =>
                {
                    // 「oepsg」の値を書き換え
                    if (line.StartsWith("  \"oepsg\""))
                    {
                        return $"  \"oepsg\": {epsgCode},";
                    }

                    return line;
                },
                new UTF8Encoding(false)))
            {
                App.Logger.Error($"cfgファイルの複製に失敗");
                return false;
            }

            // txtファイルが残っていると次のシミュレーションエンジンの変換の対象になってしまうためクリーンアップを行う
            DirectoryUtility.CleanupDirectory(this.OutFolderPath, "*.txt");

            // gmlファイルが残っていると変換の対象になってしまうためクリーンアップを行う
            DirectoryUtility.CleanupDirectory(this.InFolderPath, "*.gml");

            // 残り続けディスク容量を圧迫するおそれがあるためクリーンアップを行う
            var cachedFolderPath = Path.Combine(this.WorkingFolderPath, "cached");
            if (Directory.Exists(cachedFolderPath))
            {
                DirectoryUtility.CleanupDirectory(cachedFolderPath, "*.pkl");
            }

            // 入力ファイルの複製（ファイル名は「数値8桁_」で始まる必要がある）
            var targetFilePath = Path.Combine(this.InFolderPath, $"{meshNumer}_bldg.gml");
            File.Copy(gmlFilePath, targetFilePath, true);

            this.isCalceled = false;

            // exeの実行
            var pInfo = new ProcessStartInfo
            {
                FileName = this.ExeFilePath,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = this.WorkingFolderPath,
            };

            this.process = Process.Start(pInfo);

            if (this.process == null)
            {
                App.Logger.Error($"データ変換ツール実行exe「GeneFile/plateau_conv.exe」の起動に失敗");
                return false;
            }

            // 終了するまで待つ
            this.process.WaitForExit();

            // 終了コードを確認
            if (this.process.ExitCode != 0)
            {
                if (!this.isCalceled)
                {
                    App.Logger.Error($"データ変換ツール「GeneFile/plateau_conv.exe」の実行に失敗");
                }

                return false;
            }

            return true;
        }
    }
}
