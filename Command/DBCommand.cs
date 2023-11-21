using HS.DB.Result;
using System;
using System.Data;
using System.Threading.Tasks;

namespace HS.DB.Command
{
    public abstract class DBCommand : IDisposable
    {
        public abstract string SQLQuery { get; }
        public abstract CommandType CommandType { get; set; }

        public abstract DBCommand Add(DBParam Param);
        public abstract DBCommand Add(object Value);
        public abstract DBCommand Add(string Key, object Value);

        public virtual DBCommand AddRange(params DBParam[] Params)
        {
            for (int i = 0; Params != null && i < Params.Length; i++) Add(Params[i]);
            return this;
        }

        public abstract DBResult Excute();
        public abstract Task<DBResult> ExcuteAsync();


        public abstract int ExcuteNonQuery();
        public abstract Task<int> ExcuteNonQueryAsync();


        public abstract object ExcuteOnce();
        public abstract Task<object> ExcuteOnceAsync();

        public abstract void Dispose();
    }
}
