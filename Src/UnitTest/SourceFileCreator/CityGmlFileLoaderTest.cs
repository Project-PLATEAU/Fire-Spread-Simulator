using System.Reflection;
using SimulationSourceFileCreator.Controller;
using SimulationSourceFileCreator.Model;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// CityGmlFileLoaderテストクラス
/// </summary>
[TestClass]
public class CityGmlFileLoaderTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SourceFileCreator", "TestData", "CityGmlFileLoaderTest");

    /// <summary>
    /// <see cref="CityGmlFileLoader.Load"/>のテスト（ファイル）
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="expectedValid">期待値：ファイルが有効かどうか</param>
    /// <param name="expectedMeshNumer">期待値：メッシュ番号</param>
    [TestMethod]
    [DataRow(null,                 false, null, DisplayName = "パスがnull")]
    [DataRow("",                   false, null, DisplayName = "パスが空文字")]
    [DataRow("53395313_dummy.gml", false, null, DisplayName = "ファイルがない")]
    [DataRow("5339531.gml",        false, null)]
    [DataRow("53395313.gml",       true,  "53395313")]
    [DataRow("5339531X.gml",       false, null)]
    [DataRow("53395313_ファイル確認_タグ不正.gml", false, null)]
    [DataRow("53395313_ファイル確認_空.gml",       false, null)]
    [DataRow("53395313_ファイル確認_正常.gml",     true, "53395313")]
    public void LoadTest_ForFile(string fileName, bool expectedValid, string expectedMeshNumer)
    {
        string? filePath;
        if (string.IsNullOrEmpty(fileName))
        {
            filePath = fileName;
        }
        else
        {
            filePath = Path.Combine(TestDataFolder, fileName);
        }

        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out _, out _, out var meshNumer);
        Assert.AreEqual(expectedValid, isSuccess);

        if (expectedValid)
        {
            Assert.AreEqual(expectedMeshNumer, meshNumer);
        }
    }

    /// <summary>
    /// <see cref="CityGmlFileLoader.Load"/>のテスト（内容）
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="expectedValid">期待値：ファイルが有効かどうか</param>
    /// <param name="expectedBuildingCount">期待値：建物件数</param>
    [TestMethod]
    [DataRow("53395313_内容確認_Buildingノードなし.gml",       false, 0)]
    [DataRow("53395313_内容確認_CityGMLではない_31水涯線.xml", false, -1)]
    public void LoadTest_ForContent(string fileName, bool expectedValid, int? expectedBuildingCount)
    {
        var filePath = Path.Combine(TestDataFolder, fileName);

        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out _, out var buildingNodes, out _);
        Assert.AreEqual(expectedValid, isSuccess);
        Assert.AreEqual(expectedBuildingCount, buildingNodes == null ? -1 : buildingNodes.Count);
    }

    /// <summary>
    /// <see cref="CityGmlFileLoader.CheckAndAddNamespace"/>のテスト
    /// </summary>
    /// <param name="prefix">プレフィックス</param>
    /// <param name="expectedAdded">期待値：追加したかどうか</param>
    /// <param name="expectedUri">期待値：uri</param>
    [TestMethod]
    [DataRow("brid", false, "http://www.opengis.net/citygml/bridge/2.0")]
    [DataRow("test", true,  "testuri")]
    public void CheckAndAddNamespaceTest(string prefix, bool expectedAdded, string expectedUri)
    {
        var filePath = Path.Combine(TestDataFolder, "53395313.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out _, out _);
        Assert.IsTrue(isSuccess);

        var (added, uri) = CityGmlFileLoader.CheckAndAddNamespace(xmlDoc, xmlnsManager, prefix, "testuri", "testxsd");

        Assert.AreEqual(expectedAdded, added);
        Assert.AreEqual(expectedUri, uri);
    }

    /// <summary>
    /// <see cref="CityGmlFileLoader.GetTagValue"/>のテスト
    /// </summary>
    /// <param name="targetType">取得タイプ</param>
    /// <param name="tagName">要素名</param>
    /// <param name="attributeName">属性名</param>
    /// <param name="attributeValue">属性値</param>
    /// <param name="fixedValue">固定値</param>
    /// <param name="expectedValue">期待値：取得値</param>
    [TestMethod]
    [DataRow(0, "gen:stringAttribute", "name", null, null, "AAAA")]
    [DataRow(0, "stringAttribute",     "name", null, null, "")]
    [DataRow(0, "gen:stringAttribute", "test", null, null, "")]
    [DataRow(0, "gen:dummyTagName",    "name", null, null, "")]

    [DataRow(1, "gen:stringAttribute", "name", "建物 ID", null, "EEEE")]
    [DataRow(1, "stringAttribute",     "name", "建物 ID", null, "")]
    [DataRow(1, "gen:stringAttribute", "test", "建物 ID", null, "")]
    [DataRow(1, "gen:stringAttribute", "name", "建物ID",  null, "")]
    [DataRow(1, "gen:dummyTagName",    "name", "建物 ID", null, "")]

    [DataRow(2, "uro:buildingDetailAttribute/uro:BuildingDetailAttribute/uro:fireproofStructureType", null, null, null, "FFFF")]
    [DataRow(2, "uro:buildingDetailAttribute/uro:BuildingDetailAttribute",                            null, null, null, "FFFFGGGGHHHH")]
    [DataRow(2, "uro:buildingDetailAttribute",                                                        null, null, null, "FFFFGGGGHHHH")]
    [DataRow(2, "uro:dummyTagName/uro:BuildingDetailAttribute/uro:fireproofStructureType",            null, null, null, "")]

    [DataRow(3, null, null, null, "0.5", "0.5")]
    [DataRow(3, null, null, null, "aaa", "aaa")]
    [DataRow(3, null, null, null, "",    "")]
    public void GetTagValueTest(int targetType, string tagName, string attributeName, string attributeValue, string fixedValue, string expectedValue)
    {
        var filePath = Path.Combine(TestDataFolder, "53395313_要素取得テスト用.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var getSetting = new GetElement()
        {
            TargetType = targetType,
            TagName = tagName,
            AttributeName = attributeName,
            AttributeValue = attributeValue,
            FixedValue = fixedValue,
        };

        var tagValue = CityGmlFileLoader.GetTagValue(buildingNodes[0], xmlnsManager, getSetting);
        Assert.AreEqual(expectedValue, tagValue);
    }

    /// <summary>
    /// <see cref="CityGmlFileLoader.CheckPrefix"/>のテスト
    /// </summary>
    /// <param name="tagName">要素名</param>
    /// <param name="expectedCheckOK">期待値：プレフィックスがあるかどうか</param>
    [TestMethod]
    [DataRow("gen:test", true)]
    [DataRow("GEN:test", false)]
    [DataRow("aaa:test", false)]
    [DataRow(":test", false)]
    [DataRow("test", true)]
    [DataRow("", true)]
    public void CheckPrefixTest(string tagName, bool expectedCheckOK)
    {
        var filePath = Path.Combine(TestDataFolder, "53395313.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out var xmlnsManager, out _, out _);
        Assert.IsTrue(isSuccess);

        var getSetting = new GetElement()
        {
            TagName = tagName,
        };

        var method = typeof(CityGmlFileLoader).GetMethod("CheckPrefix", BindingFlags.Static | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(null, [xmlnsManager, getSetting]);

        Assert.AreEqual(expectedCheckOK, result);
    }
}
