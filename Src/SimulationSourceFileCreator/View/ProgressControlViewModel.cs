using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// ProgressControlのViewModel
    /// </summary>
    public partial class ProgressControlViewModel : ObservableObject
    {
        /// <summary>
        /// 非同期で実行する処理のAction
        /// </summary>
        private readonly Action<IProgress<(int, int, string, string)>> action;

        /// <summary>
        /// 中止時に呼び出されるAction
        /// </summary>
        private readonly Action cancelAction;

        /// <summary>
        /// キャンセルトークン
        /// </summary>
        private readonly CancellationTokenSource cancelToken;

        /// <summary>
        /// 処理の進捗通知プロパティ
        /// </summary>
        private readonly IProgress<(int, int, string, string)> progress;

        /// <summary>
        /// プログレスバーの最大値
        /// </summary>
        [ObservableProperty]
        private int progressMaxValue = 0;

        /// <summary>
        /// プログレスバーの現在の値
        /// </summary>
        [ObservableProperty]
        private int progressValue = 0;

        /// <summary>
        /// 進捗表示メッセージ（メイン）
        /// </summary>
        [ObservableProperty]
        private string progressMessage = string.Empty;

        /// <summary>
        /// 進捗表示メッセージ（サブ）
        /// </summary>
        [ObservableProperty]
        private string progressSubMessage = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="action">非同期で実行する処理のAction</param>
        /// <param name="cancelAction">中止時に呼び出されるAction</param>
        /// <param name="cancelToken">キャンセルトークン</param>
        public ProgressControlViewModel(Action<IProgress<(int, int, string, string)>> action, Action cancelAction, CancellationTokenSource cancelToken)
        {
            this.action = action;
            this.cancelAction = cancelAction;
            this.cancelToken = cancelToken;
            this.progress = new Progress<(int maxCout, int count, string message, string messageSub)>(
                (parameters) =>
                {
                    if (parameters.maxCout != 0 && parameters.count != 0)
                    {
                        this.ProgressMaxValue = parameters.maxCout;
                        this.ProgressValue = parameters.count;
                    }

                    this.ProgressMessage = parameters.message;
                    this.ProgressSubMessage = parameters.messageSub;
                });
        }

        /// <summary>
        /// 非同期で処理を実行します。
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        public async Task ExecuteAsync()
        {
            await Task.Run(() => this.DoWork());
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        private void DoWork()
        {
            if (this.action == null)
            {
                return;
            }

            this.action.Invoke(this.progress);
        }

        #region コマンド

        /// <summary>
        /// 処理を中止するコマンド
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            this.progress.Report((0, 0, "中止しています...", string.Empty));
            this.cancelToken.Cancel();
            this.cancelAction?.Invoke();
        }

        #endregion コマンド
    }
}
