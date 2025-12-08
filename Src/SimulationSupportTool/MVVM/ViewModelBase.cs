using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimulationSupportTool.MVVM
{
    /// <summary>
    /// MVVM パターンにおける ViewModel の基本機能を提供する基底クラス<br/>
    /// プロパティ変更通知 (<see cref="ObservableObject"/>) と<br/>
    /// 入力検証エラー通知 (<see cref="INotifyDataErrorInfo"/>) をサポートする。<br/>
    /// </summary>
    public partial class ViewModelBase : ObservableObject, INotifyDataErrorInfo
    {
        /// <summary>
        /// プロパティ名とエラー内容のディクショナリ
        /// </summary>
        private readonly Dictionary<string, string> currentErrors = [];

        /// <inheritdoc/>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <inheritdoc/>
        public bool HasErrors => this.currentErrors.Count > 0;

        /// <inheritdoc/>
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !this.currentErrors.TryGetValue(propertyName, out string? value))
            {
                return string.Empty;
            }

            return value;
        }

        /// <summary>
        /// エラーを更新します。
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="error">エラー内容</param>
        public void UpdateError(string propertyName, string error)
        {
            this.currentErrors.Remove(propertyName);

            if (!string.IsNullOrEmpty(error))
            {
                this.currentErrors.Add(propertyName, error);
            }

            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
