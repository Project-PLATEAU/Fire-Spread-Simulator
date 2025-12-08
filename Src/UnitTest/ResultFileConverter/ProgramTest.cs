using System.Reflection;
using SimulationCommonLibrary.Model;

namespace UnitTest.ResultFileConverter;

/// <summary>
/// Programテストクラス
/// </summary>
[TestClass]
public class ProgramTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    internal static string TestDataFolder => Path.Combine(@"..\..\..\ResultFileConverter", "TestData", "ProgramTest");

    /// <summary>
    /// 正常
    /// </summary>
    [TestMethod]
    public void CheckSettingFileTest_Success()
    {
        var settingFilePath = Path.Combine(TestDataFolder, "ResultFileConv_正常.setting");

        var asmb = Assembly.LoadFrom(@".\SimulationResultFileConverter.dll");
        var type = asmb?.GetType("SimulationResultFileConverter.Program");
        var method = type.GetMethod("CheckSettingFile", BindingFlags.Static | BindingFlags.NonPublic);

        var result = (ResultFileConvSetting?)method.Invoke(null, [settingFilePath]);
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// 不正；ファイル不正
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    [TestMethod]
    [DataRow(null,                           DisplayName = "パスがnull")]
    [DataRow("",                             DisplayName = "パスが空文字")]
    [DataRow("ResultFileConv_dummy.setting", DisplayName = "ファイルがない")]
    [DataRow("ResultFileConv_形式不正_タグ不正.setting")]
    [DataRow("ResultFileConv_形式不正_空.setting")]
    public void CheckSettingFileTest_Failure_InvalidFile(string fileName)
    {
        string? settingFilePath;
        if (string.IsNullOrEmpty(fileName))
        {
            settingFilePath = fileName;
        }
        else
        {
            settingFilePath = Path.Combine(TestDataFolder, fileName);
        }

        var asmb = Assembly.LoadFrom(@".\SimulationResultFileConverter.dll");
        var type = asmb?.GetType("SimulationResultFileConverter.Program");
        var method = type.GetMethod("CheckSettingFile", BindingFlags.Static | BindingFlags.NonPublic);

        var result = (ResultFileConvSetting?)method.Invoke(null, [settingFilePath]);
        Assert.IsNull(result);
    }

    /// <summary>
    /// 不正：内容不正
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    [TestMethod]
    [DataRow("ResultFileConv_内容不正_すべて空.setting")]
    [DataRow("ResultFileConv_内容不正_データフォルダ無し.setting")]
    [DataRow("ResultFileConv_内容不正_結果フォルダ無し.setting")]
    [DataRow("ResultFileConv_内容不正_出力フォルダ無し.setting")]
    [DataRow("ResultFileConv_内容不正_同一フォルダ1.setting")]
    [DataRow("ResultFileConv_内容不正_同一フォルダ2.setting")]
    [DataRow("ResultFileConv_内容不正_同一フォルダ3.setting")]
    public void CheckSettingFileTest_Failure_InvalidContent(string fileName)
    {
        var settingFilePath = Path.Combine(TestDataFolder, fileName);

        var asmb = Assembly.LoadFrom(@".\SimulationResultFileConverter.dll");
        var type = asmb?.GetType("SimulationResultFileConverter.Program");
        var method = type.GetMethod("CheckSettingFile", BindingFlags.Static | BindingFlags.NonPublic);

        var result = (ResultFileConvSetting?)method.Invoke(null, [settingFilePath]);
        Assert.IsNull(result);
    }
}
