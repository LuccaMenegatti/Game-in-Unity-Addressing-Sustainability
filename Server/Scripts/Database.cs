using AStarPathfinding;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Database
    {

        #region Main Data And Methods

        private const string _mysqlServer = "127.0.0.1";
        private const string _mysqlUsername = "root";
        private const string _mysqlPassword = "";
        private const string _mysqlDatabase = "eco_city";

        public static MySqlConnection GetMysqlConnection()
        {
            MySqlConnection connection = new MySqlConnection("SERVER=" + _mysqlServer + "; DATABASE=" + _mysqlDatabase + "; UID=" + _mysqlUsername + "; PWD=" + _mysqlPassword + "; POOLING=TRUE; CHARSET=UTF8");
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            else if (connection.State != ConnectionState.Open)
            {
                connection.Close();
                connection.Open();
            }
            return connection;
        }

        private static DateTime collectTime = DateTime.Now;
        private static bool collecting = false;
        private static double collectPeriod = 5d;

        private static DateTime updateTime = DateTime.Now;
        private static bool updating = false;
        private static double updatePeriod = 0.1d;

        private static DateTime warUpdateTime = DateTime.Now;
        private static bool warUpdating = false;
        private static double warUpdatePeriod = 0.1d;

        private static DateTime warCheckTime = DateTime.Now;
        private static bool warCheckUpdating = false;
        private static double warCheckPeriod = 0.1d;

        private static DateTime obsticlesTime = DateTime.Now;
        private static bool obsticlesUpdating = false;
        private static double obsticlesPeriod = 86400d;

        public static void Update()
        {
            if (!collecting)
            {
                double deltaTime = (DateTime.Now - collectTime).TotalSeconds;
                if (deltaTime >= collectPeriod)
                {
                    collecting = true;
                    collectTime = DateTime.Now;
                    UpdateCollectabes(deltaTime);
                }
            }
            if (!updating)
            {
                double deltaTime = (DateTime.Now - updateTime).TotalSeconds;
                if (deltaTime >= updatePeriod)
                {
                    updating = true;
                    updateTime = DateTime.Now;
                    GeneralUpdate(deltaTime);
                }
            }
        }

        public static void Initialize()
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("UPDATE accounts SET is_online = 0, client_id = 0;");
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public async static void PlayerDisconnected(int id)
        {
            long account_id = Server.clients[id].account;
            if (account_id > 0)
            {
                await PlayerDisconnectedAsync(account_id);
            }
        }

        private async static Task<bool> PlayerDisconnectedAsync(long account_id)
        {
            Task<bool> task = Task.Run(() =>
            {
                return Retry.Do(() => _PlayerDisconnectedAsync(account_id), TimeSpan.FromSeconds(0.1), 100, false);
            });
            return await task;
        }

        private static bool _PlayerDisconnectedAsync(long account_id)
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("UPDATE accounts SET is_online = 0, client_id = 0 WHERE id = {0}", account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            return true;
        }

        #endregion

        #region Player

        public async static void AuthenticatePlayer(int id, string device, string password, string username)
        {
            Data.InitializationData auth = await AuthenticatePlayerAsync(id, device, password, username);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.AUTH);
            if (auth != null)
            {
                Server.clients[id].device = device;
                Server.clients[id].account = auth.accountID;
                auth.versions = Terminal.clientVersions;
                string authData = await Data.SerializeAsync<Data.InitializationData>(auth);
                byte[] authBytes = await Data.CompressAsync(authData);
                packet.Write(1);
                packet.Write(authBytes.Length);
                packet.Write(authBytes);
                int battles = 0;
                packet.Write(battles);
            }
            else
            {
                packet.Write(0);
            }
            Sender.TCP_Send(id, packet);
        }

        private async static Task<Data.InitializationData> AuthenticatePlayerAsync(int id, string device, string password, string username)
        {
            Task<Data.InitializationData> task = Task.Run(() =>
            {
                return Retry.Do(() => _AuthenticatePlayerAsync(id, device, password, username), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static Data.InitializationData _AuthenticatePlayerAsync(int id, string device, string password, string username)
        {
            Data.InitializationData initializationData = new Data.InitializationData();
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("SELECT id, password, is_online, client_id FROM accounts WHERE device_id = '{0}' AND password = '{1}'", device, password);
                bool found = false;
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                bool online = int.Parse(reader["is_online"].ToString()) > 0;
                                int online_id = int.Parse(reader["client_id"].ToString());
                                long _id = long.Parse(reader["id"].ToString());
                                if (online && Server.clients[online_id].account == _id)
                                {
                                    Server.clients[online_id].Disconnect();
                                }
                                initializationData.accountID = _id;
                                initializationData.password = password;
                                found = true;
                            }
                        }
                    }
                }
                if (found == false)
                {
                    query = String.Format("UPDATE accounts SET device_id = '' WHERE device_id = '{0}'", device);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    initializationData.password = Data.EncrypteToMD5(Tools.GenerateToken());
                    query = String.Format("INSERT INTO accounts (device_id, password, name) VALUES('{0}', '{1}', '{2}');", device, initializationData.password, username);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                        initializationData.accountID = command.LastInsertedId;
                    }

                    int thX = 10; int thY = 10;
                    int gsX = 16; int gsY = 10;
                    int esX = 5; int esY = 10;
                    int bhX = 11; int bhY = 6;

                    query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, track_time, x_war, y_war) VALUES('{0}', {1}, {2}, {3}, 1, NOW() - INTERVAL 1 HOUR, {4}, {5});",
                        Data.BuildingID.townhall.ToString(), initializationData.accountID, thX, thY, thX, thY);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }

                    query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, track_time, x_war, y_war) VALUES('{0}', {1}, {2}, {3}, 1, NOW() - INTERVAL 1 HOUR, {4}, {5});",
                        Data.BuildingID.goldstorage.ToString(), initializationData.accountID, gsX, gsY, gsX, gsY);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }

                    query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, track_time, x_war, y_war) VALUES('{0}', {1}, {2}, {3}, 1, NOW() - INTERVAL 1 HOUR, {4}, {5});",
                        Data.BuildingID.elixirstorage.ToString(), initializationData.accountID, esX, esY, esX, esY);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }


                    List<Vector2Int> occupiedPositions = new List<Vector2Int>();

                    occupiedPositions.Add(new Vector2Int(thX, thY));
                    occupiedPositions.Add(new Vector2Int(gsX, gsY));
                    occupiedPositions.Add(new Vector2Int(esX, esY));

                    Random rnd = new Random();
                    int treesCreated = 0;
                    int maxAttempts = 300;
                    int attempts = 0;

                    while (treesCreated < 28 && attempts < maxAttempts)
                    {
                        attempts++;

                        int rx = rnd.Next(1, 22);
                        int ry = rnd.Next(1, 22);

                        Vector2Int newPos = new Vector2Int(rx, ry);
                        bool positionIsBad = false;

                        if (Math.Sqrt(Math.Pow(rx - thX, 2) + Math.Pow(ry - thY, 2)) < 3.5) positionIsBad = true;
                        if (Math.Sqrt(Math.Pow(rx - gsX, 2) + Math.Pow(ry - gsY, 2)) < 2.5) positionIsBad = true;
                        if (Math.Sqrt(Math.Pow(rx - esX, 2) + Math.Pow(ry - esY, 2)) < 2.5) positionIsBad = true;

                        if (!positionIsBad)
                        {
                            foreach (var occupied in occupiedPositions)
                            {
                                if (Math.Sqrt(Math.Pow(rx - occupied.X, 2) + Math.Pow(ry - occupied.Y, 2)) < 2.0)
                                {
                                    positionIsBad = true;
                                    break;
                                }
                            }
                        }

                        if (positionIsBad) continue;

                        occupiedPositions.Add(newPos);

                        query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, track_time, x_war, y_war) VALUES('{0}', {1}, {2}, {3}, 1, NOW() - INTERVAL 1 HOUR, {4}, {5});",
                            Data.BuildingID.tree.ToString(), initializationData.accountID, rx, ry, rx, ry);

                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        treesCreated++;
                    }

                    AddResources(connection, initializationData.accountID, 1000, 0, 0, 250);
                }
                query = String.Format("UPDATE accounts SET is_online = 1, client_id = {0}, last_login = NOW() WHERE id = {1}", id, initializationData.accountID);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                initializationData.serverBuildings = GetServerBuildings(connection);
                connection.Close();
            }
            return initializationData;
        }

        public async static void SyncPlayerData(int id, string device)
        {
            long account_id = Server.clients[id].account;
            Data.Player player = await GetPlayerDataAsync(account_id);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.SYNC);
            if (player != null)
            {
                packet.Write(1);
                List<Data.Building> buildings = await GetBuildingsAsync(account_id);
                player.units = new List<Data.Unit>();
                player.spells = new List<Data.Spell>();
                player.buildings = buildings;
                string playerData = await Data.SerializeAsync<Data.Player>(player);
                byte[] playerBytes = await Data.CompressAsync(playerData);
                packet.Write(playerBytes.Length);
                packet.Write(playerBytes);
            }
            else
            {
                packet.Write(0);
            }
            Sender.TCP_Send(id, packet);
        }

        public async static void ChangePlayerName(int id, string name)
        {
            long account_id = Server.clients[id].account;
            if (account_id > 0)
            {
                int response = await ChangePlayerNameAsync(account_id, name);
                Packet packet = new Packet();
                packet.Write((int)Terminal.RequestsID.RENAME);
                packet.Write(response);
                Sender.TCP_Send(id, packet);
            }
        }

        private async static Task<int> ChangePlayerNameAsync(long id, string name)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _ChangePlayerNameAsync(id, name), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _ChangePlayerNameAsync(long id, string name)
        {
            int response = 0;
            if (!string.IsNullOrEmpty(name))
            {
                using (MySqlConnection connection = GetMysqlConnection())
                {
                    string query = String.Format("UPDATE accounts SET name = '{0}' WHERE id = {1};", name, id);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    response = 1;
                }
            }
            return response;
        }

        private async static Task<Data.Player> GetPlayerDataAsync(long id)
        {
            Task<Data.Player> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetPlayerDataAsync(id), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static Data.Player _GetPlayerDataAsync(long id)
        {
            Data.Player data = new Data.Player();
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("SELECT id, name, gems, trophies, banned, shield, level, xp, NOW() AS now_time, email, map_layout, shld_cldn_1, shld_cldn_2, shld_cldn_3 FROM accounts WHERE id = {0};", id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                data.id = id;
                                data.name = reader["name"].ToString();
                                data.email = reader["email"].ToString();
                                int.TryParse(reader["gems"].ToString(), out data.gems);
                                int.TryParse(reader["trophies"].ToString(), out data.trophies);
                                int ban = 1;
                                int.TryParse(reader["banned"].ToString(), out ban);
                                data.banned = ban > 0;
                                DateTime.TryParse(reader["now_time"].ToString(), out data.nowTime);
                                DateTime.TryParse(reader["shield"].ToString(), out data.shield);
                                DateTime.TryParse(reader["shld_cldn_1"].ToString(), out data.shield1);
                                DateTime.TryParse(reader["shld_cldn_2"].ToString(), out data.shield2);
                                DateTime.TryParse(reader["shld_cldn_3"].ToString(), out data.shield3);
                                int.TryParse(reader["level"].ToString(), out data.level);
                                int.TryParse(reader["xp"].ToString(), out data.xp);
                                int.TryParse(reader["map_layout"].ToString(), out data.layout);
                            }
                        }
                    }
                }
                connection.Close();
            }
            return data;
        }

        public async static void LogOut(int id, string device)
        {
            long account_id = Server.clients[id].account;
            int response = await LogOutAsync(account_id, device);
        }

        private async static Task<int> LogOutAsync(long account_id, string device)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _LogOutAsync(account_id, device), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _LogOutAsync(long account_id, string device)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("SELECT id FROM accounts WHERE id = {0} AND device_id = '{1}' AND is_online > 0;", account_id, device);
                bool found = false;
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            found = true;
                        }
                    }
                }
                List<int> clients = new List<int>();
                if (found)
                {
                    /*
                    query = String.Format("SELECT client_id FROM accounts WHERE device_id = '{0}' AND is_online > 0 AND id <> = {1};", device, account_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int id = 0;
                                    int.TryParse(reader["client_id"].ToString(), out id);
                                    clients.Add(id);
                                }
                            }
                        }
                    }
                    */
                    query = String.Format("UPDATE accounts SET device_id = '', is_online = 0 WHERE device_id = '{0}';", device);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    /*
                    for (int i = 0; i < clients.Count; i++)
                    {
                        // TODO -> Disconnect
                    }
                    */
                }
                connection.Close();
            }
            return response;
        }

        public async static void BuyShield(int id, int pack)
        {
            long account_id = Server.clients[id].account;
            int response = await BuyShieldAsync(account_id, pack);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BUYSHIELD);
            packet.Write(response);
            packet.Write(pack);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> BuyShieldAsync(long account_id, int pack)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _BuyShieldAsync(account_id, pack), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _BuyShieldAsync(long account_id, int pack)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int cooldown1 = 0;
                int cooldown2 = 0;
                int cooldown3 = 0;
                int gems = 0;
                string query = String.Format("SELECT gems, IF(shld_cldn_1 <= NOW(), 1, 0) AS cd1, IF(shld_cldn_2 <= NOW(), 1, 0) AS cd2, IF(shld_cldn_3 <= NOW(), 1, 0) AS cd3 FROM accounts WHERE id = {0};", account_id);
                bool ok = false;
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            ok = true;
                            while (reader.Read())
                            {
                                int.TryParse(reader["cd1"].ToString(), out cooldown1);
                                int.TryParse(reader["cd2"].ToString(), out cooldown2);
                                int.TryParse(reader["cd3"].ToString(), out cooldown3);
                                int.TryParse(reader["gems"].ToString(), out gems);
                            }
                        }
                    }
                }
                if (ok)
                {
                    int price = 99999;
                    switch (pack)
                    {
                        case 1: price = 10; break;
                        case 2: price = 100; break;
                        case 3: price = 250; break;
                    }
                    if (gems >= price)
                    {
                        if ((pack == 1 && cooldown1 == 1) || (pack == 2 && cooldown2 == 1) || (pack == 3 && cooldown1 == 3))
                        {
                            if (SpendResources(connection, account_id, 0, 0, price, 0))
                            {
                                switch (pack)
                                {
                                    case 1:
                                        query = String.Format("UPDATE accounts SET shld_cldn_1 = NOW() + INTERVAL 23 HOUR WHERE id = {0};", account_id);
                                        using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                                        AddShield(connection, account_id, 23 * 60 * 60);
                                        break;
                                    case 2:
                                        query = String.Format("UPDATE accounts SET shld_cldn_2 = NOW() + INTERVAL 5 DAY WHERE id = {0};", account_id);
                                        using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                                        AddShield(connection, account_id, 5 * 24 * 60 * 60);
                                        break;
                                    case 3:
                                        query = String.Format("UPDATE accounts SET shld_cldn_3 = NOW() + INTERVAL 35 DAY WHERE id = {0};", account_id);
                                        using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                                        AddShield(connection, account_id, 35 * 24 * 60 * 60);
                                        break;
                                }
                                response = 1;
                            }
                            else
                            {
                                response = 2;
                            }
                        }
                        else
                        {
                            response = 3;
                        }
                    }
                    else
                    {
                        response = 2;
                    }
                }
                connection.Close();
            }
            return response;
        }

        public async static void BuyGem(int id, int pack, string token, string product, string password, string package, string market)
        {
            long account_id = Server.clients[id].account;
            // var valid = await ValidateBazzarPurchaseAsync(package, product, token);
            int response = await BuyGemAsync(account_id, pack, token, password, product, package, false, "0", market);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BUYGEM);
            packet.Write(response);
            packet.Write(pack);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> BuyGemAsync(long account_id, int pack, string token, string password, string product, string package, bool valid, string price, string market)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _BuyGemAsync(account_id, pack, token, password, product, package, valid, price, market), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _BuyGemAsync(long account_id, int pack, string token, string password, string product, string package, bool valid, string price, string market)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("SELECT id FROM accounts WHERE id = {0} AND password = '{1}';", account_id, password);
                bool ok = false;
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            ok = true;
                        }
                    }
                }
                if (ok)
                {
                    query = String.Format("SELECT id FROM iap WHERE product_id = '{0}' AND token = '{1}';", product, token);
                    ok = true;
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                ok = false;
                            }
                        }
                    }
                    int gems = 0;
                    switch (pack)
                    {
                        case 1: gems = 100; break;
                        case 2: gems = 300; break;
                        case 3: gems = 1000; break;
                        case 4: gems = 2500; break;
                        case 5: gems = 6000; break;
                    }
                    if (gems > 0)
                    {
                        query = String.Format("INSERT INTO iap (account_id, market, product_id, token, price, currency, validated) VALUES({0}, '{1}', '{2}', '{3}', '{4}', 'IRR', {5});", account_id, market, product, token, price, valid ? 1 : 0);
                        using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                        query = String.Format("UPDATE accounts SET gems = gems + {0} WHERE id = {1};", gems, account_id);
                        using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                        response = 1;
                    }
                    else
                    {
                        response = 2;
                    }
                }
                connection.Close();
            }
            return response;
        }

        private static readonly string bazzarClientID = "FEmqTV6tmWGbBIqAzvuEOy7V504EX2T5c5GYfzzT";
        private static readonly string bazzarClientSecret = "Dh2JvsJrDaaRtFovC0ASJ34kpopH8v4vM8KLOj4WZACDHFFzzzsT3WgaXllq";
        private static readonly string bazzarRefreshToken = "KLVfDElMUuAURX8WXdBJEEYQLtW8QA";

        [Serializable]
        private struct BazzarToken
        {
            public string access_token;
        }

        [Serializable]
        private struct BazzarValidateResult
        {
            public bool isConsumed;
            public bool isRefund;
            public string kind;
            public string payload;
            public string time;
        }

        private async static Task<(bool, string)> ValidateBazzarPurchaseAsync(string package, string product, string order)
        {
            try
            {
                var url = "https://pardakht.cafebazaar.ir/devapi/v2/auth/token/";
                var values = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", bazzarClientID },
                    { "client_secret", bazzarClientSecret },
                    { "refresh_token", bazzarRefreshToken }
                };
                var content = new FormUrlEncodedContent(values);
                using var client = new HttpClient();
                var post = await client.PostAsync(url, content);
                var responseString = await post.Content.ReadAsStreamAsync();
                BazzarToken bazzarToken = await JsonSerializer.DeserializeAsync<BazzarToken>(responseString);
                url = "https://pardakht.cafebazaar.ir/devapi/v2/api/validate/" + package + "/inapp/" + product + "/purchases/" + order + "/?access_token=" + bazzarToken.access_token;
                var get = await client.GetStreamAsync(url);
                BazzarValidateResult bazzarResult = await JsonSerializer.DeserializeAsync<BazzarValidateResult>(get);
                if (bazzarResult.isRefund)
                {
                    return (false, "0");
                }
                else
                {
                    return (true, bazzarResult.payload);
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace, "IAP");
                return (false, "0");
            }
        }

        public async static void BuyGold(int id, int pack)
        {
            long account_id = Server.clients[id].account;
            int response = await BuyGoldAsync(account_id, pack);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BUYSHIELD);
            packet.Write(response);
            packet.Write(pack);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> BuyGoldAsync(long account_id, int pack)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _BuyGoldAsync(account_id, pack), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _BuyGoldAsync(long account_id, int pack)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {


                connection.Close();
            }
            return response;
        }

        public async static void GetPlayersRanking(int id, int page)
        {
            long account_id = Server.clients[id].account;
            Data.PlayersRanking response = await GetPlayersRankingAsync(page, account_id);
            string rawData = await Data.SerializeAsync<Data.PlayersRanking>(response);
            byte[] bytes = await Data.CompressAsync(rawData);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.PLAYERSRANK);
            packet.Write(bytes.Length);
            packet.Write(bytes);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<Data.PlayersRanking> GetPlayersRankingAsync(int page, long account_id = 0)
        {
            Task<Data.PlayersRanking> task = Task.Run(() =>
            {
                Data.PlayersRanking response = new Data.PlayersRanking();
                response = Retry.Do(() => _GetPlayersRankingAsync(page, account_id), TimeSpan.FromSeconds(0.1), 1, false);
                if (response == null)
                {
                    response = new Data.PlayersRanking();
                    response.players = new List<Data.PlayerRank>();
                }
                return response;
            });
            return await task;
        }

        private static int players_ranking_per_page = 30;

        private static Data.PlayersRanking _GetPlayersRankingAsync(int page, long account_id = 0)
        {
            Data.PlayersRanking response = new Data.PlayersRanking();
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int playersCount = 0;
                string query = "SELECT COUNT(*) AS count FROM accounts";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int.TryParse(reader["count"].ToString(), out playersCount);
                            }
                        }
                    }
                }

                response.pagesCount = Convert.ToInt32(Math.Ceiling((double)playersCount / (double)players_ranking_per_page));

                if (response.pagesCount > 0)
                {
                    if (page == 0 && account_id > 0)
                    {
                        page = 1;
                        int playerRank = GetPlayerRank(connection, account_id);
                        if (playerRank > 0)
                        {
                            page = Convert.ToInt32(Math.Ceiling((double)playerRank / (double)players_ranking_per_page));
                        }
                    }
                    else if (page <= 0)
                    {
                        page = 1;
                    }

                    response.page = page;

                    int start = ((page - 1) * players_ranking_per_page) + 1;
                    int end = start + players_ranking_per_page;

                    query = String.Format(
                        @"SELECT 
                            ranks.id, ranks.name, ranks.xp, ranks.level, ranks.trophies, ranks.rank, ranks.gold, ranks.elixir
                          FROM (
                            SELECT 
                                p_stats.id, p_stats.name, p_stats.xp, p_stats.level, p_stats.trophies, p_stats.gold, p_stats.elixir,
                                ROW_NUMBER() OVER(ORDER BY p_stats.elixir DESC, p_stats.gold DESC) AS 'rank'
                            FROM (
                                SELECT 
                                    a.id, a.name, a.xp, a.level, a.trophies,
                                    IFNULL(g.total_gold, 0) AS gold,
                                    IFNULL(e.total_elixir, 0) AS elixir
                                FROM accounts AS a
                                LEFT JOIN (
                                    SELECT account_id, SUM(gold_storage) AS total_gold 
                                    FROM buildings 
                                    WHERE global_id IN ('townhall', 'goldstorage') 
                                    GROUP BY account_id
                                ) AS g ON a.id = g.account_id
                                LEFT JOIN (
                                    SELECT account_id, SUM(elixir_storage) AS total_elixir 
                                    FROM buildings 
                                    WHERE global_id IN ('townhall', 'elixirstorage') 
                                    GROUP BY account_id
                                ) AS e ON a.id = e.account_id
                            ) AS p_stats
                          ) AS ranks
                          WHERE ranks.rank >= {0} AND ranks.rank < {1}",
                        start, end
                    );

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Data.PlayerRank player = new Data.PlayerRank();
                                    player.name = reader["name"].ToString();
                                    long.TryParse(reader["id"].ToString(), out player.id);
                                    int.TryParse(reader["rank"].ToString(), out player.rank);
                                    int.TryParse(reader["xp"].ToString(), out player.xp);
                                    int.TryParse(reader["level"].ToString(), out player.level);
                                    int.TryParse(reader["trophies"].ToString(), out player.trophies);
                                    int.TryParse(reader["gold"].ToString(), out player.gold);
                                    int.TryParse(reader["elixir"].ToString(), out player.elixir);
                                    response.players.Add(player);
                                }
                            }
                        }
                    }
                }
                connection.Close();
            }
            return response;
        }

        private static int GetPlayerRank(MySqlConnection connection, long account_id)
        {
            int rank = 0;
            string query = String.Format(
                @"SELECT rank
                  FROM (
                    SELECT 
                        p_stats.id,
                        ROW_NUMBER() OVER(ORDER BY p_stats.elixir DESC, p_stats.gold DESC) AS 'rank'
                    FROM (
                        SELECT 
                            a.id,
                            IFNULL(g.total_gold, 0) AS gold,
                            IFNULL(e.total_elixir, 0) AS elixir
                        FROM accounts AS a
                        LEFT JOIN (
                            SELECT account_id, SUM(gold_storage) AS total_gold 
                            FROM buildings 
                            WHERE global_id IN ('townhall', 'goldstorage') 
                            GROUP BY account_id
                        ) AS g ON a.id = g.account_id
                        LEFT JOIN (
                            SELECT account_id, SUM(elixir_storage) AS total_elixir 
                            FROM buildings 
                            WHERE global_id IN ('townhall', 'elixirstorage') 
                            GROUP BY account_id
                        ) AS e ON a.id = e.account_id
                    ) AS p_stats
                  ) AS ranks
                  WHERE id = {0}",
                account_id
            );

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int.TryParse(reader["rank"].ToString(), out rank);
                        }
                    }
                }
            }
            return rank;
        }

        #endregion

        #region Helpers

        private static int GetBuildingCount(long accountID, string globalID, MySqlConnection connection)
        {
            int count = 0;
            string query = String.Format("SELECT id FROM buildings WHERE account_id = {0} AND global_id = '{1}';", accountID, globalID);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        private static int GetBuildingConstructionCount(long accountID, MySqlConnection connection)
        {
            int count = 0;
            string query = String.Format("SELECT id FROM buildings WHERE account_id = {0} AND is_constructing > 0;", accountID);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        #endregion

        #region Resource Manager

        private static bool SpendResources(MySqlConnection connection, long account_id, int gold, int elixir, int gems, int darkElixir)
        {
            if (CheckResources(connection, account_id, gold, elixir, gems, darkElixir))
            {
                if (gold > 0 || elixir > 0 || darkElixir > 0)
                {
                    List<Data.Building> buildings = new List<Data.Building>();
                    string query = String.Format("SELECT id, global_id, gold_storage, elixir_storage, dark_elixir_storage FROM buildings WHERE account_id = {0} AND global_id IN('townhall', 'goldstorage', 'elixirstorage', 'darkelixirstorage');", account_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Data.Building building = new Data.Building();
                                    building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                                    building.goldStorage = (int)Math.Floor(float.Parse(reader["gold_storage"].ToString()));
                                    building.elixirStorage = (int)Math.Floor(float.Parse(reader["elixir_storage"].ToString()));
                                    building.darkStorage = (int)Math.Floor(float.Parse(reader["dark_elixir_storage"].ToString()));
                                    building.databaseID = long.Parse(reader["id"].ToString());
                                    buildings.Add(building);
                                }
                            }
                        }
                    }
                    if (buildings.Count > 0)
                    {
                        int spendGold = 0;
                        int spendElixir = 0;
                        int spendDarkElixir = 0;
                        for (int i = 0; i < buildings.Count; i++)
                        {
                            if (spendGold >= gold && spendElixir >= elixir && spendDarkElixir >= darkElixir)
                            {
                                break;
                            }
                            int toSpendGold = 0;
                            int toSpendElixir = 0;
                            int toSpendDark = 0;
                            switch (buildings[i].id)
                            {
                                case Data.BuildingID.townhall:
                                    if (spendGold < gold && buildings[i].goldStorage > 0)
                                    {
                                        if (buildings[i].goldStorage >= (gold - spendGold))
                                        {
                                            toSpendGold = gold - spendGold;
                                        }
                                        else
                                        {
                                            toSpendGold = buildings[i].goldStorage;
                                        }
                                        spendGold += toSpendGold;
                                    }
                                    if (spendElixir < elixir && buildings[i].elixirStorage > 0)
                                    {
                                        if (buildings[i].elixirStorage >= (elixir - spendElixir))
                                        {
                                            toSpendElixir = elixir - spendElixir;
                                        }
                                        else
                                        {
                                            toSpendElixir = buildings[i].elixirStorage;
                                        }
                                        spendElixir += toSpendElixir;
                                    }
                                    if (spendDarkElixir < darkElixir && buildings[i].darkStorage > 0)
                                    {
                                        if (buildings[i].darkStorage >= (darkElixir - spendDarkElixir))
                                        {
                                            toSpendDark = darkElixir - spendDarkElixir;
                                        }
                                        else
                                        {
                                            toSpendDark = buildings[i].darkStorage;
                                        }
                                        spendDarkElixir += toSpendDark;
                                    }
                                    break;
                                case Data.BuildingID.goldstorage:
                                    if (spendGold < gold && buildings[i].goldStorage > 0)
                                    {
                                        if (buildings[i].goldStorage >= (gold - spendGold))
                                        {
                                            toSpendGold = gold - spendGold;
                                        }
                                        else
                                        {
                                            toSpendGold = buildings[i].goldStorage;
                                        }
                                        spendGold += toSpendGold;
                                    }
                                    break;
                                case Data.BuildingID.elixirstorage:
                                    if (spendElixir < elixir && buildings[i].elixirStorage > 0)
                                    {
                                        if (buildings[i].elixirStorage >= (elixir - spendElixir))
                                        {
                                            toSpendElixir = elixir - spendElixir;
                                        }
                                        else
                                        {
                                            toSpendElixir = buildings[i].elixirStorage;
                                        }
                                        spendElixir += toSpendElixir;
                                    }
                                    break;
                                case Data.BuildingID.darkelixirstorage:
                                    if (spendDarkElixir < darkElixir && buildings[i].darkStorage > 0)
                                    {
                                        if (buildings[i].darkStorage >= (darkElixir - spendDarkElixir))
                                        {
                                            toSpendDark = darkElixir - spendDarkElixir;
                                        }
                                        else
                                        {
                                            toSpendDark = buildings[i].darkStorage;
                                        }
                                        spendDarkElixir += toSpendDark;
                                    }
                                    break;
                            }
                            query = String.Format("UPDATE buildings SET gold_storage = gold_storage - {0}, elixir_storage = elixir_storage - {1}, dark_elixir_storage = dark_elixir_storage - {2} WHERE id = {3};", toSpendGold, toSpendElixir, toSpendDark, buildings[i].databaseID);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        if (spendGold < gold || spendElixir < elixir || spendDarkElixir < darkElixir)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (gems > 0)
                {
                    string query = String.Format("UPDATE accounts SET gems = gems - {0} WHERE id = {1};", gems, account_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private static bool CheckResources(MySqlConnection connection, long account_id, int gold, int elixir, int gems, int darkElixir)
        {
            int haveGold = 0;
            int haveElixir = 0;
            int haveGems = 0;
            int haveDarkElixir = 0;

            if (gold > 0 || elixir > 0 || darkElixir > 0)
            {
                string query = String.Format("SELECT global_id, gold_storage, elixir_storage, dark_elixir_storage FROM buildings WHERE account_id = {0} AND global_id IN('townhall', 'goldstorage', 'elixirstorage', 'darkelixirstorage');", account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.BuildingID id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                                int gold_storage = (int)Math.Floor(float.Parse(reader["gold_storage"].ToString()));
                                int elixir_storage = (int)Math.Floor(float.Parse(reader["elixir_storage"].ToString()));
                                int dark_elixir_storage = (int)Math.Floor(float.Parse(reader["dark_elixir_storage"].ToString()));
                                switch (id)
                                {
                                    case Data.BuildingID.townhall:
                                        haveGold += gold_storage;
                                        haveElixir += elixir_storage;
                                        haveDarkElixir += dark_elixir_storage;
                                        break;
                                    case Data.BuildingID.goldstorage:
                                        haveGold += gold_storage;
                                        break;
                                    case Data.BuildingID.elixirstorage:
                                        haveElixir += elixir_storage;
                                        break;
                                    case Data.BuildingID.darkelixirstorage:
                                        haveDarkElixir += dark_elixir_storage;
                                        break;
                                }
                            }
                        }
                    }
                }
                bool missingGold = (gold > 0 && haveGold < gold);
                bool missingElixir = (elixir > 0 && haveElixir < elixir);
                bool missingDark = (darkElixir > 0 && haveDarkElixir < darkElixir);

                if (missingGold || missingElixir || missingDark)
                {
                    return false;
                }
            }

            if (gems > 0)
            {
                string query = String.Format("SELECT gems FROM accounts WHERE id = {0}", account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                haveGems = int.Parse(reader["gems"].ToString());
                            }
                        }
                    }
                }
                if (haveGems < gems)
                {
                    return false;
                }
            }

            return true;
        }

        private static (int, int, int, int) AddResources(MySqlConnection connection, long account_id, int gold, int elixir, int darkElixir, int gems)
        {
            int addedGold = 0;
            int addedElixir = 0;
            int addedDark = 0;

            if (gold > 0 || elixir > 0 || darkElixir > 0)
            {
                List<Data.Building> storages = new List<Data.Building>();

                string query = String.Format("SELECT buildings.id, buildings.global_id, buildings.gold_storage, buildings.elixir_storage, buildings.dark_elixir_storage, server_buildings.gold_capacity, server_buildings.elixir_capacity, server_buildings.dark_elixir_capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.account_id = {0} AND buildings.global_id IN('townhall', 'goldstorage', 'elixirstorage', 'darkelixirstorage') AND buildings.level > 0;", account_id);

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.Building building = new Data.Building();
                                long.TryParse(reader["id"].ToString(), out building.databaseID);

                                if (Enum.TryParse(typeof(Data.BuildingID), reader["global_id"].ToString(), out object parsedId))
                                {
                                    building.id = (Data.BuildingID)parsedId;
                                }

                                float tempVal = 0;
                                float.TryParse(reader["gold_storage"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tempVal);
                                building.goldStorage = (int)Math.Floor(tempVal);

                                float.TryParse(reader["elixir_storage"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tempVal);
                                building.elixirStorage = (int)Math.Floor(tempVal);

                                float.TryParse(reader["dark_elixir_storage"].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tempVal);
                                building.darkStorage = (int)Math.Floor(tempVal);

                                int cap = 0;
                                int.TryParse(reader["gold_capacity"].ToString(), out cap);
                                building.goldCapacity = cap;

                                int.TryParse(reader["elixir_capacity"].ToString(), out cap);
                                building.elixirCapacity = cap;

                                int.TryParse(reader["dark_elixir_capacity"].ToString(), out cap);
                                building.darkCapacity = cap;

                                storages.Add(building);
                            }
                        }
                    }
                }

                if (storages.Count > 0)
                {
                    int remainedGold = gold;
                    int remainedElixir = elixir;
                    int remainedDatk = darkElixir;

                    for (int i = 0; i < storages.Count; i++)
                    {
                        if (remainedGold <= 0 && remainedElixir <= 0 && remainedDatk <= 0) break;

                        int goldSpace = storages[i].goldCapacity - storages[i].goldStorage;
                        int elixirSpace = storages[i].elixirCapacity - storages[i].elixirStorage;
                        int darkSpace = storages[i].darkCapacity - storages[i].darkStorage;

                        if (goldSpace < 0) goldSpace = 0;
                        if (elixirSpace < 0) elixirSpace = 0;
                        if (darkSpace < 0) darkSpace = 0;

                        int addGold = 0;
                        int addElixir = 0;
                        int addDark = 0;

                        switch (storages[i].id)
                        {
                            case Data.BuildingID.townhall:
                                addGold = (goldSpace >= remainedGold) ? remainedGold : goldSpace;
                                addElixir = (elixirSpace >= remainedElixir) ? remainedElixir : elixirSpace;
                                addDark = (darkSpace >= remainedDatk) ? remainedDatk : darkSpace;
                                break;
                            case Data.BuildingID.goldstorage:
                                addGold = (goldSpace >= remainedGold) ? remainedGold : goldSpace;
                                break;
                            case Data.BuildingID.elixirstorage:
                                addElixir = (elixirSpace >= remainedElixir) ? remainedElixir : elixirSpace;
                                break;
                            case Data.BuildingID.darkelixirstorage:
                                addDark = (darkSpace >= remainedDatk) ? remainedDatk : darkSpace;
                                break;
                        }

                        query = String.Format("UPDATE buildings SET gold_storage = gold_storage + {0}, elixir_storage = elixir_storage + {1}, dark_elixir_storage = dark_elixir_storage + {2} WHERE id = {3};", addGold, addElixir, addDark, storages[i].databaseID);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        remainedGold -= addGold;
                        remainedElixir -= addElixir;
                        remainedDatk -= addDark;

                        addedGold += addGold;
                        addedElixir += addElixir;
                        addedDark += addDark;
                    }
                }
            }

            if (gems > 0)
            {
                string query = String.Format("UPDATE accounts SET gems = gems + {0} WHERE id = {1};", gems, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            return (addedGold, addedElixir, addedDark, gems);
        }

        private static void AddXP(MySqlConnection connection, long account_id, int xp)
        {
            int haveXp = 0;
            int level = 0;
            string query = String.Format("SELECT xp, level FROM accounts WHERE id = {0}", account_id);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int.TryParse(reader["xp"].ToString(), out haveXp);
                            int.TryParse(reader["level"].ToString(), out level);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            int reachedLevel = level;
            int reqXp = Data.GetNexLevelRequiredXp(reachedLevel);
            int remainedXp = haveXp + xp;
            while (remainedXp >= reqXp)
            {
                remainedXp -= reqXp;
                reachedLevel++;
                reqXp = Data.GetNexLevelRequiredXp(reachedLevel);
            }
            query = String.Format("UPDATE accounts SET level = {0}, xp = {1} WHERE id = {2} AND level = {3} AND xp = {4}", reachedLevel, remainedXp, account_id, level, haveXp);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void AddClanXP(MySqlConnection connection, long clan_id, int xp)
        {
            int haveXp = 0;
            int level = 0;
            string query = String.Format("SELECT xp, level FROM clans WHERE id = {0}", clan_id);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int.TryParse(reader["xp"].ToString(), out haveXp);
                            int.TryParse(reader["level"].ToString(), out level);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            int reachedLevel = level;
            int reqXp = Data.GetClanNexLevelRequiredXp(reachedLevel);
            int remainedXp = haveXp + xp;
            while (remainedXp >= reqXp)
            {
                remainedXp -= reqXp;
                reachedLevel++;
                reqXp = Data.GetClanNexLevelRequiredXp(reachedLevel);
            }
            query = String.Format("UPDATE clans SET level = {0}, xp = {1} WHERE id = {2} AND level = {3} AND xp = {4}", reachedLevel, remainedXp, clan_id, level, haveXp);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void ChangeClanTrophies(MySqlConnection connection, long clan_id, int amount)
        {
            if (amount == 0) { return; }
            if (amount > 0)
            {
                string query = String.Format("UPDATE clans SET trophies = trophies + {0} WHERE id = {1}", amount, clan_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                string query = String.Format("UPDATE clans SET trophies = trophies - {0} WHERE id = {1}", -amount, clan_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                query = String.Format("UPDATE clans SET trophies = 0 WHERE id = {0} AND trophies < 0", clan_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ChangeTrophies(MySqlConnection connection, long account_id, int amount)
        {
            if (amount == 0) { return; }
            if (amount > 0)
            {
                string query = String.Format("UPDATE accounts SET trophies = trophies + {0} WHERE id = {1}", amount, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                string query = String.Format("UPDATE accounts SET trophies = trophies - {0} WHERE id = {1}", -amount, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                query = String.Format("UPDATE accounts SET trophies = 0 WHERE id = {0} AND trophies < 0", account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private async static Task<bool> AddShieldAsync(long account_id, int seconds)
        {
            Task<bool> task = Task.Run(() =>
            {
                return Retry.Do(() => _AddShieldAsync(account_id, seconds), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static bool AddShield(MySqlConnection connection, long account_id, int seconds)
        {
            if (seconds <= 0) { return false; }
            bool haveShield = false;
            string query = String.Format("SELECT shield FROM accounts WHERE id = {0} AND shield > NOW()", account_id);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        haveShield = true;
                    }
                }
            }
            if (haveShield)
            {
                query = String.Format("UPDATE accounts SET shield = shield + INTERVAL {0} SECOND WHERE id = {1};", seconds, account_id);
            }
            else
            {
                query = String.Format("UPDATE accounts SET shield = NOW() + INTERVAL {0} SECOND WHERE id = {1};", seconds, account_id);
            }
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
            return true;
        }

        private static bool _AddShieldAsync(long account_id, int seconds)
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                return AddShield(connection, account_id, seconds);
            }
        }

        private async static Task<bool> RemoveShieldAsync(long account_id)
        {
            Task<bool> task = Task.Run(() =>
            {
                return Retry.Do(() => _RemoveShieldAsync(account_id), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static bool RemoveShield(MySqlConnection connection, long account_id)
        {
            string query = String.Format("UPDATE accounts SET shield = NOW() - INTERVAL 1 SECOND WHERE id = {0};", account_id);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
            return true;
        }

        private static bool _RemoveShieldAsync(long account_id)
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                return RemoveShield(connection, account_id);
            }
        }

        public async static void BoostResource(int id, long building_id)
        {
            long account_id = Server.clients[id].account;
            int res = await BoostResourceAsync(account_id, building_id);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BOOST);
            packet.Write(res);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> BoostResourceAsync(long account_id, long building_id)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _BoostResourceAsync(account_id, building_id), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _BoostResourceAsync(long account_id, long building_id)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                Data.Building building = null;
                DateTime now = DateTime.Now;
                string query = String.Format("SELECT level, global_id, boost, NOW() as now FROM buildings WHERE id = {0} AND account_id = {1};", building_id, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            building = new Data.Building();
                            while (reader.Read())
                            {
                                DateTime.TryParse(reader["now"].ToString(), out now);
                                DateTime.TryParse(reader["boost"].ToString(), out building.boost);
                                building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                                int.TryParse(reader["level"].ToString(), out building.level);
                            }
                        }
                    }
                }
                if (building != null)
                {
                    int cost = Data.GetBoostResourcesCost(building.id, building.level);
                    if (SpendResources(connection, account_id, 0, 0, cost, 0))
                    {
                        if (building.boost >= now)
                        {
                            query = String.Format("UPDATE buildings SET boost = boost + INTERVAL 24 HOUR WHERE id = {0}", building_id);
                        }
                        else
                        {
                            query = String.Format("UPDATE buildings SET boost = NOW() + INTERVAL 24 HOUR WHERE id = {0}", building_id);
                        }
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        response = 1;
                    }
                }
                connection.Close();
            }
            return response;
        }

        public async static void BuyResources(int id, int pack)
        {
            long account_id = Server.clients[id].account;
            int res = await BuyResourcesAsync(account_id, pack);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BUYRESOURCE);
            packet.Write(res);
            packet.Write(pack);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> BuyResourcesAsync(long account_id, int pack)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _BuyResourcesAsync(account_id, pack), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _BuyResourcesAsync(long account_id, int pack)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int goldCapacity = 0;
                int elixirCapacity = 0;
                int darkCapacity = 0;
                int gold = 0;
                int elixir = 0;
                int dark = 0;
                string query = String.Format("SELECT buildings.gold_storage, buildings.elixir_storage, buildings.dark_elixir_storage, server_buildings.gold_capacity, server_buildings.elixir_capacity, server_buildings.dark_elixir_capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.account_id = {0} AND buildings.global_id IN ('{1}', '{2}', '{3}', '{4}');", account_id, Data.BuildingID.townhall.ToString(), Data.BuildingID.goldstorage.ToString(), Data.BuildingID.elixirstorage.ToString(), Data.BuildingID.darkelixirstorage.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int result = 0;
                                int.TryParse(reader["gold_capacity"].ToString(), out result);
                                goldCapacity += result;
                                result = 0;
                                int.TryParse(reader["elixir_capacity"].ToString(), out result);
                                elixirCapacity += result;
                                result = 0;
                                int.TryParse(reader["dark_elixir_capacity"].ToString(), out result);
                                darkCapacity += result;
                                result = 0;
                                int.TryParse(reader["gold_storage"].ToString(), out result);
                                gold += result;
                                result = 0;
                                int.TryParse(reader["elixir_storage"].ToString(), out result);
                                elixir += result;
                                result = 0;
                                int.TryParse(reader["dark_elixir_storage"].ToString(), out result);
                                dark += result;
                            }
                        }
                    }
                }

                int tatgetGold = goldCapacity - gold;
                int tatgetElixir = elixirCapacity - elixir;
                int tatgetDark = darkCapacity - dark;

                switch ((Data.BuyResourcePack)pack)
                {
                    case Data.BuyResourcePack.gold_10:
                        tatgetGold = (int)Math.Floor(tatgetGold * 0.1d);
                        tatgetElixir = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.gold_50:
                        tatgetGold = (int)Math.Floor(tatgetGold * 0.5d);
                        tatgetElixir = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.gold_100:
                        tatgetElixir = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.elixir_10:
                        tatgetElixir = (int)Math.Floor(tatgetElixir * 0.1d);
                        tatgetGold = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.elixir_50:
                        tatgetElixir = (int)Math.Floor(tatgetElixir * 0.5d);
                        tatgetGold = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.elixir_100:
                        tatgetGold = 0;
                        tatgetDark = 0;
                        break;
                    case Data.BuyResourcePack.dark_10:
                        tatgetDark = (int)Math.Floor(tatgetDark * 0.1d);
                        tatgetGold = 0;
                        tatgetElixir = 0;
                        break;
                    case Data.BuyResourcePack.dark_50:
                        tatgetDark = (int)Math.Floor(tatgetDark * 0.5d);
                        tatgetGold = 0;
                        tatgetElixir = 0;
                        break;
                    case Data.BuyResourcePack.dark_100:
                        tatgetGold = 0;
                        tatgetElixir = 0;
                        break;
                }

                if (tatgetGold < 0) { tatgetGold = 0; }
                if (tatgetElixir < 0) { tatgetElixir = 0; }
                if (tatgetDark < 0) { tatgetDark = 0; }

                int cost = Data.GetResourceGemCost(tatgetGold, tatgetElixir, tatgetDark);
                if (SpendResources(connection, account_id, 0, 0, cost, 0))
                {
                    var add = AddResources(connection, account_id, tatgetGold, tatgetElixir, tatgetDark, 0);
                    response = 1;
                }
                connection.Close();
            }
            return response;
        }

        #endregion

        #region Server Buildings

        private async static Task<Data.ServerBuilding> GetServerBuildingAsync(string id, int level)
        {
            Task<Data.ServerBuilding> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetServerBuildingAsync(id, level), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static Data.ServerBuilding _GetServerBuildingAsync(string id, int level)
        {
            Data.ServerBuilding data = null;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                string query = String.Format("SELECT id, req_gold, req_elixir, req_gems, req_dark_elixir, columns_count, rows_count, build_time, gained_xp FROM server_buildings WHERE global_id = '{0}' AND level = {1};", id, level);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                data = new Data.ServerBuilding();
                                data.id = id;
                                long.TryParse(reader["id"].ToString(), out data.databaseID);
                                int.TryParse(reader["req_gold"].ToString(), out data.requiredGold);
                                int.TryParse(reader["req_elixir"].ToString(), out data.requiredElixir);
                                int.TryParse(reader["req_gems"].ToString(), out data.requiredGems);
                                int.TryParse(reader["req_dark_elixir"].ToString(), out data.requiredDarkElixir);
                                data.level = level;
                                int.TryParse(reader["columns_count"].ToString(), out data.columns);
                                int.TryParse(reader["rows_count"].ToString(), out data.rows);
                                int.TryParse(reader["build_time"].ToString(), out data.buildTime);
                                int.TryParse(reader["gained_xp"].ToString(), out data.gainedXp);
                            }
                        }
                    }
                }
                connection.Close();
            }
            return data;
        }

        private static List<Data.ServerBuilding> GetServerBuildings(MySqlConnection connection)
        {
            List<Data.ServerBuilding> buildings = new List<Data.ServerBuilding>();
            string query = String.Format("SELECT id, global_id, level, req_gold, req_elixir, req_gems, req_dark_elixir, columns_count, rows_count, build_time, gained_xp FROM server_buildings;");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Data.ServerBuilding building = new Data.ServerBuilding();
                            long.TryParse(reader["id"].ToString(), out building.databaseID);
                            // building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                            building.id = reader["global_id"].ToString();
                            int.TryParse(reader["level"].ToString(), out building.level);
                            int.TryParse(reader["req_gold"].ToString(), out building.requiredGold);
                            int.TryParse(reader["req_elixir"].ToString(), out building.requiredElixir);
                            int.TryParse(reader["req_gems"].ToString(), out building.requiredGems);
                            int.TryParse(reader["req_dark_elixir"].ToString(), out building.requiredDarkElixir);
                            int.TryParse(reader["columns_count"].ToString(), out building.columns);
                            int.TryParse(reader["rows_count"].ToString(), out building.rows);
                            int.TryParse(reader["build_time"].ToString(), out building.buildTime);
                            int.TryParse(reader["gained_xp"].ToString(), out building.gainedXp);
                            buildings.Add(building);
                        }
                    }
                }
            }
            return buildings;
        }

        #endregion

        #region Collect Resources

        public async static void UpdateCollectabes(double deltaTime)
        {
            await UpdateCollectabesAsync(deltaTime);
            collecting = false;
        }

        private async static Task<bool> UpdateCollectabesAsync(double deltaTime)
        {
            Task<bool> task = Task.Run(() =>
            {
                return Retry.Do(() => _UpdateCollectabesAsync(deltaTime), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static bool _UpdateCollectabesAsync(double deltaTime)
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                // Add Gold
                string query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.gold_storage = buildings.gold_storage + (server_buildings.speed * {0} * IF(buildings.boost >= NOW(), 2, 1)) WHERE buildings.global_id = '{1}' AND buildings.level > 0", deltaTime / 3600d, Data.BuildingID.goldmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Add Elixir
                query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.elixir_storage = buildings.elixir_storage + (server_buildings.speed * {0} * IF(buildings.boost >= NOW(), 2, 1)) WHERE buildings.global_id = '{1}' AND buildings.level > 0", deltaTime / 3600d, Data.BuildingID.elixirmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Add Dark Elixir
                query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.dark_elixir_storage = buildings.dark_elixir_storage + (server_buildings.speed * {0} * IF(buildings.boost >= NOW(), 2, 1)) WHERE buildings.global_id = '{1}' AND buildings.level > 0", deltaTime / 3600d, Data.BuildingID.darkelixirmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Limit Gold
                query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.gold_storage = server_buildings.gold_capacity WHERE buildings.gold_storage > server_buildings.gold_capacity AND buildings.global_id = '{0}' And buildings.level > 0", Data.BuildingID.goldmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Limit Elixir
                query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.elixir_storage = server_buildings.elixir_capacity WHERE buildings.elixir_storage > server_buildings.elixir_capacity AND buildings.global_id = '{0}' And buildings.level > 0", Data.BuildingID.elixirmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Limit Dark Elixir
                query = String.Format("UPDATE buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level SET buildings.dark_elixir_storage = server_buildings.dark_elixir_capacity WHERE buildings.dark_elixir_storage > server_buildings.dark_elixir_capacity AND buildings.global_id = '{0}' And buildings.level > 0", Data.BuildingID.darkelixirmine.ToString());
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            return true;
        }

        public async static void Collect(int id, long database_id)
        {
            long account_id = Server.clients[id].account;
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.COLLECT);
            int amount = await CollectAsync(account_id, database_id);
            packet.Write(database_id);
            packet.Write(amount);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> CollectAsync(long account_id, long database_id)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _CollectAsync(account_id, database_id), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _CollectAsync(long account_id, long database_id)
        {
            int amount = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int amountGold = 0;
                int amountElixir = 0;
                int amountDark = 0;
                string query = String.Format("SELECT global_id, gold_storage, elixir_storage, dark_elixir_storage FROM buildings WHERE id = {0} AND account_id = {1};", database_id, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.BuildingID global_id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                                switch (global_id)
                                {
                                    case Data.BuildingID.goldmine:
                                        amountGold = (int)Math.Floor(float.Parse(reader["gold_storage"].ToString()));
                                        break;
                                    case Data.BuildingID.elixirmine:
                                        amountElixir = (int)Math.Floor(float.Parse(reader["elixir_storage"].ToString()));
                                        break;
                                    case Data.BuildingID.darkelixirmine:
                                        amountDark = (int)Math.Floor(float.Parse(reader["dark_elixir_storage"].ToString()));
                                        break;
                                }
                            }
                        }
                    }
                }
                if (amountGold > 0)
                {
                    amount = AddResources(connection, account_id, amountGold, 0, 0, 0).Item1;
                    query = String.Format("UPDATE buildings SET gold_storage = gold_storage - {0} WHERE id = {1};", amount, database_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                }
                else if (amountElixir > 0)
                {
                    amount = AddResources(connection, account_id, 0, amountElixir, 0, 0).Item2;
                    query = String.Format("UPDATE buildings SET elixir_storage = elixir_storage - {0} WHERE id = {1};", amount, database_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                }
                else if (amountDark > 0)
                {
                    amount = AddResources(connection, account_id, 0, 0, amountDark, 0).Item3;
                    query = String.Format("UPDATE buildings SET dark_elixir_storage = dark_elixir_storage - {0} WHERE id = {1};", amount, database_id);
                    using (MySqlCommand command = new MySqlCommand(query, connection)) { command.ExecuteNonQuery(); }
                }
                connection.Close();
            }
            return amount;
        }

        #endregion

        #region General Update

        public async static void GeneralUpdate(double deltaTime)
        {
            await GeneralUpdateAsync(deltaTime);
            updating = false;
        }

        private async static Task<bool> GeneralUpdateAsync(double deltaTime)
        {
            Task<bool> task = Task.Run(() =>
            {
                return Retry.Do(() => _GeneralUpdateAsync(deltaTime), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static bool _GeneralUpdateAsync(double deltaTime)
        {
            using (MySqlConnection connection = GetMysqlConnection())
            {
                GeneralUpdateBuildings(connection);
                connection.Close();
            }
            return true;
        }

        private static void GeneralUpdateBuildings(MySqlConnection connection)
        {
            string time = "";
            string query = String.Format("SELECT NOW() AS time");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            time = DateTime.Parse(reader["time"].ToString()).ToString("yyyy-MM-dd H:mm:ss");
                        }
                    }
                }
            }

            List<Tuple<long, int, int>> playerUpdates = new List<Tuple<long, int, int>>();

            query = String.Format("SELECT b.account_id, b.global_id, b.level, sb.gained_xp FROM buildings AS b LEFT JOIN server_buildings AS sb ON b.global_id = sb.global_id AND b.level = sb.level WHERE b.is_constructing > 0 AND b.construction_time <= NOW()");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            long accountId = 0;
                            int xpGained = 0;
                            string globalId = "";
                            int currentLevel = 0;

                            if (long.TryParse(reader["account_id"].ToString(), out accountId))
                            {
                                globalId = reader["global_id"].ToString();
                                int.TryParse(reader["gained_xp"].ToString(), out xpGained);
                                int.TryParse(reader["level"].ToString(), out currentLevel);

                                int elixirGained = 0;
                                switch (globalId)
                                {
                                    case "obstacle":
                                        elixirGained = 0;
                                        break;
                                    case "tree":
                                        if (currentLevel == 0)
                                        {
                                            elixirGained = 50;
                                        }
                                        else
                                        {
                                            elixirGained = 0;
                                        }
                                        break;
                                    case "goldmine":
                                        elixirGained = 100;
                                        break;
                                    case "goldstorage":
                                        elixirGained = 150;
                                        break;
                                    case "elixirmine":
                                        elixirGained = 150;
                                        break;
                                    case "elixirstorage":
                                        elixirGained = 200;
                                        break;
                                    case "armycamp":
                                        elixirGained = 200;
                                        break;
                                    case "spellfactory":
                                        elixirGained = 300;
                                        break;
                                    case "laboratory":
                                        elixirGained = 400;
                                        break;
                                }

                                playerUpdates.Add(new Tuple<long, int, int>(accountId, xpGained, elixirGained));
                            }
                        }
                    }
                }
            }

            query = String.Format("DELETE FROM buildings WHERE is_constructing > 0 AND construction_time <= NOW() AND (global_id = '{0}' OR (global_id = '{1}' AND level = 1))", Data.BuildingID.obstacle.ToString(), Data.BuildingID.tree.ToString());
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE buildings SET level = level + 1, is_constructing = 0, track_time = '{0}' WHERE is_constructing > 0 AND construction_time <= NOW() AND global_id <> '{1}' AND (global_id <> '{2}' OR level = 0)", time, Data.BuildingID.obstacle.ToString(), Data.BuildingID.tree.ToString());
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE buildings SET track_time = track_time - INTERVAL 1 HOUR WHERE track_time = '{0}'", time);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            if (playerUpdates.Count > 0)
            {
                var groupedUpdates = playerUpdates.GroupBy(u => u.Item1)
                                                .Select(g => new
                                                {
                                                    AccountId = g.Key,
                                                    TotalXp = g.Sum(x => x.Item2),
                                                    TotalElixir = g.Sum(x => x.Item3)
                                                });

                foreach (var update in groupedUpdates)
                {
                    if (update.TotalXp > 0)
                    {
                        AddXP(connection, update.AccountId, update.TotalXp);
                    }
                    if (update.TotalElixir > 0)
                    {
                        AddResources(connection, update.AccountId, 0, update.TotalElixir, 0, 0);
                    }
                }
            }
        }

        private static void GeneralUpdateUnitTraining(MySqlConnection connection, float deltaTime)
        {
            string query = String.Format("UPDATE units LEFT JOIN server_units ON units.global_id = server_units.global_id AND units.level = server_units.level SET trained = 1 WHERE units.trained_time >= server_units.train_time");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE units AS t1 INNER JOIN (SELECT units.id FROM units LEFT JOIN server_units ON units.global_id = server_units.global_id AND units.level = server_units.level WHERE units.trained <= 0 AND units.trained_time < server_units.train_time GROUP BY units.account_id) t2 ON t1.id = t2.id SET trained_time = trained_time + {0}", deltaTime);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE units AS t1 INNER JOIN (SELECT units.id, (IFNULL(buildings.capacity, 0) - IFNULL(t.occupied, 0)) AS capacity, server_units.housing FROM units LEFT JOIN server_units ON units.global_id = server_units.global_id AND units.level = server_units.level LEFT JOIN (SELECT buildings.account_id, SUM(server_buildings.capacity) AS capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.global_id = 'armycamp' AND buildings.level > 0 GROUP BY buildings.account_id) AS buildings ON units.account_id = buildings.account_id LEFT JOIN (SELECT units.account_id, SUM(server_units.housing) AS occupied FROM units LEFT JOIN server_units ON units.global_id = server_units.global_id AND units.level = server_units.level WHERE units.ready > 0 GROUP BY units.account_id) AS t ON units.account_id = t.account_id WHERE units.trained > 0 AND units.ready <= 0 GROUP BY units.account_id) t2 ON t1.id = t2.id SET ready = 1 WHERE housing <= capacity");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void GeneralUpdateSpellBrewing(MySqlConnection connection, float deltaTime)
        {
            string query = String.Format("UPDATE spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level SET brewed = 1 WHERE spells.brewed_time >= server_spells.brew_time");
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE spells AS t1 INNER JOIN (SELECT spells.id FROM spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level WHERE spells.brewed <= 0 AND spells.brewed_time < server_spells.brew_time GROUP BY spells.account_id) t2 ON t1.id = t2.id SET brewed_time = brewed_time + {0}", deltaTime);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE spells AS t1 INNER JOIN (SELECT spells.id, (IFNULL(buildings.capacity, 0) - IFNULL(t.occupied, 0)) AS capacity, server_spells.housing, server_spells.building_code FROM spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level LEFT JOIN (SELECT buildings.account_id, SUM(server_buildings.capacity) AS capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.global_id = '{0}' AND buildings.level > 0 GROUP BY buildings.account_id) AS buildings ON spells.account_id = buildings.account_id LEFT JOIN (SELECT spells.account_id, SUM(server_spells.housing) AS occupied FROM spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level WHERE spells.ready > 0 AND server_spells.building_code = {1} GROUP BY spells.account_id) AS t ON spells.account_id = t.account_id WHERE spells.brewed > 0 AND spells.ready <= 0 GROUP BY spells.account_id) t2 ON t1.id = t2.id SET ready = 1 WHERE housing <= capacity AND building_code = {1}", Data.BuildingID.spellfactory.ToString(), 0);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }

            query = String.Format("UPDATE spells AS t1 INNER JOIN (SELECT spells.id, (IFNULL(buildings.capacity, 0) - IFNULL(t.occupied, 0)) AS capacity, server_spells.housing, server_spells.building_code FROM spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level LEFT JOIN (SELECT buildings.account_id, SUM(server_buildings.capacity) AS capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.global_id = '{0}' AND buildings.level > 0 GROUP BY buildings.account_id) AS buildings ON spells.account_id = buildings.account_id LEFT JOIN (SELECT spells.account_id, SUM(server_spells.housing) AS occupied FROM spells LEFT JOIN server_spells ON spells.global_id = server_spells.global_id AND spells.level = server_spells.level WHERE spells.ready > 0 AND server_spells.building_code = {1} GROUP BY spells.account_id) AS t ON spells.account_id = t.account_id WHERE spells.brewed > 0 AND spells.ready <= 0 GROUP BY spells.account_id) t2 ON t1.id = t2.id SET ready = 1 WHERE housing <= capacity AND building_code = {1}", Data.BuildingID.darkspellfactory.ToString(), 1);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }    
        
        #endregion

        #region Buildings

        private async static Task<Data.Building> GetBuildingAsync(long id, long account)
        {
            Task<Data.Building> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetBuildingAsync(id, account), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static Data.Building _GetBuildingAsync(long id, long account)
        {
            Data.Building building = null;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                building = GetBuilding(connection, id, account);
                connection.Close();
            }
            return building;
        }

        private static Data.Building GetBuilding(MySqlConnection connection, long id, long account)
        {
            Data.Building building = null;
            string query = String.Format("SELECT level, global_id FROM buildings WHERE id = {0} AND account_id = {1};", id, account);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        building = new Data.Building();
                        while (reader.Read())
                        {
                            building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                            int.TryParse(reader["level"].ToString(), out building.level);
                        }
                    }
                }
            }
            return building;
        }

        private static List<Data.Building> GetBuildingsByGlobalID(string globalID, long account, MySqlConnection connection)
        {
            List<Data.Building> buildings = new List<Data.Building>();
            string query = String.Format("SELECT buildings.level, buildings.global_id, server_buildings.capacity FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.global_id = '{0}' AND buildings.account_id = {1};", globalID, account);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Data.Building building = new Data.Building();
                            building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                            int.TryParse(reader["level"].ToString(), out building.level);
                            int.TryParse(reader["capacity"].ToString(), out building.capacity);
                            buildings.Add(building);
                        }
                    }
                }
            }
            return buildings;
        }

        private async static Task<List<Data.Building>> GetBuildingsAsync(long account)
        {
            Task<List<Data.Building>> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetBuildingsAsync(account), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static List<Data.Building> _GetBuildingsAsync(long account)
        {
            List<Data.Building> data = new List<Data.Building>();
            using (MySqlConnection connection = GetMysqlConnection())
            {
                data = GetBuildings(connection, account);
                connection.Close();
            }
            return data;
        }

        private static List<Data.Building> GetBuildings(MySqlConnection connection, long account)
        {
            List<Data.Building> data = new List<Data.Building>();
            string query = String.Format("SELECT buildings.id, buildings.global_id, buildings.level, buildings.x_position, buildings.x_war, buildings.y_war, buildings.boost, buildings.gold_storage, buildings.elixir_storage, buildings.dark_elixir_storage, buildings.y_position, buildings.construction_time, buildings.is_constructing, buildings.construction_build_time, server_buildings.columns_count, server_buildings.rows_count, server_buildings.health, server_buildings.speed, server_buildings.radius, server_buildings.capacity, server_buildings.gold_capacity, server_buildings.elixir_capacity, server_buildings.dark_elixir_capacity, server_buildings.damage, server_buildings.target_type, server_buildings.blind_radius, server_buildings.splash_radius, server_buildings.projectile_speed FROM buildings LEFT JOIN server_buildings ON buildings.global_id = server_buildings.global_id AND buildings.level = server_buildings.level WHERE buildings.account_id = {0};", account);
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Data.Building building = new Data.Building();
                            building.id = (Data.BuildingID)Enum.Parse(typeof(Data.BuildingID), reader["global_id"].ToString());
                            long.TryParse(reader["id"].ToString(), out building.databaseID);
                            int.TryParse(reader["level"].ToString(), out building.level);
                            int.TryParse(reader["x_position"].ToString(), out building.x);
                            int.TryParse(reader["y_position"].ToString(), out building.y);
                            int.TryParse(reader["x_war"].ToString(), out building.warX);
                            int.TryParse(reader["y_war"].ToString(), out building.warY);
                            int.TryParse(reader["columns_count"].ToString(), out building.columns);
                            int.TryParse(reader["rows_count"].ToString(), out building.rows);

                            float storage = 0;
                            float.TryParse(reader["gold_storage"].ToString(), out storage);
                            building.goldStorage = (int)Math.Floor(storage);

                            storage = 0;
                            float.TryParse(reader["elixir_storage"].ToString(), out storage);
                            building.elixirStorage = (int)Math.Floor(storage);

                            storage = 0;
                            float.TryParse(reader["dark_elixir_storage"].ToString(), out storage);
                            building.darkStorage = (int)Math.Floor(storage);

                            DateTime.TryParse(reader["boost"].ToString(), out building.boost);
                            float.TryParse(reader["damage"].ToString(), out building.damage);
                            int.TryParse(reader["capacity"].ToString(), out building.capacity);
                            int.TryParse(reader["gold_capacity"].ToString(), out building.goldCapacity);
                            int.TryParse(reader["elixir_capacity"].ToString(), out building.elixirCapacity);
                            int.TryParse(reader["dark_elixir_capacity"].ToString(), out building.darkCapacity);
                            float.TryParse(reader["speed"].ToString(), out building.speed);
                            float.TryParse(reader["radius"].ToString(), out building.radius);
                            int.TryParse(reader["health"].ToString(), out building.health);
                            DateTime.TryParse(reader["construction_time"].ToString(), out building.constructionTime);
                            float.TryParse(reader["blind_radius"].ToString(), out building.blindRange);
                            float.TryParse(reader["splash_radius"].ToString(), out building.splashRange);
                            float.TryParse(reader["projectile_speed"].ToString(), out building.rangedSpeed);
                            string tt = reader["target_type"].ToString();
                            if (!string.IsNullOrEmpty(tt))
                            {
                                building.targetType = (Data.BuildingTargetType)Enum.Parse(typeof(Data.BuildingTargetType), tt);
                            }
                            int isConstructing = 0;
                            int.TryParse(reader["is_constructing"].ToString(), out isConstructing);
                            building.isConstructing = isConstructing > 0;
                            int.TryParse(reader["construction_build_time"].ToString(), out building.buildTime);
                            data.Add(building);
                        }
                    }
                }
            }
            return data;
        }

        #endregion

        #region Build And Replace

        public async static void PlaceBuilding(int id, string device, string buildingID, int x, int y, int layout, long layoutID)
        {
            long account_id = Server.clients[id].account;
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.BUILD);
            Data.ServerBuilding building = await GetServerBuildingAsync(buildingID, 1);
            int response = await PlaceBuildingAsync(account_id, building, x, y, layout, layoutID);
            packet.Write(response);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> PlaceBuildingAsync(long account_id, Data.ServerBuilding building, int x, int y, int layout, long layoutID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _PlaceBuildingAsync(account_id, building, x, y, layout, layoutID), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _PlaceBuildingAsync(long account_id, Data.ServerBuilding building, int x, int y, int layout, long layoutID)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                bool canPlaceBuilding = true;
                if (building == null || x < 0 || y < 0 || x + building.columns > Data.gridSize || y + building.rows > Data.gridSize)
                {
                    canPlaceBuilding = false;
                }
                else
                {
                    List<Data.Building> buildings = GetBuildings(connection, account_id);
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        int bX = (layout == 2) ? buildings[i].warX : buildings[i].x;
                        int bY = (layout == 2) ? buildings[i].warY : buildings[i].y;
                        Rectangle rect1 = new Rectangle(bX, bY, buildings[i].columns, buildings[i].rows);
                        Rectangle rect2 = new Rectangle(x, y, building.columns, building.rows);
                        if (rect2.IntersectsWith(rect1))
                        {
                            canPlaceBuilding = false;
                            break;
                        }
                    }
                }
                if (canPlaceBuilding)
                {
                    if (layout == 2)
                    {
                        long war_id = 0;
                        string query = String.Format("SELECT war_id FROM accounts WHERE id = {0};", account_id);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        long.TryParse(reader["war_id"].ToString(), out war_id);
                                    }
                                }
                            }
                        }
                        if (war_id > 0)
                        {
                            int war_stage = 0;
                            query = String.Format("SELECT stage FROM clan_wars WHERE id = {0};", war_id);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            int.TryParse(reader["stage"].ToString(), out war_stage);
                                        }
                                    }
                                }
                            }
                            if (war_stage == 1)
                            {
                                query = String.Format("UPDATE buildings SET x_war = {0}, y_war = {1} WHERE id = {2}", x, y, layoutID);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                    response = 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (building.id == "buildershut")
                        {
                            int c = GetBuildingCount(account_id, "buildershut", connection);
                            switch (c)
                            {
                                case 0: building.requiredGems = 0; break;
                                case 1: building.requiredGems = 250; break;
                                case 2: building.requiredGems = 500; break;
                                case 3: building.requiredGems = 1000; break;
                                case 4: building.requiredGems = 2000; break;
                                default: building.requiredGems = 999999; break;
                            }
                        }
                        int time = 0;
                        bool haveBuilding = false;
                        string query = String.Format("SELECT build_time FROM server_buildings WHERE global_id = '{0}' AND level = 1;", building.id);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    haveBuilding = true;
                                    while (reader.Read())
                                    {
                                        time = int.Parse(reader["build_time"].ToString());
                                    }
                                }
                            }
                        }
                        if (haveBuilding)
                        {
                            int buildersCount = 100;
                            int constructingCount = GetBuildingConstructionCount(account_id, connection);
                            if (time > 0 && buildersCount <= constructingCount)
                            {
                                response = 5;
                            }
                            else
                            {
                                bool limited = false;
                                Data.Building townHall = GetBuildingsByGlobalID("townhall", account_id, connection)[0];
                                if (building.id == "townhall")
                                {

                                }
                                else
                                {
                                    //Data.BuildingCount limits = Data.GetBuildingLimits(townHall.level, building.id);
                                    //int haveCount = GetBuildingCount(account_id, building.id, connection);
                                    //if (limits == null || haveCount >= limits.count)
                                    //{
                                    //    limited = true;
                                    //}
                                }
                                if (limited)
                                {
                                    response = 6;
                                }
                                else
                                {
                                    if (SpendResources(connection, account_id, building.requiredGold, building.requiredElixir, building.requiredGems, building.requiredDarkElixir))
                                    {
                                        if (time > 0)
                                        {
                                            query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, is_constructing, construction_time, construction_build_time, track_time) VALUES('{0}', {1}, {2}, {3}, 0, 1, NOW() + INTERVAL {4} SECOND, {5}, NOW() - INTERVAL 1 HOUR);", building.id, account_id, x, y, time, time);
                                        }
                                        else
                                        {
                                            query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, level, is_constructing, track_time) VALUES('{0}', {1}, {2}, {3}, 1, 0, NOW() - INTERVAL 1 HOUR);", building.id, account_id, x, y);
                                            AddXP(connection, account_id, building.gainedXp);
                                        }
                                        using (MySqlCommand command = new MySqlCommand(query, connection))
                                        {
                                            command.ExecuteNonQuery();
                                            response = 1;
                                        }
                                    }
                                    else
                                    {
                                        response = 2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            response = 3;
                        }
                    }
                }
                else
                {
                    response = 4;
                }
                connection.Close();
            }
            return response;
        }

        public async static void ReplaceBuilding(int id, long databaseID, int x, int y, int layout)
        {
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.REPLACE);
            long account_id = Server.clients[id].account;
            int response = await ReplaceBuildingAsync(account_id, databaseID, x, y, layout);
            packet.Write(response);
            packet.Write(x);
            packet.Write(y);
            packet.Write(databaseID);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> ReplaceBuildingAsync(long account_id, long building_id, int x, int y, int layout)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _ReplaceBuildingAsync(account_id, building_id, x, y, layout), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _ReplaceBuildingAsync(long account_id, long building_id, int x, int y, int layout)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                List<Data.Building> buildings = GetBuildings(connection, account_id);
                Data.Building building = null;

                if (buildings != null && buildings.Count > 0)
                {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        if (buildings[i].databaseID == building_id)
                        {
                            building = buildings[i];
                            break;
                        }
                    }
                }
                if (building != null)
                {
                    bool canPlaceBuilding = true;
                    if (x < 0 || y < 0 || x + building.columns > Data.gridSize || y + building.rows > Data.gridSize)
                    {
                        canPlaceBuilding = false;
                    }
                    else
                    {
                        for (int i = 0; i < buildings.Count; i++)
                        {
                            if (buildings[i].databaseID != building.databaseID)
                            {
                                int bX = (layout == 2) ? buildings[i].warX : buildings[i].x;
                                int bY = (layout == 2) ? buildings[i].warY : buildings[i].y;
                                Rectangle rect1 = new Rectangle(bX, bY, buildings[i].columns, buildings[i].rows);
                                Rectangle rect2 = new Rectangle(x, y, building.columns, building.rows);
                                if (rect2.IntersectsWith(rect1))
                                {
                                    canPlaceBuilding = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (canPlaceBuilding)
                    {
                        string query = "";
                        if (layout == 2)
                        {
                            long war_id = 0;
                            query = String.Format("SELECT war_id FROM accounts WHERE id = {0};", account_id);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            long.TryParse(reader["war_id"].ToString(), out war_id);
                                        }
                                    }
                                }
                            }
                            if (war_id > 0)
                            {
                                int war_stage = 0;
                                query = String.Format("SELECT stage FROM clan_wars WHERE id = {0};", war_id);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            while (reader.Read())
                                            {
                                                int.TryParse(reader["stage"].ToString(), out war_stage);
                                            }
                                        }
                                    }
                                }
                                if (war_stage == 1)
                                {
                                    query = String.Format("UPDATE buildings SET x_war = {0}, y_war = {1} WHERE id = {2};", x, y, building_id);
                                }
                                else
                                {
                                    query = "";
                                }
                            }
                            else
                            {
                                query = "";
                            }
                        }
                        else
                        {
                            query = String.Format("UPDATE buildings SET x_position = {0}, y_position = {1} WHERE id = {2};", x, y, building_id);
                        }
                        if (!string.IsNullOrEmpty(query))
                        {
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            response = 1;
                        }
                        else
                        {
                            response = 3;
                        }
                    }
                    else
                    {
                        response = 2;
                    }
                }
                connection.Close();
            }
            return response;
        }

        public async static void UpgradeBuilding(int id, long buildingID)
        {
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.UPGRADE);
            long account_id = Server.clients[id].account;
            Data.Building building = await GetBuildingAsync(buildingID, account_id);
            if (building == null)
            {
                packet.Write(0);
            }
            else
            {
                int response = await UpgradeBuildingAsync(account_id, buildingID, building.level, building.id.ToString());
                packet.Write(response);
            }
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> UpgradeBuildingAsync(long account_id, long buildingID, int level, string globalID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _UpgradeBuildingAsync(account_id, buildingID, level, globalID), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _UpgradeBuildingAsync(long account_id, long buildingID, int level, string globalID)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int time = 0;
                bool haveLevel = false;
                int reqGold = 0;
                int reqElixir = 0;
                int reqDarkElixir = 0;
                int reqGems = 0;

                bool isTreeOrObstacle = (globalID == Data.BuildingID.obstacle.ToString() || globalID == Data.BuildingID.tree.ToString());

                if (isTreeOrObstacle)
                {
                    haveLevel = true;
                    time = 0;
                    reqElixir = 50;
                    reqGold = 100;
                }
                else
                {
                    string query = String.Format("SELECT req_gold, req_elixir, req_dark_elixir, req_gems, build_time FROM server_buildings WHERE global_id = '{0}' AND level = {1};", globalID, level + 1);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    haveLevel = true;
                                    time = int.Parse(reader["build_time"].ToString());
                                    reqGold = int.Parse(reader["req_gold"].ToString());
                                    reqElixir = int.Parse(reader["req_elixir"].ToString());
                                    reqDarkElixir = int.Parse(reader["req_dark_elixir"].ToString());
                                    reqGems = int.Parse(reader["req_gems"].ToString());
                                }
                            }
                        }
                    }
                }

                if (haveLevel)
                {
                    int buildersCount = 100;
                    int constructingCount = GetBuildingConstructionCount(account_id, connection);

                    if (time > 0 && buildersCount <= constructingCount)
                    {
                        response = 5;
                    }
                    else
                    {
                        bool limited = false;
                        if (limited)
                        {
                            response = 6;
                        }
                        else
                        {
                            bool resourcesOk = false;

                            if (isTreeOrObstacle)
                            {
                                long townHallId = 0;
                                string thQuery = String.Format("SELECT id FROM buildings WHERE account_id = {0} AND global_id = '{1}';", account_id, Data.BuildingID.townhall.ToString());

                                using (MySqlCommand cmdGet = new MySqlCommand(thQuery, connection))
                                {
                                    var result = cmdGet.ExecuteScalar();
                                    if (result != null) long.TryParse(result.ToString(), out townHallId);
                                }

                                if (townHallId > 0)
                                {
                                    string updateQuery = String.Format("UPDATE buildings SET elixir_storage = elixir_storage - {0} WHERE id = {1};", reqElixir, townHallId);
                                    using (MySqlCommand cmdUpdate = new MySqlCommand(updateQuery, connection))
                                    {
                                        cmdUpdate.ExecuteNonQuery();
                                    }

                                    AddResources(connection, account_id, reqGold, 0, 0, 0);
                                    resourcesOk = true;
                                }
                            }
                            else
                            {
                                if (SpendResources(connection, account_id, reqGold, reqElixir, reqGems, reqDarkElixir))
                                {
                                    resourcesOk = true;
                                }
                            }

                            if (resourcesOk)
                            {
                                if (isTreeOrObstacle)
                                {
                                    string query = String.Format("DELETE FROM buildings WHERE id = {0} AND (global_id = '{1}' OR global_id = '{2}');",
                                        buildingID, Data.BuildingID.obstacle.ToString(), Data.BuildingID.tree.ToString());

                                    using (MySqlCommand command = new MySqlCommand(query, connection))
                                    {
                                        command.ExecuteNonQuery();
                                        response = 1;
                                    }
                                }
                                else
                                {
                                    string query = String.Format("UPDATE buildings SET is_constructing = 1, construction_time = NOW() + INTERVAL {0} SECOND, construction_build_time = {1} WHERE id = {2};", time, time, buildingID);
                                    using (MySqlCommand command = new MySqlCommand(query, connection))
                                    {
                                        command.ExecuteNonQuery();
                                        response = 1;
                                    }
                                }
                            }
                            else
                            {
                                response = 2;
                            }
                        }
                    }
                }
                else
                {
                    response = 3;
                }
                connection.Close();
            }
            return response;
        }

        public async static void InstantBuild(int id, long buildingID)
        {
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.INSTANTBUILD);
            long account_id = Server.clients[id].account;
            Data.Building building = await GetBuildingAsync(buildingID, account_id);
            if (building == null)
            {
                packet.Write(0);
            }
            else
            {
                int res = await InstantBuildAsync(account_id, buildingID, building.level, building.id.ToString());
                packet.Write(res);
            }
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> InstantBuildAsync(long account_id, long buildingID, int level, string globalID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _InstantBuildAsync(account_id, buildingID, level, globalID), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _InstantBuildAsync(long account_id, long buildingID, int level, string globalID)
        {
            int id = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                int time = 0;
                string query = String.Format("SELECT construction_time, NOW() AS now_time FROM buildings WHERE id = {0} AND account_id = {1} AND is_constructing > 0;", buildingID, account_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                DateTime target = DateTime.Parse(reader["construction_time"].ToString());
                                DateTime now = DateTime.Parse(reader["now_time"].ToString());
                                if (target > now)
                                {
                                    time = (int)(target - now).TotalSeconds;
                                }
                            }
                        }
                    }
                }
                if (time > 0)
                {
                    int requiredGems = Data.GetInstantBuildRequiredGems(time);
                    if (SpendResources(connection, account_id, 0, 0, requiredGems, 0))
                    {
                        query = String.Format("UPDATE buildings SET construction_time = NOW() WHERE id = {0}", buildingID);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                            id = 1;
                        }
                    }
                    else
                    {
                        id = 2;
                    }
                }
                connection.Close();
            }
            return id;
        }

        #endregion

        #region Email

        public async static void SendRecoveryCode(int id, string device, string email)
        {
            var code = await SendRecoveryCodeAsync(id, device, email);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.SENDCODE);
            packet.Write(code.Item1);
            packet.Write(code.Item2);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<(int, int)> SendRecoveryCodeAsync(int id, string device, string email)
        {
            Task<(int, int)> task = Task.Run(() =>
            {
                return Retry.Do(() => _SendRecoveryCodeAsync(id, device, email), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static (int, int) _SendRecoveryCodeAsync(int id, string device, string email)
        {
            int expiration = 0;
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                long account_id = 0;
                if (!string.IsNullOrEmpty(email))
                {
                    string query = String.Format("SELECT id FROM accounts WHERE email = '{0}' AND is_online <= 0;", email);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    long.TryParse(reader["id"].ToString(), out account_id);
                                }
                            }
                        }
                    }
                }
                if (account_id > 0)
                {
                    long code_id = 0;
                    DateTime nowTime = DateTime.Now;
                    DateTime expireTime = nowTime;
                    string query = String.Format("SELECT id, NOW() AS now_time, expire_time FROM verification_codes WHERE device_id = '{0}' AND target = '{1}' AND NOW() < expire_time;", device, email);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    long.TryParse(reader["id"].ToString(), out code_id);
                                    DateTime.TryParse(reader["now_time"].ToString(), out nowTime);
                                    DateTime.TryParse(reader["expire_time"].ToString(), out expireTime);
                                }
                            }
                        }
                    }
                    if (code_id > 0)
                    {
                        response = 1;
                        expiration = (int)Math.Floor((expireTime - nowTime).TotalSeconds);
                    }
                    else
                    {
                        string code = Data.RandomCode(Data.recoveryCodeLength);
                        if (Email.SendEmailVerificationCode(code, email))
                        {
                            query = String.Format("INSERT INTO verification_codes (target, device_id, code, expire_time) VALUES('{0}', '{1}', '{2}', NOW() + INTERVAL {3} SECOND)", email, device, code, Data.recoveryCodeExpiration);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            response = 1;
                            expiration = Data.recoveryCodeExpiration;
                        }
                        else
                        {
                            response = 2;
                        }
                    }
                }
                connection.Close();
            }
            return (response, expiration);
        }

        public async static void ConfirmRecoveryCode(int id, string device, string email, string code)
        {
            var response = await ConfirmRecoveryCodeAsync(id, device, email, code);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.CONFIRMCODE);
            packet.Write(response.Item1);
            packet.Write(response.Item2);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<(int, string)> ConfirmRecoveryCodeAsync(int id, string device, string email, string code)
        {
            Task<(int, string)> task = Task.Run(() =>
            {
                return Retry.Do(() => _ConfirmRecoveryCodeAsync(id, device, email, code), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static (int, string) _ConfirmRecoveryCodeAsync(int id, string device, string email, string code)
        {
            string password = "";
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                long account_id = 0;
                if (!string.IsNullOrEmpty(email))
                {
                    string query = String.Format("SELECT id FROM accounts WHERE email = '{0}' AND is_online <= 0;", email);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    long.TryParse(reader["id"].ToString(), out account_id);
                                }
                            }
                        }
                    }
                }
                if (account_id > 0)
                {
                    long code_id = 0;
                    string query = String.Format("SELECT id FROM verification_codes WHERE device_id = '{0}' AND target = '{1}' AND code = '{2}' AND NOW() < expire_time;", device, email, code);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    long.TryParse(reader["id"].ToString(), out code_id);
                                }
                            }
                        }
                    }
                    if (code_id > 0)
                    {
                        password = Data.EncrypteToMD5(Tools.GenerateToken());
                        query = String.Format("UPDATE accounts SET device_id = '{0}', password = '{1}' WHERE id = {2};", device, password, account_id);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        query = String.Format("DELETE FROM verification_codes WHERE id = {0};", code_id);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        response = 1;
                    }
                    else
                    {
                        response = 2;
                    }
                }
                connection.Close();
            }
            return (response, password);
        }

        public async static void SendEmailCode(int id, string device, string email)
        {
            long account_id = Server.clients[id].account;
            var code = await SendEmailCodeAsync(id, account_id, device, email);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.EMAILCODE);
            packet.Write(code.Item1);
            packet.Write(code.Item2);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<(int, int)> SendEmailCodeAsync(int id, long account_id, string device, string email)
        {
            Task<(int, int)> task = Task.Run(() =>
            {
                return Retry.Do(() => _SendEmailCodeAsync(id, account_id, device, email), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static (int, int) _SendEmailCodeAsync(int id, long account_id, string device, string email)
        {
            int expiration = 0;
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                bool found = false;
                if (!string.IsNullOrEmpty(email))
                {
                    string query = String.Format("SELECT id FROM accounts WHERE id = {0} AND device_id = '{1}' AND is_online > 0;", account_id, device);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                found = true;
                            }
                        }
                    }
                }
                if (found)
                {
                    found = false;
                    string query = String.Format("SELECT id FROM accounts WHERE email = '{0}';", email);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                found = true;
                            }
                        }
                    }
                    if (!found)
                    {
                        long code_id = 0;
                        DateTime nowTime = DateTime.Now;
                        DateTime expireTime = nowTime;
                        query = String.Format("SELECT id, NOW() AS now_time, expire_time FROM verification_codes WHERE device_id = '{0}' AND target = '{1}' AND NOW() < expire_time;", device, email);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        long.TryParse(reader["id"].ToString(), out code_id);
                                        DateTime.TryParse(reader["now_time"].ToString(), out nowTime);
                                        DateTime.TryParse(reader["expire_time"].ToString(), out expireTime);
                                    }
                                }
                            }
                        }
                        if (code_id > 0)
                        {
                            response = 1;
                            expiration = (int)Math.Floor((expireTime - nowTime).TotalSeconds);
                        }
                        else
                        {
                            string code = Data.RandomCode(Data.recoveryCodeLength);
                            if (Email.SendEmailConfirmationCode(code, email))
                            {
                                query = String.Format("INSERT INTO verification_codes (target, device_id, code, expire_time) VALUES('{0}', '{1}', '{2}', NOW() + INTERVAL {3} SECOND)", email, device, code, Data.confirmationCodeExpiration);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                                response = 1;
                                expiration = Data.confirmationCodeExpiration;
                            }
                            else
                            {
                                response = 2;
                            }
                        }
                    }
                    else
                    {
                        response = 3;
                    }
                }
                connection.Close();
            }
            return (response, expiration);
        }

        public async static void ConfirmEmailCode(int id, string device, string email, string code)
        {
            long account_id = Server.clients[id].account;
            int response = await ConfirmEmailCodeAsync(account_id, device, email, code);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.EMAILCONFIRM);
            packet.Write(response);
            packet.Write(email);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> ConfirmEmailCodeAsync(long account_id, string device, string email, string code)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _ConfirmEmailCodeAsync(account_id, device, email, code), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _ConfirmEmailCodeAsync(long account_id, string device, string email, string code)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                bool found = false;
                if (!string.IsNullOrEmpty(email))
                {
                    string query = String.Format("SELECT id FROM accounts WHERE id = {0} AND device_id = '{1}' AND is_online > 0;", account_id, device);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                found = true;
                            }
                        }
                    }
                }
                if (found)
                {
                    found = false;
                    string query = String.Format("SELECT id FROM accounts WHERE email = '{0}';", email);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                found = false;
                            }
                        }
                    }
                    if (!found)
                    {
                        long code_id = 0;
                        query = String.Format("SELECT id FROM verification_codes WHERE device_id = '{0}' AND target = '{1}' AND code = '{2}' AND NOW() < expire_time;", device, email, code);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        long.TryParse(reader["id"].ToString(), out code_id);
                                    }
                                }
                            }
                        }
                        if (code_id > 0)
                        {
                            query = String.Format("UPDATE accounts SET email = '{0}' WHERE id = {1};", email, account_id);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            query = String.Format("DELETE FROM verification_codes WHERE id = {0};", code_id);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            response = 1;
                        }
                        else
                        {
                            response = 2;
                        }
                    }
                    else
                    {
                        response = 3;
                    }
                }
                connection.Close();
            }
            return response;
        }

        #endregion

        #region Messages

        public async static void SyncMessages(int id, Data.ChatType type, long lastMessage)
        {
            long account_id = Server.clients[id].account;
            List<Data.CharMessage> response = await GetChatMessagesAsync(account_id, type, lastMessage);
            string data = await Data.SerializeAsync<List<Data.CharMessage>>(response);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.GETCHATS);
            byte[] bytes = await Data.CompressAsync(data);
            packet.Write(bytes.Length);
            packet.Write(bytes);
            packet.Write((int)type);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<List<Data.CharMessage>> GetChatMessagesAsync(long account_id, Data.ChatType type, long lastMessage)
        {
            Task<List<Data.CharMessage>> task = Task.Run(() =>
            {
                List<Data.CharMessage> response = null;
                response = Retry.Do(() => _GetChatMessagesAsync(account_id, type, lastMessage), TimeSpan.FromSeconds(0.1), 1, false);
                if (response == null)
                {
                    response = new List<Data.CharMessage>();
                }
                return response;
            });
            return await task;
        }

        private static List<Data.CharMessage> _GetChatMessagesAsync(long account_id, Data.ChatType type, long lastMessage)
        {
            List<Data.CharMessage> response = new List<Data.CharMessage>();
            using (MySqlConnection connection = GetMysqlConnection())
            {
                response = GetChatMessages(connection, account_id, type, lastMessage);
                connection.Close();
            }
            return response;
        }

        private static List<Data.CharMessage> GetChatMessages(MySqlConnection connection, long account_id, Data.ChatType type, long lastMessage)
        {
            List<Data.CharMessage> messages = new List<Data.CharMessage>();
            long global_id = 0;
            string query = "";
            string filterTime = "";

            if (type == Data.ChatType.clan) return messages;

            if (lastMessage > 0)
            {
                query = String.Format("SELECT DATE_FORMAT(send_time, '{0}') AS send_time FROM chat_messages WHERE id = {1}", Data.mysqlDateTimeFormat, lastMessage);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                filterTime = reader["send_time"].ToString();
                            }
                        }
                    }
                }
            }
         
            if (type == Data.ChatType.global)
            {
                if (!string.IsNullOrEmpty(filterTime))
                {
                    query = String.Format("SELECT chat_messages.id, chat_messages.account_id, chat_messages.type, chat_messages.global_id, chat_messages.clan_id, chat_messages.message, DATE_FORMAT(chat_messages.send_time, '{0}') AS send_time, accounts.name, accounts.chat_color FROM chat_messages LEFT JOIN accounts ON chat_messages.account_id = accounts.id WHERE chat_messages.global_id = {1} AND chat_messages.type = {2} AND chat_messages.send_time > '{3}'", Data.mysqlDateTimeFormat, global_id, (int)type, filterTime);
                }
                else
                {
                    query = String.Format("SELECT chat_messages.id, chat_messages.account_id, chat_messages.type, chat_messages.global_id, chat_messages.clan_id, chat_messages.message, DATE_FORMAT(chat_messages.send_time, '{0}') AS send_time, accounts.name, accounts.chat_color FROM chat_messages LEFT JOIN accounts ON chat_messages.account_id = accounts.id WHERE chat_messages.global_id = {1} AND chat_messages.type = {2}", Data.mysqlDateTimeFormat, global_id, (int)type);
                }

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.CharMessage message = new Data.CharMessage();
                                long.TryParse(reader["id"].ToString(), out message.id);
                                long.TryParse(reader["account_id"].ToString(), out message.accountID);
                                int t = 0;
                                int.TryParse(reader["type"].ToString(), out t);
                                message.type = (Data.ChatType)t;
                                long.TryParse(reader["global_id"].ToString(), out message.globalID);
                                long.TryParse(reader["clan_id"].ToString(), out message.clanID);
                                message.message = reader["message"].ToString();
                                message.name = reader["name"].ToString();
                                message.color = reader["chat_color"].ToString();
                                message.time = reader["send_time"].ToString();
                                messages.Add(message);
                            }
                        }
                    }
                }
            }
            return messages;
        }

        public async static void SendChatMessage(int id, string message, Data.ChatType type, long target)
        {
            long account_id = Server.clients[id].account;
            int response = await SendChatMessageAsync(account_id, message, type, target);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.SENDCHAT);
            packet.Write(response);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> SendChatMessageAsync(long account_id, string message, Data.ChatType type, long target)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _SendChatMessageAsync(account_id, message, type, target), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _SendChatMessageAsync(long account_id, string message, Data.ChatType type, long target)
        {
            int response = 0;
            if (type == Data.ChatType.clan) return 2;

            if (!string.IsNullOrEmpty(message) && Data.IsMessageGoodToSend(message))
            {
                using (MySqlConnection connection = GetMysqlConnection())
                {
                    int global_chat_blocked = 0;
                    bool timeOk = false;

                    string query = String.Format("SELECT global_chat_blocked FROM accounts WHERE id = {0} AND last_chat <= NOW() - INTERVAL 1 SECOND;", account_id);

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {                            
                                    int.TryParse(reader["global_chat_blocked"].ToString(), out global_chat_blocked);
                                    timeOk = true;
                                }
                            }
                        }
                    }

                    if (timeOk)
                    {
                        if (global_chat_blocked > 0)
                        {                          
                            response = 2;
                        }
                        else
                        {
                            if (type == Data.ChatType.global)
                            {                           
                                query = String.Format("INSERT INTO chat_messages (account_id, type, global_id, clan_id, message) VALUES({0}, {1}, {2}, 0, '{3}');", account_id, (int)type, target, message);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }

                                query = String.Format("DELETE FROM chat_messages WHERE type = {0} AND global_id = {1} AND send_time <= (SELECT send_time FROM (SELECT send_time FROM chat_messages WHERE type = {0} AND global_id = {1} ORDER BY send_time DESC LIMIT 1 OFFSET {2}) messages);", (int)type, target, Data.globalChatArchiveMaxMessages);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }

                                query = String.Format("UPDATE accounts SET last_chat = NOW() WHERE id = {0};", account_id);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.ExecuteNonQuery();
                                }
                                response = 1;
                            }
                        }
                    }
                    connection.Close();
                }
            }
            return response;
        }

        public async static void ReportChatMessage(int id, long message_id)
        {
            long account_id = Server.clients[id].account;
            int response = await ReportChatMessageAsync(account_id, message_id);
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.REPORTCHAT);
            packet.Write(response);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<int> ReportChatMessageAsync(long account_id, long message_id)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _ReportChatMessageAsync(account_id, message_id), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int max_chat_reports_for_auto_block = 25;
        private static int max_chat_reports_allowed_per_day = 25;

        private static int _ReportChatMessageAsync(long account_id, long message_id)
        {
            int response = 0;
            using (MySqlConnection connection = GetMysqlConnection())
            {
                long target_id = 0;
                string message = "";
                string query = String.Format("SELECT account_id, message FROM chat_messages WHERE id = {0};", message_id);
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                long.TryParse(reader["account_id"].ToString(), out target_id);
                                message = reader["message"].ToString();
                            }
                        }
                    }
                }
                if (target_id > 0)
                {
                    int reports_count = 0;
                    query = String.Format("SELECT COUNT(id) AS reports FROM chat_reports WHERE reporter_id = {0} AND report_time >= NOW() - INTERVAL {1} HOUR;", account_id, 24);
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int.TryParse(reader["reports"].ToString(), out reports_count);
                                }
                            }
                        }
                    }
                    if (reports_count >= max_chat_reports_allowed_per_day)
                    {
                        response = 2;
                    }
                    else
                    {
                        bool found = false;
                        query = String.Format("SELECT id FROM chat_reports WHERE reporter_id = {0} AND message_id = {1};", account_id, message_id);
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    found = true;
                                }
                            }
                        }
                        if (!found)
                        {
                            query = String.Format("INSERT INTO chat_reports (message_id, reporter_id, target_id, message) VALUES({0}, {1}, {2}, '{3}');", message_id, account_id, target_id, message);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            found = false;
                            query = String.Format("SELECT id FROM accounts WHERE id = {0} AND global_chat_blocked <= 0;", target_id);
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        found = true;
                                    }
                                }
                            }
                            if (found)
                            {
                                reports_count = 0;
                                query = String.Format("SELECT accounts.clan_id AS clan FROM chat_reports LEFT JOIN accounts ON accounts.id = chat_reports.reporter_id WHERE chat_reports.target_id = {0} AND chat_reports.report_time >= NOW() - INTERVAL {1} HOUR;", target_id, 24);
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (reader.HasRows)
                                        {
                                            List<long> clans = new List<long>();
                                            while (reader.Read())
                                            {
                                                long clan = 0;
                                                if (long.TryParse(reader["clan"].ToString(), out clan))
                                                {
                                                    if (clan > 0)
                                                    {
                                                        if (!clans.Contains(clan))
                                                        {
                                                            clans.Add(clan);
                                                            reports_count++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        reports_count++;
                                                    }
                                                    if (reports_count >= max_chat_reports_for_auto_block)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                if (reports_count >= max_chat_reports_for_auto_block)
                                {
                                    query = String.Format("UPDATE accounts SET global_chat_blocked = 1 WHERE id = {0};", target_id);
                                    using (MySqlCommand command = new MySqlCommand(query, connection))
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        response = 1;
                    }
                }
                connection.Close();
            }
            return response;
        }

        #endregion
    }
}