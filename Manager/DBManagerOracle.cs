using HS.DB.Command;
using HS.DB.Connection;
using HS.DB.Result;
using HS.DB.Param;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;

namespace HS.DB.Manager
{
    public class DBManagerOracle : DBManager
    {
        private DBConnectionOracle conn;
        public override DBConnection Connector { get { return conn; } }
        public override DBConnectionKind Kind { get { return DBConnectionKind.MySQL; } }

        public override char StatementPrefix => ':';
        public override string GetQuote(string Keyword) => EnableQuote ? $"\"{Keyword}\"" : Keyword;

        /// <summary>
        /// Turn off Quote when Oracle build SQL query
        /// </summary>
        public bool EnableQuote { get; set; } = false;

        internal DBManagerOracle(DBConnectionOracle Connector)
        {
            conn = Connector;
        }
        public override DBCommand Prepare(string SQLQuery) { return new DBCommandOracle(this, SQLQuery); }

        #region Transaction
        private OracleTransaction _Transaction;
        public override void StartTransaction() { _Transaction = conn.Connector.BeginTransaction(); }
        public override void StartTransaction(IsolationLevel isolation) { _Transaction = conn.Connector.BeginTransaction(isolation); }
        public override void CommitTransaction() { _Transaction?.Commit(); _Transaction?.Dispose(); _Transaction = null; }
        public override void RollbackTransaction() { _Transaction?.Rollback(); _Transaction?.Dispose(); _Transaction = null; }
        public override DbTransaction Transaction => _Transaction;
        #endregion

        #region ExcuteArea
        public override DBResult Excute(string SQLQuery, params DBParam[] param) { return new DBResultOracle(Build(SQLQuery, param)); }
        public override async Task<DBResult> ExcuteAsync(string SQLQuery, params DBParam[] param) { return await Task.Run(() => new DBResultOracle(Build(SQLQuery, param))); }
        public override int ExcuteNonQuery(string SQLQuery, params DBParam[] param) { return ExcuteRawNonQuery(SQLQuery, param as DBParamOracle[]); }
        public override async Task<int> ExcuteNonQueryAsync(string SQLQuery, params DBParam[] param) { return await ExcuteRawNonQueryAsync(SQLQuery, param); }

        public override object ExcuteOnce(string SQLQuery, params DBParam[] param) 
        {
            OracleCommand builder = null;
            try
            {
                builder = CommandBuilder(conn.Connector, SQLQuery, param);
                return builder.ExecuteScalar();
            }
            finally { builder.Dispose(); }
        }
        public override async Task<object> ExcuteOnceAsync(string SQLQuery, params DBParam[] param)
        {
            OracleCommand builder = null;
            try
            {
                builder = CommandBuilder(conn.Connector, SQLQuery, param);
                return await builder.ExecuteScalarAsync();
            }
            finally { builder.Dispose(); }
        }

        #region ExcuteRaw
        public OracleCommand Build(string SQLQuery, params DBParam[] param) { return CommandBuilder(conn.Connector, SQLQuery, param); }
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

        public static OracleCommand CommandBuilder(OracleConnection Connect, string SQLQuery, params DBParam[] Params)
        {
            var cmd = new OracleCommand(SQLQuery, Connect);
            for (int i = 0; Params != null && i < Params.Length; i++) cmd.Parameters.Add((OracleParameter)(DBParamOracle)Params[i]);
            return cmd;
        }

        public override void Dispose() { conn.Close(); }
    }
}
