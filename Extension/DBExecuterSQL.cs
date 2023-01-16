using HS.DB.Extension.Attributes;
using HS.DB;
using HS.DB.Result;
using HS.DB.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HS.DB.Extension
{
    public static class DBExecuterSQL
    {
        public static async Task<bool> SQLInsert<T>(this DBManager Manager, T Instance) where T : class
        {
            Type type = Instance.GetType();
            var columns = GetColumns(type, out string _);
            bool First = true;

            StringBuilder sb = new StringBuilder("INSERT INTO ");

            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false)) 
                sb.Append(attr.ToString(true, type.Name));

            //컬럼
            foreach (var col in columns)
            {
                sb.Append(First ? " (" : ", ");
                sb.Append($"`{col.Key}`");
                First = false;
            }

            sb.Append(") VALUES (");

            Dictionary<string, object> VALUES = new Dictionary<string, object>();
            //값
            First = true;
            foreach (var col in columns)
            {
                VALUES.Add(col.Key, col.Value.GetValue(Instance));
                sb.Append(First ? null : ", ").Append($"@{col.Key}");
                First = false;
            }
            sb.Append(")");

            using (var prepare = Manager.Prepare(sb.ToString()))
            {
                foreach (var item in VALUES) prepare.Add($"@{item.Key}", item.Value);
                return await prepare.ExcuteNonQueryAsync() > 0;
            }
        }
        public static async Task<bool> SQLUpdate<T>(this DBManager Manager, T Instance) where T : class
        {
            Type type = Instance.GetType();
            var columns = GetColumns(type, out string _);
            bool First = true;

            StringBuilder sb = new StringBuilder("UPDATE ");

            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                sb.Append(attr.ToString(true, type.Name));

            sb.Append(" SET ");

            //컬럼
            foreach (var col in columns)
            {
                if(col.Value.Where == null)
                {
                    sb.Append(First ? null : ", ");
                    sb.Append($"`{col.Key}`=@{col.Key}");
                    First = false;
                }
            }

            //조건 (Primary Key)
            var where = BuildWhere(columns);
            if (!where.IsEmpty()) sb.Append(where.Where);

            using (var prepare = Manager.Prepare(sb.ToString()))
            {
                //foreach (var item in VALUES) prepare.Add($"@{item.Key}", item.Value);
                foreach(var col in columns) prepare.Add($"@{col.Key}", col.Value.GetValue(Instance));
                return await prepare.ExcuteNonQueryAsync() > 0;
            }
        }

        #region SQLQuery
        public static async Task<bool> SQLQuery<T>(this DBManager Manager, T Instance) where T : class
        {
            Type type = Instance.GetType();
            var columns = GetColumns(type, out string Sort);
            bool First = true;

            StringBuilder sb = new StringBuilder("SELECT ");

            foreach (var col in columns)
            {
                if (col.Value.Where == null)
                {
                    sb.Append(First ? null : ", ");
                    if (col.Value.OriginName == col.Key) sb.Append($"`{col.Key}`");
                    else sb.Append($"`{col.Key}`").Append(" AS ").Append($"`{col.Value.OriginName}`");
                    First = false;
                }
            }

            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                sb.Append(attr.ToString(true, type.Name));

            //조건
            var where = BuildWhere(columns);
            if (!where.IsEmpty()) sb.Append(where.Where);

            //정렬
            if (Sort != null) sb.Append(Sort);

            using (var prepare = Manager.Prepare(sb.ToString()))
            {
                for(int i = 0; i < where.Columns.Count; i++) prepare.Add($"@{where.Columns[i]}", columns[where.Columns[i]].GetValue(Instance));
                using (var data = await prepare.ExcuteAsync())
                {
                    if (data.MoveNext())
                    {
                        for(int i = 0; i < data.Columns.Length; i++)
                        {
                            string col = data.Columns[i].Name;
                            columns[col].Info.SetValue(Instance, data[col]);
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
        /// <param name="Where"></param>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public static async Task<T> SQLQueryOnce<T>(this DBManager Manager, List<ColumnWhere> Where = null, int Offset = 0) where T : class
        {
            List<T> list = await SQLQuery<T>(Manager, Where, 1, Offset);
            return list.Count == 0 ? null : list[0];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Manager"></param>
        /// <param name="Where"></param>
        /// <param name="Count"></param>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public static async Task<List<T>> SQLQuery<T>(this DBManager Manager, List<ColumnWhere> Where = null, int Count = -1, int Offset = 0) where T : class
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
            var data = ListData.FromInstance<T>(out string Table);
            using (DBResult result = await DBExecuter.ListBuild(Manager, Table, Offset, Count, data.Columns, Where, data.Sort).ExcuteAsync())
            {
                List<T> list = new List<T>();
                while (result.MoveNext())
                {
                    T instance = (T)Activator.CreateInstance(type);
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        ColumnDataReflect column = (ColumnDataReflect)data.Columns[i];

                        string col = column.ColumnName;
                        object obj = result[col];
                        Type type_col = obj.GetType();
                        
                        if (column.TypeRef != type_col)
                        {
                            if (type_col == typeof(DBNull)) obj = null;
                            else if (column.TypeRef == typeof(bool))
                            {
                                if (type_col == typeof(string)) obj = ((string)obj).ToUpper() == "TRUE";
                                else obj = Convert.ToBoolean(obj);
                                /*
                                if (type_col == typeof(int) ||
                                    type_col == typeof(long) ||
                                    type_col == typeof(float) ||
                                    type_col == typeof(decimal) ||
                                    type_col == typeof(double)) obj = obj.ToString() == "1";
                                else if (type_col == typeof(string)) obj = (string)obj;
                                */
                            }
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_BYTE) obj = Convert.ToByte(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_SBYTE) obj = Convert.ToSByte(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_USHORT) obj = Convert.ToInt16(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_SHORT) obj = Convert.ToUInt16(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_INT) obj = Convert.ToInt32(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_UINT) obj = Convert.ToUInt32(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_LONG) obj = Convert.ToInt64(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_ULONG) obj = Convert.ToUInt64(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_FLOAT) obj = Convert.ToSingle(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_DOUBLE) obj = Convert.ToDouble(obj);
                            else if (column.TypeRef == SQLColumnAttribute.TYPE_DATETIME) obj = Convert.ToDecimal(obj);
                        }
                        column.Info.SetValue(instance, obj);
                    }
                    list.Add(instance);
                }

                return list;
            }
        }
        #endregion

        #region SQLCount
        public static async Task<long> SQLCount<T>(this DBManager Manager, T Instance) where T : class
        {
            Type type = Instance.GetType();
            var columns = GetColumns(type, out string _);
            StringBuilder sb = new StringBuilder("SELECT COUNT(*)");

            //테이블
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                sb.Append(attr.ToString(true, type.Name));

            //조건
            var where = BuildWhere(columns);
            if (!where.IsEmpty()) sb.Append(where.Where);

            return long.Parse((await Manager.ExcuteOnceAsync(sb.ToString())).ToString());
        }
        public static async Task<long> SQLCount<T>(this DBManager Manager, List<ColumnWhere> Where = null)
        {
            Type type = typeof(T);

            string Table = null;
            foreach (SQLTableAttribute attr in type.GetCustomAttributes(typeof(SQLTableAttribute), false))
                Table = attr.ToString(true, type.Name);

            return await DBExecuter.Count(Manager, Table, Where);
        }
        #endregion

        #region SQLExist
        /*
        public static async Task<bool> SQLExist<T>(this DBManager Manager, T Instance) where T : class
        {

        }
        */
        #endregion

        //컬럼 빌드
        private static readonly Type SQLColumnType = typeof(SQLColumnAttribute);
        private static readonly Type SQLWhereType = typeof(SQLWhereAttribute);
        private static readonly Type SQLSortType = typeof(SQLSortAttribute);
        private static Dictionary<string, ColumnData> GetColumns(Type type, out string Sort)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Dictionary<string, ColumnData> Columns = new Dictionary<string, ColumnData>();

            var func = new BuildAction<dynamic, Type>((Info, Type) => 
            {
                string col = null;
                SQLWhereAttribute iswhere = null;
                SQLSortAttribute issort = null;

                foreach (SQLColumnAttribute column in Info.GetCustomAttributes(SQLColumnType, false))
                {
                    col = string.IsNullOrEmpty(column.Name) ? Info.Name : column.Name;
                    //PK 면 자동으로 Where 생성
                    if(column.PrimaryKey) iswhere = new SQLWhereAttribute(WhereKind.Equal, WhereCondition.AND);
                }
                foreach (SQLWhereAttribute where in Info.GetCustomAttributes(SQLWhereType, false)) iswhere = where;
                foreach (SQLSortAttribute sort in Info.GetCustomAttributes(SQLSortType, false)) issort = sort;

                if (col != null)
                {
                    Columns.Add(col, new ColumnData(Info, Type, iswhere));
                    return issort?.ToString(col);
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
            Sort = SortOut;

            return Columns;
        }

        //조건 빌드
        private static WhereData BuildWhere(Dictionary<string, ColumnData> Columns)
        {
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

                    if (_where.Kind == WhereKind.Equal) sb.Append($"`{col.Key}`=@{col.Key}");
                    else if (_where.Kind == WhereKind.NotEqual) sb.Append($"`{col.Key}`<>@{col.Key}");
                    else if (_where.Kind == WhereKind.LIKE) sb.Append($"`{col.Key}` LIKE @{col.Key}");

                    where.Add(col.Key);

                    First = false;
                }
            }

            return new WhereData(sb.ToString(), where);
        }

        private class ColumnData : SQLColumnAttribute
        {
            public ColumnData(dynamic Info, Type Type, SQLWhereAttribute Where) { this.Info = Info; this.Type = Type; this.Where = Where; }

            public dynamic Info;
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
