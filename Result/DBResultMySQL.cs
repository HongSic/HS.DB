using MySql.Data.MySqlClient;

namespace HS.DB.Result
{
    public class DBResultMySQL : DBResultSQL
    {
        protected DBResultMySQL() { }
        public DBResultMySQL(MySqlCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBResultMySQL(MySqlDataReader Reader) : base(Reader) { this.Reader = Reader; }

        public new MySqlCommand Command { get; protected set; }
        public new MySqlDataReader Reader { get; protected set; }
    }
}
