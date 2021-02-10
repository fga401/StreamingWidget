using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Splatoon2StreamingWidget
{
    /// <summary>
    /// HttpClientの重複起動対策 ソケット使用数の増加を抑える
    /// </summary>
    static class HttpManager
    {
        private static readonly CookieContainer CookieContainer = new CookieContainer();
        private static readonly HttpClientHandler Handler = new HttpClientHandler { CookieContainer = CookieContainer };
        private static readonly HttpClient Client = new HttpClient(Handler);

        public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => await Client.SendAsync(request);
        public static async Task<string> GetStringAsync(string uri) => await Client.GetStringAsync(uri);

        public static async Task<TJson> GetDeserializedJsonAsync<TJson>(string uri)
        {
            var response = await Client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<TJson>(response);
        }

        // gzipで圧縮されているかを判定しながらデータ形式に落とし込む
        public static async Task<TJson> GetAutoDeserializedJsonAsync<TJson>(HttpRequestMessage request)
        {
            var response = await Client.SendAsync(request);
            string json;
            if (response.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip")
            {
                response.EnsureSuccessStatusCode();
                var inStream = await response.Content.ReadAsStreamAsync();
                var decompStream = new GZipStream(inStream, CompressionMode.Decompress);
                await using (inStream)
                await using (decompStream)
                {
                    using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                    json = await reader.ReadToEndAsync();
                }
            }
            else
            {
                json = await response.Content.ReadAsStringAsync();
            }

            return JsonConvert.DeserializeObject<TJson>(json);
        }

        public static async Task<TJson> GetDeserializedJsonAsync<TJson>(HttpRequestMessage request)
        {
            var response = await Client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TJson>(json);
        }

        public static async Task<string> GetDecompressedAsync(HttpRequestMessage request)
        {
            using var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var inStream = await response.Content.ReadAsStreamAsync();
            var decompStream = new GZipStream(inStream, CompressionMode.Decompress);
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                return await reader.ReadToEndAsync();
            }
        }

        public static async Task<TJson> GetDecompressedDeserializedJsonAsync<TJson>(HttpRequestMessage request)
        {
            var json = await GetDecompressedAsync(request);
            return JsonConvert.DeserializeObject<TJson>(json);
        }

        public static async Task<CookieContainer> GetCookieContainer(HttpRequestMessage request)
        {
            await Client.SendAsync(request);
            return Handler.CookieContainer;
        }

        public static async Task<string> GetStringAsyncWithCookieContainer(string uri, Cookie cookie)
        {
            Handler.CookieContainer.Add(new Uri(""), cookie);
            return await Client.GetStringAsync(uri);
        }

        public static async Task<TJson> GetDeserializedJsonAsyncWithCookieContainer<TJson>(string uri, Cookie cookie)
        {
            Handler.CookieContainer.Add(new Uri(""), cookie);
            var json = await Client.GetStringAsync(uri);

            return JsonConvert.DeserializeObject<TJson>(json);
        }

        public static async Task<string> DownloadAsync(string uri, string folderPath)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var content = response.Content;
            await using var stream = await content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(folderPath + "/" + request.RequestUri.Segments.Last(), FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fileStream);
            return request.RequestUri.Segments.Last();
        }
    }
}
