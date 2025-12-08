using System.Drawing;
using System.Text;
using CesiumLanguageWriter;
using Newtonsoft.Json;
using SimulationCommonLibrary.Model;
using SimulationCommonLibrary.Utility;
using SimulationResultFileConverter.Model;

namespace SimulationResultFileConverter.Controller
{
    /// <summary>
    /// シミュレーション結果を変換するクラス
    /// </summary>
    internal class ResultFileConverter
    {
        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="inputSimulationSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="inputSimulationResultFolderPath">シミュレーション結果フォルダパス</param>
        /// <param name="outputGisDataFolderPath">GISデータ出力フォルダパス</param>
        /// <param name="isOutputBuilding">建物延焼情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isOutputFirePath">延焼経路情報ファイル（CZMLファイル）を出力するかどうか</param>
        /// <param name="isEllipsoidHeight">CZMLファイルの高さを楕円体高にするかどうか（true = 楕円体高、false = 標高）</param>
        /// <returns>成否</returns>
        public bool Execute(string inputSimulationSourceFolderPath, string inputSimulationResultFolderPath, string outputGisDataFolderPath, bool isOutputBuilding, bool isOutputFirePath, bool isEllipsoidHeight)
        {
            try
            {
                var information = this.ReadInformationFile(inputSimulationResultFolderPath);
                if (information == null)
                {
                    return false;
                }

                if (isOutputBuilding)
                {
                    var bldgSuccess = this.CreateBuilding(inputSimulationSourceFolderPath, inputSimulationResultFolderPath, outputGisDataFolderPath, information, isEllipsoidHeight);
                    if (!bldgSuccess)
                    {
                        return false;
                    }
                }

                if (isOutputFirePath)
                {
                    var firePathSuccess = this.CreateFirePath(inputSimulationSourceFolderPath, inputSimulationResultFolderPath, outputGisDataFolderPath, information, isEllipsoidHeight);
                    if (!firePathSuccess)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"想定外のエラー", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// シミュレーション実行情報ファイルを読み込みます。
        /// </summary>
        /// <param name="inputResultFolderPath">シミュレーション実行情報ファイルパス</param>
        /// <returns>シミュレーション実行情報</returns>
        private SimulationInformation? ReadInformationFile(string inputResultFolderPath)
        {
            var simulationInformationFilePath = Path.Combine(inputResultFolderPath, "sim_info.txt");
            if (!File.Exists(simulationInformationFilePath))
            {
                Program.Logger.Error($"シミュレーション情報ファイルがない simulationInformationFilePath = {simulationInformationFilePath}");
                return null;
            }

            SimulationInformation? condition = null;
            using (var sr = new StreamReader(simulationInformationFilePath))
            {
                condition = JsonConvert.DeserializeObject<SimulationInformation>(sr.ReadToEnd());
            }

            if (condition == null
                || condition.SimulationStartDateTime.Equals(DateTime.MinValue)
                || condition.SimulationTimeTotalMinutes <= 0
                || condition.SelectedSimulationRangeMeshNumbers.Length == 0)
            {
                Program.Logger.Error($"シミュレーション情報ファイルの読み込みに失敗 simulationInformationFilePath = {simulationInformationFilePath}");
                return null;
            }

            return condition;
        }

        /// <summary>
        /// 建物延焼情報ファイル（CZMLファイル）を作成します。
        /// </summary>
        /// <param name="inputSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="inputResultFolderPath">シミュレーション結果フォルダパス</param>
        /// <param name="outputFolderPath">GISデータ出力フォルダパス</param>
        /// <param name="information">シミュレーション実行情報</param>
        /// <param name="isEllipsoidHeight">CZMLファイルの高さを楕円体高にするかどうか（true = 楕円体高、false = 標高）</param>
        /// <returns>成否</returns>
        private bool CreateBuilding(string inputSourceFolderPath, string inputResultFolderPath, string outputFolderPath, SimulationInformation information, bool isEllipsoidHeight)
        {
            var igoutFilePath = Path.Combine(inputResultFolderPath, "igout.dat");
            if (!File.Exists(igoutFilePath))
            {
                Program.Logger.Error($"建物延焼情報がない igoutFilePath = {igoutFilePath}");
                return false;
            }

            var bldgFireTimeDict = new Dictionary<string, (int startSec, int endSec)>(); // key = bldgId、vaule = （開始秒、終了秒）

            // 建物延焼情報から必要な情報を収集
            using (var sr = new StreamReader(igoutFilePath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    var words = line.Split(',');
                    if (words.Length < 5)
                    {
                        continue;
                    }

                    var bldgId = words[2].Trim();
                    var startSec = words[3];
                    var endSec = words[4];

                    if (!int.TryParse(startSec, out var s) || !int.TryParse(endSec, out var e))
                    {
                        Program.Logger.Error($"出火時刻 or 燃え尽き時刻が数値ではない line = {line}");
                        return false;
                    }

                    if (99999999 <= s && 99999999 <= e)
                    {
                        continue;
                    }

                    if (99999999 <= s || information.SimulationTimeTotalMinutes * 60 < s)
                    {
                        Program.Logger.Error($"出火時刻無効値 or 計算時間を超えている line = {line}");
                        return false;
                    }

                    if (e < s)
                    {
                        Program.Logger.Error($"出火時刻と燃え尽き時刻の前後関係が逆 line = {line}");
                        return false;
                    }

                    if (bldgFireTimeDict.ContainsKey(bldgId))
                    {
                        // ここには来ないはず
                        Program.Logger.Error("ファイルの内容不備（同じBldgIdが複数記載されている）");
                        return false;
                    }

                    bldgFireTimeDict.Add(bldgId, (s, e));
                }
            }

            // シミュレーション実行情報から開始・終了時間を取得
            var simStart = new GregorianDate(information.SimulationStartDateTime).ToJulianDate();
            var simEnd = simStart.AddSeconds(information.SimulationTimeTotalMinutes * 60);

            // GISデータ変換ツール変換用建物データを元に建物延焼情報ファイル（CZMLファイル）を作成
            foreach (var meshNumber in information.SelectedSimulationRangeMeshNumbers)
            {
                var orgBldgCzmlFilePath = Path.Combine(inputSourceFolderPath, "bldg_czml", $"Building_{meshNumber}.czml");
                if (!File.Exists(orgBldgCzmlFilePath))
                {
                    Program.Logger.Error($"建物CZMLファイルがない orgCzmlFilePath = {orgBldgCzmlFilePath}");
                    return false;
                }

                var outputBldgCzmlFilePath = Path.Combine(outputFolderPath, $"bldg_{meshNumber}.czml");
                var geoidHeight = 0d;

                using (var sw = new StreamWriter(outputBldgCzmlFilePath))
                using (var sr = new StreamReader(orgBldgCzmlFilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        var isCommaStart = false;
                        var jsonstr = line;

                        if (jsonstr.StartsWith(','))
                        {
                            isCommaStart = true;
                            jsonstr = line.Substring(1); // 初めの1文字を削除
                        }

                        if (!jsonstr.StartsWith('{'))
                        {
                            // そのまま出力して継続（最初の[と最後の]が対象）
                            sw.WriteLine(line);
                            continue;
                        }

                        var model = JsonConvert.DeserializeObject<CzmlModel>(jsonstr);

                        if (model == null)
                        {
                            Program.Logger.Error($"建物CZMLファイルが不正 orgBldgCzmlFilePath = {orgBldgCzmlFilePath}");
                            return false;
                        }

                        var newLine = string.Empty;

                        if (model.Id.Equals("document"))
                        {
                            // "clock" を追加
                            using (var stringWriter = new StringWriter())
                            {
                                var output = new CesiumOutputStream(stringWriter);
                                var csw = new CesiumStreamWriter();

                                using (var packet = csw.OpenPacket(output))
                                {
                                    packet.WriteId("document");
                                    packet.WriteName("Bulding");
                                    packet.WriteVersion("1.0");

                                    using (var clock = packet.OpenClockProperty())
                                    {
                                        clock.WriteInterval(new TimeInterval(simStart, simEnd));
                                        clock.WriteCurrentTime(simStart);
                                        clock.WriteMultiplier(60);
                                    }
                                }

                                newLine = stringWriter.ToString();
                            }
                        }

                        if (model.Id.Contains("bldg"))
                        {
                            // "material" を追加
                            using (var stringWriter = new StringWriter())
                            {
                                var output = new CesiumOutputStream(stringWriter);
                                var csw = new CesiumStreamWriter();

                                using (var packet = csw.OpenPacket(output))
                                {
                                    packet.WriteId(model.Id);

                                    using (var polygon = packet.OpenPolygonProperty())
                                    {
                                        using (var material = polygon.OpenMaterialProperty())
                                        {
                                            using (var solidColor = material.OpenSolidColorProperty())
                                            {
                                                using (var color = solidColor.OpenColorProperty())
                                                {
                                                    if (!bldgFireTimeDict.TryGetValue(model.Id, out (int startSec, int endSec) value))
                                                    {
                                                        /* 建物延焼情報がない場合 */

                                                        color.WriteRgba(255, 255, 255, 200);
                                                    }
                                                    else
                                                    {
                                                        /* 建物延焼情報がある場合 */

                                                        using (var intervals = color.OpenMultipleIntervals())
                                                        {
                                                            if (value.startSec != 0)
                                                            {
                                                                // 白
                                                                using (var interval = intervals.OpenInterval())
                                                                {
                                                                    var fireStart = simStart.AddSeconds(value.startSec);
                                                                    var fireEnd = simStart.AddSeconds(value.startSec);

                                                                    interval.WriteInterval(simStart, fireStart);
                                                                    interval.WriteRgba(255, 255, 255, 200);
                                                                }
                                                            }

                                                            // 赤
                                                            using (var interval = intervals.OpenInterval())
                                                            {
                                                                var fireStart = simStart.AddSeconds(value.startSec);
                                                                var fireEnd = simEnd;
                                                                if (value.endSec < 99999999)
                                                                {
                                                                    fireEnd = simStart.AddSeconds(value.endSec);
                                                                }

                                                                interval.WriteInterval(fireStart, fireEnd);
                                                                interval.WriteRgba(255, 0, 0, 200);
                                                            }

                                                            if (value.endSec < 99999999)
                                                            {
                                                                // 黒
                                                                using (var interval = intervals.OpenInterval())
                                                                {
                                                                    var fireEnd = simStart.AddSeconds(value.endSec);

                                                                    interval.WriteInterval(fireEnd, simEnd);
                                                                    interval.WriteRgba(0, 0, 0, 200);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                newLine = stringWriter.ToString();
                            }

                            // 高さを補正する場合に備えて保持しておく
                            geoidHeight = model.GeoidHeight;
                        }

                        if (model.Parent.Contains("bldg"))
                        {
                            /*
                             * 高さを補正（データの高さは楕円体高）
                             * 　標高の場合：　　データの高さ - ジオイド高   ※isEllipsoidHeight = false
                             * 　楕円体高の場合：データの高さ（変更なし）    ※isEllipsoidHeight = true
                             */

                            if (isEllipsoidHeight)
                            {
                                // そのまま出力して継続
                                sw.WriteLine(line);
                                continue;
                            }
                            else
                            {
                                var subModel = JsonConvert.DeserializeObject<CzmlSubModel>(jsonstr);

                                if (subModel == null)
                                {
                                    Program.Logger.Error($"建物CZMLファイルが不正 orgBldgCzmlFilePath = {orgBldgCzmlFilePath}");
                                    return false;
                                }

                                for (var i = 0; i < subModel.Polygon.Positions.CartographicDegrees.Length; i++)
                                {
                                    if (i % 3 == 2)
                                    {
                                        subModel.Polygon.Positions.CartographicDegrees[i] = subModel.Polygon.Positions.CartographicDegrees[i] - geoidHeight;
                                    }
                                }

                                newLine = JsonConvert.SerializeObject(subModel);
                            }
                        }

                        if (string.IsNullOrEmpty(newLine))
                        {
                            sw.WriteLine(line);
                        }
                        else
                        {
                            if (isCommaStart)
                            {
                                sw.Write(',');
                            }

                            sw.WriteLine(newLine);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 延焼経路情報ファイル（CZMLファイル）を作成します。
        /// </summary>
        /// <param name="inputSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <param name="inputResultFolderPath">シミュレーション結果フォルダパス</param>
        /// <param name="outputFolderPath">GISデータ出力フォルダパス</param>
        /// <param name="information">シミュレーション実行情報</param>
        /// <param name="isEllipsoidHeight">CZMLファイルの高さを楕円体高にするかどうか（true = 楕円体高、false = 標高）</param>
        /// <returns>成否</returns>
        private bool CreateFirePath(string inputSourceFolderPath, string inputResultFolderPath, string outputFolderPath, SimulationInformation information, bool isEllipsoidHeight)
        {
            var firepathFilePath = Path.Combine(inputResultFolderPath, "firepath.dat");
            if (!File.Exists(firepathFilePath))
            {
                Program.Logger.Error($"延焼経路情報がない igoutFilePath = {firepathFilePath}");
                return false;
            }

            var bldgDict = new Dictionary<string, CzmlModel>(); // key = bldgId、vaule = 建物情報
            var timePosList = new List<(int timeSec, string fromBldg, double formLng, double formLat, double fz, string toBldg, double toLng, double toLat, double tz)>();
            var seriesNumber = 9;

            // 建物延焼情報から必要な情報を収集
            using (var sr = new StreamReader(firepathFilePath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("<projection>XY"))
                    {
                        var projectionNum = line.Substring(14);
                        if (!int.TryParse(projectionNum, out seriesNumber))
                        {
                            seriesNumber = 9;
                        }
                    }

                    var words = line.Split(',');
                    if (words.Length < 19)
                    {
                        continue;
                    }

                    var time = words[0];

                    var fromBldgId = words[7].Trim();
                    var fromX = words[11];
                    var fromY = words[12];
                    var fromZ = words[13];

                    var toBldgId = words[9].Trim();
                    var toX = words[14];
                    var toY = words[15];
                    var toZ = words[16];

                    if (!int.TryParse(time, out var timeSec)
                        || !double.TryParse(fromX, out var fx) || !double.TryParse(fromY, out var fy) || !double.TryParse(fromZ, out var fz)
                        || !double.TryParse(toX, out var tx) || !double.TryParse(toY, out var ty) || !double.TryParse(toZ, out var tz))
                    {
                        Program.Logger.Error($"延焼時刻 or 加害側代表点座標 or 受害側代表点座標が数値ではない line = {line}");
                        return false;
                    }

                    if (!GisUtility.TryParseXYToLatLng(fx, fy, fz, out var formLng, out var formLat, out var fromHeight, seriesNumber)
                        || !GisUtility.TryParseXYToLatLng(tx, ty, tz, out var toLng, out var toLat, out var toHeight, seriesNumber))
                    {
                        Program.Logger.Error($"平面直角座標系を経緯度に変換できない line = {line}");
                        return false;
                    }

                    if (!bldgDict.ContainsKey(fromBldgId))
                    {
                        bldgDict.Add(fromBldgId, new CzmlModel());
                    }

                    if (!bldgDict.ContainsKey(toBldgId))
                    {
                        bldgDict.Add(toBldgId, new CzmlModel());
                    }

                    if (fromHeight < 0)
                    {
                        // 念のため警告出力
                        Program.Logger.Warn($"標高がマイナス fromBldgId = {fromBldgId}, fromHeight = {fromHeight}");
                    }

                    if (toHeight < 0)
                    {
                        // 念のため警告出力
                        Program.Logger.Warn($"標高がマイナス toBldgId = {toBldgId}, toHeight = {toHeight}");
                    }

                    timePosList.Add((timeSec, fromBldgId, formLng, formLat, fromHeight, toBldgId, toLng, toLat, toHeight));
                }
            }

            // GISデータ変換ツール変換用建物データから建物延焼情報を収集
            foreach (var meshNumber in information.SelectedSimulationRangeMeshNumbers)
            {
                var bldgCzmlFilePath = Path.Combine(inputSourceFolderPath, "bldg_czml", $"Building_{meshNumber}.czml");
                if (!File.Exists(bldgCzmlFilePath))
                {
                    Program.Logger.Error($"建物CZMLファイルがない bldgCzmlFilePath = {bldgCzmlFilePath}");
                    return false;
                }

                using (var sr = new StreamReader(bldgCzmlFilePath))
                {
                    var models = JsonConvert.DeserializeObject<List<CzmlModel>>(sr.ReadToEnd());

                    if (models == null)
                    {
                        Program.Logger.Error($"建物CZMLファイルが不正 bldgCzmlFilePath = {bldgCzmlFilePath}");
                        return false;
                    }

                    foreach (var m in models)
                    {
                        if (string.IsNullOrEmpty(m.Id))
                        {
                            continue;
                        }

                        if (bldgDict.ContainsKey(m.Id))
                        {
                            bldgDict[m.Id] = m;
                        }
                    }
                }
            }

            // シミュレーション実行情報から開始・終了時間を取得
            var simStart = new GregorianDate(information.SimulationStartDateTime).ToJulianDate();
            var simEnd = simStart.AddSeconds(information.SimulationTimeTotalMinutes * 60);

            // 延焼経路情報ファイル（CZMLファイル）を作成
            using (var stringWriter = new StringWriter())
            {
                var output = new CesiumOutputStream(stringWriter);

                using (var packet = new CesiumStreamWriter().OpenPacket(output))
                {
                    packet.WriteId("document");
                    packet.WriteName("FirePath");
                    packet.WriteVersion("1.0");

                    using (var clock = packet.OpenClockProperty())
                    {
                        clock.WriteInterval(new TimeInterval(simStart, simEnd));
                        clock.WriteCurrentTime(simStart);
                        clock.WriteMultiplier(60);
                    }
                }

                for (var index = 0; index < timePosList.Count; index++)
                {
                    stringWriter.WriteLine(string.Empty); // 改行を入れる

                    using (var packet = new CesiumStreamWriter().OpenPacket(output))
                    {
                        packet.WriteId($"ID_{index}");
                        packet.WriteAvailability(new TimeInterval(simStart, simEnd));

                        using (var path = packet.OpenPathProperty())
                        {
                            using (var material = path.OpenMaterialProperty())
                            {
                                using (var poly = material.OpenPolylineOutlineProperty())
                                {
                                    poly.WriteColorPropertyRgbaf(Color.Red);
                                    poly.WriteOutlineColorPropertyRgbaf(Color.White);
                                    poly.WriteOutlineWidthProperty(2);
                                }
                            }

                            path.WriteWidthProperty(5);
                            path.WriteLeadTimeProperty(1);
                            path.WriteTrailTimeProperty(information.SimulationTimeTotalMinutes * 60);
                            path.WriteResolutionProperty(5);
                        }

                        using (var pos = packet.OpenPositionProperty())
                        {
                            var dates = new List<JulianDate>();
                            var values = new List<Cartographic>();

                            var (timeSec, fromBldg, formLng, formLat, fz, toBldg, toLng, toLat, tz) = timePosList[index];

                            var startSec = 0;
                            if (60 < timeSec)
                            {
                                startSec = timeSec - 60;
                            }

                            dates.Add(simStart.AddSeconds(startSec));
                            dates.Add(simStart.AddSeconds(timeSec));

                            /*
                             * 高さを補正（データの高さは標高）
                             * 　標高の場合：　　データの高さ（変更なし）   ※isEllipsoidHeight = false
                             * 　楕円体高の場合：データの高さ + ジオイド高  ※isEllipsoidHeight = true
                             */

                            var fromAdjust = 0d;
                            if (bldgDict.TryGetValue(fromBldg, out CzmlModel? fromValue))
                            {
                                if (isEllipsoidHeight)
                                {
                                    fromAdjust = fromValue.GeoidHeight;
                                }
                            }

                            var toAdjust = 0d;
                            if (bldgDict.TryGetValue(toBldg, out CzmlModel? toValue))
                            {
                                if (isEllipsoidHeight)
                                {
                                    toAdjust = toValue.GeoidHeight;
                                }
                            }

                            values.Add(new Cartographic(formLng, formLat, fz + fromAdjust));
                            values.Add(new Cartographic(toLng, toLat, tz + toAdjust));

                            pos.WriteCartographicDegrees(dates, values);
                        }
                    }
                }

                var outputFilePath = Path.Combine(outputFolderPath, "firepath.czml");

                using (var sw = new StreamWriter(outputFilePath, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("[");
                    sw.WriteLine(stringWriter.ToString());
                    sw.WriteLine("]");
                }
            }

            return true;
        }
    }
}
