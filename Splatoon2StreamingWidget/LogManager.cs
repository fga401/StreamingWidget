using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon2StreamingWidget
{
    public static class LogManager
    {
        private const string Path = "data/log.txt";

        public static async Task WriteLogAsync(string text)
        {
            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            if (!File.Exists(Path)) File.Create(Path);

            text = "[" + DateTime.Now + "] " + text + "\n";
            var encodedText = Encoding.Unicode.GetBytes(text);
            await using var sourceStream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Write, 4096, true);
            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
        }
    }
}
