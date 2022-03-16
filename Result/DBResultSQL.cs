using System;
using System.Data.Common;

namespace HS.DB.Result
{
    public class DBResultSQL : DBResult
    {
        protected DBResultSQL() { }
        public DBResultSQL(DbCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBResultSQL(DbDataReader Reader)
        {
            this.Reader = Reader;

            _ColumnsCount = Reader.FieldCount;

            _Columns = new DBColumn[_ColumnsCount];
            for (int i = 0; i < _ColumnsCount; i++) _Columns[i] = new DBColumn(Reader.GetName(i), Reader.GetDataTypeName(i), Reader.GetFieldType(i));
        }

        #region 필드 Private 변수
        protected DbCommand Command;

        protected DBColumn[] _Columns;
        protected int _ColumnsCount;
        #endregion

        public DbDataReader Reader { get; protected set; }

        public override DBColumn[] Columns { get { return _Columns; } }
        public override int ColumnsCount { get { return _ColumnsCount; } }
        public override bool HasRows { get { return Reader.HasRows; } }

        //public override int Count { get { return Reader.FieldCount; } }

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

        public override void Dispose()
        {
            Reader.Close();
            Command?.Dispose();
        }
    }
}
