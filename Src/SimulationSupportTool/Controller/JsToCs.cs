using System.Text.Json;
using SimulationCommonLibrary.Utility;
using SimulationSupportTool.Model;
using SimulationSupportTool.View;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// JavaScriptからC#のメソッドを呼び出すためのホストオブジェクト
    /// </summary>
    public class JsToCs
    {
        /// <summary>
        /// 遅延初期化インスタンス
        /// </summary>
        private static readonly Lazy<JsToCs> InstanceValue = new Lazy<JsToCs>(() => new JsToCs());

        /// <summary>
        /// MainWindowのViewModel
        /// </summary>
        private MainWindowViewModel? mainWindowViewModel;

        /// <summary>
        /// 初期化済みかどうか
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// シングルトンパターンで利用するため private にして外部からのインスタンス化を禁止します。
        /// </remarks>
        private JsToCs()
        {
        }

        /// <summary>
        /// インスタンス
        /// </summary>
        internal static JsToCs Instance => InstanceValue.Value;

        /// <summary>
        /// 出火点編集中かどうかを設定します。
        /// </summary>
        /// <param name="arg">JavaScriptからの値</param>
        public void SetFirePointEditingStatus(string arg)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var isEditing = JsonSerializer.Deserialize<bool>(arg);
            this.mainWindowViewModel.IsFirePointEditing = isEditing;
        }

        /// <summary>
        /// 出火点を追加します。
        /// </summary>
        /// <param name="arg">JavaScriptからの値</param>
        public void AddFirePoint(string arg)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var firePointResult = JsonSerializer.Deserialize<JsFirePointResult>(arg);

            if (firePointResult == null)
            {
                App.Logger.Error($"jsからの引数が不正 arg = {arg}");
                return;
            }

            this.mainWindowViewModel.AddFirePoint(firePointResult);
        }

        /// <summary>
        /// 出火点のリストを更新します。
        /// </summary>
        /// <param name="arg">JavaScriptからの値</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        public async Task UpdateFirePoints(string arg)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var firePointResults = JsonSerializer.Deserialize<List<JsFirePointResult>>(arg);

            if (firePointResults == null)
            {
                App.Logger.Error($"jsからの引数が不正 arg = {arg}");
                return;
            }

            await this.mainWindowViewModel.UpdateFirePointsAsync(firePointResults);
        }

        /// <summary>
        /// 情報メッセージボックスを表示します。
        /// </summary>
        /// <param name="arg">JavaScriptからの値</param>
        public void ShowInformationMessageBox(string arg)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var message = JsonSerializer.Deserialize<string>(arg);

            if (string.IsNullOrEmpty(message))
            {
                App.Logger.Error($"jsからの引数が不正 arg = {arg}");
                return;
            }

            MessageBoxUtility.ShowInformation(message, Properties.Resources.WindowTitle);
        }

        /// <summary>
        /// 警告メッセージボックスを表示します。
        /// </summary>
        /// <param name="arg">JavaScriptからの値</param>
        public void ShowWarningMessageBox(string arg)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var message = JsonSerializer.Deserialize<string>(arg);

            if (string.IsNullOrEmpty(message))
            {
                App.Logger.Error($"jsからの引数が不正 arg = {arg}");
                return;
            }

            MessageBoxUtility.ShowWarning(message, Properties.Resources.WindowTitle);
        }

        /// <summary>
        /// 初期化します。
        /// </summary>
        /// <param name="mainWindowViewModel">MainWindowのViewModel</param>
        /// <exception cref="InvalidOperationException">このインスタンスがすでに初期化されている場合に発生します。</exception>
        internal void Initialize(MainWindowViewModel mainWindowViewModel)
        {
            ArgumentNullException.ThrowIfNull(mainWindowViewModel);

            if (this.isInitialized)
            {
                // プログラムの不備なので落とす
                throw new InvalidOperationException("初期化済み");
            }

            this.mainWindowViewModel = mainWindowViewModel;
            this.isInitialized = true;
        }

        /// <summary>
        /// 初期化済みかどうかを検証します。
        /// </summary>
        /// <returns>初期化済みかどうか</returns>
        /// <exception cref="InvalidOperationException">このインスタンスが初期化されていない場合に発生します。</exception>
        private bool CheckInitialize()
        {
            if (!this.isInitialized || this.mainWindowViewModel == null)
            {
                // プログラムの不備なので落とす
                throw new InvalidOperationException("初期化されていない");
            }

            return this.isInitialized;
        }
    }
}
