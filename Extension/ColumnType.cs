namespace HS.DB.Extension
{
    public enum ColumnType
    {
        ETC = -1,
        AUTO = 0, //DB 형식에 따름
        NUMBER,
        DECIMAL,
        STRING,
        /// <summary>
        /// Class 및 Struct 만 사용가능
        /// </summary>
        JSON,
        /// <summary>
        /// Class 및 Struct 만 사용가능
        /// </summary>
        XML,
        DATETIME,
        BIN,
        BOOL,
    }
}
