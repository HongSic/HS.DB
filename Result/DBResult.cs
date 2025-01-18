using System;
using System.Collections;
using System.Collections.Generic;

namespace HS.DB.Result
{
    public abstract class DBResult : IEnumerator<object[]>, IDisposable
    {
        int _Offset = -1;

        public abstract object this[string Column] { get; }
        public abstract object this[int Index] { get; }

        public abstract DBColumn[] Columns { get; }
        public abstract int ColumnsCount { get; }
        public abstract bool HasRows { get; }
        public virtual int Offset { get { return _Offset; } protected set { _Offset = value; } }

        //public abstract int Count { get; }

        //public abstract bool Exist(string Column);

        #region IEnumerator 구현
        object IEnumerator.Current { get { return Current; } }
        public abstract object[] Current { get; }
        public abstract bool MoveNext();
        public abstract void Reset();
        public abstract void Dispose();
        #endregion

        public virtual object GetCurrent(int Index)
        {
            if(HasRows && ColumnsCount > 0)
            {
                if (Offset < 0) MoveNext();
                return Current[Index];
            }
            return null;
        }

        public virtual bool ColumnExist(string Column, bool MathchCase = false) { return ColumnIndex(Column, MathchCase) > -1; }
        public virtual DBColumn ColumnFromName(string Column, bool MathchCase = false) 
        {
            int index = ColumnIndex(Column, MathchCase);
            return index > -1 ? Columns[index] : null;
        }

        public virtual int ColumnIndex(string Column, bool MathchCase = false)
        {
            if (ColumnsCount > 0)
            {
                for (int i = 0; i < ColumnsCount; i++)
                {
                    if ((MathchCase && Columns[i].Name == Column) ||
                        Columns[i].Name.ToLower() == Column.ToLower()) return i;
                }
            }

            return -1;
        }
    }
}
