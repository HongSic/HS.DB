using HS.DB.Command;
using HS.DB.Utils;
using HS.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Extension
{
    public sealed class ColumnWhere
    {
        private const string DefaultOperator = "AND";

        public static ColumnWhere Like(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, null, Join);
        public static ColumnWhere IsNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS ", Join);
        public static ColumnWhere IsNotNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS NOT ", Join);

        public string Column;
        public string Operator;
        public object Value;
        public string Join;
        public bool IsLike { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Column">컬럼</param>
        /// <param name="Value">값</param>
        /// <param name="Operator">연산자 [=, !=, <>, ...]. 만약 null 이면 Like 문으로 간주</param>
        /// <param name="Join">현재 이 조건의 연결자 [AND, OR, ...]</param>
        public ColumnWhere(string Column, object Value, string Operator = "=", string Join = DefaultOperator)
        {
            this.Column = Column;
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

        public string ToString(DBManager Conn, bool ForStatement, bool Next = false)
        {
            char Prefix = Conn == null ? '\0' : Conn.GetStatementPrefix();
            //string Statement = ForStatement ? Conn.GetQuote($"{Prefix}{Row}") : Value.ToString();
            string Statement = ForStatement ? $"{Prefix}{Column}" : Convert.ToString(Value);
            string RowQuote = Conn == null ? Column : Conn.GetQuote(Column);

            string str = Operator == null ?
            $"{RowQuote} LIKE CONCAT('%%', {(ForStatement ? Statement : Value)}, '%%') " :
            $"{RowQuote}{Operator}{(Value == null ? "NULL" : Statement)} ";

            if (Next) str = $"{Join} {str}";

            return str;
        }
        public override string ToString() => ToString(null, false);

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

        public static Statement JoinForStatement(IEnumerable<ColumnWhere> Queries, DBManager Conn) => new Statement(Queries, Conn);

        public class Statement
        {
            private readonly char Prefix;
            public Statement(IEnumerable<ColumnWhere> Queries, DBManager Conn)
            {
                this.Queries = Queries;
                this.Conn = Conn;
                this.Prefix = Conn.GetStatementPrefix();
            }

            public DBManager Conn;
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
                        sb.Append(" ").Append(query.ToString(Conn, true, !First));
                        if (First) First = false;
                    }
                }
                return sb.ToString();
            }

            public DBCommand Apply(DBCommand stmt)
            {
                foreach (var var in Queries)
                    if(var != null && var.Value != null) 
                        //stmt.Add(Conn.GetQuote($"{Prefix}{var.Row}"), var.Value);
                        stmt.Add($"{Prefix}{var.Column}", var.Value);

                return stmt;
            }
        }
    }
}
