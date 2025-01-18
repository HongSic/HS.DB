using System.Data;

namespace HS.DB
{
    public abstract class DBParam
    {
        public DBParam(string Key, object Value) { this.Key = Key; this.Value = Value;}
        public virtual string Key { get; private set; }
        public virtual object Value { get; private set; }
    }
}
