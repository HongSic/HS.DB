#if MSSQL_MICROSOFT
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;

namespace HS.DB.Data
{
    public class DBDataMSSQL : DBData
    {
        protected DBDataMSSQL() { }
        public DBDataMSSQL(SqlDataReader Reader)
        {
            this.Reader = Reader;

            _ColumnsCount = Reader.FieldCount;

            _Columns = new DBColumn[_ColumnsCount];
            for (int i = 0; i < _ColumnsCount; i++) _Columns[i] = new DBColumn(Reader.GetName(i), Reader.GetDataTypeName(i), Reader.GetFieldType(i));
        }

        #region 필드 Private 변수
        private DBColumn[] _Columns;
        private int _ColumnsCount;
        #endregion

        public SqlDataReader Reader { get; protected set; }

        public override DBColumn[] Columns { get{ return _Columns; } }
        public override int ColumnsCount { get { return _ColumnsCount; } }

        public override bool HasRows { get { return Reader.HasRows; } }

        public override object[] Current 
        {
            get
            {
                object[] data = new object[Reader.FieldCount];
                for (int i = 0; i < data.Length; i++) data[i] = Reader[i];
                return data;
            } 
        }
        public override object this[string Column] { get { return Reader[Column]; } }
        public override object this[int Index] { get { return Reader[Index]; } }

        public override bool MoveNext() { bool read = Reader.Read(); if (read) Offset++; return read; }
        public override void Reset() { throw new NotSupportedException(); }

        public override void Dispose() { Reader.Close(); }


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
