using HS.DB.Command;
using HS.DB.Manager;
using HS.Utils;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public static DBCommand ListBuild(DBManager Conn, string Table, int Offset, int Count, IEnumerable<ColumnData> Columns = null, IEnumerable<ColumnWhere> Where = null, IEnumerable<ColumnOrderBy> Sort = null)
        {
            StringBuilder sb = new StringBuilder();

            var where = ColumnWhere.JoinForStatement(Where, Conn);
            string where_query = where?.QueryString();

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

            //정렬 연산자
            if (Sort != null)
            {
                bool First = true;
                foreach (var sort in Sort)
                {
                    if (First) sb.Append(" ORDER BY ");
                    else sb.Append(", ");
                    sb.Append(sort.ToString(Conn));
                    First = false;
                }
            }

            //
            string query = LimitBuild(Conn, sb.ToString(), Offset, Count);
            var Stmt = Conn.Prepare(query);
            //추가 조건절이 존재하면 할당
            if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);
            return Stmt;
        }

        /// <summary>
        /// Add LIMIT / OFFSET Query (support Oracle, MSSQL(2016~), MySQL)
        /// </summary>
        /// <param name="DB"></param>
        /// <param name="SQLQuery"></param>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public static string LimitBuild(DBManager Conn, string SQLQuery, int Offset, int Count)
        {
            StringBuilder builder = new StringBuilder();
            string where_limit = null;
            if (Count > 0)
            {
                if (Conn is DBManagerMySQL) where_limit = $" LIMIT {Offset}, {Count}";
                else if (Conn is DBManagerMSSQL) where_limit = $" OFFSET {Offset} ROWS FETCH NEXT {Count} ROWS ONLY";
                else if (Conn is DBManagerOracle)
                {
                    //Conn.Connector.ServerVersion
                    builder.Append("SELECT * FROM (SELECT a.*, ROWNUM rnum FROM (");
                    where_limit = $") a WHERE ROWNUM <= {Count + Offset}) WHERE rnum  >= {Offset};";
                }
            }
            builder.Append(SQLQuery).Append(where_limit);
            return builder.ToString();
        }
        #endregion

        #region Count
        /// <summary>
        /// 갯수 구하기
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<long> CountAsync(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                var where = ColumnWhere.JoinForStatement(Where, Conn);
                string where_query = where?.QueryString();
                StringBuilder sb = new StringBuilder("SELECT COUNT(*) FROM ").Append(Table);

                //추가 조건절
                if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

                using (var Stmt = Conn.Prepare(sb.ToString()))
                {
                    //추가 조건절이 존재하면 할당
                    if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);

                    return Convert.ToInt64(await Stmt.ExcuteOnceAsync());
                }
            }
            finally { if (Close) Conn.Dispose(); }
        }
        #endregion

        #region Delete
        /// <summary>
        /// 아이템 삭제
        /// </summary>
        /// <param name="Conn">SQL 커넥션</param>
        /// <param name="Table">게시판 테이블 이름</param>
        /// <param name="Where">조건</param>
        /// <param name="Close">커넥션 닫기 여부</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(DBManager Conn, string Table, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                var where = ColumnWhere.JoinForStatement(Where, Conn);
                string where_query = where?.QueryString();
                StringBuilder sb = new StringBuilder("DELETE FROM ").Append(Table);

                //추가 조건절
                if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

                using (var Stmt = Conn.Prepare(sb.ToString()))
                {
                    //추가 조건절이 존재하면 할당
                    if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);
                    return await Stmt.ExcuteNonQueryAsync() > 0;
                }
            }
            finally { if (Close) Conn.Dispose(); }
        }
        #endregion

        #region Max
        public static async Task<object> MaxAsync(DBManager Conn, string Table, string Column, IEnumerable<ColumnWhere> Where = null, bool Close = false)
        {
            try
            {
                var where = ColumnWhere.JoinForStatement(Where, Conn);
                string where_query = where?.QueryString();
                StringBuilder sb = new StringBuilder($"SELECT MAX({Column}) FROM ").Append(Table);

                //추가 조건절
                if (!string.IsNullOrEmpty(where_query)) sb.Append(" WHERE ").Append(where_query);

                using (var Stmt = Conn.Prepare(sb.ToString()))
                {
                    //추가 조건절이 존재하면 할당
                    if (!string.IsNullOrEmpty(where_query)) where.Apply(Stmt);

                    var result = await Stmt.ExcuteOnceAsync();
                    return result is DBNull ? null : result;
                }
            }
            finally { if (Close) Conn.Dispose(); }
        }
        #endregion
    }
}
