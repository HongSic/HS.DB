using System;

namespace HS.DB.Extension.Attributes
{
    public sealed class SQLSortAttribute : Attribute
    {
        public ColumnSort Sort { get; set; }
        public SQLSortAttribute(ColumnSort Sort)
        {
            this.Sort = Sort;
        }

        internal string ToString(string ColumnName) => $" ORDER BY `{ColumnName}` {(Sort == ColumnSort.ASC ? "ASC" : "DESC") }";
    }
}
