using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// C#からJavaScriptのメソッドを呼び出すためのオブジェクトクラス
    /// </summary>
    public class CsToJs
    {
        /// <summary>
        /// 遅延初期化インスタンス
        /// </summary>
        private static readonly Lazy<CsToJs> InstanceValue = new Lazy<CsToJs>(() => new CsToJs());

        /// <summary>
        /// WebView2コントロール
        /// </summary>
        private WebView2CompositionControl? webview2;

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
        private CsToJs()
        {
        }

        /// <summary>
        /// インスタンス
        /// </summary>
        internal static CsToJs Instance => InstanceValue.Value;

        /// <summary>
        /// ベースフォルダパスを設定します。
        /// </summary>
        /// <param name="path">シミュレーションデータフォルダパス</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task SetBaseDataFolderPath(string path)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var jsonStr = JsonSerializer.Serialize(path.Replace("\\", "/"));
            await this.webview2.ExecuteScriptAsync($"setBaseDataFolderPath({jsonStr})");
        }

        /// <summary>
        /// シミュレーション実行中かどうかを設定します。
        /// </summary>
        /// <param name="isRunning">シミュレーション実行中かどうか</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task SetSimulationRunningStatus(bool isRunning)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var jsonStr = JsonSerializer.Serialize(isRunning);
            await this.webview2.ExecuteScriptAsync($"setSimulationRunningStatus({jsonStr})");
        }

        /// <summary>
        /// シミュレーション完了済みかどうかを設定します。
        /// </summary>
        /// <param name="isCompleated">シミュレーション完了済みかどうか</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task SetSimulationCompleatedStatus(bool isCompleated)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var jsonStr = JsonSerializer.Serialize(isCompleated);
            await this.webview2.ExecuteScriptAsync($"setSimulationCompleatedStatus({jsonStr})");
        }

        /// <summary>
        /// シミュレーション範囲の選択を開始します。
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task StartMeshSelection()
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            await this.webview2.ExecuteScriptAsync("startMeshSelection()");
        }

        /// <summary>
        /// シミュレーション範囲の選択を終了します。
        /// </summary>
        /// <param name="isConfirm">シミュレーション範囲の選択を確定するかどうか（true = 確定、false = キャンセル）</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task<string[]> EndMeshSelection(bool isConfirm)
        {
            if (!this.CheckInitialize())
            {
                return [];
            }

            var jsonStr = JsonSerializer.Serialize(isConfirm);
            var result = await this.webview2.ExecuteScriptAsync($"endMeshSelection({jsonStr})");

            var selectedMeshNumbers = JsonSerializer.Deserialize<string[]>(result);

            if (selectedMeshNumbers == null)
            {
                return [];
            }

            return selectedMeshNumbers;
        }

        /// <summary>
        /// 出火点を削除します。
        /// </summary>
        /// <param name="deleteNumber">削除する出火点の番号</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task DeleteFirePoint(int deleteNumber)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var jsonStr = JsonSerializer.Serialize(deleteNumber);
            await this.webview2.ExecuteScriptAsync($"deleteFirePoint({jsonStr})");
        }

        /// <summary>
        /// 出火点を削除します。
        /// </summary>
        /// <param name="deleteNumbers">削除する出火点の番号のリスト</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task DeleteFirePoints(int[] deleteNumbers)
        {
            if (!this.CheckInitialize())
            {
                return;
            }

            var jsonStr = JsonSerializer.Serialize(deleteNumbers);
            await this.webview2.ExecuteScriptAsync($"deleteFirePoints({jsonStr})");
        }

        /// <summary>
        /// 初期化します。
        /// </summary>
        /// <param name="webview2">WebView2コントロール</param>
        /// <exception cref="InvalidOperationException">このインスタンスがすでに初期化されている場合に発生します。</exception>
        internal void Initialize(WebView2CompositionControl webview2)
        {
            ArgumentNullException.ThrowIfNull(webview2);

            if (this.isInitialized)
            {
                // プログラムの不備なので落とす
                throw new InvalidOperationException("初期化済み");
            }

            this.webview2 = webview2;
            this.isInitialized = true;
        }

        /// <summary>
        /// 初期化済みかどうかを検証します。
        /// </summary>
        /// <returns>初期化済みかどうか</returns>
        /// <exception cref="InvalidOperationException">このインスタンスが初期化されていない場合に発生します。</exception>
        private bool CheckInitialize()
        {
            if (!this.isInitialized || this.webview2 == null)
            {
                // プログラムの不備なので落とす
                throw new InvalidOperationException("初期化されていない");
            }

            return this.isInitialized;
        }
    }
}
