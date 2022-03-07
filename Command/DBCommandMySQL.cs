using HS.DB.Connection;
using HS.DB.Data;
using HS.DB.Manager;
using HS.DB.Param;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace HS.DB.Command
{
    public class DBCommandMySQL : DBCommand
    {
        public DBManagerMySQL Manager { get; private set; }
        public MySqlCommand Command { get; private set; }

        public string SQLQuery { get; private set; }

        public DBCommandMySQL(DBManagerMySQL Manager, string SQLQuery)
        {
            this.Manager = Manager;
            Command = new MySqlCommand(SQLQuery, (MySqlConnection)(DBConnectionMySQL)Manager.Connector);
        }

        public override DBCommand Add(DBParam Param) { Command.Parameters.Add((MySqlParameter)(DBParamMySQL)Param); return this; }
        public override DBCommand Add(object Value)
        {
            Command.Parameters.Add(Value);
            return this;
        }
        public DBCommand Add(string Key, object Value, MySqlDbType Type)
        {
            Command.Parameters.Add(Key, Type);
            Command.Parameters[Key].Value = Value;
            return this;
        }

        public override DBData Excute() { using (Command) return new DBDataMySQL(Command.ExecuteReader()); }
        public override async Task<DBData> ExcuteAsync() { using (Command) return new DBDataMySQL((MySqlDataReader) await Command.ExecuteReaderAsync()); }

        public override int ExcuteNonQuery() { using (Command) return Command.ExecuteNonQuery(); }
        public override async Task<int> ExcuteNonQueryAsync() { using (Command) return await Command.ExecuteNonQueryAsync(); }

        public override object ExcuteOnce() { using (Command) return Command.ExecuteScalar(); }
        public override async Task<object> ExcuteOnceAsync() { using (Command) return await Command.ExecuteScalarAsync(); }

        public override void Dispose(){ Command.Dispose(); }
    }
}
