using HS.DB.Manager;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace HS.DB.Connection
{
    public class DBConnectionMySQL : DBConnection
    {
        public const int PORT = 3306;
        DBManagerMySQL manager;
        public DBConnectionMySQL(string Server, string ID, string PW, string DB, int Timeout) : this(Server, PORT, ID, PW, DB, Timeout) { }
        public DBConnectionMySQL(string Server, int Port, string ID, string PW, string DB, int Timeout) : base(Server, ID, PW, DB, Timeout)
        {
            this.Port = Port;
            manager = new DBManagerMySQL(this);
            Connector = new MySqlConnection(ConnectionString);
            Connector.StateChange += (object sender, StateChangeEventArgs e) => OnDBStatusChanged(this, Status);
        }
        public int Port { get; private set; }

        public override string ConnectionString
        {
            get 
            {
                var sqlBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = Server,
                    
                    UserID = ID,
                    Password = PW,
                    InitialCatalog = DB,
                    ConnectTimeout = Timeout,
                }; 
                return string.Format("{0};Port={1}", sqlBuilder.ToString(), Port); 
            }
        }
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
        public static DBManagerMySQL Connect(string Server, string ID, string PW, string DB, int Timeout) { return Connect(Server, PORT, ID, PW, DB, Timeout); }
        public static DBManagerMySQL Connect(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, Port, ID, PW, DB, Timeout);
            return (DBManagerMySQL)conn.Open();
        }
        public static async Task<DBManagerMySQL> ConnectAsync(string Server, string ID, string PW, string DB, int Timeout) { return await ConnectAsync(Server, PORT, ID, PW, DB, Timeout); }
        public static async Task<DBManagerMySQL> ConnectAsync(string Server, int Port, string ID, string PW, string DB, int Timeout)
        {
            DBConnectionMySQL conn = new DBConnectionMySQL(Server, Port, ID, PW, DB, Timeout);
            return (DBManagerMySQL)await conn.OpenAsync();
        }
        #endregion
    }
}
