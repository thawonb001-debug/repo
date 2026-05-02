using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using TS_Server.DataTools;
using TS_Server.Guild;
using TS_Server.Server;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using TS_Server.PacketHandlers;
using System.Data;
using static TS_Server.DataTools.BattleGroundData;
using System.Runtime.ConstrainedExecution;

namespace TS_Server.Client
{
    public struct RoleInfo
    {
        public uint role_number;
        public uint role_accid;
        public string role_name;
        public byte role_level;
        public byte role_rank;
        public byte role_elm;
        public int goodness;
        public string date;
    }

    public class TSCharacter
    {
        public byte orient;
        public ushort horseID = 0;
        public TSParty party;
        public int agi;
        public int agi2;
        public int atk;
        public int atk2;
        public TSItemContainer bag, storage;
        public int charId;
        public TSClient client;
        public uint color1, color2;
        public int def;
        public int def2;
        public byte element;
        public TSEquipment[] equipment;
        public byte face;
        public ushort ghost, god;
        public uint gold, gold_bank;
        public byte hair;
        public uint honor;
        public int hp;
        public int hp2;
        public int hp_max;
        public int clone_hp;
        public int clone_sp;
        public ushort FullHpMax;
        public ushort FullHp_Max;
        public int hpx;
        public TSItemContainer inventory;
        public byte job, level;
        public int mag;
        public int mag2;
        public ushort mapID, mapX, mapY;
        public ushort s_mapID, s_mapX, s_mapY;
        public string name;
        public byte nb_equips;
        public int next_item;
        public int next_pet;
        public TSPet[] pet;
        public sbyte pet_battle;
        public TSPet[] pet_car;
        public TSPet[] pet_inn;
        public ushort horseSadd_Agi2;
        public byte rb;
        public byte sex;
        public ConcurrentDictionary<ushort, byte> skill;
        public int skill_point;
        public int sp;
        public int sp2;
        public int sp_max;
        public ushort FullSpMax;
        public ushort FullSp_Max;
        public int spx;
        public int stt_point;
        public byte style;
        public int currentxp;
        public uint totalxp;
        public double xp_pow;
        public ushort[] hotkey;
        public byte ball_point;
        public byte skill_ball;
        public byte int_ball;
        public byte atk_ball;
        public List<byte> allball;
        public bool[] ballList;
        public ushort[] skill_rb2;
        public byte ai;
        public byte itemhpsp;
        public byte onoffbt;
        public int guild_id;

        public ConcurrentDictionary<ushort, ushort> armypoint;
        public ConcurrentDictionary<ushort, ushort> uesitemcout;

        public int numberID;
        public int charID;
        public int itemID;
        public int uescout;

        public ushort outfitId = 0;
        System.Timers.Timer timerautosave = new System.Timers.Timer();
        public System.Timers.Timer timer = new System.Timers.Timer();
        System.Timers.Timer timerAutospSub = new System.Timers.Timer();
        System.Timers.Timer looktime = new System.Timers.Timer();
        System.Timers.Timer combotime = new System.Timers.Timer();
        System.Timers.Timer checkEndtime = new System.Timers.Timer();
        public bool PkSwich = false;
        public bool JamSwich = false;
        public uint streamBattleId = 0;
        public uint jamBattleId = 0;
        public byte gm;
        public bool gmchat = false;
        public bool syschat = false;
        public int point;

        // Trade related
        public uint myTradeItemsTraderId = 0;
        public uint myTradeItemsGold = 0;
        public bool myTradeItemsAccept = false;
        public int[] myTradeItemsRegisterSlots;
        public uint myTradePetTraderId = 0;
        public uint myTradePetGold = 0;
        public bool myTradePetAccept = false;
        public uint myTradePetRegisterSlot;
        public string myShopName;
        public int myShopImage = 1;
        public List<int[]> myShopItems;
        public uint visitShopId = 0;

        public bool autobotout = false;
        public bool botout = false;
        public bool autospSub = false;
        public byte autopotion = 0;

        // DB null check flags
        public bool boolequip = false;
        public bool boolinventory = false;
        public bool boolstorage = false;
        public bool boolbag = false;
        public bool boolskill = false;
        public bool boolskill_rb2 = false;
        public bool boolball_point = false;
        public bool boolballlist = false;
        public bool boolhotkey = false;
        public bool boolarmypoint = false;
        public bool booluesitemcout = false;
        public bool boolallball = false;
        public bool boolPetuesitemcout = false;
        public bool boolPetequip = false;

        public uint accid;
        public DateTime StartDate { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime ExpiryDate { get; private set; }
        public int DaysLeft { get; private set; }

        public TSCharCenter center;

        // Random instance (thread-safe)
        private static readonly Random _staticRandom = new Random();
        private static readonly object _randomLock = new object();
        private static int GetRandomIntStatic(int min, int max)
        {
            lock (_randomLock)
            {
                return _staticRandom.Next(min, max);
            }
        }

        public TSCharacter(TSClient c)
        {
            client = c;
            pet = new TSPet[14];
            next_pet = 0;
            pet_battle = -1;
            equipment = new TSEquipment[6];
            inventory = new TSItemContainer(this, 25);
            bag = new TSItemContainer(this, 25);
            storage = new TSItemContainer(this, 50);
            hotkey = new ushort[10];
            ballList = new bool[12];
            skill_rb2 = new ushort[8];
            next_item = 0;
            nb_equips = 0;
            skill = new ConcurrentDictionary<ushort, byte>();
            armypoint = new ConcurrentDictionary<ushort, ushort>();
            uesitemcout = new ConcurrentDictionary<ushort, ushort>();
        }

        // ====================== การจัดการฐานข้อมูลอย่างปลอดภัย (Parameterized + using) ======================

        public void loadAirtime()
        {
            string query = $"SELECT start_date_time, end_date_time FROM " + TSServer.config.tbAccount + " WHERE id = @accId";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@accId", client.accID);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        StartDate = reader.IsDBNull(0) ? changYear(DateTime.Now) : changYear(reader.GetDateTime(0));
                        ExpiryDate = reader.IsDBNull(1) ? changYear(DateTime.Now) : changYear(reader.GetDateTime(1));
                    }
                    else
                    {
                        Console.WriteLine("ไม่พบข้อมูลสำหรับ id " + client.accID);
                    }
                }
            }
        }

        public void loadCharDB()
        {
            string query = $"SELECT * FROM " + TSServer.config.tbChars + " WHERE accountid = @accId";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@accId", client.accID);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        try
                        {
                            charId = reader.GetInt32("id");
                            level = reader.GetByte("level");
                            hp = reader.GetInt32("hp");
                            hp = Math.Max(1, hp);
                            sp = reader.GetInt32("sp");
                            mag = reader.GetInt32("mag");
                            atk = reader.GetInt32("atk");
                            def = reader.GetInt32("def");
                            hpx = reader.GetInt32("hpx");
                            spx = reader.GetInt32("spx");
                            agi = reader.GetInt32("agi");
                            FullHpMax = reader.GetUInt16("fullhpmax");
                            FullSpMax = reader.GetUInt16("fullspmax");
                            hp2 = reader.GetInt32("hp2");
                            sp2 = reader.GetInt32("sp2");
                            mag2 = reader.GetInt32("mag2");
                            atk2 = reader.GetInt32("atk2");
                            def2 = reader.GetInt32("def2");
                            agi2 = reader.GetInt32("agi2");
                            skill_point = reader.GetInt32("sk_point");
                            stt_point = reader.GetInt32("stt_point");
                            sex = reader.GetByte("sex");
                            ghost = reader.GetUInt16("ghost");
                            god = reader.GetUInt16("god");
                            style = reader.GetByte("style");
                            hair = reader.GetByte("hair");
                            face = reader.GetByte("face");
                            color1 = reader.GetUInt32("color1");
                            color2 = reader.GetUInt32("color2");
                            mapID = reader.GetUInt16("map_id");
                            mapX = reader.GetUInt16("map_x");
                            mapY = reader.GetUInt16("map_y");
                            s_mapID = reader.GetUInt16("s_map_id");
                            s_mapX = reader.GetUInt16("s_map_x");
                            s_mapY = reader.GetUInt16("s_map_y");
                            currentxp = reader.GetInt32("exp");
                            totalxp = reader.GetUInt32("exp_tot");
                            honor = reader.GetUInt32("honor");
                            element = reader.GetByte("element");
                            rb = reader.GetByte("reborn");
                            job = reader.GetByte("job");
                            gold = reader.GetUInt32("gold");
                            gold_bank = reader.GetUInt32("gold_bank");
                            name = reader.GetString("name");
                            pet_battle = (sbyte)reader.GetByte("pet_battle");
                            ai = reader.GetByte("ai");
                            onoffbt = reader.GetByte("onoffbt");

                            center = JsonConvert.DeserializeObject<TSCharCenter>(reader.GetString("center"));
                            if (center == null) center = new TSCharCenter();
                            center.init(this);

                            // โหลด JSON fields โดยใช้ helper
                            boolequip = loadEquipmentJsonFromReader(reader);
                            boolinventory = loadInventoryJsonFromReader(reader);
                            boolstorage = loadStorageJsonFromReader(reader);
                            boolbag = loadBagJsonFromReader(reader);
                            boolskill = loadSkillFromReader(reader);
                            boolskill_rb2 = loadSkillRb2FromReader(reader);
                            boolball_point = loadBallPointFromReader(reader);
                            boolballlist = loadBallListFromReader(reader);
                            boolhotkey = loadHotkeyFromReader(reader);
                            booluesitemcout = loadUesitemcoutFromReader(reader);
                            boolallball = loadAllballFromReader(reader);
                        }
                        catch (Exception e)
                        {
                            WriteLog.ErrorDB("At loadCharDB >> " + e);
                        }
                    }
                }
            }

            hp_max = getHpMax();
            sp_max = getSpMax();
            hp_max = hp_max > 50000 ? 50000 : hp_max;
            sp_max = sp_max > 50000 ? 50000 : sp_max;
            hp = hp > hp_max ? hp_max : hp;
            sp = sp > sp_max ? sp_max : sp;
            xp_pow = rb == 0 ? 2.9 : rb == 1 ? 3.0 : 3.05;

            loadPet();
        }

        // Helper methods for loading JSON safely from reader
        private bool loadEquipmentJsonFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("equip")) ? null : reader.GetString("equip");
            if (!string.IsNullOrEmpty(json))
            {
                loadEquipmentJson(JArray.Parse(json));
                return false;
            }
            loadEquipmentJson(JArray.Parse("[[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]"));
            return true;
        }

        private bool loadInventoryJsonFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("inventory")) ? null : reader.GetString("inventory");
            if (!string.IsNullOrEmpty(json))
            {
                inventory.loadContainerJson(JArray.Parse(json));
                return false;
            }
            inventory.loadContainerJson(JArray.Parse("[[0,0,0,0,0,0,0,0,0,0], ...]")); // ใช้ default
            return true;
        }

        private bool loadStorageJsonFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("storage")) ? null : reader.GetString("storage");
            if (!string.IsNullOrEmpty(json))
            {
                storage.loadContainerJson(JArray.Parse(json));
                return false;
            }
            storage.loadContainerJson(JArray.Parse("[[0,0,0,0,0,0,0,0,0,0], ...]"));
            return true;
        }

        private bool loadBagJsonFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("bag")) ? null : reader.GetString("bag");
            if (!string.IsNullOrEmpty(json))
            {
                bag.loadContainerJson(JArray.Parse(json));
                return false;
            }
            bag.loadContainerJson(JArray.Parse("[[0,0,0,0,0,0,0,0,0,0], ...]"));
            return true;
        }

        private bool loadSkillFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("skill")) ? null : reader.GetString("skill");
            if (!string.IsNullOrEmpty(json))
            {
                skill = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, byte>>(json);
                return false;
            }
            skill = new ConcurrentDictionary<ushort, byte>();
            return true;
        }

        private bool loadSkillRb2FromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("skill_rb2")) ? null : reader.GetString("skill_rb2");
            if (!string.IsNullOrEmpty(json))
            {
                skill_rb2 = JsonConvert.DeserializeObject<ushort[]>(json);
                return false;
            }
            skill_rb2 = new ushort[8];
            return true;
        }

        private bool loadBallPointFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("ball_point")) ? null : reader.GetString("ball_point");
            if (!string.IsNullOrEmpty(json))
            {
                ball_point = JsonConvert.DeserializeObject<byte>(json);
                return false;
            }
            ball_point = 0;
            return true;
        }

        private bool loadBallListFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("ballList")) ? null : reader.GetString("ballList");
            if (!string.IsNullOrEmpty(json))
            {
                ballList = JsonConvert.DeserializeObject<bool[]>(json);
                if (ballList == null) ballList = new bool[12];
                return false;
            }
            ballList = new bool[12];
            return true;
        }

        private bool loadHotkeyFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("hotkey")) ? null : reader.GetString("hotkey");
            if (!string.IsNullOrEmpty(json))
            {
                hotkey = JsonConvert.DeserializeObject<ushort[]>(json);
                return false;
            }
            hotkey = new ushort[10];
            return true;
        }

        private bool loadUesitemcoutFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("uesitemcout")) ? null : reader.GetString("uesitemcout");
            if (!string.IsNullOrEmpty(json))
            {
                uesitemcout = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, ushort>>(json);
                return false;
            }
            uesitemcout = new ConcurrentDictionary<ushort, ushort>();
            return true;
        }

        private bool loadAllballFromReader(MySqlDataReader reader)
        {
            string json = reader.IsDBNull(reader.GetOrdinal("allball")) ? null : reader.GetString("allball");
            if (!string.IsNullOrEmpty(json))
            {
                allball = JsonConvert.DeserializeObject<List<byte>>(json);
                return false;
            }
            allball = new List<byte> { 0, 0, 0 };
            return true;
        }

        public void loadMallpoint()
        {
            string query = $"SELECT gm, point FROM " + TSServer.config.tbAccount + " WHERE id = @id";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", client.accID);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        gm = reader.GetByte("gm");
                        point = reader.GetInt32("point");
                    }
                }
            }
        }

        public void SavePoint()
        {
            string query = $"UPDATE " + TSServer.config.tbAccount + " SET point = @point WHERE Id = @id";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@point", point);
                cmd.Parameters.AddWithValue("@id", client.accID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void initChar(byte[] data, byte[] name)
        {
            loadAirtime();
            if (TSServer.config.airtime > 0)
            {
                Add_Airtime(TSServer.config.airtime);
                CreationDate = changYear(DateTime.Now);
                StartDate = CreationDate;
            }
            else
            {
                CreationDate = StartDate;
            }

            string ctime = CreationDate.ToString("yyyy-MM-dd HH:mm:ss");
            string extime = ExpiryDate.ToString("yyyy-MM-dd HH:mm:ss");
            string pass1 = PacketReader.readString(data, 22, data[21]);
            string pass2 = PacketReader.readString(data, 22 + pass1.Length + 1, data[22 + pass1.Length]);

            // อัปเดต account
            string updateAccount = $"UPDATE " + TSServer.config.tbAccount + " SET password = @pass1, password2 = @pass2, start_date_time = @start, end_date_time = @end WHERE id = @id";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(updateAccount, conn))
            {
                cmd.Parameters.AddWithValue("@pass1", pass1);
                cmd.Parameters.AddWithValue("@pass2", pass2);
                cmd.Parameters.AddWithValue("@start", ctime);
                cmd.Parameters.AddWithValue("@end", extime);
                cmd.Parameters.AddWithValue("@id", client.accID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // สร้างตัวละคร
            string insertChar = $@"INSERT INTO " + TSServer.config.tbChars + @" 
                (accountid, name, mag, atk, def, hpx, spx, agi, sex, style, hair, face, color1, color2, element, map_id, map_x, map_y, s_map_id, s_map_x, s_map_y, equip, inventory, storage, bag, allball, ball_point, skill, skill_rb2, balllist, hotkey, uesitemcout)
                VALUES (@accId, @name, @mag, @atk, @def, @hpx, @spx, @agi, @sex, @style, @hair, @face, @color1, @color2, @element, @mapId, @mapX, @mapY, @sMapId, @sMapX, @sMapY, @equip, @inventory, @storage, @bag, @allball, @ballPoint, @skill, @skillRb2, @balllist, @hotkey, @uesitemcout)";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(insertChar, conn))
            {
                cmd.Parameters.AddWithValue("@accId", client.accID);
                cmd.Parameters.AddWithValue("@name", Encoding.UTF8.GetString(name)); // เปลี่ยนเป็น UTF8
                cmd.Parameters.AddWithValue("@mag", data[15]);
                cmd.Parameters.AddWithValue("@atk", data[16]);
                cmd.Parameters.AddWithValue("@def", data[17]);
                cmd.Parameters.AddWithValue("@hpx", data[18]);
                cmd.Parameters.AddWithValue("@spx", data[19]);
                cmd.Parameters.AddWithValue("@agi", data[20]);
                cmd.Parameters.AddWithValue("@sex", data[2]);
                cmd.Parameters.AddWithValue("@style", data[3]);
                cmd.Parameters.AddWithValue("@hair", data[4]);
                cmd.Parameters.AddWithValue("@face", data[5]);
                cmd.Parameters.AddWithValue("@color1", PacketReader.read32(data, 6));
                cmd.Parameters.AddWithValue("@color2", PacketReader.read32(data, 10));
                cmd.Parameters.AddWithValue("@element", data[14]);
                cmd.Parameters.AddWithValue("@mapId", 10817);
                cmd.Parameters.AddWithValue("@mapX", 442);
                cmd.Parameters.AddWithValue("@mapY", 758);
                cmd.Parameters.AddWithValue("@sMapId", 12003);
                cmd.Parameters.AddWithValue("@sMapX", 500);
                cmd.Parameters.AddWithValue("@sMapY", 500);
                cmd.Parameters.AddWithValue("@equip", "[[0,0,0,0,0,0,0,0,0,0],[2,19737,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]");
                cmd.Parameters.AddWithValue("@inventory", "[]");
                cmd.Parameters.AddWithValue("@storage", "[]");
                cmd.Parameters.AddWithValue("@bag", "[]");
                cmd.Parameters.AddWithValue("@allball", "[0,0,0]");
                cmd.Parameters.AddWithValue("@ballPoint", 0);
                cmd.Parameters.AddWithValue("@skill", "{}");
                cmd.Parameters.AddWithValue("@skillRb2", "[0,0,0,0,0,0,0,0]");
                cmd.Parameters.AddWithValue("@balllist", "[false,false,false,false,false,false,false,false,false,false,false,false]");
                cmd.Parameters.AddWithValue("@hotkey", "[0,0,0,0,0,0,0,0,0,0]");
                cmd.Parameters.AddWithValue("@uesitemcout", "{}");

                conn.Open();
                cmd.ExecuteNonQuery();
                charId = (int)cmd.LastInsertedId;
            }
        }

        public void saveCharDB(MySqlConnection conn) // รับ connection จากภายนอก
        {
            string query = $@"UPDATE " + TSServer.config.tbChars + @" SET 
                level = @level, exp = @curr_exp, exp_tot = @exp_tot, hp = @hp, fullhpmax = @fullhpmax,
                sp = @sp, fullspmax = @fullspmax, mag = @mag, atk = @atk, def = @def, hpx = @hpx,
                spx = @spx, agi = @agi, sk_point = @sk_point, stt_point = @stt_point, ghost = @ghost,
                god = @god, map_id = @map_id, map_x = @map_x, map_y = @map_y, s_map_id = @s_map_id,
                s_map_x = @s_map_x, s_map_y = @s_map_y, gold = @gold, hair = @hair, color1 = @color1,
                color2 = @color2, gold_bank = @gold_bank, element = @element, honor = @honor,
                pet_battle = @pet_battle, equip = @equip, inventory = @inventory, bag = @bag,
                storage = @storage, skill = @skill, skill_rb2 = @skill_rb2, ball_point = @ball_point,
                balllist = @balllist, hotkey = @hotkey, uesitemcout = @uesitemcout, reborn = @rb,
                job = @job, ai = @ai, onoffbt = @onoffbt, allball = @allball, center = @center
                WHERE accountid = @id";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@level", level);
                cmd.Parameters.AddWithValue("@curr_exp", currentxp);
                cmd.Parameters.AddWithValue("@exp_tot", totalxp);
                cmd.Parameters.AddWithValue("@hp", hp);
                cmd.Parameters.AddWithValue("@fullhpmax", FullHpMax);
                cmd.Parameters.AddWithValue("@sp", sp);
                cmd.Parameters.AddWithValue("@fullspmax", FullSpMax);
                cmd.Parameters.AddWithValue("@mag", mag);
                cmd.Parameters.AddWithValue("@atk", atk);
                cmd.Parameters.AddWithValue("@def", def);
                cmd.Parameters.AddWithValue("@hpx", hpx);
                cmd.Parameters.AddWithValue("@spx", spx);
                cmd.Parameters.AddWithValue("@agi", agi);
                cmd.Parameters.AddWithValue("@sk_point", skill_point);
                cmd.Parameters.AddWithValue("@stt_point", stt_point);
                cmd.Parameters.AddWithValue("@ghost", ghost);
                cmd.Parameters.AddWithValue("@god", god);
                cmd.Parameters.AddWithValue("@hair", hair);
                cmd.Parameters.AddWithValue("@color1", color1);
                cmd.Parameters.AddWithValue("@color2", color2);
                cmd.Parameters.AddWithValue("@map_id", mapID);
                cmd.Parameters.AddWithValue("@map_x", mapX);
                cmd.Parameters.AddWithValue("@map_y", mapY);
                cmd.Parameters.AddWithValue("@s_map_id", s_mapID);
                cmd.Parameters.AddWithValue("@s_map_x", s_mapX);
                cmd.Parameters.AddWithValue("@s_map_y", s_mapY);
                cmd.Parameters.AddWithValue("@gold", gold);
                cmd.Parameters.AddWithValue("@gold_bank", gold_bank);
                cmd.Parameters.AddWithValue("@honor", honor);
                cmd.Parameters.AddWithValue("@pet_battle", pet_battle);
                cmd.Parameters.AddWithValue("@equip", JsonConvert.SerializeObject(saveEquipmentJson(), Formatting.None));
                cmd.Parameters.AddWithValue("@inventory", JsonConvert.SerializeObject(inventory.saveContainerJson(), Formatting.None));
                cmd.Parameters.AddWithValue("@bag", JsonConvert.SerializeObject(bag.saveContainerJson(), Formatting.None));
                cmd.Parameters.AddWithValue("@storage", JsonConvert.SerializeObject(storage.saveContainerJson(), Formatting.None));
                cmd.Parameters.AddWithValue("@skill", JsonConvert.SerializeObject(skill, Formatting.None));
                cmd.Parameters.AddWithValue("@skill_rb2", JsonConvert.SerializeObject(skill_rb2, Formatting.None));
                cmd.Parameters.AddWithValue("@ball_point", JsonConvert.SerializeObject(ball_point, Formatting.None));
                cmd.Parameters.AddWithValue("@balllist", JsonConvert.SerializeObject(ballList, Formatting.None));
                cmd.Parameters.AddWithValue("@hotkey", JsonConvert.SerializeObject(hotkey, Formatting.None));
                cmd.Parameters.AddWithValue("@uesitemcout", JsonConvert.SerializeObject(uesitemcout, Formatting.None));
                cmd.Parameters.AddWithValue("@rb", rb);
                cmd.Parameters.AddWithValue("@job", job);
                cmd.Parameters.AddWithValue("@element", element);
                cmd.Parameters.AddWithValue("@ai", ai);
                cmd.Parameters.AddWithValue("@onoffbt", onoffbt);
                cmd.Parameters.AddWithValue("@allball", JsonConvert.SerializeObject(allball, Formatting.None));
                cmd.Parameters.AddWithValue("@center", JsonConvert.SerializeObject(center, Formatting.None));
                cmd.Parameters.AddWithValue("@id", client.accID);

                cmd.ExecuteNonQuery();
            }
            SavePoint();
            if (!client.removeChr)
                client.saveQuest();
        }

        public void loadPet()
        {
            string query = $"SELECT pet_sid, slot, location FROM " + TSServer.config.tbPet + " WHERE charid = @charId";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@charId", charId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int s = reader.GetInt32("slot");
                        int sid = reader.GetInt32("pet_sid");
                        if (s - 1 >= 0 && s - 1 < pet.Length)
                        {
                            pet[s - 1] = new TSPet(this, sid, (byte)s);
                            pet[s - 1].loadPetDB();
                        }
                    }
                }
            }
            nextPet();
        }

        public void changePetName(byte slot, byte[] newName)
        {
            if (pet[slot - 1] == null) return;
            string newNameString = Encoding.UTF8.GetString(newName);
            string query = $"UPDATE " + TSServer.config.tbPet + " SET `name` = @name WHERE pet_sid = @pet_sid";
            using (var conn = new MySqlConnection(TSServer.config.dbConnectionString))
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", newNameString);
                cmd.Parameters.AddWithValue("@pet_sid", pet[slot - 1].pet_sid);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            pet[slot - 1].name = newNameString;
            pet[slot - 1].nameBytes = Encoding.UTF8.GetBytes(newNameString);
            PacketCreator p = new PacketCreator(0xf, 9);
            p.add32(client.accID);
            p.add8(slot);
            p.addBytes(pet[slot - 1].nameBytes);
            reply(p.send());
        }

        public DateTime changYear(DateTime date)
        {
            if (date.Year > 2400)
            {
                return date.AddYears(-543);
            }
            return date;
        }

        public void Add_Airtime(int cout)
        {
            if (ExpiryDate > DateTime.Now)
            {
                DateTime oldCreationDate = ExpiryDate;
                ExpiryDate = oldCreationDate.AddDays(cout);
            }
            else
            {
                CreationDate = DateTime.Now;
                ExpiryDate = CreationDate.AddDays(cout);
            }
        }

        public void UpdateDaysLeft()
        {
            TimeSpan timeRemaining = ExpiryDate - DateTime.Now;
            DaysLeft = (int)timeRemaining.TotalDays;
            announce("เหลือเวลาเล่น >> " + DaysLeft);
        }

        public void PlayGame()
        {
            UpdateDaysLeft();
        }

        public void checktimeout()
        {
            Console.WriteLine($"วันที่เติม: {CreationDate}");
            Console.WriteLine($"วันหมดอายุ: {ExpiryDate}");
            Console.WriteLine($"วันที่เหลืออยู่: {DaysLeft} วัน");
            Console.WriteLine($"วันที่เหลืออยู่: {DaysLeft} วัน");
        }

        public bool checkDBnull(MySqlDataReader data, string str)
        {
            switch (str)
            {
                case "equip":
                    if (data.GetString("equip") != "" && !data.IsDBNull(data.GetOrdinal("equip")))
                    {
                        loadEquipmentJson(JArray.Parse(data.GetString("equip")));
                    }
                    else
                    {
                        string jstring = "[[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]";
                        loadEquipmentJson(JArray.Parse(jstring));
                        return true;
                    }
                    break;
                case "inventory":
                    if (data.GetString("inventory") != "" && !data.IsDBNull(data.GetOrdinal("inventory")))
                    {
                        inventory.loadContainerJson(JArray.Parse(data.GetString("inventory")));
                    }
                    else
                    {
                        string jstring = "[[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]";
                        inventory.loadContainerJson(JArray.Parse(jstring));
                        return true;
                    }
                    break;
                case "storage":
                    if (data.GetString("storage") != "" && !data.IsDBNull(data.GetOrdinal("storage")))
                    {
                        storage.loadContainerJson(JArray.Parse(data.GetString("storage")));
                    }
                    else
                    {
                        string jstring = "[[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,4,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]";
                        storage.loadContainerJson(JArray.Parse(jstring));
                        return true;
                    }
                    break;
                case "bag":
                    if (data.GetString("bag") != "" && !data.IsDBNull(data.GetOrdinal("bag")))
                    {
                        bag.loadContainerJson(JArray.Parse(data.GetString("bag")));
                    }
                    else
                    {
                        string jstring = "[[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,4,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]";
                        bag.loadContainerJson(JArray.Parse(jstring));
                        return true;
                    }
                    break;
                case "skill":
                    if (data.GetString("skill") != "" && !data.IsDBNull(data.GetOrdinal("skill")))
                    {
                        skill = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, byte>>(data.GetString("skill"));
                    }
                    else
                    {
                        skill = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, byte>>("{}");
                        return true;
                    }
                    break;
                case "skill_rb2":
                    if (data.GetString("skill_rb2") != "" && !data.IsDBNull(data.GetOrdinal("skill_rb2")))
                    {
                        skill_rb2 = JsonConvert.DeserializeObject<ushort[]>(data.GetString("skill_rb2"));
                    }
                    else
                    {
                        skill_rb2 = JsonConvert.DeserializeObject<ushort[]>("[0,0,0,0,0,0,0,0]");
                        return true;
                    }
                    break;
                case "ball_point":
                    if (data.GetString("ball_point") != "" && !data.IsDBNull(data.GetOrdinal("ball_point")))
                    {
                        ball_point = JsonConvert.DeserializeObject<byte>(data.GetString("ball_point"));
                    }
                    else
                    {
                        ball_point = JsonConvert.DeserializeObject<byte>("0");
                        return true;
                    }
                    break;
                case "ballList":
                    if (data.GetString("ballList") != "" && !data.IsDBNull(data.GetOrdinal("ballList")))
                    {
                        ballList = JsonConvert.DeserializeObject<bool[]>(data.GetString("ballList"));
                    }
                    else
                    {
                        ballList = JsonConvert.DeserializeObject<bool[]>("[false,false,false,false,false,false,false,false,false,false,false,false]");
                        return true;
                    }
                    break;
                case "hotkey":
                    if (data.GetString("hotkey") != "" && !data.IsDBNull(data.GetOrdinal("hotkey")))
                    {
                        hotkey = JsonConvert.DeserializeObject<ushort[]>(data.GetString("hotkey"));
                    }
                    else
                    {
                        hotkey = JsonConvert.DeserializeObject<ushort[]>("[0,0,0,0,0,0,0,0,0,0]");
                        return true;
                    }
                    break;
                case "armypoint":
                    if (data.GetString("armypoint") != "" && !data.IsDBNull(data.GetOrdinal("armypoint")))
                    {
                        armypoint = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, ushort>>(data.GetString("armypoint"));
                    }
                    else
                    {
                        armypoint = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, ushort>>("{\"1\":0,\"2\":0,\"3\":0,\"4\":0,\"5\":0}");// ค่ายทหาร  
                        return true;
                    }
                    break;
                case "uesitemcout":
                    if (data.GetString("uesitemcout") != "" && !data.IsDBNull(data.GetOrdinal("uesitemcout")))
                    {
                        uesitemcout = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, ushort>>(data.GetString("uesitemcout"));
                    }
                    else
                    {
                        uesitemcout = JsonConvert.DeserializeObject<ConcurrentDictionary<ushort, ushort>>("{}");
                        return true;
                    }
                    break;
                case "allball":
                    if (data.GetString("allball") != "" && !data.IsDBNull(data.GetOrdinal("allball")))
                    {
                        allball = JsonConvert.DeserializeObject<List<byte>>(data.GetString("allball"));
                    }
                    else
                    {
                        allball = JsonConvert.DeserializeObject<List<byte>>("[0,0,0]");
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        public void loadMallpoint()
        {
            try
            {
                var c = new TSMysqlConnection();
                MySqlDataReader data = c.selectQuery("SELECT * FROM " + TSServer.config.tbAccount + " WHERE id = " + client.accID);
                data.Read();
                gm = data.GetByte("gm");
                point = data.GetInt32("point");
                data.Close();
                c.connection.Close();
            }
            catch (Exception e)
            {
                WriteLog.Error("loadMallpoint : " + e);
            }
        }

        public void expitem()
        {
            for (int i = 0; i < equipment.Length; i++)
            {
                if (ItemSoul.ItemSoulList.TryGetValue(equipment[i].Itemid, out var soulvar))
                {
                    var p = new PacketCreator(0x17, 0x0b);
                    if (soulvar.itemID > 0)
                    {
                        p.add16(equipment[i].Itemid);
                        p.addByte(equipment[i].quantity);
                        p.addByte((byte)(equipment[i]?.elem_type ?? 0));
                        p.addByte((byte)(100 + (byte)(equipment[i]?.elem_val ?? 0)));
                        p.addByte((byte)(equipment[i]?.anti ?? 0));
                        p.add32((uint)(equipment[i]?.exp ?? 0));
                    }
                    reply(p.send());
                }
            }
        }

        public void armor_exp_soulitem(int lv)
        {
            foreach (TSEquipment equ in equipment.Where(equ => equ != null && equ.level <= lv && equ.Itemid != 0))
            {
                if (equ.elem_val == 20)
                    continue;
                TSEquipment equ2 = equipment[equ.slot - 1];
                if (ItemSoul.ItemSoulList.TryGetValue(equ.Itemid, out ItemSoulinfo val) && ItemData.itemList.TryGetValue(equ2.Itemid, out var itemm) && itemm.equippos != 0 && itemm.equippos != 3 && itemm.equippos != 6)
                {
                    equ2.exp = Math.Min(equ2.exp + (uint)TSServer.config.perSoulexp, val.soulExp[20]);
                    for (int i = 1; i < 21; i++)
                    {
                        if (equ.exp >= val.soulExp[i] && equ.exp < val.soulExp[i + 1])
                        {
                            int odl_elem_val = equ2.elem_val;
                            equ2.elem_val = i;
                            addEquipSoul(itemm.prop1, equ2.elem_val, odl_elem_val, 0);
                            addEquipSoul(itemm.prop2, equ2.elem_val, odl_elem_val, 0);
                            refreshBonus();
                        }
                    }
                    sendEquip();
                }
            }
        }

        public void weapon_exp_soulitem(int lv)
        {
            foreach (TSEquipment equ in equipment.Where(equ => equ != null && equ.level <= lv && equ.Itemid != 0))
            {
                if (equ.elem_val == 20)
                    continue;
                TSEquipment equ2 = equipment[equ.slot - 1];
                if (ItemSoul.ItemSoulList.TryGetValue(equ.Itemid, out ItemSoulinfo val) && ItemData.itemList.TryGetValue(equ2.Itemid, out var itemm) && itemm.equippos == 3)
                {
                    equ2.exp = Math.Min(equ2.exp + (uint)TSServer.config.perSoulexp, val.soulExp[20]);
                    for (int i = 1; i < 21; i++)
                    {
                        if (equ.exp >= val.soulExp[i] && equ.exp < val.soulExp[i + 1])
                        {
                            int odl_elem_val = equ2.elem_val;
                            equ2.elem_val = i;
                            addEquipSoul(itemm.prop1, equ2.elem_val, odl_elem_val, 0);
                            addEquipSoul(itemm.prop2, equ2.elem_val, odl_elem_val, 0);
                            refreshBonus();
                        }
                    }
                    sendEquip();
                }
            }
        }

        public void StartAutosave()
        {
            timerautosave.Interval = 30000;
            timerautosave.Elapsed += autosaveDB;
            timerautosave.Start();
        }

        public void StopAutosave()
        {
            timerautosave.Stop();
        }

        public void StartAutoSpSub()
        {
            timerAutospSub.Interval = 10000;
            timerAutospSub.Elapsed += autoSpSub;
            timerAutospSub.Start();
        }

        public void StopAutoSpSup()
        {
            timerAutospSub.Stop();
        }

        public void Tike()
        {
            timer.Interval = 1000;
            if (client.outgame == true)
            {
                Toke();
            }
            else
            {
                timer.Start();
                timer.Elapsed += timer_Elapsed;
            }
        }

        public void Toke()
        {
            timer.Stop();
        }

        public void lookTike()
        {
            looktime.Interval = 200;
            if (client.targetBattle == false)
            {
                lookToke();
            }
            else
            {
                looktime.Elapsed += timer_ElapsedLook;
                looktime.Start();
            }
        }

        public void lookToke()
        {
            looktime.Stop();
            Cosslukshin();
        }

        public void lukshin()
        {
            var p = new PacketCreator(0x0B, 07);
            p.addByte(0x02);
            p.addByte(0xff);
            client.reply(p.send());
        }

        public void Cosslukshin()
        {
            var p = new PacketCreator(0x0B, 07);
            p.addByte(0x00);
            p.addByte(0x00);
            client.reply(p.send());
        }

        public void checkcombotimeTick()
        {
            combotime.Interval = 3600000;
            combotime.Start();
            combotime.Elapsed += timer_ElapsedCombotimr;
        }

        public void checkEndAirtime()
        {
            checkEndtime.Interval = 1000;
            checkEndtime.Start();
            checkEndtime.Elapsed += timer_ElapsedEnditem;
        }

        public void timer_ElapsedEnditem(object sedr, System.Timers.ElapsedEventArgs e)
        {
            if (DateAndTime.Now > ExpiryDate && gm != 1)
            {
                announce("ตัดการเชื่อมต่อเนื่องจาก AirTime หมด");
                client.map.listPlayers.TryRemove(client.accID, out TSClient clientMap1);
                if (clientMap1 != null)
                {
                    clientMap1.disconnect();
                    TSServer.listPlayers.TryRemove(client.accID, out TSClient clientListServer1);
                    if (clientListServer1 != null)
                        clientListServer1.disconnect();
                }
                PacketCreator p = new PacketCreator(0x23, 2);
                p.addByte((byte)1);
                client.reply(p.send());
                checkEndtime.Stop();
            }
        }

        public void endtimetoke()
        {
            checkEndtime.Stop();
        }

        public void checkcombotimeToke()
        {
            combotime.Stop();
        }

        public void timer_ElapsedCombotimr(object sedr, EventArgs e)
        {
            if (point < 100 && gm == 2)
            {
                combotime.Stop();
                client.aicombo = false;
                announce("ปิดใช้งาน combo 100 % เนื่องจากพ้อยไม่พอ");
                if (party != null)
                {
                    for (int i = 0; i < party.member.Count; i++)
                    {
                        if (party.member[i].client.accID != client.accID)
                        {
                            client.getChar().announce("ปิดใช้งาน combo 100 % ลูกตี้ชื่อ [ " + party.member[i].client.getChar().name + " ] เนื่องจากพ้อยไม่พอ");
                            party.member[i].client.aicombo = false;
                            party.member[i].client.getChar().announce("ปิดใช้งาน combo 100 % เนื่องจากหัวตี้ไม่พอ");
                        }
                    }
                }
            }
            else
            {
                announce("ใช้งาน combo 100 % ต่อเนื่อง >> " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"));
                point -= 100;
                sendpoint();
            }
        }

        public void timer_Elapsed(object sedr, EventArgs e)
        {
            if (client.battle == null)
            {
                try
                {
                    if (!EveData.listNpcOnMap.ContainsKey(client.map.mapid) && EveData.listNpcOnMap[client.map.mapid].Count < 1)
                    {
                        offlinemodeOff();
                        timer.Stop();
                        return;
                    }
                    if (EveData.listNpcOnMap.TryGetValue(client.map.mapid, out var x))
                    {
                        var EvntID = x.Where(xx => xx.npcForeast == 1).ToArray();
                        if (EvntID.Length > 0)
                        {
                            var EvnID = EvntID[GetRandomIntStatic(0, EvntID.Length)].EventId[0];
                            StepQ[] steps = EveData.listStepQust[client.map.mapid].Where(item => item.EvenID == EvnID && item.resBattle == 0).ToArray();
                            if (client.battle == null && steps != null)
                            {
                                client.BattlecurrentStep = steps[GetRandomIntStatic(0, steps.Length)];
                                client.processStepEncouter(client);
                            }
                        }
                        else
                        {
                            offlinemodeOff();
                            timer.Stop();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("AI BOT " + ex.ToString());
                }
            }
        }

        public void offlinemodeOff()
        {
            announce("ปิดใช้โหมด OFFLINE เนื่องจากไม่ได้อยู่ในป่า");
            client.offlineAi = false;
            client.autobot = true;
            client.aicombo = false;
            client.autosell = false;
            client.sellitem = 0;
            autobotout = false;
            if (party != null)
            {
                for (int n = 0; n < party.member.Count; n++)
                {
                    if (party.member[n].client.accID != client.accID)
                    {
                        party.member[n].client.offlineAi = false;
                        party.member[n].client.aicombo = false;
                        party.member[n].client.autosell = false;
                        party.member[n].client.sellitem = 0;
                    }
                }
            }
        }

        public void winautobot()
        {
            if (autobotout)
            {
                setHp(getHpMax());
                refresh(hp, TSConstants._HP);
                setSp(getSpMax());
                refresh(sp, TSConstants._SP);
                if (this.pet_battle != -1)
                {
                    TSPet pet = this.pet[this.pet_battle];
                    pet.setHp(pet.getHpMax());
                    pet.refresh(pet.hp, TSConstants._HP);
                    pet.setSp(pet.getSpMax());
                    pet.refresh(pet.sp, TSConstants._SP);
                }
                if (party != null)
                {
                    for (int n = 0; n < party.member.Count; n++)
                    {
                        if (party.member[n].client.accID != client.accID)
                        {
                            party.member[n].setHp(party.member[n].getHpMax());
                            party.member[n].refresh(party.member[n].hp, TSConstants._HP);
                            party.member[n].setSp(party.member[n].getSpMax());
                            party.member[n].refresh(party.member[n].sp, TSConstants._SP);
                            if (party.member[n].pet_battle != -1)
                            {
                                TSPet pet = party.member[n].pet[party.member[n].pet_battle];
                                pet.setHp(pet.getHpMax());
                                pet.refresh(pet.hp, TSConstants._HP);
                                pet.setSp(pet.getSpMax());
                                pet.refresh(pet.sp, TSConstants._SP);
                            }
                        }
                    }
                }
            }
        }

        public void winbot()
        {
            if (botout)
            {
                lukshin();
                setHp(getHpMax());
                refresh(hp, TSConstants._HP);
                setSp(getSpMax());
                refresh(sp, TSConstants._SP);
                if (this.pet_battle != -1)
                {
                    TSPet pet = this.pet[this.pet_battle];
                    pet.setHp(pet.getHpMax());
                    pet.refresh(pet.hp, TSConstants._HP);
                    pet.setSp(pet.getSpMax());
                    pet.refresh(pet.sp, TSConstants._SP);
                }
            }
        }

        public void hpsp()
        {
            if (ai == 1 && (point > 9 || gm == 1))
            {
                if (gm != 1)
                {
                    point -= 1;
                    if (point < 1)
                    {
                        ai = 0;
                        announce("ปิดใช้งาน AUTOHP_SP เนื่องจาก point ไม่พอ");
                        client.saveChrtoDB();
                    }
                    sendpoint();
                }
                setHp(getHpMax());
                refresh(hp, TSConstants._HP);
                setSp(getSpMax());
                refresh(sp, TSConstants._SP);
                if (this.pet_battle != -1)
                {
                    TSPet pet = this.pet[this.pet_battle];
                    pet.setHp(pet.getHpMax());
                    pet.refresh(pet.hp, TSConstants._HP);
                    pet.setSp(pet.getSpMax());
                    pet.refresh(pet.sp, TSConstants._SP);
                }
            }
            else
            {
                announce("ปิดใช้งาน AUTOHP_SP เนื่องจาก point ไม่พอ");
                client.saveChrtoDB();
            }
        }

        public void autosell()
        {
            foreach (var item in inventory.items)
            {
                if (item == null) continue;
                byte total_hp_potion_used = 0;
                var hpItems = ItemData.itemList[item.Itemid];
                if (client.sellitem == 0 && item.quantity == 50 && (hpItems.prop1 == 25 || hpItems.prop1 == 26 || hpItems.prop2 == 25 || hpItems.prop2 == 26) && hpItems.equippos == 0)
                {
                    byte quantity = item.quantity;
                    total_hp_potion_used = quantity;
                    gold += (quantity * hpItems.SellingPrice);
                    inventory.items[item.slot - 1] = null;
                }
                else if (client.sellitem == 1 && item.quantity == 50 || item.Itemid == 23024)
                {
                    byte quantity = item.quantity;
                    total_hp_potion_used = quantity;
                    gold += (quantity * hpItems.SellingPrice);
                    inventory.items[item.slot - 1] = null;
                }
                if (total_hp_potion_used > 0)
                {
                    client.getChar().sendGold();
                    announce("ขายขยะ >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength) + " จำนวน >> " + total_hp_potion_used);
                    reply(new PacketCreator(new byte[] { 0x17, 9, item.slot, total_hp_potion_used }).send());
                    reply(new PacketCreator(0x17, 0xf).send());
                    inventory.nextSlot();
                }
                if (((equipment[5] != null && equipment[5].Itemid == 23024) || equipment[5] == null) && new byte[] { 40, 50, 60, }.Contains(hpItems.unk3))
                {
                    inventory.items[item.slot - 1].equip?.equipOnChar();
                    announce("สลับตรา >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength));
                    inventory.nextSlot();
                }
            }
        }

        public void autohpspWitItem()
        {
            double percentage_hp = ((double)hp / hp_max) * 100;
            double percentage_sp = ((double)sp / sp_max) * 100;
            if (percentage_hp < 80)
            {
                foreach (var item in inventory.items)
                {
                    if (item == null) continue;
                    byte total_hp_potion_used = 0;
                    var hpItems = ItemData.itemList[item.Itemid];
                    if (hpItems.prop1 == 25 && hpItems.prop1_val > 0 && hpItems.equippos == 0)
                    {
                        if (hp < hp_max)
                        {
                            byte quantity = item.quantity;
                            for (int i = 0; i < quantity; i++)
                            {
                                setHp(hpItems.prop1_val);
                                inventory.items[item.slot - 1].quantity--;
                                total_hp_potion_used++;
                                if (item.quantity == 0)
                                    inventory.items[item.slot - 1] = null;
                                if (hp >= hp_max)
                                    break;
                            }
                        }
                    }
                    if (total_hp_potion_used > 0)
                    {
                        announce("กินยา HP >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength) + " จำนวน >> " + total_hp_potion_used);
                        reply(new PacketCreator(new byte[] { 0x17, 9, item.slot, total_hp_potion_used }).send());
                        reply(new PacketCreator(0x17, 0xf).send());
                    }
                }
                refresh(hp, TSConstants._HP);
            }
            if (percentage_sp < 80)
            {
                foreach (var item in inventory.items)
                {
                    if (item == null) continue;
                    byte total_sp_potion_used = 0;
                    var hpItems = ItemData.itemList[item.Itemid];
                    if (hpItems.prop1 == 26 && hpItems.prop1_val > 0 && hpItems.equippos == 0)
                    {
                        if (sp < sp_max)
                        {
                            byte quantity = item.quantity;
                            for (int i = 0; i < quantity; i++)
                            {
                                setSp(hpItems.prop1_val);
                                inventory.items[item.slot - 1].quantity--;
                                total_sp_potion_used++;
                                if (item.quantity == 0)
                                    inventory.items[item.slot - 1] = null;
                                if (sp >= sp_max)
                                    break;
                            }
                        }
                    }
                    else if (hpItems.prop2 == 26 && hpItems.prop2_val > 0 && hpItems.equippos == 0)
                    {
                        if (sp < sp_max)
                        {
                            byte quantity = item.quantity;
                            for (int i = 0; i < quantity; i++)
                            {
                                setSp(hpItems.prop2_val);
                                inventory.items[item.slot - 1].quantity--;
                                total_sp_potion_used++;
                                if (item.quantity == 0)
                                    inventory.items[item.slot - 1] = null;
                                if (sp >= sp_max)
                                    break;
                            }
                        }
                    }
                    if (total_sp_potion_used > 0)
                    {
                        announce("กินยา SP >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength) + " จำนวน >> " + total_sp_potion_used);
                        reply(new PacketCreator(new byte[] { 0x17, 9, item.slot, total_sp_potion_used }).send());
                        reply(new PacketCreator(0x17, 0xf).send());
                    }
                }
                refresh(sp, TSConstants._SP);
            }
            if (pet_battle != -1)
            {
                if (percentage_hp < 80)
                {
                    foreach (var item in inventory.items)
                    {
                        if (item == null) continue;
                        byte total_hp_potion_used = 0;
                        var hpItems = ItemData.itemList[item.Itemid];
                        if (hpItems.prop1 == 25 && hpItems.prop1_val > 0 && hpItems.equippos == 0)
                        {
                            if (pet[pet_battle].hp < pet[pet_battle].hp_max)
                            {
                                byte quantity = item.quantity;
                                for (int i = 0; i < quantity; i++)
                                {
                                    pet[pet_battle].setHp(hpItems.prop1_val);
                                    inventory.items[item.slot - 1].quantity--;
                                    total_hp_potion_used++;
                                    if (item.quantity == 0)
                                        inventory.items[item.slot - 1] = null;
                                    if (pet[pet_battle].hp >= pet[pet_battle].hp_max)
                                        break;
                                }
                            }
                        }
                        if (total_hp_potion_used > 0)
                        {
                            announce("ขุนพลกินยา HP >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength) + " จำนวน >> " + total_hp_potion_used);
                            reply(new PacketCreator(new byte[] { 0x17, 9, item.slot, total_hp_potion_used }).send());
                            reply(new PacketCreator(0x17, 0xf).send());
                        }
                    }
                    refresh(pet[pet_battle].hp, TSConstants._HP);
                }
                if (percentage_sp < 80)
                {
                    foreach (var item in inventory.items)
                    {
                        if (item == null) continue;
                        byte total_sp_potion_used = 0;
                        var hpItems = ItemData.itemList[item.Itemid];
                        if (hpItems.prop1 == 26 && hpItems.prop1_val > 0 && hpItems.equippos == 0)
                        {
                            if (pet[pet_battle].sp < pet[pet_battle].sp_max)
                            {
                                byte quantity = item.quantity;
                                for (int i = 0; i < quantity; i++)
                                {
                                    pet[pet_battle].setSp(hpItems.prop1_val);
                                    inventory.items[item.slot - 1].quantity--;
                                    total_sp_potion_used++;
                                    if (item.quantity == 0)
                                        inventory.items[item.slot - 1] = null;
                                    if (pet[pet_battle].sp >= pet[pet_battle].sp_max)
                                        break;
                                }
                            }
                        }
                        else if (hpItems.prop2 == 26 && hpItems.prop2_val > 0 && hpItems.equippos == 0)
                        {
                            if (pet[pet_battle].sp < pet[pet_battle].sp_max)
                            {
                                byte quantity = item.quantity;
                                for (int i = 0; i < quantity; i++)
                                {
                                    pet[pet_battle].setSp(hpItems.prop2_val);
                                    inventory.items[item.slot - 1].quantity--;
                                    total_sp_potion_used++;
                                    if (item.quantity == 0)
                                        inventory.items[item.slot - 1] = null;
                                    if (pet[pet_battle].sp >= pet[pet_battle].sp_max)
                                        break;
                                }
                            }
                        }
                        if (total_sp_potion_used > 0)
                        {
                            announce("ขุนกินยา SP >> " + Encoding.UTF8.GetString(hpItems.name, 0, hpItems.namelength) + " จำนวน >> " + total_sp_potion_used);
                            reply(new PacketCreator(new byte[] { 0x17, 9, item.slot, total_sp_potion_used }).send());
                            reply(new PacketCreator(0x17, 0xf).send());
                        }
                    }
                    refresh(pet[pet_battle].sp, TSConstants._SP);
                }
            }
            inventory.nextSlot();
        }

        public void getspForsub()
        {
            TSClient subleader_Client = TSServer.getInstance().getPlayerById(party.subleader_id);
            if (subleader_Client != null)
            {
                int amont = subleader_Client.getChar().mag / 8;
                setSp(amont);
                refresh(sp, TSConstants._SP);
                if (this.pet_battle != -1)
                {
                    TSPet pet = this.pet[this.pet_battle];
                    pet.setSp(amont);
                    pet.refresh(pet.sp, TSConstants._SP);
                }
                if (party != null)
                {
                    for (int n = 0; n < party.member.Count; n++)
                    {
                        if (party.member[n].client.accID != client.accID)
                        {
                            party.member[n].setSp(amont);
                            party.member[n].refresh(party.member[n].sp, TSConstants._SP);
                            if (party.member[n].pet_battle != -1)
                            {
                                TSPet pet = party.member[n].pet[party.member[n].pet_battle];
                                pet.setSp(amont);
                                pet.refresh(pet.sp, TSConstants._SP);
                            }
                        }
                    }
                }
            }
        }

        public bool isLeader()
        {
            if (client.accID == this.party.leader_id)
                return true;
            return false;
        }

        public void getspForsub2()
        {
            if (party != null)
            {
                TSClient subleader_Client = TSServer.getInstance()?.getPlayerById(party.subleader_id);
                if (subleader_Client != null)
                {
                    int amont = subleader_Client.getChar().mag / 8;
                    setSp(amont);
                    refresh(sp, TSConstants._SP);
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 5; j++)
                        {
                            if (client?.battle?.position[i][j]?.chr != null)
                            {
                                PacketCreator p = new PacketCreator(0x32, 1);
                                p.add16(15);
                                p.addByte((byte)i); p.addByte((byte)j);
                                p.add16(20003);
                                p.add8((byte)1);
                                p.add8(1);
                                p.addByte((byte)i); p.addByte((byte)j);
                                p.addByte(1);
                                p.addByte(0);
                                p.addByte(1);
                                p.addByte(0x1a);
                                p.add16((ushort)amont);
                                p.addByte(0);
                                byte[] pCommand = p.send();
                                client?.battle?.battleBroadcast(pCommand);
                                client?.battle?.sterm(pCommand);
                            }
                            else if (client?.battle?.position[i][j]?.pet != null)
                            {
                                PacketCreator p = new PacketCreator(0x32, 1);
                                p.add16(15);
                                p.addByte((byte)i); p.addByte((byte)j);
                                p.add16(20003);
                                p.add8((byte)1);
                                p.add8(1);
                                p.addByte((byte)i); p.addByte((byte)j);
                                p.addByte(1);
                                p.addByte(0);
                                p.addByte(1);
                                p.addByte(0x1a);
                                p.add16((ushort)amont);
                                p.addByte(0);
                                byte[] pCommand = p.send();
                                client?.battle?.battleBroadcast(pCommand);
                                client?.battle?.sterm(pCommand);
                            }
                        }
                    if (this.pet_battle != -1)
                    {
                        TSPet pet = this.pet[this.pet_battle];
                        pet.setSp(amont);
                        pet.refresh(pet.sp, TSConstants._SP);
                    }
                    if (client?.getChar().party != null)
                    {
                        for (int n = 0; n < party?.member?.Count; n++)
                        {
                            if (party?.member[n]?.client.accID != client?.accID)
                            {
                                party?.member[n]?.setSp(amont);
                                party?.member[n]?.refresh(party.member[n].sp, TSConstants._SP);
                                if (party?.member[n]?.pet_battle != -1)
                                {
                                    TSPet pet = party?.member[n]?.pet[party.member[n].pet_battle];
                                    pet.setSp(amont);
                                    pet.refresh(pet.sp, TSConstants._SP);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static NpcOnMapInfo GetRandomNpcOnMap()
        {
            Random rnd = new Random();
            var key = NpcOnMapData.listKeysNpcOnMap[rnd.Next(NpcOnMapData.listKeysNpcOnMap.Count)];
            return NpcOnMapData.npcOnMapList[(Key_NpcOnMap)key];
        }

        public void SwitchPk(byte[] data)
        {
            if (PkSwich == false)
            {
                PkSwich = true;
                PacketCreator p0 = new PacketCreator(0x21, 2);
                p0.add8(data[2]);
                p0.addZero(2);
                client.reply(p0.send());
            }
            else
            {
                PkSwich = false;
                PacketCreator p0 = new PacketCreator(0x21, 2);
                p0.add8(data[2]);
                p0.addZero(2);
                client.reply(p0.send());
            }
        }

        public void SwitchJam(byte[] data)
        {
            if (JamSwich == false)
            {
                JamSwich = true;
                PacketCreator p0 = new PacketCreator(0x21, 3);
                p0.add8(data[2]);
                p0.addZero(2);
                client.reply(p0.send());
            }
            else
            {
                JamSwich = false;
                PacketCreator p0 = new PacketCreator(0x21, 3);
                p0.add8(data[2]);
                p0.addZero(2);
                client.reply(p0.send());
            }
        }

        public void loadSwitchPk()
        {
            PacketCreator p0 = new PacketCreator(0x21, 2);
            p0.add8(0);
            p0.addZero(2);
            client.reply(p0.send());
        }

        public void exitStreamBattle()
        {
            if (streamBattleId > 0)
            {
                if (TSServer.listPlayers.ContainsKey(streamBattleId))
                {
                    TSClient c = TSServer.getInstance().getPlayerById(streamBattleId);
                    if (c != null && c.battle != null && c.battle.streamers != null)
                    {
                        c.battle.streamers.Remove(client.accID);
                    }
                    streamBattleId = 0;
                    PacketCreator p = new PacketCreator(0x0b);
                    p.addByte(0);
                    p.add32(client.accID);
                    p.addByte(0); p.addByte(0);
                    reply(p.send());
                }
            }
        }

        public void loadPet()
        {
            var c = new TSMysqlConnection();
            MySqlDataReader data = c.selectQuery("SELECT pet_sid, slot, location FROM pet WHERE charid = " + charId);
            try
            {
                while (data.Read())
                {
                    int s = data.GetInt32("slot");
                    int sid = data.GetInt32("pet_sid");
                    pet[s - 1] = new TSPet(this, sid, (byte)s);
                    pet[s - 1].loadPetDB();
                }
            }
            catch (Exception e)
            {
                WriteLog.ErrorDB("At TSCharacter loadPet >> " + e);
            }
            finally
            {
                data.Close();
                c.connection.Close();
            }
            nextPet();
        }

        public void initChar(byte[] data, byte[] name)
        {
            loadAirtime();
            if (TSServer.config.airtime > 0)
            {
                Add_Airtime(TSServer.config.airtime);
                CreationDate = changYear(DateTime.Now);
                StartDate = CreationDate;
            }
            else
            {
                CreationDate = StartDate;
            }
            string ctime = CreationDate.ToString("yyyy-MM-dd HH:mm:ss");
            string extime = ExpiryDate.ToString("yyyy-MM-dd HH:mm:ss");
            string pass1 = PacketReader.readString(data, 22, data[21]);
            string pass2 = PacketReader.readString(data, 22 + pass1.Length + 1, data[22 + pass1.Length]);
            var c = new TSMysqlConnection();
            string ball = "0";
            string sk = "'{}'";
            armypoint = new ConcurrentDictionary<ushort, ushort>();
            armypoint.TryAdd(1, 0);
            armypoint.TryAdd(2, 0);
            armypoint.TryAdd(3, 0);
            armypoint.TryAdd(4, 0);
            armypoint.TryAdd(5, 0);
            string uesitemCout = "'{}'";
            string sk_rb2 = "'[0,0,0,0,0,0,0,0]'";
            string ball_l = "'[false,false,false,false,false,false,false,false,false,false,false,false]'";
            string allball = "'[0,0,0]'";
            string hot = "'[0,0,0,0,0,0,0,0,0,0]'";
            string Newquip = "'[[0,0,0,0,0,0,0,0,0,0],[2,19737,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0]]'";
            string mapid = "10817";
            string mapx = "442";
            string mapy = "758";
            string smapid = "12003";
            string smapx = "500";
            string smapy = "500";
            string add = "'[]'";
            c.updateQuery("UPDATE " + TSServer.config.tbAccount + " SET password = '" + pass1 + "', password2 = '" + pass2 + "', start_date_time = '" + ctime + "', end_date_time = '" + extime + "' WHERE id = " + client.accID);
            c.connection.Open();
            var cmd = new MySqlCommand();
            cmd.Connection = c.connection;
            cmd.CommandText = "INSERT INTO " + TSServer.config.tbChars + " (accountid, name, mag, atk, def, hpx, spx, agi, sex, style, hair, face, color1, color2, element, map_id, map_x, map_y, s_map_id, s_map_x, s_map_y, equip, inventory, storage, bag, allball, ball_point , skill , skill_rb2 , balllist, hotkey , uesitemcout) "
                          + "VALUES (" + client.accID + ", @name ," + data[15] + "," + data[16] + "," + data[17] + "," + data[18] + "," + data[19] + "," + data[20] + "," + data[2] + "," + data[3] + "," + data[4] + "," + data[5] +
                          "," + PacketReader.read32(data, 6) + "," + PacketReader.read32(data, 10) + "," + data[14] + "," + mapid + "," + mapx + "," + mapy + "," + smapid + "," + smapx + "," + smapy + "," + Newquip + "," + add + "," + add + "," + add + "," + allball + "," + ball + "," + sk + "," + sk_rb2 + "," + ball_l + "," + hot + "," + uesitemCout + ");";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@name", Encoding.UTF8.GetString(name));
            cmd.ExecuteNonQuery();
            c.connection.Close();
            charId = c.getLastId(TSServer.config.tbChars);
        }

        public void saveAirtime()
        {
            var c = new TSMysqlConnection();
            string ctime = CreationDate.ToString("yyyy-MM-dd HH:mm:ss");
            string extime = ExpiryDate.ToString("yyyy-MM-dd HH:mm:ss");
            c.connection.Open();
            var cmd = new MySqlCommand();
            cmd.Connection = c.connection;
            cmd.CommandText =
                "UPDATE " + TSServer.config.tbAccount + " SET start_date_time = @start_date_time , end_date_time = @end_date_time WHERE `id` = " + client.accID + ";";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@start_date_time", ctime);
            cmd.Parameters.AddWithValue("@end_date_time", extime);
            cmd.ExecuteNonQuery();
            c.connection.Close();
        }

        public void loginChar()
        {
            loadCharDB();
            loadAirtime();
            if (TSServer.config.debugmode == true)
            {
                checktimeout();
            }
            var sqlc = new TSMysqlConnection();
            sqlc.updateQuery("UPDATE " + TSServer.config.tbAccount + " SET lastlogin = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE id = " + client.accID);
            client.online = true;
            reply(new PacketCreator(new byte[] { 0x14, 0x08 }).send());
            reply(new PacketCreator(new byte[] { 0x14, 0x21, 0x00 }).send());
            sendLook(false);
            sendInfo();
            sendPetInfo();
            reply(new PacketCreator(new byte[] { 0x21, 2, 0, 0 }).send());
            inventory.sendItems(0x17, 5);
            bag.sendItems(0x17, 0x2f);
            storage.sendItems(0x1e, 1);
            sendEquip();
            client.UImportant();
            client.AllowMove();
            sendGold();
            announce(TSServer.config.welcomeMessage);
            sendHotkey();
            loadMallpoint();
            sendVoucher();
            loadSwitchPk();
            refreshFull(TSConstants._UNOWK, 1, 1);
            if (GuildData.guildMemberList.ContainsKey(client.accID))
            {
                guild_id = GuildData.guildMemberList[client.accID].guildId;
                GuildSystem.OnPlayerLogin(this);
            }
            GuildSystem.SendAllGuildIcons(this);
            client.loadQuestDB();
            client.refreshQuestTask();
            client.refreshDontTask();
            TSServer.getInstance().addPlayer(client);
            if (client.map != null)
            {
                foreach (TSClient c in client.map.listPlayers.Values)
                {
                    sendOtherPlayerDoing(c);
                }
            }
            EventActivityTime();
            announce_in_server();
            addReNewchr();
            UpdateDaysLeft();
            if (client.QuestTime.Count > 0)
            {
                TimeQuest.getInstance().QustTime(client);
            }
            checkEndAirtime();
            replyMagtoChr();
            ActivityHandler.removeQ(client);
        }

        public async void addReNewchr()
        {
            if (client.map.mapid == 10817)
            {
                await Task.Delay(500);
                addPet(11144, 0, 1);
                PacketCreator p = new PacketCreator(0x02);
                p.add8(0x00);
                p.add32(0);
                p.addBytes(Encoding.UTF8.GetBytes("ยินดีต้อนรับคุณ " + name + " เข้าสู่ Test Server TS Online ขอให้สนุกกับการผจญภัยนะครับ"));
                byte[] textByte = p.send();
                foreach (TSClient c in TSServer.listPlayers.Values)
                {
                    c.reply(textByte);
                }
                client.savetoDB();
            }
        }

        public void replyMagtoChr()
        {
            if (boolequip)
                announce("อุปกรณ์สวมใส่ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ ของที่คุณใส่อยู่ ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolinventory)
                announce("ไอเท็มในกระเป๋าของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ ไอเท็มที่คุณได้ ในขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolstorage)
                announce("ไอเท็มในคลังของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ คลัง ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolbag)
                announce("ไอเท็มในเป้ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ เป้ ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolskill)
                announce("สกิลของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ สกิล ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolskill_rb2)
                announce("สกิลจุติ 2 ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ สกิลจุติ2 ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolball_point)
                announce("พ้อยบอลของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ พ้อยบอล ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolballlist)
                announce("บอลที่คุณเปิดมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ บอลที่คุณเปิด ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolhotkey)
                announce("hotKey ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ HotKey ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolarmypoint)
                announce("ค่าค่ายทหารคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ ค่าค่ายทหาร ขณะออนไลน์ โปรดแจ้ง GM ");
            if (booluesitemcout)
                announce("การใช้ไอเท็มของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับของ การใช้ไอเท็ม ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolallball)
                announce("มุกที่เปิด ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ มุกที่เปิด ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolPetuesitemcout)
                announce("การใช้ไอเท็มของขุนพล ของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ การใช้ไอเท็มของขุนพล ขณะออนไลน์ โปรดแจ้ง GM ");
            if (boolPetequip)
                announce("อุปกรณ์สวมใส่ขุนพลของคุณมีปัณหาเราจะไม่มีการ Save ค่าใดๆที่เกี่ยวกับ อุปกรณ์สวมใส่ขุนพล ขณะออนไลน์ โปรดแจ้ง GM ");
        }

        public void announce_in_server()
        {
            announce("ดูคำสั่งทั้งหมดพิม /help");
            announce("EXP x " + TSServer.config.perExp + " DROP x " + TSServer.config.perDrop);
            announce("EXP ไอเท็มวิญญาณ x " + TSServer.config.perSoulexp);
            announce("เก็บเลเวลห่างมอน +-" + TSServer.config.perLvGetexp);
            if (gm == 2)
            {
                announce("ID VIP");
                announce("ได้รับ Bonus EXP 2 เท่า");
            }
            if (ai == 1)
            {
                announce("เปิดใช้ AUTOHP_SP");
            }
            else
            {
                announce("ปิดใช้งาน AUTOHP_SP");
                ai = 0;
            }
            if (onoffbt == 1)
            {
                client.battle2 = true;
                announce("เปิดโหมดการต่อสู้ NPC");
            }
            else
            {
                client.battle2 = false;
                announce("ปิดโหมดการต่อสู้ NPC");
                onoffbt = 0;
                var p = new PacketCreator(0x17, 0x26);
                client.reply(p.send());
            }
            if (client.aicombo == true)
            {
                announce("เปิดใช้ combo 100 %");
            }
            else
            {
                announce("ปิดใช้ combo 100 %");
            }
        }

        public void autosaveDB(object sedr, EventArgs e)
        {
            timerautosave.Stop();
            client.savetoDB();
            timerautosave.Start();
        }

        public void autoSpSub(object sedr, EventArgs e)
        {
            if (party != null && party.subleader_id > 0)
            {
                TSClient c = TSServer.getInstance().getPlayerById(party.subleader_id);
                getspForsub();
            }
            else
            {
                autospSub = false;
            }
        }

        public void EventActivityTime()
        {
            if (ActivityHandler.isEventRunning)
            {
                ActivityHandler.isRemoveQ = true;
                announce("กิจกรรมหอนกยูงเริ่มขึ้นแล้ว ขอเชิญเหล่าผู้กล้ามาร่วมกิจกรรมได้ตั้งแต่ 1 12.00 น. ถึง 23.59 น. ของทุก วันพุธ และ อาทิตย์ ขอให้สนุกกับกิจกรรมหอนกยูงนะครับ");
            }
            else if (ActivityHandler.isEventRunning40)
            {
                ActivityHandler.isRemoveQ = true;
                announce("กิจกรรมประลอง NPC 50 ด่านเริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันจันทร์ และ วันพฤหัสบดี");
            }
            else if (ActivityHandler.isEventRunningPK1)
            {
                ActivityHandler.isRemoveQ = true;
                announce("กิจกรรมประลองเดี่ยว เริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันอังคาร");
            }
            else if (ActivityHandler.isEventRunningPK5)
            {
                ActivityHandler.isRemoveQ = true;
                announce("กิจกรรมประลองชุลมุนแบบกลุ่ม เริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันศุกร์");
            }
            else if (ActivityHandler.isEventRunningPK10)
            {
                ActivityHandler.isRemoveQ = true;
                announce("กิจกรรมประลองเขาวงกรต เริ่มขึ้นแล้วตั้งแต่เวลา 12.00 น. ถึง 23.59 น. ของวันศุกร์");
            }
            else
            {
                ActivityHandler.isRemoveQ = false;
            }
            if (mapID == 10991)
            {
                if (ActivityHandler.isEventRunning40)
                {
                    client.show_hideNPC(0, 5);
                    ActivityHandler.isRemoveQ = true;
                    announce("กิจกรรมประลอง NPC 50 ด่านเริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันจันทร์ และ วันพฤหัสบดี");
                    return;
                }
                else if (ActivityHandler.isEventRunningPK1)
                {
                    ActivityHandler.isRemoveQ = true;
                    announce("กิจกรรมประลองเดี่ยว เริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันอังคาร");
                    client.show_hideNPC(5, 4);
                    return;
                }
                else if (ActivityHandler.isEventRunningPK5)
                {
                    ActivityHandler.isRemoveQ = true;
                    announce("กิจกรรมประลองชุลมุนแบบกลุ่ม เริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันศุกร์");
                    client.show_hideNPC(5, 3);
                    return;
                }
                else if (ActivityHandler.isEventRunningPK10)
                {
                    ActivityHandler.isRemoveQ = true;
                    announce("กิจกรรมประลองเขาวงกรต เริ่มขึ้นแล้วตั้งแต่เวลา 12.00 น. ถึง 23.59 น. ของวันศุกร์");
                    client.show_hideNPC(5, 7);
                    return;
                }
                else
                {
                    client.show_hideNPC(5, 0);
                    return;
                }
            }
            if (ActivityHandler.isEventRunningTSwar)
            {
                announce("สงครามศึกชิงเมือง เริ่มขึ้นแล้วตั้งแต่เวลา  12.00 น. ถึง 23.59 น. ของวันเสาร์");
            }
        }

        public void sendTeam()
        {
            if (client.map != null)
            {
                foreach (TSClient c in client.map.listPlayers.Values)
                {
                    var Chr = c.getChar();
                    Chr.sendUpdateTeam(true);
                }
            }
        }

        public void addPet(ushort npcid, int bonus, byte quest)
        {
            for (int i = 0; i < next_pet; i++)
                if (pet[i].NPCid == npcid) return;
            if (next_pet < 4 && NpcData.npcList.ContainsKey(npcid))
            {
                pet[next_pet] = new TSPet(this, (byte)(next_pet + 1), quest);
                pet[next_pet].initPet(NpcData.npcList[npcid]);
                pet[next_pet].sendNewPet();
                for (int i = 0; i < bonus; i++)
                    pet[next_pet].getSttPoint();
                nextPet();
            }
        }

        public void changePetName(byte slot, byte[] newName)
        {
            if (pet[slot - 1] == null) return;
            string newNameString = Encoding.UTF8.GetString(newName, 0, newName.Length);
            var c = new TSMysqlConnection();
            c.connection.Open();
            var cmd = new MySqlCommand();
            cmd.Connection = c.connection;
            cmd.CommandText = "UPDATE " + TSServer.config.tbPet + " SET `name` = @name WHERE pet_sid=" + pet[slot - 1].pet_sid;
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@name", newNameString);
            cmd.ExecuteNonQuery();
            c.connection.Close();
            pet[slot - 1].name = newNameString;
            pet[slot - 1].nameBytes = Encoding.UTF8.GetBytes(newNameString);
            PacketCreator p = new PacketCreator(0xf, 9);
            p.add32(client.accID);
            p.add8(slot);
            p.addBytes(pet[slot - 1].nameBytes);
            reply(p.send());
        }

        public void removePet(byte slot)
        {
            int index = Array.FindIndex(pet, item => item?.slot == slot);
            if (index >= 0)
            {
                if (pet[index] != null)
                {
                    int pet_sidold = pet[index].pet_sid;
                    pet[index] = null;
                    var c = new TSMysqlConnection();
                    try
                    {
                        c.updateQuery("DELETE FROM pet WHERE pet_sid=" + pet_sidold);
                        if (pet_battle == index)
                        {
                            pet_battle = -1;
                            c.updateQuery("UPDATE " + TSServer.config.tbChars + " SET `pet_battle` = " + pet_battle + " WHERE id=" + this.charId);
                        }
                        nextPet();
                        PacketCreator p = new PacketCreator(0xf, 2);
                        p.add32(client.accID);
                        p.add8(slot);
                        replyToMap(p.send(), true);
                        sendPetInfo();
                    }
                    catch (Exception e)
                    {
                        WriteLog.ErrorDB("removePet " + e);
                    }
                    finally
                    {
                        c.connection.Close();
                    }
                }
            }
        }

        public void ChangSlotPet()
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (pet[i].slot == 4)
                {
                    pet[i].slot = 3;
                }
                if (pet[i].slot == 3)
                {
                    pet[i].slot = 2;
                }
                if (pet[i].slot == 2)
                {
                    pet[i].slot = 1;
                }
            }
            for (int i = 0; i < count; i++)
            {
                pet[i].slot = 0;
                pet[i].slot = (byte)(i + 1);
            }
        }

        public void removePetQ(ushort id)
        {
            try
            {
                byte slot = pet.Where(x => x != null)
               .FirstOrDefault(x => x.NPCid == id && x.quest == 0)?.slot ?? 0;
                removePet(slot);
            }
            catch (Exception e)
            {
                WriteLog.ErrorDB("removePetQById " + e);
            }
        }

        public void removePetN(ushort id)
        {
            try
            {
                byte slot = pet.Where(x => x != null)
               .FirstOrDefault(x => x.NPCid == id && x.quest == 1)?.slot ?? 0;
                removePet(slot);
            }
            catch (Exception e)
            {
                WriteLog.ErrorDB("removePetByID " + e);
            }
        }

        public void nextPet()
        {
            for (next_pet = 0; next_pet < 4; next_pet++)
            {
                if (pet[next_pet] == null)
                    break;
            }
        }

        public void sendEquipBonus()
        {
            Dictionary<ushort, int> bonus_list = new Dictionary<ushort, int>();
            int _hp = hp;
            int _sp = sp;
            mag2 = 0; atk2 = 0; def2 = 0; hp2 = 0; sp2 = 0; agi2 = 0;
            Dictionary<ushort, int> comboSets = new Dictionary<ushort, int>();
            foreach (KeyValuePair<ushort, int> cmb in comboSets.ToList())
            {
                SuitInfo suitInfo = SuitData.suitList[cmb.Key];
                if (comboSets[cmb.Key] >= suitInfo.count1)
                {
                    if (bonus_list.ContainsKey(suitInfo.prop1))
                        bonus_list[suitInfo.prop1] += suitInfo.prop1_val;
                    else
                        bonus_list.Add(suitInfo.prop1, suitInfo.prop1_val);
                }
                if (comboSets[cmb.Key] >= suitInfo.count2)
                {
                    if (bonus_list.ContainsKey(suitInfo.prop2))
                        bonus_list[suitInfo.prop2] += suitInfo.prop2_val;
                    else
                        bonus_list.Add(suitInfo.prop2, suitInfo.prop2_val);
                }
                if (comboSets[cmb.Key] >= suitInfo.count3)
                {
                    if (bonus_list.ContainsKey(suitInfo.prop3))
                        bonus_list[suitInfo.prop3] += suitInfo.prop3_val;
                    else
                        bonus_list.Add(suitInfo.prop3, suitInfo.prop3_val);
                }
            }
            bool all_equip_same_elm = equipment.Where(eq => eq != null && (
                eq.elem_type == this.element
                || eq.other_type == this.element
                || eq.elem_type == 5
                || eq.other_type == 5
            )).Count() >= 5;
            if (all_equip_same_elm)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (equipment[i] != null)
                    {
                        if (bonus_list.ContainsKey(equipment[i].Itemid))
                        {
                        }
                    }
                }
            }
        }

        public void addEquipSoul(ushort prop, int soullv, int odl_soullv, int type)
        {
            if (prop == 214)
            {
                soullv = 0;
            }
            int val = type == 0 ? soullv : -soullv;
            int val2 = odl_soullv <= 0 ? 0 : odl_soullv;
            switch (prop)
            {
                case 207:
                    hp2 = (hp2 - val2) + val;
                    break;
                case 208:
                    sp2 = (sp2 - val2) + val;
                    break;
                case 210:
                    atk2 = (atk2 - val2) + val;
                    break;
                case 211:
                    def2 = (def2 - val2) + val;
                    break;
                case 212:
                    mag2 = (mag2 - val2) + val;
                    break;
                case 214:
                    agi2 = (agi2 - val2) + val;
                    break;
                default:
                    break;
            }
        }

        public void addEquipBonus(ushort prop, int prop_val, int type)
        {
            int val = type == 0 ? prop_val : -prop_val;
            switch (prop)
            {
                case 207:
                    hp2 += val;
                    break;
                case 208:
                    sp2 += val;
                    break;
                case 210:
                    atk2 += val;
                    break;
                case 211:
                    def2 += val;
                    break;
                case 212:
                    mag2 += val;
                    break;
                case 214:
                    agi2 += val;
                    break;
                default:
                    break;
            }
        }

        public void sendLook(bool forReborn)
        {
            var p = new PacketCreator(3);
            p.add32(client.accID);
            p.addByte(sex);
            p.addByte((byte)ghost);
            p.addByte((byte)god);
            p.add16(mapID);
            p.add16(mapX);
            p.add16(mapY);
            p.addByte(style);
            p.addByte(hair);
            p.addByte(face);
            p.add32(color1);
            p.add32(color2);
            p.addByte(nb_equips);
            for (int i = 0; i < 6; i++)
                if (equipment[i] != null) p.add16(equipment[i].Itemid);
            p.add32((uint)guild_id);
            p.addByte(5);
            p.addByte(rb);
            p.addByte(job);
            if (!forReborn)
                p.addBytes(Encoding.UTF8.GetBytes(name));
            reply(p.send());
        }

        public byte[] sendLookForOther()
        {
            var p = new PacketCreator(0x03);
            p.add32(client.accID);
            p.addByte(sex);
            p.addByte(element);
            p.addByte(level);
            p.addByte((byte)ghost);
            p.addByte((byte)god);
            p.add16(mapID);
            p.add16(mapX);
            p.add16(mapY);
            p.addByte(style);
            p.addByte(hair);
            p.addByte(face);
            p.add32(color1);
            p.add32(color2);
            p.addByte(nb_equips);
            for (int i = 0; i < 6; i++)
                if (equipment[i] != null) p.add16(equipment[i].Itemid);
            p.add32((uint)guild_id);
            p.add16(0);
            p.addByte(rb);
            p.addByte(job);
            p.addBytes(Encoding.UTF8.GetBytes(name));
            byte[] basePacket = p.send();
            if (guild_id > 0)
            {
                var pLink = GuildData.BuildGuildLinkPacket(this);
                byte[] linkBytes = pLink != null ? pLink.send() : new byte[0];
                byte[] combined = new byte[basePacket.Length + linkBytes.Length];
                Buffer.BlockCopy(basePacket, 0, combined, 0, basePacket.Length);
                Buffer.BlockCopy(linkBytes, 0, combined, basePacket.Length, linkBytes.Length);
                return combined;
            }
            return basePacket;
        }

        public byte[] sendLookForOther2()
        {
            var p = new PacketCreator(0x03);
            p.add32(client.accID);
            p.addByte(sex);
            p.addByte(element);
            p.addByte(level);
            p.addByte((byte)ghost);
            p.addByte((byte)god);
            p.add16(mapID);
            p.add16(mapX);
            p.add16(mapY);
            p.addByte(style);
            p.addByte(hair);
            p.addByte(face);
            p.add32(color1);
            p.add32(color2);
            p.addByte(nb_equips);
            for (int i = 0; i < 6; i++)
                if (equipment[i] != null) p.add16(equipment[i].Itemid);
            p.add32((uint)guild_id);
            p.add16(0);
            p.addByte(rb);
            p.addByte(job);
            p.addBytes(Encoding.UTF8.GetBytes(name));
            byte[] basePacket2 = p.send();
            if (guild_id > 0)
            {
                var pLink2 = GuildData.BuildGuildLinkPacket(this);
                byte[] linkB = pLink2 != null ? pLink2.send() : new byte[0];
                byte[] combined2 = new byte[basePacket2.Length + linkB.Length];
                Buffer.BlockCopy(basePacket2, 0, combined2, 0, basePacket2.Length);
                Buffer.BlockCopy(linkB, 0, combined2, basePacket2.Length, linkB.Length);
                return combined2;
            }
            return basePacket2;
        }

        public byte[] setExpress(byte expressType, byte expressCode)
        {
            var p = new PacketCreator(0x20);
            p.add8(expressType);
            p.add32(client.accID);
            p.add8(expressCode);
            return p.send();
        }

        public void sendInfo()
        {
            var p = new PacketCreator(5, 3);
            p.addByte(element);
            p.add16((UInt16)hp);
            p.add16((UInt16)sp);
            p.add16((UInt16)mag);
            p.add16((UInt16)atk);
            p.add16((UInt16)def);
            p.add16((UInt16)agi);
            p.add16((UInt16)hpx);
            p.add16((UInt16)spx);
            p.addByte(level);
            p.add32(totalxp);
            p.add16((UInt16)skill_point);
            p.add16((UInt16)stt_point);
            p.add32(honor);
            p.add16((UInt16)hp_max);
            p.add16((UInt16)sp_max);
            p.add32((UInt32)atk2);
            p.add32((UInt32)def2);
            p.add32((UInt32)mag2);
            p.add32((UInt32)agi2);
            p.add32((UInt32)hp2);
            p.add32((UInt32)sp2);
            p.add16(500);
            p.add16(500);
            p.add16(500);
            p.add16(500);
            p.add16(500);
            p.addZero(0x2B);
            foreach (ushort s in skill.Keys)
            {
                p.add16(s);
                p.addByte(skill[s]);
            }
            reply(p.send());
            refresh(FullHpMax, TSConstants._FULLHPMAX);
            refresh(FullSpMax, TSConstants._FULLSPMAX);
            if (rb == 2)
                sendBallList();
        }

        public void sendBallList()
        {
            PacketCreator p = new PacketCreator(0x17, 0x4d);
            p.add8(ball_point);
            for (int i = 0; i < 12; i++)
                if (ballList[i])
                    p.add8((byte)(i + 1));
            reply(p.send());
            PacketCreator p1 = new PacketCreator(0x17, 0x4e);
            for (int i = 0; i < 8; i++)
                if (skill_rb2[i] != 0)
                {
                    p1.add8((byte)(i + 1));
                    p1.add16(skill_rb2[i]);
                }
            reply(p1.send());
        }

        public void sendUpdateTeam(bool self)
        {
            try
            {
                if (isTeamLeader())
                {
                    var p = new PacketCreator(0x0D);
                    p.add8(6);
                    p.add32((uint)client.accID);
                    p.add8((byte)(party.member.Count - 1));
                    foreach (TSCharacter c in party.member)
                    {
                        if (c.client.accID != party.leader_id)
                            p.add32((uint)c.client.accID);
                        c.refreshTeam();
                    }
                    replyToMap(p.send(), self);
                    refreshTeam();
                }
                try
                {
                    if (pet != null)
                    {
                        for (int i = 0; i < pet.Length; i++)
                        {
                            if (pet[i] != null)
                            {
                                var p1 = new PacketCreator(0x0f);
                                p1.add8(0x07);
                                p1.add32((uint)client.accID);
                                p1.addByte((byte)(i + 1));
                                p1.add16(pet[i].NPCid);
                                p1.addZero(7);
                                p1.add8(0x01);
                                p1.addByte((byte)pet[i].name.Length);
                                p1.addBytes(pet[i].nameBytes);
                                replyToMap(p1.send(), false);
                            }
                        }
                    }
                }
                catch (Exception se)
                {
                    Console.WriteLine(se);
                }
                if (horseID > 0)
                    rideHorse(true, self, horseID);
                else
                    rideHorse(false, self);
            }
            catch (Exception se)
            {
                Console.WriteLine(se);
            }
        }

        public ushort findpetid(ushort pet_id)
        {
            if (pet_id == 0)
                return 0;
            byte? ii = pet?.FirstOrDefault(x => x?.NPCid == pet_id)?.slot;
            TSEquipment pet_eq = pet?[(byte)(ii - 1)].equipment?[5];
            ushort itemid = pet_eq == null ? (ushort)0 : pet_eq.Itemid;
            return itemid;
        }

        public void addSaddleEquipBonus(ushort unk3, int unk5, int unk9)
        {
            horseSadd_Agi2 = (ushort)unk9;
            if (unk5 == 140)
            {
                switch (unk3)
                {
                    case 81:
                        agi2 += unk9;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                agi2 += -horseSadd_Agi2;
            }
        }

        public void sendPetInfo()
        {
            var p1 = new PacketCreator(0x0f, 8);
            for (int i = 0; i < 4; i++)
                if (pet[i] != null)
                    p1.addBytes(pet[i].sendInfo());
            reply(p1.send());
            PacketCreator pPetHorse = new PacketCreator(0x0f, 0x0a);
            for (int i = 11; i <= 14; i++)
            {
                TSPet petInHourse = pet[i - 1];
                if (petInHourse != null)
                {
                    pPetHorse.addBytes(petInHourse.sendRestInfo("HORSE"));
                }
            }
            reply(pPetHorse.send());
            PacketCreator pPetHotel = new PacketCreator(0x1f, 0x06);
            for (int i = 5; i <= 10; i++)
            {
                TSPet petInHotel = pet[i - 1];
                if (petInHotel != null)
                {
                    pPetHotel.addBytes(petInHotel.sendRestInfo("HOTEL"));
                }
            }
            reply(pPetHotel.send());
            if (pet_battle != -1)
            {
                if (pet[pet_battle] != null)
                {
                    var p2 = new PacketCreator(0x13);
                    p2.addByte(1);
                    p2.add16(pet[pet_battle].NPCid);
                    p2.add16(0);
                    reply(p2.send());
                }
                else
                {
                    pet_battle = -1;
                }
            }
            if (pet != null)
                for (int i = 0; i < pet.Length; i++)
                    if (pet[i] != null)
                        pet[i].refreshPet();
        }

        public void sendEquip()
        {
            var p = new PacketCreator(0x17, 0x0b);
            for (int i = 0; i < 6; i++)
                if (equipment[i] != null)
                {
                    p.add16(equipment[i].Itemid);
                    p.addByte(equipment[i].quantity);
                    p.addByte((byte)(equipment[i]?.other_type ?? 0));
                    p.addByte((byte)(100 + (byte)(equipment[i]?.other_val ?? 0)));
                    p.addByte((byte)(equipment[i]?.anti ?? 0));
                    p.add32((uint)(equipment[i]?.exp ?? 0));
                }
            reply(p.send());
        }

        public void sendGold()
        {
            var p = new PacketCreator(0x1a, 4);
            p.add32(gold);
            p.add32(gold_bank);
            reply(p.send());
        }

        public void sendHotkey()
        {
            var p = new PacketCreator(0x28, 1);
            foreach (ushort skid in hotkey)
            {
                if (skill.ContainsKey(skid))
                {
                    for (byte i = 1; i <= 10; i++)
                        if (hotkey[i - 1] != 0)
                        {
                            p.add8(2);
                            p.add16(hotkey[i - 1]);
                            p.add8(i);
                        }
                    reply(p.send());
                }
            }
        }

        public void sendpoint()
        {
            var p = new PacketCreator(0x23, 4);
            p.add32((uint)point);
            p.addZero(12);
            reply(p.send());
        }

        public void sendVoucher()
        {
            var p = new PacketCreator(0x23, 0x04);
            uint val1 = (UInt32)point;
            uint val2 = 0;
            p.add32(val1);
            p.add32(val2);
            DateTime airtime_expire = ExpiryDate;
            byte[] next_month_bytes = BitConverter.GetBytes(airtime_expire.ToOADate());
            p.addBytes(next_month_bytes);
            reply(p.send());
        }

        public void sendVoucher3()
        {
            refresh(1000, TSConstants._HP_MAX);
        }

        public void FullHpItem(ushort q)
        {
            FullHpMax += (ushort)(q * 50);
            hp_max = getHpMax();
            refresh(FullHpMax, TSConstants._FULLHPMAX);
        }

        public void FullSpItem(ushort q)
        {
            FullSpMax += (ushort)(q * 10);
            sp_max = getSpMax();
            refresh(FullSpMax, TSConstants._FULLSPMAX);
        }

        public void refreshChr()
        {
            refresh(hpx, TSConstants._HPX);
            refresh(spx, TSConstants._SPX);
            refresh(atk, TSConstants._ATK);
            refresh(def, TSConstants._DEF);
            refresh(mag, TSConstants._MAG);
            refresh(agi, TSConstants._AGI);
            refresh(hp, TSConstants._HP);
            refresh(sp, TSConstants._SP);
            refreshBonus();
        }

        public void showOutfit()
        {
            if (!NpcData.npcList.ContainsKey(outfitId)) return;
            PacketCreator p = new PacketCreator(5, 5);
            p.add32(client.accID);
            p.add16(outfitId);
            replyToMap(p.send(), true);
        }

        public void refreshBonus()
        {
            refresh(mag2, TSConstants._MAG2);
            refresh(atk2, TSConstants._ATK2);
            refresh(def2, TSConstants._DEF2);
            refresh(hp2, TSConstants._HP2);
            refresh(sp2, TSConstants._SP2);
            if (horseID > 0)
                refresh(agi2 - horseSadd_Agi2, TSConstants._AGI2);
            else
                refresh(agi2, TSConstants._AGI2);
        }

        public void refresh(int prop, byte prop_code, bool team = false)
        {
            var p = new PacketCreator(0x08);
            if (party != null && team)
            {
                p.addByte(0x03);
                p.add32((uint)client.accID);
            }
            else
                p.addByte(0x01);
            p.addByte(prop_code);
            if (prop >= 0)
            {
                p.addByte(0x01);
                p.add32((UInt32)prop);
            }
            else
            {
                p.addByte(0x02);
                p.add32((UInt32)(-prop));
            }
            p.add32(0);
            if (party != null && team)
                replyToTeam(p.send());
            else
                reply(p.send());
        }

        public void refresh2(uint prop, byte prop_code, bool team = false)
        {
            var p = new PacketCreator(0x08);
            if (party != null && team)
            {
                p.addByte(0x03);
                p.add32((uint)client.accID);
            }
            else
                p.addByte(0x01);
            p.addByte(prop_code);
            if (prop >= 0)
            {
                p.addByte(0x01);
                p.add32((UInt32)prop);
            }
            else
            {
                p.addByte(0x02);
                p.add32((UInt32)(-prop));
            }
            p.add32(0);
            if (party != null && team)
                replyToTeam(p.send());
            else
                reply(p.send());
        }

        public void refreshNotme(int prop, byte prop_code, bool team = false)
        {
            var p = new PacketCreator(0x08);
            if (party != null && team)
            {
                p.addByte(0x03);
                p.add32((uint)client.accID);
            }
            else
                p.addByte(0x01);
            p.addByte(prop_code);
            if (prop >= 0)
            {
                p.addByte(0x01);
                p.add32((UInt32)prop);
            }
            else
            {
                p.addByte(0x02);
                p.add32((UInt32)(-prop));
            }
            p.add32(0);
            if (party != null && team)
                replyToTeamNotme(p.send());
            else
                reply(p.send());
        }

        public void refreshTeam()
        {
            refresh(hpx, TSConstants._HPX, true);
            refresh(spx, TSConstants._SPX, true);
            refresh(atk, TSConstants._ATK, true);
            refresh(def, TSConstants._DEF, true);
            refresh(mag, TSConstants._MAG, true);
            refresh(agi, TSConstants._AGI, true);
            refresh(hp, TSConstants._HP, true);
            refresh(sp, TSConstants._SP, true);
            refresh(mag2, TSConstants._MAG2, true);
            refresh(atk2, TSConstants._ATK2, true);
            refresh(def2, TSConstants._DEF2, true);
            refresh(hp2, TSConstants._HP2, true);
            refresh(sp2, TSConstants._SP2, true);
            refresh(FullHpMax, TSConstants._FULLHPMAX);
            refresh(FullSpMax, TSConstants._FULLSPMAX);
        }

        public void refreshTeamNotme()
        {
            refreshNotme(hpx, TSConstants._HPX, true);
            refreshNotme(spx, TSConstants._SPX, true);
            refreshNotme(atk, TSConstants._ATK, true);
            refreshNotme(def, TSConstants._DEF, true);
            refreshNotme(mag, TSConstants._MAG, true);
            refreshNotme(agi, TSConstants._AGI, true);
            refreshNotme(hp, TSConstants._HP, true);
            refreshNotme(sp_max, TSConstants._SP, true);
            refreshNotme(mag2, TSConstants._MAG2, true);
            refreshNotme(atk2, TSConstants._ATK2, true);
            refreshNotme(def2, TSConstants._DEF2, true);
            refreshNotme(hp2, TSConstants._HP2, true);
            refreshNotme(sp2, TSConstants._SP2, true);
            if (horseID > 0)
                refreshNotme(agi2 - horseSadd_Agi2, TSConstants._AGI2, true);
            else
                refreshNotme(agi2, TSConstants._AGI2, true);
            refreshNotme(FullHpMax, TSConstants._FULLHPMAX);
            refreshNotme(FullSpMax, TSConstants._FULLSPMAX);
        }

        public void refreshTeamNotmeBt()
        {
            refreshNotme(hp, TSConstants._HP, true);
            refreshNotme(sp, TSConstants._SP, true);
            refreshNotme(hpx, TSConstants._HPX, true);
            refreshNotme(spx, TSConstants._SPX, true);
            refreshNotme(FullHpMax, TSConstants._FULLHPMAX);
            refreshNotme(FullSpMax, TSConstants._FULLSPMAX);
        }

        public void refreshFull(byte prop_code, int prop1, int prop2)
        {
            var p = new PacketCreator(8, 1);
            p.addByte(prop_code);
            if (prop1 >= 0)
            {
                p.addByte(0x01);
                p.add32((UInt32)prop1);
            }
            else
            {
                p.addByte(0x02);
                p.add32((UInt32)(-prop1));
            }
            p.add32((UInt32)prop2);
            reply(p.send());
        }

        public void announce(string msg)
        {
            var p = new PacketCreator(2, 0x0b);
            p.add32(0);
            p.addString(msg);
            reply(p.send());
        }

        public void sendGMMessage(string msg)
        {
            if (this != null)
            {
                PacketCreator p = new PacketCreator(0x02);
                p.add8(0x04);
                p.add32(0);
                p.addBytes(Encoding.UTF8.GetBytes(msg));
                reply(p.send());
            }
        }

        public void saveCharDB(MySqlConnection conn)
        {
            var cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText =
                "UPDATE " + TSServer.config.tbChars + " SET level = @level , exp = @curr_exp, exp_tot = @exp_tot , hp = @hp , fullhpmax = @fullhpmax , sp = @sp , fullspmax = @fullspmax , mag = @mag , atk = @atk," +
                "def = @def , hpx = @hpx , spx = @spx , agi = @agi , sk_point = @sk_point , stt_point = @stt_point," +
                "ghost = @ghost , god = @god , map_id = @map_id , map_x = @map_x , map_y = @map_y , s_map_id = @s_map_id , s_map_x = @s_map_x , s_map_y = @s_map_y , gold = @gold ,hair = @hair ,color1 = @color1 ,color2 = @color2, " +
                "gold_bank = @gold_bank , element = @element , honor = @honor , pet_battle = @pet_battle, equip = @equip, inventory = @inventory, bag = @bag, storage = @storage, " +
                "skill = @skill, skill_rb2 = @skill_rb2, ball_point = @ball_point, balllist = @balllist, hotkey = @hotkey, uesitemcout = @uesitemcout, reborn = @rb, job = @job, ai = @ai, onoffbt = @onoffbt, allball = @allball, center = @center WHERE accountid = @id";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@level", level);
            cmd.Parameters.AddWithValue("@curr_exp", currentxp);
            cmd.Parameters.AddWithValue("@exp_tot", totalxp);
            cmd.Parameters.AddWithValue("@hp", hp);
            cmd.Parameters.AddWithValue("@sp", sp);
            cmd.Parameters.AddWithValue("@fullhpmax", FullHpMax);
            cmd.Parameters.AddWithValue("@fullspmax", FullSpMax);
            cmd.Parameters.AddWithValue("@mag", mag);
            cmd.Parameters.AddWithValue("@atk", atk);
            cmd.Parameters.AddWithValue("@def", def);
            cmd.Parameters.AddWithValue("@hpx", hpx);
            cmd.Parameters.AddWithValue("@spx", spx);
            cmd.Parameters.AddWithValue("@agi", agi);
            cmd.Parameters.AddWithValue("@sk_point", skill_point);
            cmd.Parameters.AddWithValue("@stt_point", stt_point);
            cmd.Parameters.AddWithValue("@ghost", ghost);
            cmd.Parameters.AddWithValue("@god", god);
            cmd.Parameters.AddWithValue("@hair", hair);
            cmd.Parameters.AddWithValue("@color1", color1);
            cmd.Parameters.AddWithValue("@color2", color2);
            cmd.Parameters.AddWithValue("@map_id", mapID);
            cmd.Parameters.AddWithValue("@map_x", mapX);
            cmd.Parameters.AddWithValue("@map_y", mapY);
            cmd.Parameters.AddWithValue("@s_map_id", s_mapID);
            cmd.Parameters.AddWithValue("@s_map_x", s_mapX);
            cmd.Parameters.AddWithValue("@s_map_y", s_mapY);
            cmd.Parameters.AddWithValue("@gold", gold);
            cmd.Parameters.AddWithValue("@gold_bank", gold_bank);
            cmd.Parameters.AddWithValue("@honor", honor);
            cmd.Parameters.AddWithValue("@pet_battle", pet_battle);
            cmd.Parameters.AddWithValue("@id", client.accID);
            cmd.Parameters.AddWithValue("@equip", JsonConvert.SerializeObject(saveEquipmentJson(), Formatting.None));
            cmd.Parameters.AddWithValue("@inventory", JsonConvert.SerializeObject(inventory.saveContainerJson(), Formatting.None));
            cmd.Parameters.AddWithValue("@bag", JsonConvert.SerializeObject(bag.saveContainerJson(), Formatting.None));
            cmd.Parameters.AddWithValue("@storage", JsonConvert.SerializeObject(storage.saveContainerJson(), Formatting.None));
            cmd.Parameters.AddWithValue("@skill", JsonConvert.SerializeObject(skill, Formatting.None));
            cmd.Parameters.AddWithValue("@skill_rb2", JsonConvert.SerializeObject(skill_rb2, Formatting.None));
            cmd.Parameters.AddWithValue("@ball_point", JsonConvert.SerializeObject(ball_point, Formatting.None));
            cmd.Parameters.AddWithValue("@balllist", JsonConvert.SerializeObject(ballList, Formatting.None));
            cmd.Parameters.AddWithValue("@hotkey", JsonConvert.SerializeObject(hotkey, Formatting.None));
            cmd.Parameters.AddWithValue("@uesitemcout", JsonConvert.SerializeObject(uesitemcout, Formatting.None));
            cmd.Parameters.AddWithValue("@rb", rb);
            cmd.Parameters.AddWithValue("@job", job);
            cmd.Parameters.AddWithValue("@element", element);
            cmd.Parameters.AddWithValue("@ai", ai);
            cmd.Parameters.AddWithValue("@onoffbt", onoffbt);
            cmd.Parameters.AddWithValue("@allball", JsonConvert.SerializeObject(allball, Formatting.None));
            cmd.Parameters.AddWithValue("@center", JsonConvert.SerializeObject(center, Formatting.None));
            cmd.ExecuteNonQuery();
            SavePoint();
            if (!client.removeChr)
                client.saveQuest();
        }

        public void SavePoint()
        {
            var c = new TSMysqlConnection();
            c.updateQuery("UPDATE " + TSServer.config.tbAccount + " SET point =" + point + " WHERE Id = " + client.accID);
            c.connection.Close();
        }

        public byte[] saveEquipment()
        {
            var data = new byte[100];
            int pos = 0;
            for (int i = 0; i < 6; i++)
                if (equipment[i] != null)
                    equipment[i].generateEquipBinary(ref data, ref pos);
            return data;
        }

        public int[,] saveEquipmentJson()
        {
            int[,] equips = new int[6, 10];
            for (int i = 0; i < 6; i++)
            {
                if (equipment[i] != null)
                {
                    equips[i, 0] = equipment[i].slot;
                    equips[i, 1] = equipment[i].Itemid;
                    equips[i, 2] = equipment[i].duration;
                    equips[i, 3] = equipment[i].elem_type;
                    equips[i, 4] = equipment[i].elem_val;
                    equips[i, 5] = equipment[i].other_type;
                    equips[i, 6] = equipment[i].other_val;
                    equips[i, 7] = equipment[i].anti;
                    equips[i, 8] = (int)equipment[i].exp;
                    equips[i, 9] = equipment[i].uescout;
                }
            }
            return equips;
        }

        public void loadEquipment(byte[] data)
        {
            int pos = 0;
            ushort itemid;
            while (pos < data.Length)
            {
                if (data[pos] != 0)
                {
                    itemid = (ushort)(data[pos + 1] + (data[pos + 2] << 8));
                    equipment[data[pos] - 1] = new TSEquipment(null, itemid, data[pos], 1);
                    equipment[data[pos] - 1].equip.duration = data[pos + 3];
                    equipment[data[pos] - 1].equip.elem_type = data[pos + 4];
                    equipment[data[pos] - 1].equip.elem_val = data[pos + 5] + (data[pos + 6] << 8);
                    equipment[data[pos] - 1].char_owner = this;
                    nb_equips++;
                    addEquipBonus(ItemData.itemList[itemid].prop1, ItemData.itemList[itemid].prop1_val, 0);
                    addEquipBonus(ItemData.itemList[itemid].prop2, ItemData.itemList[itemid].prop2_val, 0);
                    pos += 7;
                }
                else
                    break;
            }
        }

        public void loadEquipmentJson(JArray jArray)
        {
            for (int i = 0; i < jArray.Count; i++)
            {
                JArray v = (JArray)jArray[i];
                byte slot = (byte)v[0];
                ushort itemid = (ushort)v[1];
                byte duration = (byte)v[2];
                byte elem_type = (byte)v[3];
                int elem_val = (int)v[4];
                byte other_type = (byte)v[5];
                int other_val = (int)v[6];
                byte anti = (byte)v[7];
                int exp = (int)v[8];
                ushort uescout = (ushort)v[9];
                if (slot == 0)
                {
                    continue;
                }
                equipment[slot - 1] = new TSEquipment(null, itemid, slot, 1);
                equipment[slot - 1].equip.duration = duration;
                equipment[slot - 1].equip.elem_type = elem_type;
                equipment[slot - 1].equip.elem_val = elem_val;
                equipment[slot - 1].equip.other_type = other_type;
                equipment[slot - 1].equip.other_val = other_val;
                equipment[slot - 1].equip.anti = anti;
                equipment[slot - 1].equip.exp = (uint)exp;
                equipment[slot - 1].equip.uescout = uescout;
                equipment[slot - 1].char_owner = this;
                nb_equips++;
                addEquipBonus(ItemData.itemList[itemid].prop1, ItemData.itemList[itemid].prop1_val, 0);
                addEquipBonus(ItemData.itemList[itemid].prop2, ItemData.itemList[itemid].prop2_val, 0);
                if (equipment[slot - 1].elem_type == element)
                {
                    addEquipBonus(ItemData.itemList[itemid].prop1, equipment[slot - 1].elem_val, 0);
                    if (ItemData.itemList[itemid].prop2_val > 0)
                    {
                        addEquipBonus(ItemData.itemList[itemid].prop2, equipment[slot - 1].elem_val, 0);
                    }
                }
                if (equipment[slot - 1].other_type == 5 || equipment[slot - 1].other_type == element)
                {
                    addEquipBonus(ItemData.itemList[itemid].prop1, equipment[slot - 1].other_val, 0);
                    if (ItemData.itemList[itemid].prop2_val > 0)
                    {
                        addEquipBonus(ItemData.itemList[itemid].prop2, equipment[slot - 1].other_val, 0);
                    }
                }
            }
        }

        public byte[] saveSkill()
        {
            var data = new byte[600];
            int pos = 0;
            foreach (ushort s in skill.Keys)
            {
                data[pos] = (byte)s;
                data[pos + 1] = (byte)(s >> 8);
                data[pos + 2] = skill[s];
                pos += 3;
            }
            if (rb == 2)
            {
                data[pos] = 0xff;
                data[pos + 1] = 0xff;
                data[pos + 2] = ball_point;
                pos += 3;
                for (int i = 0; i < 12; i++)
                    if (ballList[i])
                    {
                        data[pos] = (byte)(i + 1);
                        pos++;
                    }
                data[pos] = 0xff;
                pos++;
                for (int i = 0; i < 8; i++)
                    if (skill_rb2[i] != 0)
                    {
                        data[pos] = (byte)(i + 6);
                        data[pos + 1] = (byte)skill_rb2[i];
                        data[pos + 2] = (byte)(skill_rb2[i] >> 8);
                        pos += 3;
                    }
            }
            return data;
        }

        public void loadSkill(byte[] data)
        {
            int pos = 0;
            ushort sk_id;
            if (data.Length < 3) return;
            while (pos < data.Length)
            {
                sk_id = (ushort)(data[pos] + (data[pos + 1] << 8));
                if (sk_id != 0 && sk_id != 0xffff)
                {
                    skill.TryAdd(sk_id, data[pos + 2]);
                    pos += 3;
                }
                else if (sk_id == 0xffff)
                {
                    ball_point = data[pos + 2];
                    pos += 3;
                    while (data[pos] != 0xff && data[pos] != 0)
                    {
                        ballList[data[pos] - 1] = true;
                        pos++;
                    }
                    pos++;
                    while (data[pos] != 0)
                    {
                        skill_rb2[data[pos] - 6] = PacketReader.read16(data, pos + 1);
                        pos += 3;
                    }
                    break;
                }
                else
                    break;
            }
        }

        public byte[] saveHotkey()
        {
            var data = new byte[30];
            int pos = 0;
            for (byte i = 1; i <= 10; i++)
                if (hotkey[i - 1] != 0)
                {
                    data[pos] = i;
                    data[pos + 1] = (byte)hotkey[i - 1];
                    data[pos + 2] = (byte)(hotkey[i - 1] >> 8);
                    pos += 3;
                }
            return data;
        }

        public void loadHotkey(byte[] data)
        {
            int pos = 0;
            while (pos < data.Length)
            {
                if (data[pos] != 0)
                {
                    hotkey[data[pos] - 1] = (ushort)(data[pos + 1] + (data[pos + 2] << 8));
                    pos += 3;
                }
                else break;
            }
        }

        public void reply(byte[] data)
        {
            if (client.online)
                client.reply(data);
        }

        public void replyToMap(byte[] data, bool self)
        {
            if (client.map != null && client != null)
                client.map.BroadCast(client, data, self);
        }

        public void replyToAll(byte[] data, bool self)
        {
            if (client.map != null)
            {
                if (TSWorld.getInstance().listMap.ContainsKey(client.map.mapid))
                {
                    foreach (TSMap m in TSWorld.getInstance().listMap.Values)
                    {
                        if (client != null)
                            m.BroadCast(client, data, self);
                    }
                }
            }
        }

        public void replyToTeam(byte[] data)
        {
            if (party != null && party.member != null)
            {
                List<TSCharacter> members = new List<TSCharacter>(party.member);
                for (int i = 0; i < members.Count; i++)
                {
                    members[i].reply(data);
                }
            }
        }

        public void replyToTeamNotme(byte[] data)
        {
            if (party != null && party.member != null)
            {
                List<TSCharacter> members = new List<TSCharacter>(party.member);
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].client.accID != client.accID)
                    {
                        members[i].reply(data);
                    }
                }
            }
        }

        public void replyToArmy(byte[] data, bool self)
        {
        }

        public bool isTeamLeader()
        {
            if (party == null)
                return false;
            else
            {
                if (party.leader_id == client.accID)
                    return true;
                else
                    return false;
            }
        }

        public bool isTeamMember()
        {
            if (party == null)
                return false;
            else
            {
                if (party.leader_id != client.accID)
                    return true;
                else
                    return false;
            }
        }

        public bool isJoinedTeam()
        {
            return party != null;
        }

        public void setHp(int amount)
        {
            hp += amount;
            if (hp > hp_max)
                hp = hp_max;
            if (hp <= 0)
                if (client.battle != null)
                    hp = 0;
                else hp = 1;
        }

        public void setCHp(int amount)
        {
            clone_hp += amount;
            if (clone_hp > hp_max)
                clone_hp = hp_max;
            if (clone_hp <= 0)
                if (client.battle != null)
                    clone_hp = 0;
                else clone_hp = 1;
        }

        public void setCSp(int amount)
        {
            clone_sp += amount;
            if (clone_sp > sp_max)
                clone_sp = sp_max;
            if (clone_sp < 0) clone_sp = 0;
        }

        public void setSp(int amount)
        {
            sp += amount;
            if (sp > sp_max)
                sp = sp_max;
            if (sp < 0) sp = 0;
        }

        public int getHpMax()
        {
            if (rb == 0)
                return (int)Math.Round((Math.Pow(level, 0.35) + 1) * hpx * 2 + 80 + hp2 + FullHpMax + level);
            else if (rb == 1)
                return (int)Math.Round((Math.Pow(level, 0.35) + 2) * hpx * 2 + 180 + hp2 + FullHpMax + level);
            else
            {
                if (job == 1)
                    return (int)Math.Round((Math.Pow(level, 0.35) * 2 + 25) * hpx + 280 + hp2 + FullHpMax + level);
                else if (job == 2)
                    return (int)Math.Round((Math.Pow(level, 0.35) * 3 + 30) * hpx + 380 + hp2 + FullHpMax + level);
                else if (job == 3)
                    return (int)Math.Round((Math.Pow(level, 0.35) + 11.5) * hpx * 2 + 180 + hp2 + FullHpMax + level);
                else
                    return (int)Math.Round((Math.Pow(level, 0.35) + 10.5) * hpx * 2 + 180 + hp2 + FullHpMax + level);
            }
        }

        public int getSpMax()
        {
            if (rb == 0)
                return (int)Math.Round(Math.Pow(level, 0.25) * spx * 2 + 60 + sp2 + FullSpMax + level);
            else if (rb == 1)
                return (int)Math.Round(Math.Pow(level, 0.25) * spx * 2 + 110 + sp2 + FullSpMax + level);
            else
            {
                if (job == 1)
                    return (int)Math.Round(Math.Pow(level, 0.25) * spx * 2 + 160 + sp2 + FullSpMax + level);
                else if (job == 2)
                    return (int)Math.Round(Math.Pow(level, 0.25) * spx * 2 + 160 + sp2 + FullSpMax + level);
                else if (job == 3)
                    return (int)Math.Round(Math.Pow(level, 0.25) * spx * 3 + 310 + sp2 + FullSpMax + level);
                else
                    return (int)Math.Round(Math.Pow(level, 0.25) * spx * 3.5 + 410 + sp2 + FullSpMax + level);
            }
        }

        public void setExp(double amountD)
        {
            int amount = 0;
            if (gm == 2) amount = ((int)amountD * 2);
            if (rb == 1)
            {
                amount = ((int)amountD / 2);
            }
            else if (rb == 2)
            {
                amount = ((int)amountD / 4);
            }
            else
                amount = (int)amountD;
            if (level >= 200) return;
            totalxp = (uint)(totalxp + amount);
            currentxp += amount;
            if (amount > 0)
            {
                int next_level_xp = (int)(Math.Pow(level + 1, xp_pow) + 5);
                while (currentxp >= next_level_xp)
                {
                    currentxp -= next_level_xp;
                    if (level >= 200) return;
                    levelUp();
                    next_level_xp = (int)(Math.Pow(level + 1, xp_pow) + 5);
                }
            }
            else if (currentxp < 0) currentxp = 0;
            refresh((int)totalxp, TSConstants._TOTALEXP);
        }

        public void setlostExp(double amountD)
        {
            int amount = (int)amountD;
            if (level >= 200) return;
            if (currentxp <= 0) return;
            if (currentxp > amount)
            {
                currentxp -= amount;
            }
            else
                currentxp = 0;
            refresh((int)totalxp, TSConstants._TOTALEXP);
        }

        public void levelUp()
        {
            if (level >= 200) return;
            level++;
            stt_point += 2;
            skill_point += 1;
            hp_max = getHpMax() + hp2;
            sp_max = getSpMax() + sp2;
            hp = hp_max;
            sp = sp_max;
            refresh(level, 0x23);
            refresh(skill_point, 0x25);
            refresh(stt_point, 0x26);
            refreshTeam();
        }

        public void levelUpcomman(byte index)
        {
            level = index;
            int sttpointForLv = index * 2;
            stt_point += sttpointForLv;
            skill_point += index;
            hp_max = getHpMax();
            sp_max = getSpMax();
            hp = hp_max;
            sp = sp_max;
            refresh(level, TSConstants._LVL);
            refresh(skill_point, TSConstants._SKILL_POINT);
            refresh(stt_point, TSConstants._STATS_POINT);
            refreshTeam();
        }

        public void addGold(uint amont)
        {
            PacketCreator p = new PacketCreator(0x1A, 01);
            gold += amont;
            p.add32(amont);
            p.addZero(8);
            client.reply(p.send());
            client.saveChrtoDB();
        }

        public void removeGold(uint amont)
        {
            PacketCreator p = new PacketCreator(0x1A, 01);
            gold -= amont;
            p.add32(amont);
            p.addZero(8);
            client.reply(p.send());
            client.saveChrtoDB();
        }

        public void setStat(byte prop_code, int val)
        {
            switch (prop_code)
            {
                case 0x1b:
                    checkSetStat(ref mag, prop_code, val);
                    break;
                case 0x1c:
                    checkSetStat(ref atk, prop_code, val);
                    break;
                case 0x1d:
                    checkSetStat(ref def, prop_code, val);
                    break;
                case 0x1e:
                    checkSetStat(ref agi, prop_code, val);
                    break;
                case 0x1f:
                    checkSetStat(ref hpx, prop_code, val);
                    hp_max = getHpMax();
                    break;
                case 0x20:
                    checkSetStat(ref spx, prop_code, val);
                    sp_max = getSpMax();
                    break;
            }
        }

        public void checkSetStat(ref int prop, byte prop_code, int val)
        {
            if (val > prop + 1 || stt_point == 0)
                return;
            prop++;
            stt_point--;
            refresh(stt_point, TSConstants._STATS_POINT);
            refresh(prop, prop_code);
        }

        public void setSkill(ushort skillid, byte sk_lvl)
        {
            if (SkillData.skillList.ContainsKey(skillid) && skill_point > 0)
            {
                SkillInfo s = SkillData.skillList[skillid];
                int skillpt_needed;
                bool newskill;
                if (skill.ContainsKey(skillid))
                {
                    skillpt_needed = sk_lvl - skill[skillid];
                    newskill = false;
                }
                else if (s.require_sk == 0 || skill.ContainsKey(s.require_sk) || s.id == 13014)
                {
                    if (SkillData.skillList[skillid].elem != element)
                    {
                        if (s.elem == 5 && skill_point > 0)
                        {
                            skillpt_needed = s.sk_point + sk_lvl - 1;
                        }
                        else
                            skillpt_needed = s.sk_point * 2 + sk_lvl - 1;
                    }
                    else
                        skillpt_needed = s.sk_point + sk_lvl - 1;
                    newskill = true;
                }
                else return;
                if (skillpt_needed > 0 && skill_point >= skillpt_needed)
                {
                    if (newskill)
                        skill.TryAdd(skillid, sk_lvl);
                    else skill[skillid] = sk_lvl;
                    skill_point -= skillpt_needed;
                    refresh(skill_point, TSConstants._SKILL_POINT);
                    refreshFull(0x6e, sk_lvl, skillid);
                }
            }
            sendHotkey();
        }

        public void setSkillRb2(byte[] data)
        {
            int pos = 2;
            uint ball_use = PacketReader.read32(data, pos);
            if (ball_point < ball_use) return;
            ball_point -= (byte)ball_use;
            pos += 4;
            for (int i = 0; i < ball_use; i++)
                ballList[data[pos + i] - 1] = true;
            pos += (int)ball_use;
            uint nbskill = PacketReader.read32(data, pos);
            pos += 4;
            for (int i = 0; i < nbskill; i++)
            {
                setSkill(PacketReader.read16(data, pos), data[pos + 2]);
                pos += 3;
            }
            uint skill_place = PacketReader.read32(data, pos) / 3;
            pos += 4;
            for (int i = 0; i < skill_place; i++)
            {
                skill_rb2[data[pos] - 6] = PacketReader.read16(data, pos + 1);
                pos += 3;
            }
            sendBallList();
        }

        public bool setBattlePet(ushort npcid)
        {
            var c = new TSMysqlConnection();
            for (int i = 0; i < 4; i++)
                if (pet[i] != null)
                    if (pet[i].NPCid == npcid)
                    {
                        pet_battle = (sbyte)i;
                        c.updateQuery("UPDATE " + TSServer.config.tbChars + " SET `pet_battle` = " + pet_battle + " WHERE id=" + this.charId);
                        return true;
                    }
            return false;
        }

        public bool unsetBattlePet()
        {
            var c = new TSMysqlConnection();
            if (pet_battle != -1)
            {
                pet_battle = -1;
                c.updateQuery("UPDATE " + TSServer.config.tbChars + " SET `pet_battle` = " + pet_battle + " WHERE id=" + this.charId);
                return true;
            }
            return false;
        }

        public void rebornChar(byte nb_reborn, byte j)
        {
            if (level < 120) return;
            if (rb != nb_reborn - 1) return;
            if (rb == 2 && (j < 1 || j > 4)) return;
            if (nb_equips > 0) return;
            int sttEx = 0;
            int allskpoint = 0;
            int skEx = 0;
            if (rb == 0)
            {
                sttEx = (atk + mag + def + agi + hpx + spx + stt_point) - ((level - 1) * 2);
                foreach (KeyValuePair<ushort, byte> entry in skill)
                {
                    byte Sk_uespoint = SkillData.skillList[entry.Key].sk_point;
                    byte sk_elem = SkillData.skillList[entry.Key].elem;
                    if (sk_elem != 5 && sk_elem != 0)
                    {
                        if (element == sk_elem)
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += Sk_uespoint;
                            }
                            else
                                allskpoint += (entry.Value - 1) + Sk_uespoint;
                        }
                        else
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += (Sk_uespoint * 2);
                            }
                            else
                                allskpoint += (entry.Value - 1) + (Sk_uespoint * 2);
                        }
                    }
                    skEx = (allskpoint + skill_point) - (level - 1);
                }
            }
            else
            {
                sttEx = ((atk + mag + def + agi + hpx + spx + stt_point) - ((level - 1) * 2)) - (6 + (int)(level / (10 / (1))));
                foreach (KeyValuePair<ushort, byte> entry in skill)
                {
                    byte Sk_uespoint = SkillData.skillList[entry.Key].sk_point;
                    byte sk_elem = SkillData.skillList[entry.Key].elem;
                    if (sk_elem != 5 && sk_elem != 0)
                    {
                        if (element == sk_elem)
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += Sk_uespoint;
                            }
                            else
                                allskpoint += (entry.Value - 1) + Sk_uespoint;
                        }
                        else
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += (Sk_uespoint * 2);
                            }
                            else
                                allskpoint += (entry.Value - 1) + (Sk_uespoint * 2);
                        }
                    }
                    skEx = (allskpoint + skill_point) - (level - 1) - (int)(level / (4 / 1));
                }
            }
            rb++;
            if (rb == 2) job = j;
            stt_point = 6 + sttEx + (int)(level / (10 / nb_reborn));
            skill_point = skEx + (int)(level / (4 / nb_reborn));
            atk = 0; mag = 0; def = 0; agi = 0; hpx = 0; spx = 0;
            level = 1;
            totalxp = 0;
            currentxp = 0;
            hp_max = getHpMax(); hp = hp_max;
            sp_max = getSpMax(); sp = sp_max;
            honor = 0;
            foreach (ushort sk_id in skill.Keys)
            {
                refreshFull(0x6e, 0, sk_id);
            }
            skill.Clear();
            skill.TryAdd(14001, 1);
            skill.TryAdd(14015, 10);
            skill.TryAdd(14021, 5);
            skill.TryAdd(14023, 1);
            skill.TryAdd(14035, 1);
            refresh(stt_point, TSConstants._STATS_POINT);
            refresh(skill_point, TSConstants._SKILL_POINT);
            refresh((int)totalxp, TSConstants._TOTALEXP);
            refresh(level, TSConstants._LVL);
            refreshChr();
            sendLook(true);
            sendInfo();
            client.saveChrtoDB();
        }

        public void rebornCharQ(byte nb_reborn, byte j, byte hai, uint col1, uint col2)
        {
            if (level < 120) return;
            if (rb != nb_reborn - 1) return;
            if (rb == 2 && (j < 1 || j > 4)) return;
            if (nb_equips > 0) return;
            int sttEx = 0;
            int allskpoint = 0;
            int skEx = 0;
            if (rb == 0)
            {
                sttEx = (atk + mag + def + agi + hpx + spx + stt_point) - ((level - 1) * 2);
                foreach (KeyValuePair<ushort, byte> entry in skill)
                {
                    byte Sk_uespoint = SkillData.skillList[entry.Key].sk_point;
                    byte sk_elem = SkillData.skillList[entry.Key].elem;
                    if (sk_elem != 5 && sk_elem != 0)
                    {
                        if (element == sk_elem)
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += Sk_uespoint;
                            }
                            else
                                allskpoint += (entry.Value - 1) + Sk_uespoint;
                        }
                        else
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += (Sk_uespoint * 2);
                            }
                            else
                                allskpoint += (entry.Value - 1) + (Sk_uespoint * 2);
                        }
                    }
                    skEx = (allskpoint + skill_point) - (level - 1);
                }
            }
            else
            {
                sttEx = ((atk + mag + def + agi + hpx + spx + stt_point) - ((level - 1) * 2)) - (6 + (int)(level / (10 / (1))));
                foreach (KeyValuePair<ushort, byte> entry in skill)
                {
                    byte Sk_uespoint = SkillData.skillList[entry.Key].sk_point;
                    byte sk_elem = SkillData.skillList[entry.Key].elem;
                    if (sk_elem != 5 && sk_elem != 0)
                    {
                        if (element == sk_elem)
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += Sk_uespoint;
                            }
                            else
                                allskpoint += (entry.Value - 1) + Sk_uespoint;
                        }
                        else
                        {
                            if (entry.Value == 1)
                            {
                                allskpoint += (Sk_uespoint * 2);
                            }
                            else
                                allskpoint += (entry.Value - 1) + (Sk_uespoint * 2);
                        }
                    }
                    skEx = (allskpoint + skill_point) - (level - 1) - (int)(level / (4 / 1));
                }
            }
            rb++;
            if (rb == 2) job = j;
            stt_point = 6 + sttEx + (int)(level / (10 / nb_reborn));
            skill_point = skEx + (int)(level / (4 / nb_reborn));
            atk = 0; mag = 0; def = 0; agi = 0; hpx = 0; spx = 0;
            level = 1;
            totalxp = 0;
            currentxp = 0;
            hp_max = getHpMax(); hp = hp_max;
            sp_max = getSpMax(); sp = sp_max;
            hair = hai;
            color1 = col1;
            color2 = col2;
            honor = 0;
            foreach (ushort sk_id in skill.Keys)
            {
                refreshFull(0x6e, 0, sk_id);
            }
            skill.Clear();
            skill.TryAdd(14001, 1);
            skill.TryAdd(14015, 10);
            skill.TryAdd(14021, 5);
            skill.TryAdd(14023, 1);
            skill.TryAdd(14035, 1);
            refresh(stt_point, TSConstants._STATS_POINT);
            refresh(skill_point, TSConstants._SKILL_POINT);
            refresh((int)totalxp, TSConstants._TOTALEXP);
            refresh(level, TSConstants._LVL);
            refreshChr();
            sendLook(true);
            sendInfo();
            foreach (TSClient c in client.map.listPlayers.Values.ToArray())
            {
                if (c != null && c.accID != client.accID)
                {
                    c.reply(sendLookForOther());
                    if (guild_id > 0)
                    {
                        var pIcon = GuildData.BuildGuildIconPacket(guild_id);
                        if (pIcon != null) c.reply(pIcon.send());
                    }
                    if (guild_id > 0 && c.getChar() != null && c.getChar().guild_id == guild_id)
                    {
                        var pFlag = GuildData.BuildGuildFlagPacket(guild_id, client.accID);
                        if (pFlag != null) c.reply(pFlag.send());
                    }
                }
            }
            client.saveChrtoDB();
        }

        public bool checkPetReborn(byte nb_reborn)
        {
            int rb_prop = nb_reborn == 1 ? 65 : 67;
            for (int i = 0; i < 4; i++)
                if (pet[i] != null)
                    if (pet[i].reborn == nb_reborn - 1 && pet[i].level >= nb_reborn * 30 && pet[i].fai >= nb_reborn * 40 + 20)
                    {
                        ushort rb_item = 0;
                        foreach (ItemInfo it in ItemData.itemList.Values)
                            if (it.prop1 == rb_prop && it.prop1_val == pet[i].NPCid)
                            {
                                rb_item = it.id;
                                break;
                            }
                        if (rb_item != 0)
                            if (inventory.getItemById(rb_item) != 25)
                                return true;
                    }
            return false;
        }

        public void checkPetAddSkill4()
        {
            byte[] listSolt = new byte[] { };
            int item_prop1 = 66;
            List<byte> listpet = new List<byte>();
            for (int i = 0; i < 4; i++)
                if (pet[i] != null)
                    if (pet[i].reborn >= 0 && pet[i].level >= 60 && pet[i].fai >= 60 && NpcData.npcList[pet[i].NPCid].skill4 >= 10000 && NpcData.npcList[pet[i].NPCid].reborn > 0 && pet[i].skill4_lvl == 0)
                    {
                        int elementPet = NpcData.npcList[pet[i].NPCid].element;
                        ushort idpet = pet[i].NPCid;
                        ushort item_skill4 = 0;
                        int item_pet_element = elementPet == 1 ? 30064 : elementPet == 2 ? 30065 : elementPet == 3 ? 30066 : elementPet == 4 ? 30067 : 0;
                        foreach (ItemInfo it in ItemData.itemList.Values)
                            if (it.prop1 == item_prop1 && (it.prop1_val == idpet || it.prop2_val == idpet))
                            {
                                item_skill4 = it.id;
                                break;
                            }
                        if (item_skill4 != 0)
                            if (inventory.getItemById(item_skill4) != 25)
                                if (inventory.getItemById((ushort)item_pet_element) != 25)
                                {
                                    listpet.Add(pet[i].slot);
                                }
                    }
            listSolt = listpet.ToArray();
            if (listSolt.Length > 0)
            {
                var p = new PacketCreator(0x24, 12);
                p.addByte((byte)listSolt.Length);
                p.addBytes(listSolt);
                reply(p.send());
            }
        }

        public void rebornPet(byte nb_reborn, byte slot)
        {
            int rb_prop = nb_reborn == 1 ? 65 : 67;
            ushort rb_item = 0;
            ushort pet_rb = 0;
            byte item_slot = 25;
            foreach (ItemInfo i in ItemData.itemList.Values)
                if (i.prop1 == rb_prop && i.prop1_val == pet[slot - 1].NPCid)
                {
                    rb_item = i.id;
                    pet_rb = (ushort)i.prop2_val;
                    break;
                }
            if (rb_item == 0) return;
            item_slot = inventory.getItemById(rb_item);
            if (item_slot == 25) return;
            inventory.dropItemBySlot((byte)(item_slot + 1), 1);
            int stt_point_bonus = (int)((pet[slot - 1].level) / (nb_reborn * 2));
            removePet(slot);
            addPet(pet_rb, stt_point_bonus, 1);
            client.savetoDB();
        }

        public void Addskill4Pet(byte slot)
        {
            int item_prop1 = 66;
            byte item_slot = 25;
            byte item_slot2 = 25;
            int elementPet = NpcData.npcList[pet[slot - 1].NPCid].element;
            ushort idpet = pet[slot - 1].NPCid;
            ushort idpetSkill4 = NpcData.npcList[idpet].skill4;
            ushort item_skill4 = 0;
            int item_pet_element = elementPet == 1 ? 30064 : elementPet == 2 ? 30065 : elementPet == 3 ? 30066 : elementPet == 4 ? 30067 : 0;
            foreach (ItemInfo it in ItemData.itemList.Values)
                if (it.prop1 == item_prop1 && (it.prop1_val == idpet || it.prop2_val == idpet))
                {
                    item_skill4 = it.id;
                    break;
                }
            item_slot = inventory.getItemById(item_skill4);
            item_slot2 = inventory.getItemById((ushort)item_pet_element);
            if (item_slot == 25) return;
            if (item_slot2 == 25) return;
            pet[slot - 1].skill4_lvl = SkillData.skillList[idpetSkill4].max_lvl;
            pet[slot - 1].refreshFull(TSConstants._SKILL_LV, pet[slot - 1].skill4_lvl, idpetSkill4);
            client.Sendpacket("F44402002C01");
            inventory.dropItemBySlot((byte)(item_slot + 1), 1);
            inventory.dropItemBySlot((byte)(item_slot2 + 1), 1);
            client.savetoDB();
            this.sendPetInfo();
        }

        public void Addskill4PetNohaveITEM(byte slot)
        {
            int item_prop1 = 66;
            ushort idpet = pet[slot - 1].NPCid;
            ushort idpetSkill4 = NpcData.npcList[idpet].skill4;
            ushort item_skill4 = 0;
            foreach (ItemInfo it in ItemData.itemList.Values)
                if (it.prop1 == item_prop1 && (it.prop1_val == idpet || it.prop2_val == idpet))
                {
                    item_skill4 = it.id;
                    break;
                }
            if (item_skill4 == 0 && idpetSkill4 >= 10000)
            {
                pet[slot - 1].skill4_lvl = SkillData.skillList[idpetSkill4].max_lvl;
                pet[slot - 1].refreshFull(TSConstants._SKILL_LV, pet[slot - 1].skill4_lvl, idpetSkill4);
            }
            else
                return;
        }

        public void rideHorse(bool ride, bool self, ushort horseid = 0)
        {
            if (ride)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (pet[i] != null)
                        if (pet[i].NPCid == horseid)
                        {
                            if (NpcData.npcList[horseid].type == 9 || NpcData.npcList[horseid].type == 22)
                            {
                                PacketCreator p = new PacketCreator(0xf, 5);
                                p.add32(this.client.accID);
                                p.add16(horseid);
                                p.addZero(6);
                                replyToMap(p.send(), self);
                                break;
                            }
                        }
                }
                horseID = horseid;
            }
            else
            {
                PacketCreator p1 = new PacketCreator(0xf, 6);
                p1.add32(this.client.accID);
                p1.addZero(2);
                replyToMap(p1.send(), self);
                horseID = 0;
            }
        }

        public void calculateAgiSadd(ushort horseid)
        {
            ushort petEqitem = findpetid(horseid);
            if (petEqitem.ToString().Length == 5 && ItemData.itemList.TryGetValue(petEqitem, out var p))
            {
                if (p.unk3 == 81 && p.unk5 == 140)
                {
                    int petAgi = pet.FirstOrDefault(x => x.NPCid == horseid).agi;
                    double divide = petAgi / ((double)100 / p.unk9);
                    double digi_divide = Math.Round(divide, MidpointRounding.AwayFromZero);
                    agi2 += (ushort)digi_divide;
                    horseSadd_Agi2 = (ushort)digi_divide;
                    if (TSServer.config.debugmode == true)
                        Console.WriteLine("ride Horse AGI2 > " + agi2);
                }
            }
            else
            {
                agi2 -= horseSadd_Agi2;
                horseSadd_Agi2 = 0;
                if (TSServer.config.debugmode == true)
                    Console.WriteLine("ride Horse AGI2 > " + agi2);
            }
        }

        public void setCharElement(byte elemen)
        {
            this.element = elemen;
            sendInfo();
        }

        public void addskillpoint(int point)
        {
            this.skill_point += point;
            refresh(skill_point, TSConstants._SKILL_POINT);
        }

        public void addsttpoint(int point)
        {
            this.stt_point += point;
            refresh(stt_point, 0x26);
        }

        public void setstt(string stname, int stt)
        {
            switch (stname)
            {
                case "int":
                    this.mag = stt;
                    refreshChr();
                    break;
                case "atk":
                    this.atk = stt;
                    refreshChr();
                    break;
                case "def":
                    this.def = stt;
                    refreshChr();
                    break;
                case "hpx":
                    this.hpx = stt;
                    this.hp_max = getHpMax();
                    refreshChr();
                    break;
                case "spx":
                    this.spx = stt;
                    this.sp_max = getSpMax();
                    refreshChr();
                    break;
                case "agi":
                    this.agi = stt;
                    refreshChr();
                    break;
                default:
                    break;
            }
        }

        public void addSkillTestServer()
        {
            addSummonSkill(10);
            skill.TryAdd(14001, 1);
            skill.TryAdd(14015, 10);
            skill.TryAdd(14021, 5);
            skill.TryAdd(14023, 1);
            skill.TryAdd(14035, 1);
            foreach (KeyValuePair<ushort, byte> entry in skill)
            {
                refreshFull(0x6e, entry.Value, entry.Key);
            }
        }

        public void resetskillPoint()
        {
            foreach (KeyValuePair<ushort, byte> entry in skill)
            {
                byte Sk_uespoint = SkillData.skillList[entry.Key].sk_point;
                byte sk_elem = SkillData.skillList[entry.Key].elem;
                byte skill_p = SkillData.skillList[entry.Key].sk_point;
                if (element == sk_elem && skill_p > 0)
                {
                    if (entry.Value == 1)
                    {
                        skill_point += Sk_uespoint;
                    }
                    else
                        skill_point += (entry.Value - 1) + Sk_uespoint;
                }
                else if (element != sk_elem && skill_p > 0)
                {
                    if (sk_elem == 5)
                    {
                        if (entry.Value == 1)
                        {
                            skill_point += Sk_uespoint;
                        }
                        else
                            skill_point += (entry.Value - 1) + Sk_uespoint;
                    }
                    else if (entry.Value == 1)
                    {
                        skill_point += (Sk_uespoint * 2);
                    }
                    else
                        skill_point += (entry.Value - 1) + (Sk_uespoint * 2);
                }
            }
            refresh(skill_point, TSConstants._SKILL_POINT);
        }

        public void resetskill()
        {
            resetskillPoint();
            foreach (ushort sk_id in skill.Keys)
            {
                refreshFull(0x6e, 0, sk_id);
            }
            skill.Clear();
            addSkillTestServer();
            sendBallList();
        }

        public void resetstt()
        {
            int repoint = mag + atk + def + hpx + spx + agi;
            stt_point += repoint;
            int zero = 0;
            this.mag = zero;
            this.atk = zero;
            this.def = zero;
            this.hpx = zero;
            this.spx = zero;
            this.agi = zero;
            this.hp_max = getHpMax();
            this.sp_max = getSpMax();
            refresh(stt_point, TSConstants._STATS_POINT);
            refreshChr();
        }

        public void allstt(int stt)
        {
            this.mag = stt;
            this.atk = stt;
            this.def = stt;
            this.hpx = stt;
            this.spx = stt;
            this.agi = stt;
            this.hp_max = getHpMax();
            this.sp_max = getSpMax();
            refreshChr();
        }

        public void refreshskill(ushort skillid, byte lv_skill)
        {
            refreshFull(0x6e, lv_skill, skillid);
        }

        public void setallskill()
        {
            skill.Clear();
            skill.TryAdd(14001, 1);
            skill.TryAdd(14015, 10);
            skill.TryAdd(14021, 5);
            skill.TryAdd(14023, 1);
            setskillElement5();
            setallskillForJob();
            setBalllisRb2();
            setallskillForElement();
            AddSkillrb2();
            foreach (ushort sk_id in skill.Keys)
            {
                skill.TryGetValue(sk_id, out var lvsk);
                refreshFull(0x6e, lvsk, sk_id);
            }
            sendBallList();
        }

        public void AddSkillrb2()
        {
            switch (element)
            {
                case 1:
                    skill_rb2[0] = 10027;
                    skill_rb2[1] = 10028;
                    skill_rb2[2] = 10029;
                    skill_rb2[3] = 10030;
                    skill_rb2[4] = 10031;
                    skill_rb2[5] = 10032;
                    skill_rb2[6] = 10033;
                    skill_rb2[7] = 10034;
                    break;
                case 2:
                    skill_rb2[0] = 11027;
                    skill_rb2[1] = 11028;
                    skill_rb2[2] = 11029;
                    skill_rb2[3] = 11030;
                    skill_rb2[4] = 11031;
                    skill_rb2[5] = 11032;
                    skill_rb2[6] = 11033;
                    skill_rb2[7] = 11034;
                    break;
                case 3:
                    skill_rb2[0] = 12027;
                    skill_rb2[1] = 12028;
                    skill_rb2[2] = 12029;
                    skill_rb2[3] = 12030;
                    skill_rb2[4] = 12031;
                    skill_rb2[5] = 12032;
                    skill_rb2[6] = 12033;
                    skill_rb2[7] = 12034;
                    break;
                case 4:
                    skill_rb2[0] = 13027;
                    skill_rb2[1] = 13026;
                    skill_rb2[2] = 13028;
                    skill_rb2[3] = 13029;
                    skill_rb2[4] = 13030;
                    skill_rb2[5] = 13031;
                    skill_rb2[6] = 13032;
                    skill_rb2[7] = 13033;
                    break;
                default:
                    break;
            }
        }

        public void setskillElement5()
        {
            skill.TryAdd(14026, 10);
            skill.TryAdd(14032, 10);
            skill.TryAdd(14002, 1);
            skill.TryAdd(14003, 10);
            skill.TryAdd(14004, 10);
            skill.TryAdd(14005, 10);
            skill.TryAdd(14006, 10);
            skill.TryAdd(14007, 1);
            skill.TryAdd(14008, 5);
        }

        public void setallskillForJob()
        {
            skill.TryAdd(14038, 10);
            switch (job)
            {
                case 1:
                    skill.TryAdd(14043, 10);
                    skill.TryAdd(14044, 5);
                    skill.TryAdd(14045, 10);
                    skill.TryAdd(14046, 5);
                    break;
                case 2:
                    skill.TryAdd(14039, 10);
                    skill.TryAdd(14040, 5);
                    skill.TryAdd(14041, 10);
                    skill.TryAdd(14042, 5);
                    break;
                case 3:
                    skill.TryAdd(14047, 10);
                    skill.TryAdd(14048, 10);
                    skill.TryAdd(14049, 10);
                    skill.TryAdd(14050, 10);
                    break;
                case 4:
                    skill.TryAdd(14051, 10);
                    skill.TryAdd(14052, 5);
                    skill.TryAdd(14053, 5);
                    skill.TryAdd(14054, 5);
                    break;
                default:
                    break;
            }
        }

        public void setallskillForElement()
        {
            switch (element)
            {
                case 1:
                    skill.TryAdd(10001, 10);
                    skill.TryAdd(10002, 10);
                    skill.TryAdd(10003, 10);
                    skill.TryAdd(10004, 10);
                    skill.TryAdd(10005, 10);
                    skill.TryAdd(10006, 10);
                    skill.TryAdd(10007, 10);
                    skill.TryAdd(10008, 10);
                    skill.TryAdd(10009, 10);
                    skill.TryAdd(10010, 10);
                    skill.TryAdd(10011, 10);
                    skill.TryAdd(10012, 10);
                    skill.TryAdd(10013, 10);
                    skill.TryAdd(10014, 1);
                    skill.TryAdd(10015, 5);
                    skill.TryAdd(11001, 10);
                    skill.TryAdd(11002, 10);
                    skill.TryAdd(11003, 10);
                    skill.TryAdd(11004, 10);
                    skill.TryAdd(11005, 10);
                    skill.TryAdd(11006, 10);
                    skill.TryAdd(11007, 10);
                    skill.TryAdd(11008, 10);
                    skill.TryAdd(11009, 10);
                    skill.TryAdd(11010, 10);
                    skill.TryAdd(11011, 10);
                    skill.TryAdd(11012, 1);
                    skill.TryAdd(11013, 10);
                    skill.TryAdd(11014, 10);
                    skill.TryAdd(11015, 1);
                    skill.TryAdd(13001, 10);
                    skill.TryAdd(13002, 5);
                    skill.TryAdd(13003, 5);
                    skill.TryAdd(13004, 10);
                    skill.TryAdd(13005, 5);
                    skill.TryAdd(13006, 10);
                    skill.TryAdd(13007, 10);
                    skill.TryAdd(13008, 1);
                    skill.TryAdd(13009, 10);
                    skill.TryAdd(13010, 10);
                    skill.TryAdd(13011, 5);
                    skill.TryAdd(13012, 5);
                    skill.TryAdd(13013, 10);
                    skill.TryAdd(13014, 10);
                    skill.TryAdd(10020, 10);
                    skill.TryAdd(10021, 10);
                    skill.TryAdd(10022, 10);
                    skill.TryAdd(10023, 10);
                    skill.TryAdd(10024, 10);
                    skill.TryAdd(10025, 5);
                    skill.TryAdd(10026, 5);
                    skill.TryAdd(11020, 10);
                    skill.TryAdd(11021, 10);
                    skill.TryAdd(11022, 10);
                    skill.TryAdd(11023, 10);
                    skill.TryAdd(11024, 5);
                    skill.TryAdd(11025, 1);
                    skill.TryAdd(11026, 10);
                    skill.TryAdd(13019, 10);
                    skill.TryAdd(13020, 5);
                    skill.TryAdd(13021, 5);
                    skill.TryAdd(13022, 10);
                    skill.TryAdd(13023, 10);
                    skill.TryAdd(13024, 10);
                    skill.TryAdd(13025, 5);
                    skill.TryAdd(10027, 10);
                    skill.TryAdd(10028, 10);
                    skill.TryAdd(10029, 10);
                    skill.TryAdd(10030, 10);
                    skill.TryAdd(10031, 10);
                    skill.TryAdd(10032, 10);
                    skill.TryAdd(10033, 5);
                    skill.TryAdd(10034, 10);
                    skill.TryAdd(11027, 5);
                    skill.TryAdd(11028, 5);
                    skill.TryAdd(11029, 10);
                    skill.TryAdd(11030, 10);
                    skill.TryAdd(11031, 1);
                    skill.TryAdd(11032, 5);
                    skill.TryAdd(11033, 10);
                    skill.TryAdd(11034, 10);
                    skill.TryAdd(13026, 10);
                    skill.TryAdd(13027, 10);
                    skill.TryAdd(13028, 10);
                    skill.TryAdd(13029, 10);
                    skill.TryAdd(13030, 5);
                    skill.TryAdd(13031, 10);
                    skill.TryAdd(13032, 5);
                    skill.TryAdd(13033, 10);
                    break;
                case 2:
                    skill.TryAdd(10001, 10);
                    skill.TryAdd(10002, 10);
                    skill.TryAdd(10003, 10);
                    skill.TryAdd(10004, 10);
                    skill.TryAdd(10005, 10);
                    skill.TryAdd(10006, 10);
                    skill.TryAdd(10007, 10);
                    skill.TryAdd(10008, 10);
                    skill.TryAdd(10009, 10);
                    skill.TryAdd(10010, 10);
                    skill.TryAdd(10011, 10);
                    skill.TryAdd(10012, 10);
                    skill.TryAdd(10013, 10);
                    skill.TryAdd(10014, 1);
                    skill.TryAdd(10015, 5);
                    skill.TryAdd(11001, 10);
                    skill.TryAdd(11002, 10);
                    skill.TryAdd(11003, 10);
                    skill.TryAdd(11004, 10);
                    skill.TryAdd(11005, 10);
                    skill.TryAdd(11006, 10);
                    skill.TryAdd(11007, 10);
                    skill.TryAdd(11008, 10);
                    skill.TryAdd(11009, 10);
                    skill.TryAdd(11010, 10);
                    skill.TryAdd(11011, 10);
                    skill.TryAdd(11012, 1);
                    skill.TryAdd(11013, 10);
                    skill.TryAdd(11014, 10);
                    skill.TryAdd(11015, 1);
                    skill.TryAdd(12001, 10);
                    skill.TryAdd(12002, 10);
                    skill.TryAdd(12003, 10);
                    skill.TryAdd(12004, 10);
                    skill.TryAdd(12005, 10);
                    skill.TryAdd(12006, 10);
                    skill.TryAdd(12007, 10);
                    skill.TryAdd(12008, 10);
                    skill.TryAdd(12009, 10);
                    skill.TryAdd(12010, 10);
                    skill.TryAdd(12011, 10);
                    skill.TryAdd(12012, 10);
                    skill.TryAdd(12013, 10);
                    skill.TryAdd(12014, 10);
                    skill.TryAdd(12015, 10);
                    skill.TryAdd(10020, 10);
                    skill.TryAdd(10021, 10);
                    skill.TryAdd(10022, 10);
                    skill.TryAdd(10023, 10);
                    skill.TryAdd(10024, 10);
                    skill.TryAdd(10025, 5);
                    skill.TryAdd(10026, 5);
                    skill.TryAdd(11020, 10);
                    skill.TryAdd(11021, 10);
                    skill.TryAdd(11022, 10);
                    skill.TryAdd(11023, 10);
                    skill.TryAdd(11024, 5);
                    skill.TryAdd(11025, 1);
                    skill.TryAdd(11026, 10);
                    skill.TryAdd(12020, 10);
                    skill.TryAdd(12021, 10);
                    skill.TryAdd(12022, 10);
                    skill.TryAdd(12023, 10);
                    skill.TryAdd(12024, 5);
                    skill.TryAdd(12025, 5);
                    skill.TryAdd(12026, 10);
                    skill.TryAdd(10027, 10);
                    skill.TryAdd(10028, 10);
                    skill.TryAdd(10029, 10);
                    skill.TryAdd(10030, 10);
                    skill.TryAdd(10031, 10);
                    skill.TryAdd(10032, 10);
                    skill.TryAdd(10033, 5);
                    skill.TryAdd(10034, 10);
                    skill.TryAdd(11027, 5);
                    skill.TryAdd(11028, 5);
                    skill.TryAdd(11029, 10);
                    skill.TryAdd(11030, 10);
                    skill.TryAdd(11031, 1);
                    skill.TryAdd(11032, 5);
                    skill.TryAdd(11033, 10);
                    skill.TryAdd(11034, 10);
                    skill.TryAdd(12027, 10);
                    skill.TryAdd(12028, 10);
                    skill.TryAdd(12029, 10);
                    skill.TryAdd(12030, 10);
                    skill.TryAdd(12031, 10);
                    skill.TryAdd(12032, 10);
                    skill.TryAdd(12033, 10);
                    skill.TryAdd(12034, 10);
                    break;
                case 3:
                    skill.TryAdd(11001, 10);
                    skill.TryAdd(11002, 10);
                    skill.TryAdd(11003, 10);
                    skill.TryAdd(11004, 10);
                    skill.TryAdd(11005, 10);
                    skill.TryAdd(11006, 10);
                    skill.TryAdd(11007, 10);
                    skill.TryAdd(11008, 10);
                    skill.TryAdd(11009, 10);
                    skill.TryAdd(11010, 10);
                    skill.TryAdd(11011, 10);
                    skill.TryAdd(11012, 1);
                    skill.TryAdd(11013, 10);
                    skill.TryAdd(11014, 10);
                    skill.TryAdd(11015, 1);
                    skill.TryAdd(12001, 10);
                    skill.TryAdd(12002, 10);
                    skill.TryAdd(12003, 10);
                    skill.TryAdd(12004, 10);
                    skill.TryAdd(12005, 10);
                    skill.TryAdd(12006, 10);
                    skill.TryAdd(12007, 10);
                    skill.TryAdd(12008, 10);
                    skill.TryAdd(12009, 10);
                    skill.TryAdd(12010, 10);
                    skill.TryAdd(12011, 10);
                    skill.TryAdd(12012, 10);
                    skill.TryAdd(12013, 10);
                    skill.TryAdd(12014, 10);
                    skill.TryAdd(12015, 10);
                    skill.TryAdd(13001, 10);
                    skill.TryAdd(13002, 5);
                    skill.TryAdd(13003, 5);
                    skill.TryAdd(13004, 10);
                    skill.TryAdd(13005, 5);
                    skill.TryAdd(13006, 10);
                    skill.TryAdd(13007, 10);
                    skill.TryAdd(13008, 1);
                    skill.TryAdd(13009, 10);
                    skill.TryAdd(13010, 10);
                    skill.TryAdd(13011, 5);
                    skill.TryAdd(13012, 5);
                    skill.TryAdd(13013, 10);
                    skill.TryAdd(13014, 10);
                    skill.TryAdd(11020, 10);
                    skill.TryAdd(11021, 10);
                    skill.TryAdd(11022, 10);
                    skill.TryAdd(11023, 10);
                    skill.TryAdd(11024, 5);
                    skill.TryAdd(11025, 1);
                    skill.TryAdd(11026, 10);
                    skill.TryAdd(12020, 10);
                    skill.TryAdd(12021, 10);
                    skill.TryAdd(12022, 10);
                    skill.TryAdd(12023, 10);
                    skill.TryAdd(12024, 5);
                    skill.TryAdd(12025, 5);
                    skill.TryAdd(12026, 10);
                    skill.TryAdd(13019, 10);
                    skill.TryAdd(13020, 5);
                    skill.TryAdd(13021, 5);
                    skill.TryAdd(13022, 10);
                    skill.TryAdd(13023, 10);
                    skill.TryAdd(13024, 10);
                    skill.TryAdd(13025, 5);
                    skill.TryAdd(11027, 5);
                    skill.TryAdd(11028, 5);
                    skill.TryAdd(11029, 10);
                    skill.TryAdd(11030, 10);
                    skill.TryAdd(11031, 1);
                    skill.TryAdd(11032, 5);
                    skill.TryAdd(11033, 10);
                    skill.TryAdd(11034, 10);
                    skill.TryAdd(12027, 10);
                    skill.TryAdd(12028, 10);
                    skill.TryAdd(12029, 10);
                    skill.TryAdd(12030, 10);
                    skill.TryAdd(12031, 10);
                    skill.TryAdd(12032, 10);
                    skill.TryAdd(12033, 10);
                    skill.TryAdd(12034, 10);
                    skill.TryAdd(13026, 10);
                    skill.TryAdd(13027, 10);
                    skill.TryAdd(13028, 10);
                    skill.TryAdd(13029, 10);
                    skill.TryAdd(13030, 5);
                    skill.TryAdd(13031, 10);
                    skill.TryAdd(13032, 5);
                    skill.TryAdd(13033, 10);
                    break;
                case 4:
                    skill.TryAdd(10001, 10);
                    skill.TryAdd(10002, 10);
                    skill.TryAdd(10003, 10);
                    skill.TryAdd(10004, 10);
                    skill.TryAdd(10005, 10);
                    skill.TryAdd(10006, 10);
                    skill.TryAdd(10007, 10);
                    skill.TryAdd(10008, 10);
                    skill.TryAdd(10009, 10);
                    skill.TryAdd(10010, 10);
                    skill.TryAdd(10011, 10);
                    skill.TryAdd(10012, 10);
                    skill.TryAdd(10013, 10);
                    skill.TryAdd(10014, 1);
                    skill.TryAdd(10015, 5);
                    skill.TryAdd(12001, 10);
                    skill.TryAdd(12002, 10);
                    skill.TryAdd(12003, 10);
                    skill.TryAdd(12004, 10);
                    skill.TryAdd(12005, 10);
                    skill.TryAdd(12006, 10);
                    skill.TryAdd(12007, 10);
                    skill.TryAdd(12008, 10);
                    skill.TryAdd(12009, 10);
                    skill.TryAdd(12010, 10);
                    skill.TryAdd(12011, 10);
                    skill.TryAdd(12012, 10);
                    skill.TryAdd(12013, 10);
                    skill.TryAdd(12014, 10);
                    skill.TryAdd(12015, 10);
                    skill.TryAdd(13001, 10);
                    skill.TryAdd(13002, 5);
                    skill.TryAdd(13003, 5);
                    skill.TryAdd(13004, 10);
                    skill.TryAdd(13005, 5);
                    skill.TryAdd(13006, 10);
                    skill.TryAdd(13007, 10);
                    skill.TryAdd(13008, 1);
                    skill.TryAdd(13009, 10);
                    skill.TryAdd(13010, 10);
                    skill.TryAdd(13011, 5);
                    skill.TryAdd(13012, 5);
                    skill.TryAdd(13013, 10);
                    skill.TryAdd(13014, 10);
                    skill.TryAdd(10020, 10);
                    skill.TryAdd(10021, 10);
                    skill.TryAdd(10022, 10);
                    skill.TryAdd(10023, 10);
                    skill.TryAdd(10024, 10);
                    skill.TryAdd(10025, 5);
                    skill.TryAdd(10026, 5);
                    skill.TryAdd(12020, 10);
                    skill.TryAdd(12021, 10);
                    skill.TryAdd(12022, 10);
                    skill.TryAdd(12023, 10);
                    skill.TryAdd(12024, 5);
                    skill.TryAdd(12025, 5);
                    skill.TryAdd(12026, 10);
                    skill.TryAdd(13019, 10);
                    skill.TryAdd(13020, 5);
                    skill.TryAdd(13021, 5);
                    skill.TryAdd(13022, 10);
                    skill.TryAdd(13023, 10);
                    skill.TryAdd(13024, 10);
                    skill.TryAdd(13025, 5);
                    skill.TryAdd(10027, 10);
                    skill.TryAdd(10028, 10);
                    skill.TryAdd(10029, 10);
                    skill.TryAdd(10030, 10);
                    skill.TryAdd(10031, 10);
                    skill.TryAdd(10032, 10);
                    skill.TryAdd(10033, 5);
                    skill.TryAdd(10034, 10);
                    skill.TryAdd(12027, 10);
                    skill.TryAdd(12028, 10);
                    skill.TryAdd(12029, 10);
                    skill.TryAdd(12030, 10);
                    skill.TryAdd(12031, 10);
                    skill.TryAdd(12032, 10);
                    skill.TryAdd(12033, 10);
                    skill.TryAdd(12034, 10);
                    skill.TryAdd(13026, 10);
                    skill.TryAdd(13027, 10);
                    skill.TryAdd(13028, 10);
                    skill.TryAdd(13029, 10);
                    skill.TryAdd(13030, 5);
                    skill.TryAdd(13031, 10);
                    skill.TryAdd(13032, 5);
                    skill.TryAdd(13033, 10);
                    break;
                default:
                    break;
            }
        }

        public void setBalllisRb2()
        {
            for (int i = 0; i < 12; i++)
                if (ballList[i] != true)
                    ballList[i] = true;
        }

        public bool chacksoltEquip(ushort val)
        {
            bool res = false;
            int eq = 0;
            switch (val)
            {
                case 7:
                    for (int k = 0; k < equipment.Length; k++)
                        if (equipment[k] == null)
                            eq++;
                    if (eq != equipment.Length)
                    {
                        res = true;
                    }
                    return res;
                case 8:
                    for (int k = 0; k < equipment.Length; k++)
                        if (equipment[k] == null)
                            eq++;
                    if (eq == equipment.Length)
                    {
                        res = true;
                    }
                    return res;
                default:
                    return res;
            }
        }

        public bool chackItemEquip(ushort itemid)
        {
            bool res = false;
            for (int k = 0; k < equipment.Length; k++)
                if (equipment[k] != null)
                    if (equipment[k].Itemid == itemid)
                    {
                        res = true;
                    }
            return res;
        }

        public int chacksoltitem()
        {
            int res = -1;
            for (int k = 0; k < inventory.items.Length; k++)
                if (inventory.items[k] == null)
                {
                    res = 3;
                    break;
                }
            return res;
        }

        public int chacksoltpet()
        {
            int res = -1;
            for (int i = 0; i < 4; i++)
            {
                if (pet[i] == null)
                {
                    res = 2;
                    break;
                }
            }
            return res;
        }

        public int chackparty()
        {
            int res = 1;
            if (party != null)
            {
                res = party.member.Count;
            }
            return res;
        }

        public bool chackMemLvInparty(ushort val1, ushort sing, ushort val2)
        {
            bool res = false;
            if (party != null)
            {
                for (int i = 0; i < party.member.Count; i++)
                {
                    bool chack = client.checkCompare(party.member[i].level, sing, val2);
                    if (chack)
                    {
                        res = true;
                    }
                    else
                    {
                        res = false;
                        break;
                    }
                }
            }
            else
            {
                res = true;
            }
            return res;
        }

        public int chackgold()
        {
            int res = -1;
            if (gold > 0)
            {
                res = (int)gold;
            }
            return res;
        }

        public int chackLv()
        {
            int res = -1;
            if (level > 0)
            {
                res = level;
            }
            return res;
        }

        public int chackElement()
        {
            return element;
        }

        public bool chackball()
        {
            bool res = false;
            if (allball[0] < 12)
            {
                res = true;
            }
            return res;
        }

        public bool chackIntBall()
        {
            bool res = false;
            if (allball[1] < 12)
            {
                res = true;
            }
            return res;
        }

        public bool chackAtkBall()
        {
            bool res = false;
            if (allball[2] < 12)
            {
                res = true;
            }
            return res;
        }

        public int chackPetInchar(int id)
        {
            int npcid = -1;
            for (int sl = 0; sl < 4; sl++)
                if (pet[sl] != null)
                {
                    if (pet[sl].NPCid == id)
                    {
                        npcid = pet[sl].NPCid;
                    }
                }
            return npcid;
        }

        public int chackPetIncar(int id)
        {
            int npcid = -1;
            for (int sl = 10; sl < 14; sl++)
                if (pet[sl] != null)
                {
                    if (pet[sl].NPCid == id)
                    {
                        npcid = pet[sl].NPCid;
                    }
                }
            return npcid;
        }

        public int chackPetInhotel(int id)
        {
            int npcid = -1;
            for (int sl = 4; sl < 10; sl++)
                if (pet[sl] != null)
                {
                    if (pet[sl].NPCid == id)
                    {
                        npcid = pet[sl].NPCid;
                    }
                }
            return npcid;
        }

        public void sleep()
        {
            reply(new PacketCreator(0x1f, 0xa).send());
            setHp(getHpMax());
            refresh(hp, TSConstants._HP);
            setSp(getSpMax());
            refresh(sp, TSConstants._SP);
            for (int i = 0; i < 4; i++)
                if (pet[i] != null)
                {
                    pet[i].setHp(pet[i].getHpMax());
                    pet[i].refresh(pet[i].hp, TSConstants._HP);
                    pet[i].setSp(pet[i].getSpMax());
                    pet[i].refresh(pet[i].sp, TSConstants._SP);
                }
            client.reply(new PacketCreator(new byte[] { 0x1f, 1, 0 }).send());
        }

        public void Sendpacket(string packet)
        {
            try
            {
                byte[] array = PacketCreator.modifiyB2(PacketCreator.modifiyB3(packet));
                if (client.getSocket().Connected)
                {
                    client.getSocket().Send(array, 0, array.Length, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                WriteLog.Error("Sendpacket : " + e);
            }
        }

        public void addSummonSkill(byte level)
        {
            if (skill.ContainsKey(14026))
            {
                return;
            }
            skill.TryAdd(14026, level);
        }

        public void addSummonSkill()
        {
            if (skill.ContainsKey(14026))
            {
                switch (element)
                {
                    case 1:
                        skill.TryAdd(10016, skill[14026]);
                        refreshFull(0x6e, skill[14026], 10016);
                        break;
                    case 2:
                        skill.TryAdd(11016, skill[14026]);
                        refreshFull(0x6e, skill[14026], 11016);
                        break;
                    case 3:
                        skill.TryAdd(12016, skill[14026]);
                        refreshFull(0x6e, skill[14026], 12016);
                        break;
                    case 4:
                        skill.TryAdd(13015, skill[14026]);
                        refreshFull(0x6e, skill[14026], 13015);
                        break;
                    default:
                        break;
                }
            }
        }

        public void changLost_ItemEquip(int slot, ushort newitem)
        {
            equipment[5].Itemid = newitem;
            equipment[5].level = 0;
            sendEquip();
        }

        public void removeSummonSkill()
        {
            if (skill.ContainsKey(14026))
            {
                byte value;
                switch (element)
                {
                    case 1:
                        skill.TryRemove(10016, out value);
                        refreshFull(0x6e, 0, 10016);
                        break;
                    case 2:
                        skill.TryRemove(11016, out value);
                        refreshFull(0x6e, 0, 11016);
                        break;
                    case 3:
                        skill.TryRemove(12016, out value);
                        refreshFull(0x6e, 0, 12016);
                        break;
                    case 4:
                        skill.TryRemove(13015, out value);
                        refreshFull(0x6e, 0, 13015);
                        break;
                    default:
                        break;
                }
            }
        }

        public void timer_ElapsedLook(object sedr, EventArgs e)
        {
            PacketCreator p = new PacketCreator();
            uint battlePlayerId = client.targetIDBt;
            ushort ground = 105;
            if (TSServer.listPlayers.ContainsKey(battlePlayerId))
            {
                TSClient clientTarget = TSServer.getInstance().getPlayerById(battlePlayerId);
                if (clientTarget.battle != null && clientTarget.battle.look)
                {
                    clientTarget.battle.look = false;
                    TSCharacter c = client.getChar();
                    BattleAbstract battle = clientTarget.battle;
                    client.getChar().streamBattleId = battlePlayerId;
                    battle.streamers.Add(client.accID);
                    BattleGroundData.battleGroundList.TryGetValue(client.map.mapid, out BattleGroundInfo grounds);
                    ground = grounds.ground > 0 ? grounds.ground : ground;
                    p = new PacketCreator(0x0b, 0xfa);
                    p.add16(ground);
                    p.addBytes(new byte[] { 4, 2 });
                    p.add32(client.accID);
                    p.addZero(6);
                    p.addBytes(new byte[] { 0xff, 0xff });
                    p.add16((ushort)c.hp_max);
                    p.add16((ushort)c.sp_max);
                    p.add16((ushort)c.hp);
                    p.add16((ushort)c.sp);
                    p.addByte(c.level);
                    p.addByte(c.element);
                    List<byte[]> lpb = new List<byte[]>();
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 5; j++)
                        {
                            if (battle.position[i][j].exist)
                            {
                                lpb.Add(battle.position[i][j].announce(battle.battle_type, battle.countAlly).getData());
                            }
                        }
                    lpb.ForEach(x => p.addBytes(x));
                    client.reply(p.send());
                    if (c.party != null)
                    {
                        for (int i = 0; i < c.party.member.Count; i++)
                        {
                            if (client.accID != c.party.member[i].client.accID)
                            {
                                TSClient Pclient = c.party.member[i].client;
                                TSCharacter Pc = Pclient.getChar();
                                Pc.streamBattleId = battlePlayerId;
                                battle.streamers.Add(Pclient.accID);
                                BattleGroundData.battleGroundList.TryGetValue(Pclient.map.mapid, out BattleGroundInfo grounds1);
                                ground = grounds1.ground > 0 ? grounds1.ground : ground;
                                p = new PacketCreator(0x0b, 0xfa);
                                p.add16(ground);
                                p.addBytes(new byte[] { 4, 2 });
                                p.add32(Pclient.accID);
                                p.addZero(6);
                                p.addBytes(new byte[] { 0xff, 0xff });
                                p.add16((ushort)Pc.hp_max);
                                p.add16((ushort)Pc.sp_max);
                                p.add16((ushort)Pc.hp);
                                p.add16((ushort)Pc.sp);
                                p.addByte(Pc.level);
                                p.addByte(Pc.element);
                                lpb.ForEach(x => p.addBytes(x));
                                Pclient.reply(p.send());
                            }
                        }
                    }
                }
            }
        }

        public void sendPetFromCharToHotel(byte slot)
        {
            try
            {
                if (pet_battle == (byte)(slot - 1))
                {
                    client.reply(new PacketCreator(0x1f, 0x09).send());
                    announce("ไม่สามารถเก็บขุนพลที่กำลังออกรบได้");
                    return;
                }
                int next_hotel_pet = 4;
                while (next_hotel_pet < 10)
                    if (pet[next_hotel_pet] != null)
                        next_hotel_pet++;
                    else break;
                if (next_hotel_pet < 10)
                {
                    byte slotHotel = (byte)(next_hotel_pet + 1);
                    TSMysqlConnection c = new TSMysqlConnection();
                    try
                    {
                        c.connection.Open();
                        pet[next_hotel_pet] = pet[slot - 1];
                        pet[next_hotel_pet].slot = slotHotel;
                        pet[slot - 1] = null;
                        pet[next_hotel_pet].savePetDB(c.connection, false);
                    }
                    catch (Exception e)
                    {
                        WriteLog.ErrorDB("sendPetFromCharToHotel " + e);
                    }
                    finally
                    {
                        c.connection.Close();
                    }
                    PacketCreator pPetHotel = new PacketCreator(0x1f, 0x06);
                    pPetHotel.addBytes(pet[next_hotel_pet].sendRestInfo("HOTEL"));
                    reply(pPetHotel.send());
                    PacketCreator p = new PacketCreator(0xf, 2);
                    p.add32(client.accID);
                    p.add8(slot);
                    reply(p.send());
                    nextPet();
                }
                reply(new PacketCreator(0x1f, 0x09).send());
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> sendPetFromCharToHotel = " + e);
                client.disconnect();
            }
        }

        public void sendPetFromHotelToChar(byte slot)
        {
            try
            {
                if (next_pet < 4)
                {
                    byte slotPlayer = (byte)(next_pet + 1);
                    TSMysqlConnection c = new TSMysqlConnection();
                    try
                    {
                        c.connection.Open();
                        pet[next_pet] = pet[slot + 3];
                        pet[next_pet].slot = slotPlayer;
                        pet[slot + 3] = null;
                        pet[next_pet].savePetDB(c.connection, false);
                    }
                    catch (Exception e)
                    {
                        WriteLog.ErrorDB("sendPetFromHotelToChar " + e);
                    }
                    finally
                    {
                        c.connection.Close();
                    }
                    reply(new PacketCreator(new byte[] { 0x1f, 0x04, slot }).send());
                    var p1 = new PacketCreator(0x0f, 8);
                    p1.addBytes(pet[next_pet].sendInfo());
                    reply(p1.send());
                    nextPet();
                }
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> sendPetFromHotelToChar = " + e);
                client.disconnect();
            }
            reply(new PacketCreator(0x1f, 0x0c).send());
        }

        public void switchPetFromCharAndHotel(byte slotHotel, byte slotChar)
        {
            try
            {
                if (pet_battle == (byte)(slotChar - 1))
                {
                    reply(new PacketCreator(0x1f, 0x09).send());
                    reply(new PacketCreator(0x1f, 0x0c).send());
                    announce("ไม่สามารถเก็บขุนพลที่กำลังออกรบได้");
                    return;
                }
                TSPet petHotelClone = pet[slotHotel + 3].clone();
                TSPet petCharClone = pet[slotChar - 1].clone();
                petHotelClone.slot = slotChar;
                petCharClone.slot = (byte)(slotHotel + 4);
                pet[slotChar - 1] = petHotelClone;
                pet[slotHotel + 3] = petCharClone;
                TSMysqlConnection c = new TSMysqlConnection();
                try
                {
                    c.connection.Open();
                    pet[slotChar - 1].savePetDB(c.connection, false);
                    pet[slotHotel + 3].savePetDB(c.connection, false);
                }
                catch (Exception e)
                {
                    WriteLog.ErrorDB("switchPetFromCharAndHotel " + e);
                }
                finally
                {
                    c.connection.Close();
                }
                PacketCreator pPetHotel = new PacketCreator(0x1f, 0x06);
                pPetHotel.addBytes(pet[slotHotel + 3].sendRestInfo("HOTEL"));
                reply(pPetHotel.send());
                PacketCreator p = new PacketCreator(0x0f, 2);
                p.add32(client.accID);
                p.addByte(slotChar);
                reply(p.send());
                var p2 = new PacketCreator(0x0f, 8);
                p2.addBytes(pet[slotChar - 1].sendInfo());
                reply(p2.send());
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> switchPetFromCharAndHotel = " + e);
                client.disconnect();
            }
            client.reply(new PacketCreator(new byte[] { 0x1f, 0x0c, 0x09 }).send());
        }

        public void sendPetFromCharToHorse(byte slot)
        {
            try
            {
                if (pet_battle == (byte)(slot - 1))
                {
                    announce("ไม่สามารถเก็บขุนพลที่กำลังออกรบได้");
                    return;
                }
                int next_horse_pet = 10;
                while (next_horse_pet < 14)
                    if (pet[next_horse_pet] != null)
                        next_horse_pet++;
                    else break;
                if (next_horse_pet < 14)
                {
                    byte slotHorse = (byte)(next_horse_pet + 1);
                    TSMysqlConnection c = new TSMysqlConnection();
                    try
                    {
                        c.connection.Open();
                        pet[next_horse_pet] = pet[slot - 1];
                        pet[next_horse_pet].slot = slotHorse;
                        pet[slot - 1] = null;
                        pet[next_horse_pet].savePetDB(c.connection, false);
                    }
                    catch (Exception e)
                    {
                        WriteLog.ErrorDB("sendPetFromCharToHorse " + e);
                    }
                    finally
                    {
                        c.connection.Close();
                    }
                    PacketCreator pPetHorse = new PacketCreator(0x0f, 0x0a);
                    pPetHorse.addBytes(pet[next_horse_pet].sendRestInfo("HORSE"));
                    reply(pPetHorse.send());
                    PacketCreator p = new PacketCreator(0xf, 2);
                    p.add32(client.accID);
                    p.add8(slot);
                    reply(p.send());
                    nextPet();
                }
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> sendPetFromCharToHorse = " + e);
                client.disconnect();
            }
        }

        public void sendPetFromHorseToChar(byte slot)
        {
            try
            {
                if (next_pet < 4)
                {
                    byte slotPlayer = (byte)(next_pet + 1);
                    TSMysqlConnection c = new TSMysqlConnection();
                    try
                    {
                        c.connection.Open();
                        pet[next_pet] = pet[slot + 9];
                        pet[next_pet].slot = slotPlayer;
                        pet[slot + 9] = null;
                        pet[next_pet].savePetDB(c.connection, false);
                    }
                    catch (Exception e)
                    {
                        WriteLog.ErrorDB("sendPetFromHorseToChar " + e);
                    }
                    finally
                    {
                        c.connection.Close();
                    }
                    reply(new PacketCreator(new byte[] { 0xf, 0x0b, slot }).send());
                    var p1 = new PacketCreator(0x0f, 8);
                    p1.addBytes(pet[next_pet].sendInfo());
                    reply(p1.send());
                    nextPet();
                }
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> sendPetFromHotelToChar = " + e);
                client.disconnect();
            }
        }

        public void switchPetFromCharAndHorse(byte slotChar, byte slotHorse)
        {
            try
            {
                if (pet_battle == (byte)(slotChar - 1))
                {
                    announce("ไม่สามารถเก็บขุนพลที่กำลังออกรบได้");
                    return;
                }
                TSPet petHorseClone = pet[slotHorse + 9].clone();
                TSPet petCharClone = pet[slotChar - 1].clone();
                petHorseClone.slot = slotChar;
                petCharClone.slot = (byte)(slotHorse + 10);
                pet[slotChar - 1] = petHorseClone;
                pet[slotHorse + 9] = petCharClone;
                TSMysqlConnection c = new TSMysqlConnection();
                try
                {
                    c.connection.Open();
                    pet[slotChar - 1].savePetDB(c.connection, false);
                    pet[slotHorse + 9].savePetDB(c.connection, false);
                }
                catch (Exception e)
                {
                    WriteLog.ErrorDB("switchPetFromCharAndHorse " + e);
                }
                finally
                {
                    c.connection.Close();
                }
                PacketCreator pPetHorse = new PacketCreator(0x0f, 0x0a);
                pPetHorse.addBytes(pet[slotHorse + 9].sendRestInfo("HORSE"));
                reply(pPetHorse.send());
                PacketCreator p = new PacketCreator(0x0f, 2);
                p.add32(client.accID);
                p.addByte(slotChar);
                reply(p.send());
                var p2 = new PacketCreator(0x0f, 8);
                p2.addBytes(pet[slotChar - 1].sendInfo());
                reply(p2.send());
            }
            catch (Exception e)
            {
                WriteLog.Error(client.accID + " :: TSCharector >> switchPetFromCharAndHotel = " + e);
                client.disconnect();
            }
        }

        public bool transferItem(TSClient clientTarget, int mySlot, int amount, bool removeFromOwner)
        {
            bool ret = false;
            try
            {
                TSItem itemCloned = inventory.items[mySlot - 1]?.clone();
                TSItemContainer inven = clientTarget.getChar().inventory;
                int next_item = inven.next_item;
                int qtyBalance = ((int)itemCloned.quantity) - amount;
                if (next_item < 25)
                {
                    inven.items[next_item] = new TSItem(clientTarget.getChar().inventory, inventory.items[mySlot - 1].Itemid, (byte)(next_item + 1), (byte)amount);
                    inven.items[next_item].slot = (byte)(next_item + 1);
                    inven.items[next_item].Itemid = inventory.items[mySlot - 1].Itemid;
                    inven.items[next_item].quantity = (byte)amount;
                    if (ItemData.itemList[inventory.items[mySlot - 1].Itemid].equippos > 0)
                    {
                        inven.items[next_item].equip = new TSEquipment(clientTarget.getChar().inventory.items[next_item].container, inventory.items[mySlot - 1].Itemid, (byte)(next_item + 1), (byte)1);
                        inven.items[next_item].equip.duration = inventory.items[mySlot - 1].equip.duration;
                        inven.items[next_item].equip.elem_type = inventory.items[mySlot - 1].equip.elem_type;
                        inven.items[next_item].equip.elem_val = inventory.items[mySlot - 1].equip.elem_val;
                        inven.items[next_item].equip.other_type = inventory.items[mySlot - 1].equip.other_type;
                        inven.items[next_item].equip.other_val = inventory.items[mySlot - 1].equip.other_val;
                        inven.items[next_item].equip.anti = inventory.items[mySlot - 1].equip.anti;
                        inven.items[next_item].equip.exp = inventory.items[mySlot - 1].equip.exp;
                        inven.items[next_item].equip.uescout = inventory.items[mySlot - 1].equip.uescout;
                    }
                    inven.items[next_item].sendSlotItem(clientTarget, inven.items[next_item].slot);
                    if (removeFromOwner)
                    {
                        if (qtyBalance <= 0)
                        {
                            inventory.destroyItem((byte)mySlot);
                        }
                        else
                        {
                            inventory.items[mySlot - 1].quantity = (byte)qtyBalance;
                        }
                        reply(new PacketCreator(new byte[] { 0x17, 9, (byte)mySlot, (byte)amount }).send());
                    }
                    inven.owner.reply(new PacketCreator(0x17, 0xf).send());
                    inven.nextSlot();
                    ret = true;
                }
            }
            catch (Exception e)
            {
                WriteLog.Error("TSCharecter > transferItems " + client.accID + " " + e.Message);
                return false;
            }
            return ret;
        }

        public void transferGold(TSClient clientTarget, uint myGold, bool removeFromOwner)
        {
            TSCharacter charTo = clientTarget.getChar();
            charTo.gold += myGold;
            if (charTo.gold > 9999999) charTo.gold = 9999999;
            charTo.sendGold();
            if (removeFromOwner)
            {
                gold -= myGold;
                if (gold < 0) gold = 0;
                sendGold();
            }
        }

        public int[] transferPet(TSClient clientTarget, byte mySlot)
        {
            int[] result = new int[3] { 0, 9, 9 };
            TSCharacter chrTo = clientTarget.getChar();
            int target_next_pet = chrTo.next_pet;
            if (target_next_pet >= 4)
            {
                result[1] = 10;
                result[2] = 10;
                return result;
            }
            TSPet myPet = pet[mySlot - 1];
            for (int i = 0; i < chrTo.pet.Length; i++)
            {
                TSPet pet1 = chrTo.pet[i];
                if (pet1 != null && pet1.NPCid == myPet.NPCid)
                {
                    result[1] = 7;
                    result[2] = 7;
                    return result;
                }
            }
            myPet.owner = chrTo;
            myPet.slot = (byte)(target_next_pet + 1);
            chrTo.pet[target_next_pet] = myPet;
            chrTo.sendPetInfo();
            TSMysqlConnection c = new TSMysqlConnection();
            try
            {
                c.connection.Open();
                chrTo.pet[target_next_pet].savePetDB(c.connection, false);
                pet[mySlot - 1] = null;
            }
            catch (Exception e)
            {
                WriteLog.ErrorDB("transferPet " + e);
            }
            finally
            {
                c.connection.Close();
            }
            result[0] = 1;
            result[1] = 4;
            result[2] = 4;
            chrTo.nextPet();
            nextPet();
            PacketCreator p = new PacketCreator(0xf, 2);
            p.add32(client.accID);
            p.add8(mySlot);
            replyToMap(p.send(), true);
            sendTeam();
            return result;
        }

        public int getNullInventoryCount()
        {
            int ret = 0;
            for (int i = 0; i < 25; i++)
                if (inventory.items[i] == null)
                    ret++;
            return ret;
        }

        public void tradingItemsClear()
        {
            myTradeItemsTraderId = 0;
            myTradeItemsGold = 0;
            myTradeItemsAccept = false;
            myTradeItemsRegisterSlots = null;
        }

        public void tradingPetClear()
        {
            myTradePetTraderId = 0;
            myTradePetGold = 0;
            myTradePetAccept = false;
            myTradePetRegisterSlot = 0;
        }

        public byte[] getMyShopLabel()
        {
            if (myShopName != null)
            {
                int shopNameLen = myShopName.Length;
                PacketCreator pShop = new PacketCreator(0x17, 0x1f);
                pShop.add32(client.accID);
                pShop.addByte((byte)shopNameLen);
                pShop.addString(myShopName);
                pShop.addByte((byte)myShopImage);
                return pShop.send();
            }
            return null;
        }

        public void sendOtherPlayerDoing(TSClient clientOther)
        {
            if (clientOther.battle != null && clientOther.accID != client.accID) clientOther.map.announceBattle(clientOther);
            if (client.battle != null && client.accID != clientOther.accID) client.map.announceBattle(client);
            byte[] cShop = clientOther.getChar().getMyShopLabel();
            if (cShop != null) client.reply(cShop);
            if (clientOther.battle == null)
                client.map.ClearBattleSmoke(clientOther);
            if (client.battle == null)
                clientOther.map.ClearBattleSmoke(client);
            if (clientOther.accID != client.accID)
                GuildSystem.OnEnterMap(this, clientOther.getChar());
        }

        public void StopAllTimers()
        {
            timerautosave?.Stop();
            timerautosave?.Dispose();
            timer?.Stop();
            timer?.Dispose();
            timerAutospSub?.Stop();
            timerAutospSub?.Dispose();
            looktime?.Stop();
            looktime?.Dispose();
            combotime?.Stop();
            combotime?.Dispose();
            checkEndtime?.Stop();
            checkEndtime?.Dispose();
        }
    }
}
