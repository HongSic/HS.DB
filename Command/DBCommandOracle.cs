using HS.DB.Connection;
using HS.DB.Data;
using HS.DB.Manager;
using HS.DB.Param;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Command
{
    public class DBCommandOracle : DBCommand
    {
        public DBManagerOracle Manager { get; private set; }
        public OracleCommand Command { get; private set; }

        public string SQLQuery { get; private set; }

        public DBCommandOracle(DBManagerOracle Manager, string SQLQuery)
        {
            this.Manager = Manager;
            Command = new OracleCommand(SQLQuery, (OracleConnection)(DBConnectionOracle)Manager.Connector);
        }

        public override DBCommand Add(DBParam Param) { Command.Parameters.Add((OracleParameter)(DBParamOracle)Param); return this; }
        public DBCommand Add(string Key, object Value, OracleDbType Type)
        {
            Command.Parameters.Add(Key, Type);
            Command.Parameters[Key].Value = Value;
            return this;
        }

        public override DBData Excute() { using (Command) return new DBDataOracle(Command.ExecuteReader()); }
        public override async Task<DBData> ExcuteAsync() { using (Command) return new DBDataOracle((OracleDataReader)await Command.ExecuteReaderAsync()); }

        public override int ExcuteNonQuery() { using (Command) return Command.ExecuteNonQuery(); }
        public override async Task<int> ExcuteNonQueryAsync() { using (Command) return await Command.ExecuteNonQueryAsync(); }

        public override object ExcuteOnce() { using (Command) return Command.ExecuteScalar(); }
        public override async Task<object> ExcuteOnceAsync() { using (Command) return await Command.ExecuteScalarAsync(); }

        public override void Dispose(){ Command.Dispose(); }
    }
}
