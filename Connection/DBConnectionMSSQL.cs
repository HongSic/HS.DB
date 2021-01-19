#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Manager;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Connection
{
    //접속 이슈 해결!!
    //https://github.com/dotnet/runtime/issues/17719
    //서버에 SQL Server 2008 SP3 로 업그레이드하면서 작동함
    public class DBConnectionMSSQL : DBConnection
    {
        DBManagerMSSQL manager;
        private DBConnectionMSSQL(string Server, string ID, string PW, string DB, int Timeout) : base(Server, ID, PW, DB, Timeout)
        {
            manager = new DBManagerMSSQL(this);
            Connector = new SqlConnection(ConnectionString);
            //System.Console.WriteLine(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }

        //public override string ConnectionString { get { return string.Format(@"Server={0};uid={1};pwd={2};database={3};timeout={4};Pooling=False;Persist Security Info=True;TrustServerCertificate=False;", Server, ID, PW, DB, Timeout); } }
        public override string ConnectionString { get { return string.Format(@"Data Source={0};UID={1};PWD={2};DATABASE={3};TIMEOUT={4};TrustServerCertificate=true", Server, ID, PW, DB, Timeout); } }
        public SqlConnection Connector { get; private set; }

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

        public static explicit operator SqlConnection(DBConnectionMSSQL connection) { return connection.Connector; }

        #region Connect
        public static DBManagerMSSQL Connect(string Server, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMSSQL conn = new DBConnectionMSSQL(Server, ID, PW, DB, Timeout);
            return (DBManagerMSSQL)conn.Open();
        }
        public static async Task<DBManagerMSSQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMSSQL conn = new DBConnectionMSSQL(Server, ID, PW, DB, Timeout);
            return (DBManagerMSSQL)await conn.OpenAsync();
        }
        #endregion
    }
}
