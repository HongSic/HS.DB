﻿using System;
using System.Diagnostics;

//https://www.csharpstudy.com/Data/EF-annotation.aspx
namespace HS.DB.Extension.Attributes
{
    public class SQLColumnAttribute : Attribute
    {
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


#if NETCORE || NETSTANDARD
        internal static readonly Type TYPE_NULLABLE = typeof(Nullable);
#endif
        internal static readonly Type TYPE_STRING = typeof(string);
        internal static readonly Type TYPE_CHAR = typeof(char);
        internal static readonly Type TYPE_ENUM = typeof(Enum);
        internal static readonly Type TYPE_BOOL = typeof(bool);
        internal static readonly Type TYPE_BYTE = typeof(byte);
        internal static readonly Type TYPE_SBYTE = typeof(sbyte);
        internal static readonly Type TYPE_SHORT = typeof(short);
        internal static readonly Type TYPE_USHORT = typeof(ushort);
        internal static readonly Type TYPE_INT = typeof(int);
        internal static readonly Type TYPE_UINT = typeof(uint);
        internal static readonly Type TYPE_LONG = typeof(long);
        internal static readonly Type TYPE_ULONG = typeof(ulong);
        internal static readonly Type TYPE_DECIMAL = typeof(decimal);
        internal static readonly Type TYPE_DOUBLE = typeof(double);
        internal static readonly Type TYPE_FLOAT = typeof(float);
        internal static readonly Type TYPE_DATETIME = typeof(DateTime);
        internal static readonly Type TYPE_DATA = typeof(byte[]);
        public static ColumnType CalulateType(Type type)
        {
            Debug.WriteLine(type.FullName);
            if (type.Equals(TYPE_STRING)) return ColumnType.STRING;
            else if (type.Equals(TYPE_BOOL)) return ColumnType.BOOL;
            else if (type.Equals(TYPE_BYTE) ||
                     type.Equals(TYPE_SBYTE) ||
                     type.Equals(TYPE_SHORT) ||
                     type.Equals(TYPE_USHORT) ||
                     type.Equals(TYPE_INT) ||
                     type.Equals(TYPE_UINT) ||
                     type.Equals(TYPE_LONG) ||
                     type.Equals(TYPE_ULONG)) return ColumnType.NUMBER;
            else if (type.Equals(TYPE_DECIMAL) ||
                     type.Equals(TYPE_DOUBLE) ||
                     type.Equals(TYPE_FLOAT)) return ColumnType.DECIMAL;
            else if (type.Equals(TYPE_DATETIME)) return ColumnType.DATETIME;
            else if (type.Equals(TYPE_DATA)) return ColumnType.BIN;
            else
            {
                if (type.BaseType.Equals(TYPE_ENUM)) return ColumnType.NUMBER;
#if NETCORE || NETSTANDARD
                else if (type.Namespace == TYPE_NULLABLE.Namespace &&
                         type.Name.StartsWith(TYPE_NULLABLE.Name)) return CalulateType(Nullable.GetUnderlyingType(type));
#endif
                return ColumnType.ETC;
            }
        }
    }
}
