using Oracle.ManagedDataAccess.Client;

namespace HS.DB.Result
{
    public class DBResultOracle : DBResultSQL
    {
        protected DBResultOracle() { }
        public DBResultOracle(OracleCommand Command) : this(Command.ExecuteReader()) { this.Command = Command; }
        public DBResultOracle(OracleDataReader Reader) : base(Reader) { this.Reader = Reader; }

        public new OracleCommand Command { get; protected set; }
        public new OracleDataReader Reader { get; protected set; }
    }
}
