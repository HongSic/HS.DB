using System;

namespace HS.DB.Extension.Attributes
{

    public sealed class SQLWhereAttribute : Attribute
    {
        public WhereKind Kind { get; set; }
        public WhereCondition Condition { get; set; }
        public SQLWhereAttribute(WhereKind Kind = WhereKind.Equal, WhereCondition Condition = WhereCondition.AND)
        {
            this.Kind = Kind;
            this.Condition = Condition;
        }
    }
}
