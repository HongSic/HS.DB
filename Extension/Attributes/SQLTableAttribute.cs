using System;

namespace HS.DB.Extension.Attributes
{
    public sealed class SQLTableAttribute : Attribute
    {
        public string Name { get; set; }
        public string Scheme { get; set; }
        public SQLTableAttribute(string Name = null, string Scheme = null)
        {
            this.Name = Name;
            this.Scheme = Scheme;
        }

        public string ToString(bool Quote, string TableAlt = null)
        {
            string table = null;
            if (!string.IsNullOrEmpty(Scheme)) table = $"{Scheme}.";
            if (string.IsNullOrEmpty(Name)) table += TableAlt; 

            if (!string.IsNullOrEmpty(table)) table += Quote ? $"`{Name}`" : Name;
            return table;
        }
        public override string ToString() => ToString(false);
    }
}
