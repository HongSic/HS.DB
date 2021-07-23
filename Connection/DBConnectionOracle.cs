using HS.DB.Manager;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Connection
{
    public class DBConnectionOracle : DBConnection
    {
        public const int PORT = 1521;
        DBManagerOracle manager;
        private DBConnectionOracle(string Server, string ID, string PW, int Timeout, IReadOnlyDictionary<string, string> Param) : base(Server, ID, PW, null, Timeout, Param)
        {
            this.Param = Param;
            manager = new DBManagerOracle(this);
            Connector = new OracleConnection(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }

        public override string ConnectionString
        {
            get 
            {
                var sqlBuilder = new OracleConnectionStringBuilder
                {
                    DataSource = Server,
                    UserID = ID,
                    Password = PW,
                    
                    ConnectionTimeout = Timeout,
                };
                foreach (var pair in Param) sqlBuilder.Add(pair.Key, pair.Value);
                return sqlBuilder.ToString();
            }
        }
        public OracleConnection Connector { get; private set; }

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
        internal override void Close() { try { Connector?.Close(); } catch { } }

        public static explicit operator OracleConnection(DBConnectionOracle connection) { return connection.Connector; }


        #region Connect
        public static DBManagerOracle ConnectTNS(string Server, string TNSDirectory, int Port, string ID, string PW, int Timeout)
        {
            return ConnectRaw(Server, ID, PW, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "TnsAdmin", TNSDirectory },
            });
        }
        public static DBManagerOracle ConnectSID(string Server, string SID, int Port, string ID, string PW, int Timeout)
        {
            return ConnectRaw(Server, ID, PW, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SID", SID },
            });
        }
        public static DBManagerOracle ConnectRaw(string Server, string ID, string PW, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            var conn = new DBConnectionOracle(Server, ID, PW, Timeout, Param);
            return (DBManagerOracle)conn.Open();
        }

        public static async Task<DBManagerOracle> ConnectTNSAsync(string Server, string TNSDirectory, int Port, string ID, string PW, int Timeout)
        {
            return await ConnectRawAsync(Server, ID, PW, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "TnsAdmin", TNSDirectory },
            });
        }
        public static async Task<DBManagerOracle> ConnectSIDAsync(string Server, string SID, int Port, string ID, string PW, int Timeout)
        {
            return await ConnectRawAsync(Server, ID, PW, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SID", SID },
            });
        }
        public static async Task<DBManagerOracle> ConnectRawAsync(string Server, string ID, string PW, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionOracle conn = new DBConnectionOracle(Server, ID, PW, Timeout, Param);
            return (DBManagerOracle)await conn.OpenAsync();
        }
        #endregion
    }
}
