using System;
using System.Windows;
using System.Windows.Media;
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

        protected override void OnClosed(EventArgs e)
        {
            IsClosed = true;
        }

        public void UpdateWindow(PlayerData playerData, int ruleIndex, string ruleName)
        {
            if (battleNum == playerData.WinCount + playerData.LoseCount) return;
            battleNum = playerData.WinCount + playerData.LoseCount;

            UdemaeLabel.Content = playerData.udemae[ruleIndex][0];
            if (playerData.udemae[ruleIndex] != "X")
                XPowerLabel.Content = playerData.udemae[ruleIndex].Substring(1);
            else
                XPowerLabel.Content = playerData.udemae[ruleIndex] != "X" ? "" : playerData.xPower[ruleIndex] == 0 ? "Calculating" : $"{playerData.xPower[ruleIndex] - playerData.xPowerDiff:F1}";
            XPowerSubtractLabel.Foreground = playerData.xPowerDiff >= 0 ? Brushes.DeepSkyBlue : Brushes.Red;
            RuleLabel.Content = ruleName;
            WLabel.Content = playerData.WinCount;
            LLabel.Content = playerData.LoseCount;

            // コンテンツデータの更新（targetに向かってnowからアニメーションが進行）
            contentTarget.xpower = playerData.xPower[ruleIndex];
            contentTarget.xpowerSubtract = playerData.xPowerDiff;
            contentTarget.killCount = playerData.KillCount;
            contentTarget.assistCount = playerData.AssistCount;
            contentTarget.deathCount = playerData.DeathCount;
            contentTarget.kdRate = (float)playerData.KillCount / (playerData.DeathCount == 0 ? 1 : playerData.DeathCount);
            contentTarget.killCountN = playerData.KillCountN;
            contentTarget.assistCountN = playerData.AssistCountN;
            contentTarget.deathCountN = playerData.DeathCountN;
            contentTarget.kdRateN = (float)playerData.KillCountN / (playerData.DeathCountN == 0 ? 1 : playerData.DeathCountN);
            contentTarget.wlRate = (int)Math.Round((float)playerData.WinCount / (playerData.WinCount + playerData.LoseCount == 0 ? 1 : playerData.WinCount + playerData.LoseCount) * 100);

            contentNow.xpower = playerData.xPower[ruleIndex] - playerData.xPowerDiff;
            contentNow.xpowerSubtract = 0;
            contentNow.killCount = playerData.KillCount - playerData.KillCountN;
            contentNow.assistCount = playerData.AssistCount - playerData.AssistCountN;
            contentNow.deathCount = playerData.DeathCount - playerData.DeathCountN;
            contentNow.kdRate = (float)contentNow.killCount / (contentNow.deathCount == 0 ? 1 : contentNow.deathCount);
            contentNow.killCountN = 0;
            contentNow.assistCountN = 0;
            contentNow.deathCountN = 0;
            contentNow.kdRateN = float.Parse(KDLabelN.Content.ToString());
            contentNow.wlRate = int.Parse(WLLabel.Content.ToString().TrimEnd('%'));

            animationTimes = 100;
            _animationDispatcherTimer.Start();
        }

        public void UpdateRule(PlayerData playerData, int ruleIndex, string ruleName)
        {
            UdemaeLabel.Content = playerData.udemae[ruleIndex][0];
            RuleLabel.Content = ruleName;
            if (playerData.udemae[ruleIndex] != "X")
                XPowerLabel.Content = playerData.udemae[ruleIndex].Substring(1);
            else
                XPowerLabel.Content = playerData.udemae[ruleIndex] != "X" ? "" : playerData.xPower[ruleIndex] == 0 ? "Calculating" : $"{playerData.xPower[ruleIndex]}";

            XPowerSubtractLabel.Content = "";
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
                XPowerSubtractLabel.Content = contentNow.xpowerSubtract > 0 ? "+" + $"{contentNow.xpowerSubtract:F1}" : (contentNow.xpowerSubtract == 0 ? "" : $"{contentNow.xpowerSubtract:F1}");
            }

            KALabel.Content = (contentNow.killCount + contentNow.assistCount) + "(" + contentNow.assistCount + ")";
            DLabel.Content = contentNow.deathCount;
            KDLabel.Content = $"{contentNow.kdRate:F2}";
            KALabelN.Content = (contentNow.killCountN + contentNow.assistCountN) + "(" + contentNow.assistCountN + ")";
            DLabelN.Content = contentNow.deathCountN;
            KDLabelN.Content = $"{contentNow.kdRateN:F2}";
            WLLabel.Content = contentNow.wlRate + "%";

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
        }
    }
}
