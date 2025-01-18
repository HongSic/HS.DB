using System;

namespace HS.DB.Command
{
    [Obsolete]
    public class DBCommandBuilder
    {
        public DBCommandBuilder(string Select, string From, string Where = null)
        {
            this.Select = Select;
            this.From = From;
            this.Where = Where;
        }

        public string Select { get; set; }
        public string From { get; set; }
        public string Where { get; set; }

        public string GetCommand()
        {
            return Where == null ?
                string.Format("SELECT {0} FROM {1};", Select, From) :
                string.Format("SELECT {0} FROM {1} WHERE {2};", Select, From, Where);
        }

        public override string ToString(){return GetCommand(); }

        public static implicit operator string(DBCommandBuilder command) { return command.GetCommand(); }

        public static class Builder
        {
            #region CommandPreset
            public static DBCommandBuilder GetCount(string Table, string Column = "*", string Where = null) { return new DBCommandBuilder(string.Format("COUNT({0})", Column), Table, Where); }
            public static DBCommandBuilder GetTables(DBConnectionKind Kind, string DBName, bool IncludeScheme = true, SortOption Sort = SortOption.Not, string Where = null)
            {
                //https://hellogk.tistory.com/42 각 dbms별로 문자열을 합치는 코드

                string sort = null;
                if (Sort == SortOption.Descending) sort = "order by TABLES desc";
                else if (Sort == SortOption.Ascending) sort = "order by TABLES asc";

                if (Kind == DBConnectionKind.MSSQL) return new DBCommandBuilder(IncludeScheme ? "TABLE_SCHEMA+'.'+TABLE_NAME TABLES" : "TABLE_NAME TABLES", string.Format("{0}.INFORMATION_SCHEMA.TABLES {1}", DBName, sort), Where);
                else if (Kind == DBConnectionKind.Oracle) return new DBCommandBuilder(IncludeScheme ? "TABLE_SCHEMA+'.'+TABLE_NAME TABLES" : "TABLE_NAME TABLES", string.Format("{0}.INFORMATION_SCHEMA.TABLES {1}", DBName, sort), Where);
                return null;
                /*
                 데이터베이스 테이블 목록 가져오기
                    <ORACLE>

                    SELECT *

                    FROM USER_OBJECTS

                    WHERE OBJECT_TYPE =’TABLE’

                    ORDER BY OBJECT_NAME

                    <MSSQL>

                    SELECT *

                    FROM sysobjects

                    WHERE type = ‘U’

                    ORDER BY name

                    <MySQL>

                    SELECT *

                    FROM INFORMATION_SCHEMA.TABLES

                    WHERE TABLE_SCHEMA = ‘계정명
               */
            }
            #endregion
        }
    }
}
