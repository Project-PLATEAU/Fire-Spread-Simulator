using System.IO;
using BAMCIS.GeoJSON;
using Newtonsoft.Json;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// 三次メッシュデータ（ファイル）を作成するクラス
    /// </summary>
    internal class TertiaryMeshFileCreator
    {
        /// <summary>
        /// 三次メッシュデータ（ファイル）を作成します。
        /// </summary>
        /// <param name="inputSimulationSourceFolderPath">シミュレーションデータフォルダパス</param>
        /// <returns>成否</returns>
        internal bool Create(string inputSimulationSourceFolderPath)
        {
            var bldgFolder = Path.Combine(inputSimulationSourceFolderPath, "bldg_geojson");
            if (!Directory.Exists(bldgFolder))
            {
                App.Logger.Error($"フォルダが存在しない bldgFolder = {bldgFolder}");
                return false;
            }

            var targetFilePathList = Directory.GetFiles(bldgFolder, "Building_*.geojson");

            if (targetFilePathList.Length == 0)
            {
                App.Logger.Error($"対象ファイルが1件もない bldgFolder = {bldgFolder}");
                return false;
            }

            var meshFeatures = new List<Feature>();

            foreach (var filePath in targetFilePathList)
            {
                if (!File.Exists(filePath))
                {
                    App.Logger.Warn($"ファイルが存在しない filePath = {filePath}");
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.Length < 17)
                {
                    App.Logger.Warn($"ファイル名が短い filePath = {filePath}");
                    continue;
                }

                var meshNumerStr = fileName.Substring(9, 8);

                // メッシュフィーチャー作成
                var meahFeature = this.CreateMeshFeature(meshNumerStr);
                if (meahFeature == null)
                {
                    App.Logger.Warn($"メッシュフィーチャーが作成できない filePath = {filePath}, meshNumerStr = {meshNumerStr}");
                    continue;
                }

                meshFeatures.Add(meahFeature);
            }

            if (meshFeatures.Count == 0)
            {
                return false;
            }

            // 作業フォルダ作成（既にあれば何もしない）
            var currentDir = Directory.GetCurrentDirectory();
            var outputFolder = Path.Combine(currentDir, "workspace");
            Directory.CreateDirectory(outputFolder);

            var meshGeojson = JsonConvert.SerializeObject(new FeatureCollection(meshFeatures));
            File.WriteAllText(Path.Combine(outputFolder, "TertiaryMesh.geojson"), meshGeojson);

            return true;
        }

        /// <summary>
        /// メッシュ番号からメッシュフィーチャーを作成します。
        /// </summary>
        /// <param name="meshNumerStr">三次メッシュ番号</param>
        /// <returns>フィーチャー</returns>
        private Feature? CreateMeshFeature(string meshNumerStr)
        {
            var isNotNum = false;
            var numArray = new int[8];

            for (var i = 0; i < 8; i++)
            {
                if (!int.TryParse(meshNumerStr[i].ToString(), out var num))
                {
                    isNotNum = true;
                    break;
                }

                numArray[i] = num;
            }

            if (isNotNum)
            {
                App.Logger.Error($"メッシュ番号にあたる部分が数値ではない meshNumerStr = {meshNumerStr}");
                return null;
            }

            // 緯度（北方向）＝（①②×80+⑤×10+⑦）×30/3600
            var south = ((((numArray[0] * 10) + numArray[1]) * 80) + (numArray[4] * 10) + numArray[6]) * 30d / 3600d;
            var north = south + (30d / 3600d);

            // 経度（東方向）＝（③④×80+⑥×10+⑧）×45/3600+100
            var east = (((((numArray[2] * 10) + numArray[3]) * 80) + (numArray[5] * 10) + numArray[7]) * 45d / 3600d) + 100d;
            var weat = east + (45d / 3600d);

            var coordinates = new List<Position>
            {
                new Position(east, south),
                new Position(east, north),
                new Position(weat, north),
                new Position(weat, south),
                new Position(east, south),
            };

            var ring = new List<LinearRing>
            {
                new LinearRing(coordinates),
            };

            var polygon = new Polygon(ring);
            var properties = new Dictionary<string, dynamic>
            {
                { "meshNum", meshNumerStr },
            };

            return new Feature(polygon, properties);
        }
    }
}
