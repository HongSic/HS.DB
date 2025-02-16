using HS.DB.Connection;
using HS.DB.Manager;
using HS.DB.Param;
using HS.DB.Result;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Command
{
    public class DBCommandOracle : DBCommand
    {
        public DBManagerOracle Manager { get; private set; }
        public OracleCommand Command { get; private set; }

        public override string SQLQuery => Command.CommandText;

        public override CommandType CommandType { get => Command.CommandType; set => Command.CommandType = value; }

        public DBCommandOracle(DBManagerOracle Manager, string SQLQuery)
        {
            this.Manager = Manager;
            var conn = (OracleConnection)(DBConnectionOracle)Manager.Connector;
            Command = new OracleCommand(SQLQuery, conn);
        }

        public override DBCommand Add(DBParam Param) { Command.Parameters.Add((OracleParameter)(DBParamOracle)Param); return this; }
        public override DBCommand Add(object Value)
        {
            Command.Parameters.Add(Value);
            return this;
        }
        public override DBCommand Add(string Key, object Value)
        {
            Command.Parameters.Add(new OracleParameter(Key, Value));
            return this;
        }
        public DBCommand Add(string Key, object Value, OracleDbType Type)
        {
            Command.Parameters.Add(Key, Type);
            Command.Parameters[Key].Value = Value;
            return this;
        }

        public override DBResult Excute() { using (Command) return new DBResultOracle(Command.ExecuteReader()); }
        public override async Task<DBResult> ExcuteAsync() { using (Command) return new DBResultOracle((OracleDataReader)await Command.ExecuteReaderAsync()); }

        public override int ExcuteNonQuery() { using (Command) return Command.ExecuteNonQuery(); }
        public override async Task<int> ExcuteNonQueryAsync() { using (Command) return await Command.ExecuteNonQueryAsync(); }

        public override object ExcuteOnce() { using (Command) return Command.ExecuteScalar(); }
        public override async Task<object> ExcuteOnceAsync() { using (Command) return await Command.ExecuteScalarAsync(); }

        public override void Dispose(){ Command.Dispose(); }
    }
}
