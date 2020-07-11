using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Splatoon2StreamingWidget
{
    // https://github.com/frozenpandaman/splatnet2statink/blob/master/iksm.py
    public static class SplatNet2SessionToken
    {
        // https://salmonia.mydns.jp/
        private static readonly HttpClient _client = new HttpClient();

        private class SessionToken
        {
            public string code { get; set; }
            public string session_token { get; set; }
        }

        private class AccessToken
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string id_token { get; set; }
        }

        private class UserInfo
        {
            public string birthday { get; set; }
            public string country { get; set; }
            public long createdAt { get; set; }
            public string gender { get; set; }
            public string language { get; set; }
            public string nickname { get; set; }
            public string screenname { get; set; }
        }

        private class SplatoonToken
        {
            public string correlationId { get; set; }
            public int status { get; set; }
            public TokenResult result { get; set; }

            public class TokenResult
            {
                public UserData user { get; set; }
                
                public WebApiServerCredential webApiServerCredential { get; set; }
                public FirebaseCredential firebaseCredential { get; set; }

                public class UserData
                {
                    public string name { get; set; }
                    public string id { get; set; }
                    public string supportId { get; set; }
                    public string imageUri { get; set; }
                    public MemberShip membership { get; set; }

                    public class MemberShip
                    {
                        public bool active { get; set; }
                    }
                }

                public class WebApiServerCredential
                {
                    public string accessToken { get; set; }
                    public int expiresIn { get; set; }
                }

                public class FirebaseCredential
                {
                    public string accessToken { get; set; }
                    public int expiresIn { get; set; }
                }
            }
        }

        public class WebServiceToken
        {
            public WebServiceTokenResult result { get; set; }

            public class WebServiceTokenResult
            {
                public string accessToken { get; set; }
            }
        }

        public class FlapgResult
        {
            public FlapgInnerResult result { get; set; }

            public class FlapgInnerResult
            {
                public string f { get; set; }
                public string p1 { get; set; }
                public string p2 { get; set; }
                public string p3 { get; set; }
            }
        }

        private class S2SResult
        {
            public string hash { get; set; }
        }

        /// <summary>
        /// Login to accounts.nintendo.com
        /// </summary>
        /// <returns>session_token</returns>
        public static (string authCodeVerifier,string url) GenerateLoginURL()
        {
            var rnd = new Random();
            var rndBytes = new byte[36];
            rnd.NextBytes(rndBytes);
            var authState = Convert.ToBase64String(rndBytes).Replace('+', '-').Replace('/', '_');

            rndBytes = new byte[32];
            rnd.NextBytes(rndBytes);
            var authCodeVerifier = Convert.ToBase64String(rndBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

            var sha256 = SHA256.Create();
            var authCvHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(authCodeVerifier)));

            var authCodeChallenge = authCvHash.TrimEnd('=').Replace('+', '-').Replace('/', '_');


            var url = "";

            var body = new Dictionary<string, string>
            {
                {"state", authState},
                {"redirect_uri", HttpUtility.UrlEncode("npf71b963c1b7b6d119://auth")},
                {"client_id", "71b963c1b7b6d119"},
                {"scope", "openid user user.birthday user.mii user.screenName"},
                {"response_type", "session_token_code"},
                {"session_token_code_challenge", authCodeChallenge},
                {"session_token_code_challenge_method", "S256"},
                {"theme", "login_form"}
            };
            url += string.Join('&', body.Select(v => v.Key + "=" + v.Value));
            return (authCodeVerifier,url);
        }

        public static async Task<string> GetSessionToken(string sessionTokenCode, string authCodeVerifier)
        {
            var url = "";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("User-Agent", "");
            request.Headers.Add("Accept-Language", "en-US");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Host", "");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");

            var body = new Dictionary<string, string>
            {
                {"client_id", ""},
                {"session_token_code", sessionTokenCode},
                {"session_token_code_verifier", authCodeVerifier}
            };

            var json = JsonConvert.SerializeObject(body);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(request);
            var inStream = await response.Content.ReadAsStreamAsync();

            var decompStream = new GZipStream(inStream, CompressionMode.Decompress);
            
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                var resJson  = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<SessionToken>(resJson).session_token;
            }
        }

        public static async Task<string> GetCookie(string sessionToken)
        {
            var timeStamp = GetUnixTime();
            var guid = Guid.NewGuid().ToString();

            // access token 取得
            var url = "";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("User-Agent", "");
            request.Headers.Add("Accept-Language", "ja-JP");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Host", "");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");

            var body = new Dictionary<string, string>
            {
                {"client_id", "71b963c1b7b6d119"},
                {"session_token", sessionToken},
                {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer-session-token"}
            };

            var json = JsonConvert.SerializeObject(body);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(request);
            var inStream = await response.Content.ReadAsStreamAsync();

            var decompStream = new GZipStream(inStream, CompressionMode.Decompress);
            string accessToken;
            
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                var resJson  = await reader.ReadToEndAsync();
                accessToken = JsonConvert.DeserializeObject<AccessToken>(resJson).access_token;
            }


            // user data 取得
            var url2 = "";
            using var request2 = new HttpRequestMessage(HttpMethod.Get, url2);
            
            request2.Headers.Add("User-Agent", "");
            request2.Headers.Add("Accept-Language", "ja-JP");
            request2.Headers.Add("Accept", "application/json");
            request2.Headers.Add("Authorization", "Bearer " + accessToken);
            request2.Headers.Add("Host", "");
            request2.Headers.Add("Connection", "Keep-Alive");
            request2.Headers.Add("Accept-Encoding", "gzip");

            var response2 = await _client.SendAsync(request2);
            inStream = await response2.Content.ReadAsStreamAsync();

            decompStream = new GZipStream(inStream, CompressionMode.Decompress);
            UserInfo userInfo;
            
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                var resJson  = await reader.ReadToEndAsync();
                userInfo = JsonConvert.DeserializeObject<UserInfo>(resJson);
            }


            // 新 access token 取得
            var url3 = "";

            using var request3 = new HttpRequestMessage(HttpMethod.Post, url3);

            request3.Headers.Add("User-Agent", "");
            request3.Headers.Add("Accept-Language", "ja-JP");
            request3.Headers.Add("Accept", "application/json");
            request3.Headers.Add("Host", "");
            request3.Headers.Add("Connection", "Keep-Alive");
            request3.Headers.Add("Accept-Encoding", "gzip");
            request3.Headers.Add("X-ProductVersion", "1.6.1.2");
            request3.Headers.Add("Authorization", "Bearer");
            request3.Headers.Add("X-Platform", "Android");

            var flapgNSO = await CallFlapgAPI(accessToken, guid, timeStamp, "nso");

            var body3 = new Dictionary<string, Dictionary<string,string>>
            {
                {
                    "parameter", 
                    new Dictionary<string, string>
                    {
                        {"f", flapgNSO.f},
                        {"naIdToken", flapgNSO.p1},
                        {"timestamp", flapgNSO.p2},
                        {"requestId", flapgNSO.p3},
                        {"naCountry", userInfo.country},
                        {"naBirthday", userInfo.birthday},
                        {"language", userInfo.language}
                    }
                },
            };

            var json3 = JsonConvert.SerializeObject(body3);

            request3.Content = new StringContent(json3, Encoding.UTF8, "application/json");

            var response3 = await _client.SendAsync(request3);
            inStream = await response3.Content.ReadAsStreamAsync();
            decompStream = new GZipStream(inStream, CompressionMode.Decompress);

            string idToken;
            
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                var resJson  = await reader.ReadToEndAsync();
                idToken = JsonConvert.DeserializeObject<SplatoonToken>(resJson).result.webApiServerCredential.accessToken;
            }

            var flapgApp = await CallFlapgAPI(idToken, guid, timeStamp, "app");


            // splatoon access token 取得
            var url4 = "";

            using var request4 = new HttpRequestMessage(HttpMethod.Post, url4);

            request4.Headers.Add("User-Agent", "");
            request4.Headers.Add("Accept", "application/json");
            request4.Headers.Add("Host", "");
            request4.Headers.Add("Connection", "Keep-Alive");
            request4.Headers.Add("Accept-Encoding", "gzip");
            request4.Headers.Add("X-ProductVersion", "1.6.1.2");
            request4.Headers.Add("Authorization", "Bearer " + idToken);
            request4.Headers.Add("X-Platform", "Android");

            var body4 = new Dictionary<string, Dictionary<string,string>>
            {
                {
                    "parameter", 
                    new Dictionary<string, string>
                    {
                        { "id",""},
                        {"f", flapgApp.f},
                        {"registrationToken", flapgApp.p1},
                        {"timestamp", flapgApp.p2},
                        {"requestId", flapgApp.p3}
                    }
                },
            };

            var json4 = JsonConvert.SerializeObject(body4);

            request4.Content = new StringContent(json4, Encoding.UTF8, "application/json");

            var response4 = await _client.SendAsync(request4);
            inStream = await response4.Content.ReadAsStreamAsync();
            decompStream = new GZipStream(inStream, CompressionMode.Decompress);

            string splatoonAccessToken;
            
            await using (inStream)
            await using (decompStream)
            {
                using var reader = new StreamReader(decompStream, Encoding.UTF8, true) as TextReader;
                var resJson  = await reader.ReadToEndAsync();
                splatoonAccessToken = JsonConvert.DeserializeObject<WebServiceToken>(resJson).result.accessToken;
            }


            // iksm_session 取得
            var url5 = "";

            using var request5 = new HttpRequestMessage(HttpMethod.Get, url5);

            request5.Headers.Add("Host", "");
            request5.Headers.Add("X-IsAppAnalyticsOptedIn", "false");
            request5.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request5.Headers.Add("Accept-Encoding", "gzip,deflate");
            request5.Headers.Add("X-GameWebToken", splatoonAccessToken);
            request5.Headers.Add("Accept-Language", "ja-JP");
            request5.Headers.Add("X-IsAnalyticsOptedIn", "false");
            request5.Headers.Add("Connection", "keep-alive");
            request5.Headers.Add("DNT", "0");
            request5.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.1.2; Pixel Build/NJH47D; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/59.0.3071.125 Mobile Safari/537.36");
            request5.Headers.Add("X-Requested-With", "");

            var cookies = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = cookies};
            using var iksmClient = new HttpClient(handler);

            var response5 = await iksmClient.SendAsync(request5);
            var responseCookies = cookies.GetCookies(new Uri("")).Cast<Cookie>();
            return responseCookies.First().Value;
        }

        public static async Task<FlapgResult.FlapgInnerResult> CallFlapgAPI(string idToken, string guid, long timeStamp, string type)
        {
            var url = "";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("x-token", idToken);
            request.Headers.Add("x-time", timeStamp.ToString());
            request.Headers.Add("x-guid", guid);
            request.Headers.Add("x-hash", await GetHashFromS2SAPI(idToken,timeStamp));
            request.Headers.Add("x-ver", "3");
            request.Headers.Add("x-iid", type);

            var response = await _client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<FlapgResult>(json).result;
        }

        public static async Task<string> GetHashFromS2SAPI(string idToken, long timeStamp)
        {
            var url = "";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("User-Agent", "");

            var body = new Dictionary<string, string>
            {
                {"naIdToken", idToken},
                {"timestamp", timeStamp.ToString()}
            };

            request.Content = new StringContent(await new FormUrlEncodedContent(body).ReadAsStringAsync(), Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<S2SResult>(json).hash;
        }

        private static long GetUnixTime() => (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}
