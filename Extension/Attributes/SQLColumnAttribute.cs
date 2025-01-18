using HS.Utils.Convert;
using System;
using System.Diagnostics;
using System.Reflection;

//https://www.csharpstudy.com/Data/EF-annotation.aspx
namespace HS.DB.Extension.Attributes
{
    public class SQLColumnAttribute : Attribute
    {
        public static readonly Type ClassType = typeof(SQLColumnAttribute);
        /// <summary>
        /// 컬럼 이름
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Primary Key, Unique 여부
        /// </summary>
        public bool Key { get; set; }
        /// <summary>
        /// 컬럼 타입
        /// </summary>
        public ColumnType Type { get; set; }
        /// <summary>
        /// 무시할 값 
        /// (해당 값을 가진 컬럼은 주어진 값이랑 일치하면 조회/수정/삽입시 영향을 받지 않습니다)
        /// </summary>
        public object IgnoreValue { get; set; }
        /// <summary>
        /// 무시할 값 사용 여부
        /// </summary>
        public bool UseIgnoreValue { get; set; }
        /// <summary>
        /// 최대 자릿수 (0 이면 미사용)
        /// </summary>
        public int MaxLength { get; set; }
        /// <summary>
        /// 검색가능 여부
        /// </summary>
        public bool Searchable { get; set; }

        public override string ToString() => Name;

        public SQLColumnAttribute() { }

        public SQLColumnAttribute(string Name, int MaxLength = 0)
        {
            this.Name = Name;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(string Name, ColumnType Type, int MaxLength = 0)
        {
            this.Type = Type;
            this.Name = Name;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(string Name, bool Key, bool UseIgnoreValue = false, object IgnoreValue = null, int MaxLength = 0)
        {
            this.Name = Name;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(string Name, ColumnType Type, bool Key, bool UseIgnoreValue = false, object IgnoreValue = null, int MaxLength = 0)
        {
            this.Type = Type;
            this.Name = Name;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(string Name, bool Key, ColumnType Type, int MaxLength = 0, bool UseIgnoreValue = false, object IgnoreValue = null)
        {
            this.Type = Type;
            this.Name = Name;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(string Name, bool Key, ColumnType Type, bool Searchable, int MaxLength = 0, bool UseIgnoreValue = false, object IgnoreValue = null)
        {
            this.Type = Type;
            this.Name = Name;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
            this.Searchable = Searchable;
        }

        public SQLColumnAttribute(bool Key, int MaxLength = 0, bool UseIgnoreValue = false, object IgnoreValue = null)
        {
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(bool Key, ColumnType Type, int MaxLength = 0, bool UseIgnoreValue = false, object IgnoreValue = null)
        {
            this.Type = Type;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(ColumnType Type, int MaxLength = 0)
        {
            this.Type = Type;
            this.MaxLength = MaxLength;
        }
        public SQLColumnAttribute(ColumnType Type, bool Key, int MaxLength = 0, bool UseIgnoreValue = false, object IgnoreValue = null)
        {
            this.Type = Type;
            this.Key = Key;
            this.UseIgnoreValue = UseIgnoreValue;
            this.IgnoreValue = IgnoreValue;
            this.MaxLength = MaxLength;
        }


        public static ColumnType CalulateType(Type type)
        {
            Debug.WriteLine(type.FullName);
            if (type.Equals(ConvertUtils.TYPE_STRING)) return ColumnType.STRING;
            else if (type.Equals(ConvertUtils.TYPE_BOOL)) return ColumnType.BOOL;
            else if (type.Equals(ConvertUtils.TYPE_BYTE) ||
                     type.Equals(ConvertUtils.TYPE_SBYTE) ||
                     type.Equals(ConvertUtils.TYPE_SHORT) ||
                     type.Equals(ConvertUtils.TYPE_USHORT) ||
                     type.Equals(ConvertUtils.TYPE_INT) ||
                     type.Equals(ConvertUtils.TYPE_UINT) ||
                     type.Equals(ConvertUtils.TYPE_LONG) ||
                     type.Equals(ConvertUtils.TYPE_ULONG)) return ColumnType.NUMBER;
            else if (type.Equals(ConvertUtils.TYPE_DECIMAL) ||
                     type.Equals(ConvertUtils.TYPE_DOUBLE) ||
                     type.Equals(ConvertUtils.TYPE_FLOAT)) return ColumnType.DECIMAL;
            else if (type.Equals(ConvertUtils.TYPE_DATETIME)) return ColumnType.DATETIME;
            else if (type.Equals(ConvertUtils.TYPE_DATA)) return ColumnType.BIN;
            else
            {
                if (type.BaseType.Equals(ConvertUtils.TYPE_ENUM)) return ColumnType.NUMBER;
#if NETCORE || NETSTANDARD
                else if (type.Namespace == ConvertUtils.TYPE_NULLABLE.Namespace &&
                         type.Name.StartsWith(ConvertUtils.TYPE_NULLABLE.Name)) return CalulateType(Nullable.GetUnderlyingType(type));
#endif
                return ColumnType.ETC;
            }
        }



        //private delegate string StringCallback<T>(T variable);
        /// <summary>
        /// 해당 클래스의 프로퍼티 및 필드 변수로부터 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Name"></param>
        /// <param name="Sort"></param>
        /// <returns></returns>
        public static string GetColumnFromName<T>(string Name, out SQLColumnAttribute Column)
        {
            Column = null;
            if (string.IsNullOrWhiteSpace(Name)) { return null; }
            Type type = typeof(T);

            string ColName = _GetColumnFromName(type.GetProperty(Name), out Column);
            if (ColName == null) return _GetColumnFromName(type.GetField(Name), out Column);
            return ColName;
        }
        private static string _GetColumnFromName(dynamic Info, out SQLColumnAttribute Column)
        {
            Column = null;
            if (Info != null)
            {
                foreach (SQLColumnAttribute column in Info.GetCustomAttributes(ClassType, false))
                {
                    Column = column;
                    return Column.Name ?? Info.Name;
                }
            }

            return null;
        }
    }
}