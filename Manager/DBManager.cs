#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Command;
using HS.DB.Data;
using System;
using System.Threading.Tasks;

namespace HS.DB
{
    public abstract class DBManager : IDisposable
    {
        public abstract DBConnection Connector { get; }
        public abstract DBConnectionKind Kind { get; }

        public abstract DBCommand Prepare(string SQLQuery);

        public abstract void StartTransaction();
        public abstract void EndTransaction(bool Commit = true);
        public abstract bool IsTransactionMode { get; }

        #region ExcuteArea
        #region Excute
        public virtual DBData Excute(string command) { return Excute(command, null); }
        public abstract DBData Excute(string SQLQuery, params DBParam[] param);
        #endregion

        #region ExcuteAsync
        public virtual async Task<DBData> ExcuteAsync(string command) { return await ExcuteAsync(command, null); }
        public abstract Task<DBData> ExcuteAsync(string SQLQuery, params DBParam[] param);
        #endregion

        #region ExcuteNonQuery
        public virtual int ExcuteNonQuery(string command) { return ExcuteNonQuery(command, null); }
        public abstract int ExcuteNonQuery(string SQLQuery, params DBParam[] param);
        #endregion

        #region ExcuteNonQueryAsync
        public virtual async Task<int> ExcuteNonQueryAsync(string command) { return await ExcuteNonQueryAsync(command, null); }
        public abstract Task<int> ExcuteNonQueryAsync(string SQLQuery, params DBParam[] param);
        #endregion

        #region ExcuteOnce
        public virtual object ExcuteOnce(string command) { return ExcuteOnce(command, null); }
        public abstract object ExcuteOnce(string SQLQuery, params DBParam[] param);
        #endregion

        #region ExcuteOnceAsync
        public virtual async Task<object> ExcuteOnceAsync(string command) { return await ExcuteOnceAsync(command, null); }
        public abstract Task<object> ExcuteOnceAsync(string SQLQuery, params DBParam[] param);
        #endregion
        #endregion

        #region GetJSON
        public virtual string GetJSON(string command, bool Bracket = true) { return GetJSON(command, Bracket, null); }
        public abstract string GetJSON(string SQLQuery, bool Bracket = true, params DBParam[] param);
        #endregion

        public virtual void Dispose() { Connector?.Close(); }
    }
}
