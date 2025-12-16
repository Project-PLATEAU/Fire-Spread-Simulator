using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationSupportTool.Model;

namespace SimulationSupportTool.View
{
    /// <summary>
    /// WindDirectionSettingDialogのViewModel
    /// </summary>
    public partial class WindDirectionSettingDialogViewModel : ObservableObject
    {
        /// <summary>
        /// 方位テキストと角度のディクショナリ（key = 方位テキスト、value = 角度（単位：°））
        /// </summary>
        private readonly Dictionary<double, string> directionDict = [];

        /// <summary>
        /// 方位テキストのリスト
        /// </summary>
        private readonly string[] directionText =
        [
                "北",
                "北北東",
                "北東",
                "東北東",
                "東",
                "東南東",
                "南東",
                "南南東",
                "南",
                "南南西",
                "南西",
                "西南西",
                "西",
                "西北西",
                "北西",
                "北北西",
        ];

        /// <summary>
        /// 360° ÷ 16方位 = 22.5°
        /// </summary>
        private readonly double directionSpan = 22.5d;

        /// <summary>
        /// 風向（単位：°）
        /// </summary>
        [ObservableProperty]
        private double windDirection;

        /// <summary>
        /// 風向（単位：°）の方位テキスト
        /// </summary>
        [ObservableProperty]
        private string windDirectionText = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="windCondition">風向・風速</param>
        public WindDirectionSettingDialogViewModel(WindCondition windCondition)
        {
            for (var i = 0; i < this.directionText.Length; i++)
            {
                this.directionDict.Add(this.directionSpan * i, this.directionText[i]);
            }

            if (windCondition.HasErrorWindDirection)
            {
                // 元の入力値が不正の場合は「北」で表示しておく
                this.windDirection = 0d;
                this.WindDirectionText = this.directionDict[0d];
                return;
            }

            this.windDirection = windCondition.WindDirection;

            // 16方位でピッタリの値の場合に16方位のテキストを表示
            // ピッタリでない場合は空欄
            foreach (var (direction, directionText) in this.directionDict)
            {
                if (this.windDirection == direction)
                {
                    this.WindDirectionText = directionText;
                    break;
                }
            }
        }

        /// <summary>
        /// ウィンドウを閉じるAction
        /// </summary>
        internal Action? CloseAction { get; set; }

        /// <summary>
        /// コンパスを描画するキャンバス
        /// </summary>
        internal Canvas? CompassCanvas { get; set; }

        /// <summary>
        /// 結果を取得します。
        /// </summary>
        /// <returns>選択中の風向（単位：°）</returns>
        internal double GetResult()
        {
            return this.WindDirection;
        }

        /// <summary>
        /// コンパスを描画するキャンバスに刻みを作成します。
        /// </summary>
        internal void CreateCompassNick()
        {
            var r1 = 40;
            var r2 = 35;

            for (var i = 0; i < this.directionText.Length; i++)
            {
                var radian = (this.directionSpan * i) * Math.PI / 180d;

                var line = new Line
                {
                    Stroke = Brushes.Black,
                    X1 = (Math.Sin(radian) * r1) + r1,
                    Y1 = (Math.Cos(radian) * r1) + r1,
                    X2 = (Math.Sin(radian) * r2) + r1,
                    Y2 = (Math.Cos(radian) * r2) + r1,
                    StrokeThickness = 1,
                };

                this.CompassCanvas.Children.Add(line);
            }
        }

        #region コマンド

        /// <summary>
        /// コンパスを描画するキャンバスの円をクリックした時のコマンド
        /// </summary>
        /// <param name="param">コンパスを描画するキャンバスの円</param>
        [RelayCommand]
        private void SelectDirection(object param)
        {
#pragma warning disable SA1004 // Documentation lines should begin with single space
            /**
             * 北風を0°とした時計回り0°～360°で指定
             *
             *            北風
             *             0°
             * 　　　　　　↑
             * 西風 270°←　→　90°東風
             * 　　　　　　↓
             * 　　　　　　180°
             *            南風
             *
             */
#pragma warning restore SA1004 // Documentation lines should begin with single space

            // マウス座標
            var element = (Ellipse)param;
            var position = Mouse.GetPosition(element);

            // 中心座標
            var centerX = element.Width / 2;
            var centerY = element.Height / 2;

            // 角度を計算
            var radian = Math.Atan2(position.Y - centerY, position.X - centerX);
            var degree = radian * 180d / Math.PI;

            // 0-diff ～ 360-diff になるようにシフト
            var diff = this.directionSpan / 2d;
            var angle = degree - 90d;
            if (angle < diff * (-1d))
            {
                angle += 360d;
            }

            // 16方位でピッタリの値に補正する
            foreach (var (direction, directionText) in this.directionDict)
            {
                if (angle < direction + diff)
                {
                    this.WindDirection = direction;
                    this.WindDirectionText = directionText;
                    break;
                }
            }
        }

        /// <summary>
        /// OK（ウィンドウを閉じる）コマンド
        /// </summary>
        [RelayCommand]
        private void Ok()
        {
            this.CloseAction?.Invoke();
        }

        #endregion コマンド
    }
}
