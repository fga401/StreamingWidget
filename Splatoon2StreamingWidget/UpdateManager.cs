using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Splatoon2StreamingWidget
{
    public static class UpdateManager
    {
        public const string VersionNumber = "1.2.7";
        private static string newVersionNumber = "";
        private static UpdateWindow _updateWindow;

        /// <summary>
        /// アップデートをGitHub上にあるデータを参照し、チェックする
        /// </summary>
        /// <returns>アップデートがある場合、Trueを返す</returns>
        public static async Task<bool> CheckUpdate()
        {
            const string url = "https://raw.githubusercontent.com/boomxch/StreamingWidget/master/version";
            var versionJson = await HttpManager.GetDeserializedJsonAsync<SplatNet2DataStructure.VersionData>(url);
            newVersionNumber = versionJson.version;
            return versionJson.version != VersionNumber;
        }

        public static async Task ShowUpdateWindow()
        {
            const string url = "https://raw.githubusercontent.com/boomxch/StreamingWidget/master/README.md";
            var indexMD = await HttpManager.GetStringAsync(url);
            var updateInfo = Regex.Match(indexMD, "## 更新情報\\n(- ([^\\n]*)\\n)*\\n").Groups[2].Captures.Select(v => v.Value.TrimEnd('\n'));
            var updateInfoText = "StreamingWidget  Ver " + newVersionNumber + "\n\n更新内容\n・" + updateInfo.Aggregate((text, s) => text + "\n・" + s);
            _updateWindow = new UpdateWindow { UpdateTextBlock = { Text = updateInfoText } };
            _updateWindow.ShowDialog();
        }

        public static async Task<bool> UpdateApplication()
        {
            const string url = "https://github.com/boomxch/StreamingWidget/raw/master/Splatoon2StreamingWidget.exe";
            if (!Directory.Exists("data")) Directory.CreateDirectory("data");

            try
            {
                // config削除
                if (File.Exists("data/config.xml")) File.Delete("data/config.xml");

                // ファイル置き換え
                await HttpManager.DownloadAsync(url, "data");
                if (File.Exists("Splatoon2StreamingWidget.old")) File.Delete("Splatoon2StreamingWidget.old");
                File.Move("Splatoon2StreamingWidget.exe", "Splatoon2StreamingWidget.old");
                File.Move("data/Splatoon2StreamingWidget.exe", "Splatoon2StreamingWidget.exe");
                Process.Start("Splatoon2StreamingWidget.exe", "/up " + Process.GetCurrentProcess().Id);
            }
            catch (HttpRequestException)
            {
                return false;
            }
            finally
            {
                DeleteTempUpdateFiles();
            }

            return true;
        }

        public static void DeleteTempUpdateFiles()
        {
            if (!File.Exists("Splatoon2StreamingWidget.exe") && File.Exists("Splatoon2StreamingWidget.old")) File.Move("Splatoon2StreamingWidget.old", "Splatoon2StreamingWidget.exe");
            if (File.Exists("data/Splatoon2StreamingWidget.exe")) File.Delete("data/Splatoon2StreamingWidget.exe");
        }
    }
}
