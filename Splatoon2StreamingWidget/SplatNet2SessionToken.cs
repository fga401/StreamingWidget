using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Newtonsoft.Json;

namespace Splatoon2StreamingWidget
{
    // https://github.com/frozenpandaman/splatnet2statink/blob/master/iksm.py
    // https://salmonia.mydns.jp/
    public static class SplatNet2SessionToken
    {
        private static long GetUnixTime() => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        /// <summary>
        /// Generate login URL
        /// </summary>
        /// <returns>login url</returns>
        public static (string authCodeVerifier, string url) GenerateLoginURL()
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
                {"scope", "openid user user.birthday user.mii user.screenName"},
                {"response_type", "session_token_code"},
                {"session_token_code_challenge", authCodeChallenge},
                {"session_token_code_challenge_method", "S256"},
                {"theme", "login_form"}
            };
            url += string.Join('&', body.Select(v => v.Key + "=" + v.Value));
            return (authCodeVerifier, url);
        }

        /// <summary>
        /// Get session token
        /// </summary>
        /// <returns>session_token</returns>
        public static async Task<string> GetSessionToken(string sessionTokenCode, string authCodeVerifier)
        {
            const string url = "";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("Accept-Language", "en-US");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");

            var body = new Dictionary<string, string>
            {
                {"session_token_code", sessionTokenCode},
                {"session_token_code_verifier", authCodeVerifier}
            };

            if (IsBodyEmpty(body))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            var json = JsonConvert.SerializeObject(body);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.SessionToken>(request);
                return res.session_token;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"session token\"");
                return "";
            }
            catch (InvalidDataException)
            {
                await LogManager.WriteLogAsync("Failed to decompress \"session token\"");
                return "";
            }
        }

        /// <summary>
        /// Login process for SplatNet2
        /// </summary>
        /// <returns>iksm session</returns>
        public static async Task<string> GetCookie(string sessionToken)
        {
            var timeStamp = GetUnixTime();
            var guid = Guid.NewGuid().ToString();

            // access token 取得
            const string url = "";

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("Accept-Language", "ja-JP");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");

            var body = new Dictionary<string, string>
            {
                {"session_token", sessionToken},
                {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer-session-token"}
            };

            if (IsBodyEmpty(body))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            var json = JsonConvert.SerializeObject(body);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            string accessToken;
            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.AccessToken>(request);
                accessToken = res.access_token;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"access token\"");
                return "";
            }
            catch (InvalidDataException)
            {
                await LogManager.WriteLogAsync("Failed to decompress \"access token\"");
                return "";
            }
            finally
            {
                request.Dispose();
            }

            // user data 取得
            const string url2 = "";
            request = new HttpRequestMessage(HttpMethod.Get, url2);

            request.Headers.Add("Accept-Language", "ja-JP");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");

            if (IsBodyEmpty(accessToken))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            SplatNet2DataStructure.UserInfo userInfo;
            try
            {
                userInfo = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.UserInfo>(request);
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"user data\"");
                return "";
            }
            catch (InvalidDataException)
            {
                await LogManager.WriteLogAsync("Failed to decompress \"user data\"");
                return "";
            }
            finally
            {
                request.Dispose();
            }

            // 新 access token 取得
            const string url3 = "";

            request = new HttpRequestMessage(HttpMethod.Post, url3);

            request.Headers.Add("Accept-Language", "ja-JP");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");
            request.Headers.Add("Authorization", "Bearer");
            request.Headers.Add("X-Platform", "Android");

            var flapgNSO = await CallFlapgAPI(accessToken, guid, timeStamp, "nso");

            var body3 = new Dictionary<string, Dictionary<string, string>>
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

            if (IsBodyEmpty(body3))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            var json3 = JsonConvert.SerializeObject(body3);

            request.Content = new StringContent(json3, Encoding.UTF8, "application/json");

            string idToken;
            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.SplatoonToken>(request);
                idToken = res.result.webApiServerCredential.accessToken;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"id token\"");
                return "";
            }
            catch (InvalidDataException)
            {
                await LogManager.WriteLogAsync("Failed to decompress \"id token\"");
                return "";
            }
            finally
            {
                request.Dispose();
            }

            var flapgApp = await CallFlapgAPI(idToken, guid, timeStamp, "app");

            // splatoon access token 取得
            const string url4 = "";

            request = new HttpRequestMessage(HttpMethod.Post, url4);

            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Connection", "Keep-Alive");
            request.Headers.Add("Accept-Encoding", "gzip");
            request.Headers.Add("Authorization", "Bearer " + idToken);
            request.Headers.Add("X-Platform", "Android");

            if (IsBodyEmpty(idToken))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            var body4 = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "parameter",
                    new Dictionary<string, string>
                    {
                        {"f", flapgApp.f},
                        {"registrationToken", flapgApp.p1},
                        {"timestamp", flapgApp.p2},
                        {"requestId", flapgApp.p3}
                    }
                },
            };

            if (IsBodyEmpty(body4))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            var json4 = JsonConvert.SerializeObject(body4);

            request.Content = new StringContent(json4, Encoding.UTF8, "application/json");

            string splatoonAccessToken;
            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.WebServiceToken>(request);
                splatoonAccessToken = res.result.accessToken;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"splatoon access token\"");
                return "";
            }
            catch (InvalidDataException)
            {
                await LogManager.WriteLogAsync("Failed to decompress \"session token\"");
                return "";
            }
            finally
            {
                request.Dispose();
            }

            // iksm_session 取得
            const string url5 = "";

            request = new HttpRequestMessage(HttpMethod.Get, url5);

            request.Headers.Add("X-IsAppAnalyticsOptedIn", "false");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Encoding", "gzip,deflate");
            request.Headers.Add("X-GameWebToken", splatoonAccessToken);
            request.Headers.Add("Accept-Language", "ja-JP");
            request.Headers.Add("X-IsAnalyticsOptedIn", "false");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("DNT", "0");

            if (IsBodyEmpty(splatoonAccessToken))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            try
            {
                var cookies = await HttpManager.GetCookieContainer(request);
                var responseCookies = cookies.GetCookies(new Uri("")).Cast<Cookie>();
                return responseCookies.First().Value;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"iksm session\"");
                return "";
            }
            catch (IndexOutOfRangeException)
            {
                await LogManager.WriteLogAsync("Failed to get \"cookie\"");
                return "";
            }
            finally
            {
                request.Dispose();
            }
        }

        public static async Task<SplatNet2DataStructure.FlapgResult.FlapgInnerResult> CallFlapgAPI(string idToken, string guid, long timeStamp, string type)
        {
            const string url = "";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("x-token", idToken);
            request.Headers.Add("x-time", timeStamp.ToString());
            request.Headers.Add("x-guid", guid);
            request.Headers.Add("x-hash", await GetHashFromS2SAPI(idToken, timeStamp));
            request.Headers.Add("x-ver", "3");
            request.Headers.Add("x-iid", type);

            if (IsBodyEmpty(idToken, guid, type))
            {
                await LogManager.WriteLogAsync("Body is null");
                return new SplatNet2DataStructure.FlapgResult.FlapgInnerResult();
            }

            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.FlapgResult>(request);
                return res.result;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"f\"");
                return new SplatNet2DataStructure.FlapgResult.FlapgInnerResult();
            }
        }

        public static async Task<string> GetHashFromS2SAPI(string idToken, long timeStamp)
        {
            const string url = "";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("User-Agent", "" + UpdateManager.VersionNumber);

            var body = new Dictionary<string, string>
            {
                {"naIdToken", idToken},
                {"timestamp", timeStamp.ToString()}
            };

            if (IsBodyEmpty(body))
            {
                await LogManager.WriteLogAsync("Body is null");
                return "";
            }

            request.Content = new StringContent(await new FormUrlEncodedContent(body).ReadAsStringAsync(), Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                var res = await HttpManager.GetAutoDeserializedJsonAsync<SplatNet2DataStructure.S2SResult>(request);
                return res.hash;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"hash\"");
                return "";
            }
        }

        private static bool IsBodyEmpty(Dictionary<string, string> dic)
        {
            return dic.Any(data => string.IsNullOrEmpty(data.Value));
        }

        private static bool IsBodyEmpty(Dictionary<string, Dictionary<string, string>> dic)
        {
            return dic.Any(data => Enumerable.Any<KeyValuePair<string, string>>(data.Value, data2 => string.IsNullOrEmpty(data2.Value)));
        }

        private static bool IsBodyEmpty(params string[] val)
        {
            return val.Any(data => string.IsNullOrEmpty(data));
        }
    }
}