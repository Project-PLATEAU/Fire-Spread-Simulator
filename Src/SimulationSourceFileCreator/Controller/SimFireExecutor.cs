using System.Diagnostics;
using System.IO;
using System.Text;
using SimulationCommonLibrary.Utility;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// シミュレーションエンジン実行クラス
    /// </summary>
    internal class SimFireExecutor
    {
        /// <summary>
        /// 実行プロセス
        /// </summary>
        private Process? process;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// private にして外部からのインスタンス化を禁止します。
        /// </remarks>
        /// <param name="workingFolderPath">作業フォルダパス</param>
        /// <param name="exeFilePath">実行exeファイルパス</param>
        private SimFireExecutor(string workingFolderPath, string exeFilePath)
        {
            this.WorkingFolderPath = workingFolderPath;
            this.ExeFilePath = exeFilePath;
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
        /// インスタンスを作成します。
        /// </summary>
        /// <returns>インスタンス</returns>
        internal static SimFireExecutor? CreateInstance()
        {
            var currentDir = Directory.GetCurrentDirectory();

            var workingFolderPath = Path.Combine(currentDir, "SimFire");
            if (!Directory.Exists(workingFolderPath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（シミュレーションエンジンフォルダ「SimFire」がない）");
                return null;
            }

            var exeFilePath = Path.Combine(workingFolderPath, "simFireMP64.exe");
            if (!File.Exists(exeFilePath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（シミュレーションエンジン実行exeファイル「SimFire/simFireMP64.exe」がない）");
                return null;
            }

            return new SimFireExecutor(workingFolderPath, exeFilePath);
        }

        /// <summary>
        /// 処理を中止します。
        /// </summary>
        internal void Cancel()
        {
            // 強制終了
            this.process?.Kill();
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="meshNumer">メッシュ番号</param>
        /// <param name="outputFolderPath">出力フォルダパス</param>
        /// <returns>成否</returns>
        internal bool Execute(string meshNumer, string outputFolderPath)
        {
            // iniファイルの複製（出力の設定を変更）
            var orgFilePath = Path.Combine(this.WorkingFolderPath, "simfire_org.ini");
            var newFilePath = Path.Combine(this.WorkingFolderPath, "simfire.ini");

            if (!FileUtility.CopyAndRewrite(
                orgFilePath,
                newFilePath,
                (line) =>
                {
                    // 「OutPath」の値を書き換え
                    if (line.StartsWith("OutPath"))
                    {
                        return $"OutPath = {outputFolderPath}";
                    }

                    // 「outBuildFile」の値を書き換え
                    if (line.StartsWith("outBuildFile"))
                    {
                        return $"outBuildFile = builds_{meshNumer}.dat";
                    }

                    // 「outRoomFile」の値を書き換え
                    if (line.StartsWith("outRoomFile"))
                    {
                        return $"outRoomFile = rooms_{meshNumer}.dat";
                    }

                    return line;
                },
                Encoding.GetEncoding("Shift_JIS")))
            {
                App.Logger.Error($"iniファイルの複製に失敗");
                return false;
            }

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
                App.Logger.Error($"シミュレーションエンジン実行exe「SimFire/simFireMP64.exe」の起動に失敗");
                return false;
            }

            // 終了するまで待つ（終了コードは設定されていない為確認しない）
            this.process.WaitForExit();
            return true;
        }
    }
}
