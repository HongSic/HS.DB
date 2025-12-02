using HS.DB.Command;

namespace HS.DB.Utils
{
    public static class DBExtension
    {
        public static DBCommand Add(this DBCommand Command, string Key, object Value)
        {
            Command.Add(Key, Value);
            return Command;
        }
    }
}
