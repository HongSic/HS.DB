#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using HS.DB.Result;
using System;
using System.Data.Common;
using System.Text;

namespace HS.Utils
{
    public static class DBUtils
    {
        #region JSONException
        public static string JSONException(this DbException ex) { return JSONException(ex, "데이터베이스 문제로 명령이 실패하였습니다."); }
        public static string JSONException(this DbException ex, string Message)
        {
            StringBuilder sb = new StringBuilder("{\"result\":\"fail\",\"message\":\"");
            sb.Append(JSONUtils.EncodeJSON(Message)).Append("\",");
            sb.Append("\"exception\": {\"code\":").Append(ex.ErrorCode).Append("\"message\": \"").Append(ex.Message.EncodeJSON());
            sb.Append("\"}}");
            return sb.ToString();
        }
        public static string JSONException(this SqlException ex) { return JSONException(ex, "데이터베이스 문제로 명령이 실패하였습니다."); }
        public static string JSONException(this SqlException ex, string Message)
        {
            StringBuilder sb = new StringBuilder("{\"result\":\"fail\",\"message\":\"");
            sb.Append(JSONUtils.EncodeJSON(Message)).Append("\",");
            sb.Append("\"exception\": {\"code\":").Append(ex.Number).Append("\"message\": \"").Append(ex.Message.EncodeJSON());
            sb.Append("\"}}");
            return sb.ToString();
            //string JSONmsg = "{\"status\":\"fail\",\"message\":\"" + HttpUtility.JavaScriptStringEncode(Message) + "\",\"exception\":\"" +
            //HttpUtility.JavaScriptStringEncode(ex.Message) + "\",\"code_sql\":" + ex.ErrorCode + "}";
            //return JSONmsg;
        }
        #endregion

        #region ToJSON
        public static string ToJSON(this DBResult data, bool Bracket = true, bool KeyValuePair = false)
        {
            if (data == null) return null;

            int cnt = data.Columns.Length;
            if (cnt > 0)
            {
                StringBuilder sb = new StringBuilder(Bracket ? "{" : ""); //"{\"status\":\"ok\",\"message\":\"목록을 불러왔습니다.\","
                
                StringBuilder sb_type = new StringBuilder("\"types\":[");

                if (KeyValuePair)
                {
                    sb_type.AppendFormat("\"{0}\"", GetTypeString(data.Columns[0].Type));
                    for (int i = 1; i < cnt; i++) sb_type.AppendFormat(",\"{0}\"", GetTypeString(data.Columns[i].Type));

                    sb_type.Append("]");
                    sb.Append(sb_type.ToString()).Append(",\"rows\":[");
                }
                else
                {
                    StringBuilder sb_name = new StringBuilder("\"cols\":[");

                    sb_name.AppendFormat("\"{0}\"", data.Columns[0].Name);
                    sb_type.AppendFormat("\"{0}\"", GetTypeString(data.Columns[0].Type));
                    for (int i = 1; i < cnt; i++)
                    {
                        sb_type.AppendFormat(",\"{0}\"", GetTypeString(data.Columns[i].Type));
                        sb_name.AppendFormat(",\"{0}\"", data.Columns[i].Name);
                    }

                    sb_name.Append("]");
                    sb_type.Append("],");
                    sb.Append(sb_type.ToString()).Append(sb_name.ToString()).Append(",\"rows\":[");
                }

                if (cnt > 0)
                {
                    bool first = true;
                    while (data.MoveNext())
                    {
                        for (int i = 0; i < data.ColumnsCount; i++)
                        {
                            if (KeyValuePair)
                            {
                                if (i == 0) sb.Append(first ? "{" : ",{").AppendFormat("\"{0}\": {1}", data.Columns[i], JSONUtils.ToStringForJSON(data[0]));
                                else sb.AppendFormat(",\"{0}\":{1}", data.Columns[i], JSONUtils.ToStringForJSON(data[i]));
                            }
                            else
                            {
                                if (i == 0) sb.AppendFormat((first ? "[{0}" : ",[{0}"), JSONUtils.ToStringForJSON(data[0]));
                                else sb.AppendFormat(",{0}", JSONUtils.ToStringForJSON(data[i]));
                            }
                        }
                        sb.Append(KeyValuePair ? "}" : "]");
                        first = false;
                    }
                }
                sb.Append("]");
                if (Bracket) sb.Append("}");
                return sb.ToString();
            }
            else return Bracket ? "{}" : "";
        }

        public static string ToJSON(this DbDataReader reader, bool Bracket = true, bool KeyValuePair = false)
        {
            if (reader == null) return null;

            int cnt = reader.FieldCount;
            if (cnt > 0)
            {
                StringBuilder sb = new StringBuilder(Bracket ? "{" : ""); //"{\"status\":\"ok\",\"message\":\"목록을 불러왔습니다.\","

                StringBuilder sb_type = new StringBuilder("\"types\":[");

                string[] column = new string[cnt];
                column[0] = reader.GetName(0);
                if (KeyValuePair)
                {
                    sb_type.AppendFormat("\"{0}\"", GetTypeString(reader.GetFieldType(0)));
                    for (int i = 1; i < cnt; i++)
                    {
                        column[i] = reader.GetName(i);
                        sb_type.AppendFormat(",\"{0}\"", GetTypeString(reader.GetFieldType(i)));
                    }

                    sb_type.Append("]");
                    sb.Append(sb_type.ToString()).Append(",\"rows\":[");
                }
                else
                {
                    StringBuilder sb_name = new StringBuilder("\"cols\":[");

                    sb_name.AppendFormat("\"{0}\"", column[0]);
                    sb_type.AppendFormat("\"{0}\"", GetTypeString(reader.GetFieldType(0)));
                    for (int i = 1; i < cnt; i++)
                    {
                        column[i] = reader.GetName(i);
                        sb_type.AppendFormat(",\"{0}\"", GetTypeString(reader.GetFieldType(i)));
                        sb_name.AppendFormat(",\"{0}\"", column[i]);
                    }

                    sb_name.Append("]");
                    sb_type.Append("],");
                    sb.Append(sb_type.ToString()).Append(sb_name.ToString()).Append(",\"rows\":[");
                }

                if (cnt > 0)
                {
                    bool first = true;
                    while (reader.Read())
                    {
                        for (int i = 0; i < column.Length; i++)
                        {
                            if (KeyValuePair)
                            {
                                if (i == 0) sb.Append(first ? "{" : ",{").AppendFormat("\"{0}\": {1}", column[i], JSONUtils.ToStringForJSON(reader[i]));
                                else sb.AppendFormat(",\"{0}\":{1}", column[i], JSONUtils.ToStringForJSON(reader[i]));
                            }
                            else
                            {
                                if (i == 0) sb.AppendFormat((first ? "[{0}" : ",[{0}"), JSONUtils.ToStringForJSON(reader[i]));
                                else sb.AppendFormat(",{0}", JSONUtils.ToStringForJSON(reader[i]));
                            }
                        }
                        sb.Append(KeyValuePair ? "}" :"]");
                        first = false;
                    }
                }
                sb.Append("]");
                if (Bracket) sb.Append("}");
                return sb.ToString();
            }
            else return Bracket ? "{}" : "";
        }
        #endregion

        public static string GetString(object value, bool Quote = true)
        {
            if (value == null) return null;
            else
            {
                Type type = value.GetType();
                if (type == typeof(DBNull)) return null;
                else if (type == typeof(byte[])) return Convert.ToBase64String((byte[])value, Base64FormattingOptions.None);
                else if (type == typeof(byte) ||
                         type == typeof(short) ||
                         type == typeof(int) ||
                         type == typeof(float) ||
                         type == typeof(double) ||
                         type == typeof(decimal)) return Quote ? string.Format("\"{0}\"", value.ToString()) : value.ToString();
                else return Quote ? string.Format("\"{0}\"", value.ToString()) : value.ToString();
            }
        }
        public static string GetTypeString(Type type)
        {
            if (type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(int)) return "number";
            if (type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal)) return "number";//"number_point";
            else if (type == typeof(byte[])) return "array_byte";
            else if (type == typeof(DateTime)) return "datetime";
            else if (type == typeof(TimeSpan)) return "timespan";
            else return "string";
        }
    }
}
