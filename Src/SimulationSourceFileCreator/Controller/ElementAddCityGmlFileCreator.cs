using System.IO;
using System.Text;
using System.Xml;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// 要素追加済みCityGMLファイルを作成するクラス
    /// </summary>
    internal class ElementAddCityGmlFileCreator
    {
        /// <summary>
        /// 要素追加済みCityGMLファイルを作成します。
        /// </summary>
        /// <param name="xmlDoc">XMLドキュメント</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="buildingNodes">建物情報ノード</param>
        /// <param name="inputCsvFilePath">入力中間CSVファイルパス</param>
        /// <param name="outputCityGMLFilePath">出力CityGMLファイルパス</param>
        /// <param name="setting">要素追加設定</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>成否</returns>
        internal bool CreateGmlFile(XmlDocument xmlDoc, XmlNamespaceManager xmlnsManager, XmlNodeList? buildingNodes, string inputCsvFilePath, string outputCityGMLFilePath, ElementAddSetting setting, CancellationTokenSource cancelToken)
        {
            // 名前空間の追加（sim）
            var simNamespaceUri = setting.SimNamespaceUri;
            var simNamespaceXsd = setting.SimNamespaceXsd;
            (_, simNamespaceUri) = CityGmlFileLoader.CheckAndAddNamespace(xmlDoc, xmlnsManager, "sim", simNamespaceUri, simNamespaceXsd);

            // 名前空間の追加（gen）
            var genNamespaceUri = "http://www.opengis.net/citygml/generics/2.0";
            var genNamespaceXsd = "http://schemas.opengis.net/citygml/generics/2.0/generics.xsd";
            (_, genNamespaceUri) = CityGmlFileLoader.CheckAndAddNamespace(xmlDoc, xmlnsManager, "gen", genNamespaceUri, genNamespaceXsd);

            // データ変換ツールで使用するnamespaceを追加
            this.AddRequiredNamespace(xmlDoc, xmlnsManager);

            if (!File.Exists(inputCsvFilePath))
            {
                App.Logger.Error($"ファイルが存在しない filePath = {inputCsvFilePath}");
                return false;
            }

            string[] headers = [];
            var bldgDataValueDict = new Dictionary<string, string[]>(); // key = bldgId、vaule = データ行

            // 中間CSVファイルからディクショナリを作成
            var isFirst = true;
            using (var sr = new StreamReader(inputCsvFilePath, new UTF8Encoding(true)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (isFirst)
                    {
                        headers = line.Split(',');
                        isFirst = false;
                        continue;
                    }

                    var values = line.Split(',');

                    if (bldgDataValueDict.ContainsKey(values[0]))
                    {
                        // ここには来ないはず
                        App.Logger.Error($"ファイルの内容不備（同じBldgIdが複数記載されている）filePath = {inputCsvFilePath}");
                        return false;
                    }

                    bldgDataValueDict.Add(values[0], values);
                }
            }

            // bldg:Buildingのノード数だけ処理を実行する
            var index = 0;
            foreach (var buildingNode in buildingNodes)
            {
                index++;

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
                    App.Logger.Error($"BldgIdのないデータ index = {index}");
                    return false;
                }

                // 建物IDの要素を追加
                {
                    var idChildElement = xmlDoc.CreateElement("gen:stringAttribute", genNamespaceUri);
                    var idChildAttribute = xmlDoc.CreateAttribute("name");
                    idChildAttribute.Value = "建物ID";
                    idChildElement.Attributes.Append(idChildAttribute);

                    xmlBuildingNode.AppendChild(idChildElement);

                    var idChildElementSub = xmlDoc.CreateElement("gen:value", genNamespaceUri);
                    idChildElementSub.InnerText = bldgId;
                    idChildElement.AppendChild(idChildElementSub);
                }

                // sim要素のベースを追加
                var simBaseNode = xmlBuildingNode.SelectSingleNode(setting.AddParentElement, xmlnsManager);
                if (simBaseNode == null)
                {
                    var tagNames = setting.AddParentElement.Split('/');
                    foreach (var tagName in tagNames)
                    {
                        if (simBaseNode == null)
                        {
                            simBaseNode = xmlDoc.CreateElement(tagName, simNamespaceUri);
                            xmlBuildingNode.AppendChild(simBaseNode);
                        }
                        else
                        {
                            var childNode = xmlDoc.CreateElement(tagName, simNamespaceUri);
                            simBaseNode.AppendChild(childNode);

                            simBaseNode = childNode;
                        }
                    }
                }

                // sim要素の追加
                foreach (var addSetting in setting.AddElements)
                {
                    if (!bldgDataValueDict.TryGetValue(bldgId, out var dataValues))
                    {
                        App.Logger.Error($"中間CSVファイルに対象のBdgIdの行がない bldgId = {bldgId}");
                        return false;
                    }

                    var dataValue = addSetting.DefaultValue.ToString();

                    var columIndex = Array.IndexOf(headers, addSetting.KeyName);
                    if (columIndex != -1)
                    {
                        var tempDataValue = dataValues[columIndex];
                        if (!string.IsNullOrEmpty(tempDataValue))
                        {
                            dataValue = tempDataValue;
                        }
                    }

                    var newnode = xmlDoc.CreateElement(addSetting.TagName, simNamespaceUri);
                    simBaseNode.AppendChild(newnode);
                    newnode.InnerText = dataValue;
                }

                // 地上階数の要素の削除（LOD2で1フロア分の高さが低すぎる場合）
                {
                    if (!bldgDataValueDict.TryGetValue(bldgId, out var dataValues))
                    {
                        App.Logger.Error($"中間CSVファイルに対象のBdgIdの行がない bldgId = {bldgId}");
                        return false;
                    }

                    var columIndex = Array.IndexOf(headers, "LOD2");
                    if (columIndex != -1)
                    {
                        var tempDataValue = dataValues[columIndex];
                        if (bool.TryParse(tempDataValue, out var isLOD2))
                        {
                            if (isLOD2)
                            {
                                var isTakasaValid = false;
                                var takasaNumber = 0d;

                                var takasaNode = xmlBuildingNode.SelectSingleNode("bldg:measuredHeight", xmlnsManager);
                                if (takasaNode != null && !(takasaNode is not XmlNode xmlTakasaNode))
                                {
                                    var takasaValue = xmlTakasaNode.InnerText;
                                    isTakasaValid = double.TryParse(takasaValue, out takasaNumber);
                                    isTakasaValid &= 0 <= takasaNumber && takasaNumber <= 999;
                                }

                                var kaisuNode = xmlBuildingNode.SelectSingleNode("bldg:storeysAboveGround", xmlnsManager);
                                if (kaisuNode != null && !(kaisuNode is not XmlNode xmlKaisuNode))
                                {
                                    var kaisuValue = xmlKaisuNode.InnerText;
                                    var isKaisuValid = double.TryParse(kaisuValue, out var kaisuNumber);
                                    isKaisuValid &= 0 <= kaisuNumber && kaisuNumber <= 999;

                                    if (isTakasaValid && isKaisuValid && 1 < kaisuNumber)
                                    {
                                        if (takasaNumber / kaisuNumber < 1.25d)
                                        {
                                            xmlBuildingNode.RemoveChild(kaisuNode);
                                            App.Logger.Warn($"1フロア分の高さが1.25mに満たない為、地上階数要素（タグ）を削除  bldgId = {bldgId}, 地上階数 = {kaisuNumber}, 高さ = {takasaNumber}, 1フロア分の高さ = {takasaNumber / kaisuNumber}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 欠測値の要素の削除
                foreach (var removeSetting in setting.RemoveElements)
                {
                    var targetNode = xmlBuildingNode.SelectSingleNode(removeSetting.TagName, xmlnsManager);
                    if (targetNode == null)
                    {
                        continue;
                    }

                    if (targetNode is not XmlNode xmlNode)
                    {
                        continue;
                    }

                    var orgValueText = xmlNode.InnerText;

                    if (!double.TryParse(orgValueText, out var orgValueNumber)
                        || orgValueNumber < removeSetting.CheckMinValue
                        || removeSetting.CheckMaxValue < orgValueNumber)
                    {
                        var lastSeparatorIndex = removeSetting.TagName.LastIndexOf('/');

                        if (lastSeparatorIndex == -1)
                        {
                            // bldg:Buildingノードの直下の場合　→　そのままRemove
                            xmlBuildingNode.RemoveChild(xmlNode);
                        }
                        else
                        {
                            // bldg:Buildingノードの直下ではない場合　→　対象のノードの親ノードからRemove
                            var parentTag = removeSetting.TagName.Substring(0, lastSeparatorIndex);
                            var parentNode = xmlBuildingNode.SelectSingleNode(parentTag, xmlnsManager);

                            if (parentNode == null)
                            {
                                continue;
                            }

                            parentNode.RemoveChild(xmlNode);
                        }
                    }
                }

                // 地盤高さの修正（lod0FootPrint、lod0RoofEdgeのZ値）
                {
                    var surfaceMemberNodes = xmlBuildingNode.SelectNodes("bldg:lod1Solid/gml:Solid/gml:exterior/gml:CompositeSurface/gml:surfaceMember", xmlnsManager);
                    (var minHeight, var maxHeight) = CityGmlFileLoader.GetPolygonsZValue(surfaceMemberNodes, xmlnsManager);

                    var lod0RoofEdgeSurfaceMemberNodes = xmlBuildingNode.SelectNodes("bldg:lod0RoofEdge/gml:MultiSurface/gml:surfaceMember", xmlnsManager);
                    (var edgeMinHeight, var edgeMaxHeight) = CityGmlFileLoader.GetPolygonsZValue(lod0RoofEdgeSurfaceMemberNodes, xmlnsManager);

                    var lod0FootPrintSurfaceMemberNodes = xmlBuildingNode.SelectNodes("bldg:lod0FootPrint/gml:MultiSurface/gml:surfaceMember", xmlnsManager);
                    (var printMinHeight, var printMaxHeight) = CityGmlFileLoader.GetPolygonsZValue(lod0FootPrintSurfaceMemberNodes, xmlnsManager);

                    // bldg:lod0RoofEdge のZ値がすべて0の場合　→　bldg:lod0RoofEdge のZ値を bldg:lod1Solid の"最大"Z値 に修正
                    if (!double.IsNaN(edgeMinHeight) && !double.IsNaN(edgeMaxHeight) && edgeMinHeight == 0d && edgeMaxHeight == 0d)
                    {
                        CityGmlFileLoader.SetPolygonsZValue(lod0RoofEdgeSurfaceMemberNodes, xmlnsManager, maxHeight);
                    }

                    // bldg:lod0FootPrint のZ値がすべて0の場合　→　bldg:lod0FootPrint のZ値を bldg:lod1Solid の"最小"Z値 に修正
                    if (!double.IsNaN(printMinHeight) && !double.IsNaN(printMaxHeight) && printMinHeight == 0d && printMaxHeight == 0d)
                    {
                        CityGmlFileLoader.SetPolygonsZValue(lod0FootPrintSurfaceMemberNodes, xmlnsManager, minHeight);
                    }
                }
            }

            var xmlSettings = new XmlWriterSettings
            {
                Indent = true,         // インデントを有効にする
                IndentChars = "\t",    // タブ文字を使用する
                NewLineChars = "\r\n", // 改行コードを設定
            };

            using (XmlWriter writer = XmlWriter.Create(outputCityGMLFilePath, xmlSettings))
            {
                xmlDoc.Save(writer);
            }

            return true;
        }

        /// <summary>
        /// データ変換ツールで使用するnamespaceを追加します。（既にある場合は何もしません）
        /// </summary>
        /// <param name="xmlDoc">XMLドキュメント</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        private void AddRequiredNamespace(XmlDocument xmlDoc, XmlNamespaceManager xmlnsManager)
        {
            // core
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "core", "http://www.opengis.net/citygml/2.0", "http://schemas.opengis.net/citygml/2.0/cityGMLBase.xsd");

            // bldg
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "bldg", "http://www.opengis.net/citygml/building/2.0", "http://schemas.opengis.net/citygml/building/2.0/building.xsd");

            // gml
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "gml", "http://www.opengis.net/gml", "http://schemas.opengis.net/gml/3.1.1/base/gml.xsd");

            // app
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "app", "http://www.opengis.net/citygml/appearance/2.0", "http://schemas.opengis.net/citygml/appearance/2.0/appearance.xsd");

            // uro
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "uro", "https://www.geospatial.jp/iur/uro/3.2", "../../schemas/iur/uro/3.2/urbanObject.xsd");

            // tran
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "tran", "http://www.opengis.net/citygml/transportation/2.0", "http://schemas.opengis.net/citygml/transportation/2.0/transportation.xsd");

            // dem
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "dem", "http://www.opengis.net/citygml/relief/2.0", string.Empty);

            // xAL
            this.CallCheckAndAddNamespace(xmlDoc, xmlnsManager, "xAL", "urn:oasis:names:tc:ciq:xsdschema:xAL:2.0", string.Empty);
        }

        /// <summary>
        /// 名前空間マネージャに指定のプレフィックスを追加します。（既にある場合は何もしません）
        /// </summary>
        /// <param name="xmlDoc">XMLドキュメント</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="prefix">プレフィックス</param>
        /// <param name="namespaceUri">uri</param>
        /// <param name="namespaceXsd">xsd</param>
        private void CallCheckAndAddNamespace(XmlDocument xmlDoc, XmlNamespaceManager xmlnsManager, string prefix, string namespaceUri, string namespaceXsd)
        {
            var (added, _) = CityGmlFileLoader.CheckAndAddNamespace(xmlDoc, xmlnsManager, prefix, namespaceUri, namespaceXsd);
            if (added)
            {
                App.Logger.Info($"データ変換ツール「GeneFile/plateau_conv.exe」で使用するnamespaceを追加 prefix = {prefix}");
            }
        }
    }
}
