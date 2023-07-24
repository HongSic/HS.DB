using HS.DB.Command;
using HS.Utils;
using HS.Utils.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Extension
{
    public sealed class ColumnWhere
    {
        private static readonly Random random = new Random();
        private const string DefaultOperator = "AND";

        public static ColumnWhere Raw(string WhereQuery, string Column, object Value, string Join = DefaultOperator) => new ColumnWhere(Column, Value, null, Join, WhereQuery);
        public static ColumnWhere Raw(string WhereQuery, string Join = DefaultOperator) => Raw(WhereQuery, null, null, Join);
        public static ColumnWhere Like(string Column, object Value, string Join = DefaultOperator) => new ColumnWhere(Column, Value, null, Join);
        public static ColumnWhere Custom(string Column, object Value, string Operator, string Join = DefaultOperator) => new ColumnWhere(Column, Value, Operator, Join);
        public static ColumnWhere Is(string Column, object Value, string Join = DefaultOperator) => new ColumnWhere(Column, Value, "=", Join);
        public static ColumnWhere IsNot(string Column, object Value, string Join = DefaultOperator) => new ColumnWhere(Column, Value, "<>", Join);
        public static ColumnWhere IsNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS ", Join);
        public static ColumnWhere IsNotNull(string Column, string Join = DefaultOperator) => new ColumnWhere(Column, null, " IS NOT ", Join);


        public string WhereQuery { get; set; }
        public bool IsRaw { get; set; }

        public string Column { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string Join { get; set; }
        public bool IncludeNull { get; set; }
        public SubWhere Sub { get; set; } = new SubWhere();

        public bool IsLike { get; private set; }

        /// <summary>
        /// where 바인딩을위한 키
        /// </summary>
        internal string BindKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Column">컬럼</param>
        /// <param name="Value">값</param>
        /// <param name="Operator">연산자 [=, !=, <>, ...]. 만약 null 이면 Like 문으로 간주</param>
        /// <param name="Join">현재 이 조건의 연결자 [AND, OR, ...]</param>
        private ColumnWhere(string Column, object Value, string Operator = "=", string Join = DefaultOperator, string WhereQuery = null)
        {
            this.Column = Column;
            this.Value = Value;
            this.Join = Join;
            this.Operator = Operator;
            this.WhereQuery = WhereQuery;

            IsLike = Operator == null;
            IsRaw = WhereQuery != null;


            this.BindKey = IsRaw ? Column : $"{Column}_{StringUtils.NextString(random, 10)}";
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
            string str;
            if (IsRaw) str = WhereQuery;
            else
            {
                char Prefix = Conn == null ? '\0' : Conn.StatementPrefix;
                //string Statement = ForStatement ? Conn.GetQuote($"{Prefix}{Row}") : Value.ToString();
                string Statement = ForStatement ? $"{Prefix}{BindKey}" : Convert.ToString(Value);
                string RowQuote = Conn == null ? Column : Conn.GetQuote(Column);

                str = Operator == null ?
                $"{RowQuote} LIKE CONCAT('%%', {(ForStatement ? Statement : Value)}, '%%') " :
                $"{RowQuote}{Operator}{(Value == null ? "NULL" : Statement)} ";
            }

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
                /*
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
                */

                foreach (var query in Queries)
                {
                    _QueryString(query, sb, First);
                    First = false;
                }

                return sb.ToString();
            }

            private void _QueryString(ColumnWhere data, StringBuilder sb, bool First)
            {
                // 노드가 null이면 아무것도 하지 않습니다.
                if (data == null) return;

                // 노드의 값을 추가합니다.
                sb.Append(data.ToString(Conn, true, !First));

                // 노드의 자식을 추가합니다.
                if (data.Sub.Count > 0)
                {
                    sb.Append($"{data.Sub.Operator} (");
                    for(int i = 0; i < data.Sub.Count; i++)
                        _QueryString(data.Sub[i], sb, i == 0);
                    sb.Append(')');
                }
            }

            public DBCommand Apply(DBCommand stmt)
            {
                Stack<ColumnWhere> stack = new Stack<ColumnWhere>(Queries);
                while(stack.Count > 0)
                {
                    var where = stack.Pop();
                    if (where != null && (where.IncludeNull || where.Value != null))
                        //stmt.Add(Conn.GetQuote($"{Prefix}{var.Row}"), var.Value);
                        stmt.Add($"{Prefix}{where.BindKey}", where.Value);

                    stack.PushAll(where?.Sub);
                }

                return stmt;
            }

            class Parenthesis
            {
                public Parenthesis(ColumnWhere Column, bool End) { this.Column = Column; this.End = End; }
                public ColumnWhere Column;
                public bool End;
            }
        }

        public class SubWhere : List<ColumnWhere>
        {
            internal SubWhere() : base(5) { }
            public string Operator { get; set; } = "AND";
        }
    }
}
