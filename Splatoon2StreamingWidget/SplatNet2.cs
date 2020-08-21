using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

// 画像認識、アニメーション、時間で自動更新、xamlとずれている（上の部分の解像度？）、非同期、SplatNet2SessionToken組み込み, 計測中から終わったときにdiffが大変なことになりそう
namespace Splatoon2StreamingWidget
{
    public class PlayerData
    {
        // ここに武器の調子メータのデータも入れる
        // gachi
        public string[] Udemae { get; set; }
        public float[] XPower { get; set; }
        public float XPowerDiff = 0;
        // league
        public float LeaguePower = 0;
        public float LeaguePowerDiff = 0;
        // regular
        public float WinMeter = 0;
        public Uri ImageUri;
        public int PaintPoint = 0;
        // private
        public int KillMVP = 0;
        public int DeathMVP = 0;
        public int KDMVP = 0;
        public int PointMVP = 0;
        // fes
        public float FesPower = 0;
        public float FesPowerDiff = 0;
        public long ContributionPointTotal = 0;
        // common
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

    public class RuleData
    {
        public GameMode Mode;
        public int RuleIndex;
        public string Name;

        internal RuleData(GameMode mode, int ruleIndex, string name)
        {
            Mode = mode;
            RuleIndex = ruleIndex;
            Name = name;
        }

        public enum GameMode
        {
            Gachi,
            Private,
            League2,
            League4,
            Regular,
            FestivalSolo,
            FestivalTeam
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
            {"turf_war",4}
        };

        public bool WillDisplayEstimateLp { get; set; }
        public readonly string[] ruleNamesJP = new[] { "ガチエリア", "ガチヤグラ", "ガチホコバトル", "ガチアサリ", "ナワバリバトル" };
        public PlayerData PlayerData { get; private set; }
        public RuleData RuleData { get; private set; }
        public int lastBattleNumber { get; private set; }
        private List<SplatNet2DataStructure.Schedules.GachiSchedule> schedules = new List<SplatNet2DataStructure.Schedules.GachiSchedule>();
        private readonly string iksmSession;
        private readonly Cookie Cookie;
        private const string ApiUriPrefix = "";
        private const decimal SplatNet2Time2020 = 1577836800;
        private readonly DateTime DateTime2020 = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public SplatNet2(string sessionID)
        {
            iksmSession = sessionID;

            Cookie = new Cookie("iksm_session", iksmSession);
        }

        public async Task<bool> TryInitializePlayerData()
        {
            SplatNet2DataStructure.Records.PersonalRecords.PlayerRecords udemaeData;
            try
            {
                var result = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.Records>(ApiUriPrefix + "records", Cookie);
                udemaeData = result.records.player;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"records\"");
                return false;
            }
            catch (JsonException)
            {
                await LogManager.WriteLogAsync("Failed to convert \"records\"");
                return false;
            }

            PlayerData = new PlayerData(udemaeData.nickname, udemaeData.principal_id);

            string GetUdemaeName(SplatNet2DataStructure.Records.PersonalRecords.PlayerRecords.RuleData r) => r.name == null ? "-" : (r.name + r.s_plus_number);
            PlayerData.Udemae = new[]
            {
                GetUdemaeName(udemaeData.udemae_zones),
                GetUdemaeName(udemaeData.udemae_tower),
                GetUdemaeName(udemaeData.udemae_rainmaker),
                GetUdemaeName(udemaeData.udemae_clam),
            };

            SplatNet2DataStructure.XPowerRanking powerData;
            try
            {
                powerData = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.XPowerRanking>(ApiUriPrefix + "x_power_ranking/" + GetSeason() + "/summary", Cookie);
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"XPowerRanking\"");
                return false;
            }
            catch (JsonException)
            {
                await LogManager.WriteLogAsync("Failed to convert \"XPowerRanking\"");
                return false;
            }

            // parse時にCultureInfo.InvariantCultureを付けないとフランスの方を筆頭にバグる
            float GetXPower(SplatNet2DataStructure.Records.PersonalRecords.PlayerRecords.RuleData r, SplatNet2DataStructure.XPowerRanking.RuleStats m) => r.is_x && m.my_ranking != null ? float.Parse(m.my_ranking.x_power, CultureInfo.InvariantCulture) : 0;
            PlayerData.XPower = new[]
            {
                GetXPower(udemaeData.udemae_zones,powerData.splat_zones),
                GetXPower(udemaeData.udemae_tower,powerData.tower_control),
                GetXPower(udemaeData.udemae_rainmaker,powerData.rainmaker),
                GetXPower(udemaeData.udemae_clam,powerData.clam_blitz)
            };

            List<SplatNet2DataStructure.Results.BattleResult> battleData;
            try
            {
                var result2 = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.Results>(ApiUriPrefix + "results", Cookie);
                battleData = result2.results;
            }
            catch (HttpRequestException)
            {
                await LogManager.WriteLogAsync("Failed to get \"results\"");
                return false;
            }
            catch (JsonException)
            {
                await LogManager.WriteLogAsync("Failed to convert \"results\"");
                return false;
            }

            lastBattleNumber = battleData.Max(a => a.battle_number);
#if DEBUG
            lastBattleNumber = battleData.Max(a => a.battle_number) - 50;
#endif

            var ruleIndex = await GetGachiSchedule();
            RuleData = new RuleData(RuleData.GameMode.Gachi, ruleIndex, ruleNamesJP[ruleIndex]);

            return true;
        }

        public async Task<int> GetGachiSchedule()
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

            var res = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.Schedules>(ApiUriPrefix + "schedules", Cookie);
            schedules = res.gachi;
            schedules.Sort((a, b) => a.start_time - b.start_time > 0 ? 1 : -1);

            return GetRuleFromSchedule();
        }

        // LPの処理について、機能をオンにするなら別のリグマが始まったときとの区別をつける必要がある
        public async Task UpdatePlayerData()
        {
            var res = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.Results>(ApiUriPrefix + "results", Cookie);
            var battleData = res.results;
            battleData = battleData.Where(v => v.battle_number > lastBattleNumber).OrderBy(a => a.battle_number).ToList();
#if DEBUG
            battleData = battleData.Where(v => v.battle_number == lastBattleNumber + 1).OrderBy(a => a.battle_number).ToList();
#endif
            if (battleData.Count == 0) return;

            if (battleData.Last().battle_number - lastBattleNumber > 50)
            {
                await TryInitializePlayerData();
                return;
            }

            lastBattleNumber = battleData.Last().battle_number;
            var ruleNow = Rules[battleData.Last().rule.key];
            PlayerData.KillCountN = battleData.Last().player_result.kill_count;
            PlayerData.AssistCountN = battleData.Last().player_result.assist_count;
            PlayerData.DeathCountN = battleData.Last().player_result.death_count;
            PlayerData.PaintPoint = battleData.Last().player_result.game_paint_point;

            foreach (var item in battleData)
            {
                // common
                PlayerData.KillCount += item.player_result.kill_count;
                PlayerData.AssistCount += item.player_result.assist_count;
                PlayerData.DeathCount += item.player_result.death_count;
                if (item.my_team_result.key == "victory") PlayerData.WinCount++;
                else PlayerData.LoseCount++;

                RuleData.RuleIndex = Rules[item.rule.key];

                switch (item.game_mode.key)
                {
                    // XPについての処理
                    case "gachi":
                        if (item.player_result.player.udemae.is_x)
                        {
                            PlayerData.XPowerDiff = 0;
                            var xPower = item.x_power ?? 0;

                            // 計測が終わった直後はDiffの表示は行わない
                            if (ruleNow == Rules[item.rule.key] && PlayerData.XPower[ruleNow] != 0)
                                PlayerData.XPowerDiff = xPower - PlayerData.XPower[ruleNow];

                            PlayerData.XPower[Rules[item.rule.key]] = xPower;
                        }
                        else
                        {
                            // ウデマエ更新
                            PlayerData.Udemae[Rules[item.rule.key]] = item.player_result.player.udemae.name + item.player_result.player.udemae.s_plus_number;
                        }

                        RuleData.Mode = RuleData.GameMode.Gachi;
                        RuleData.Name = ruleNamesJP[RuleData.RuleIndex];
                        break;

                    // LPについての処理
                    case "league_pair":
                        PlayerData.LeaguePowerDiff = 0;
                        var lp2 = WillDisplayEstimateLp
                            ? item.league_point ?? (item.my_estimate_league_point ?? 0)
                            : (item.league_point ?? 0);
                            

                        if (PlayerData.LeaguePower != 0)
                            PlayerData.LeaguePowerDiff = lp2 - PlayerData.LeaguePower;

                        PlayerData.LeaguePower = lp2;

                        RuleData.Mode = RuleData.GameMode.League2;
                        RuleData.Name = "リーグマッチ";
                        break;

                    // LPについての処理
                    case "league_team":
                        PlayerData.LeaguePowerDiff = 0;
                        var lp4 = WillDisplayEstimateLp
                            ? item.league_point ?? (item.my_estimate_league_point ?? 0)
                            : (item.league_point ?? 0);

                        if (PlayerData.LeaguePower != 0)
                            PlayerData.LeaguePowerDiff = lp4 - PlayerData.LeaguePower;

                        PlayerData.LeaguePower = lp4;

                        RuleData.Mode = RuleData.GameMode.League4;
                        RuleData.Name = "リーグマッチ";
                        break;

                    // いくつかの項目についてMVPの回数を算出
                    case "private":
                        var detailResult = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.DetailResult>(ApiUriPrefix + "results/" + item.battle_number, Cookie);

                        // 味方チーム内でのMVP 1on1の時は常にプラスになる 敵チームとの複合にした方が良い？
                        if (!detailResult.my_team_members.Any(v => v.kill_count > item.player_result.kill_count))
                            PlayerData.KillMVP++;
                        if (!detailResult.my_team_members.Any(v => v.death_count < item.player_result.death_count))
                            PlayerData.DeathMVP++;
                        if (!detailResult.my_team_members.Any(v => (float)v.kill_count / v.death_count > (float)item.player_result.kill_count / item.player_result.death_count))
                            PlayerData.KDMVP++;
                        if (!detailResult.my_team_members.Any(v => v.game_paint_point > item.player_result.game_paint_point))
                            PlayerData.PointMVP++;

                        RuleData.Mode = RuleData.GameMode.Private;
                        RuleData.Name = "プライベートマッチ";
                        break;

                    // 調子メータについての処理
                    case "regular":
                        var winMeter = item.win_meter ?? 0;
                        PlayerData.WinMeter = winMeter;
                        PlayerData.ImageUri = new Uri(ApiUriPrefix.Substring(0, ApiUriPrefix.Length - 5) + item.player_result.player.weapon.image);

                        RuleData.Mode = RuleData.GameMode.Regular;
                        RuleData.Name = "ナワバリバトル";
                        break;

                    // フェス
                    case "fes_solo":
                        PlayerData.FesPowerDiff = 0;

                        var fesPower = item.fes_power ?? 0;

                        // 計測が終わった直後はDiffの表示は行わない
                        if (PlayerData.FesPower != 0)
                            PlayerData.FesPowerDiff = fesPower - PlayerData.FesPower;

                        PlayerData.FesPower = fesPower;

                        RuleData.Mode = RuleData.GameMode.FestivalSolo;
                        RuleData.Name = "フェス(チャレンジ)";
                        break;

                    case "fes_team":
                        PlayerData.ContributionPointTotal = item.contribution_point_total ?? 0;

                        RuleData.Mode = RuleData.GameMode.FestivalTeam;
                        RuleData.Name = "フェス(レギュラー)";
                        break;
                }
            }
        }

        public async Task<float> GetLoseXP()
        {
            var rule = await GetGachiSchedule();
            var powerData = await HttpManager.GetDeserializedJsonAsyncWithCookieContainer<SplatNet2DataStructure.XPowerRanking>(ApiUriPrefix + "x_power_ranking/" + GetSeason() + "/summary", Cookie);

            float GetXPower(string r, SplatNet2DataStructure.XPowerRanking.RuleStats m) => r == "X" && m.my_ranking != null ? float.Parse(m.my_ranking.x_power, CultureInfo.InvariantCulture) : 0;
            var xPower = new[]
            {
                GetXPower(PlayerData.Udemae[0], powerData.splat_zones),
                GetXPower(PlayerData.Udemae[1], powerData.tower_control),
                GetXPower(PlayerData.Udemae[2], powerData.rainmaker),
                GetXPower(PlayerData.Udemae[3], powerData.clam_blitz)
            };

            return (xPower[rule] * 10 - PlayerData.XPower[rule] * 10) / 10;
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
