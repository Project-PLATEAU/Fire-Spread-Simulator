using SimulationSourceFileCreator.Controller;
using SimulationSourceFileCreator.Model;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// ElementAddCityGmlFileCreatorテストクラス
/// </summary>
[TestClass]
public class ElementAddCityGmlFileCreatorTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SourceFileCreator", "TestData", "ElementAddCityGmlFileCreator");

    /// <summary>
    /// <see cref="ElementAddCityGmlFileCreator.CreateGmlFile"/>のテスト<br/>
    /// 中間CSVファイルの読み込み
    /// </summary>
    /// <param name="csvFileName">中間CSVファイル名</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    [TestMethod]
    [DataRow("53395313_dummy.csv",                 false)]
    [DataRow("53395313_BldgId重複.csv",            false)]
    [DataRow("53395313.csv",                       true)]
    [DataRow("53395313_対象のBdgIdの行がない.csv", false)]
    public void CreateGmlFileTest_ForCsv(string csvFileName, bool expectedIsSuccess)
    {
        var setting = this.CreateMinimumSetting();
        Assert.IsNotNull(setting);

        var testFolder = Path.Combine(TestDataFolder, "ForCsv");

        var filePath = Path.Combine(testFolder, "53395313.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var inputCsvFilePath = Path.Combine(testFolder, csvFileName);
        var outputCityGMLFilePath = Path.Combine(testFolder, "53395313_result.gml");
        var cancelToken = new CancellationTokenSource();

        var elementAddCityGmlFileCreator = new ElementAddCityGmlFileCreator();
        var result = elementAddCityGmlFileCreator.CreateGmlFile(xmlDoc, xmlnsManager, buildingNodes, inputCsvFilePath, outputCityGMLFilePath, setting, cancelToken);

        Assert.AreEqual(expectedIsSuccess, result);

        // 作成したファイルの削除
        File.Delete(outputCityGMLFilePath);
    }

    /// <summary>
    /// <see cref="ElementAddCityGmlFileCreator.CreateGmlFile"/>のテスト<br/>
    /// 地上階数の要素の削除（LOD2で1フロア分の高さが低すぎる場合）
    /// </summary>
    /// <param name="gmlFileName">CityGMLファイル名</param>
    /// <param name="expectedFileName">期待値：結果ファイル名</param>
    /// <param name="csvFileName">中間CSVファイル名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("53395347_1_1フロア分の高さが1.25m以上.gml", "53395347_expected_1.gml")]
    [DataRow("53395347_2_1フロア分の高さが1.25m未満.gml", "53395347_expected_2.gml")]
    [DataRow("53395347_2_1フロア分の高さが1.25m未満.gml", "53395347_expected_8.gml", "53395347_LOD1.csv")]
    [DataRow("53395347_3_建物高さ無効.gml",               "53395347_expected_3.gml")]
    [DataRow("53395347_4_建物高さ要素無し.gml",           "53395347_expected_4.gml")]
    [DataRow("53395347_5_地上階数無効.gml",               "53395347_expected_5.gml")]
    [DataRow("53395347_6_地上階数要素無し.gml",           "53395347_expected_6.gml")]
    [DataRow("53395347_7_地上階数1建物高さ1.24m.gml",     "53395347_expected_7.gml")]
    public void CreateGmlFileTest_ForFloorHeight(string gmlFileName, string expectedFileName, string? csvFileName = null)
    {
        var setting = this.CreateMinimumSetting();
        Assert.IsNotNull(setting);

        var testFolder = Path.Combine(TestDataFolder, "ForFloorHeight");

        var filePath = Path.Combine(testFolder, gmlFileName);
        var isSuccess = CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var inputCsvFilePath = Path.Combine(testFolder, csvFileName ?? "53395347_LOD2.csv");
        var outputCityGMLFilePath = Path.Combine(testFolder, "53395347_result.gml");
        var cancelToken = new CancellationTokenSource();

        var elementAddCityGmlFileCreator = new ElementAddCityGmlFileCreator();
        var result = elementAddCityGmlFileCreator.CreateGmlFile(xmlDoc, xmlnsManager, buildingNodes, inputCsvFilePath, outputCityGMLFilePath, setting, cancelToken);

        Assert.IsTrue(result);

        var expectedFilePath = Path.Combine(testFolder, expectedFileName);
        UnitTestHelper.CheckEqualsContent(expectedFilePath, outputCityGMLFilePath);

        // 作成したファイルの削除
        File.Delete(outputCityGMLFilePath);
    }

    /// <summary>
    /// <see cref="ElementAddCityGmlFileCreator.CreateGmlFile"/>のテスト<br/>
    /// 欠測値の要素の削除
    /// </summary>
    /// <param name="tagName">要素名</param>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    /// <param name="expectedFileName">期待値：結果ファイル名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("bldg:removeTest1", 1232, 1233, "53395313_expected_test1Removed.gml")]
    [DataRow("bldg:removeTest1", 1233, 1234, "53395313_expected.gml")]
    [DataRow("bldg:removeTest1", 1234, 1235, "53395313_expected.gml")]
    [DataRow("bldg:removeTest1", 1235, 1236, "53395313_expected_test1Removed.gml")]
    [DataRow("uro:buildingIDAttribute/uro:BuildingIDAttribute/bldg:removeTest2", 1232, 1233, "53395313_expected_test2Removed.gml")]
    [DataRow("uro:buildingIDAttribute/uro:BuildingIDAttribute/bldg:removeTest2", 1233, 1234, "53395313_expected.gml")]
    [DataRow("uro:buildingIDAttribute/uro:BuildingIDAttribute/bldg:removeTest2", 1234, 1235, "53395313_expected.gml")]
    [DataRow("uro:buildingIDAttribute/uro:BuildingIDAttribute/bldg:removeTest2", 1235, 1236, "53395313_expected_test2Removed.gml")]
    public void CreateGmlFileTest_ForRemoveElement(string tagName, int min, int max, string expectedFileName)
    {
        var setting = this.CreateMinimumSetting();
        setting.RemoveElements.Add(new RemoveElement()
        {
            TagName = tagName,
            CheckMinValue = min,
            CheckMaxValue = max,
        });
        Assert.IsNotNull(setting);

        var testFolder = Path.Combine(TestDataFolder, "ForRemoveElement");

        var filePath = Path.Combine(testFolder, "53395313_要素削除テスト用.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var inputCsvFilePath = Path.Combine(testFolder, "53395313.csv");
        var outputCityGMLFilePath = Path.Combine(testFolder, "53395313_result.gml");
        var cancelToken = new CancellationTokenSource();

        var elementAddCityGmlFileCreator = new ElementAddCityGmlFileCreator();
        var result = elementAddCityGmlFileCreator.CreateGmlFile(xmlDoc, xmlnsManager, buildingNodes, inputCsvFilePath, outputCityGMLFilePath, setting, cancelToken);

        Assert.IsTrue(result);

        var expectedFilePath = Path.Combine(testFolder, expectedFileName);
        UnitTestHelper.CheckEqualsContent(expectedFilePath, outputCityGMLFilePath);

        // 作成したファイルの削除
        File.Delete(outputCityGMLFilePath);
    }

    /// <summary>
    /// <see cref="ElementAddCityGmlFileCreator.CreateGmlFile"/>のテスト<br/>
    /// 地盤高さの修正（lod0FootPrint、lod0RoofEdgeのZ値）
    /// </summary>
    /// <param name="gmlFileName">CityGMLファイル名</param>
    /// <param name="expectedFileName">期待値：結果ファイル名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("53360199_lod0FootPrintZ値0.gml", "53360199_expected_0.gml")]
    [DataRow("53360199_lod0FootPrintZ値1.gml", "53360199_expected_1.gml")]
    [DataRow("53395346_lod0RoofEdgeZ値0.gml", "53395346_expected_0.gml")]
    [DataRow("53395346_lod0RoofEdgeZ値1.gml", "53395346_expected_1.gml")]
    public void CreateGmlFileTest_ForGroundHeight(string gmlFileName, string expectedFileName)
    {
        var setting = this.CreateMinimumSetting();
        Assert.IsNotNull(setting);

        var testFolder = Path.Combine(TestDataFolder, "ForGroundHeight");

        var filePath = Path.Combine(testFolder, gmlFileName);
        var isSuccess = CityGmlFileLoader.Load(filePath, out var xmlDoc, out var xmlnsManager, out var buildingNodes, out var meshNumer);
        Assert.IsTrue(isSuccess);

        var inputCsvFilePath = Path.Combine(testFolder, $"{meshNumer}.csv");
        var outputCityGMLFilePath = Path.Combine(testFolder, $"{meshNumer}_result.gml");
        var cancelToken = new CancellationTokenSource();

        var elementAddCityGmlFileCreator = new ElementAddCityGmlFileCreator();
        var result = elementAddCityGmlFileCreator.CreateGmlFile(xmlDoc, xmlnsManager, buildingNodes, inputCsvFilePath, outputCityGMLFilePath, setting, cancelToken);

        Assert.IsTrue(result);

        var expectedFilePath = Path.Combine(testFolder, expectedFileName);
        UnitTestHelper.CheckEqualsContent(expectedFilePath, outputCityGMLFilePath);

        // 作成したファイルの削除
        File.Delete(outputCityGMLFilePath);
    }

    private ElementAddSetting CreateMinimumSetting()
    {
        return new ElementAddSetting
        {
            BldgId = new GetElement()
            {
                TargetType = 0,
                AttributeName = "gml:id",
            },
            SimNamespaceUri = "simuri",
            AddParentElement = "sim:test",
        };
    }
}
