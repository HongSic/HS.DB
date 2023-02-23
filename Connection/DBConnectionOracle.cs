using HS.DB.Manager;
using HS.Utils.Text;
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
        private DBConnectionOracle(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param) : base(Server, ID, PW, DB, Timeout, Param)
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
                int.TryParse(GetParam("Port"), out int Port);
                string ConnectData = null;
                if (!string.IsNullOrEmpty(GetParam("SID"))) ConnectData = string.Format("(CONNECT_DATA =(SID = {0}))", GetParam("SID"));
                else if (!string.IsNullOrEmpty("SERVICE_NAME")) ConnectData = string.Format("(CONNECT_DATA =(SERVICE_NAME = {0}))", GetParam("SERVICE_NAME"));

                string dataSourceFormat = string.Format(@"(DESCRIPTION =(ADDRESS_LIST =(ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1}))){2})",
                    Server,
                    Port < 1 ? 1521 : Port,
                    ConnectData);

                var sqlBuilder = new OracleConnectionStringBuilder
                {
                    DataSource = dataSourceFormat,
                    UserID = ID,
                    Password = PW,

                    ConnectionTimeout = Timeout,
                };
                foreach (var pair in Param)
                    if (pair.Key.ToUpper() != "PORT" && pair.Key.ToUpper() != "SID" && pair.Key.ToUpper() != "SERVICE_NAME") sqlBuilder.Add(pair.Key.ToUpper(), pair.Value);
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
        public override string ServerVersion => Connector.ServerVersion;


        public override DBManager Open()
        {
            Connector.Open();
            if(!StringUtils.IsNullOrWhiteSpace(DB))
            {
                var cm = Connector.CreateCommand();
                cm.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {DB}";
                cm.ExecuteNonQuery();
            }
            return manager;
        }
        public override async Task<DBManager> OpenAsync()
        {
            await Connector.OpenAsync();
            if (!StringUtils.IsNullOrWhiteSpace(DB))
            {
                var cm = Connector.CreateCommand();
                cm.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {DB}";
                await cm.ExecuteNonQueryAsync();
            }
            return manager;
        }
        internal override void Close() { try { Connector?.Close(); } catch { } }

        public static explicit operator OracleConnection(DBConnectionOracle connection) { return connection.Connector; }


        #region Connect
        public static DBManagerOracle ConnectTNS(string Server, string TNSDirectory, int Port, string ID, string PW, string DB, int Timeout)
        {
            return ConnectRaw(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "TnsAdmin", TNSDirectory },
            });
        }
        public static DBManagerOracle ConnectSID(string Server, string SID, int Port, string ID, string PW, string DB, int Timeout)
        {
            return ConnectRaw(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SID", SID },
            });
        }
        public static DBManagerOracle ConnectServiceName(string Server, string ServiceName, int Port, string ID, string PW, string DB, int Timeout)
        {
            return ConnectRaw(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SERVICE_NAME", ServiceName },
            });
        }
        public static DBManagerOracle ConnectRaw(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            var conn = new DBConnectionOracle(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerOracle)conn.Open();
        }

        public static async Task<DBManagerOracle> ConnectTNSAsync(string Server, string TNSDirectory, int Port, string ID, string PW, string DB, int Timeout)
        {
            return await ConnectRawAsync(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "TnsAdmin", TNSDirectory },
            });
        }
        public static async Task<DBManagerOracle> ConnectSIDAsync(string Server, string SID, int Port, string ID, string PW, string DB, int Timeout)
        {
            return await ConnectRawAsync(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SID", SID },
            });
        }
        public static async Task<DBManagerOracle> ConnectServiceNameAsync(string Server, string ServiceName, int Port, string ID, string PW, string DB, int Timeout)
        {
            return await ConnectRawAsync(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
                { "SERVICE_NAME", ServiceName },
            });
        }
        public static async Task<DBManagerOracle> ConnectRawAsync(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionOracle conn = new DBConnectionOracle(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerOracle)await conn.OpenAsync();
        }
        #endregion
    }
}
