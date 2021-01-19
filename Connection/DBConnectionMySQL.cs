using HS.DB.Manager;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Connection
{
    public class DBConnectionMySQL : DBConnection
    {
        DBManagerMySQL manager;
        public DBConnectionMySQL(string Server, string ID, string PW, string DB, int Timeout) : base(Server, ID, PW, DB, Timeout)
        {
            manager = new DBManagerMySQL(this);
            Connector = new MySqlConnection(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }

        public override string ConnectionString { get { return string.Format(@"Server={0};uid={1};pwd={2};database={3};timeout={4}", Server, ID, PW, DB, Timeout); } }
        public MySqlConnection Connector { get; private set; }

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

        public static explicit operator MySqlConnection(DBConnectionMySQL connection) { return connection.Connector; }


        #region Connect
        public static DBManagerMySQL Connect(string Server, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, ID, PW, DB, Timeout);
            return (DBManagerMySQL)conn.Open();
        }
        public static async Task<DBManagerMySQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, ID, PW, DB, Timeout);
            return (DBManagerMySQL)await conn.OpenAsync();
        }
        #endregion
    }
}
