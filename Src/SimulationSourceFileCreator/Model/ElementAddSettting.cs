using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// 要素追加設定ファイルのモデルクラス
    /// </summary>
    public class ElementAddSettting
    {
        /// <summary>
        /// 取得要素設定（建物IDの設定）
        /// </summary>
        public GetElement? BldgId { get; set; } = null;

        /// <summary>
        /// 取得要素設定（データ補完対象項目）KOZO
        /// </summary>
        public ElementAddSetttingSupplementItem KOZO { get; set; } = new ElementAddSetttingSupplementItem();

        /// <summary>
        /// 取得要素設定（データ補完対象項目）MOKU
        /// </summary>
        public ElementAddSetttingSupplementItem MOKU { get; set; } = new ElementAddSetttingSupplementItem();

        /// <summary>
        /// 取得要素設定（データ補完対象項目）YOTO
        /// </summary>
        public ElementAddSetttingSupplementItem YOTO { get; set; } = new ElementAddSetttingSupplementItem();

        /// <summary>
        /// 取得要素設定
        /// </summary>
        public List<GetElement> GetElements { get; set; } = [];

        /// <summary>
        /// 削除要素設定
        /// </summary>
        public List<RemoveElement> RemoveElements { get; set; } = [];

        /// <summary>
        /// プレフィックスsimのuri
        /// </summary>
        public string SimNamespaceUri { get; set; } = string.Empty;

        /// <summary>
        /// プレフィックスsimのxsd
        /// </summary>
        public string SimNamespaceXsd { get; set; } = string.Empty;

        /// <summary>
        /// 追加要素設定の親要素
        /// </summary>
        public string AddParentElement { get; set; } = string.Empty;

        /// <summary>
        /// 追加要素設定
        /// </summary>
        public List<AddElement> AddElements { get; set; } = [];

        /// <summary>
        /// 初期化します。<br/>
        /// ファイルが無い場合に初期値で復元します。
        /// </summary>
        /// <param name="configFilePath">要素追加設定ファイルパス</param>
        internal static void Initialize(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                return;
            }

            var configFolderPath = Path.GetDirectoryName(configFilePath) ?? string.Empty; // 親フォルダ
            Directory.CreateDirectory(configFolderPath);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SimulationSourceFileCreator.Resources.ElementAddSettting.xml";

            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream != null)
                {
                    using (var outputStream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write))
                    {
                        inputStream.CopyTo(outputStream);
                    }
                }
            }

            App.Logger.Info($"configファイルを初期値で復元 configFilePath = {configFilePath}");
        }

        /// <summary>
        /// 要素追加設定ファイルを読み込みます。
        /// </summary>
        /// <param name="configFilePath">要素追加設定ファイルパス</param>
        /// <returns>要素追加設定</returns>
        internal static ElementAddSettting? Load(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                App.Logger.Error($"configファイルがない configFilePath = {configFilePath}");
                return null;
            }

            ElementAddSettting? setting = null;
            var serializer = new XmlSerializer(typeof(ElementAddSettting));

            try
            {
                using (var reader = new StreamReader(configFilePath))
                {
                    setting = serializer.Deserialize(reader) as ElementAddSettting;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error($"configファイルの読み込みに失敗 configFilePath = {configFilePath}", ex);
                return null;
            }

            if (setting == null)
            {
                App.Logger.Error($"configファイルの読み込みに失敗 configFilePath = {configFilePath}");
                return null;
            }

            var errors = setting.Check();
            if (0 < errors.Count)
            {
                App.Logger.Error($"configファイルの設定内容不備 errors = \r\n・{string.Join("\r\n・", errors)}");
                return null;
            }

            return setting;
        }

        /// <summary>
        /// 内容をチェックします。
        /// </summary>
        /// <returns>エラーメッセージ</returns>
        internal List<string> Check()
        {
            List<string> errors = [];

            // 建物IDの設定
            errors.Add(this.CheckGetElementSetting("建物IDの設定", this.BldgId, true));

            // 取得要素設定
            var keyCheckList = new List<string>();
            foreach (var getSetting in this.GetElements)
            {
                errors.Add(this.CheckGetElementSetting("取得要素設定", getSetting));

                if (keyCheckList.Contains(getSetting.KeyName ?? string.Empty))
                {
                    // キーが重複しています
                    errors.Add($"取得要素設定のKeyNameが重複（KeyName = {getSetting.KeyName}）");
                }

                keyCheckList.Add(getSetting.KeyName ?? string.Empty);
            }

            // 取得要素設定（データ補完対象項目）
            errors.Add(this.CheckGetElementSetting("取得要素設定（KOZOデータ補完対象項目[耐火構造]）", this.KOZO.Taika));
            errors.Add(this.CheckGetElementSetting("取得要素設定（KOZOデータ補完対象項目[建物構造]）", this.KOZO.Tatemono));
            errors.Add(this.CheckGetElementSetting("取得要素設定（KOZOデータ補完対象項目[地上階数]）", this.KOZO.Kaisu));
            errors.Add(this.CheckGetElementSetting("取得要素設定（KOZOデータ補完対象項目[延床面積]）", this.KOZO.Nobeyuka));
            errors.Add(this.CheckGetElementSetting("取得要素設定（KOZOデータ補完対象項目[建築面積]）", this.KOZO.Kenchiku));
            errors.Add(this.CheckGetElementSetting("取得要素設定（MOKUデータ補完対象項目[建物構造]）", this.MOKU.Tatemono));
            errors.Add(this.CheckGetElementSetting("取得要素設定（YOTOデータ補完対象項目[利用目的]）", this.YOTO.Mokuteki));

            // 削除要素設定
            foreach (var removeSetting in this.RemoveElements)
            {
                errors.Add(this.CheckRemoveElementSetting(removeSetting));
            }

            if (string.IsNullOrEmpty(this.SimNamespaceUri))
            {
                errors.Add("追加要素設定 : SimNamespaceUriの設定がない");
            }

            if (string.IsNullOrEmpty(this.SimNamespaceXsd))
            {
                // SimNamespaceXsdがEmptyはOK
            }

            if (string.IsNullOrEmpty(this.AddParentElement))
            {
                errors.Add("追加要素設定 : AddParentElementの設定がない");
            }

            // 追加要素設定
            foreach (var addSetting in this.AddElements)
            {
                errors.Add(this.CheckAddElementSetting(addSetting));
            }

            // 空文字などのItemを削除
            errors.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            return errors;
        }

        /// <summary>
        /// 取得要素設定のエラーメッセージを整形
        /// </summary>
        /// <param name="targetName">チェック対象</param>
        /// <param name="getSetting">取得要素設定</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns>整形済みエラーメッセージ</returns>
        private static string ErrorMessageGetSetting(string targetName, GetElement getSetting, string message)
            => $"{targetName} : {message}（KeyName = [{getSetting.KeyName}], TargetType = [{getSetting.TargetType}], TargetValue = [{getSetting.TargetValue}]）";

        /// <summary>
        /// 削除要素設定のエラーメッセージを整形
        /// </summary>
        /// <param name="targetName">チェック対象</param>
        /// <param name="removeSetting">削除要素設定</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns>整形済みエラーメッセージ</returns>
        private static string ErrorMessageRemoveSetting(string targetName, RemoveElement removeSetting, string message)
            => $"{targetName} : {message}（TagName = [{removeSetting.TagName}], CheckMinValue = [{removeSetting.CheckMinValue}], CheckMaxValue = [{removeSetting.CheckMaxValue}]）";

        /// <summary>
        /// 追加要素設定のエラーメッセージを整形
        /// </summary>
        /// <param name="targetName">チェック対象</param>
        /// <param name="addSetting">追加要素設定</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns>整形済みエラーメッセージ</returns>
        private static string ErrorMessageAddSetting(string targetName, AddElement addSetting, string message)
            => $"{targetName} : {message}（KeyName = [{addSetting.KeyName}], TagName = [{addSetting.TagName}], DefaultValue = [{addSetting.DefaultValue}]）";

        /// <summary>
        /// 取得要素設定の内容をチェックします。
        /// </summary>
        /// <param name="targetName">チェック対象（エラーメッセージに使用）</param>
        /// <param name="getSetting">取得要素設定</param>
        /// <param name="isBdgId">建物IDの取得要素設定かどうか</param>
        /// <returns>エラーメッセージ</returns>
        private string CheckGetElementSetting(string targetName, GetElement? getSetting, bool isBdgId = false)
        {
            if (getSetting == null)
            {
                return $"{targetName} : 設定がない";
            }

            if (!isBdgId && string.IsNullOrEmpty(getSetting.KeyName))
            {
                return ErrorMessageGetSetting(targetName, getSetting, "KeyNameがない");
            }

            if (string.IsNullOrEmpty(getSetting.TargetValue))
            {
                return ErrorMessageGetSetting(targetName, getSetting, "TargetValueがない");
            }

            switch (getSetting.TargetType)
            {
                case 0:
                    {
                        var index = getSetting.TargetValue.IndexOf(' ');
                        if (index < 0 || getSetting.TargetValue.Length <= index + 1)
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性名がない");
                        }

                        var tagName = getSetting.TargetValue.Substring(0, index);
                        if (tagName.StartsWith("bldg:Building"))
                        {
                            tagName = tagName.Substring(13);
                        }

                        getSetting.TagName = tagName;
                        getSetting.AttributeName = getSetting.TargetValue.Substring(index + 1);

                        if (string.IsNullOrEmpty(getSetting.AttributeName))
                        {
                            // TagName（要素名）がEmptyはOK
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性名がない");
                        }

                        break;
                    }

                case 1:
                    {
                        var indexTag = getSetting.TargetValue.IndexOf(' ');
                        if (indexTag < 0 || getSetting.TargetValue.Length <= indexTag + 1)
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性名がない");
                        }

                        var indexAttribute = getSetting.TargetValue.IndexOf('=');
                        if (indexAttribute < 0 || getSetting.TargetValue.Length <= indexAttribute + 1)
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性値がない");
                        }

                        if (indexAttribute < indexTag)
                        {
                            // ' 'と'='の順番が反転（'='が先にある）している
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性名がない");
                        }

                        getSetting.TagName = getSetting.TargetValue.Substring(0, indexTag);
                        getSetting.AttributeName = getSetting.TargetValue.Substring(indexTag + 1, indexAttribute - indexTag - 1);
                        getSetting.AttributeValue = getSetting.TargetValue.Substring(indexAttribute + 1).Trim('\"');

                        if (string.IsNullOrEmpty(getSetting.TagName))
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに要素名がない");
                        }

                        if (string.IsNullOrEmpty(getSetting.AttributeName))
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性名がない");
                        }

                        if (string.IsNullOrEmpty(getSetting.AttributeValue))
                        {
                            return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに属性値がない");
                        }

                        break;
                    }

                case 2:
                    getSetting.TagName = getSetting.TargetValue;

                    if (string.IsNullOrEmpty(getSetting.TagName))
                    {
                        return ErrorMessageGetSetting(targetName, getSetting, "TargetValueに要素名がない");
                    }

                    break;

                case 3:
                    getSetting.FixedValue = getSetting.TargetValue;
                    break;

                default:
                    return ErrorMessageGetSetting(targetName, getSetting, "TargetTypeが範囲外（0～3の整数以外）");
            }

            return string.Empty;
        }

        /// <summary>
        /// 削除要素設定の内容をチェックします。
        /// </summary>
        /// <param name="removeSetting">削除要素設定</param>
        /// <returns>エラーメッセージ</returns>
        private string CheckRemoveElementSetting(RemoveElement removeSetting)
        {
            if (string.IsNullOrEmpty(removeSetting.TagName))
            {
                return ErrorMessageRemoveSetting("削除要素設定", removeSetting, "TagNameがない");
            }

            return string.Empty;
        }

        /// <summary>
        /// 追加要素設定の内容をチェックします。
        /// </summary>
        /// <param name="addSetting">追加要素設定</param>
        /// <returns>エラーメッセージ</returns>
        private string CheckAddElementSetting(AddElement addSetting)
        {
            if (string.IsNullOrEmpty(addSetting.KeyName))
            {
                return ErrorMessageAddSetting("追加要素設定", addSetting, "KeyNameがない");
            }

            if (string.IsNullOrEmpty(addSetting.TagName))
            {
                return ErrorMessageAddSetting("追加要素設定", addSetting, "TagNameがない");
            }

            return string.Empty;
        }
    }
}
