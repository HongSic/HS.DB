#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Connection;
using HS.DB.Data;
using HS.DB.Manager;
using HS.DB.Param;
using System.Data;
using System.Threading.Tasks;
using System;

namespace HS.DB.Command
{
    public class DBCommandMSSQL : DBCommand
    {
        public DBManagerMSSQL Manager { get; private set; }
        public SqlCommand Command { get; private set; }

        public string SQLQuery { get; private set; }

        public DBCommandMSSQL(DBManagerMSSQL Manager, string SQLQuery)
        {
            this.Manager = Manager;
            Command = new SqlCommand(SQLQuery, (SqlConnection)(DBConnectionMSSQL)Manager.Connector);
        }

        public override DBCommand Add(DBParam Param) { Command.Parameters.Add((SqlParameter)(DBParamMSSQL)Param); return this; }

        [Obsolete]
        public override DBCommand Add(object Value)
        {
            Command.Parameters.Add(Value);
            return this;
        }

        public DBCommand Add(string Key, object Value, SqlDbType Type)
        {
            Command.Parameters.Add(Key, Type);
            Command.Parameters[Key].Value = Value;
            return this;
        }

        public override DBData Excute() { using (Command) return new DBDataMSSQL(Command.ExecuteReader()); }
        public override async Task<DBData> ExcuteAsync() { using (Command) return new DBDataMSSQL(await Command.ExecuteReaderAsync()); }

        public override int ExcuteNonQuery() { using (Command) return Command.ExecuteNonQuery(); }
        public override async Task<int> ExcuteNonQueryAsync() { using (Command) return await Command.ExecuteNonQueryAsync(); }

        public override object ExcuteOnce() { using (Command) return Command.ExecuteScalar(); }
        public override async Task<object> ExcuteOnceAsync() { using (Command) return await Command.ExecuteScalarAsync(); }

        public override void Dispose(){ Command.Dispose(); }
    }
}
