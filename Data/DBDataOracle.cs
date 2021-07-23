using Oracle.ManagedDataAccess.Client;

namespace HS.DB.Data
{
    public class DBDataOracle : DBDataSQL
    {
        protected DBDataOracle() { }
        public DBDataOracle(OracleCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBDataOracle(OracleDataReader Reader) : base(Reader) { this.Reader = Reader; }

        public new OracleCommand Command { get; protected set; }
        public new OracleDataReader Reader { get; protected set; }
    }
}
