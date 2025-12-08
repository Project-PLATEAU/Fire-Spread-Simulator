using System.Reflection;
using SimulationSourceFileCreator.Model;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// ElementAddSetttingテストクラス
/// </summary>
[TestClass]
public class ElementAddSetttingTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SourceFileCreator", "TestData", "ElementAddSetttingTest");

    /// <summary>
    /// <see cref="ElementAddSettting.Load"/>のテスト（ファイル）
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="expectedValid">期待値：ファイルが有効かどうか</param>
    [TestMethod]
    [DataRow(null,                           false, DisplayName = "パスがnull")]
    [DataRow("",                             false, DisplayName = "パスが空文字")]
    [DataRow("ElementAddSettting_dummy.xml", false, DisplayName = "ファイルがない")]
    [DataRow("ElementAddSettting_ファイル確認_タグ不正.xml",       false)]
    [DataRow("ElementAddSettting_ファイル確認_空.xml",             false)]
    [DataRow("ElementAddSettting_ファイル確認_正常（初期値）.xml", true)]
    public void LoadTest_ForFile(string fileName, bool expectedValid)
    {
        string? configFilePath;
        if (string.IsNullOrEmpty(fileName))
        {
            configFilePath = fileName;
        }
        else
        {
            configFilePath = Path.Combine(TestDataFolder, fileName);
        }

        var setting = ElementAddSettting.Load(configFilePath);

        if (expectedValid)
        {
            Assert.IsNotNull(setting);
        }
        else
        {
            Assert.IsNull(setting);
        }
    }

    /// <summary>
    /// <see cref="ElementAddSettting.Load"/>のテスト（内容）
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="expectedValid">期待値：ファイルが有効かどうか</param>
    [TestMethod]
    [DataRow("ElementAddSettting_内容確認_すべて空.xml",                                     false)]
    [DataRow("ElementAddSettting_内容確認_建物IDの設定なし.xml",                             false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOデータ補完）1_耐火構造なし.xml", false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOデータ補完）2_建物構造なし.xml", false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOデータ補完）3_地上階数なし.xml", false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOデータ補完）4_延床面積なし.xml", false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOデータ補完）5_建築面積なし.xml", false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（KOZOなし）.xml",                     false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（MOKUデータ補完）建物構造なし.xml",   false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（MOKUなし）.xml",                     false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（YOTOデータ補完）利用目的なし.xml",   false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定（YOTOなし）.xml",                     false)]
    [DataRow("ElementAddSettting_内容確認_取得要素設定のKeyNameが重複.xml",                  false)]
    [DataRow("ElementAddSettting_内容確認_追加要素設定_AddParentElementなし.xml",            false)]
    [DataRow("ElementAddSettting_内容確認_追加要素設定_AddParentElement空.xml",              false)]
    [DataRow("ElementAddSettting_内容確認_追加要素設定_SimNamespaceUriなし.xml",             false)]
    [DataRow("ElementAddSettting_内容確認_追加要素設定_SimNamespaceUri空.xml",               false)]
    [DataRow("ElementAddSettting_内容確認_追加要素設定_SimNamespaceXsdなし.xml",             true)] // SimNamespaceXsdがnullまたはEmptyはOK
    [DataRow("ElementAddSettting_内容確認_追加要素設定_SimNamespaceXsd空.xml",               true)] // SimNamespaceXsdがnullまたはEmptyはOK
    public void LoadTest_ForContent(string fileName, bool expectedValid)
    {
        var configFilePath = Path.Combine(TestDataFolder, fileName);
        var setting = ElementAddSettting.Load(configFilePath);

        if (expectedValid)
        {
            Assert.IsNotNull(setting);
        }
        else
        {
            Assert.IsNull(setting);
        }
    }

    /// <summary>
    /// <see cref="ElementAddSettting.CheckGetElementSetting"/>のテスト（全体）
    /// </summary>
    /// <param name="keyName">キー</param>
    /// <param name="targetType">取得タイプ</param>
    /// <param name="targetValue">取得タイプに応じた値</param>
    /// <param name="isBdgId">建物IDかどうか</param>
    /// <param name="expectedCheckOK">期待値：正常かどうか</param>
    [TestMethod]
    [DataRow("KeyName", 0, "gen:stringAttribute name", true,  true)]
    [DataRow("KeyName", 0, "gen:stringAttribute name", false, true)]

    [DataRow("",        0, "gen:stringAttribute name", true,  true)]
    [DataRow("",        0, "gen:stringAttribute name", false, false)]

    [DataRow("KeyName", 0, "", true,  false)]
    [DataRow("KeyName", 0, "", false, false)]

    [DataRow("KeyName", -1, "gen:stringAttribute name", true,  false)]
    [DataRow("KeyName", -1, "gen:stringAttribute name", false, false)]
    [DataRow("KeyName",  4, "gen:stringAttribute name", true,  false)]
    [DataRow("KeyName",  4, "gen:stringAttribute name", false, false)]
    public void CheckGetElementSettingTest_Overall(string keyName, int targetType, string targetValue, bool isBdgId, bool expectedCheckOK)
    {
        var getSetting = new GetElement()
        {
            KeyName = keyName,
            TargetType = targetType,
            TargetValue = targetValue,
        };

        var method = typeof(ElementAddSettting).GetMethod("CheckGetElementSetting", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string)method.Invoke(new ElementAddSettting(), ["取得要素設定", getSetting, isBdgId]);

        Console.WriteLine($"[{getSetting.TagName}] [{getSetting.AttributeName}] [{getSetting.AttributeValue}] [{getSetting.FixedValue}]");
        Console.WriteLine(result);
        Assert.AreEqual(expectedCheckOK, string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// <see cref="ElementAddSettting.CheckGetElementSetting"/>のテスト（取得タイプに応じた値）
    /// </summary>
    /// <param name="targetType">取得タイプ</param>
    /// <param name="targetValue">取得タイプに応じた値</param>
    /// <param name="expectedCheckOK">期待値：正常かどうか</param>
    /// <param name="expectedTagName">期待値：要素名</param>
    /// <param name="expectedAttributeName">期待値：属性名</param>
    /// <param name="expectedAttributeValue">期待値：属性値</param>
    /// <param name="expectedFixedValue">期待値：固定値</param>
    [TestMethod]
    [DataRow(0, "gen:stringAttribute name",      true,  "gen:stringAttribute", "name",     "", "")]
    [DataRow(0, "gen:stringAttribute ",          false, "",                    "",         "", "")]
    [DataRow(0, "gen:stringAttribute",           false, "",                    "",         "", "")]
    [DataRow(0, " name",                         true,  "",                    "name",     "", "")]
    [DataRow(0, " ",                             false, "",                    "",         "", "")]
    [DataRow(0, "gen:stringAttribute name test", true, "gen:stringAttribute", "name test", "", "")]

    [DataRow(1, "gen:stringAttribute name=&quot;建物 ID&quot;", true, "gen:stringAttribute", "name", "&quot;建物 ID&quot;", "")]
    [DataRow(1, "gen:stringAttribute name=",                    false, "", "", "", "")]
    [DataRow(1, "gen:stringAttribute name",                     false, "", "", "", "")]
    [DataRow(1, "gen:stringAttribute ",                         false, "", "", "", "")]
    [DataRow(1, "gen:stringAttribute",                          false, "", "", "", "")]
    [DataRow(1, "gen:stringAttribute =&quot;建物 ID&quot;",     false, "", "", "", "")]
    [DataRow(1, "gen:stringAttribute=&quot;建物 ID&quot;",      false, "", "", "", "")]
    [DataRow(1, " name=&quot;建物 ID&quot;",                    false, "", "", "", "")]
    [DataRow(1, " =&quot;建物 ID&quot;",                        false, "", "", "", "")]
    [DataRow(1, " =",                                           false, "", "", "", "")]

    [DataRow(2, "uro:buildingDetailAttribute/uro:BuildingDetailAttribute/uro:districtsAndZonesType", true, "uro:buildingDetailAttribute/uro:BuildingDetailAttribute/uro:districtsAndZonesType", "", "", "")]

    [DataRow(3, "0.5", true, "", "", "", "0.5")]
    [DataRow(3, "aaa", true, "", "", "", "aaa")]
    public void CheckGetElementSettingTest_TargetValue(int targetType, string targetValue, bool expectedCheckOK, string expectedTagName, string expectedAttributeName, string expectedAttributeValue, string expectedFixedValue)
    {
        var getSetting = new GetElement()
        {
            KeyName = "KeyName",
            TargetType = targetType,
            TargetValue = targetValue,
        };

        var method = typeof(ElementAddSettting).GetMethod("CheckGetElementSetting", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string)method.Invoke(new ElementAddSettting(), ["取得要素設定", getSetting, false]);

        Console.WriteLine($"[{getSetting.TagName}] [{getSetting.AttributeName}] [{getSetting.AttributeValue}] [{getSetting.FixedValue}]");
        Console.WriteLine(result);

        Assert.AreEqual(expectedCheckOK, string.IsNullOrEmpty(result));

        if (expectedCheckOK)
        {
            Assert.AreEqual(expectedTagName,        getSetting.TagName);
            Assert.AreEqual(expectedAttributeName,  getSetting.AttributeName);
            Assert.AreEqual(expectedAttributeValue, getSetting.AttributeValue);
            Assert.AreEqual(expectedFixedValue,     getSetting.FixedValue);
        }
    }

    /// <summary>
    /// <see cref="ElementAddSettting.CheckRemoveElementSetting"/>のテスト
    /// </summary>
    /// <param name="tagName">要素名</param>
    /// <param name="expectedCheckOK">期待値：正常かどうか</param>
    [TestMethod]
    [DataRow("KeyName", true)]
    [DataRow("",        false)]
    public void CheckRemoveElementSettingTest(string tagName, bool expectedCheckOK)
    {
        var removeSetting = new RemoveElement()
        {
            TagName = tagName,
            CheckMinValue = 0,
            CheckMaxValue = 0,
        };

        var method = typeof(ElementAddSettting).GetMethod("CheckRemoveElementSetting", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string)method.Invoke(new ElementAddSettting(), [removeSetting]);

        Console.WriteLine(result);
        Assert.AreEqual(expectedCheckOK, string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// <see cref="ElementAddSettting.CheckAddElementSetting"/>のテスト
    /// </summary>
    /// <param name="keyName">キー</param>
    /// <param name="tagName">要素名</param>
    /// <param name="expectedCheckOK">期待値：正常かどうか</param>
    [TestMethod]
    [DataRow("KeyName", "sim:floorHeight", true)]
    [DataRow("",        "sim:floorHeight", false)]
    [DataRow("KeyName", "",                false)]
    [DataRow("",        "",                false)]
    public void CheckAddElementSettingTest(string keyName, string tagName, bool expectedCheckOK)
    {
        var addSetting = new AddElement()
        {
            KeyName = keyName,
            TagName = tagName,
            DefaultValue = 0,
        };

        var method = typeof(ElementAddSettting).GetMethod("CheckAddElementSetting", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (string)method.Invoke(new ElementAddSettting(), [addSetting]);

        Console.WriteLine(result);
        Assert.AreEqual(expectedCheckOK, string.IsNullOrEmpty(result));
    }
}
