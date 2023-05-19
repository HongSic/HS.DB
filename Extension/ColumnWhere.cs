using HS.DB.Command;
using HS.DB.Utils;
using HS.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Extension
{
    public sealed class ColumnWhere
    {
        private const string DefaultOperator = "AND";

        public static ColumnWhere Like(string Column, object Value, string Join = DefaultOperator) => new ColumnWhere(Column, Value, null, Join);
        public static ColumnWhere IsNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS ", Join);
        public static ColumnWhere IsNotNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS NOT ", Join);

        public string Column { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string Join { get; set; }
        public bool IncludeNull { get; set; }
        public List<ColumnWhere> Sub { get; set; } = new List<ColumnWhere>();

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

        public string ToString(DBManager Conn, bool ForStatement, bool Next = false, bool Parenthesis = false)
        {
            char Prefix = Conn == null ? '\0' : Conn.StatementPrefix;
            //string Statement = ForStatement ? Conn.GetQuote($"{Prefix}{Row}") : Value.ToString();
            string Statement = ForStatement ? $"{Prefix}{Column}" : Convert.ToString(Value);
            string RowQuote = Conn == null ? Column : Conn.GetQuote(Column);
            string pth = Parenthesis ? "(" : null;

            string str = Operator == null ?
            $"{pth}{RowQuote} LIKE CONCAT('%%', {(ForStatement ? Statement : Value)}, '%%') " :
            $"{pth}{RowQuote}{Operator}{(Value == null ? "NULL" : Statement)} ";

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
                this.Prefix = Conn.StatementPrefix;
            }

            public DBManager Conn;
            public IEnumerable<ColumnWhere> Queries;

            public string QueryString()
            {
                if (Queries == null) return null;

                StringBuilder sb = new StringBuilder();
                bool First = true;
                Stack<Parenthesis1> stack = new Stack<Parenthesis1>();
                stack.Push(new Parenthesis1(Queries, false));

                bool IsParenthesis = false;
                while (stack.Count > 0)
                {
                    var quries = stack.Pop();
                    foreach (var query in quries.Columns)
                    {
                        sb.Append(query.ToString(Conn, true, !First, IsParenthesis));

                        if (IsParenthesis = query.Sub?.Count > 0) stack.Push(new Parenthesis(query.Sub, true));
                        if (First) First = false;
                    }

                    if (quries.Close) sb.Append(")");
                }

                /*
                Stack<Parenthesis> stack = new Stack<Parenthesis>();
                foreach (var query in Queries) stack.Push(new Parenthesis(query, false));

                bool IsParenthesis = false;
                while (stack.Count > 0)
                {
                    var data = stack.Pop();
                    sb.Append(data.Column.ToString(Conn, true, !First, IsParenthesis));
                    if (First) First = false;

                    if (IsParenthesis = data.Column.Sub?.Count > 0)
                    {
                        foreach (var query in data.Column.Sub) stack.Push(new Parenthesis(query, false));
                        stack.Peek().End = true;
                    }

                    if (data.End) sb.Append(")");
                }
                */
                return sb.ToString();
            }

            public DBCommand Apply(DBCommand stmt)
            {
                Stack<ColumnWhere> stack = new Stack<ColumnWhere>(Queries);
                while(stack.Count > 0)
                {
                    var where = stack.Pop();
                    if (where != null && (where.IncludeNull || where.Value != null))
                        //stmt.Add(Conn.GetQuote($"{Prefix}{var.Row}"), var.Value);
                        stmt.Add($"{Prefix}{where.Column}", where.Value);

                    stack.PushAll(where?.Sub);
                }

                return stmt;
            }

            class Parenthesis1
            {
                public Parenthesis1(IEnumerable<ColumnWhere> Columns, bool Close) { this.Columns = Columns; this.Close = Close; }
                public IEnumerable<ColumnWhere> Columns;
                public bool Close;
            }
            class Parenthesis
            {
                public Parenthesis(ColumnWhere Column, bool End) { this.Column = Column; this.End = End; }
                public ColumnWhere Column;
                public bool End;
            }
        }
    }
}
