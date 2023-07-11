using System;

namespace HS.DB.Extension.Attributes
{
    public sealed class SQLSortAttribute : Attribute
    {
        internal string Column { get; set; }

        public ColumnSort Sort { get; set; }
        public SQLSortAttribute(ColumnSort Sort = ColumnSort.ASC)
        {
            this.Sort = Sort;
        }

        internal string ToString(DBManager Manager) => $"{Manager.GetQuote(Column)} {(Sort == ColumnSort.ASC ? "ASC" : "DESC") }";
    }
}
