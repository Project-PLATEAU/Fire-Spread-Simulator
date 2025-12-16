using SimulationSupportTool.Model;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// シミュレーションを実行するクラス
    /// </summary>
    internal class SimulationExecutor
    {
        /// <summary>
        /// 遅延初期化インスタンス
        /// </summary>
        private static readonly Lazy<SimulationExecutor> InstanceValue = new Lazy<SimulationExecutor>(() => new SimulationExecutor());

        /// <summary>
        /// シミュレーションエンジン実行クラス
        /// </summary>
        private SimFireExecutor? simFireExecutor;

        /// <summary>
        /// GISデータ変換ツール実行クラス
        /// </summary>
        private ResultFileConvExecutor? resultFileConvExecutor;

        /// <summary>
        /// 中止したかどうか
        /// </summary>
        private bool isCanceled = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// シングルトンパターンで利用するため private にして外部からのインスタンス化を禁止します。
        /// </remarks>
        private SimulationExecutor()
        {
        }

        /// <summary>
        /// インスタンス
        /// </summary>
        internal static SimulationExecutor Instance => InstanceValue.Value;

        /// <summary>
        /// 非同期で処理を実行します。
        /// </summary>
        /// <param name="inputSimulationSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="outputGisDataFolderPath">GISデータ出力フォルダパス</param>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        /// <param name="selectedSimulationRangeMeshNumbers">シミュレーション範囲のメッシュ番号のリスト</param>
        /// <param name="firePointList">出火点のリスト</param>
        /// <param name="windConditionList">風向・風速のリスト</param>
        /// <param name="isOutputKml">KMLファイルを出力するかどうか</param>
        /// <param name="isOutputCzmlBuilding">建物延焼情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isOutputCzmlFirePath">延焼経路情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isEllipsoidHeight">CZMLファイルの高さを楕円体高にするかどうか（true = 楕円体高、false = 標高）</param>
        /// <param name="progress">処理の進捗通知プロパティ</param>
        /// <returns>非同期操作を表す <see cref="Task"/>（成否）</returns>
        internal async Task<bool> ExecuteAsync(string inputSimulationSourceFolderPath, string outputGisDataFolderPath, int simulationTimeTotalMinutes, string[] selectedSimulationRangeMeshNumbers, List<FirePoint> firePointList, List<WindCondition> windConditionList, bool isOutputKml, bool isOutputCzmlBuilding, bool isOutputCzmlFirePath, bool isEllipsoidHeight, IProgress<(string, string)> progress)
        {
            this.isCanceled = false;

            this.simFireExecutor = SimFireExecutor.CreateInstance();
            if (this.simFireExecutor == null)
            {
                return false;
            }

            this.resultFileConvExecutor = ResultFileConvExecutor.CreateInstance();
            if (this.resultFileConvExecutor == null)
            {
                return false;
            }

            /* シミュレーションエンジンで使用するファイル・フォルダの準備 */
            progress.Report(("シミュレーション準備中...", string.Empty));

            var simFireExecutorPrepareRes = false;
            await Task.Run(() =>
            {
                simFireExecutorPrepareRes = this.simFireExecutor.Prepare(
                    inputSimulationSourceFolderPath,
                    simulationTimeTotalMinutes,
                    selectedSimulationRangeMeshNumbers,
                    firePointList,
                    windConditionList);
            });

            if (!simFireExecutorPrepareRes)
            {
                return false;
            }

            if (this.isCanceled)
            {
                return true;
            }

            /* シミュレーションエンジンの実行と進捗確認 */
            progress.Report(("シミュレーション実行中...", string.Empty));
            if (!await this.simFireExecutor.ExecuteAndCheckProgressAsync(
                (seconds) =>
                {
                    progress.Report(("シミュレーション実行中...", $"{this.MMToHHMM(seconds / 60)} / {this.MMToHHMM(simulationTimeTotalMinutes)} 計算中"));
                }))
            {
                return false;
            }

            if (this.isCanceled)
            {
                return true;
            }

            /* 出力変換処理 */
            progress.Report(("シミュレーション結果変換中...", string.Empty));

            var simOutputFolderPath = this.simFireExecutor.GetSimOutFolderPath();
            var resultFileConvExecutorExecuteRes = false;
            await Task.Run(() =>
            {
                resultFileConvExecutorExecuteRes = this.resultFileConvExecutor.Execute(
                    inputSimulationSourceFolderPath,
                    simOutputFolderPath,
                    outputGisDataFolderPath,
                    isOutputCzmlBuilding,
                    isOutputCzmlFirePath,
                    isEllipsoidHeight,
                    isOutputKml);
            });

            if (!resultFileConvExecutorExecuteRes)
            {
                return false;
            }

            /* 処理終了 */
            return true;
        }

        /// <summary>
        /// 処理を中止します。
        /// </summary>
        internal void Cancel()
        {
            // 強制終了
            this.isCanceled = true;
            this.simFireExecutor?.Cancel();
            this.resultFileConvExecutor?.Cancel();
        }

        /// <summary>
        /// 分の数値を00時間00分形式にフォーマットします。
        /// </summary>
        /// <param name="minutes">分</param>
        /// <returns>00時間00分形式の文字列</returns>
        private string MMToHHMM(int minutes)
        {
            var hh = minutes / 60;
            var mm = minutes % 60;

            return $"{hh:00}時間{mm:00}分";
        }
    }
}
