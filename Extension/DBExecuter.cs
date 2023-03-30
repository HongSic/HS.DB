using HS.DB.Command;
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
        public static async Task<List<Dictionary<string, object>>> List(DBManager Conn, string Table, int Offset, int Count, ListData Data, bool Close = false) => await List(Conn, Table, Offset, Count, Data?.Columns, Data?.Where, Data?.Sort, Close);
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
        public static async Task<List<Dictionary<string, object>>> List(DBManager Conn, string Table, int Offset, int Count, List<ColumnData> Columns = null, List<ColumnWhere> Where = null, ColumnOrderBy Sort = null, bool Close = false)
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
                            Dictionary<string, object> subarr = new Dictionary<string, object>(Columns == null ? Result.ColumnsCount : Columns.Count);
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
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Offset">불러올 오프셋</param>
        /// <param name="Count">불러올 갯수 (-1 면 모두)</param>
        /// <param name="Columns">가져올 열 (null 이면 모두 가져오기)</param>
        /// <param name="Where">조건</param>
        /// <param name="Sort">정렬</param>
        /// <returns></returns>
        public static DBCommand ListBuild(DBManager Conn, string Table, int Offset, int Count, List<ColumnData> Columns = null, List<ColumnWhere> Where = null, ColumnOrderBy Sort = null)
        {
            var where = ColumnWhere.JoinForStatement(Where);
            string where_query = where?.QueryString();
            string where_limit = Count < 0 ? null : $" LIMIT {Offset}, {Count}";

            StringBuilder sb = new StringBuilder("SELECT ");
            if (Columns?.Count > 0)
            {
                bool First = true;
                foreach (var col in Columns)
                {
                    string name = $"`{col.ColumnName}`";
                    if (First) sb.Append(name);
                    else sb.Append(',').Append(name);
                    First = false;
                }
            }
            else sb.Append(" * ");

            sb.Append(" FROM ").Append(Table);

            //추가 조건절
            if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

            //정렬 연산자
            if (Sort != null) sb.Append(Sort.ToString()).Append(where_limit);

            var Stmt = Conn.Prepare(sb.ToString());
            //추가 조건절이 존재하면 할당
            if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);
            return Stmt;
        }
        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<long> Count(DBManager Conn, string Table, List<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                var where = ColumnWhere.JoinForStatement(Where);
                string where_query = where?.QueryString();
                StringBuilder sb = new StringBuilder("SELECT COUNT(*) FROM ").Append(Table);

                //추가 조건절
                if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

                using (var Stmt = Conn.Prepare(sb.ToString()))
                {

                    //추가 조건절이 존재하면 할당
                    if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);

                    using (var Result = await Stmt.ExcuteAsync())
                    {
                        Result.MoveNext();
                        return (long)Result[0];
                    }
                }
            }
            finally { if (Close) Conn.Dispose(); }
        }
    }
}
