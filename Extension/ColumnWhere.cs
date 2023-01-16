using HS.DB.Command;
using HS.DB.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Extension
{
    public sealed class ColumnWhere
    {
        public string Row;
        public string Operator;
        public object Value;
        public string Join;
        public bool IsLike { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Row">컬럼</param>
        /// <param name="Value">값</param>
        /// <param name="Operator">연산자 [=, !=, <>, ...]. 만약 null 이면 Like 문으로 간주</param>
        /// <param name="Join">현재 이 조건의 연결자 [AND, OR, ...]</param>
        public ColumnWhere(string Row, object Value, string Operator = null, string Join = "AND")
        {
            this.Row = Row;
            this.Value = Value;
            this.Join = Join;
            this.Operator = Operator;
            this.IsLike = Operator == null;
        }

        public Enum ValueType()
        {
            Type type = Value?.GetType();
            switch (type.Name)
            {
                case "Int64":
                case "Int32": return ColumnType.NUMBER;
                case "Single":
                case "Double": return ColumnType.DECIMAL;
                case "String": return ColumnType.STRING;
                default: return ColumnType.ETC;
            }
        }

        public string ToString(bool ForStatement, bool Next = false)
        {
            string str = Operator == null ?
            $"`{Row}` LIKE CONCAT('%%', {(ForStatement ? $"@{Row}" : Value)}, '%%') " :
            $"`{Row}`{Operator}{(ForStatement ? $"@{Row}" : Value)} ";

            if (Next) str = $"{Join} {str}";

            return str;
        }
        public override string ToString() => ToString(false);

        /**
         * @param QueryCondition[] $Queries
         * @return string|null
         */
        /*
        public static string Join(QueryCondition[] Queries)
        {
            if (Queries == null) return null;

            string[] strs = new string[Queries.Length];
            for(int i = 0; i < Queries.Length; i++) strs[i] = Queries[i].ToString(false);
            return PHPUtils.implode(' ', strs);
        }
        */

        public static Statement JoinForStatement(IEnumerable<ColumnWhere> Queries) => new Statement(Queries);

        public class Statement
        {
            public Statement(IEnumerable<ColumnWhere> Queries)
            {
                this.Queries = Queries;
            }
            public IEnumerable<ColumnWhere> Queries;

            public string QueryString()
            {
                if (Queries == null) return null;

                StringBuilder sb = new StringBuilder();
                bool First = true;
                foreach (var query in Queries)
                {
                    if(query != null)
                    {
                        sb.Append(" ").Append(query.ToString(true, !First));
                        if (First) First = false;
                    }
                }
                return sb.ToString();
            }

            public DBCommand Apply(DBCommand stmt)
            {
                foreach (var var in Queries)
                    if(var != null) stmt.Add($"@{var.Row}", var.Value);

                return stmt;
            }
        }
    }
}
