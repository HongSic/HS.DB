using HS.DB.Extension.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HS.DB.Extension
{
    public class ListData
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Columns">가져올 열 (null 이면 모두 가져오기)</param>
        /// <param name="Where">조건</param>
        /// <param name="Sort">정렬</param>
        public ListData(List<ColumnData> Columns = null, List<ColumnWhere> Where = null, List<ColumnOrderBy> Sort = null)
        {
            this.Columns = Columns;
            this.Where = Where;
            this.Sort = Sort;
        }
        public List<ColumnData> Columns { get; internal set; }
        public List<ColumnWhere> Where { get; internal set; }
        public List<ColumnOrderBy> Sort { get; internal set; }


        private delegate void BuildAction<T1, T2>(T1 Info, T2 Type);
        private static readonly Type SQLTableType = typeof(SQLTableAttribute);
        private static readonly Type SQLColumnType = typeof(SQLColumnAttribute);
        private static readonly Type SQLWhereType = typeof(SQLWhereAttribute);
        private static readonly Type SQLSortType = typeof(SQLSortAttribute);

        public static ListData FromInstance<T>(DBManager Manager = null) where T : class => FromInstance<T>(out _, Manager);
        public static ListData FromInstance<T>(out string Table, DBManager Manager = null) where T : class => FromInstance((T)Activator.CreateInstance(typeof(T)), false, out Table, Manager);
        public static ListData FromInstance<T>(T Instance, DBManager Manager = null) where T : class => FromInstance(Instance, out _, Manager);
        public static ListData FromInstance<T>(T Instance, out string Table, DBManager Manager = null) where T : class => FromInstance(Instance, true, out Table, Manager);
        private static ListData FromInstance<T>(T Instance, bool WhereInclude, out string Table, DBManager Manager = null) where T : class
        {
            var type = Instance.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            List<ColumnData> Columns = new List<ColumnData>(10);
            List<ColumnWhere> Where = WhereInclude ? new List<ColumnWhere>(10) : null;
            List<ColumnOrderBy> Sort = new List<ColumnOrderBy>(10);

            var func = new BuildAction<dynamic, Type>((Info, Type) =>
            {
                var column = _GetColumns(Info, Type);
                if (column != null)
                {
                    SQLWhereAttribute iswhere = null;
                    SQLSortAttribute issort = null;

                    foreach (SQLSortAttribute sort in Info.GetCustomAttributes(SQLSortType, false)) issort = sort;
                    if (WhereInclude)
                        foreach (SQLWhereAttribute where in Info.GetCustomAttributes(SQLWhereType, false)) iswhere = where;

                    Columns.Add(column);
                    if(iswhere != null)
                    {
                        string wherekind = "=";
                        switch(iswhere.Kind)
                        {
                            case WhereKind.NotEqual: wherekind = "<>"; break;
                            case WhereKind.LIKE: wherekind = null; break;
                        }

                        Where.Add(ColumnWhere.Custom(column.ColumnName, Info.GetValue(Instance), wherekind, iswhere.Condition.ToString()));
                    }

                    if(issort != null) Sort.Add(new ColumnOrderBy(column.ColumnName, issort.Sort));
                }
            });

            for (int i = 0; i < properties.Length; i++)
                func(properties[i], properties[i].PropertyType);

            for (int i = 0; i < fields.Length; i++)
                func(fields[i], fields[i].FieldType);

            Table = type.Name;
            foreach (SQLTableAttribute table in type.GetCustomAttributes(SQLTableType, false)) Table = table.ToString(Manager);
            return new ListData(Columns, Where, Sort);
        }

        private static ColumnDataReflect _GetColumns(dynamic Info, Type type)
        {
            foreach (SQLColumnAttribute column in Info.GetCustomAttributes(SQLColumnType, false))
            {
                string col = string.IsNullOrEmpty(column.Name) ? Info.Name : column.Name;

                ColumnType col_type = column.Type == ColumnType.AUTO ? SQLColumnAttribute.CalulateType(type) : column.Type;

                if (col_type == ColumnType.ETC) throw new NotSupportedException("Unsupport SQL Column type!!");
                return new ColumnDataReflect(col, col_type, Info, type);
            }

            return null;
        }
    }
}
