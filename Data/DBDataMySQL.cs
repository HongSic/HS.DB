using MySql.Data.MySqlClient;

namespace HS.DB.Data
{
    public class DBDataMySQL : DBDataSQL
    {
        protected DBDataMySQL() { }
        public DBDataMySQL(MySqlCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBDataMySQL(MySqlDataReader Reader) : base(Reader) { this.Reader = Reader; }

        public new MySqlCommand Command { get; protected set; }
        public new MySqlDataReader Reader { get; protected set; }
    }
}
