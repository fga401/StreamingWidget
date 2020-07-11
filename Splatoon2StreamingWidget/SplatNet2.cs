using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json;
using System.Linq;

namespace Splatoon2StreamingWidget
{
    public class PlayerData
    {
        public string[] udemae { get; set; }
        public float[] xPower { get; set; }
        public float xPowerDiff = 0;
        public int KillCount = 0;
        public int AssistCount = 0;
        public int DeathCount = 0;
        public int KillCountN = 0;
        public int AssistCountN = 0;
        public int DeathCountN = 0;
        public int WinCount = 0;
        public int LoseCount = 0;
        public readonly string nickName;
        public readonly string principalID;

        internal PlayerData(string name, string id)
        {
            nickName = name;
            principalID = id;
        }
    }

    public class SplatNet2
    {
        public readonly Dictionary<string, int> Rules = new Dictionary<string, int>()
        {
            {"splat_zones",0},
            {"tower_control",1},
            {"rainmaker",2},
            {"clam_blitz",3},
        };

        public readonly string[] ruleNamesJP = new[] { "ガチエリア", "ガチヤグラ", "ガチホコバトル", "ガチアサリ" };
        public PlayerData PlayerData { get; private set; }
        public int lastBattleRule { get; private set; }
        private int lastBattleNumber;
        private List<SplatNet2DataStructure.Schedules.GachiSchedule> schedules = new List<SplatNet2DataStructure.Schedules.GachiSchedule>();
        private readonly string iksmSession;
        private readonly HttpClient httpClient;
        private readonly HttpClientHandler handler;
        private readonly CookieContainer cc;
        private const string ApiUriPrefix = "";
        private const decimal SplatNet2Time2020 = 1577836800;
        private readonly DateTime DateTime2020 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public SplatNet2(string sessionID)
        {
            iksmSession = sessionID;

            cc = new CookieContainer();
            cc.Add(new Uri("https://app.splatoon2.nintendo.net"), new Cookie("iksm_session", iksmSession));
            handler = new HttpClientHandler { CookieContainer = cc };
            httpClient = new HttpClient(handler);
        }

        ~SplatNet2()
        {
            httpClient.Dispose();
            handler.Dispose();
        }

        public bool TryInitializePlayerData()
        {
            try
            {
                var result = httpClient.GetStringAsync(ApiUriPrefix + "records").Result;
                var udemaeData = JsonConvert.DeserializeObject<SplatNet2DataStructure.Records>(result).records.player;
                PlayerData = new PlayerData(udemaeData.nickname, udemaeData.principal_id);

                string GetUdemaeName(SplatNet2DataStructure.Records.PersonalRecords.PlayerRecords.RuleData r) => r.name == null ? "-" : (r.name + r.s_plus_number);
                PlayerData.udemae = new[]
                {
                    GetUdemaeName(udemaeData.udemae_zones),
                    GetUdemaeName(udemaeData.udemae_tower),
                    GetUdemaeName(udemaeData.udemae_rainmaker),
                    GetUdemaeName(udemaeData.udemae_clam),
                };

                result = httpClient.GetStringAsync(ApiUriPrefix + "x_power_ranking/" + GetSeason() + "/summary").Result;
                var powerData = JsonConvert.DeserializeObject<SplatNet2DataStructure.XPowerRanking>(result);

                // is_number_reachedの詳細が不明
                float GetXPower(SplatNet2DataStructure.Records.PersonalRecords.PlayerRecords.RuleData r, SplatNet2DataStructure.XPowerRanking.RuleStats m) => r.is_x && m.my_ranking != null ? float.Parse(m.my_ranking.x_power) : 0;
                PlayerData.xPower = new[]
                {
                    GetXPower(udemaeData.udemae_zones,powerData.splat_zones),
                    GetXPower(udemaeData.udemae_tower,powerData.tower_control),
                    GetXPower(udemaeData.udemae_rainmaker,powerData.rainmaker),
                    GetXPower(udemaeData.udemae_clam,powerData.clam_blitz)
                };

                result = httpClient.GetStringAsync(ApiUriPrefix + "results").Result;
                var battleData = JsonConvert.DeserializeObject<SplatNet2DataStructure.Results>(result).results;
                lastBattleNumber = battleData.Max(a => a.battle_number);
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("セッションの有効期限が切れているか,無効な値です。\niksm_sessionを再設定してください。", "無効なセッション情報", MessageBoxButton.OK);
                PlayerData = null;
                return false;
            }
            catch (JsonException)
            {
                MessageBox.Show("セッションの有効期限が切れているか,無効な値です。\niksm_sessionを再設定してください。", "無効なセッション情報", MessageBoxButton.OK);
                PlayerData = null;
                return false;
            }
            catch (Exception) // これ本当はダメ
            {
                MessageBox.Show("セッションの有効期限が切れているか,無効な値です。\niksm_sessionを再設定してください。", "無効なセッション情報", MessageBoxButton.OK);
                PlayerData = null;
                return false;
            }

            return true;
        }

        public int GetSchedule()
        {
            var splatNet2TimeNow = (decimal)Math.Floor((DateTime.UtcNow - DateTime2020).TotalSeconds) + SplatNet2Time2020;

            int GetRuleFromSchedule()
            {
                for (int i = 0; i < schedules.Count; i++)
                {
                    if (schedules[i].start_time <= splatNet2TimeNow && schedules[i].end_time < splatNet2TimeNow)
                    {
                        schedules.RemoveAt(i);
                        i--;
                    }
                    else if (schedules[i].start_time <= splatNet2TimeNow && splatNet2TimeNow < schedules[i].end_time)
                    {
                        foreach (var ruleName in Rules.Keys)
                        {
                            if (ruleName == schedules[i].rule.key) return Rules[ruleName];
                        }
                    }
                }

                return -1;
            }

            var ans = GetRuleFromSchedule();
            if (ans != -1) return ans;

            var result = httpClient.GetStringAsync(ApiUriPrefix + "schedules").Result;
            schedules = JsonConvert.DeserializeObject<SplatNet2DataStructure.Schedules>(result).gachi;
            schedules.Sort((a, b) => a.start_time - b.start_time > 0 ? 1 : -1);

            return GetRuleFromSchedule();
        }

        public void UpdatePlayerData()
        {
            var result = httpClient.GetStringAsync(ApiUriPrefix + "results").Result;
            var battleData = JsonConvert.DeserializeObject<SplatNet2DataStructure.Results>(result).results;
            battleData = battleData.Where(v => v.battle_number > lastBattleNumber).OrderBy(a => a.battle_number).ToList();
            if (battleData.Count == 0) return;

            if (battleData.Last().battle_number - lastBattleNumber > 50)
            {
                TryInitializePlayerData();
                return;
            }

            lastBattleNumber = battleData.Last().battle_number;
            lastBattleRule = Rules[battleData.Last().rule.key];
            var ruleNow = lastBattleRule;
            foreach (var item in battleData)
            {
                PlayerData.KillCount += item.player_result.kill_count;
                PlayerData.AssistCount += item.player_result.assist_count;
                PlayerData.DeathCount += item.player_result.death_count;
                if (item.my_team_count > item.other_team_count) PlayerData.WinCount++;
                else PlayerData.LoseCount++;

                if (item.game_mode.key != "gachi") continue;

                PlayerData.xPowerDiff = 0;
                if (!float.TryParse(item.x_power, out var xPower)) continue;

                if (ruleNow == Rules[item.rule.key])
                    PlayerData.xPowerDiff = xPower - PlayerData.xPower[ruleNow];

                PlayerData.xPower[Rules[item.rule.key]] = xPower;
            }

            PlayerData.KillCountN = battleData.Last().player_result.kill_count;
            PlayerData.AssistCountN = battleData.Last().player_result.assist_count;
            PlayerData.DeathCountN = battleData.Last().player_result.death_count;
        }

        private string GetSeason()
        {
            var dt = DateTime.UtcNow;
            var season = dt.Month == 12 ?
                $"{dt.Year.ToString().Substring(2, 2)}1201T00_{(dt.Year + 1).ToString().Substring(2, 2)}0101T00" :
                $"{dt.Year.ToString().Substring(2, 2)}{dt.Month.ToString().PadLeft(2, '0')}01T00_{dt.Year.ToString().Substring(2, 2)}{(dt.Month + 1).ToString().PadLeft(2, '0')}01T00";

            return season;
        }
    }
}
