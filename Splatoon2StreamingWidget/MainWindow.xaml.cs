using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        // 外人向け(string型の小数点をパースするときに、特定のリージョンではドットの部分がカンマとして扱われるのでドットで統一している)
        // client_id : 固定値（アプリに記載されてる）
        // session_token : 乱数で生成したURL?から
        // 最初に与えられたiksmSessionを使ってユーザー名を取得し、使用可能かを見る
        // マッチIDを使ってKDとかWL、今日全体のXP変動を表示　左からランク帯、パワー、ランキングみたいな感じ
        // /api/records ウデマエ取得　ナワバリように武器ごとのデータ？
        // /api/schedules　今のスケジュール
        // /api/results /api/results/番号 最新のバトルから今やってるものを取得
        // /api/x_power_ranking/200201T00_200301T00/ 今のXP ランキング
        // /api/coop_results サーモン
        private SplatNet2 _splatNet2; // データ取得
        private StreamingWindow _streamingWindow; // 配信Window
        private DispatcherTimer _autoUpdateTimer; // 配信Windowの更新タイミングを決定するタイマー
        // 自動更新の時間の表示と中身
        private Dictionary<string, int> _autoUpdatecomboBoxItems = new Dictionary<string, int>
        {
            {"30秒",30},
            {"1分",60},
            {"2分",120},
            {"3分",180},
            {"5分",300}
        };
        private DateTime nextUpdateTime; // 次のアップデートを行う時間
        private DateTime autoUpdateLockTime; // この時間までに新規データの取得が一回も行われなかった場合は、自動更新をオフにする
        private string authCodeVerifier;
        private string sessionToken = "";
        private bool isUpdating = false; // 更新ボタン連打を阻止

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += Window_Loaded;
            this.Closed += MainWindowClosed;
        }

        private bool LockUpdate()
        {
            if (isUpdating) return false;

            isUpdating = true;
            IksmSessionTextBox.IsReadOnly = true;
            UpdateSessionButton.Content = "Updating...";
            InformationViewTextBlock.Text = "更新中...";
            return true;
        }

        private void UnLockUpdate()
        {
            isUpdating = false;
            IksmSessionTextBox.IsReadOnly = false;
            UpdateSessionButton.Content = "Update session";
            InformationViewTextBlock.Text = "更新完了!";
        }

        /// <summary>
        /// 配信画面の初期化
        /// ここで初期データを取得する
        /// </summary>
        /// <param name="sessionID">iksm</param>
        /// <returns>データ取得に成功したか</returns>
        private async Task<bool> TryInitializeSessionWindow(string sessionID)
        {
            _splatNet2 = new SplatNet2(sessionID);
            if (!await _splatNet2.TryInitializePlayerData()) return false;

            var ud = new UserData { user_name = _splatNet2.PlayerData.nickName, iksm_session = sessionID, principal_id = _splatNet2.PlayerData.principalID, session_token = sessionToken };
            DataManager.SaveConfig(ud);

            var ruleNumber = await _splatNet2.GetGachiSchedule();
            if (_streamingWindow != null && !_streamingWindow.IsClosed) _streamingWindow.Close();
            _streamingWindow = new StreamingWindow();
            _streamingWindow.UpdateWindow(_splatNet2.PlayerData, _splatNet2.RuleData);
            _streamingWindow.Show();
            this.Closing += (sender, args) => _streamingWindow.Close();

            UserNameLabel.Content = "ユーザー名 : " + _splatNet2.PlayerData.nickName;

            return true;
        }

        /// <summary>
        /// 配信者用画面のgridの切り替え
        /// </summary>
        /// <param name="grid">切り替えたいgrid</param>
        private void ChangeViewGrid(Grid grid)
        {
            MainGrid.Visibility = Visibility.Hidden;
            SessionGrid.Visibility = Visibility.Hidden;

            grid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 配信画面の更新
        /// </summary>
        private async Task UpdateViewer()
        {
            if (!LockUpdate()) return;

            try
            {
                _autoUpdateTimer.Stop();

                if (_streamingWindow.IsClosed)
                {
                    var ud = DataManager.LoadConfig();
                    if (ud.iksm_session == "" || !await TryInitializeSessionWindow(ud.iksm_session))
                    {
                        ChangeViewGrid(SessionGrid);
                        isUpdating = false;
                        return;
                    }
                }

                var lastBattleNumber = _splatNet2.lastBattleNumber;
                await _splatNet2.UpdatePlayerData();
                _streamingWindow.UpdateWindow(_splatNet2.PlayerData, _splatNet2.RuleData);

                // 新しいバトルが存在した場合
                if (lastBattleNumber != _splatNet2.lastBattleNumber)
                {
                    autoUpdateLockTime = DateTime.Now + new TimeSpan(0, 15, 0);
                    nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, 150);
                    XpowerDecreaseTextBlock.Text = "";
                }

                if ((bool)AutoUpdateCheckBox.IsChecked)
                    _autoUpdateTimer.Start();
            }
            finally
            {
                UnLockUpdate();
            }
        }

        /// <summary>
        /// 自動更新タイマーの更新(0.5秒置き)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void AutoUpdateTimerElapsed(object sender, EventArgs e)
        {
            InformationViewTextBlock.Text = "次の更新まで " + (int)(nextUpdateTime - DateTime.Now).TotalSeconds + " 秒";
            if (DateTime.Now < nextUpdateTime) return;

            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            await UpdateViewer();
            // 一度も更新されないまま15分経過した場合、autoUpdateのチェックを外す
            if (autoUpdateLockTime < DateTime.Now) AutoUpdateCheckBox.IsChecked = false;
        }

        #region UI Operation
        /// <summary>
        /// 初期化
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LockUpdate();

            // アップデート後の後処理
            if (Environment.CommandLine.IndexOf("/up", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                var args = Environment.GetCommandLineArgs();
                var pid = Convert.ToInt32(args[2]);
                Process.GetProcessById(pid).WaitForExit();
                if (File.Exists("Splatoon2StreamingWidget.old")) File.Delete("Splatoon2StreamingWidget.old");
            }

            try
            {
                // 同期的にasyncメソッドを扱ってTask.Resultとかでawaitを使用しているメソッドを取得しようとするとデッドロックが起きる
                var authURL = SplatNet2SessionToken.GenerateLoginURL();
                authCodeVerifier = authURL.authCodeVerifier;
                IksmSessionTextBox.Text = authURL.url;

                // iksm_sessionを入力するときは空白や改行などを除去
                foreach (var cbi in _autoUpdatecomboBoxItems.Select(item => new ComboBoxItem { Content = item.Key }))
                    AutoUpdateTimeComboBox.Items.Add(cbi);

                _autoUpdateTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
                _autoUpdateTimer.Tick += AutoUpdateTimerElapsed;

                ChangeViewGrid(SessionGrid);
                var ud = DataManager.LoadConfig();
                if (ud.iksm_session == null) return;
                if (await TryInitializeSessionWindow(ud.iksm_session))
                {
                    ChangeViewGrid(MainGrid);
                    return;
                }

                if (ud.session_token == null) return;
                sessionToken = ud.session_token;
                var cookie = await SplatNet2SessionToken.GetCookie(sessionToken);
                if (await TryInitializeSessionWindow(cookie))
                {
                    ChangeViewGrid(MainGrid);
                    return;
                }

                MessageBox.Show("ログインに失敗しました。\n初回起動時と同様、手動でログイン手順を踏んでください。", "ログインに失敗しました", MessageBoxButton.OK,
                    MessageBoxImage.Hand);
            }
            finally
            {
                UnLockUpdate();
                if (await UpdateManager.CheckUpdate())
                    await UpdateManager.ShowUpdateWindow();
            }
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 手動更新ボタンのクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateViewerButton_Click(object sender, RoutedEventArgs e)
        {
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            await UpdateViewer();
            if (!XpowerDecreaseMenuItem.IsChecked) return;
            var loseXp = await _splatNet2.GetLoseXP();
            if (loseXp < 0) XpowerDecreaseTextBlock.Text = $"負けた場合 : {loseXp:F1}";
        }

        /// <summary>
        /// セッションの初期化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LockUpdate()) return;

            try
            {
                var text = IksmSessionTextBox.Text.Trim();
                if (Regex.IsMatch(text, "session_token_code=(.*)&"))
                {
                    var sessionTokenCode = Regex.Match(text, "session_token_code=(.*)&").Groups[1].Value;
                    sessionToken = await SplatNet2SessionToken.GetSessionToken(sessionTokenCode, authCodeVerifier);
                    var cookie = await SplatNet2SessionToken.GetCookie(sessionToken);
                    IksmSessionTextBox.Text = cookie;
                }

                // 空白文字だけ？ 全角空白やtab?や改行はどうするか
                var iksmSession = IksmSessionTextBox.Text.Trim();
                if (await TryInitializeSessionWindow(iksmSession))
                    ChangeViewGrid(MainGrid);
            }
            finally
            {
                UnLockUpdate();
            }
        }

        /// <summary>
        /// セッションを変えるボタンのクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeSessionMenuItem_Click(object sender, RoutedEventArgs e) => ChangeViewGrid(SessionGrid);

        /// <summary>
        /// 自動更新ボタンをアクティブにするチェック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            autoUpdateLockTime = DateTime.Now + new TimeSpan(0, 15, 0);
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[AutoUpdateTimeComboBox.Text]);
            _autoUpdateTimer.Start();
        }

        /// <summary>
        /// 自動更新ボタンを非アクティブにするチェック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoUpdateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoUpdateTimer.Stop();
            InformationViewTextBlock.Text = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoUpdateTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)AutoUpdateTimeComboBox.Items[AutoUpdateTimeComboBox.SelectedIndex];
            var str = comboBoxItem.Content.ToString();
            if (!_autoUpdatecomboBoxItems.ContainsKey(str)) return;
            nextUpdateTime = DateTime.Now + new TimeSpan(0, 0, _autoUpdatecomboBoxItems[str]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateRuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ruleNumber = await _splatNet2.GetGachiSchedule();
            _streamingWindow.UpdateRule(_splatNet2.PlayerData, _splatNet2.RuleData);
        }

        private async void InitializeBattleRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_streamingWindow == null || _streamingWindow.IsClosed || isUpdating) return;

            _autoUpdateTimer.Stop();
            InformationViewTextBlock.Text = "初期化中...";
            isUpdating = true;

            await _splatNet2.TryInitializePlayerData();
            var ruleNumber = await _splatNet2.GetGachiSchedule();
            _streamingWindow.UpdateWindow(_splatNet2.PlayerData, _splatNet2.RuleData);

            InformationViewTextBlock.Text = "初期化完了!";
            if (AutoUpdateCheckBox.IsChecked != null && (bool)AutoUpdateCheckBox.IsChecked)
                _autoUpdateTimer.Start();

            isUpdating = false;
        }

        private void VersionInformationMenuItem_Click(object sender, RoutedEventArgs e) => MessageBox.Show("StreamingWidget Ver" + UpdateManager.VersionNumber + "\nmade by @sisno_boomx\ndesigned by @aok_no_simpi", "バージョン情報", MessageBoxButton.OK, MessageBoxImage.Asterisk);

        private void XpowerDecreaseMenuItem_Checked(object sender, RoutedEventArgs e) => MessageBox.Show("この機能は、自動更新に対応していません。\nバトルが始まった段階で手動更新ボタンを押すと配信者側UIに負けた場合のXPが表示されます。", "情報表示について", MessageBoxButton.OK, MessageBoxImage.Exclamation);

        private async void CheckUpdateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var canUpdate = await UpdateManager.CheckUpdate();
            if (!canUpdate) MessageBox.Show("このバージョンは最新です。", "アップデート確認", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            else await UpdateManager.ShowUpdateWindow();
        }

        private void MatchAnalyzeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ud = DataManager.LoadConfig();
            var url = "https://splatool.net/analytics/?iksm=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ud.iksm_session)) + "#iklink";
            var ps = new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }

        private void MatchRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ud = DataManager.LoadConfig();
            var url = "https://splatool.net/records/?iksm=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ud.iksm_session));
            var ps = new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }

        private void LeagueEstimateLPMenuItem_Checked(object sender, RoutedEventArgs e) => _splatNet2.WillDisplayEstimateLp = true;
        private void LeagueEstimateLPMenuItem_Unchecked(object sender, RoutedEventArgs e) => _splatNet2.WillDisplayEstimateLp = false;
        #endregion
    }
}
