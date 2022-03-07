using HS.DB.Data;
using System;
using System.Threading.Tasks;

namespace HS.DB.Command
{
    public abstract class DBCommand : IDisposable
    {
        public abstract DBCommand Add(DBParam Param);
        public abstract DBCommand Add(object Value);

        public virtual DBCommand AddRange(params DBParam[] Params)
        {
            for (int i = 0; Params != null && i < Params.Length; i++) Add(Params[i]);
            return this;
        }

        public abstract DBData Excute();
        public abstract Task<DBData> ExcuteAsync();


        public abstract int ExcuteNonQuery();
        public abstract Task<int> ExcuteNonQueryAsync();


        public abstract object ExcuteOnce();
        public abstract Task<object> ExcuteOnceAsync();

        public abstract void Dispose();
    }
}
