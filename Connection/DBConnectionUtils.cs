using System;
using System.Collections.Generic;

namespace HS.DB.Connection
{
    public static class DBConnectionUtils
    {
        public static Dictionary<string, string> CreateParamDictionary()
        {
            return new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public static void AddParam(Dictionary<string, string> param, string key, object value)
        {
            if (param == null || string.IsNullOrWhiteSpace(key)) return;
            var text = Convert.ToString(value);
            if (text == null) return;
            param[key] = text;
        }

        public static void AddParam(Dictionary<string, string> param, string key, string value)
        {
            AddParam(param, key, (object)value);
        }

        public static bool IsKey(string key, params string[] keys)
        {
            if (string.IsNullOrWhiteSpace(key) || keys == null) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (string.Equals(key, keys[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        public static string ExtractHost(string dataSource)
        {
            if (string.IsNullOrWhiteSpace(dataSource)) return dataSource;
            var parts = dataSource.Split(',');
            return parts.Length > 0 ? parts[0] : dataSource;
        }

        public static int ExtractPort(string dataSource, int defaultPort)
        {
            if (string.IsNullOrWhiteSpace(dataSource)) return defaultPort;
            var parts = dataSource.Split(',');
            if (parts.Length < 2) return defaultPort;
            return int.TryParse(parts[1], out var port) && port > 0 ? port : defaultPort;
        }

        public static void ParseOracleDataSource(string dataSource, int defaultPort, out string host, out int port, out string serviceName)
        {
            host = dataSource;
            port = defaultPort;
            serviceName = string.Empty;

            if (string.IsNullOrWhiteSpace(dataSource)) return;
            if (dataSource.TrimStart().StartsWith("(")) return;

            var parts = dataSource.Split('/');
            var hostPort = parts[0];
            if (parts.Length > 1) serviceName = parts[1];

            var hostParts = hostPort.Split(':');
            host = hostParts[0];
            if (hostParts.Length > 1 && int.TryParse(hostParts[1], out var parsed) && parsed > 0)
            {
                port = parsed;
            }
        }
    }
}
