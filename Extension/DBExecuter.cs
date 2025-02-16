using HS.DB.Command;
using HS.DB.Manager;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HS.DB.Extension
{
    public class DBExecuter
    {
        /*
        public delegate Task ListCallback(DBManager Conn, List<KeyObjectMemory> List);
        public static async Task<APIData> List(RouterData Router, APIModuleData Data, string Table, List<ColumnData> Columns, List<WhereCondition> Where, ListCallback Callback)
        {
            var Request = Router.Context.Request;

            //Page: 페이지 (기본값: 1)
            //Count: 한번에 가져올 수 (기본값: 15)
            //※메모※ Count 가 Page 보다 우선순위가 높음
            string Keyword = Request.GetParam("keyword", null);
            int Page = int.Parse(Request.GetParam("page", "1"));
            int Count = int.Parse(Request.GetParam("count", "15"));
            int Offset = Math.Max(Page - 1, 0) * Count;
        }
        */

        #region List
        /// <summary>
        /// 목록 불러오기 (컬럼 직접 지정)
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Offset">불러올 오프셋</param>
        /// <param name="Count">불러올 갯수 (-1 면 모두)</param>
        /// <param name="Data">목록 데이터</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<List<Dictionary<string, object>>> ListAsync(DBManager Conn, string Table, int Offset, int Count, ListData Data, bool Close = false) => await ListAsync(Conn, Table, Offset, Count, Data?.Columns, Data?.Where, Data?.Sort, Close);
        /// <summary>
        /// 목록 불러오기 (컬럼 직접 지정)
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Offset">불러올 오프셋</param>
        /// <param name="Count">불러올 갯수 (-1 면 모두)</param>
        /// <param name="Columns">가져올 열 (null 이면 모두 가져오기)</param>
        /// <param name="Where">조건</param>
        /// <param name="Sort">정렬</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<List<Dictionary<string, object>>> ListAsync(DBManager Conn, string Table, int Offset, int Count, IEnumerable<ColumnData> Columns = null, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, bool Close = false)
        {
            try
            {
                List<Dictionary<string, object>> data = null;
                using (var Result = await ListBuild(Conn, Table, Offset, Count, Columns, Where, Sort).ExcuteAsync())
                {
                    if (Result.HasRows)
                    {
                        data = new List<Dictionary<string, object>>();
                        int count = 1;
                        while (Result.MoveNext())
                        {
                            Dictionary<string, object> subarr = new Dictionary<string, object>(Result.ColumnsCount);
                            //번호 할당
                            if (Offset > -1) subarr.Add("no", Offset + count++);

                            if (Columns == null)
                            {
                                for (int i = 0; i < Result.ColumnsCount; i++)
                                    subarr.Add(Result.Columns[i].Name, Result[i]);
                            }
                            else
                            {
                                foreach (var Column in Columns)
                                    subarr.Add(Column.DisplayName, Result[Column.ColumnName]);
                            }
                            data.Add(subarr);
                        }
                    }
                }

                return data;
            }
            finally { if (Close) Conn.Dispose(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Conn"></param>
        /// <param name="Table"></param>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static DBCommand ListBuild(DBManager Conn, string Table, int Offset, int Count, ListData Data) => ListBuild(Conn, Table, Offset, Count, Data?.Columns, Data?.Where, Data?.Sort);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Offset">불러올 오프셋 ()</param>
        /// <param name="Count">불러올 갯수 (-1 면 모두)</param>
        /// <param name="Columns">가져올 열 (null 이면 모두 가져오기)</param>
        /// <param name="Where">조건</param>
        /// <param name="Sort">정렬 </param>
        /// <returns></returns>
        public static DBCommand ListBuild(DBManager Conn, string Table, int Offset, int Count, IEnumerable<ColumnData> Columns = null, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, IEnumerable<string> GroupBy = null)
        {
            var query = _ListBuild(Conn, Table, Offset, Count, Columns, Where, Sort, GroupBy, true, out var where);
            var Stmt = Conn.Prepare(query);
            //추가 조건절이 존재하면 할당
            where.Apply(Stmt);
            return Stmt;
        }
        public static string ListBuildString(DBManager Conn, string Table, int Offset, int Count, IEnumerable<ColumnData> Columns = null, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null, IEnumerable<string> GroupBy = null) => _ListBuild(Conn, Table, Offset, Count, Columns, Where, Sort, GroupBy, false, out var _);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">테이블 이름</param>
        /// <param name="Offset">불러올 오프셋 ()</param>
        /// <param name="Count">불러올 갯수 (-1 면 모두)</param>
        /// <param name="Columns">가져올 열 (null 이면 모두 가져오기)</param>
        /// <param name="Where">조건</param>
        /// <param name="Sort">정렬 </param>
        /// <returns></returns>
        private static string _ListBuild(DBManager Conn, string Table, int Offset, int Count, IEnumerable<ColumnData> Columns, IEnumerable<ColumnWhere> Where, IEnumerable<ColumnOrderBy> Sort, IEnumerable<string> GroupBy, bool UseStatement, out ColumnWhere.Statement where)
        {
            StringBuilder sb = new StringBuilder();

            where = ColumnWhere.JoinForStatement(Where, Conn);
            string where_query = where?.QueryString(UseStatement);

            sb.Append("SELECT ");
            if (Columns != null)
            {
                bool First = true;
                foreach (var col in Columns)
                {
                    string name = Conn.GetQuote(col.ColumnName);
                    if (First) sb.Append(name);
                    else sb.Append(',').Append(name);
                    First = false;
                }
            }
            else sb.Append(" * ");

            sb.Append(" FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            // GroupBy
            if (GroupBy != null)
            {
                bool GroupFirst = true;
                foreach (var group in GroupBy)
                {
                    if (!string.IsNullOrWhiteSpace(group))
                    {
                        if (GroupFirst) { sb.Append($" GROUP BY {group}"); GroupFirst = false; }
                        else sb.Append($", {group}");
                    }
                }
            }

            //정렬 연산자
            if (Sort != null)
            {
                bool First = true;
                foreach (var sort in Sort)
                {
                    if (sort != null)
                    {
                        if (First) sb.Append(" ORDER BY ");
                        else sb.Append(", ");
                        sb.Append(sort.ToString(Conn));
                        First = false;
                    }
                }
            }

            return Conn.ApplyLimitBuild(sb.ToString(), Offset, Count);
        }
        #endregion
    }
}
