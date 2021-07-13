using System;

namespace HS.DB
{
    public class DBColumn
    {
        public DBColumn(string Name, string SQLType, Type Type) { this.Name = Name;  this.SQLType = SQLType; this.Type = Type; }
        public string Name { get; private set; }
        public string SQLType { get; private set; }
        public Type Type { get; private set; }

        public override string ToString() { return string.Format("{0}[{1}]", Name, SQLType); }
    }
}
