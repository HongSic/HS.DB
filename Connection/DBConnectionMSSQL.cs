#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Manager;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using HS.Utils.Text;

namespace HS.DB.Connection
{
    //접속 이슈 해결!!
    //https://github.com/dotnet/runtime/issues/17719
    //서버에 SQL Server 2008 SP3 로 업그레이드하면서 작동함
    public class DBConnectionMSSQL : DBConnection
    {
        public const int PORT = 1433;
        public const string KIND = "MSSQL";

        DBManagerMSSQL manager;
        private DBConnectionMSSQL(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param) : base(Server, ID, PW, DB, Timeout, Param)
        {
            this.Param = Param;
            manager = new DBManagerMSSQL(this);
            Connector = new SqlConnection(ConnectionString);
            //System.Console.WriteLine(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }


        public override string ConnectionString
        {
            get
            {
                int.TryParse(GetParam("Port"), out int port);
                port = port < 1 ? PORT : port;
                var sqlBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = $"{Server},{port}",
                    UserID = ID,
                    Password = PW,
                    ConnectTimeout = Timeout,
                    TrustServerCertificate = true
                };
                if (!StringUtils.IsNullOrWhiteSpace(DB)) sqlBuilder.InitialCatalog = DB;
                if (Param != null)
                {
                    foreach (var pair in Param)
                        if (!string.Equals(pair.Key, "Port", StringComparison.InvariantCultureIgnoreCase)) sqlBuilder.Add(pair.Key, pair.Value);
                }
                return sqlBuilder.ToString();
            }
        }

        public SqlConnection Connector { get; private set; }

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

        public static implicit operator SqlConnection(DBConnectionMSSQL connection) { return connection.Connector; }
        public static implicit operator DBConnectionMSSQL(SqlConnection connector) { return FromConnector(connector); }

        public static DBConnectionMSSQL FromConnector(SqlConnection connector)
        {
            if (connector == null) return null;

            var builder = new SqlConnectionStringBuilder(connector.ConnectionString);
            var param = DBConnectionUtils.CreateParamDictionary();
            DBConnectionUtils.AddParam(param, "Port", DBConnectionUtils.ExtractPort(builder.DataSource, PORT));

            foreach (var key in builder.Keys)
            {
                var name = key == null ? null : key.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (DBConnectionUtils.IsKey(name, "Data Source", "Server", "User ID", "UserID", "Password", "Initial Catalog", "Database", "Connect Timeout")) continue;
                DBConnectionUtils.AddParam(param, name, builder[name]?.ToString());
            }

            return new DBConnectionMSSQL(DBConnectionUtils.ExtractHost(builder.DataSource), builder.UserID, builder.Password, builder.InitialCatalog, builder.ConnectTimeout, param);
        }

        #region Connect
        public static DBManagerMSSQL Connect(string Server, string ID, string PW, string DB, int Timeout) { return Connect(Server, PORT, ID, PW, DB, Timeout); }
        public static DBManagerMSSQL Connect(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            return Connect(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() }
            });
        }
        public static DBManagerMSSQL Connect(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionMSSQL conn = new DBConnectionMSSQL(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerMSSQL)conn.Open();
        }

        public static async Task<DBManagerMSSQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout) { return await ConnectAsync(Server, ID, PW, DB, Timeout, null); }
        public static async Task<DBManagerMSSQL> ConnectAsync(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            return await ConnectAsync(Server, ID, PW, DB, Timeout, new Dictionary<string, string>()
            {
                { "Port", Port.ToString() },
            });
        }
        public static async Task<DBManagerMSSQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout, IReadOnlyDictionary<string, string> Param)
        {
            DBConnectionMSSQL conn = new DBConnectionMSSQL(Server, ID, PW, DB, Timeout, Param);
            return (DBManagerMSSQL)await conn.OpenAsync();
        }
        #endregion
    }
}
