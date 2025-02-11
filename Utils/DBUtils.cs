#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Data.Common;
using System.Text;
using HS.DB.Result;
using HS.Utils.Text;
using System.Collections.Generic;

namespace HS.Utils
{
    public static class DBUtils
    {
        #region Type
        public static readonly Type TYPE_STRING = typeof(string);

        #region Numric
        public static readonly Type TYPE_BYTE = typeof(byte);
        public static readonly Type TYPE_SBYTE = typeof(sbyte);
        public static readonly Type TYPE_SHORT = typeof(short);
        public static readonly Type TYPE_USHORT = typeof(ushort);
        public static readonly Type TYPE_INT = typeof(int);
        public static readonly Type TYPE_UINT = typeof(uint);
        public static readonly Type TYPE_LONG = typeof(long);
        public static readonly Type TYPE_ULONG = typeof(ulong);
        public static readonly Type TYPE_FLOAT = typeof(float);
        public static readonly Type TYPE_DOUBLE = typeof(double);
        public static readonly Type TYPE_DECIMAL = typeof(decimal);
        #endregion
        #region Time / Date
        public static readonly Type TYPE_TIMESPAN = typeof(TimeSpan);
        public static readonly Type TYPE_DATETIME = typeof(DateTime);
        public static readonly Type TYPE_DATETIME_OFFSET = typeof(DateTimeOffset);
        #endregion
        #region Array
        public static readonly Type TYPE_ARRAY_BYTE = typeof(byte[]);
        public static readonly Type TYPE_LIST_BYTE = typeof(List<byte>);
        #endregion

        public static bool IsTypeInteger(this Type type)
        {
            return
                type == TYPE_BYTE || type == TYPE_SBYTE ||
                type == TYPE_SHORT || type == TYPE_USHORT ||
                type == TYPE_INT || type == TYPE_UINT ||
                type == TYPE_LONG || type == TYPE_ULONG;
        }
        public static bool IsTypeNumeric(this Type type)
        {
            return type == TYPE_FLOAT || type == TYPE_DOUBLE || type == TYPE_DECIMAL;
        }
        public static bool IsTypeDateTime(this Type type)
        {
            return type == TYPE_DATETIME || type == TYPE_DATETIME_OFFSET;
        }
        #endregion

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
                else if (type == TYPE_ARRAY_BYTE) return System.Convert.ToBase64String((byte[])value, Base64FormattingOptions.None);
                else if (IsTypeInteger(type) || IsTypeNumeric(type)) return Quote ? string.Format("\"{0}\"", value.ToString()) : value.ToString();
                else return Quote ? string.Format("\"{0}\"", value.ToString()) : value.ToString();
            }
        }
        public static string GetTypeString(Type type)
        {
            if (IsTypeInteger(type)) return "integer";
            if (IsTypeNumeric(type)) return "numeric";
            if (type == TYPE_ARRAY_BYTE) return "array_byte";
            if (IsTypeDateTime(type)) return "datetime";
            if (type == TYPE_TIMESPAN) return "timespan";
            else return "string";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDBString(dynamic value)
        {
            if (value == null) return "null";
            else
            {
                var type = value.GetType();
                string _value;

                if (IsTypeInteger(type) || IsTypeNumeric(type)) return value.ToString();
                else if (IsTypeDateTime(type)) _value = value.ToString("yyyy-MM-dd hh:mm:ss.fff");
                else if (type == TYPE_ARRAY_BYTE || type == TYPE_LIST_BYTE)
                {
                    //_value = BitConverter.ToString(value).Replace("-", "");
                    StringBuilder sb = new StringBuilder("0x");
                    int Count = type == TYPE_ARRAY_BYTE ? value.Length : value.Count;
                    for (int i = 0; i < Count; i++) sb.AppendFormat("{0:X2}", value[i]);
                    return sb.ToString();
                }
                else if (type == TYPE_TIMESPAN) _value = value.ToString("hh:mm:ss.fff");
                else _value = value.ToString();
                return $"'{_value}'";
            }
        }
    }
}
