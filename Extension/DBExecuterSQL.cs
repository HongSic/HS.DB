using HS.DB.Command;
using HS.DB.Extension.Attributes;
using HS.DB.Result;
using HS.Utils;
using HS.Utils.Convert;
using HS.Utils.Text;
using Org.BouncyCastle.Asn1.Esf;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HS.DB.Extension
{
    public static class DBExecuterSQL
    {
        public static string GetTable<T>(this DBManager Manager) => GetTable(Manager, typeof(T));
        internal static string GetTable(DBManager Manager, Type type)
        {
            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                return attr.ToString(Manager, type.Name);

            throw new NullReferenceException($"Class \"{type.Name}\" does not have table");
        }

        internal static string SQLRawCommandBuild<T>(DBManager Manager, T Instance, string PrefixQuery, string Table, bool UseStatement, out Dictionary<string, ColumnData> columns, out WhereData where) where T : class
        {
            Type type = Instance.GetType();
            columns = GetColumns(type, Instance, out var _);
            StringBuilder sb = new StringBuilder(PrefixQuery);

            //테이블
            sb.Append(Table ?? GetTable(Manager, type));

            //조건
            where = BuildWhere(columns, UseStatement, Instance, Manager);
            if (!where.IsEmpty()) sb.Append(where.Where);

            return sb.ToString();
        }
        internal static DBCommand SQLRawCommandPrepare<T>(DBManager Manager, T Instance, string PrefixQuery, string Table) where T : class
        {
            char p = Manager.StatementPrefix;
            var query = SQLRawCommandBuild(Manager, Instance, PrefixQuery, Table, true, out var columns, out var where);
            var prepare = Manager.Prepare(query);
            for (int i = 0; i < where.Columns.Count; i++)
            {
                var column = columns[where.Columns[i]];
                prepare.Add($"{p}{where.Columns[i]}", ConvertValue(column.Column.Type, column.GetValue(Instance)));
            }

            return prepare;
        }


        /// <summary>
        /// 1보다 크면 변경됨
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="ResultCount"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        internal static int GetExcuteNonQuery(DBManager Manager, int ResultCount, bool Close)
        {
            try { return ResultCount; }
            finally { if (Close) Manager.Dispose(); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Value"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        internal static object GetValue(DBManager Manager, object Value, bool Close)
        {
            try
            {
                //값이 DBNull 이면 null 반환
                return Value == DBNull.Value ? null : Value;
            }
            finally { if (Close) Manager.Dispose(); }
        }

        #region SQL Insert
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        internal static string SQLInsertBuild<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where, bool UseStatement, out string Prefix, out Dictionary<string, object> VALUES, out ColumnWhere.Statement where) where T : class
        {
            Prefix = Manager.GetStatementPrefix();
            Type type = Instance.GetType();
            var columns = GetColumns(type, Instance, out var _);
            bool First = true;

            StringBuilder sb = new StringBuilder("INSERT INTO ");

            //테이블
            string Table = GetTable(Manager, type);
            sb.Append(Table);

            //컬럼
            foreach (var col in columns)
            {
                //col.Value.IgnoreValue == col.Value.Info.
                sb.Append(First ? " (" : ", ");
                sb.Append(Manager.GetQuote(col.Key));
                First = false;
            }

            sb.Append(") VALUES (");

            VALUES = new Dictionary<string, object>();
            List<string> keys_remove = new List<string>(columns.Count);
            //값
            First = true;
            foreach (var col in columns)
            {
                sb.Append(First ? null : ", ");
                if (col.Value.GetValue(Instance) == null)
                {
                    sb.Append("null");
                    keys_remove.Add(col.Key);
                }
                else
                {
                    if (UseStatement)
                    {
                        VALUES.Add(col.Key, ConvertValue(col.Value.Column.Type, col.Value.GetValue(Instance)));
                        sb.Append($"{Prefix}{col.Key}");
                    }
                    else sb.Append(col.Value.GetDBValue(Instance));
                }
                First = false;
            }
            sb.Append(")");
            for (int i = 0; i < keys_remove.Count; i++) columns.Remove(keys_remove[i]);

            where = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = where?.QueryString(UseStatement);
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            return sb.ToString();
        }
        internal static DBCommand SQLInsertPrepare<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where) where T : class
        {
            var builder = SQLInsertBuild(Manager, Instance, Where, true, out var p, out var VALUES, out var where);
            var prepare = Manager.Prepare(builder);
            //추가 조건절이 존재하면 할당
            where.Apply(prepare);
            //값 할당
            foreach (var item in VALUES) prepare.Add($"{p}{item.Key}", item.Value);
            return prepare;
        }
        public static int SQLInsert<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var prepare = SQLInsertPrepare(Manager, Instance, Where))
                return prepare.ExcuteNonQuery();
        }
        public static int SQLInsertUnsafe<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var query = SQLInsertBuild(Manager, Instance, Where, false, out var _, out var _, out var _);
            return Manager.ExcuteNonQuery(query);
        }
        public static async Task<int> SQLInsertAsync<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var prepare = SQLInsertPrepare(Manager, Instance, Where))
                return await prepare.ExcuteNonQueryAsync();
        }
        public static Task<int> SQLInsertAsyncUnsafe<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var query = SQLInsertBuild(Manager, Instance, Where, false, out var _, out var _, out var _);
            return Manager.ExcuteNonQueryAsync(query);
        }
        #endregion

        #region SQL Update
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        internal static string SQLUpdateBuild<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where, bool UseStatement, out string prefix, out Dictionary<string, ColumnData> columns, out ColumnWhere.Statement statement) where T : class
        {
            prefix = Manager.GetStatementPrefix();
            Type type = Instance.GetType();
            columns = GetColumns(type, Instance, out var _);
            bool First = true;

            StringBuilder sb = new StringBuilder("UPDATE ");

            //테이블
            string Table = GetTable(Manager, type);
            sb.Append(Table);

            sb.Append(" SET ");

            //컬럼
            List<string> keys_remove = new List<string>(columns.Count);
            foreach (var col in columns)
            {
                if (col.Value.Where == null)
                {
                    sb.Append(First ? null : ", ");
                    sb.Append(Manager.GetQuote(col.Key)).Append(" =");
                    if (col.Value.GetValue(Instance) == null)
                    {
                        sb.Append("null");
                        keys_remove.Add(col.Key);
                    }
                    else
                    {
                        if (UseStatement) sb.Append($"{prefix}{col.Key}");
                        else sb.Append(col.Value.GetDBValue(Instance));
                    }
                    First = false;
                }
            }
            for (int i = 0; i < keys_remove.Count; i++) columns.Remove(keys_remove[i]);

            //조건 (Primary Key)
            if (Where == null)
            {
                statement = null;
                var where = BuildWhere(columns, UseStatement, Instance, Manager);
                if (!where.IsEmpty()) sb.Append(where.Where);
            }
            else
            {
                statement = ColumnWhere.JoinForStatement(Where, Manager);
                sb.Append(" WHERE ").Append(statement?.QueryString(UseStatement));
            }

            return sb.ToString();
        }
        internal static DBCommand SQLUpdatePrepare<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var builder = SQLUpdateBuild(Manager, Instance, Where, true, out var p, out var columns, out var statement);
            var prepare = Manager.Prepare(builder);
            //추가 조건절이 존재하면 할당
            statement.Apply(prepare);
            //값 할당
            foreach (var col in columns) prepare.Add($"{p}{col.Key}", ConvertValue(col.Value.Column.Type, col.Value.GetValue(Instance)) ?? DBNull.Value);
            return prepare;
        }

        public static int SQLUpdate<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var cmd = SQLUpdatePrepare<T>(Manager, Instance, Where))
                return cmd.ExcuteNonQuery();
        }
        public static int SQLUpdateUnsafe<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var query = SQLUpdateBuild(Manager, Instance, Where, false, out var _, out var _, out var _);
            return Manager.ExcuteNonQuery(query);
        }
        public static async Task<int> SQLUpdateAsync<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var cmd = SQLUpdatePrepare<T>(Manager, Instance, Where))
                return await cmd.ExcuteNonQueryAsync();
        }
        public static Task<int> SQLUpdateAsyncUnsafe<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var query = SQLUpdateBuild(Manager, Instance, Where, false, out var _, out var _, out var _);
            return Manager.ExcuteNonQueryAsync(query);
        }
        #endregion

        #region SQL Delete
        /// <summary>
        /// 아이템 삭제
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <returns></returns>
        internal static string SQLDeleteBuild(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where, bool UseStatement, out ColumnWhere.Statement where)
        {
            where = ColumnWhere.JoinForStatement(Where, Conn);
            string where_query = where?.QueryString();
            StringBuilder sb = new StringBuilder("DELETE FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            return sb.ToString();
        }
        internal static DBCommand SQLDeletePrepare(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where)
        {
            var query = SQLDeleteBuild(Conn, Table, Where, true, out var where);
            var Stmt = Conn.Prepare(query);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        public static int SQLDelete(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = SQLDeletePrepare(Manager, Table, Where))
                return GetExcuteNonQuery(Manager, cmd.ExcuteNonQuery(), Close);
        }
        public static int SQLDelete(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDelete(Manager, Table, Where, false);
        public static int SQLDelete<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDelete(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static int SQLDelete<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            using (var cmd = SQLRawCommandPrepare(Manager, Instance, "DELETE ", GetTable(Manager, typeof(T))))
                return GetExcuteNonQuery(Manager, cmd.ExcuteNonQuery(), Close);
        }

        public static int SQLDeleteUnsafe(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SQLDeleteBuild(Manager, Table, Where, false, out var _);
            return GetExcuteNonQuery(Manager, Manager.ExcuteNonQuery(query), Close);
        }
        public static int SQLDeleteUnsafe(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDeleteUnsafe(Manager, Table, Where, false);
        public static int SQLDeleteUnsafe<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDeleteUnsafe(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static int SQLDeleteUnsafe<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            var query = SQLRawCommandBuild(Manager, Instance, "DELETE ", GetTable(Manager, typeof(T)), false, out var _, out var _);
            return GetExcuteNonQuery(Manager, Manager.ExcuteNonQuery(query), Close);
        }
        #endregion

        #region Async
        public static async Task<int> SQLDeleteAsync(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = SQLDeletePrepare(Manager, Table, Where))
                return GetExcuteNonQuery(Manager, await cmd.ExcuteNonQueryAsync(), Close);
        }
        public static Task<int> SQLDeleteAsync(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDeleteAsync(Manager, Table, Where, false);
        public static Task<int> SQLDeleteAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDeleteAsync(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<int> SQLDeleteAsync<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            using (var cmd = SQLRawCommandPrepare(Manager, Instance, "DELETE ", GetTable(Manager, typeof(T))))
                return GetExcuteNonQuery(Manager, await cmd.ExcuteNonQueryAsync(), Close);
        }

        public static async Task<int> SQLDeleteUnsafeAsync(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SQLDeleteBuild(Manager, Table, Where, false, out var _);
            return GetExcuteNonQuery(Manager, await Manager.ExcuteNonQueryAsync(query), Close);
        }
        public static Task<int> SQLDeleteUnsafeAsync(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDeleteUnsafeAsync(Manager, Table, Where, false);
        public static Task<int> SQLDeleteUnsafeAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDeleteUnsafeAsync(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<int> SQLDeleteUnsafeAsync<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            var query = SQLRawCommandBuild(Manager, Instance, "DELETE ", GetTable(Manager, typeof(T)), false, out var _, out var _);
            return GetExcuteNonQuery(Manager, await Manager.ExcuteNonQueryAsync(query), Close);
        }
        #endregion
        #endregion

        #region SQL Query
        #region SQL Query Instance
        internal static bool SQLQueryInstanceApply<T>(DBResult result, Dictionary<string, ColumnData> columns, T Instance)
        {
            using (result)
            {
                if (result.MoveNext())
                {
                    foreach (var column in columns)
                    {
                        if (result.ColumnExist(column.Value.OriginName))
                        {
                            object value = ConvertValue(column.Value.Type, result[column.Value.OriginName]);
                            column.Value.Info.SetValue(Instance, value);
                        }
                    }
                    return true;
                }
                else return false;
            }
        }
        internal static string SQLQueryInstanceBuild<T>(this DBManager Manager, T Instance, bool UseStatement, out Dictionary<string, ColumnData> columns, out WhereData where) where T : class
        {
            var p = Manager.StatementPrefix;
            Type type = Instance.GetType();
            columns = GetColumns(type, Instance, out var Sort);
            bool First = true;

            StringBuilder sb = new StringBuilder("SELECT ");

            foreach (var col in columns)
            {
                if (col.Value.Where == null)
                {
                    sb.Append(First ? null : ", ");
                    if (col.Value.OriginName == col.Key) sb.Append(Manager.GetQuote(col.Key));
                    else sb.Append(Manager.GetQuote(col.Key)).Append(" AS ").Append(Manager.GetQuote(col.Value.OriginName));
                    First = false;
                }
            }

            sb.Append(" FROM ");

            //테이블
            string Table = GetTable(Manager, type);
            sb.Append(Table);

            //조건
            where = BuildWhere(columns, UseStatement, Manager);
            if (!where.IsEmpty()) sb.Append(where.Where);

            //정렬
            if (Sort != null)
            {
                First = true;
                foreach (var sort in Sort)
                {
                    if (First) sb.Append(" ORDER BY ");
                    else sb.Append(", ");
                    sb.Append(sort.ToString(Manager));
                    First = false;
                }
            }

            return sb.ToString();
        }
        internal static DBCommand SQLQueryInstancePrepare<T>(this DBManager Manager, T Instance, out Dictionary<string, ColumnData> columns) where T : class
        {
            var p = Manager.StatementPrefix;
            var query = SQLQueryInstanceBuild(Manager, Instance, true, out columns, out var where);
            var prepare = Manager.Prepare(query);
            for (int i = 0; i < where.Columns.Count; i++)
            {
                var column = columns[where.Columns[i]];
                prepare.Add($"{p}{where.Columns[i]}", ConvertValue(column.Column.Type, column.GetValue(Instance)));
            }
            return prepare;
        }

        #region Normal
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static bool SQLQueryInstance<T>(this DBManager Manager, T Instance) where T : class
        {
            using (DBCommand cmd = SQLQueryInstancePrepare(Manager, Instance, out var columns))
                return SQLQueryInstanceApply(cmd.Excute(), columns, Instance);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static bool SQLQueryInstanceUnsafe<T>(this DBManager Manager, T Instance) where T : class
        {
            var query = SQLQueryInstanceBuild(Manager, Instance, false, out var columns, out var _);
            return SQLQueryInstanceApply(Manager.Excute(query), columns, Instance);
        }
        #endregion

        #region Async
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static async Task<bool> SQLQueryInstanceAsync<T>(this DBManager Manager, T Instance) where T : class
        {
            using (var cmd = SQLQueryInstancePrepare(Manager, Instance, out var columns))
                return SQLQueryInstanceApply(await cmd.ExcuteAsync(), columns, Instance);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static async Task<bool> SQLQueryInstanceUnsafeAsync<T>(this DBManager Manager, T Instance) where T : class
        {
            var query = SQLQueryInstanceBuild(Manager, Instance, false, out var columns, out var _);
            return SQLQueryInstanceApply(await Manager.ExcuteAsync(query), columns, Instance);
        }
        #endregion
        #endregion

        #region SQL Query Once
        #region Normal
        public static T SQLQueryOnce<T>(this DBManager Manager, params ColumnWhere[] Where) where T : class => SQLQueryOnce<T>(Manager, null, Where);
        public static T SQLQueryOnce<T>(this DBManager Manager, string Table, params ColumnWhere[] Where) where T : class
        {
            List<T> list = SQLQuery<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnce<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnce<T>(Manager, null, Where);
        public static T SQLQueryOnce<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = SQLQuery<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }

        public static T SQLQueryOnceUnsafe<T>(this DBManager Manager, params ColumnWhere[] Where) where T : class => SQLQueryOnceUnsafe<T>(Manager, null, Where);
        public static T SQLQueryOnceUnsafe<T>(this DBManager Manager, string Table, params ColumnWhere[] Where) where T : class
        {
            List<T> list = SQLQuery<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnceUnsafe<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceUnsafe<T>(Manager, null, Where);
        public static T SQLQueryOnceUnsafe<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = SQLQuery<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        #endregion

        #region Async
        public static Task<T> SQLQueryOnceAsync<T>(this DBManager Manager, params ColumnWhere[] Where) where T : class => SQLQueryOnceAsync<T>(Manager, null, Where);
        public static async Task<T> SQLQueryOnceAsync<T>(this DBManager Manager, string Table, params ColumnWhere[] Where) where T : class
        {
            List<T> list = await SQLQueryAsync<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static Task<T> SQLQueryOnceAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceAsync<T>(Manager, null, Where);
        public static async Task<T> SQLQueryOnceAsync<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = await SQLQueryAsync<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }

        public static Task<T> SQLQueryOnceUnsafeAsync<T>(this DBManager Manager, params ColumnWhere[] Where) where T : class => SQLQueryOnceUnsafeAsync<T>(Manager, null, Where);
        public static async Task<T> SQLQueryOnceUnsafeAsync<T>(this DBManager Manager, string Table, params ColumnWhere[] Where) where T : class
        {
            List<T> list = await SQLQueryUnsafeAsync<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static Task<T> SQLQueryOnceUnsafeAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceUnsafeAsync<T>(Manager, null, Where);
        public static async Task<T> SQLQueryOnceUnsafeAsync<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = await SQLQueryUnsafeAsync<T>(Manager, Table, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        #endregion
        #endregion

        #region SQL Query Once Group
        #region Normal
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceGroup<T>(Manager, null, GroupBy, Where);
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = SQLQueryGroup<T>(Manager, Table, GroupBy, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnceGroupUnsafe<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceGroupUnsafe<T>(Manager, null, GroupBy, Where);
        public static T SQLQueryOnceGroupUnsafe<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = SQLQueryGroupUnsafe<T>(Manager, Table, GroupBy, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class => SQLQueryOnceGroup<T>(Manager, null, GroupBy, Where, Offset);
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = SQLQueryGroup<T>(Manager, Table, GroupBy, Where, null, Offset, 1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnceGroupUnsafe<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class => SQLQueryOnceGroupUnsafe<T>(Manager, null, GroupBy, Where, Offset);
        public static T SQLQueryOnceGroupUnsafe<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = SQLQueryGroupUnsafe<T>(Manager, Table, GroupBy, Where, null, Offset, 1);
            return list.Count == 0 ? null : list[0];
        }
        #endregion

        #region Async
        public static Task<T> SQLQueryOnceGroupAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceGroupAsync<T>(Manager, null, GroupBy, Where);
        public static async Task<T> SQLQueryOnceGroupAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = await SQLQueryGroupAsync<T>(Manager, Table, GroupBy, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static Task<T> SQLQueryOnceGroupAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class => SQLQueryOnceGroupAsync<T>(Manager, null, GroupBy, Where, Offset);
        public static async Task<T> SQLQueryOnceGroupAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = await SQLQueryGroupAsync<T>(Manager, Table, GroupBy, Where, null, Offset, 1);
            return list.Count == 0 ? null : list[0];
        }

        public static Task<T> SQLQueryOnceGroupUnsafeAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceGroupUnsafeAsync<T>(Manager, null, GroupBy, Where);
        public static async Task<T> SQLQueryOnceGroupUnsafeAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = await SQLQueryGroupUnsafeAsync<T>(Manager, Table, GroupBy, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static Task<T> SQLQueryOnceGroupUnsafeAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class => SQLQueryOnceGroupUnsafeAsync<T>(Manager, null, GroupBy, Where, Offset);
        public static async Task<T> SQLQueryOnceGroupUnsafeAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = await SQLQueryGroupUnsafeAsync<T>(Manager, Table, GroupBy, Where, null, Offset, 1);
            return list.Count == 0 ? null : list[0];
        }
        #endregion
        #endregion

        #region SQL Query / SQL Query Group [개선]
        internal static List<T> SQLQueryGroupApply<T>(DBResult result, Type type, ListData data)
        {
            using (result)
            {
                List<T> list = new List<T>();
                while (result.MoveNext())
                {
                    T instance = (T)Activator.CreateInstance(type);
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        ColumnDataReflect column = (ColumnDataReflect)data.Columns[i];

                        string col = column.ColumnName;
                        object value = ConvertValue(column.TypeRef, result[col]);
                        column.Info.SetValue(instance, value);
                    }
                    list.Add(instance);
                }

                return list;
            }
        }

        #region Normal
        public static List<T> SQLQuery<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroup<T>(Manager, null, null, Where, Sort, Offset, Count);
        public static List<T> SQLQuery<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroup<T>(Manager, Table, null, Where, Sort, Offset, Count);
        public static List<T> SQLQueryGroup<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroup<T>(Manager, null, GroupBy, Where, Sort, Offset, Count);
        public static List<T> SQLQueryGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => _SQLQueryGroup<T>(Manager, null, GroupBy, Where, Sort, Offset, Count, true);

        public static List<T> SQLQueryUnsafe<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafe<T>(Manager, null, null, Where, Sort, Offset, Count);
        public static List<T> SQLQueryUnsafe<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafe<T>(Manager, Table, null, Where, Sort, Offset, Count);
        public static List<T> SQLQueryGroupUnsafe<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafe<T>(Manager, null, GroupBy, Where, Sort, Offset, Count);
        public static List<T> SQLQueryGroupUnsafe<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => _SQLQueryGroup<T>(Manager, null, GroupBy, Where, Sort, Offset, Count, false);
        private static List<T> _SQLQueryGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where, IEnumerable<ColumnOrderBy> Sort, int Offset, int Count, bool UseStatement) where T : class
        {
            Type type = typeof(T);
            var data = ListData.FromInstance<T>(out string _Table, Manager);
            DBResult result;
            if (UseStatement)
            {
                var prepare = DBExecuter.ListBuild(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
                result = prepare.Excute();
            }
            else
            {
                var query = DBExecuter.ListBuildString(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
                result = Manager.Excute(query);
            }
            return SQLQueryGroupApply<T>(result, type, data);
        }
        #endregion

        #region Async
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Where"></param>
        /// <param name="Sort">null 이면 인스턴스의 값을 사용합니다</param>
        /// <param name="Count"></param>
        /// <param name="Offset"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static Task<List<T>> SQLQueryAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupAsync<T>(Manager, null, null, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryAsync<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupAsync<T>(Manager, Table, null, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryUnsafeAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafeAsync<T>(Manager, null, null, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryUnsafeAsync<T>(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafeAsync<T>(Manager, Table, null, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryGroupAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupAsync<T>(Manager, null, GroupBy, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryGroupAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => _SQLQueryGroupAsync<T>(Manager, null, GroupBy, Where, Sort, Offset, Count, true);
        public static Task<List<T>> SQLQueryGroupUnsafeAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupUnsafeAsync<T>(Manager, null, GroupBy, Where, Sort, Offset, Count);
        public static Task<List<T>> SQLQueryGroupUnsafeAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => _SQLQueryGroupAsync<T>(Manager, null, GroupBy, Where, Sort, Offset, Count, false);
        private static async Task<List<T>> _SQLQueryGroupAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where, IEnumerable<ColumnOrderBy> Sort, int Offset, int Count, bool UseStatement) where T : class
        {
            Type type = typeof(T);
            /*
            var columns = GetColumns(type, out string Sort);
            bool First = true;

            StringBuilder sb = new StringBuilder("SELECT ");

            foreach (var col in columns)
            {
                if (!First) sb.Append(',');
                sb.Append($"`{col.Key}`");
                First = false;
            }

            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                sb.Append(" FROM `").Append(string.IsNullOrEmpty(attr.Table) ? type.Name : attr.Table).Append("`");

            //조건
            var where = BuildWhere(columns);
            if (!where.IsEmpty()) sb.Append(where.Where);
            
            //정렬
            if (Sort != null) sb.Append(Sort);

            //LIMIT, COUNT
            if (Count > 0)
            {
                if (Manager.Kind == DBConnectionKind.MySQL)
                {
                    if (Offset > 0) sb.Append($" LIMIT {Offset},{Count}");
                    else sb.Append($" LIMIT {Count}");
                }
            }

            using (var prepare = Manager.Prepare(sb.ToString()))
            {
                for (int i = 0; i < where.Columns.Count; i++) prepare.Add($"@{where.Columns[i]}", columns[where.Columns[i]].GetValue(Instance));
                using (var data = await prepare.ExcuteAsync())
                {
                    List<T> list = new List<T>();
                    while (data.MoveNext())
                    {
                        for (int i = 0; i < data.Columns.Length; i++)
                        {
                            T instance = (T)Activator.CreateInstance(type);
                            string col = data.Columns[i].Name;
                            columns[col].Info.SetValue(instance, data[col]);
                            list.Add(instance);
                        }
                    }
                    
                    return list;
                }
            }
            */
            var data = ListData.FromInstance<T>(out string _Table, Manager);
            DBResult result;
            if (UseStatement)
            {
                var prepare = DBExecuter.ListBuild(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
                result = await prepare.ExcuteAsync();
            }
            else 
            {
                var query = DBExecuter.ListBuildString(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
                result = await Manager.ExcuteAsync(query);
            }

            return SQLQueryGroupApply<T>(result, type, data);
        }
        #endregion
        #endregion

        #region SQL Value
        #region SQL Value Get
        internal static string GetValueOnceBuild(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where, bool UseStatement, out ColumnWhere.Statement where)
        {
            where = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = where?.QueryString(UseStatement);
            StringBuilder sb = new StringBuilder($"SELECT {Column} FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);
            return sb.ToString();
        }
        public static DBCommand GetValueOncePrepare(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null)
        {
            var query = GetValueOnceBuild(Manager, Table, Column, Where, true, out var where);
            var Stmt = Manager.Prepare(query);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static object SQLGetValueOnce(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = GetValueOncePrepare(Manager, Table, Column, Where))
                return GetValue(Manager, cmd.ExcuteOnce(), Close);
        }
        public static object SQLGetValueOnce<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLGetValueOnce(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static object SQLGetValueOnceUnsafe(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = GetValueOnceBuild(Manager, Table, Column, Where, false, out var _);
            return GetValue(Manager, Manager.ExcuteOnce(query), Close);
        }
        public static object SQLGetValueOnceUnsafe<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLGetValueOnce(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        #endregion

        #region Async
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static async Task<object> SQLGetValueOnceAsync(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = GetValueOncePrepare(Manager, Table, Column, Where))
                return GetValue(Manager, await cmd.ExcuteOnceAsync(), Close);
        }
        public static Task<object> SQLGetValueOnceAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLGetValueOnceAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static async Task<object> SQLGetValueOnceUnsafeAsync(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = GetValueOnceBuild(Manager, Table, Column, Where, false, out var _);
            return GetValue(Manager, await Manager.ExcuteOnceAsync(query), Close);
        }
        public static Task<object> SQLGetValueOnceUnsafeAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLGetValueOnceUnsafeAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        #endregion
        #endregion

        #region SQL Value Set
        internal static string SetValueOnceBuild(this DBManager Manager, string Prefix, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where, bool UseStatement, out ColumnWhere.Statement WhereStatement)
        {
            WhereStatement = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = WhereStatement?.QueryString();
            StringBuilder sb = new StringBuilder($"UPDATE {Table} SET {Manager.GetQuote(Column)}=");
            if (UseStatement) sb.Append($"{Prefix}{Column}");
            else sb.Append(DBUtils.GetDBString(Value));

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            return sb.ToString();
        }
        internal static DBCommand SetValueOncePrepare(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null)
        {
            var p = Manager.GetStatementPrefix();
            var query = SetValueOnceBuild(Manager, p, Table, Column, Value, Where, true, out var where);

            var Stmt = Manager.Prepare(query);
            Stmt.Add($"{p}{Column}", Value ?? DBNull.Value);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        public static int SQLSetValueOnce(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = SetValueOncePrepare(Manager, Table, Column, Value, Where))
                return GetExcuteNonQuery(Manager, cmd.ExcuteNonQuery(), Close);
        }
        public static int SQLSetValueOnce<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLSetValueOnce(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);
        public static int SQLSetValueOnceUnsafe(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SetValueOnceBuild(Manager, null, Table, Column, Value, Where, false, out var _);
            return GetExcuteNonQuery(Manager, Manager.ExcuteNonQuery(query), Close);
        }
        public static int SQLSetValueOnceUnsafe<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLSetValueOnceUnsafe(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);
        #endregion

        #region Async
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Value"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static async Task<int> SQLSetValueOnceAsync(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var cmd = SetValueOncePrepare(Manager, Table, Column, Value, Where))
                return GetExcuteNonQuery(Manager, await cmd.ExcuteNonQueryAsync(), Close);
        }
        public static Task<int> SQLSetValueOnceAsync<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class =>  SQLSetValueOnceAsync(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Value"></param>
        /// <param name="Where"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static async Task<int> SQLSetValueOnceUnsafeAsync(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SetValueOnceBuild(Manager, null, Table, Column, Value, Where, false, out var _);
            return GetExcuteNonQuery(Manager, await Manager.ExcuteNonQueryAsync(query), Close);
        }
        public static Task<int> SQLSetValueOnceUnsafeAsync<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLSetValueOnceUnsafeAsync(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);
        #endregion
        #endregion
        #endregion

        #region SQL Count
        internal static long SQLCountApply(this DBManager Manager, object Count, bool Close = false)
        {
            try { return Convert.ToInt64(Count); }
            finally { if (Close) Manager.Dispose(); }
        }
        internal static string SQLCountBuild(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where, bool UseStatement, out ColumnWhere.Statement where)
        {
            where = ColumnWhere.JoinForStatement(Where, Conn);
            string where_query = where?.QueryString(UseStatement);
            StringBuilder sb = new StringBuilder("SELECT COUNT(*) FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            return sb.ToString();
        }
        internal static DBCommand SQLCountPrepare(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where)
        {
            var query = SQLCountBuild(Conn, Table, Where, true, out var where);
            var Stmt = Conn.Prepare(query);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static long SQLCount(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var Stmt = SQLCountPrepare(Manager, Table, Where))
                return SQLCountApply(Manager, Stmt.ExcuteOnce(), Close);
        }
        public static long SQLCount(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCount(Manager, Table, Where, false);
        public static long SQLCount<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCount(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static long SQLCount<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            using (var Stmt = SQLRawCommandPrepare(Manager, Instance, "SELECT COUNT(*) FROM", GetTable(Manager, typeof(T))))
                return SQLCountApply(Manager, Stmt.ExcuteOnce(), Close);
        }

        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static long SQLCountUnsafe(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SQLCountBuild(Manager, Table, Where, false, out var _);
            return SQLCountApply(Manager, Manager.ExcuteOnce(query), Close);
        }
        public static long SQLCountUnsafe(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCountUnsafe(Manager, Table, Where, false);
        public static long SQLCountUnsafe<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCountUnsafe(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static long SQLCountUnsafe<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            var query = SQLRawCommandBuild(Manager, Instance, "SELECT COUNT(*) FROM", GetTable(Manager, typeof(T)), false, out var _, out var _);
            return SQLCountApply(Manager, Manager.ExcuteOnce(query), Close);
        }
        #endregion

        #region Async
        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<long> SQLCountAsync(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            using (var Stmt = SQLCountPrepare(Manager, Table, Where))
                return SQLCountApply(Manager, await Stmt.ExcuteOnceAsync(), Close);
        }
        public static Task<long> SQLCountAsync(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCountAsync(Manager, Table, Where, false);
        public static Task<long> SQLCountAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCountAsync(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<long> SQLCountAsync<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            using (var Stmt = SQLRawCommandPrepare(Manager, Instance, "SELECT COUNT(*) FROM", GetTable(Manager, typeof(T))))
                return SQLCountApply(Manager, await Stmt.ExcuteOnceAsync(), Close);
        }

        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<long> SQLCountAsyncUnsafe(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            var query = SQLCountBuild(Manager, Table, Where, false, out var _);
            return SQLCountApply(Manager, await Manager.ExcuteOnceAsync(query), Close);
        }
        public static Task<long> SQLCountAsyncUnsafe(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCountAsyncUnsafe(Manager, Table, Where, false);
        public static Task<long> SQLCountAsyncUnsafe<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCountAsyncUnsafe(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<long> SQLCountAsyncUnsafe<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            var query = SQLRawCommandBuild(Manager, Instance, "SELECT COUNT(*) FROM", GetTable(Manager, typeof(T)), false, out var _, out var _);
            return SQLCountApply(Manager, await Manager.ExcuteOnceAsync(query), Close);
        }
        #endregion
        #endregion

        #region SQL Max
        private static string SQLMaxPrefix(string Table, string Column) => $"SELECT MAX({Column}) FROM {Table}";
        internal static string SQLMaxBuild(DBManager Conn, string Table, string Column, IEnumerable<ColumnWhere> Where, bool UseStatement, out ColumnWhere.Statement where)
        {
            where = ColumnWhere.JoinForStatement(Where, Conn);
            string where_query = where?.QueryString(UseStatement);
            StringBuilder sb = new StringBuilder(SQLMaxPrefix(Table, Column));

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            return sb.ToString();
        }
        internal static DBCommand SQLMaxPrepare(DBManager Conn, string Table, string Column, IEnumerable<ColumnWhere> Where)
        {
            var query = SQLMaxBuild(Conn, Table, Column, Where, true, out var where);
            var Stmt = Conn.Prepare(query);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        /// <summary>
        /// 최대값 구하기
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static object SQLMax(this DBManager Manager, string Table, string Column, bool Close = false)
        {
            var query = SQLMaxPrefix(Table, Column);
            return GetValue(Manager, Manager.ExcuteOnce(query), Close);
        }
        public static object SQLMax<T>(this DBManager Manager, string Column, bool Close = false) => SQLMax(Manager, GetTable(Manager, typeof(T)), Column, Close);

        /// <summary>
        /// 최대값 구하기 (조건절 포함)
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static object SQLMax(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where, bool Close = false)
        {
            using (var Stmt = SQLMaxPrepare(Manager, Table, Column, Where))
                return GetValue(Manager, Stmt.ExcuteOnce(), Close);
        }
        public static object SQLMax(this DBManager Manager, string Table, string Column, params ColumnWhere[] Where) => SQLMax(Manager, Table, Column, Where, false);
        public static object SQLMax<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where, bool Close = false) => SQLMax(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);

        /// <summary>
        /// 최대값 구하기 (조건절 포함, Statement 미사용)
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static object SQLMaxUnsafe(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where, bool Close = false)
        {
            var query = SQLMaxBuild(Manager, Table, Column, Where, false, out var _);
            return GetValue(Manager, Manager.ExcuteOnce(query), Close);
        }
        public static object SQLMaxUnsafe(this DBManager Manager, string Table, string Column, params ColumnWhere[] Where) => SQLMaxUnsafe(Manager, Table, Column, Where, false);
        public static object SQLMaxUnsafe<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where, bool Close = false) => SQLMaxUnsafe(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);

        #endregion

        #region Async
        /// <summary>
        /// 최대값 구하기
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Close"></param>
        /// <returns></returns>
        public static async Task<object> SQLMaxAsync(this DBManager Manager, string Table, string Column, bool Close = false)
        {
            var query = SQLMaxPrefix(Table, Column);
            return GetValue(Manager, await Manager.ExcuteOnceAsync(query), Close);
        }
        public static Task<object> SQLMaxAsync<T>(this DBManager Manager, string Column, bool Close = false) => SQLMaxAsync(Manager, GetTable(Manager, typeof(T)), Column, Close);

        /// <summary>
        /// 최대값 구하기 (조건절 포함)
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<object> SQLMaxAsync(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where, bool Close = false)
        {
            using (var Stmt = SQLMaxPrepare(Manager, Table, Column, Where))
                return GetValue(Manager, await Stmt.ExcuteOnceAsync(), Close);
        }
        public static object SQLMaxAsync(this DBManager Manager, string Table, string Column, params ColumnWhere[] Where) => SQLMaxAsync(Manager, Table, Column, Where, false);
        public static Task<object> SQLMaxAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where, bool Close = false) => SQLMaxAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);

        /// <summary>
        /// 최대값 구하기 (조건절 포함, Statement 미사용)
        /// </summary>
        /// <param name="Manager">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<object> SQLMaxUnsafeAsync(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where, bool Close = false)
        {
            var query = SQLMaxBuild(Manager, Table, Column, Where, false, out var _);
            return GetValue(Manager, await Manager.ExcuteOnceAsync(query), Close);
        }
        public static Task<object> SQLMaxUnsafeAsync(this DBManager Manager, string Table, string Column, params ColumnWhere[] Where) => SQLMaxUnsafeAsync(Manager, Table, Column, Where, false);
        public static Task<object> SQLMaxUnsafeAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where, bool Close = false) => SQLMaxUnsafeAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        #endregion
        #endregion

        #region SQL Instance
        /// <summary>
        /// 주어진 형식에 대한 데이터를 DB 결과값에서 가져옵니다 (반드시 MoveNext() 필요!!)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Result"></param>
        /// <param name="Dispose">DB 결과값을 자동으로 Dispose 할지 여부입니다</param>
        /// <returns></returns>
        public static T ToInstance<T>(this DBResult Result, bool Dispose = false) where T : class
        {
            Type type = typeof(T);
            T Instance = (T)Activator.CreateInstance(type);
            try { return Apply(Result, Instance); }
            finally{ if (Dispose) Result.Dispose(); }
        }
        /// <summary>
        /// 주어진 형식에 대한 데이터 목록을 DB 결과값에서 가져옵니다 (MoveNext() 할 필요 없습니다)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Result"></param>
        /// <param name="Dispose">DB 결과값을 자동으로 Dispose 할지 여부입니다</param>
        /// <returns></returns>
        public static List<T> ToInstanceList<T>(this DBResult Result, bool Dispose = true) where T : class
        {
            try
            {
                Type type = typeof(T);
                List<T> list = new List<T>();
                while (Result.MoveNext())
                {
                    T instance = (T)Activator.CreateInstance(type);
                    Apply(Result, instance);
                    list.Add(instance);
                }

                return list;
            }
            finally { if (Dispose) Result.Dispose(); }
        }

        public static T Apply<T>(this DBResult Result, T Instance)
        {
            Type type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var func = new Action<dynamic, Type>((Info, Type) =>
            {
                foreach (SQLColumnAttribute column in Info.GetCustomAttributes(SQLColumnAttribute.ClassType, false))
                {
                    string ColumnName = string.IsNullOrEmpty(column.Name) ? Info.Name : column.Name;
                    if (Result.ColumnExist(ColumnName))
                    {
                        object value = ConvertValue(Type, Result[ColumnName]);
                        Info.SetValue(Instance, value);
                    }
                }
            });

            for (int i = 0; i < properties.Length; i++)
                func(properties[i], properties[i].PropertyType);

            for (int i = 0; i < fields.Length; i++)
                func(fields[i], fields[i].FieldType);

            return Instance;
        }

        #endregion

        #region SQL ETC
        #endregion

        #region ETC
        /// <summary>
        /// Get ColumnWhere list from instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Instance"></param>
        /// <returns></returns>
        public static List<ColumnWhere> GetWhere<T>(T Instance) where T : class
        {
            Type type = Instance.GetType();
            var Columns = GetColumns(type, Instance, out var _);

            List<ColumnWhere> wheres = new List<ColumnWhere>(10);
            foreach (var col in Columns)
            {
                if (col.Value.Where != null)
                {
                    var value = col.Value.GetValue(Instance);
                    var condition = col.Value.Where.Condition.ToString();

                    if (col.Value.Where.Kind == WhereKind.Equal) wheres.Add(ColumnWhere.Is(col.Key, value, condition));
                    else if (col.Value.Where.Kind == WhereKind.NotEqual) wheres.Add(ColumnWhere.IsNot(col.Key, value, condition));
                    else if (col.Value.Where.Kind == WhereKind.LIKE) wheres.Add(ColumnWhere.Like(col.Key, value, condition));
                }
            }
            return wheres;
        }
        #endregion

        //컬럼 빌드
        private static readonly Type SQLWhereType = typeof(SQLWhereAttribute);
        private static readonly Type SQLSortType = typeof(SQLSortAttribute);
        private static Dictionary<string, ColumnData> GetColumns(Type type, object Instance, out List<SQLSortAttribute> Sort)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var Columns = new Dictionary<string, ColumnData>();
            var Sorts = new List<SQLSortAttribute>(10);

            var func = new BuildAction<dynamic, Type>((Info, Type) => 
            {
                SQLColumnAttribute col = null;
                SQLWhereAttribute iswhere = null;
                SQLSortAttribute issort = null;

                bool Include = false;
                foreach (SQLColumnAttribute column in Info.GetCustomAttributes(SQLColumnAttribute.ClassType, false))
                {
                    col = column;
                    //Key 면 자동으로 Where 생성
                    Include = column.UseIgnoreValue ? !object.Equals(Info.GetValue(Instance), column.IgnoreValue) : true;
                    if (column.Key && Include) iswhere = new SQLWhereAttribute(WhereKind.Equal, WhereCondition.AND);
                }
                foreach (SQLWhereAttribute where in Info.GetCustomAttributes(SQLWhereType, false)) iswhere = where;
                foreach (SQLSortAttribute sort in Info.GetCustomAttributes(SQLSortType, false)) issort = sort;

                if (col != null)
                {
                    string Name = string.IsNullOrEmpty(col.Name) ? Info.Name : col.Name; ;
                    if (Include)
                    {
                        var data = new ColumnData(col, Info, Type, iswhere);
                        if (Columns.ContainsKey(Name)) Columns[Name] = data;
                        else Columns.Add(Name, data);
                    }

                    if (issort != null)
                    {
                        issort.Column = Name;
                        Sorts.Add(issort);
                    }
                }

                return null;
            });

            string SortOut = null;
            for (int i = 0; i < properties.Length; i++) 
                if(SortOut == null) SortOut = func(properties[i], properties[i].PropertyType);
                else func(properties[i], properties[i].PropertyType);

            for (int i = 0; i < fields.Length; i++)
                if (SortOut == null) SortOut = func(fields[i], fields[i].FieldType);
                else func(fields[i], fields[i].FieldType);
            Sort = Sorts;

            return Columns;
        }

        //조건 빌드
        private static WhereData BuildWhere<T>(Dictionary<string, ColumnData> Columns, bool UseStatement, T Instance, DBManager Manager = null) where T : class
        {
            string Prefix = Manager == null ? "" : Manager.StatementPrefix.ToString();
            StringBuilder sb = new StringBuilder();
            List<string> where = new List<string>();
            bool First = true;
            foreach (var col in Columns)
            {
                if (col.Value.Where != null)
                {
                    if (UseStatement) where.Add(col.Key);

                    var _where = col.Value.Where;
                    if (First) sb.Append(" WHERE ");
                    else
                    {
                        if (_where.Condition == WhereCondition.AND) sb.Append(" AND ");
                        else if (_where.Condition == WhereCondition.OR) sb.Append(" OR ");
                    }

                    if (_where.Kind == WhereKind.Equal) sb.Append(Manager.GetQuote(col.Key)).Append(" = ").Append(UseStatement ? $"{Prefix}{col.Key}" : col.Value.GetDBValue(Instance));
                    else if (_where.Kind == WhereKind.NotEqual) sb.Append(Manager.GetQuote(col.Key)).Append(" <> ").Append(UseStatement ? $"{Prefix}{col.Key}" : col.Value.GetDBValue(Instance));
                    else if (_where.Kind == WhereKind.LIKE) sb.Append(Manager.GetQuote(col.Key)).Append(" LIKE ").Append(UseStatement ? $"{Prefix}{col.Key}" : col.Value.GetDBValue(Instance));

                    First = false;
                }
            }

            return new WhereData(sb.ToString(), where);
        }

        /// <summary>
        /// 값 -> 인스턴스
        /// </summary>
        /// <param name="OriginalType"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static object ConvertValue(Type OriginalType, object Value, ColumnType Type = ColumnType.AUTO)
        {
            if (Type == ColumnType.XML) return ConvertUtils.ConvertValue(Value, OriginalType, ConvertType.XML);
            else if (Type == ColumnType.JSON) return ConvertUtils.ConvertValue(Value, OriginalType, ConvertType.JSON);
            else return ConvertUtils.ConvertValue(Value, OriginalType, ConvertType.Auto);
        }
        /// <summary>
        /// 인스턴스 -> 값
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        private static object ConvertValue(ColumnType Type, object Value)
        {
            if (Value == null) return null;
            else if (Type == ColumnType.DECIMAL || Type == ColumnType.NUMBER) return Convert.ToDecimal(Value).ToString();
            else if (Type == ColumnType.STRING) return Convert.ToString(Value);
            else if (Type == ColumnType.BOOL) return Convert.ToBoolean(Value).ToString();
            else if (Type == ColumnType.XML) return Value.ToSerializeXML();
            else if (Type == ColumnType.JSON) return Value.ToSerializeJSON_NS();
            return Value;
        }

        internal class ColumnData
        {
            public ColumnData(SQLColumnAttribute Column, dynamic Info, Type Type, SQLWhereAttribute Where) { this.Column = Column; this.Info = Info; this.Type = Type; this.Where = Where; }

            public dynamic Info;
            public SQLColumnAttribute Column;
            public SQLWhereAttribute Where;
            public Type Type;

            public string OriginName => Info.Name;
            public object GetValue<T>(T Instance) where T : class => Info.GetValue(Instance);
            public string GetDBValue<T>(T Instance) where T : class => DBUtils.GetDBString(GetValue(Instance));
        }

        internal class WhereData
        {
            public WhereData(string Where, List<string> Columns) { this.Where = Where; this.Columns = Columns; }
            public string Where;
            public List<string> Columns;

            public bool IsEmpty() => string.IsNullOrEmpty(Where);
            public override string ToString() => Where;
        }

        private delegate string BuildAction<T1, T2>(T1 Info, T2 Type);
    }
}
