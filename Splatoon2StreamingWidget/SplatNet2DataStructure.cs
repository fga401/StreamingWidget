using System.Collections.Generic;

namespace Splatoon2StreamingWidget
{
    public static class SplatNet2DataStructure
    {
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
                public int my_team_count;
                public int other_team_count;
                public PlayerResult player_result;
                public string x_power; // nullの可能性あり
                public GameMode game_mode;
                public RuleName rule;

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
                        public WeaponData weapon;

                        public class WeaponData
                        {
                            public string name;
                        }
                    }
                }

                public class GameMode
                {
                    public string key;
                    public string name;
                }

                public class RuleName
                {
                    public string key;
                    public string name;
                }
            }
        }
    }
}
