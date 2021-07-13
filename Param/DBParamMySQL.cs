using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Param
{
    public class DBParamMySQL : DBParam
    {
        private readonly MySqlParameter param;
        public DBParamMySQL(string Name, object Value) : base(Name, Value) { param = new MySqlParameter(Name, Value); }
        public DBParamMySQL(string Name, string Value, MySqlDbType Type) : base(Name, Value) { param = new MySqlParameter(Name, Type); param.Value = Value; }

        public MySqlDbType Type { get { return param.MySqlDbType; } }


        public static explicit operator MySqlParameter(DBParamMySQL Param) { return Param.param; }
    }
}
