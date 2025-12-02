using HS.DB.Manager;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Connection
{
    public class DBConnectionMySQL : DBConnection
    {
        public const int PORT = 3306;
        public const string KIND = "MySQL";

        DBManagerMySQL manager;
        private DBConnectionMySQL(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param) : base(Server, ID, PW, DB, Timeout, Param)
        {
            this.Param = Param;
            manager = new DBManagerMySQL(this);
            Connector = new MySqlConnection(ConnectionString);
            //System.Console.WriteLine(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }

        public override string ConnectionString
        {
            get 
            {
                var sqlBuilder = new MySqlConnectionStringBuilder
                {
                    Server = Server,

                    UserID = ID,
                    Password = PW,
                    Database = DB,
                    ConnectionTimeout = (uint)Timeout,
                };
                foreach (var pair in Param)
                {
                    if (string.Equals(pair.Key, "Port", StringComparison.InvariantCultureIgnoreCase))
                    {
                        uint port;
                        uint.TryParse(pair.Value, out port);
                        sqlBuilder.Port = port < 1 ? PORT : port;
                    }
                    else sqlBuilder.Add(pair.Key, pair.Value);
                }
                return sqlBuilder.ToString();
            }
        }
        public MySqlConnection Connector { get; private set; }

        public override string Kind { get { return KIND; } }
        public override DBStatus Status
        {
            get
            {
                switch (Connector.State)
                {
                    case ConnectionState.Broken: return DBStatus.Fail;
                    case ConnectionState.Closed: return DBStatus.Closed;
                    case ConnectionState.Connecting: return DBStatus.Opening;
                    default: return DBStatus.Opened;
                }
            }
        }
        public override string ServerVersion => Connector.ServerVersion;


        public override DBManager Open()
        {
            Connector.Open();
            return manager;
        }
        public override async Task<DBManager> OpenAsync()
        {
            await Connector.OpenAsync();
            return manager;
        }
        internal void Close() { try { Connector?.Close(); } catch { } }

        public static explicit operator MySqlConnection(DBConnectionMySQL connection) { return connection.Connector; }


        #region Connect
        public static DBManagerMySQL Connect(string Server, string ID, string PW, string DB, int Timeout) { return Connect(Server, PORT, ID, PW, DB, Timeout); }
        public static DBManagerMySQL Connect(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            return Connect(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
            });
        }
        public static DBManagerMySQL Connect(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerMySQL)conn.Open();
        }

        public static async Task<DBManagerMySQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout) { return await ConnectAsync(Server, PORT, ID, PW, DB, Timeout); }
        public static async Task<DBManagerMySQL> ConnectAsync(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            return await ConnectAsync(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
            });
        }
        public static async Task<DBManagerMySQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerMySQL)await conn.OpenAsync();
        }
        #endregion
    }
}
