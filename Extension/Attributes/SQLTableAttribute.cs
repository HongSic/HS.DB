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
            string table = string.IsNullOrEmpty(Name) ? TableAlt : Name;
            if (!string.IsNullOrEmpty(table)) table = Quote ? $"`{table}`" : table;

            string scheme = Scheme;
            if (!string.IsNullOrEmpty(Scheme)) scheme += '.';

            return scheme + table;
        }
        public override string ToString() => ToString(false);
    }
}
