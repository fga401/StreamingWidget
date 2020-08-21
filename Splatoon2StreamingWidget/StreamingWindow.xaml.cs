using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Splatoon2StreamingWidget
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class StreamingWindow : Window
    {
        public bool IsClosed = false;
        private DispatcherTimer _animationDispatcherTimer;
        private ContentManager contentNow = new ContentManager();
        private ContentManager contentTarget = new ContentManager();
        private int animationTimes = 0;
        private int battleNum;

        private class ContentManager
        {
            public float xpower { get; set; }
            public float xpowerSubtract { get; set; }
            public int killCount { get; set; }
            public int assistCount { get; set; }
            public int deathCount { get; set; }
            public float kdRate { get; set; }
            public int killCountN { get; set; }
            public int assistCountN { get; set; }
            public int deathCountN { get; set; }
            public float kdRateN { get; set; }
            public int wlRate { get; set; }
            public int paintPoint { get; set; }
        }

        public StreamingWindow()
        {
            InitializeComponent();

            _animationDispatcherTimer = new DispatcherTimer();
            _animationDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _animationDispatcherTimer.Tick += UpdateAnimation;

            battleNum = -1;

            // 表示の初期化
            XPowerLabel.Content = "2000.0";
            XPowerSubtractLabel.Content = "";
            KALabel.Content = "0(0)";
            DLabel.Content = "0";
            KDLabel.Content = "0.00";
            KALabelN.Content = "0(0)";
            DLabelN.Content = "0";
            KDLabelN.Content = "0.00";
            WLLabel.Content = "0%";
        }

        protected override void OnClosed(EventArgs e) => IsClosed = true;

        private void UpdateXPSubtractLabelColor(float diff) => XPowerSubtractLabel.Foreground = diff >= 0 ? Brushes.LimeGreen : Brushes.Red;

        public void UpdateWindow(PlayerData playerData, RuleData ruleData)
        {
            if (battleNum == playerData.WinCount + playerData.LoseCount) return;
            // common
            battleNum = playerData.WinCount + playerData.LoseCount;
            WLabel.Content = playerData.WinCount;
            LLabel.Content = playerData.LoseCount;
            RuleLabel.Content = ruleData.Name;
            UdemaeLabel.Content = "";
            XPowerLabel.Content = "";
            XPowerSubtractLabel.Content = "";
            PaintPointLabel.Content = "";
            WeaponImage.Source = null;
            MVPLabel.Visibility = Visibility.Hidden;
            ContributionLabel.Visibility = Visibility.Hidden;
            PaintPointLabel.Visibility = Visibility.Hidden;
            contentTarget.xpower = 0; // アニメーションの処理をストップ

            // ウデマエラベル、XPラベル、XPDiffラベル、色の処理
            switch (ruleData.Mode)
            {
                case RuleData.GameMode.Gachi:
                    UdemaeLabel.Content = playerData.Udemae[ruleData.RuleIndex][0];
                    if (playerData.Udemae[ruleData.RuleIndex] != "X")
                        XPowerLabel.Content = playerData.Udemae[ruleData.RuleIndex].Substring(1); // -、+、数字
                    else if (playerData.XPower[ruleData.RuleIndex] == 0)
                        XPowerLabel.Content = "Calculating";
                    UpdateXPSubtractLabelColor(playerData.XPowerDiff);
                    XPowerLabel.Margin = new Thickness(90, 35, 0, 0);
                    XPowerSubtractLabel.Margin = new Thickness(370, 91, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0x56, 0x2C));

                    // コンテンツデータの更新
                    contentTarget.xpower = playerData.XPower[ruleData.RuleIndex];
                    contentTarget.xpowerSubtract = playerData.XPowerDiff;
                    contentNow.xpower = playerData.XPowerDiff == 0 ? 0 : playerData.XPower[ruleData.RuleIndex] - playerData.XPowerDiff; // 計測終了直後の場合と分ける
                    contentNow.xpowerSubtract = 0;
                    break;
                case RuleData.GameMode.Private:
                    PaintPointLabel.Visibility = Visibility.Visible;
                    XPowerLabel.Content = playerData.KDMVP;
                    MVPLabel.Visibility = Visibility.Visible;
                    XPowerLabel.Margin = new Thickness(285, 35, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(194, 0, 255));
                    break;
                case RuleData.GameMode.League2:
                    if (playerData.LeaguePower == 0)
                        XPowerLabel.Content = "Calculating";
                    UpdateXPSubtractLabelColor(playerData.LeaguePowerDiff);
                    XPowerLabel.Margin = new Thickness(40, 35, 0, 0);
                    XPowerSubtractLabel.Margin = new Thickness(320, 91, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 1, 206));

                    // コンテンツデータの更新
                    contentTarget.xpower = playerData.LeaguePower;
                    contentTarget.xpowerSubtract = playerData.LeaguePowerDiff;
                    contentNow.xpower = playerData.LeaguePowerDiff == 0 ? 0 : playerData.LeaguePower - playerData.LeaguePowerDiff;
                    contentNow.xpowerSubtract = 0;
                    break;
                case RuleData.GameMode.League4:
                    if (playerData.LeaguePower == 0)
                        XPowerLabel.Content = "Calculating";
                    UpdateXPSubtractLabelColor(playerData.LeaguePowerDiff);
                    XPowerLabel.Margin = new Thickness(40, 35, 0, 0);
                    XPowerSubtractLabel.Margin = new Thickness(320, 91, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 1, 206));

                    // コンテンツデータの更新
                    contentTarget.xpower = playerData.LeaguePower;
                    contentTarget.xpowerSubtract = playerData.LeaguePowerDiff;
                    contentNow.xpower = playerData.LeaguePowerDiff == 0 ? 0 : playerData.LeaguePower - playerData.LeaguePowerDiff;
                    contentNow.xpowerSubtract = 0;
                    break;
                case RuleData.GameMode.Regular:
                    PaintPointLabel.Visibility = Visibility.Visible;
                    WeaponImage.Source = new BitmapImage(playerData.ImageUri);
                    XPowerLabel.Content = $"{playerData.WinMeter:F1}";
                    XPowerLabel.Margin = new Thickness(150, 35, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(59, 252, 3));
                    break;
                case RuleData.GameMode.FestivalSolo:
                    if (playerData.FesPower == 0)
                        XPowerLabel.Content = "Calculating";
                    UpdateXPSubtractLabelColor(playerData.FesPowerDiff);
                    XPowerLabel.Margin = new Thickness(40, 35, 0, 0);
                    XPowerSubtractLabel.Margin = new Thickness(320, 91, 0, 0);
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0x76, 0x4C));

                    // コンテンツデータの更新
                    contentTarget.xpower = playerData.FesPower;
                    contentTarget.xpowerSubtract = playerData.FesPowerDiff;
                    contentNow.xpower = playerData.FesPowerDiff == 0 ? 0 : playerData.FesPower - playerData.FesPowerDiff;
                    contentNow.xpowerSubtract = 0;
                    break;
                case RuleData.GameMode.FestivalTeam:
                    var cpt = playerData.ContributionPointTotal;
                    if (cpt >= (long)Math.Pow(10, 10))
                        XPowerLabel.Content = $"{cpt / Math.Pow(10, 9):F1}" + "B";
                    else if (cpt >= (long)Math.Pow(10, 7))
                        XPowerLabel.Content = $"{cpt / Math.Pow(10, 6):F1}" + "M";
                    else if (cpt >= (long)Math.Pow(10, 4))
                        XPowerLabel.Content = $"{cpt / Math.Pow(10, 3):F1}" + "K";
                    else
                        XPowerLabel.Content = cpt;

                    XPowerLabel.Margin = new Thickness(35, 35, 0, 0);
                    PaintPointLabel.Visibility = Visibility.Visible;
                    ContributionLabel.Visibility = Visibility.Visible;
                    RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(159, 252, 3));
                    break;
            }

            // コンテンツデータの更新（targetに向かってnowからアニメーションが進行）
            contentTarget.killCount = playerData.KillCount;
            contentTarget.assistCount = playerData.AssistCount;
            contentTarget.deathCount = playerData.DeathCount;
            contentTarget.kdRate = (float)playerData.KillCount / (playerData.DeathCount == 0 ? 1 : playerData.DeathCount);
            contentTarget.killCountN = playerData.KillCountN;
            contentTarget.assistCountN = playerData.AssistCountN;
            contentTarget.deathCountN = playerData.DeathCountN;
            contentTarget.kdRateN = (float)playerData.KillCountN / (playerData.DeathCountN == 0 ? 1 : playerData.DeathCountN);
            contentTarget.wlRate = (int)Math.Round((float)playerData.WinCount / (playerData.WinCount + playerData.LoseCount == 0 ? 1 : playerData.WinCount + playerData.LoseCount) * 100);
            contentTarget.paintPoint = playerData.PaintPoint;

            contentNow.killCount = playerData.KillCount - playerData.KillCountN;
            contentNow.assistCount = playerData.AssistCount - playerData.AssistCountN;
            contentNow.deathCount = playerData.DeathCount - playerData.DeathCountN;
            contentNow.kdRate = (float)contentNow.killCount / (contentNow.deathCount == 0 ? 1 : contentNow.deathCount);
            contentNow.killCountN = 0;
            contentNow.assistCountN = 0;
            contentNow.deathCountN = 0;
            contentNow.kdRateN = float.Parse(KDLabelN.Content.ToString(), CultureInfo.InvariantCulture);
            contentNow.wlRate = int.Parse(WLLabel.Content.ToString().TrimEnd('%'), CultureInfo.InvariantCulture);
            contentNow.paintPoint = 0;

            animationTimes = 100;
            _animationDispatcherTimer.Start();
            UpdateAnimation(null, null);
        }

        public void UpdateRule(PlayerData playerData, RuleData ruleData)
        {
            UdemaeLabel.Content = playerData.Udemae[ruleData.RuleIndex][0];
            RuleLabel.Content = ruleData.Name;
            if (playerData.Udemae[ruleData.RuleIndex] != "X")
                XPowerLabel.Content = playerData.Udemae[ruleData.RuleIndex].Substring(1);
            else if (playerData.XPower[ruleData.RuleIndex] == 0)
                XPowerLabel.Content = "Calculating";

            XPowerLabel.Margin = new Thickness(90, 35, 0, 0);
            XPowerSubtractLabel.Margin = new Thickness(370, 91, 0, 0);
            RuleLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0x56, 0x2C));

            XPowerSubtractLabel.Content = "";
            PaintPointLabel.Content = "";
            KALabelN.Content = "0(0)";
            DLabelN.Content = "0";
            KDLabelN.Content = "0.00";
        }

        private void UpdateAnimation(object sender, EventArgs e)
        {
            // UIの更新
            if (contentTarget.xpower > 0)
            {
                XPowerLabel.Content = $"{contentNow.xpower:F1}";
                XPowerSubtractLabel.Content = contentNow.xpowerSubtract > 0 ? "+" + $"{contentNow.xpowerSubtract:F1}" : contentNow.xpowerSubtract == 0 ? "" : $"{contentNow.xpowerSubtract:F1}";
            }

            KALabel.Content = (contentNow.killCount + contentNow.assistCount) + "(" + contentNow.assistCount + ")";
            DLabel.Content = contentNow.deathCount;
            KDLabel.Content = $"{contentNow.kdRate:F2}";
            KALabelN.Content = (contentNow.killCountN + contentNow.assistCountN) + "(" + contentNow.assistCountN + ")";
            DLabelN.Content = contentNow.deathCountN;
            KDLabelN.Content = $"{contentNow.kdRateN:F2}";
            WLLabel.Content = contentNow.wlRate + "%";
            PaintPointLabel.Content = contentNow.paintPoint + "p";

            // contentNowの更新
            contentNow.xpower += (contentTarget.xpower - contentNow.xpower) / animationTimes;
            contentNow.xpowerSubtract += (contentTarget.xpowerSubtract - contentNow.xpowerSubtract) / animationTimes;
            contentNow.killCount += (contentTarget.killCount - contentNow.killCount) / animationTimes;
            contentNow.assistCount += (contentTarget.assistCount - contentNow.assistCount) / animationTimes;
            contentNow.deathCount += (contentTarget.deathCount - contentNow.deathCount) / animationTimes;
            contentNow.kdRate += (contentTarget.kdRate - contentNow.kdRate) / animationTimes;
            contentNow.killCountN += (contentTarget.killCountN - contentNow.killCountN) / animationTimes;
            contentNow.assistCountN += (contentTarget.assistCountN - contentNow.assistCountN) / animationTimes;
            contentNow.deathCountN += (contentTarget.deathCountN - contentNow.deathCountN) / animationTimes;
            contentNow.kdRateN += (contentTarget.kdRateN - contentNow.kdRateN) / animationTimes;
            contentNow.wlRate += (contentTarget.wlRate - contentNow.wlRate) / animationTimes;
            contentNow.paintPoint += (contentTarget.paintPoint - contentNow.paintPoint) / animationTimes;

            animationTimes--;
            if (animationTimes != 0) return;

            // アニメーションを終了し、値をtargetの値と一致させる
            _animationDispatcherTimer.Stop();
            if (contentTarget.xpower > 0)
            {
                XPowerLabel.Content = $"{contentTarget.xpower:F1}";
                XPowerSubtractLabel.Content = contentTarget.xpowerSubtract > 0 ? "+" + $"{contentTarget.xpowerSubtract:F1}" : (contentTarget.xpowerSubtract == 0 ? "" : $"{contentTarget.xpowerSubtract:F1}");
            }

            KALabel.Content = (contentTarget.killCount + contentTarget.assistCount) + "(" + contentTarget.assistCount + ")";
            DLabel.Content = contentTarget.deathCount;
            KDLabel.Content = $"{contentTarget.kdRate:F2}";
            KALabelN.Content = (contentTarget.killCountN + contentTarget.assistCountN) + "(" + contentTarget.assistCountN + ")";
            DLabelN.Content = contentTarget.deathCountN;
            KDLabelN.Content = $"{contentTarget.kdRateN:F2}";
            WLLabel.Content = contentTarget.wlRate + "%";
            PaintPointLabel.Content = contentTarget.paintPoint + "p";
        }
    }
}
