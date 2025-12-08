using System.IO;
using System.Text;
using System.Xml;
using BAMCIS.GeoJSON;
using CesiumLanguageWriter;
using GeoidHeightsDotNet;
using Newtonsoft.Json;
using SimulationSourceFileCreator.Model;
using static SimulationSourceFileCreator.Model.BldgPolygon;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// 条件設定支援ツールの実行に必要なファイルを作成するクラス
    /// </summary>
    internal class ForSupportToolFileCreator
    {
        /// <summary>
        ///  建物GeoJsonファイル（条件設定支援ツール表示用建物データ）を作成します。
        /// </summary>
        /// <param name="buildingNodes">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="inputFilePath">CityGmlファイルパス（ログ出力にのみ使用）</param>
        /// <param name="outputFilePath">出力ファイルパス</param>
        /// <param name="setting">要素追加設定</param>
        /// <param name="bldgAboveFloorNumDict">建物IDと地上階数のディクショナリ（key = bldgId、vaule = 地上階数）</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>成否</returns>
        internal bool CreateBuldingGeojsonFile(XmlNodeList? buildingNodes, XmlNamespaceManager xmlnsManager, string inputFilePath, string outputFilePath, ElementAddSettting setting, Dictionary<string, int> bldgAboveFloorNumDict, CancellationTokenSource cancelToken)
        {
            var features = new List<Feature>();

            // bldg:Buildingのノード数だけ処理を実行する
            foreach (var buildingNode in buildingNodes)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return false;
                }

                if (buildingNode is not XmlNode xmlBuildingNode)
                {
                    continue;
                }

                var bldgId = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, setting.BldgId);
                if (string.IsNullOrEmpty(bldgId))
                {
                    App.Logger.Error($"BldgIdのないデータ filePath = {inputFilePath}");
                    return false;
                }

                var lod0RoofEdgePolygons = new List<Polygon>();
                var fireproofStructureType = string.Empty;

                foreach (var childNode in xmlBuildingNode.ChildNodes)
                {
                    if (childNode is not XmlNode xmlChildNode)
                    {
                        continue;
                    }

                    if (xmlChildNode.NodeType == XmlNodeType.Element)
                    {
                        switch (xmlChildNode.Name)
                        {
                            case "bldg:storeysAboveGround":
                                // 地上階数はデータ変換ツールの結果から取得する為、ここでは取得しない
                                break;

                            case "bldg:lod0RoofEdge":
                            case "bldg:lod0FootPrint":
                                var surfaceMemberNodes = xmlChildNode.SelectNodes("gml:MultiSurface/gml:surfaceMember", xmlnsManager);

                                (var polygons, var minHeightPos) = CityGmlFileLoader.GetPolygons(surfaceMemberNodes, xmlnsManager);

                                if (polygons == null || polygons.Count == 0)
                                {
                                    continue;
                                }

                                if (polygons.Count != 1)
                                {
                                    // 念のため情報出力
                                    App.Logger.Info($"lod0RoofEdgeまたはlod0FootPrintの形状が複数 filePath = {inputFilePath}, bldgId = {bldgId}");
                                }

                                foreach (var polygon in polygons)
                                {
                                    var coordinates = new List<Position>();
                                    foreach (var pos in polygon.Exterior)
                                    {
                                        coordinates.Add(new Position(pos.Longitude, pos.Latitude));
                                    }

                                    var ring = new List<LinearRing>
                                    {
                                        new LinearRing(coordinates),
                                    };

                                    lod0RoofEdgePolygons.Add(new Polygon(ring));
                                }

                                break;

                            case "sim:cityFireSimulation":
                                var fireproofStructureCityFireSimulationTypeNode = xmlChildNode.SelectSingleNode("sim:CityFireSimulation/sim:fireproofStructureCityFireSimulationType", xmlnsManager);
                                if (fireproofStructureCityFireSimulationTypeNode != null)
                                {
                                    fireproofStructureType = fireproofStructureCityFireSimulationTypeNode.InnerText;
                                }

                                break;

                            default:
                                break;
                        }
                    }
                }

                if (lod0RoofEdgePolygons.Count == 0)
                {
                    App.Logger.Warn($"ポリゴンのないデータ filePath = {inputFilePath}, bldgId = {bldgId}");
                    continue;
                }

                var multiPolygon = new MultiPolygon(lod0RoofEdgePolygons);

                // データ変換ツールの結果から地上階数を取得する
                bldgAboveFloorNumDict.TryGetValue(bldgId, out var storeysAboveGround);

                var properties = new Dictionary<string, dynamic>
                {
                    { "bldgId", bldgId },
                    { "storeysAboveGround", storeysAboveGround },
                    { "fireproofStructureType", fireproofStructureType },
                };

                features.Add(new Feature(multiPolygon, properties));
            }

            if (features.Count == 0)
            {
                App.Logger.Error($"作成できた建物フィーチャーがない filePath = {inputFilePath}");
                return false;
            }

            var buldingGeojson = JsonConvert.SerializeObject(new FeatureCollection(features));
            File.WriteAllText(outputFilePath, buldingGeojson);

            return true;
        }

        /// <summary>
        /// 建物CZMLファイル作成（GISデータ変換ツール変換用建物データ）を作成します。
        /// </summary>
        /// <param name="buildingNodes">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="inputFilePath">CityGmlファイルパス（ログ出力にのみ使用）</param>
        /// <param name="outputFilePath">出力ファイルパス</param>
        /// <param name="setting">要素追加設定</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>成否</returns>
        internal bool CreateBuldingCzmlFile(XmlNodeList? buildingNodes, XmlNamespaceManager xmlnsManager, string inputFilePath, string outputFilePath, ElementAddSettting setting, CancellationTokenSource cancelToken)
        {
            using (var stringWriter = new StringWriter())
            {
                var output = new CesiumOutputStream(stringWriter);
                var csw = new CesiumStreamWriter();

                using (var packet = csw.OpenPacket(output))
                {
                    packet.WriteId("document");
                    packet.WriteName("Bulding");
                    packet.WriteVersion("1.0");
                }

                // bldg:Buildingのノード数だけ処理を実行する
                foreach (var buildingNode in buildingNodes)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    if (buildingNode is not XmlNode xmlBuildingNode)
                    {
                        continue;
                    }

                    var bldgId = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, setting.BldgId);
                    if (string.IsNullOrEmpty(bldgId))
                    {
                        App.Logger.Error($"BldgIdのないデータ filePath = {inputFilePath}");
                        return false;
                    }

                    var lod1HeightMinPos = BldgPolygonPos.NaN;
                    var lod2HeightMinPos = BldgPolygonPos.NaN;
                    var lod1SolidPolygons = new List<BldgPolygon>();
                    var lod2SolidPolygons = new List<BldgPolygon>();

                    foreach (var childNode in xmlBuildingNode.ChildNodes)
                    {
                        if (childNode is not XmlNode xmlChildNode)
                        {
                            continue;
                        }

                        if (xmlChildNode.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlChildNode.Name)
                            {
                                case "bldg:lod1Solid":
                                    {
                                        var surfaceMemberNodes = xmlChildNode.SelectNodes("gml:Solid/gml:exterior/gml:CompositeSurface/gml:surfaceMember", xmlnsManager);
                                        (var polygons, var minHeightPos) = CityGmlFileLoader.GetPolygons(surfaceMemberNodes, xmlnsManager);

                                        if (polygons == null || polygons.Count == 0 || minHeightPos == null)
                                        {
                                            continue;
                                        }

                                        lod1SolidPolygons.AddRange(polygons);

                                        if (double.IsNaN(lod1HeightMinPos.Height) || minHeightPos?.Height < lod1HeightMinPos.Height)
                                        {
                                            lod1HeightMinPos = minHeightPos ?? BldgPolygonPos.NaN;
                                        }
                                    }

                                    break;

                                case "bldg:boundedBy":
                                    foreach (var boundedByNode in xmlChildNode.ChildNodes)
                                    {
                                        if (boundedByNode is not XmlNode xmlboundedByNode)
                                        {
                                            continue;
                                        }

                                        if (xmlboundedByNode.NodeType == XmlNodeType.Element)
                                        {
                                            XmlNodeList? surfaceMemberNodes = null;
                                            switch (xmlboundedByNode.Name)
                                            {
                                                case "bldg:GroundSurface":
                                                case "bldg:RoofSurface":
                                                case "bldg:WallSurface":
                                                    surfaceMemberNodes = xmlboundedByNode.SelectNodes("bldg:lod2MultiSurface/gml:MultiSurface/gml:surfaceMember", xmlnsManager);
                                                    break;

                                                default:
                                                    break;
                                            }

                                            (var polygons, var minHeightPos) = CityGmlFileLoader.GetPolygons(surfaceMemberNodes, xmlnsManager);

                                            if (polygons == null || polygons.Count == 0 || minHeightPos == null)
                                            {
                                                continue;
                                            }

                                            lod2SolidPolygons.AddRange(polygons);

                                            if (double.IsNaN(lod2HeightMinPos.Height) || minHeightPos?.Height < lod2HeightMinPos.Height)
                                            {
                                                lod2HeightMinPos = minHeightPos ?? BldgPolygonPos.NaN;
                                            }
                                        }
                                    }

                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    if (lod1SolidPolygons.Count == 0 && lod2SolidPolygons.Count == 0)
                    {
                        App.Logger.Warn($"ポリゴンのないデータ filePath = {inputFilePath}, bldgId = {bldgId}");
                        continue;
                    }

                    var targetPolygons = lod1SolidPolygons;
                    var targetHeight = lod1HeightMinPos;
                    if (lod2SolidPolygons.Count != 0)
                    {
                        targetPolygons = lod2SolidPolygons;
                        targetHeight = lod2HeightMinPos;
                    }

                    var geoidHeight = GeoidHeights.undulation(targetHeight.Latitude, targetHeight.Longitude);

                    stringWriter.WriteLine(string.Empty); // 改行を入れる

                    using (var packet = csw.OpenPacket(output))
                    {
                        packet.WriteId($"{bldgId}");

                        // オリジナルプロパティを追加
                        stringWriter.Write($",\"geoidHeight\":{geoidHeight}");
                    }

                    foreach (var targetPolygon in targetPolygons)
                    {
                        stringWriter.WriteLine(string.Empty); // 改行を入れる

                        using (var packet = csw.OpenPacket(output))
                        {
                            packet.WriteParent($"{bldgId}");

                            using (var polygon = packet.OpenPolygonProperty())
                            {
                                using (var material = polygon.OpenMaterialProperty())
                                {
                                    using (var solidColor = material.OpenSolidColorProperty())
                                    {
                                        using (var color = solidColor.OpenColorProperty())
                                        {
                                            color.WriteReference($"{bldgId}#polygon.material.color");
                                        }
                                    }
                                }

                                using (var pos = polygon.OpenPositionsProperty())
                                {
                                    var geoidHeightSolidPolygon = new List<Cartographic>();
                                    foreach (var exteriorPos in targetPolygon.Exterior)
                                    {
                                        geoidHeightSolidPolygon.Add(new Cartographic(exteriorPos.Longitude, exteriorPos.Latitude, exteriorPos.Height + geoidHeight));
                                    }

                                    pos.WriteCartographicDegrees(geoidHeightSolidPolygon);
                                }

                                if (targetPolygon.Holes.Count != 0)
                                {
                                    using (var holes = polygon.OpenHolesProperty())
                                    {
                                        var polygonHoles = new List<List<Cartographic>>();
                                        foreach (var hole in targetPolygon.Holes)
                                        {
                                            var holePolygon = new List<Cartographic>();
                                            foreach (var interiorPos in hole)
                                            {
                                                holePolygon.Add(new Cartographic(interiorPos.Longitude, interiorPos.Latitude, interiorPos.Height + geoidHeight));
                                            }

                                            polygonHoles.Add(holePolygon);
                                        }

                                        holes.WriteCartographicDegrees(polygonHoles);
                                    }
                                }

                                polygon.WritePerPositionHeightProperty(true);
                            }
                        }
                    }
                }

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
