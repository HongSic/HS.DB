using System;

namespace HS.DB
{
    public class DBValue
    {
        public DBValue(object Value, string SQLType, Type Type) { this.Value = Value; this.SQLType = SQLType; this.Type = Type; }
        public string SQLType { get; private set; }
        public Type Type { get; private set; }
        public object Value { get; private set; }

        public override string ToString() { return Value.ToString(); }
    }
}
