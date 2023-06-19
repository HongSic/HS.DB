#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Connection;
using HS.DB.Result;
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
            var conn = (SqlConnection)(DBConnectionMSSQL)Manager.Connector;
            var transaction = Manager.IsTransactionMode ? (SqlTransaction)Manager.Transaction : null;
            Command = new SqlCommand(SQLQuery, conn, transaction);
        }

        public override DBCommand Add(DBParam Param) { Command.Parameters.Add((SqlParameter)(DBParamMSSQL)Param); return this; }


        #pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete]
        public override DBCommand Add(object Value)
        {
            Command.Parameters.Add(Value);
            return this;
        }
        public override DBCommand Add(string Key, object Value)
        {
            Command.Parameters.Add(new SqlParameter(Key, Value));
            return this;
        }

        public DBCommand Add(string Key, object Value, SqlDbType Type)
        {
            Command.Parameters.Add(Key, Type);
            Command.Parameters[Key].Value = Value;
            return this;
        }

        public override DBResult Excute() { using (Command) return new DBResultMSSQL(Command.ExecuteReader()); }
        public override async Task<DBResult> ExcuteAsync() { using (Command) return new DBResultMSSQL(await Command.ExecuteReaderAsync()); }

        public override int ExcuteNonQuery() { using (Command) return Command.ExecuteNonQuery(); }
        public override async Task<int> ExcuteNonQueryAsync() { using (Command) return await Command.ExecuteNonQueryAsync(); }

        public override object ExcuteOnce() { using (Command) return Command.ExecuteScalar(); }
        public override async Task<object> ExcuteOnceAsync() { using (Command) return await Command.ExecuteScalarAsync(); }

        public override void Dispose(){ Command.Dispose(); }
    }
}
