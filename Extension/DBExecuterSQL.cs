using HS.DB.Command;
using HS.DB.Extension.Attributes;
using HS.DB.Result;
using HS.Utils;
using HS.Utils.Convert;
using HS.Utils.Text;
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

        #region SQL Insert
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static DBCommand _SQLInsertPrepare<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var p = Manager.StatementPrefix;
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

            Dictionary<string, object> VALUES = new Dictionary<string, object>();
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
                    VALUES.Add(col.Key, ConvertValue(col.Value.Column.Type, col.Value.GetValue(Instance)));
                    sb.Append($"{p}{col.Key}");
                }
                First = false;
            }
            sb.Append(")");
            for (int i = 0; i < keys_remove.Count; i++) columns.Remove(keys_remove[i]);


            var where = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = where?.QueryString();
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            var prepare = Manager.Prepare(sb.ToString());
            //추가 조건절이 존재하면 할당
            if (!string.IsNullOrEmpty(where_query)) where.Apply(prepare);
            return prepare;
        }
        public static bool SQLInsert<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var prepare = _SQLInsertPrepare(Manager, Instance, Where))
                return prepare.ExcuteNonQuery() > 0;
        }
        public static async Task<bool> SQLInsertAsync<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var prepare = _SQLInsertPrepare(Manager, Instance, Where))
                return await prepare.ExcuteNonQueryAsync() > 0;
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
        public static DBCommand _SQLUpdatePrepare<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            var p = Manager.StatementPrefix;
            Type type = Instance.GetType();
            var columns = GetColumns(type, Instance, out var _);
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
                    else sb.Append($"{p}{col.Key}");
                    First = false;
                }
            }
            for (int i = 0; i < keys_remove.Count; i++) columns.Remove(keys_remove[i]);

            //조건 (Primary Key)
            ColumnWhere.Statement WhereStatement = null;
            if (Where == null)
            {
                var where = BuildWhere(columns, Manager);
                if (!where.IsEmpty()) sb.Append(where.Where);
            }
            else
            {
                WhereStatement = ColumnWhere.JoinForStatement(Where, Manager);
                sb.Append(" WHERE ").Append(WhereStatement?.QueryString());
            }

            var prepare = Manager.Prepare(sb.ToString());
            WhereStatement?.Apply(prepare);
            //foreach (var item in VALUES) prepare.Add($"@{item.Key}", item.Value);
            foreach (var col in columns) prepare.Add($"{p}{col.Key}", ConvertValue(col.Value.Column.Type, col.Value.GetValue(Instance)) ?? DBNull.Value);
            return prepare;
        }

        public static bool SQLUpdate<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var cmd = _SQLUpdatePrepare<T>(Manager, Instance, Where))
                return cmd.ExcuteNonQuery() > 0;
        }
        public static async Task<bool> SQLUpdateAsync<T>(this DBManager Manager, T Instance, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            using (var cmd = _SQLUpdatePrepare<T>(Manager, Instance, Where))
                return await cmd.ExcuteNonQueryAsync() > 0;
        }
        #endregion

        #region SQLQuery
        private static DBCommand QueryPrepare<T>(this DBManager Manager, T Instance, out Dictionary<string, ColumnData> columns) where T : class
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
            var where = BuildWhere(columns, Manager);
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

            var prepare = Manager.Prepare(sb.ToString());
            for (int i = 0; i < where.Columns.Count; i++)
            {
                var column = columns[where.Columns[i]];
                prepare.Add($"{p}{where.Columns[i]}", ConvertValue(column.Column.Type, column.GetValue(Instance)));
            }
            return prepare;
        }

        #region SQLQueryInatance
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static bool SQLQueryInatance<T>(this DBManager Manager, T Instance) where T : class
        {
            using (var cmd = QueryPrepare(Manager, Instance, out var columns))
            {
                using (var result = cmd.Excute())
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
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Instance"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">When Class has no table</exception>
        public static async Task<bool> SQLQueryInatanceAsync<T>(this DBManager Manager, T Instance) where T : class
        {
            using (var cmd = QueryPrepare(Manager, Instance, out var columns))
            {
                using (var result = await cmd.ExcuteAsync())
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
        }
        #endregion

        #region SQLQueryOnceAsync
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
        #endregion
        #endregion

        #region SQLQueryOnceGroupAsync
        #region Normal
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class => SQLQueryOnceGroup<T>(Manager, null, GroupBy, Where);
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null) where T : class
        {
            List<T> list = SQLQueryGroup<T>(Manager, Table, GroupBy, Where, null, 0, -1);
            return list.Count == 0 ? null : list[0];
        }
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class => SQLQueryOnceGroup<T>(Manager, null, GroupBy, Where, Offset);
        public static T SQLQueryOnceGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = SQLQueryGroup<T>(Manager, Table, GroupBy, Where, null, Offset, 1);
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
        #endregion
        #endregion

        #region SQLQuery / SQLQueryGroup
        private static List<T> _SQLQueryGroup<T>(DBResult result, Type type, ListData data)
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
        public static List<T> SQLQueryGroup<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class
        {
            Type type = typeof(T);
            var data = ListData.FromInstance<T>(out string _Table, Manager);
            var prepare = DBExecuter.ListBuild(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
            return _SQLQueryGroup<T>(prepare.Excute(), type, data);
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
        public static Task<List<T>> SQLQueryGroupAsync<T>(this DBManager Manager, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class => SQLQueryGroupAsync<T>(Manager, null, GroupBy, Where, Sort, Offset, Count);
        public static async Task<List<T>> SQLQueryGroupAsync<T>(this DBManager Manager, string Table, IEnumerable<string> GroupBy, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, int Offset = 0, int Count = -1) where T : class
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
            var prepare = DBExecuter.ListBuild(Manager, Table ?? _Table, Offset, Count, data.Columns, Where, Sort ?? data.Sort, GroupBy);
            return _SQLQueryGroup<T>(await prepare.ExcuteAsync(), type, data);
        }
        #endregion
        #endregion

        #region SQL Get Value
        public static DBCommand GetValueOncePrepare(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null)
        {
            var where = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = where?.QueryString();
            StringBuilder sb = new StringBuilder($"SELECT {Column} FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            var Stmt = Manager.Prepare(sb.ToString());
            //추가 조건절이 존재하면 할당
            if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);
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
            try
            {
                using (var cmd = GetValueOncePrepare(Manager, Table, Column, Where))
                {
                    //값이 DBNull 이면 null 반환
                    var value = cmd.ExcuteOnce();
                    return value == DBNull.Value ? null : value;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static object SQLGetValueOnce<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class
            => SQLGetValueOnce(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
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
            try
            {
                using (var cmd = GetValueOncePrepare(Manager, Table, Column, Where))
                {
                    //값이 DBNull 이면 null 반환
                    var value = await cmd.ExcuteOnceAsync();
                    return value == DBNull.Value ? null : value;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static Task<object> SQLGetValueOnceAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class
            => SQLGetValueOnceAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        #endregion
        #endregion

        #region SQL Set Value
        private static DBCommand SetValueOncePrepare(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null)
        {
            char p = Manager.StatementPrefix;
            var where = ColumnWhere.JoinForStatement(Where, Manager);
            string where_query = where?.QueryString();
            StringBuilder sb = new StringBuilder($"UPDATE {Table} SET {Column}={p}{Column}");

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            var Stmt = Manager.Prepare(sb.ToString());

            Stmt.Add($"{p}{Column}", Value ?? DBNull.Value);

            //추가 조건절이 존재하면 할당
            if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);
            return Stmt;
        }

        #region Normal
        public static bool SQLSetValueOnce(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var cmd = SetValueOncePrepare(Manager, Table, Column, Value, Where))
                {
                    //1보다 크면 변경됨
                    return cmd.ExcuteNonQuery() > 0;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static bool SQLSetValueOnce<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) =>
            SQLSetValueOnce(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);
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
        public static async Task<bool> SQLSetValueOnceAsync(this DBManager Manager, string Table, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var cmd = SetValueOncePrepare(Manager, Table, Column, Value, Where))
                {
                    //1보다 크면 변경됨
                    return await cmd.ExcuteNonQueryAsync() > 0;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static Task<bool> SQLSetValueOnceAsync<T>(this DBManager Manager, string Column, object Value, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class =>
            SQLSetValueOnceAsync(Manager, GetTable(Manager, typeof(T)), Column, Value, Where, Close);

        #endregion
        #endregion

        private static DBCommand SQLRawCommand<T>(DBManager Manager, T Instance, string Prefix, string Table = null) where T : class
        {
            char p = Manager.StatementPrefix;
            Type type = Instance.GetType();
            var columns = GetColumns(type, Instance, out var _);
            StringBuilder sb = new StringBuilder(Prefix);

            //테이블
            sb.Append(Table ?? GetTable(Manager, type));

            //조건
            var where = BuildWhere(columns, Manager);
            if (!where.IsEmpty()) sb.Append(where.Where);


            var prepare = Manager.Prepare(sb.ToString());
            for (int i = 0; i < where.Columns.Count; i++)
            {
                var column = columns[where.Columns[i]];
                prepare.Add($"{p}{where.Columns[i]}", ConvertValue(column.Column.Type, column.GetValue(Instance)));
            }

            return prepare;
        }
        #region SQL Count
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
            try
            {
                using (var Stmt = DBExecuter.CountPrepare(Manager, Table, Where))
                    return Convert.ToInt64(Stmt.ExcuteOnce());
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static long SQLCount(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCount(Manager, Table, Where, false);
        public static long SQLCount<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCount(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static long SQLCount<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            try
            {
                using (var prepare = SQLRawCommand(Manager, Instance, "SELECT COUNT(*) FROM"))
                    return Convert.ToInt64(prepare.ExcuteOnce());
            }
            finally { if (Close) Manager.Dispose(); }
        }
        #endregion

        #region Async
        public static async Task<long> SQLCountAsync(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var Stmt = DBExecuter.CountPrepare(Manager, Table, Where))
                    return Convert.ToInt64(await Stmt.ExcuteOnceAsync());
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static Task<long> SQLCountAsync(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLCountAsync(Manager, Table, Where, false);
        public static Task<long> SQLCountAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLCountAsync(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<long> SQLCountAsync<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            try
            {
                using (var prepare = SQLRawCommand(Manager, Instance, "SELECT COUNT(*) FROM"))
                    return Convert.ToInt64(await prepare.ExcuteOnceAsync());
            }
            finally { if (Close) Manager.Dispose(); }
        }
        #endregion
        #endregion

        #region SQL Delete
        #region Normal
        public static bool SQLDelete(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var stmt = DBExecuter.DeletePrepare(Manager, Table, Where))
                    return stmt.ExcuteNonQuery() > 0;
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static bool SQLDelete(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDelete(Manager, Table, Where, false);
        public static bool SQLDelete<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDelete(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static int SQLDelete<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            try
            {
                using (var prepare = SQLRawCommand(Manager, Instance, "DELETE "))
                    return prepare.ExcuteNonQuery();
            }
            finally { if (Close) Manager.Dispose(); }
        }
        #endregion

        #region Async
        public static async Task<bool> SQLDeleteAsync(this DBManager Manager, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var stmt = DBExecuter.DeletePrepare(Manager, Table, Where))
                    return await stmt.ExcuteNonQueryAsync() > 0;
            }
            finally { if (Close) Manager.Dispose(); }
        }

        public static Task<bool> SQLDeleteAsync(this DBManager Manager, string Table, params ColumnWhere[] Where) => SQLDeleteAsync(Manager, Table, Where, false);

        public static Task<bool> SQLDeleteAsync<T>(this DBManager Manager, IEnumerable<ColumnWhere> Where = null, bool Close = false) => SQLDeleteAsync(Manager, GetTable(Manager, typeof(T)), Where, Close);
        public static async Task<long> SQLDeleteAsync<T>(this DBManager Manager, T Instance, bool Close = false) where T : class
        {
            try
            {
                using (var prepare = SQLRawCommand(Manager, Instance, "DELETE "))
                    return await prepare.ExcuteNonQueryAsync();
            }
            finally { if (Close) Manager.Dispose(); }
        }
        #endregion
        #endregion

        #region SQL Max
        #region Normal
        public static object SQLMax(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var stmt = DBExecuter.MaxPrepare(Manager, Table, Column, Where))
                {
                    var result = stmt.ExcuteOnce();
                    return result is DBNull ? null : result;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static object SQLMax<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLMax(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
        #endregion

        #region Async
        public static async Task<object> SQLMaxAsync(this DBManager Manager, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                using (var stmt = DBExecuter.MaxPrepare(Manager, Table, Column, Where))
                {
                    var result = await stmt.ExcuteOnceAsync();
                    return result is DBNull ? null : result;
                }
            }
            finally { if (Close) Manager.Dispose(); }
        }
        public static Task<object> SQLMaxAsync<T>(this DBManager Manager, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false) where T : class => SQLMaxAsync(Manager, GetTable(Manager, typeof(T)), Column, Where, Close);
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
            finally { if (Dispose) Result.Dispose(); }
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
                if (SortOut == null) SortOut = func(properties[i], properties[i].PropertyType);
                else func(properties[i], properties[i].PropertyType);

            for (int i = 0; i < fields.Length; i++)
                if (SortOut == null) SortOut = func(fields[i], fields[i].FieldType);
                else func(fields[i], fields[i].FieldType);
            Sort = Sorts;

            return Columns;
        }

        //조건 빌드
        private static WhereData BuildWhere(Dictionary<string, ColumnData> Columns, DBManager Manager = null)
        {
            char Prefix = Manager == null ? '\0' : Manager.StatementPrefix;
            StringBuilder sb = new StringBuilder();
            List<string> where = new List<string>();
            bool First = true;
            foreach (var col in Columns)
            {
                if (col.Value.Where != null)
                {
                    var _where = col.Value.Where;

                    if (First) sb.Append(" WHERE ");
                    else
                    {
                        if (_where.Condition == WhereCondition.AND) sb.Append(" AND ");
                        else if (_where.Condition == WhereCondition.OR) sb.Append(" OR ");
                    }

                    if (_where.Kind == WhereKind.Equal) sb.Append(Manager.GetQuote(col.Key)).Append($" = {Prefix}{col.Key}");
                    else if (_where.Kind == WhereKind.NotEqual) sb.Append(Manager.GetQuote(col.Key)).Append($" <> {Prefix}{col.Key}");
                    else if (_where.Kind == WhereKind.LIKE) sb.Append(Manager.GetQuote(col.Key)).Append($" LIKE {Prefix}{col.Key}");

                    where.Add(col.Key);

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
        static object ConvertValue(ColumnType Type, object Value)
        {
            if (Value == null) return null;
            else if (Type == ColumnType.DECIMAL || Type == ColumnType.NUMBER) return Convert.ToDecimal(Value).ToString();
            else if (Type == ColumnType.STRING) return Convert.ToString(Value);
            else if (Type == ColumnType.BOOL) return Convert.ToBoolean(Value).ToString();
            else if (Type == ColumnType.XML) return Value.ToSerializeXML();
            else if (Type == ColumnType.JSON) return Value.ToSerializeJSON_NS();
            return Value;
        }

        private class ColumnData
        {
            public ColumnData(SQLColumnAttribute Column, dynamic Info, Type Type, SQLWhereAttribute Where) { this.Column = Column; this.Info = Info; this.Type = Type; this.Where = Where; }

            public dynamic Info;
            public SQLColumnAttribute Column;
            public SQLWhereAttribute Where;
            public Type Type;

            public string OriginName => Info.Name;
            public object GetValue<T>(T Instance) where T : class => Info.GetValue(Instance);
        }

        private class WhereData
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
