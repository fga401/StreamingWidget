using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Splatoon2StreamingWidget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // client_id : 固定値（アプリに記載されてる）
        // session_token : 乱数で生成したURL?から
        // 最初に与えられたiksmSessionを使ってユーザー名を取得し、使用可能かを見る
        // マッチIDを使ってKDとかWL、今日全体のXP変動を表示　左からランク帯、パワー、ランキングみたいな感じ
        // /api/records ウデマエ取得　ナワバリように武器ごとのデータ？
        // /api/schedules　今のスケジュール
        // /api/results /api/results/番号 最新のバトルから今やってるものを取得
        // /api/x_power_ranking/200201T00_200301T00/ 今のXP ランキング
        // /api/coop_results サーモン
        private SplatNet2 _splatNet2;
        private StreamingWindow _streamingWindow;
        private DispatcherTimer _autoUpdateTimer;
        private Dictionary<string, int> _autoUpdatecomboBoxItems = new Dictionary<string, int>
        {
            {"30秒",30},
            {"1分",60},
            {"2分",120},
            {"3分",180},
            {"5分",300}
        };
        private DateTime nextUpdateTime;
        private string authCodeVerifier;
        private bool isUpdating = false;

        public MainWindow()
        {
            InitializeComponent();

            var authURL = SplatNet2SessionToken.GenerateLoginURL();
            authCodeVerifier = authURL.authCodeVerifier;
            IksmSessionTextBox.Text = authURL.url;

            // iksm_sessionを入力するときは空白や改行などを除去
            foreach (var item in _autoUpdatecomboBoxItems)
            {
                var cbi = new ComboBoxItem();
                cbi.Content = item.Key;
                AutoUpdateTimeComboBox.Items.Add(cbi);
            }

            _autoUpdateTimer = new DispatcherTimer();
            _autoUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _autoUpdateTimer.Tick += AutoUpdateTimerElapsed;

            ChangeViewGrid(SessionGrid);
            var ud = DataManager.LoadConfig();
            if (ud.iksm_session == null || !TryInitializeSessionWindow(ud.iksm_session)) return;
            this.Closing += (sender, args) => _streamingWindow.Close();

            ChangeViewGrid(MainGrid);
        }

        // intializeしたときに更新しないといけない
        private bool TryInitializeSessionWindow(string sessionID)
        {
            _splatNet2 = new SplatNet2(sessionID);
            if (!_splatNet2.TryInitializePlayerData()) return false;

            var ud = new UserData { user_name = _splatNet2.PlayerData.nickName, iksm_session = sessionID, principal_id = _splatNet2.PlayerData.principalID };
            DataManager.SaveConfig(ud);

            var ruleNumber = _splatNet2.GetSchedule();
            if (_streamingWindow != null && !_streamingWindow.IsClosed) _streamingWindow.Close();
            _streamingWindow = new StreamingWindow();
            _streamingWindow.UpdateWindow(_splatNet2.PlayerData, ruleNumber, _splatNet2.ruleNamesJP[ruleNumber]);
            _streamingWindow.Show();

            UserNameLabel.Content = "ユーザー名 : " + _splatNet2.PlayerData.nickName;

            return true;
        }

        private void ChangeViewGrid(Grid grid)
        {
            MainGrid.Visibility = Visibility.Hidden;
            SessionGrid.Visibility = Visibility.Hidden;

            grid.Visibility = Visibility.Visible;
        }

        private async void UpdateViewer()
        {
            if (isUpdating) return;

            _autoUpdateTimer.Stop();
            isUpdating = true;
            InformationViewTextBlock.Text = "更新中...";

            if (_streamingWindow.IsClosed)
            {
                var ud = DataManager.LoadConfig();
                if (ud.iksm_session == "" || !TryInitializeSessionWindow(ud.iksm_session))
                {
                    ChangeViewGrid(SessionGrid);
                    isUpdating = false;
                    return;
                }
            }

            var ruleNumber = _splatNet2.lastBattleRule;
            await Task.Run(() => _splatNet2.UpdatePlayerData());
            _streamingWindow.UpdateWindow(_splatNet2.PlayerData, ruleNumber, _splatNet2.ruleNamesJP[ruleNumber]);

            InformationViewTextBlock.Text = "更新完了!";
            if ((bool)AutoUpdateCheckBox.IsChecked)
                _autoUpdateTimer.Start();
            isUpdating = false;
        }

        public void AutoUpdateTimerElapsed(object sender, EventArgs e)
        {
            InformationViewTextBlock.Text = "次の更新まで " + (int)(nextUpdateTime - DateTime.Now).TotalSeconds + " 秒";
            if (DateTime.Now < nextUpdateTime) return;

            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            UpdateViewer();
        }

        private void UpdateViewerButton_Click(object sender, RoutedEventArgs e)
        {
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            UpdateViewer();
        }

        private async void UpdateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;
            UpdateSessionButton.Content = "Updating...";
            isUpdating = true;
            var text = IksmSessionTextBox.Text.Trim();
            if (Regex.IsMatch(text, "session_token_code=(.*)&"))
            {
                var sessionTokenCode = Regex.Match(text, "session_token_code=(.*)&").Groups[1].Value;
                var sessionToken = await SplatNet2SessionToken.GetSessionToken(sessionTokenCode, authCodeVerifier);
                var cookie = await SplatNet2SessionToken.GetCookie(sessionToken);
                IksmSessionTextBox.Text = cookie;
            }

            // 空白文字だけ？ 全角空白やtab?や改行はどうするか
            var iksmSession = IksmSessionTextBox.Text.Trim();
            if (TryInitializeSessionWindow(iksmSession))
                ChangeViewGrid(MainGrid);

            UpdateSessionButton.Content = "Update session";
            isUpdating = false;
        }

        private void ChangeSessionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeViewGrid(SessionGrid);
        }

        private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            _autoUpdateTimer.Start();
        }

        private void AutoUpdateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoUpdateTimer.Stop();
            InformationViewTextBlock.Text = "";
        }

        private void AutoUpdateTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)AutoUpdateTimeComboBox.Items[AutoUpdateTimeComboBox.SelectedIndex];
            var str = comboBoxItem.Content.ToString();
            if (!_autoUpdatecomboBoxItems.ContainsKey(str)) return;
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[str]);
        }

        private void UpdateRuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ruleNumber = _splatNet2.GetSchedule();
            _streamingWindow.UpdateRule(_splatNet2.PlayerData, ruleNumber, _splatNet2.ruleNamesJP[ruleNumber]);
        }

        private async void InitializeBattleRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_streamingWindow == null || _streamingWindow.IsClosed || isUpdating) return;

            _autoUpdateTimer.Stop();
            InformationViewTextBlock.Text = "初期化中...";
            isUpdating = true;

            await Task.Run(() => _splatNet2.UpdatePlayerData());

            var ruleNumber = _splatNet2.GetSchedule();
            await Task.Run(() => _splatNet2.UpdatePlayerData());
            _streamingWindow.UpdateWindow(_splatNet2.PlayerData, ruleNumber, _splatNet2.ruleNamesJP[ruleNumber]);

            InformationViewTextBlock.Text = "初期化完了!";
            if ((bool)AutoUpdateCheckBox.IsChecked)
                _autoUpdateTimer.Start();

            isUpdating = false;
        }

        private void VersionInformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("StreamingWidget Ver1.0.0\nmade by @sisno_boomx");
        }
    }
}
