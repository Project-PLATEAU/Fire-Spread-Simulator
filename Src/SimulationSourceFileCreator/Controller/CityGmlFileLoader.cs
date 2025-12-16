using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using SimulationSourceFileCreator.Model;
using static SimulationSourceFileCreator.Model.BldgPolygon;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// CityGMLファイルの操作クラス
    /// </summary>
    internal class CityGmlFileLoader
    {
        /// <summary>
        /// CityGmlファイルを読み込みます。
        /// </summary>
        /// <param name="filePath">CityGmlファイルパス</param>
        /// <param name="xmlDoc">XMLドキュメント</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="buildingNodes">建物情報ノード</param>
        /// <param name="meshNumer">メッシュ番号</param>
        /// <returns>成否</returns>
        internal static bool Load(string filePath, out XmlDocument xmlDoc, out XmlNamespaceManager xmlnsManager, out XmlNodeList? buildingNodes, out string meshNumer)
        {
            // 初期化
            xmlDoc = new XmlDocument();
            xmlnsManager = new XmlNamespaceManager(new NameTable());
            buildingNodes = null;
            meshNumer = string.Empty;

            // チェック
            if (!File.Exists(filePath))
            {
                App.Logger.Error($"ファイルが存在しない filePath = {filePath}");
                return false;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Length < 8)
            {
                App.Logger.Error($"メッシュ番号（先頭8文字）が取得できない filePath = {filePath}");
                return false;
            }

            // ファイル名の先頭8文字がメッシュ番号（[メッシュ番号]_bldg_6697_op.gml）
            meshNumer = fileName.Substring(0, 8);

            if (!Regex.IsMatch(meshNumer, @"^[0-9]+$"))
            {
                App.Logger.Error($"メッシュ番号（先頭8文字）が数字ではない filePath = {filePath}");
                return false;
            }

            // 名前空間の収集
            var nameSpaceDict = new Dictionary<string, string>();

            try
            {
                using (var reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            // 要素が見つかったら、全ての属性と値を取得
                            if (reader.Name == "core:CityModel")
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    var delimiterIndex = reader.Name.IndexOf(':');
                                    var namespaceName = reader.Name.Substring(delimiterIndex + 1);

                                    nameSpaceDict.Add(namespaceName, reader.Value);
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error($"CityGMLファイルの読み込みに失敗 filePath = {filePath}", ex);
                return false;
            }

            // 名前空間の追加
            foreach (var kvp in nameSpaceDict)
            {
                xmlnsManager.AddNamespace(kvp.Key, kvp.Value);
            }

            // 読み込み
            xmlDoc.Load(filePath);

            try
            {
                // bldg:Buildingのノードを取得
                var masterPass = "core:CityModel/core:cityObjectMember/bldg:Building";
                buildingNodes = xmlDoc.SelectNodes(masterPass, xmlnsManager);

                if (buildingNodes == null || buildingNodes.Count == 0)
                {
                    App.Logger.Error($"bldg:Buildingノードが存在しない filePath = {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error($"bldg:Buildingノードの取得に失敗 filePath = {filePath}", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 名前空間マネージャに要素追加設定ファイルのすべての取得要素設定のプレフィックスがあるかどうかをチェックします。
        /// </summary>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="setting">要素追加設定</param>
        /// <returns>成否</returns>
        internal static bool CheckGetSettingPrefix(XmlNamespaceManager xmlnsManager, ElementAddSetting setting)
        {
            var isCheckOK = true;
            isCheckOK &= CheckPrefix(xmlnsManager, setting.BldgId);

            foreach (var getSetting in setting.GetElements)
            {
                isCheckOK &= CheckPrefix(xmlnsManager, getSetting);
            }

            isCheckOK &= CheckPrefix(xmlnsManager, setting.KOZO.Taika);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.KOZO.Tatemono);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.KOZO.Kaisu);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.KOZO.Nobeyuka);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.KOZO.Kenchiku);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.MOKU.Tatemono);
            isCheckOK &= CheckPrefix(xmlnsManager, setting.YOTO.Mokuteki);

            return isCheckOK;
        }

        /// <summary>
        /// XMLドキュメントと名前空間マネージャに指定の名前空間を追加します。（既にある場合は何もしません）
        /// </summary>
        /// <param name="xmlDoc">XMLドキュメント</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="prefix">プレフィックス</param>
        /// <param name="namespaceUri">uri</param>
        /// <param name="namespaceXsd">xsd</param>
        /// <returns>追加したかどうか、uri（既にプレフィックスがある場合は既存のuri）</returns>
        internal static (bool added, string uri) CheckAndAddNamespace(XmlDocument xmlDoc, XmlNamespaceManager xmlnsManager, string prefix, string namespaceUri, string namespaceXsd)
        {
            // 指定の名前空間が存在するかを確認する。
            var uri = xmlDoc.DocumentElement.GetAttribute($"xmlns:{prefix}");

            if (!string.IsNullOrEmpty(uri))
            {
                // 存在する場合　→　既存のuriを返す
                return (false, uri);
            }

            // 存在しない場合　→　追加
            xmlDoc.DocumentElement.SetAttribute($"xmlns:{prefix}", namespaceUri);

            // 順番を整える為（一番最後にする為）に一旦削除して追加
            var orgLocationPass = xmlDoc.DocumentElement.GetAttribute("xsi:schemaLocation");
            xmlDoc.DocumentElement.RemoveAttribute("xsi:schemaLocation");

            // prefixはないがLocationPassは有る場合がある為、無い場合にのみ追加するようにする
            if (string.IsNullOrEmpty(namespaceXsd) || orgLocationPass.Contains(namespaceUri))
            {
                // 有る場合　→　そのまま追加
                xmlDoc.DocumentElement.SetAttribute("xsi:schemaLocation", $"{orgLocationPass}");
            }
            else
            {
                // 無い場合　→　追記して追加
                xmlDoc.DocumentElement.SetAttribute("xsi:schemaLocation", $"{orgLocationPass} {namespaceUri} {namespaceXsd}");
            }

            xmlnsManager.AddNamespace(prefix, namespaceUri);

            return (true, namespaceUri);
        }

        /// <summary>
        /// surfaceノードから建物ポリゴンを取得します。
        /// </summary>
        /// <param name="surfaceMemberNodes">surfaceノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <returns>建物ポリゴンのリストとその最小高さ</returns>
        internal static (List<BldgPolygon>? polygons, BldgPolygonPos? minHeightPos) GetPolygons(XmlNodeList? surfaceMemberNodes, XmlNamespaceManager xmlnsManager)
        {
            var polygons = new List<BldgPolygon>();
            var minHeightPos = BldgPolygonPos.NaN;

            if (surfaceMemberNodes == null)
            {
                return (null, null);
            }

            foreach (var surfaceMemberNode in surfaceMemberNodes)
            {
                if (surfaceMemberNode is not XmlNode xmlSurfaceMemberNode)
                {
                    continue;
                }

                var polygon = new BldgPolygon();
                var exteriorPos = new List<BldgPolygonPos>();
                var interiors = new List<List<BldgPolygonPos>>();

                var exteriorPosListNode = xmlSurfaceMemberNode.SelectSingleNode("gml:Polygon/gml:exterior/gml:LinearRing/gml:posList", xmlnsManager);
                if (exteriorPosListNode != null)
                {
                    var pos = exteriorPosListNode.InnerText.Split(" ");
                    for (var i = 0; i < pos.Length; i++)
                    {
                        if (i % 3 == 2)
                        {
                            if (!double.TryParse(pos[i - 2], out var lat) || !double.TryParse(pos[i - 1], out var lng) || !double.TryParse(pos[i], out var height))
                            {
                                App.Logger.Warn($"位置の変換エラー lat lon height = {pos[i - 2]} {pos[i - 1]} {pos[i]}");
                                return (null, null);
                            }

                            var polygonPos = new BldgPolygonPos(lng, lat, height);

                            if (double.IsNaN(minHeightPos.Height) || height < minHeightPos.Height)
                            {
                                minHeightPos = polygonPos;
                            }

                            exteriorPos.Add(polygonPos);
                        }
                    }
                }

                if (0 < exteriorPos.Count)
                {
                    polygon.Exterior = exteriorPos;
                }

                var interiorPosListNodes = xmlSurfaceMemberNode.SelectNodes("gml:Polygon/gml:interior/gml:LinearRing/gml:posList", xmlnsManager);
                if (interiorPosListNodes != null)
                {
                    foreach (var interiorPosListNode in interiorPosListNodes)
                    {
                        if (interiorPosListNode is not XmlNode xmlInteriorPosListNode)
                        {
                            continue;
                        }

                        var interiorPos = new List<BldgPolygonPos>();

                        var pos = xmlInteriorPosListNode.InnerText.Split(" ");
                        for (var i = 0; i < pos.Length; i++)
                        {
                            if (i % 3 == 2)
                            {
                                if (!double.TryParse(pos[i - 2], out var lat) || !double.TryParse(pos[i - 1], out var lng) || !double.TryParse(pos[i], out var height))
                                {
                                    App.Logger.Warn($"位置の変換エラー lat lon height = {pos[i - 2]} {pos[i - 1]} {pos[i]}");
                                    return (null, null);
                                }

                                var polygonPos = new BldgPolygonPos(lng, lat, height);
                                interiorPos.Add(polygonPos);
                            }
                        }

                        if (0 < interiorPos.Count)
                        {
                            interiors.Add(interiorPos);
                        }
                    }

                    polygon.Holes = interiors;
                }

                polygons.Add(polygon);
            }

            return (polygons, minHeightPos);
        }

        /// <summary>
        /// surfaceノードから最小高さ、最大高さを取得します。
        /// </summary>
        /// <param name="surfaceMemberNodes">surfaceノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <returns>最小高さ、最大高さ</returns>
        internal static (double minHeight, double maxHeight) GetPolygonsZValue(XmlNodeList? surfaceMemberNodes, XmlNamespaceManager xmlnsManager)
        {
            if (surfaceMemberNodes == null)
            {
                return (double.NaN, double.NaN);
            }

            var minHeight = double.NaN;
            var maxHeight = double.NaN;

            foreach (var surfaceMemberNode in surfaceMemberNodes)
            {
                if (surfaceMemberNode is not XmlNode xmlSurfaceMemberNode)
                {
                    continue;
                }

                var posListNode = xmlSurfaceMemberNode.SelectSingleNode("gml:Polygon/gml:exterior/gml:LinearRing/gml:posList", xmlnsManager);
                if (posListNode != null)
                {
                    var pos = posListNode.InnerText.Split(" ");
                    for (var i = 0; i < pos.Length; i++)
                    {
                        if (i % 3 == 2)
                        {
                            if (!double.TryParse(pos[i], out var height))
                            {
                                App.Logger.Warn($"位置の変換エラー height = {pos[i]}");
                                break;
                            }

                            if (double.IsNaN(minHeight) || height < minHeight)
                            {
                                minHeight = height;
                            }

                            if (double.IsNaN(maxHeight) || maxHeight < height)
                            {
                                maxHeight = height;
                            }
                        }
                    }
                }
            }

            return (minHeight, maxHeight);
        }

        /// <summary>
        /// surfaceノードに高さを設定します。
        /// </summary>
        /// <param name="surfaceMemberNodes">surfaceノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="height">高さ</param>
        internal static void SetPolygonsZValue(XmlNodeList? surfaceMemberNodes, XmlNamespaceManager xmlnsManager, double height)
        {
            if (surfaceMemberNodes == null)
            {
                return;
            }

            foreach (var surfaceMemberNode in surfaceMemberNodes)
            {
                if (surfaceMemberNode is not XmlNode xmlSurfaceMemberNode)
                {
                    continue;
                }

                var posListNode = xmlSurfaceMemberNode.SelectSingleNode("gml:Polygon/gml:exterior/gml:LinearRing/gml:posList", xmlnsManager);
                if (posListNode != null)
                {
                    var pos = posListNode.InnerText.Split(' ');
                    for (var i = 0; i < pos.Length; i++)
                    {
                        if (i % 3 == 2)
                        {
                            // Z値のみ修正
                            pos[i] = height.ToString();
                        }
                    }

                    posListNode.InnerText = string.Join(' ', pos);
                }
            }
        }

        /// <summary>
        /// 取得要素設定から値を取得します。
        /// </summary>
        /// <param name="xmlBuildingNode">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="getSetting">取得要素設定</param>
        /// <returns>取得結果</returns>
        internal static string GetTagValue(XmlNode xmlBuildingNode, XmlNamespaceManager xmlnsManager, GetElement? getSetting)
        {
            if (getSetting == null)
            {
                return string.Empty;
            }

            var getValue = string.Empty;

            switch (getSetting.TargetType)
            {
                case 0:
                    {
                        if (!string.IsNullOrEmpty(getSetting.TagName))
                        {
                            var node = xmlBuildingNode.SelectSingleNode(getSetting.TagName, xmlnsManager);
                            getValue = node?.Attributes[getSetting.AttributeName]?.Value;
                        }
                        else
                        {
                            getValue = xmlBuildingNode.Attributes[getSetting.AttributeName]?.Value;
                        }

                        break;
                    }

                case 1:
                    {
                        var nodes = xmlBuildingNode.SelectNodes(getSetting.TagName, xmlnsManager);
                        foreach (var node in nodes)
                        {
                            if (node is not XmlNode xmlNode)
                            {
                                continue;
                            }

                            var attValue = xmlNode.Attributes[getSetting.AttributeName]?.Value;
                            if (string.IsNullOrEmpty(attValue))
                            {
                                continue;
                            }

                            if (attValue != getSetting.AttributeValue)
                            {
                                continue;
                            }

                            getValue = xmlNode.SelectSingleNode("gen:value", xmlnsManager)?.InnerText;
                        }

                        break;
                    }

                case 2:
                    {
                        getValue = xmlBuildingNode.SelectSingleNode(getSetting.TagName, xmlnsManager)?.InnerText;
                        break;
                    }

                case 3:
                    {
                        getValue = getSetting.FixedValue;
                        break;
                    }

                default:
                    break;
            }

            return getValue ?? string.Empty;
        }

        /// <summary>
        /// 名前空間マネージャに取得要素設定のプレフィックスがあるかどうかをチェックします。
        /// </summary>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="getSetting">取得要素設定</param>
        /// <returns>true：有り、false：無し</returns>
        private static bool CheckPrefix(XmlNamespaceManager xmlnsManager, GetElement? getSetting)
        {
            if (getSetting == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(getSetting.TagName))
            {
                return true;
            }

            var index = getSetting.TagName.IndexOf(':');

            if (index < 0)
            {
                return true;
            }

            if (index == 0)
            {
                return false;
            }

            var prefix = getSetting.TagName.Substring(0, index);
            var uri = xmlnsManager.LookupNamespace(prefix);

            if (string.IsNullOrEmpty(uri))
            {
                return false;
            }

            return true;
        }
    }
}
