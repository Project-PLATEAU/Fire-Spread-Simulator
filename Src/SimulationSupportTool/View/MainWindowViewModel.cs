using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationCommonLibrary.Utility;
using SimulationSupportTool.Controller;
using SimulationSupportTool.Model;
using SimulationSupportTool.MVVM;

namespace SimulationSupportTool.View
{
    /// <summary>
    /// MainWindowのViewModel
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// 風向・風速の設定可能最大件数
        /// </summary>
        private readonly int windConditionMaxCount = 50;

        /// <summary>
        /// 出火点のDataGrid
        /// </summary>
        private readonly DataGrid firePointDataGrid;

        /// <summary>
        /// 風向・風速のDataGrid
        /// </summary>
        private readonly DataGrid windConditionDataGrid;

        /// <summary>
        /// シミュレーションデータフォルダパス
        /// </summary>
        [ObservableProperty]
        private string inputSimulationSourceFolderPath = string.Empty;

        /// <summary>
        /// GISデータ出力フォルダパス
        /// </summary>
        [ObservableProperty]
        private string outputGisDataFolderPath = string.Empty;

        /// <summary>
        /// ユーザー入力シミュレーション時間（時間）
        /// </summary>
        [ObservableProperty]
        private string simulationTimeHour = "1";

        /// <summary>
        /// ユーザー入力シミュレーション時間（分）
        /// </summary>
        [ObservableProperty]
        private string simulationTimeMinute = "0";

        /// <summary>
        /// シミュレーション時間（単位：分）
        /// </summary>
        [ObservableProperty]
        private int simulationTimeTotalMinutes = 60;

        /// <summary>
        /// KMLファイルを出力するかどうか
        /// </summary>
        [ObservableProperty]
        private bool isOutputKml = true;

        /// <summary>
        /// CZMLファイルを出力するかどうか
        /// </summary>
        [ObservableProperty]
        private bool isOutputCzml = true;

        /// <summary>
        /// 建物延焼情報ファイル（CZMLファイル）を出力するかどうか
        /// </summary>
        [ObservableProperty]
        private bool isOutputCzmlBuilding = true;

        /// <summary>
        /// 延焼経路情報ファイル（CZMLファイル）を出力するかどうか
        /// </summary>
        [ObservableProperty]
        private bool isOutputCzmlFirePath = true;

        /// <summary>
        /// CZMLファイルの高さを楕円体高にするかどうか<br/>
        /// true = 楕円体高、false = 標高
        /// </summary>
        [ObservableProperty]
        private bool isEllipsoidHeight = true;

        /// <summary>
        /// 出火点の設定のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string firePointListErrorMessage = string.Empty;

        /// <summary>
        /// シミュレーション時間の設定のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string simulationTimeErrorMessage = string.Empty;

        /// <summary>
        /// 風向・風速の設定のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string windConditionListErrorMessage = string.Empty;

        /// <summary>
        /// 出力GISデータの設定のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string outputSettingErrorMessage = string.Empty;

        /// <summary>
        /// シミュレーション範囲編集中かどうか
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartSimulationRangeEditingCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmSimulationRangeEditingCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelSimulationRangeEditingCommand))]
        private bool isSimulationRangeEditing = false;

        /// <summary>
        /// 出火点編集中かどうか
        /// </summary>
        [ObservableProperty]
        private bool isFirePointEditing = false;

        /// <summary>
        /// 進捗メッセージ
        /// </summary>
        [ObservableProperty]
        private string progressMessage = string.Empty;

        /// <summary>
        /// 進捗メッセージサブ
        /// </summary>
        [ObservableProperty]
        private string progressSubMessage = string.Empty;

        /// <summary>
        /// シミュレーション実行中かどうか
        /// </summary>
        [ObservableProperty]
        private bool isSimulationRunning = false;

        /// <summary>
        /// シミュレーション完了済かどうか
        /// </summary>
        [ObservableProperty]
        private bool isSimulationCompleated = false;

        /// <summary>
        /// シミュレーション中止中かどうか
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelSimulationCommand))]
        private bool isSimulationCancelling = false;

        /// <summary>
        /// 画面が初期化済みかどうか
        /// </summary>
        [ObservableProperty]
        private bool isInitializedWindow = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="firePointDataGrid">出火点のDataGrid</param>
        /// <param name="windConditionDataGrid">風向・風速のDataGrid</param>
        public MainWindowViewModel(DataGrid firePointDataGrid, DataGrid windConditionDataGrid)
        {
            this.firePointDataGrid = firePointDataGrid;
            this.windConditionDataGrid = windConditionDataGrid;

            // 追加ボタン用の1行を追加しておく
            this.WindConditionList.Add(new WindCondition(0));
        }

        #region プロパティ

        /// <summary>
        /// 出火点のリスト
        /// </summary>
        public ObservableCollection<FirePoint> FirePointList { get; set; } = [];

        /// <summary>
        /// 風向・風速のリスト
        /// </summary>
        public ObservableCollection<WindCondition> WindConditionList { get; set; } = [];

        /// <summary>
        /// ウィンドウをアクティブにするAction
        /// </summary>
        internal Action? WindowActivate { get; set; }

        /// <summary>
        /// スクロールを一番下に移動するAction
        /// </summary>
        internal Action? ScrollToBottom { get; set; }

        /// <summary>
        /// Not シミュレーション範囲編集中かどうか
        /// </summary>
        private bool IsNotSimulationRangeEditing => !this.IsSimulationRangeEditing;

        /// <summary>
        /// Not シミュレーション中止中かどうか
        /// </summary>
        private bool IsNotSimulationCancelling => !this.IsSimulationCancelling;

        /// <summary>
        /// 選択中のシミュレーション範囲のメッシュ番号のリスト
        /// </summary>
        private string[] SelectedSimulationRangeMeshNumbers { get; set; } = [];

        #endregion プロパティ

        /// <summary>
        /// ウィンドウが閉じられる直前に呼び出されます。
        /// </summary>
        /// <param name="e">イベント情報</param>
        internal void WindowClosing(CancelEventArgs e)
        {
            if (!this.IsSimulationRunning)
            {
                return;
            }

            if (!MessageBoxUtility.ShowQuestion("シミュレーションを中止してアプリケーションを終了します。\r\nよろしいですか？", Properties.Resources.WindowTitle))
            {
                // 終了をキャンセル
                e.Cancel = true;
                return;
            }

            this.CancelSimulation();
        }

        /// <summary>
        /// 出火点を追加します。
        /// </summary>
        /// <param name="firePointResult">JavaScriptの出火点</param>
        internal void AddFirePoint(JsFirePointResult firePointResult)
        {
            if (!firePointResult.IsPointInSimulationRange)
            {
                MessageBoxUtility.ShowWarning("出火点を設定できません。\r\nシミュレーション範囲内をクリックしてください。", Properties.Resources.WindowTitle);
                return;
            }

            if (firePointResult.Building == null)
            {
                MessageBoxUtility.ShowWarning("出火点を設定できません。\r\n建物をクリックしてください。", Properties.Resources.WindowTitle);
                return;
            }

            var firePoint = FirePoint.CreateFromResult(firePointResult);

            this.FirePointList.Add(firePoint);

            // Dispatcher を使って UI スレッドでスクロール処理
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    this.firePointDataGrid.ScrollIntoView(this.FirePointList.Last());
                }), DispatcherPriority.Background);
        }

        /// <summary>
        /// 出火点のリストを更新します。
        /// </summary>
        /// <param name="firePointResults">JavaScriptの出火点のリスト</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        internal async Task UpdateFirePointsAsync(List<JsFirePointResult> firePointResults)
        {
            var deleteNoList = new List<int>();
            foreach (var firePointResult in firePointResults)
            {
                var firePoint = this.FirePointList.Where(f => f.No == firePointResult.No).FirstOrDefault();

                if (firePoint == null)
                {
                    // ここには来ないはずだが念のためチェックを行う
                    App.Logger.Error($"番号に対応する出火点がない firePointResult.No = {firePointResult.No}");
                    continue;
                }

                if (!firePointResult.IsPointInSimulationRange)
                {
                    MessageBoxUtility.ShowWarning($"番号 {firePointResult.No} の出火点はシミュレーション\r\n範囲外です。削除されます。", Properties.Resources.WindowTitle);

                    this.FirePointList.Remove(firePoint);
                    deleteNoList.Add(firePoint.No);
                    continue;
                }

                if (firePointResult.Building == null)
                {
                    MessageBoxUtility.ShowWarning($"番号 {firePointResult.No} の出火点は建物ではありません。\r\n削除されます。", Properties.Resources.WindowTitle);

                    this.FirePointList.Remove(firePoint);
                    deleteNoList.Add(firePoint.No);
                    continue;
                }

                var newFirePoint = FirePoint.CreateFromResult(firePointResult);

                if (firePoint.EqualsCoordinate(newFirePoint))
                {
                    // 位置の変更なしは何もしない
                    continue;
                }

                var index = this.FirePointList.IndexOf(firePoint);
                this.FirePointList[index] = newFirePoint;
            }

            // 番号の振り直し
            for (var i = 0; i < this.FirePointList.Count; i++)
            {
                this.FirePointList[i].No = i + 1;
            }

            await CsToJs.Instance.DeleteFirePoints(deleteNoList.ToArray());
        }

        /// <summary>
        /// 入力内容をチェックします。
        /// </summary>
        /// <returns>成否</returns>
        private bool Validate()
        {
            var result = true;
            this.FirePointListErrorMessage = string.Empty;
            this.SimulationTimeErrorMessage = string.Empty;
            this.WindConditionListErrorMessage = string.Empty;
            this.OutputSettingErrorMessage = string.Empty;

            // 出火点の設定のチェック
            if (!this.FirePointList.Any())
            {
                result = false;
                this.FirePointListErrorMessage = "出火点は必ず指定してください。";
            }
            else
            {
                if (this.FirePointList.Where(w => w.HasErrorStartMinutes).Any())
                {
                    result = false;
                    this.FirePointListErrorMessage = "出火時間はシミュレーション時間の範囲内の数値で指定してください。";
                }
            }

            // シミュレーション時間の設定のチェック
            if (this.SimulationTimeTotalMinutes.Equals(int.MaxValue))
            {
                result = false;
                this.SimulationTimeErrorMessage = "シミュレーション時間は0時間1分～48時間59分の間で数値で指定してください。";
            }

            // 風向・風速の設定のチェック
            if (this.WindConditionList.Any())
            {
                var errors = new List<string>();
                if (this.WindConditionList.Where(w => w.HasErrorStartMinutes).Any())
                {
                    errors.Add("時間はシミュレーション時間の範囲内の数値で指定してください。");
                }

                if (this.WindConditionList.Where(w => w.HasErrorWindDirection).Any())
                {
                    errors.Add("風向は 0[°]～359.9[°] の範囲内の数値で指定してください。");
                }

                if (this.WindConditionList.Where(w => w.HasErrorWindSpeed).Any())
                {
                    errors.Add("風速は 0[m/s]～25.0[m/s] の範囲内の数値で指定してください。");
                }

                if (errors.Count > 0)
                {
                    result = false;

                    if (errors.Count == 1)
                    {
                        this.WindConditionListErrorMessage = errors[0];
                    }
                    else
                    {
                        this.WindConditionListErrorMessage = $"・{string.Join("\r\n・", errors)}";
                    }
                }
            }

            // 出力GISデータの設定のチェック
            {
                var errors = new List<string>();

                if (!this.IsOutputKml && !this.IsOutputCzml)
                {
                    errors.Add("出力対象を一つ以上は選択してください。");
                }

                if (this.IsOutputCzml && !this.IsOutputCzmlBuilding && !this.IsOutputCzmlFirePath)
                {
                    errors.Add("建物または延焼経路を一つ以上は選択してください。");
                }

                if (string.IsNullOrEmpty(this.OutputGisDataFolderPath))
                {
                    errors.Add("出力フォルダは必ず指定してください。");
                }

                if (errors.Count > 0)
                {
                    result = false;

                    if (errors.Count == 1)
                    {
                        this.OutputSettingErrorMessage = errors[0];
                    }
                    else
                    {
                        this.OutputSettingErrorMessage = $"・{string.Join("\r\n・", errors)}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// シミュレーションを実行します。
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/>（成否）</returns>
        private async Task<bool> ExecuteAsync()
        {
            this.ProgressMessage = "シミュレーション準備中...";
            this.ProgressSubMessage = string.Empty;

            var progress = new Progress<(string, string)>(
                (messages) =>
                {
                    this.ProgressMessage = messages.Item1;
                    this.ProgressSubMessage = messages.Item2;
                });

            var result = await SimulationExecutor.Instance.ExecuteAsync(
                this.InputSimulationSourceFolderPath,
                this.OutputGisDataFolderPath,
                this.SimulationTimeTotalMinutes,
                this.SelectedSimulationRangeMeshNumbers,
                this.FirePointList.ToList(),
                this.WindConditionList.ToList(),
                this.IsOutputKml,
                this.IsOutputCzml && this.IsOutputCzmlBuilding,
                this.IsOutputCzml && this.IsOutputCzmlFirePath,
                this.IsEllipsoidHeight,
                progress);

            return result;
        }

        #region コマンド

        /// <summary>
        /// シミュレーションデータフォルダパス選択時のコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand]
        private async Task SelectInputFolderPathAsync()
        {
            var defaultPath = ConfigFileManager.GetValue(ConfigFileManager.InputFolderPath);
            if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }

            // フォルダ選択ダイアログを準備
            var dialog = new Microsoft.Win32.OpenFolderDialog()
            {
                Title = "フォルダ選択",
                Multiselect = false,
                InitialDirectory = defaultPath,
            };

            // フォルダ選択ダイアログを表示
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var folderPath = dialog.FolderName;

            if (!folderPath.Equals(this.InputSimulationSourceFolderPath)
                && (this.SelectedSimulationRangeMeshNumbers.Length != 0 || this.FirePointList.Count() != 0))
            {
                if (!MessageBoxUtility.ShowQuestion("データフォルダを変更するとシミュレーション範囲の設定\r\nおよび出火点の設定がクリアされます。\r\nよろしいですか？", Properties.Resources.WindowTitle))
                {
                    // 何もしない
                    return;
                }
            }

            var creator = new TertiaryMeshFileCreator();
            if (!creator.Create(folderPath))
            {
                MessageBoxUtility.ShowWarning("選択したフォルダに有効なファイルが含まれていません。", Properties.Resources.WindowTitle);
                return;
            }

            this.InputSimulationSourceFolderPath = dialog.FolderName;

            // ベースフォルダパスの設定（地図上のクリアもパスの設定時に実施される）
            await CsToJs.Instance.SetBaseDataFolderPath(this.InputSimulationSourceFolderPath);

            // リストのクリア
            this.SelectedSimulationRangeMeshNumbers = [];
            this.FirePointList.Clear();

            ConfigFileManager.SetValue(ConfigFileManager.InputFolderPath, this.InputSimulationSourceFolderPath);
        }

        /// <summary>
        /// GISデータ出力フォルダパス選択時のコマンド
        /// </summary>
        [RelayCommand]
        private void SelectOutputFolderPath()
        {
            var defaultPath = ConfigFileManager.GetValue(ConfigFileManager.OutputFolderPath);
            if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }

            // フォルダ選択ダイアログを準備
            var dialog = new Microsoft.Win32.OpenFolderDialog()
            {
                Title = "フォルダ選択",
                Multiselect = false,
                InitialDirectory = defaultPath,
            };

            // フォルダ選択ダイアログを表示
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var folderPath = dialog.FolderName;

            var currentPath = Directory.GetCurrentDirectory();
            if (folderPath.StartsWith(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxUtility.ShowWarning("アプリケーションフォルダ内のフォルダは選択できません。", Properties.Resources.WindowTitle);
                return;
            }

            this.OutputGisDataFolderPath = dialog.FolderName;

            ConfigFileManager.SetValue(ConfigFileManager.OutputFolderPath, this.OutputGisDataFolderPath);
        }

        /// <summary>
        /// シミュレーション範囲の選択を開始するコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand(CanExecute = nameof(this.IsNotSimulationRangeEditing))]
        private async Task StartSimulationRangeEditingAsync()
        {
            if (string.IsNullOrEmpty(this.InputSimulationSourceFolderPath))
            {
                MessageBoxUtility.ShowWarning("シミュレーション範囲を選択できません。\r\nデータフォルダを選択してください。", Properties.Resources.WindowTitle);
                return;
            }

            this.IsSimulationRangeEditing = true;

            await CsToJs.Instance.StartMeshSelection();
        }

        /// <summary>
        /// シミュレーション範囲の選択を確定するコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand(CanExecute = nameof(this.IsSimulationRangeEditing))]
        private async Task ConfirmSimulationRangeEditingAsync()
        {
            var selectedMeshNumbers = await CsToJs.Instance.EndMeshSelection(true);

            this.SelectedSimulationRangeMeshNumbers = selectedMeshNumbers;

            this.IsSimulationRangeEditing = false;
        }

        /// <summary>
        /// シミュレーション範囲の選択をキャンセルするコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand(CanExecute = nameof(this.IsSimulationRangeEditing))]
        private async Task CancelSimulationRangeEditingAsync()
        {
            await CsToJs.Instance.EndMeshSelection(false);

            this.IsSimulationRangeEditing = false;
        }

        /// <summary>
        /// 出火点を削除するコマンド
        /// </summary>
        /// <param name="firePoint">削除対象の出火点</param>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand]
        private async Task DeleteFirePointAsync(FirePoint firePoint)
        {
            if (!MessageBoxUtility.ShowQuestion($"番号 {firePoint.No} の出火点を削除します。\r\nよろしいですか？", Properties.Resources.WindowTitle))
            {
                // 何もしない
                return;
            }

            // 地図上から削除
            await CsToJs.Instance.DeleteFirePoint(firePoint.No);

            // リストから削除
            this.FirePointList.Remove(firePoint);

            // 番号の振り直し
            for (var i = 0; i < this.FirePointList.Count; i++)
            {
                this.FirePointList[i].No = i + 1;
            }
        }

        /// <summary>
        /// 風向・風速を追加するコマンド
        /// </summary>
        [RelayCommand]
        private void AddWindCondition()
        {
            var maxNo = 0;
            if (this.WindConditionList.Any())
            {
                maxNo = this.WindConditionList.Max(item => item.No);
            }

            if (this.windConditionMaxCount <= maxNo)
            {
                MessageBoxUtility.ShowWarning($"風向・風速を設定できません。\r\n最大件数（{this.windConditionMaxCount}件）に達しています。", Properties.Resources.WindowTitle);
                return;
            }

            this.WindConditionList.Insert(this.WindConditionList.Count - 1, new WindCondition(maxNo + 1));
            this.ScrollToBottom?.Invoke();

            if (this.windConditionMaxCount <= maxNo + 1)
            {
                MessageBoxUtility.ShowInformation($"風向・風速の設定可能最大件数（{this.windConditionMaxCount}件）に達しました。", Properties.Resources.WindowTitle);
                return;
            }

            // Dispatcher を使って UI スレッドでスクロール処理
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    this.windConditionDataGrid.ScrollIntoView(this.WindConditionList.Last());
                }), DispatcherPriority.Background);
        }

        /// <summary>
        /// 風向・風速を削除するコマンド
        /// </summary>
        /// <param name="windCondition">削除対象の風向・風速</param>
        [RelayCommand]
        private void DeleteWindCondition(WindCondition windCondition)
        {
            if (windCondition.No == 0)
            {
                // 追加ボタン用の1行は削除しない
                // ここには来ないはずだが念のためチェックを行う
                return;
            }

            this.WindConditionList.Remove(windCondition);

            // 番号の振り直し
            for (var i = 0; i < this.WindConditionList.Count; i++)
            {
                if (this.WindConditionList[i].No == 0)
                {
                    // 追加ボタン用の1行は変更しない
                    continue;
                }

                this.WindConditionList[i].No = i + 1;
            }
        }

        /// <summary>
        /// シミュレーション時間変更のコマンド
        /// </summary>
        [RelayCommand]
        private void SimulationTimeChanged()
        {
            var hourError = string.Empty;
            if (string.IsNullOrEmpty(this.SimulationTimeHour)
                || !int.TryParse(this.SimulationTimeHour, out var hour)
                || hour < 0 || 48 < hour)
            {
                hour = int.MinValue;
                hourError = "時間不正";
            }

            var minuteError = string.Empty;
            if (string.IsNullOrEmpty(this.SimulationTimeMinute)
                || !int.TryParse(this.SimulationTimeMinute, out var minute)
                || minute < 0 || 59 < minute)
            {
                minute = int.MinValue;
                minuteError = "分不正";
            }

            if (hour == 0 && minute == 0)
            {
                hourError = "時間不正";
                minuteError = "分不正";
            }

            this.UpdateError(nameof(this.SimulationTimeHour), hourError);
            this.UpdateError(nameof(this.SimulationTimeMinute), minuteError);

            // 合計の時間を更新
            if (string.IsNullOrEmpty(hourError) && string.IsNullOrEmpty(minuteError))
            {
                this.SimulationTimeTotalMinutes = (hour * 60) + minute;
            }
            else
            {
                this.SimulationTimeTotalMinutes = int.MaxValue;
            }

            // リストのバリデーションを実行（出火点）
            foreach (var f in this.FirePointList)
            {
                f.StartMinutesChanged(this.SimulationTimeTotalMinutes);
            }

            // リストのバリデーションを実行（風向・風速）
            foreach (var w in this.WindConditionList)
            {
                w.StartMinutesChanged(this.SimulationTimeTotalMinutes);
            }
        }

        /// <summary>
        /// 「風向の設定」ダイアログを開くコマンド
        /// </summary>
        /// <param name="windCondition">編集対象の風向・風速</param>
        [RelayCommand]
        private void OpenWindDirectionSettingDialog(WindCondition windCondition)
        {
            var vm = new WindDirectionSettingDialogViewModel(windCondition);
            var dialog = new WindDirectionSettingDialog(vm)
            {
                Owner = Application.Current.MainWindow,
            };

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value)
            {
                windCondition.WindDirection = vm.GetResult();
            }
        }

        /// <summary>
        /// シミュレーションを実行するコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand]
        private async Task StartSimulationAsync()
        {
            if (!this.Validate())
            {
                return;
            }

            if (!MessageBoxUtility.ShowQuestion("この入力内容でシミュレーションを実行します。\r\nよろしいですか？", Properties.Resources.WindowTitle))
            {
                // 何もしない
                return;
            }

            this.IsSimulationRunning = true;
            await CsToJs.Instance.SetSimulationRunningStatus(true);

            // 実行
            var isSuccess = await this.ExecuteAsync();

            this.IsSimulationRunning = false;
            await CsToJs.Instance.SetSimulationRunningStatus(false);

            if (this.IsSimulationCancelling)
            {
                this.IsSimulationCancelling = false;
                this.WindowActivate?.Invoke();
                MessageBoxUtility.ShowInformation("シミュレーションを中止しました。", Properties.Resources.WindowTitle);
                return;
            }

            if (!isSuccess)
            {
                this.WindowActivate?.Invoke();
                MessageBoxUtility.ShowError("シミュレーションに失敗しました。", Properties.Resources.WindowTitle);
                return;
            }

            this.IsSimulationCompleated = true;
            await CsToJs.Instance.SetSimulationCompleatedStatus(true);

            this.WindowActivate?.Invoke();
            this.ProgressMessage = "シミュレーション完了";
            MessageBoxUtility.ShowInformation("シミュレーションが完了しました。", Properties.Resources.WindowTitle);
        }

        /// <summary>
        /// シミュレーションを中止するコマンド
        /// </summary>
        [RelayCommand(CanExecute = nameof(this.IsNotSimulationCancelling))]
        private void CancelSimulation()
        {
            this.IsSimulationCancelling = true;
            this.ProgressMessage = "中止しています...";
            this.ProgressSubMessage = string.Empty;
            SimulationExecutor.Instance.Cancel();
        }

        /// <summary>
        /// シミュレーションを完了するコマンド
        /// </summary>
        /// <returns>非同期操作を表す <see cref="Task"/></returns>
        [RelayCommand]
        private async Task FinishSimulationAsync()
        {
            this.IsSimulationCompleated = false;
            await CsToJs.Instance.SetSimulationCompleatedStatus(false);
        }

        /// <summary>
        /// GISデータ出力フォルダパスをエクスプローラーで開くコマンド
        /// </summary>
        [RelayCommand]
        private void OpenResultFolder()
        {
            Process.Start("explorer.exe", this.OutputGisDataFolderPath);
        }

        #endregion コマンド
    }
}
