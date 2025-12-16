using System.Reflection;
using SimulationSourceFileCreator.Controller;
using SimulationSourceFileCreator.Model;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// IntermediateCsvFileCreatorテストクラス
/// </summary>
[TestClass]
public class IntermediateCsvFileCreatorTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SourceFileCreator", "TestData", "CityGmlFileLoaderTest");

    /// <summary>
    /// <see cref="IntermediateCsvFileCreator.SupplementKOZO"/>のテスト
    /// </summary>
    /// <param name="taikaValue">耐火構造</param>
    /// <param name="tatemonoValue">建物構造</param>
    /// <param name="kaisuValue">地上階数</param>
    /// <param name="nobeyukaValue">延床面積</param>
    /// <param name="kenchikuValue">建築面積</param>
    /// <param name="fireproofStructureType">防火構造</param>
    /// <param name="expectedKOZOValue">期待値：補完結果</param>
    [TestMethod]

    // 1.耐火構造による判断
    [DataRow("1001", "未使用", "未使用", "未使用", "未使用", "未使用", "1")]
    [DataRow("1002", "未使用", "未使用", "未使用", "未使用", "未使用", "2")]

    // 2.建物構造による判断
    [DataRow("1003", "601", "未使用", "未使用", "未使用", "3",      "3")]
    [DataRow("1003", "601", "未使用", "未使用", "未使用", "4",      "4")]
    [DataRow("1003", "601", "未使用", "未使用", "未使用", "5",      "5")]
    [DataRow("1003", "602", "未使用", "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "603", "未使用", "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "604", "未使用", "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "605", "未使用", "未使用", "未使用", "未使用", "2")]
    [DataRow("1003", "606", "未使用", "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "610", "未使用", "未使用", "未使用", "未使用", "2")]

    // 以降、耐火構造 3種類（1003、1011、不明）
    // 　　　建物構造 2種類（611、不明）
    // 3種類×2種類の6パターンを実施する

    // ＜パターン1＞耐火構造 = 1003、建物構造 = 611
    // 3.地上階数・延床面積による判断
    [DataRow("1003", "611", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "611", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("1003", "611", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("1003", "611", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("1003", "611", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("1003", "611", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("1003", "611", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("1003", "611", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("1003", "611", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("1003", "611", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("1003", "611", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("1003", "611", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("1003", "611", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("1003", "611", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("1003", "611", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("1003", "611", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("1003", "611", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("1003", "611", "1", "不明", "3000.0", "3",      "3")]
    [DataRow("1003", "611", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("1003", "611", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("1003", "611", "3", "不明", "不明",   "3",      "3")]
    [DataRow("1003", "611", "3", "不明", "不明",   "4",      "4")]
    [DataRow("1003", "611", "3", "不明", "不明",   "5",      "5")]

    // ＜パターン2＞耐火構造 = 1003、建物構造 = 不明
    // 3.地上階数・延床面積による判断
    [DataRow("1003", "不明", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("1003", "不明", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("1003", "不明", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("1003", "不明", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("1003", "不明", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("1003", "不明", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("1003", "不明", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("1003", "不明", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("1003", "不明", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("1003", "不明", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("1003", "不明", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("1003", "不明", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("1003", "不明", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("1003", "不明", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("1003", "不明", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("1003", "不明", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("1003", "不明", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("1003", "不明", "1", "不明", "3000.0", "3",      "3")]
    [DataRow("1003", "不明", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("1003", "不明", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("1003", "不明", "3", "不明", "不明",   "3",      "3")]
    [DataRow("1003", "不明", "3", "不明", "不明",   "4",      "4")]
    [DataRow("1003", "不明", "3", "不明", "不明",   "5",      "5")]

    // ＜パターン3＞耐火構造 = 1011、建物構造 = 611
    // 3.地上階数・延床面積による判断
    [DataRow("1011", "611", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("1011", "611", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("1011", "611", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("1011", "611", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("1011", "611", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("1011", "611", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("1011", "611", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("1011", "611", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("1011", "611", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("1011", "611", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("1011", "611", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("1011", "611", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("1011", "611", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("1011", "611", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("1011", "611", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("1011", "611", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("1011", "611", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("1011", "611", "1", "不明", "3000.0", "3",      "3")]
    [DataRow("1011", "611", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("1011", "611", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("1011", "611", "3", "不明", "不明",   "3",      "3")]
    [DataRow("1011", "611", "3", "不明", "不明",   "4",      "4")]
    [DataRow("1011", "611", "3", "不明", "不明",   "5",      "5")]

    // ＜パターン4＞耐火構造 = 1011、建物構造 = 不明
    // 3.地上階数・延床面積による判断
    [DataRow("1011", "不明", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("1011", "不明", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("1011", "不明", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("1011", "不明", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("1011", "不明", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("1011", "不明", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("1011", "不明", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("1011", "不明", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("1011", "不明", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("1011", "不明", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("1011", "不明", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("1011", "不明", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("1011", "不明", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("1011", "不明", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("1011", "不明", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("1011", "不明", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("1011", "不明", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("1011", "不明", "1", "不明", "3000.0", "3",      "3")]
    [DataRow("1011", "不明", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("1011", "不明", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("1011", "不明", "3", "不明", "不明",   "3",      "3")]
    [DataRow("1011", "不明", "3", "不明", "不明",   "4",      "4")]
    [DataRow("1011", "不明", "3", "不明", "不明",   "5",      "5")]

    // ＜パターン5＞耐火構造 = 不明、建物構造 = 611
    // 3.地上階数・延床面積による判断
    [DataRow("不明", "611", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("不明", "611", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("不明", "611", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("不明", "611", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("不明", "611", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("不明", "611", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("不明", "611", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("不明", "611", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("不明", "611", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("不明", "611", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("不明", "611", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("不明", "611", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("不明", "611", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("不明", "611", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("不明", "611", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("不明", "611", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("不明", "611", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("不明", "611", "1", "不明", "3000.0", "3",       "3")]
    [DataRow("不明", "611", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("不明", "611", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("不明", "611", "3", "不明", "不明",   "3",      "3")]
    [DataRow("不明", "611", "3", "不明", "不明",   "4",      "4")]
    [DataRow("不明", "611", "3", "不明", "不明",   "5",      "5")]

    // ＜パターン6＞耐火構造 = 不明、建物構造 = 不明
    // 3.地上階数・延床面積による判断
    [DataRow("不明", "不明", "4",      "未使用", "未使用", "未使用", "1")]
    [DataRow("不明", "不明", "未使用", "3000.1", "未使用", "未使用", "1")]
    [DataRow("不明", "不明", "3",      "3000.0", "未使用", "3",      "3")]
    [DataRow("不明", "不明", "3",      "3000.0", "未使用", "4",      "4")]
    [DataRow("不明", "不明", "3",      "3000.0", "未使用", "5",      "5")]
    [DataRow("不明", "不明", "不明",   "未使用", "未使用", "3",      "3")]
    [DataRow("不明", "不明", "不明",   "未使用", "未使用", "4",      "4")]
    [DataRow("不明", "不明", "不明",   "未使用", "未使用", "5",      "5")]

    // 4.建築面積・地上階数による判断
    [DataRow("不明", "不明", "3", "不明", "1000.1", "未使用", "1")]
    [DataRow("不明", "不明", "3", "不明", "1000.0", "3",      "3")]
    [DataRow("不明", "不明", "3", "不明", "1000.0", "4",      "4")]
    [DataRow("不明", "不明", "3", "不明", "1000.0", "5",      "5")]
    [DataRow("不明", "不明", "2", "不明", "1500.1", "未使用", "1")]
    [DataRow("不明", "不明", "2", "不明", "1500.0", "3",      "3")]
    [DataRow("不明", "不明", "2", "不明", "1500.0", "4",      "4")]
    [DataRow("不明", "不明", "2", "不明", "1500.0", "5",      "5")]
    [DataRow("不明", "不明", "1", "不明", "3000.1", "未使用", "1")]
    [DataRow("不明", "不明", "1", "不明", "3000.0", "3",      "3")]
    [DataRow("不明", "不明", "1", "不明", "3000.0", "4",      "4")]
    [DataRow("不明", "不明", "1", "不明", "3000.0", "5",      "5")]
    [DataRow("不明", "不明", "3", "不明", "不明",   "3",      "3")]
    [DataRow("不明", "不明", "3", "不明", "不明",   "4",      "4")]
    [DataRow("不明", "不明", "3", "不明", "不明",   "5",      "5")]
    public void SupplementKOZOTest(string taikaValue, string tatemonoValue, string kaisuValue, string nobeyukaValue, string kenchikuValue, string fireproofStructureType, string expectedKOZOValue)
    {
        var kozoSetting = new ElementAddSettingSupplementItem()
        {
            Taika =    new GetElement() { TargetType = 3, FixedValue = taikaValue, },
            Tatemono = new GetElement() { TargetType = 3, FixedValue = tatemonoValue, },
            Kaisu =    new GetElement() { TargetType = 3, FixedValue = kaisuValue, },
            Nobeyuka = new GetElement() { TargetType = 3, FixedValue = nobeyukaValue, },
            Kenchiku = new GetElement() { TargetType = 3, FixedValue = kenchikuValue, },
        };

        var filePath = Path.Combine(TestDataFolder, "53395313_要素取得テスト用.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var method = typeof(IntermediateCsvFileCreator).GetMethod("SupplementKOZO", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string[])method.Invoke(new IntermediateCsvFileCreator(), [kozoSetting, fireproofStructureType, buildingNodes[0], xmlnsManager]);

        Console.WriteLine(string.Join(',', result));
        Assert.AreEqual(expectedKOZOValue, result[0]);
        Assert.AreEqual(taikaValue,        result[1]);
        Assert.AreEqual(tatemonoValue,     result[2]);
        Assert.AreEqual(kaisuValue,        result[3]);
        Assert.AreEqual(nobeyukaValue,     result[4]);
        Assert.AreEqual(kenchikuValue,     result[5]);
    }

    /// <summary>
    /// <see cref="IntermediateCsvFileCreator.SupplementMOKU"/>のテスト
    /// </summary>
    /// <param name="tatemonoValue">建物構造</param>
    /// <param name="kozoValue">KOZOの値</param>
    /// <param name="expectedMOKUValue">期待値：補完結果</param>
    [TestMethod]
    [DataRow("601", "未使用", "1")]
    [DataRow("602", "未使用", "2")]
    [DataRow("603", "未使用", "2")]
    [DataRow("604", "未使用", "2")]
    [DataRow("605", "未使用", "2")]
    [DataRow("606", "未使用", "2")]
    [DataRow("610", "未使用", "2")]

    [DataRow("611", "1",    "2")]
    [DataRow("611", "2",    "2")]
    [DataRow("611", "3",    "1")]
    [DataRow("611", "4",    "1")]
    [DataRow("611", "5",    "1")]
    [DataRow("611", "不明", "")]

    [DataRow("不明", "1",    "2")]
    [DataRow("不明", "2",    "2")]
    [DataRow("不明", "3",    "1")]
    [DataRow("不明", "4",    "1")]
    [DataRow("不明", "5",    "1")]
    [DataRow("不明", "不明", "")]
    public void SupplementMOKUTest(string tatemonoValue, string kozoValue, string expectedMOKUValue)
    {
        var mokuSetting = new ElementAddSettingSupplementItem()
        {
            Tatemono = new GetElement()
            {
                TargetType = 3,
                FixedValue = tatemonoValue,
            },
        };

        var filePath = Path.Combine(TestDataFolder, "53395313_要素取得テスト用.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var method = typeof(IntermediateCsvFileCreator).GetMethod("SupplementMOKU", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string[])method.Invoke(new IntermediateCsvFileCreator(), [mokuSetting, kozoValue, buildingNodes[0], xmlnsManager]);

        Console.WriteLine(string.Join(',', result));
        Assert.AreEqual(expectedMOKUValue, result[0]);
        Assert.AreEqual(tatemonoValue, result[1]);
    }

    /// <summary>
    /// <see cref="IntermediateCsvFileCreator.SupplementYOTO"/>のテスト
    /// </summary>
    /// <param name="mokutekiValue">利用目的</param>
    /// <param name="expectedYOTOValue">期待値：補完結果</param>
    [TestMethod]
    [DataRow("401",  "1")]
    [DataRow("402",  "2")]
    [DataRow("403",  "3")]
    [DataRow("404",  "6")]
    [DataRow("411",  "7")]
    [DataRow("412",  "8")]
    [DataRow("413",  "9")]
    [DataRow("414",  "10")]
    [DataRow("415",  "11")]
    [DataRow("421",  "12")]
    [DataRow("422",  "14")]
    [DataRow("431",  "15")]
    [DataRow("441",  "16")]
    [DataRow("451",  "21")]
    [DataRow("452",  "22")]
    [DataRow("453",  "22")]
    [DataRow("454",  "22")]
    [DataRow("461",  "22")]
    [DataRow("不明", "22")]
    public void SupplementYOTOTest(string mokutekiValue, string expectedYOTOValue)
    {
        var yotoSetting = new ElementAddSettingSupplementItem()
        {
            Mokuteki = new GetElement()
            {
                TargetType = 3,
                FixedValue = mokutekiValue,
            },
        };

        var filePath = Path.Combine(TestDataFolder, "53395313_要素取得テスト用.gml");
        var isSuccess = CityGmlFileLoader.Load(filePath, out _, out var xmlnsManager, out var buildingNodes, out _);
        Assert.IsTrue(isSuccess);

        var method = typeof(IntermediateCsvFileCreator).GetMethod("SupplementYOTO", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string[])method.Invoke(new IntermediateCsvFileCreator(), [yotoSetting, buildingNodes[0], xmlnsManager]);

        Console.WriteLine(string.Join(',', result));
        Assert.AreEqual(expectedYOTOValue, result[0]);
        Assert.AreEqual(mokutekiValue, result[1]);
    }
}
