using System;
using System.Collections.Generic;
using System.Text;

namespace HS.DB.Data
{
    public class DBDataGetter
    {
        //protected Dictionary<string, List<DBValue>> _Columns;
        public DBDataGetter(DBData data)
        {

            /*
            _Columns = new Dictionary<string, List<DBValue>>(count);

            Columns = new string[count];
            for (int i = 0; i < count; i++)
            { 
                Columns[i] = Reader.GetName(i);
                _Columns.Add(Columns[i], new List<DBValue>());
            }

            count = 0;
            while (Reader.Read())
            {
                for (int i = 0; i < Columns.Length; i++)
                    _Columns[Columns[i]].Add(new DBValue(Reader.GetValue(i), Reader.GetDataTypeName(i), Reader.GetFieldType(i)));
                count++;
            }
            Reader.Close();

            Count = count;
            */
        }
    }
}
