using System;
using System.Windows;

namespace Splatoon2StreamingWidget
{
    /// <summary>
    /// UpdateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private bool isUpdating = false;

        public UpdateWindow()
        {
            InitializeComponent();

            this.Closing += ClosingUpdateWindow;
        }

        private static void ClosingUpdateWindow(object sender, EventArgs e)
        {
            UpdateManager.DeleteTempUpdateFiles();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            isUpdating = true;
            UpdateButton.Content = "Updating...";
            if (!await UpdateManager.UpdateApplication())
            {
                isUpdating = false;
                UpdateButton.Content = "Update";
                await LogManager.WriteLogAsync("Failed to update");
                MessageBox.Show("アップデートに失敗しました。");
            }

            // アプリ終了
            Application.Current.Shutdown();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;
            this.Close();
        }
    }
}
