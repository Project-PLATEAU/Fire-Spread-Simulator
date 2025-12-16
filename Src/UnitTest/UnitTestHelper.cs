namespace UnitTest
{
    /// <summary>
    /// UnitTestのヘルパークラス
    /// </summary>
    internal static class UnitTestHelper
    {
        /// <summary>
        /// ファイルの内容が等しいかどうかをチェックします。<br/>
        /// ファイルの内容が異なる場合はAssertに失敗します。
        /// </summary>
        /// <param name="expectedFilePath">期待する内容のファイルパス</param>
        /// <param name="actualFilePath">検証する内容のファイルパス</param>
        internal static void CheckEqualsContent(string expectedFilePath, string actualFilePath)
        {
            var expectedContent = File.ReadAllText(expectedFilePath);
            var actualContent = File.ReadAllText(actualFilePath);

            Assert.AreEqual(expectedContent, actualContent);
        }

        /// <summary>
        /// ファイルの内容が等しいかどうかをチェックします。<br/>
        /// ファイルの内容が異なる場合はAssertに失敗します。
        /// </summary>
        /// <param name="expectedFilePath">期待する内容のファイルパス</param>
        /// <param name="actualFilePath">検証する内容のファイルパス</param>
        /// <param name="excludeLineNum">検証を除外する行番号（0 origin）</param>
        internal static void CheckEqualsContent(string expectedFilePath, string actualFilePath, int excludeLineNum)
        {
            var expectedContent = File.ReadAllLines(expectedFilePath);
            var actualContent = File.ReadAllLines(actualFilePath);

            Assert.AreEqual(expectedContent.Length, actualContent.Length);

            var isEquals = true;
            for (var i = 0; i < expectedContent.Length; i++)
            {
                if (i == excludeLineNum)
                {
                    continue;
                }

                if (expectedContent[i] != actualContent[i])
                {
                    isEquals = false;
                    break;
                }
            }

            Assert.IsTrue(isEquals);
        }
    }
}
