using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SimulationCommonLibrary.Model;
using SimulationCommonLibrary.Utility;
using SimulationSupportTool.Model;

namespace SimulationSupportTool.Controller
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
        /// <param name="simConditionFolderPath">シミュレーション条件フォルダパス</param>
        /// <param name="simMapFolderPath">シミュレーション建物情報フォルダパス</param>
        /// <param name="simOutputFolderPath">シミュレーション結果フォルダパス</param>
        private SimFireExecutor(string workingFolderPath, string exeFilePath, string simConditionFolderPath, string simMapFolderPath, string simOutputFolderPath)
        {
            this.WorkingFolderPath = workingFolderPath;
            this.ExeFilePath = exeFilePath;

            this.SimConditionFolderPath = simConditionFolderPath;
            this.SimMapFolderPath = simMapFolderPath;
            this.SimOutputFolderPath = simOutputFolderPath;
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
        /// シミュレーション条件フォルダパス
        /// </summary>
        private string SimConditionFolderPath { get; set; }

        /// <summary>
        /// シミュレーション建物情報フォルダパス
        /// </summary>
        private string SimMapFolderPath { get; set; }

        /// <summary>
        /// シミュレーション結果フォルダパス
        /// </summary>
        private string SimOutputFolderPath { get; set; }

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

            var simConditionFolderPath = Path.Combine(workingFolderPath, "sim_cond");
            if (!Directory.Exists(simConditionFolderPath))
            {
                // ここには来ないはず
                App.Logger.Error($"アプリケーションが正しく配置されていない（シミュレーション条件フォルダがない）");
                return null;
            }

            // シミュレーション建物情報フォルダ作成（既にあれば何もしない）
            var simMapFolderPath = Path.Combine(workingFolderPath, "sim_map");
            Directory.CreateDirectory(simMapFolderPath);

            // シミュレーション結果フォルダ作成（既にあれば何もしない）
            var simOutputFolderPath = Path.Combine(workingFolderPath, "sim_out");
            Directory.CreateDirectory(simOutputFolderPath);
            DirectoryUtility.CleanupDirectory(simOutputFolderPath, "*.*");

            return new SimFireExecutor(workingFolderPath, exeFilePath, simConditionFolderPath, simMapFolderPath, simOutputFolderPath);
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
        /// シミュレーションエンジンで使用するファイルを準備します。
        /// </summary>
        /// <param name="inputSimulationSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        /// <param name="selectedSimulationRangeMeshNumbers">シミュレーション範囲のメッシュ番号のリスト</param>
        /// <param name="firePointList">出火点のリスト</param>
        /// <param name="windConditionList">風向・風速のリスト</param>
        /// <returns>成否</returns>
        internal bool Prepare(string inputSimulationSourceFolderPath, int simulationTimeTotalMinutes, string[] selectedSimulationRangeMeshNumbers, List<FirePoint> firePointList, List<WindCondition> windConditionList)
        {
            var isSuccess = true;

            // simfire.ini の作成
            isSuccess &= this.CreateSimulationIniFile(simulationTimeTotalMinutes);

            // outbreak.dat の作成
            isSuccess &= this.CreateFirePointDatFile(firePointList);

            // weather.dat の作成
            isSuccess &= this.CreateWindConditionDatFile(windConditionList);

            // builds.dat rooms.dat の作成
            isSuccess &= this.CreateSimulationMapFiles(inputSimulationSourceFolderPath, selectedSimulationRangeMeshNumbers);

            // sim_info.txt の作成
            isSuccess &= this.CreateSimulationInformationFile(simulationTimeTotalMinutes, selectedSimulationRangeMeshNumbers);

            return isSuccess;
        }

        /// <summary>
        /// 非同期で処理を実行し、進捗を通知します。
        /// </summary>
        /// <param name="progressReport">処理の進捗通知プロパティのReportを呼び出すAction</param>
        /// <returns>非同期操作を表す <see cref="Task"/>（成否）</returns>
        internal async Task<bool> ExecuteAndCheckProgressAsync(Action<int> progressReport)
        {
            /* シミュレーションエンジンの実行 */

            var isRunning = true;
            var isSuccess = true;

            var runAction = new ThreadStart(
                () => this.Execute(
                    (isEngineSuccess) =>
                    {
                        isSuccess = isEngineSuccess;
                        isRunning = false;
                    }));

            var thread = new Thread(runAction)
            {
                IsBackground = true,
            };

            thread.Start();

            /* シミュレーションエンジンの進捗確認 */

            var targetFilePath = Path.Combine(this.SimOutputFolderPath, "out.csv");
            var targetCheckFilePath = Path.Combine(this.SimOutputFolderPath, "out_check.csv");

            var count = 0;
            while (isRunning)
            {
                // 1秒待機
                await Task.Delay(1000);

                // 1秒毎 終了確認
                if (!isRunning)
                {
                    break;
                }

                // 5秒毎 進捗確認
                if (count % 5 == 0)
                {
                    var seconds = this.CheckSimulationCalculationSeconds(targetFilePath, targetCheckFilePath);
                    progressReport(seconds);

                    count = 0;
                }

                count++;
            }

            // 進捗確認用に複製したファイルの削除
            if (File.Exists(targetCheckFilePath))
            {
                File.Delete(targetCheckFilePath);
            }

            return isSuccess;
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="endAction">処理終了時に実行するコールバックAction</param>
        internal void Execute(Action<bool> endAction)
        {
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
                App.Logger.Error($"シミュレーションエンジン実行exeの起動に失敗");
                endAction(false);
                return;
            }

            // 終了するまで待つ
            this.process.WaitForExit();
            endAction(true);
        }

        /// <summary>
        /// シミュレーション結果フォルダパスを取得します。
        /// </summary>
        /// <returns>シミュレーション結果フォルダパス</returns>
        internal string GetSimOutFolderPath()
        {
            return this.SimOutputFolderPath;
        }

        /// <summary>
        /// シミュレーションの進捗をチェックします。
        /// </summary>
        /// <param name="targetFilePath">対象ファイルパス</param>
        /// <param name="targetCheckFilePath">複製先ファイルパス</param>
        /// <returns>計算時刻（単位：秒）</returns>
        private int CheckSimulationCalculationSeconds(string targetFilePath, string targetCheckFilePath)
        {
            var seconds = 0;

            if (!File.Exists(targetFilePath))
            {
                return seconds;
            }

            // 直接開かずに複製をしてチェックする
            File.Copy(targetFilePath, targetCheckFilePath, true);

            using (var rsr = new ReversStreamReader(targetCheckFilePath, new UTF8Encoding(false)))
            {
                // 末尾から一行データ取得
                while (rsr.Peek() >= 0)
                {
                    var line = rsr.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        var words = line.Split(',');

                        // タイミングによっては数値の途中の場合があるのでカンマが1つ以上ある場合を対象にする
                        if (1 < words.Length && int.TryParse(words[0], out seconds))
                        {
                            // 「計算時刻」の行を見つけたら終了
                            // ※「計算時刻」の行 = 「延焼時刻,焼損棟数,燃え尽き棟数,受熱棟数,焼損面積,火面周長,鎮火周長」
                            // ※先頭カラム「延焼時刻」 = 「シミュレーション開始からの経過時間」
                            break;
                        }
                    }
                }
            }

            return seconds;
        }

        /// <summary>
        /// 条件設定ファイルを作成します。
        /// </summary>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        /// <returns>成否</returns>
        private bool CreateSimulationIniFile(int simulationTimeTotalMinutes)
        {
            var orgFilePath = Path.Combine(this.WorkingFolderPath, "simfire_org.ini");
            var newFilePath = Path.Combine(this.WorkingFolderPath, "simfire.ini");

            if (!FileUtility.CopyAndRewrite(
                orgFilePath,
                newFilePath,
                (line) =>
                {
                    // 「SimTime」の値を書き換え
                    if (line.StartsWith("SimTime"))
                    {
                        return $"SimTime = {simulationTimeTotalMinutes}";
                    }

                    return line;
                },
                Encoding.GetEncoding("Shift_JIS")))
            {
                App.Logger.Error($"iniファイルの複製に失敗");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 出火点ファイルを作成します。
        /// </summary>
        /// <param name="firePointList">出火点のリスト</param>
        /// <returns>成否</returns>
        private bool CreateFirePointDatFile(List<FirePoint> firePointList)
        {
            var outputFilePath = Path.Combine(this.SimConditionFolderPath, "outbreak.dat");

            using (var sw = new StreamWriter(outputFilePath, false, new UTF8Encoding(false)))
            {
                sw.WriteLine(firePointList.Count);

                for (var i = 0; i < firePointList.Count; i++)
                {
                    var f = firePointList[i];

                    if (!int.TryParse(f.StartMinutes, out var minute))
                    {
                        return false;
                    }

                    var bldgId = f.BldgId;
                    var story = f.SelectedStory.HasValue ? $"{f.SelectedStory.Value}F" : "1F";
                    var startSeconds = minute * 60;

                    sw.WriteLine($"0,{bldgId},{story},{startSeconds}");
                }
            }

            return true;
        }

        /// <summary>
        /// 気象情報ファイルを作成します。
        /// </summary>
        /// <param name="windConditionList">風向・風速のリスト</param>
        /// <returns>成否</returns>
        private bool CreateWindConditionDatFile(List<WindCondition> windConditionList)
        {
            // 先に開始時間と方位を変換しておく
            var wcList = new List<(int seconds, double speed, double direction)>();

            foreach (var windCondition in windConditionList)
            {
                if (windCondition.No == 0)
                {
                    // 追加ボタン用の1行は対象外
                    continue;
                }

                if (!int.TryParse(windCondition.StartMinutes, out var minute))
                {
                    return false;
                }

                wcList.Add((minute * 60, windCondition.WindSpeed, windCondition.WindDirection));
            }

            wcList.Sort(new Comparison<(int seconds, double speed, double direction)>((a, b) => a.seconds - b.seconds));

            var outputFilePath = Path.Combine(this.SimConditionFolderPath, "weather.dat");

            using (var sw = new StreamWriter(outputFilePath, false, new UTF8Encoding(false)))
            {
                sw.WriteLine($"WindSpeed,{wcList.Count}");

                for (var i = 0; i < wcList.Count; i++)
                {
                    var (seconds, speed, _) = wcList[i];
                    sw.WriteLine($"{seconds},{speed}");
                }

                sw.WriteLine($"WindAngle,{wcList.Count}");

                for (var i = 0; i < wcList.Count; i++)
                {
                    var (seconds, _, direction) = wcList[i];
                    sw.WriteLine($"{seconds},{direction}");
                }

                sw.WriteLine("Temperature, 293"); // 固定
            }

            return true;
        }

        /// <summary>
        /// 建物情報ファイル（設定範囲の結合後）<br/>
        /// 建物内部情報ファイル（設定範囲の結合後）を作成します。
        /// </summary>
        /// <param name="sourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="selectedSimulationRangeMeshNumbers">シミュレーション範囲のメッシュ番号のリスト</param>
        /// <returns>成否</returns>
        private bool CreateSimulationMapFiles(string sourceFolderPath, string[] selectedSimulationRangeMeshNumbers)
        {
            var now = DateTime.Now;

            /* builds.dat の作成*/

            // 先に建物の件数を取得する
            var firstProjection = string.Empty;
            var allBldgCount = 0;

            foreach (var meshNumber in selectedSimulationRangeMeshNumbers)
            {
                var buildsDatFilePath = Path.Combine(sourceFolderPath, "sim_source", $"builds_{meshNumber}.dat");
                if (!File.Exists(buildsDatFilePath))
                {
                    App.Logger.Error($"シミュレーションエンジンのソースデータがない buildsDatFilePath = {buildsDatFilePath}");
                    return false;
                }

                var lineCount = 0;
                using (var sr = new StreamReader(buildsDatFilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        if (string.IsNullOrEmpty(firstProjection) && line.StartsWith("<projection>"))
                        {
                            var projection = line.Substring(12);

                            // 座標系がまたがることは想定しないため最初の座標系を採用する
                            firstProjection = projection;
                        }

                        if (lineCount == 5)
                        {
                            var words = line.Split(',');
                            if (!int.TryParse(words[0], out var bldgCount))
                            {
                                App.Logger.Error($"シミュレーションエンジンのソースデータの建物の件数が取得できない buildsDatFilePath = {buildsDatFilePath}");
                                return false;
                            }

                            allBldgCount += bldgCount;
                            break;
                        }

                        lineCount++;
                    }
                }
            }

            var outputBuildsDatFilePath = Path.Combine(this.SimMapFolderPath, "builds.dat");

            using (var sw = new StreamWriter(outputBuildsDatFilePath))
            {
                sw.WriteLine("<Header>");
                sw.WriteLine("<dataVersion>");
                sw.WriteLine($"<projection>{firstProjection}");
                sw.WriteLine($"<generate>{now}");
                sw.WriteLine("</Header>");
                sw.WriteLine($"{allBldgCount},");

                foreach (var meshNumber in selectedSimulationRangeMeshNumbers)
                {
                    var buildsDatFilePath = Path.Combine(sourceFolderPath, "sim_source", $"builds_{meshNumber}.dat");
                    if (!File.Exists(buildsDatFilePath))
                    {
                        App.Logger.Error($"シミュレーションエンジンのソースデータがない buildsDatFilePath = {buildsDatFilePath}");
                        return false;
                    }

                    var lineCount = 0;
                    using (var sr = new StreamReader(buildsDatFilePath))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();

                            // 6行目までは無視
                            if (lineCount < 6)
                            {
                                lineCount++;
                                continue;
                            }

                            // 7行目以降を出力
                            sw.WriteLine(line);
                        }
                    }
                }
            }

            /* rooms.dat の作成 */

            var outputRoomsDatFilePath = Path.Combine(this.SimMapFolderPath, "rooms.dat");

            using (var sw = new StreamWriter(outputRoomsDatFilePath))
            {
                sw.WriteLine("<Header>");
                sw.WriteLine("<dataVersion>");
                sw.WriteLine($"<projection>{firstProjection}");
                sw.WriteLine($"<generate>{now}");
                sw.WriteLine("</Header>");

                foreach (var meshNumber in selectedSimulationRangeMeshNumbers)
                {
                    var roomsDatFilePath = Path.Combine(sourceFolderPath, "sim_source", $"rooms_{meshNumber}.dat");
                    if (!File.Exists(roomsDatFilePath))
                    {
                        App.Logger.Error($"シミュレーションエンジンのソースデータがない roomsDatFilePath = {roomsDatFilePath}");
                        return false;
                    }

                    var count = 0;
                    using (var sr = new StreamReader(roomsDatFilePath))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();

                            // 5行目までは無視
                            if (count < 5)
                            {
                                count++;
                                continue;
                            }

                            // 6行目以降を出力
                            sw.WriteLine(line);
                            count++;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// シミュレーション実行情報ファイルを作成します。
        /// </summary>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        /// <param name="selectedSimulationRangeMeshNumbers">シミュレーション範囲のメッシュ番号のリスト</param>
        /// <returns>成否</returns>
        private bool CreateSimulationInformationFile(int simulationTimeTotalMinutes, string[] selectedSimulationRangeMeshNumbers)
        {
            var outputFilePath = Path.Combine(this.SimOutputFolderPath, "sim_info.txt");

            var utcMidnight = DateTime.UtcNow.Date; // UTC基準の0:00

            var information = new SimulationInformation
            {
                SimulationTimeTotalMinutes = simulationTimeTotalMinutes,
                SelectedSimulationRangeMeshNumbers = selectedSimulationRangeMeshNumbers,
                SimulationStartDateTime = utcMidnight,
            };

            var jsonStr = JsonConvert.SerializeObject(information, Formatting.Indented);
            File.WriteAllText(outputFilePath, jsonStr);

            return true;
        }
    }
}
