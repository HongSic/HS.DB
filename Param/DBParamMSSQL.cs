#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Data;

namespace HS.DB.Param
{
    public class DBParamMSSQL : DBParam
    {
        SqlParameter param;
        public DBParamMSSQL(string Name, object Value) : base(Name, Value) { param = new SqlParameter(Name, Value); }
        public DBParamMSSQL(string Name, string Value, SqlDbType Type) : base(Name, Value) { param = new SqlParameter(Name, Type); param.Value = Value; }
        public SqlDbType Type { get { return param.SqlDbType; } }


        public static explicit operator SqlParameter(DBParamMSSQL Param) { return Param.param; }
    }
}
