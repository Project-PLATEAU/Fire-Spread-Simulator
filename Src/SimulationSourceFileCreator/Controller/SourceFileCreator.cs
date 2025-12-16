using System.IO;
using SimulationCommonLibrary.Utility;
using SimulationSourceFileCreator.Model;
using SimulationSourceFileCreator.View;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// CityGMLファイルを変換するクラス
    /// </summary>
    public class SourceFileCreator
    {
        /// <summary>
        /// 処理の進捗通知プロパティ
        /// </summary>
        private readonly IProgress<(int, int, string, string)> progress;

        /// <summary>
        /// キャンセルトークン
        /// </summary>
        private readonly CancellationTokenSource cancelToken;

        /// <summary>
        /// データ変換ツール実行クラス
        /// </summary>
        private readonly GeneFileExecutor geneFileExecutor;

        /// <summary>
        /// シミュレーションエンジン実行クラス
        /// </summary>
        private readonly SimFireExecutor simFireExecutor;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// private にして外部からのインスタンス化を禁止します。
        /// </remarks>
        /// <param name="geneFileExecutor">データ変換ツール実行クラス</param>
        /// <param name="simFireExecutor">シミュレーションエンジン実行クラス</param>
        /// <param name="progress">処理の進捗通知プロパティ</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        private SourceFileCreator(GeneFileExecutor geneFileExecutor, SimFireExecutor simFireExecutor, IProgress<(int, int, string, string)> progress, CancellationTokenSource cancelToken)
        {
            this.geneFileExecutor = geneFileExecutor;
            this.simFireExecutor = simFireExecutor;

            this.progress = progress;
            this.cancelToken = cancelToken;
        }

        /// <summary>
        /// インスタンスを作成します。
        /// </summary>
        /// <param name="progress">処理の進捗通知プロパティ</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>インスタンス</returns>
        internal static SourceFileCreator? CreateInstance(IProgress<(int, int, string, string)> progress, CancellationTokenSource cancelToken)
        {
            var geneFileExecutor = GeneFileExecutor.CreateInstance();
            var simFireExecutor = SimFireExecutor.CreateInstance();

            if (geneFileExecutor == null || simFireExecutor == null)
            {
                return null;
            }

            return new SourceFileCreator(geneFileExecutor, simFireExecutor, progress, cancelToken);
        }

        /// <summary>
        /// 処理を中止します。
        /// </summary>
        internal void Cancel()
        {
            this.geneFileExecutor.Cancel();
            this.simFireExecutor.Cancel();
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="convertType">変換種類</param>
        /// <param name="targetFilePathList">対象ファイルパスリスト</param>
        /// <param name="outputFolderPath">出力フォルダパス</param>
        /// <param name="coordinateSystemNumber">平面直角座標系</param>
        /// <param name="defaultFireproofStructureType">防火構造</param>
        /// <returns>成功件数、失敗件数</returns>
        internal (int successCount, int errorCount) Execute(ConvertType convertType, List<string> targetFilePathList, string outputFolderPath, int coordinateSystemNumber, int defaultFireproofStructureType)
        {
            App.Logger.Info($"変換開始");

            var intermediateCsvFileCreator = new IntermediateCsvFileCreator();
            var elementAddCityGmlFileCreator = new ElementAddCityGmlFileCreator();
            var forSupportToolFileCreator = new ForSupportToolFileCreator();

            var totalCount = targetFilePathList.Count;
            var successCount = 0;
            var errorCount = 0;
            var count = 0;

            try
            {
                var setting = ElementAddSetting.Load(ConstSystemPath.SettingFilePath);
                if (setting == null)
                {
                    App.Logger.Error($"configファイルの読み込み失敗");

                    this.OutputSummaryLog(totalCount, successCount, errorCount);
                    return (successCount, errorCount);
                }

                // 作業フォルダのクリーンアップ
                if (!convertType.Equals(ConvertType.FromCSV))
                {
                    DirectoryUtility.CleanupDirectory(ConstSystemPath.WorkspaceCSVFolderPath, "*.csv");
                }

                // 作業フォルダのクリーンアップ
                DirectoryUtility.CleanupDirectory(ConstSystemPath.WorkspaceGMLFolderPath, "*.gml");

                // 出力先フォルダパス
                var outputSimSourceFolderPath = Path.Combine(outputFolderPath, "sim_source");
                var outputBuildingGeojsonFolderPath = Path.Combine(outputFolderPath, "bldg_geojson");
                var outputBuildingCzmlFolderPath = Path.Combine(outputFolderPath, "bldg_czml");

                if (!convertType.Equals(ConvertType.ToCSV))
                {
                    // 出力先フォルダ作成（既にあれば何もしない）
                    Directory.CreateDirectory(outputSimSourceFolderPath);
                    Directory.CreateDirectory(outputBuildingGeojsonFolderPath);
                    Directory.CreateDirectory(outputBuildingCzmlFolderPath);
                }

                static string MainMessage(int totalCount, int count) => $"{count} / {totalCount} 変換中";
                static string SubMessage(string fileName, string msg) => $"[{fileName}]\r\n{msg}";

                static int MaxCout(int totalCount) => totalCount * 7;
                static int ProgressCount(int nowCount, int step) => ((nowCount - 1) * 7) + step;

                foreach (var filePath in targetFilePathList)
                {
                    count++;

                    var fileName = Path.GetFileName(filePath);
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 0), MainMessage(totalCount, count), SubMessage(fileName, "準備中")));

                    if (this.cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // CityGMLファイルの読み込み
                    if (!CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out var buildingNodes, out var meshNumer))
                    {
                        errorCount++;
                        App.Logger.Error($"変換失敗：CityGMLファイルの読み込み失敗 [{fileName}]");
                        continue;
                    }

                    // 取得要素のプレフィックスのチェック
                    if (!CityGmlFileLoader.CheckGetSettingPrefix(xmlnsManager, setting))
                    {
                        errorCount++;
                        App.Logger.Error($"変換失敗：configファイルの取得要素のプレフィックスがCityGMLファイルに定義されていない [{fileName}]");
                        continue;
                    }

                    var outputCsvFilePath = Path.Combine(ConstSystemPath.WorkspaceCSVFolderPath, $"{meshNumer}.csv");
                    var outpuCityGmlFilePath = Path.Combine(ConstSystemPath.WorkspaceGMLFolderPath, $"{meshNumer}_bldg.gml");

                    // 中間CSVファイル作成
                    if (!convertType.Equals(ConvertType.FromCSV))
                    {
                        this.progress.Report((MaxCout(totalCount), ProgressCount(count, 1), MainMessage(totalCount, count), SubMessage(fileName, "中間CSVファイル作成中")));
                        if (!intermediateCsvFileCreator.CreateCSVFile(xmlnsManager, buildingNodes, outputCsvFilePath, defaultFireproofStructureType.ToString(), setting, this.cancelToken))
                        {
                            if (this.cancelToken.IsCancellationRequested)
                            {
                                break;
                            }

                            errorCount++;
                            App.Logger.Error($"変換失敗：中間CSVファイル作成失敗 [{fileName}]");
                            continue;
                        }
                    }

                    if (convertType.Equals(ConvertType.ToCSV))
                    {
                        successCount++;
                        App.Logger.Info($"変換成功：[{fileName}]");
                        continue;
                    }

                    // 要素追加済みCityGMLファイル作成
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 2), MainMessage(totalCount, count), SubMessage(fileName, "要素追加済みCityGMLファイル作成中")));
                    if (!elementAddCityGmlFileCreator.CreateGmlFile(xmlDoc, xmlnsManager, buildingNodes, outputCsvFilePath, outpuCityGmlFilePath, setting, this.cancelToken))
                    {
                        if (this.cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        errorCount++;
                        App.Logger.Error($"変換失敗：要素追加済みCityGMLファイル作成失敗 [{fileName}]");
                        continue;
                    }

                    if (this.cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // GeneFile 実行
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 3), MainMessage(totalCount, count), SubMessage(fileName, "シミュレーションエンジン用データ作成中（GeneFile 実行中）")));
                    if (!this.geneFileExecutor.Execute(outpuCityGmlFilePath, meshNumer, coordinateSystemNumber))
                    {
                        if (this.cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        errorCount++;
                        App.Logger.Error($"変換失敗：シミュレーションエンジン用データ作成（GeneFile 実行）失敗 [{fileName}]");
                        continue;
                    }

                    // データ補正
                    if (!SmfrdatFileLoader.CorrectOrRemoveInvalidShape())
                    {
                        errorCount++;
                        App.Logger.Error($"変換失敗：データ補正失敗 [{fileName}]");
                        continue;
                    }

                    if (this.cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // SimFire 実行
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 4), MainMessage(totalCount, count), SubMessage(fileName, "シミュレーションエンジン用データ作成中（SimFire 実行中）")));
                    if (!this.simFireExecutor.Execute(meshNumer, outputSimSourceFolderPath))
                    {
                        if (this.cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        errorCount++;
                        App.Logger.Error($"変換失敗：シミュレーションエンジン用データ作成（SimFire 実行）失敗 [{fileName}]");
                        continue;
                    }

                    // 地上階数の収集
                    if (!SmfrdatFileLoader.CollectAboveFloorNum(out var bldgAboveFloorNumDict))
                    {
                        errorCount++;
                        App.Logger.Error($"変換失敗：地上階数の収集失敗 [{fileName}]");
                        continue;
                    }

                    // 建物GeoJsonファイル作成
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 5), MainMessage(totalCount, count), SubMessage(fileName, "条件設定支援ツール用データ作成中（GeoJSON）")));
                    var outpuGeojsonFilePath = Path.Combine(outputBuildingGeojsonFolderPath, $"Building_{meshNumer}.geojson");
                    if (!forSupportToolFileCreator.CreateBuldingGeojsonFile(buildingNodes, xmlnsManager, filePath, outpuGeojsonFilePath, setting, bldgAboveFloorNumDict, this.cancelToken))
                    {
                        if (this.cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        errorCount++;
                        App.Logger.Error($"変換失敗：条件設定支援ツール用データ作成（GeoJSON）失敗 [{fileName}]");
                        continue;
                    }

                    // 建物CZMLファイル作成
                    this.progress.Report((MaxCout(totalCount), ProgressCount(count, 6), MainMessage(totalCount, count), SubMessage(fileName, "条件設定支援ツール用データ作成中（CZML）")));
                    var outpuCzmlFilePath = Path.Combine(outputBuildingCzmlFolderPath, $"Building_{meshNumer}.czml");
                    if (!forSupportToolFileCreator.CreateBuldingCzmlFile(buildingNodes, xmlnsManager, filePath, outpuCzmlFilePath, setting, this.cancelToken))
                    {
                        if (this.cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        errorCount++;
                        App.Logger.Error($"変換失敗：条件設定支援ツール用データ作成（CZML）失敗 [{fileName}]");
                        continue;
                    }

                    successCount++;
                    App.Logger.Info($"変換成功：[{fileName}]");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                App.Logger.Error($"想定外のエラー", ex);
            }

            this.OutputSummaryLog(totalCount, successCount, errorCount);
            return (successCount, errorCount);
        }

        /// <summary>
        /// 実行結果をログに出力します。
        /// </summary>
        /// <param name="totalCount">全件数</param>
        /// <param name="successCount">成功件数</param>
        /// <param name="errorCount">失敗件数</param>
        private void OutputSummaryLog(int totalCount, int successCount, int errorCount)
        {
            var summaryMessage = "成功";
            if (this.cancelToken.IsCancellationRequested)
            {
                summaryMessage = "中止";
            }
            else if (successCount != totalCount)
            {
                summaryMessage = "失敗";
            }

            App.Logger.Info($"変換終了（{summaryMessage}）：全 {totalCount:#,0}件（成功 {successCount:#,0}件, 失敗 {errorCount:#,0}件, 未実施 {totalCount - successCount - errorCount:#,0}件）");
        }
    }
}
