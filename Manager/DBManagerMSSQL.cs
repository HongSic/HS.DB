﻿#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Command;
using HS.DB.Connection;
using HS.DB.Result;
using HS.DB.Param;
using HS.Utils;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;

namespace HS.DB.Manager
{
    public class DBManagerMSSQL : DBManager
    {
        private DBConnectionMSSQL conn;

        public override DBConnection Connector { get { return conn; } }
        public override DBConnectionKind Kind { get { return DBConnectionKind.MSSQL; } }

        public override string GetQuote(string Keyword) => $"[{Keyword}]";
        /// <summary>
        /// MSSQL(2016~)
        /// </summary>
        /// <param name="SQLQuery"></param>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public override string ApplyLimitBuild(string SQLQuery, int Offset, int Count) => Count > 0 ? $"{SQLQuery} OFFSET {Offset} ROWS FETCH NEXT {Count} ROWS ONLY" : SQLQuery;

        internal DBManagerMSSQL(DBConnectionMSSQL Connector)
        {
            conn = Connector;
        }

        public override DBCommand Prepare(string SQLQuery) { return new DBCommandMSSQL(this, SQLQuery); }

        #region Transaction
        private SqlTransaction _Transaction;
        public override void StartTransaction() { _Transaction = conn.Connector.BeginTransaction(); }
        public override void StartTransaction(IsolationLevel isolation) { _Transaction = conn.Connector.BeginTransaction(isolation); }
        public override void CommitTransaction() { _Transaction?.Commit(); _Transaction?.Dispose(); _Transaction = null; }
        public override void RollbackTransaction() { _Transaction?.Rollback(); _Transaction?.Dispose(); _Transaction = null; }
        public override DbTransaction Transaction => _Transaction;
        #endregion


        #region ExcuteArea
        public override DBResult Excute(string SQLQuery, params DBParam[] param) { return new DBResultMSSQL(ExcuteRaw(SQLQuery, param)); }
        public override async Task<DBResult> ExcuteAsync(string SQLQuery, params DBParam[] param) { return new DBResultMSSQL(await ExcuteRawAsync(SQLQuery, param)); }
        public override int ExcuteNonQuery(string SQLQuery, params DBParam[] param) { return ExcuteRawNonQuery(SQLQuery, param); }
        public override async Task<int> ExcuteNonQueryAsync(string SQLQuery, params DBParam[] param) { return await ExcuteRawNonQueryAsync(SQLQuery, param); }

        public override object ExcuteOnce(string SQLQuery, params DBParam[] param)
        {
            using (var reader = ExcuteRaw(SQLQuery, param))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    return reader.GetValue(0);
                }
                else return null;
            }
        }

        public override async Task<object> ExcuteOnceAsync(string SQLQuery, params DBParam[] param)
        {
            using (var reader = await ExcuteRawAsync(SQLQuery, param))
            {
                if (reader.HasRows)
                {
                    await reader.ReadAsync();
                    return reader.GetValue(0);
                }
                else return null;
            }
        }

        #region ExcuteRaw
        public SqlDataReader ExcuteRaw(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) { var a = cmd.ExecuteReader(); return a; } }
        public async Task<SqlDataReader> ExcuteRawAsync(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) return await cmd.ExecuteReaderAsync(); }
        #endregion

        #region ExcuteRawAsync
        public int ExcuteRawNonQuery(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) return cmd.ExecuteNonQuery(); }
        public async Task<int> ExcuteRawNonQueryAsync(string SQLQuery, params DBParam[] param) { using (var cmd = CommandBuilder(conn.Connector, SQLQuery, param)) return await cmd.ExecuteNonQueryAsync(); }
        #endregion
        #endregion

        public override string GetJSON(string SQLQuery, bool Bracket = true, params DBParam[] param)
        {
            var cmd = CommandBuilder((SqlConnection)(DBConnectionMSSQL)Connector, SQLQuery, param);
            using (var reader = cmd.ExecuteReader()) return DBUtils.ToJSON(reader, Bracket);
        }

        public static SqlCommand CommandBuilder(SqlConnection Connect, string SQLQuery, params DBParam[] Params)
        {
            var cmd = new SqlCommand(SQLQuery, Connect);
            for (int i = 0; Params != null && i < Params.Length; i++)
            {
                cmd.Parameters.Add(Params[i].Key, ((DBParamMSSQL)Params[i]).Type);
                cmd.Parameters[Params[i].Key].Value = Params[i].Value;
            }
            return cmd;
        }

        public override void Dispose() { conn.Close(); }
    }
}
