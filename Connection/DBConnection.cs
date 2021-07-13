using System.Threading.Tasks;

namespace HS.DB
{
    public delegate void DBStatusChangedEventHandler(object sender, DBStatus Status);
    public abstract class DBConnection
    {
        public DBConnection(string Server, string ID, string PW, string DB, int Timeout)
        {
            this.Timeout = Timeout;
            this.Server = Server;
            this.ID = ID;
            this.PW = PW;
            this.DB = DB;
        }

        //DBStatus _Status;
        //public virtual DBStatus Status { get { return _Status; } protected set { _Status = value; OnDBStatusChanged(this, value); } }

        public abstract DBStatus Status { get; }

        public abstract string ConnectionString { get; }

        public virtual string Server { get; private set; }
        public virtual string ID { get; private set; }
        public virtual string PW { get; private set; }
        public virtual string DB { get; private set; }
        
        public virtual int Timeout { get; private set; }

        public virtual event DBStatusChangedEventHandler StatusChanged;

        public abstract DBManager Open();
        public abstract Task<DBManager> OpenAsync();
        internal abstract void Close();


        /*
        public abstract DBManager Open();
        public abstract Task<DBManager> OpenAsync();
        public abstract void Close();

        public void Reconnect()
        {
            Close();
            Open();
        }
        public async Task ReconnectAsync()
        {
            Close();
            await OpenAsync();
        }
        */

        protected virtual void OnDBStatusChanged(object sender, DBStatus Status)
        {
            if (StatusChanged != null) try { StatusChanged.Invoke(this, Status); } catch { }
        }
    }
}
