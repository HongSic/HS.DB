namespace HS.DB.Extension
{
    public class ColumnOrderBy
    {
        public ColumnOrderBy(string Column, ColumnSort Sort = ColumnSort.ASC)
        {
            this.Column = Column;
            this.Sort = Sort;
        }
        public string Column { get; set; }
        public ColumnSort Sort { get; set; }

        public override string ToString() => $"{Column} {(Sort == ColumnSort.DESC ? "DESC" : "ASC")} ";
        public string ToString(DBManager Manager) => $"{Manager.GetQuote(Column)} {(Sort == ColumnSort.DESC ? "DESC" : "ASC")} ";
    }
}
