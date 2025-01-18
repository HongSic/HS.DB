using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace HS.DB.Param
{
    public class DBParamOracle : DBParam
    {
        private readonly OracleParameter param;
        public DBParamOracle(string Name, object Value) : base(Name, Value) { param = new OracleParameter(Name, Value); }
        public DBParamOracle(string Name, string Value, OracleDbType Type) : base(Name, Value) { param = new OracleParameter(Name, Type); param.Value = Value; }

        public OracleDbType Type { get { return param.OracleDbType; } }


        public static explicit operator OracleParameter(DBParamOracle Param) { return Param.param; }
    }
}
