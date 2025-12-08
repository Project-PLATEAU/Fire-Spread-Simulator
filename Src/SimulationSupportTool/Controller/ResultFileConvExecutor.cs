using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SimulationCommonLibrary.Model;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// GISデータ変換ツール実行クラス
    /// </summary>
    internal class ResultFileConvExecutor
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
        private ResultFileConvExecutor(string workingFolderPath, string exeFilePath)
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
        internal static ResultFileConvExecutor? CreateInstance()
        {
            var currentDir = Directory.GetCurrentDirectory();

            var workingFolderPath = Path.Combine(currentDir, "ResultFileConv");
            if (!Directory.Exists(workingFolderPath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（GISデータ変換ツールフォルダ「ResultFileConv」がない）");
                return null;
            }

            var exeFilePath = Path.Combine(workingFolderPath, "SimulationResultFileConverter.exe");
            if (!File.Exists(exeFilePath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（GISデータ変換ツール実行exeファイル「ResultFileConv/SimulationResultFileConverter.exe」がない）");
                return null;
            }

            return new ResultFileConvExecutor(workingFolderPath, exeFilePath);
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
        /// <param name="inputSimulationSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="simOutputFolderPath">シミュレーション結果フォルダパス</param>
        /// <param name="outputGisDataFolderPath">GISデータ出力フォルダパス</param>
        /// <param name="isOutputBuilding">建物延焼情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isOutputFirePath">延焼経路情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isEllipsoidHeight">CZMLファイルの高さを楕円体高にするかどうか（true = 楕円体高、false = 標高）</param>
        /// <param name="isOutputKml">KMLファイルを出力するかどうか</param>
        /// <returns>成否</returns>
        internal bool Execute(string inputSimulationSourceFolderPath, string simOutputFolderPath, string outputGisDataFolderPath, bool isOutputBuilding, bool isOutputFirePath, bool isEllipsoidHeight, bool isOutputKml)
        {
            // CZMLファイル -> GISデータ変換ツールの機能呼び出し
            if (isOutputBuilding || isOutputFirePath)
            {
                // settingファイルの作成
                var settingPath = Path.Combine(this.WorkingFolderPath, "ResultFileConv.setting");

                var information = new ResultFileConvSetting
                {
                    InputSimulationSourceFolderPath = inputSimulationSourceFolderPath,
                    InputSimulationResultFolderPath = simOutputFolderPath,
                    OutputGisDataFolderPath = outputGisDataFolderPath,
                    IsOutputBuilding = isOutputBuilding,
                    IsOutputFirePath = isOutputFirePath,
                    IsEllipsoidHeight = isEllipsoidHeight,
                };

                var jsonStr = JsonConvert.SerializeObject(information, Formatting.Indented);
                File.WriteAllText(settingPath, jsonStr);

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
                    App.Logger.Error($"GISデータ変換ツール実行exeの起動に失敗");
                    return false;
                }

                // 終了するまで待つ
                this.process.WaitForExit();

                // 終了コードを確認
                if (this.process.ExitCode != 0)
                {
                    return false;
                }
            }

            // KMLファイル -> そのまま複製するだけ
            if (isOutputKml)
            {
                var orgFilePath = Path.Combine(simOutputFolderPath, "out.kml");
                var newFilePath = Path.Combine(outputGisDataFolderPath, "out.kml");

                if (!File.Exists(orgFilePath))
                {
                    return false;
                }

                File.Copy(orgFilePath, newFilePath, true);
            }

            return true;
        }
    }
}
