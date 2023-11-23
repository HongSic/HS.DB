using HS.DB.Command;
using HS.DB.Connection;
using HS.DB.Result;
using HS.DB.Param;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data;

namespace HS.DB.Manager
{
    public class DBManagerMySQL : DBManager
    {
        private DBConnectionMySQL conn;
        public override DBConnection Connector { get { return conn; } }
        public override DBConnectionKind Kind { get { return DBConnectionKind.MySQL; } }

        public override char StatementPrefix => '@';
        public override string GetQuote(string Keyword) => $"`{Keyword}`";

        internal DBManagerMySQL(DBConnectionMySQL Connector)
        {
            conn = Connector;
        }
        public override DBCommand Prepare(string SQLQuery) { return new DBCommandMySQL(this, SQLQuery); }

        #region Transaction
        private MySqlTransaction _Transaction;
        public override void StartTransaction() { _Transaction = conn.Connector.BeginTransaction(); }
        public override void StartTransaction(IsolationLevel isolation) { _Transaction = conn.Connector.BeginTransaction(isolation); }
        public override void CommitTransaction() { _Transaction?.Commit(); _Transaction?.Dispose(); _Transaction = null; }
        public override void RollbackTransaction() { _Transaction?.Rollback(); _Transaction?.Dispose(); _Transaction = null; }
        public override DbTransaction Transaction => _Transaction;
        #endregion

        #region ExcuteArea
        public override DBResult Excute(string SQLQuery, params DBParam[] param) { return new DBResultMySQL(Build(SQLQuery, param)); }
        public override async Task<DBResult> ExcuteAsync(string SQLQuery, params DBParam[] param) { return await Task.Run(() => new DBResultMySQL(Build(SQLQuery, param))); }
        public override int ExcuteNonQuery(string SQLQuery, params DBParam[] param) { return ExcuteRawNonQuery(SQLQuery, param as DBParamMySQL[]); }
        public override async Task<int> ExcuteNonQueryAsync(string SQLQuery, params DBParam[] param) { return await ExcuteRawNonQueryAsync(SQLQuery, param); }

        public override object ExcuteOnce(string SQLQuery, params DBParam[] param) 
        {
            MySqlCommand builder = null;
            try
            {
                builder = CommandBuilder(conn.Connector, SQLQuery, param);
                return builder.ExecuteScalar();
            }
            finally { builder.Dispose(); }
        }
        public override async Task<object> ExcuteOnceAsync(string SQLQuery, params DBParam[] param)
        {
            MySqlCommand builder = null;
            try
            {
                builder = CommandBuilder(conn.Connector, SQLQuery, param);
                return await builder.ExecuteScalarAsync();
            }
            finally { builder.Dispose(); }
        }

        #region ExcuteRaw
        public MySqlCommand Build(string SQLQuery, params DBParam[] param) { return CommandBuilder(conn.Connector, SQLQuery, param); }
        //public async Task<MySqlDataReader> ExcuteRawAsync(string SQLQuery, params DBParam[] param) { return await CommandBuilder(conn.Connector, SQLQuery, param).ExecuteReaderAsync() as MySqlDataReader; }
        #endregion

        #region ExcuteRawAsync
        public int ExcuteRawNonQuery(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) return cmd.ExecuteNonQuery(); }
        public async Task<int> ExcuteRawNonQueryAsync(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) return await cmd.ExecuteNonQueryAsync(); }
        #endregion
        #endregion

        public override string GetJSON(string SQLQuery, bool Bracket = true, params DBParam[] param)
        {
            throw new System.NotImplementedException();
        }

        public static MySqlCommand CommandBuilder(MySqlConnection Connect, string SQLQuery, params DBParam[] Params)
        {
            var cmd = new MySqlCommand(SQLQuery, Connect);
            for (int i = 0; Params != null && i < Params.Length; i++) cmd.Parameters.Add((MySqlParameter)(DBParamMySQL)Params[i]);
            return cmd;
        }

        public override void Dispose() { conn.Close(); }
    }
}
