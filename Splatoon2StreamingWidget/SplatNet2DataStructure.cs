using System.Collections.Generic;

namespace Splatoon2StreamingWidget
{
    public static class SplatNet2DataStructure
    {
        #region StreamingWidgetGitHub
        public class VersionData
        {
            public string version { get; set; }
        }
        #endregion

        #region SessionToken
        public class SessionToken
        {
            public string code { get; set; }
            public string session_token { get; set; }
        }

        public class AccessToken
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string id_token { get; set; }
        }

        public class UserInfo
        {
            public string birthday { get; set; }
            public string country { get; set; }
            public long createdAt { get; set; }
            public string gender { get; set; }
            public string language { get; set; }
            public string nickname { get; set; }
            public string screenname { get; set; }
        }

        public class SplatoonToken
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

        public class S2SResult
        {
            public string hash { get; set; }
        }
        #endregion

        #region SplatNet2
        public class Records
        {
            public PersonalRecords records;

            public class PersonalRecords
            {
                public PlayerRecords player;

                public class PlayerRecords
                {
                    public string nickname;
                    public string principal_id;
                    public RuleData udemae_zones;
                    public RuleData udemae_tower;
                    public RuleData udemae_rainmaker;
                    public RuleData udemae_clam;

                    public class RuleData
                    {
                        public bool is_number_reached;
                        public bool is_x;
                        public string name;
                        public string s_plus_number;
                    }
                }
            }
        }

        public class Schedules
        {
            public List<GachiSchedule> gachi;

            public class GachiSchedule
            {
                public decimal start_time;
                public decimal end_time;
                public RuleName rule;

                public class RuleName
                {
                    public string key;
                    public string name;
                }
            }
        }

        public class XPowerRanking
        {
            public RuleStats splat_zones;
            public RuleStats tower_control;
            public RuleStats rainmaker;
            public RuleStats clam_blitz;

            public class RuleStats
            {
                public MyRanking my_ranking;

                public class MyRanking
                {
                    public string x_power;
                    public string rank;
                }
            }
        }

        public class Results
        {
            public List<BattleResult> results;

            public class BattleResult
            {
                public int battle_number;
                public PlayerResult player_result;
                public MyTeamResult my_team_result;
                public GameMode game_mode;
                public RuleName rule;

                public float? win_meter; // turf
                public float? x_power; // gachi
                public float? max_league_point; // league
                public float? league_point; // league
                public float? my_estimate_league_point; // league
                public float? fes_power; // fes
                public int? contribution_point; // fes
                public long? contribution_point_total; // fes
                public float? estimate_gachi_power; // ?
                

                public int? my_team_count;
                public int? other_team_count;
                public float? my_team_percentage;
                public float? other_team_percentage;

                public class PlayerResult
                {
                    public PlayerDetail player;
                    public int kill_count;
                    public int assist_count;
                    public int death_count;
                    public int special_count;
                    public int game_paint_point;

                    public class PlayerDetail
                    {
                        public string principal_id;
                        public WeaponData weapon;
                        public Udemae udemae;

                        public class WeaponData
                        {
                            public string name;
                            public string image;
                        }

                        public class Udemae
                        {
                            public string name;
                            public bool is_x;
                            public string s_plus_number;
                        }
                    }
                }

                public class MyTeamResult
                {
                    public string name;
                    public string key; // victory, defeat
                }

                public class GameMode
                {
                    public string key; // gachi, league_pair, league_team, private, regular
                    public string name;
                }

                public class RuleName
                {
                    public string key;
                    public string name;
                }
            }
        }

        public class DetailResult
        {
            public List<MyTeamMembers> my_team_members;
            public List<OtherTeamMembers> other_team_members;

            public class MyTeamMembers
            {
                public int kill_count;
                public int assist_count;
                public int death_count;
                public int special_count;
                public int game_paint_point;
                public Player player;
            }

            public class OtherTeamMembers
            {
                public int kill_count;
                public int assist_count;
                public int death_count;
                public int special_count;
                public int game_paint_point;
                public Player player;
            }

            public class Player
            {
                public string nickname;
                public string principal_id;
            }
        }
        #endregion
    }
}
