using System.IO;
using System.Text;
using System.Xml;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// 中間CSVファイルを作成するクラス
    /// </summary>
    internal class IntermediateCsvFileCreator
    {
        /// <summary>
        /// 中間CSVファイルを作成します。
        /// </summary>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <param name="buildingNodes">建物情報ノード</param>
        /// <param name="outputCsvFilePath">出力中間CSVファイルパス</param>
        /// <param name="selectedFireproofStructureType">防火構造</param>
        /// <param name="setting">要素追加設定</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        /// <returns>成否</returns>
        internal bool CreateCSVFile(XmlNamespaceManager xmlnsManager, XmlNodeList? buildingNodes, string outputCsvFilePath, string selectedFireproofStructureType, ElementAddSetting setting, CancellationTokenSource cancelToken)
        {
            using (StreamWriter writer = new StreamWriter(outputCsvFilePath, false, new UTF8Encoding(true)))
            {
                // ヘッダー項目の収集
                var headers = new List<string>
                {
                    // 取得要素設定（データ補完対象項目）のヘッダー
                    "KOZO",
                    setting.KOZO.Taika.KeyName,
                    setting.KOZO.Tatemono.KeyName,
                    setting.KOZO.Kaisu.KeyName,
                    setting.KOZO.Nobeyuka.KeyName,
                    setting.KOZO.Kenchiku.KeyName,
                    "MOKU",
                    setting.MOKU.Tatemono.KeyName,
                    "YOTO",
                    setting.YOTO.Mokuteki.KeyName,
                };

                // 取得要素設定のヘッダー
                headers.AddRange(setting.GetElements.Select(e => e.KeyName));

                // ヘッダー行の出力
                writer.WriteLine($"NAME,{string.Join(",", headers)},LOD2,LOD3");

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

                    var dataValues = new List<string>();

                    var kozoValue = string.Empty;

                    // 取得要素設定（データ補完対象項目）のデータ取得
                    // KOZO
                    var sptKOZO = this.SupplementKOZO(setting.KOZO, selectedFireproofStructureType, xmlBuildingNode, xmlnsManager);
                    dataValues.AddRange(sptKOZO);
                    kozoValue = sptKOZO[0];

                    // MOKU
                    var sptMOKU = this.SupplementMOKU(setting.MOKU, kozoValue, xmlBuildingNode, xmlnsManager);
                    dataValues.AddRange(sptMOKU);

                    // YOTO
                    var sptYOTO = this.SupplementYOTO(setting.YOTO, xmlBuildingNode, xmlnsManager);
                    dataValues.AddRange(sptYOTO);

                    // 取得要素設定のデータ取得
                    foreach (var getSetting in setting.GetElements)
                    {
                        var getValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, getSetting);
                        dataValues.Add(getValue);
                    }

                    var lod2SolidNodes = xmlBuildingNode.SelectNodes("bldg:lod2Solid", xmlnsManager);
                    var isLod2 = lod2SolidNodes != null && lod2SolidNodes.Count != 0;
                    dataValues.Add(isLod2 ? "true" : string.Empty);

                    var lod3SolidNodes = xmlBuildingNode.SelectNodes("bldg:lod3Solid", xmlnsManager);
                    var isLod3 = lod3SolidNodes != null && lod3SolidNodes.Count != 0;
                    dataValues.Add(isLod3 ? "true" : string.Empty);

                    // データ行の出力
                    writer.WriteLine($"{bldgId},{string.Join(',', dataValues)}");
                }
            }

            return true;
        }

        /// <summary>
        /// KOZOのデータ補完を行います。
        /// </summary>
        /// <param name="kozoSetting">KOZOのデータ補完設定</param>
        /// <param name="selectedFireproofStructureType">防火構造</param>
        /// <param name="xmlBuildingNode">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <returns>
        /// 補完結果と補完に利用した値の配列<br/>
        /// [KOZO][耐火構造][建物構造][地上階数][延床面積][建築面積]
        /// </returns>
        private string[] SupplementKOZO(ElementAddSettingSupplementItem kozoSetting, string selectedFireproofStructureType, XmlNode xmlBuildingNode, XmlNamespaceManager xmlnsManager)
        {
            var taikaValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, kozoSetting.Taika);
            var tatemonoValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, kozoSetting.Tatemono);

            var kaisuValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, kozoSetting.Kaisu);
            var isKaisuValid = double.TryParse(kaisuValue, out var kaisuNumber);
            isKaisuValid &= 0 <= kaisuNumber && kaisuNumber <= 999;

            var nobeyukaValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, kozoSetting.Nobeyuka);
            var isNobeyukaValid = double.TryParse(nobeyukaValue, out var nobeyukaNumber);
            isNobeyukaValid &= 0 <= nobeyukaNumber;

            var kenchikuValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, kozoSetting.Kenchiku);
            var isKenchikuValid = double.TryParse(kenchikuValue, out var kenchikuNumber);
            isKenchikuValid &= 0 <= kenchikuNumber;

            var result = new string[6];
            result[1] = taikaValue;
            result[2] = tatemonoValue;
            result[3] = kaisuValue;
            result[4] = nobeyukaValue;
            result[5] = kenchikuValue;

            /* 1.耐火構造による判断 */
            switch (taikaValue)
            {
                case "1001":
                    result[0] = "1";
                    return result;
                case "1002":
                    result[0] = "2";
                    return result;

                case "1003":
                case "1011":
                default:
                    // 2.建物構造による判断へ
                    break;
            }

            /* 2.建物構造による判断 */
            switch (tatemonoValue)
            {
                case "601":
                    result[0] = selectedFireproofStructureType;
                    return result;
                case "602":
                case "603":
                case "604":
                    result[0] = "1";
                    return result;
                case "605":
                    result[0] = "2";
                    return result;
                case "606":
                    result[0] = "1";
                    return result;
                case "610":
                    result[0] = "2";
                    return result;

                case "611":
                default:
                    // 3.地上階数・延床面積による判断へ
                    break;
            }

            /* 3.地上階数・延床面積による判断 */

            // 地上階数が4以上または延床面積が3000㎡を超える
            if ((isKaisuValid && 3d < kaisuNumber) || (isNobeyukaValid && 3000d < nobeyukaNumber))
            {
                result[0] = "1";
                return result;
            }

            // 地上階数が3以下で延床面積が3000㎡以下
            if ((isKaisuValid && kaisuNumber <= 3d) && (isNobeyukaValid && nobeyukaNumber <= 3000d))
            {
                result[0] = selectedFireproofStructureType;
                return result;
            }

            // 地上階数が不明
            if (!isKaisuValid)
            {
                result[0] = selectedFireproofStructureType;
                return result;
            }

            // 地上階数が3以下で延床面積が不明　→　4.建築面積・地上階数による判断へ

            /* 4.建築面積・地上階数による判断 */

            // 建築面積×地上階数の値が3000㎡を超える
            if (isKenchikuValid && (3000d < kenchikuNumber * kaisuNumber))
            {
                result[0] = "1";
                return result;
            }

            // 建築面積×地上階数の値が3000㎡以下または不明
            result[0] = selectedFireproofStructureType;
            return result;
        }

        /// <summary>
        /// MOKUのデータ補完を行います。
        /// </summary>
        /// <param name="mokuSetting">MOKUのデータ補完設定</param>
        /// <param name="kozoValue">KOZOの値</param>
        /// <param name="xmlBuildingNode">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <returns>
        /// 補完結果と補完に利用した値の配列<br/>
        /// [MOKU][建物構造]
        /// </returns>
        private string[] SupplementMOKU(ElementAddSettingSupplementItem mokuSetting, string kozoValue, XmlNode xmlBuildingNode, XmlNamespaceManager xmlnsManager)
        {
            var tatemonoValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, mokuSetting.Tatemono);

            var result = new string[2];
            result[1] = tatemonoValue;

            switch (tatemonoValue)
            {
                case "601":
                    result[0] = "1";
                    return result;
                case "602":
                case "603":
                case "604":
                case "605":
                case "606":
                case "610":
                    result[0] = "2";
                    return result;
                case "611":
                default:
                    break;
            }

            switch (kozoValue)
            {
                case "1":
                case "2":
                    result[0] = "2";
                    return result;
                case "3":
                case "4":
                case "5":
                    result[0] = "1";
                    return result;
            }

            // ここには来ないはず
            App.Logger.Warn($"想定外のKOZOの値が指定された KOZO = {kozoValue}");
            result[0] = string.Empty;
            return result;
        }

        /// <summary>
        /// YOTOのデータ補完を行います。
        /// </summary>
        /// <param name="yotoSetting">YOTOのデータ補完設定</param>
        /// <param name="xmlBuildingNode">建物情報ノード</param>
        /// <param name="xmlnsManager">名前空間マネージャ</param>
        /// <returns>
        /// 補完結果と補完に利用した値の配列<br/>
        /// [YOTO][利用目的]
        /// </returns>
        private string[] SupplementYOTO(ElementAddSettingSupplementItem yotoSetting, XmlNode xmlBuildingNode, XmlNamespaceManager xmlnsManager)
        {
            var mokutekiValue = CityGmlFileLoader.GetTagValue(xmlBuildingNode, xmlnsManager, yotoSetting.Mokuteki);

            var result = new string[2];
            result[1] = mokutekiValue;

            result[0] = mokutekiValue switch
            {
                "401" => "1",
                "402" => "2",
                "403" => "3",
                "404" => "6",
                "411" => "7",
                "412" => "8",
                "413" => "9",
                "414" => "10",
                "415" => "11",
                "421" => "12",
                "422" => "14",
                "431" => "15",
                "441" => "16",
                "451" => "21",
                _ => "22",
            };

            return result;
        }
    }
}
