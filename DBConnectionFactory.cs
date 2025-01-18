using HS.DB.Connection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HS.DB
{
    public static class DBConnectionFactory
    {
        #region Connect
        public static DBManager Connect(DBConnectionKind Kind, string Server, string ID, string PW, string DB, int Timeout = 5000)
        {
            if (Kind == DBConnectionKind.MSSQL) return DBConnectionMSSQL.Connect(Server, ID, PW, DB, Timeout);
            else return null;
        }
        public static async Task<DBManager> ConnectAsync(DBConnectionKind Kind, string Server, string ID, string PW, string DB, int Timeout = 5000)
        {
            if (Kind == DBConnectionKind.MSSQL) return await DBConnectionMSSQL.ConnectAsync(Server, ID, PW, DB, Timeout);
            else return null;
        }
        #endregion
    }
}
