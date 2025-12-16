using System.Text;

namespace UnitTest;

[TestClass]
public class GlobalSetup
{
    /// <summary>
    /// アセンブリ全体のテスト実行前に一度だけ呼ばれる初期化メソッド
    /// </summary>
    /// <param name="context">テスト実行に関する情報を保持する TestContext</param>
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        // エンコーディングプロバイダーの登録
        // ※ .NET(Core系)は デフォルトで shift-jis(sjis) に対応したエンコーディングプロバイダーが
        //    登録されていないため Encoding.RegisterProvider メソッドで明示的に登録する必要があります。
        //    プロバイダーは1度登録すると、プログラムが終了するまで有効です。
        //    プロバイダーを登録したプログラムのみ有効で、他のプログラムや環境などには影響しません。
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// アセンブリ全体のテスト実行後に一度だけ呼ばれるクリーンアップメソッド
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
    }
}
