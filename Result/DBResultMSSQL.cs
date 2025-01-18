#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;

namespace HS.DB.Result
{
    public class DBResultMSSQL : DBResultSQL
    {
        protected DBResultMSSQL() { }
        public DBResultMSSQL(SqlCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBResultMSSQL(SqlDataReader Reader) : base(Reader) { this.Reader = Reader; }

        public new SqlCommand Command { get; protected set; }
        public new SqlDataReader Reader { get; protected set; }

        /*
        public static string GetJSON(DBData data, bool Bracket = false)
        {
            if (data == null) return null;

            int cnt = data.Columns.Length;
            if (cnt > 0)
            {
                StringBuilder sb = new StringBuilder(Bracket ? "{" : ""); //"{\"status\":\"ok\",\"message\":\"목록을 불러왔습니다.\","

                StringBuilder sb_type = new StringBuilder("\"types\":[");

                StringBuilder sb_name = new StringBuilder("\"cols\":[");

                sb_name.AppendFormat("\"{0}\"", data.Columns[0]);
                sb_type.AppendFormat("\"{0}\"", DBManager.GetTypeString(data.Columns[0].Type));
                for (int i = 1; i < cnt; i++)
                {
                    sb_type.AppendFormat(",\"{0}\"", DBManager.GetTypeString(data.Columns[0].Type));
                    sb_name.AppendFormat(",\"{0}\"", data.Columns[i].Value);
                }

                sb_name.Append("]");
                sb_type.Append("],");
                sb.Append(sb_type.ToString()).Append(sb_name.ToString()).Append(",\"rows\":[");

                if (cnt > 0)
                {
                    bool first = true;
                    for (int i = 0; i < data.Rows; i++)
                    {
                        object[] row = data[i];
                        sb.AppendFormat(first ? "[{0}" : ",[{0}", DBManager.GetString(row[0]));
                        first = false;

                        for (int j = 1; j < cnt; j++) sb.AppendFormat(",{0}", DBManager.GetString(row[j]));
                        sb.Append("]");
                    }
                }
                if (Bracket) sb.Append("}");
                return sb.ToString();
            }
            else return Bracket ? "{}" : "";
        }
        */
    }
}
