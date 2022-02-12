using HS.DB.Command;
using HS.DB.Param;

namespace HS.DB.Utils
{
    public static class DBExtension
    {
        public static DBCommand Add(this DBCommand Command, string Name, object Value)
        {
            if (Command is DBCommandMySQL) Command.Add(new DBParamMySQL(Name, Value));
            else if (Command is DBCommandMSSQL) Command.Add(new DBParamMSSQL(Name, Value));
            else if (Command is DBCommandOracle) Command.Add(new DBParamOracle(Name, Value));

            return Command;
        }
    }
}
