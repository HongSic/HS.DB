using HS.Utils;
using System;
using HS.Utils.Text;

namespace HS.DB.Extension
{
    public class ColumnData
    {
        public ColumnData(string ColumnName, ColumnType Type = ColumnType.AUTO) : this(ColumnName, null, Type) { }
        public ColumnData(string ColumnName, string DisplayName, ColumnType Type = ColumnType.AUTO)
        {
            this.ColumnName = ColumnName;
            this.DisplayName = StringUtils.IsNullOrWhiteSpace(DisplayName) ? ColumnName : DisplayName;
            this.Type = Type;
        }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public ColumnType Type { get; set; }

        public static object Convert(object Value, ColumnType Type)
        {
            switch (Type)
            {
                /*
                case ColumnType.BOOL:
                    string value = Value.ToString();
                    if (int.TryParse(value, out int result)) return result > 0;
                    else return value.ToLower() == "true";
                */
                case ColumnType.NUMBER: return int.Parse(Value.ToString());
                case ColumnType.DECIMAL: return double.Parse(Value.ToString());
                case ColumnType.STRING: return Value.ToString();
                //case ColumnType.BIN_BASE64: return System.Convert.ToBase64String((byte[])Value);
                default: return Value;
            }
        }
    }

    public class ColumnDataReflect : ColumnData
    {
        public dynamic Info { get; private set; }
        public Type TypeRef { get; private set; }
        internal ColumnDataReflect(string ColumnName, ColumnType Type, dynamic Info, Type TypeRef) : base(ColumnName, Type)
        {
            this.Info = Info;
            this.TypeRef = TypeRef;
        }
    }
}
