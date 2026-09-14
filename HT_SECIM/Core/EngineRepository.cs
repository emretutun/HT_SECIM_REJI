using System;
using System.Collections.Generic;

namespace HT_SECIM.Core
{
    public class EngineInfo
    {
        public string Name { set; get; }
        public string IP { set; get; }
        public int Port { set; get; }

        public override string ToString()
        {
            return Name + "  (" + IP + ":" + Port + ")";
        }
    }

    public static class EngineRepository
    {
        public const int VIZ_DEFAULT_PORT = 6100;

        public static List<EngineInfo> Engines = new List<EngineInfo>();

        public static void Load()
        {
            Engines.Clear();

            List<string[]> lines = ConfigReader.ReadLines(ConfigPaths.IpListFile);

            foreach (string[] p in lines)
            {
                // ENGINE = ad = ip = port
                if (p[0].ToUpperInvariant() != "ENGINE") continue;
                if (p.Length < 3) continue;

                EngineInfo e = new EngineInfo();
                e.Name = p[1];
                e.IP = p[2];
                e.Port = p.Length > 3 ? SafeInt(p[3], VIZ_DEFAULT_PORT) : VIZ_DEFAULT_PORT;
                Engines.Add(e);
            }
        }

        private static int SafeInt(string value, int defaultValue)
        {
            int ret;
            if (int.TryParse(value, out ret) && ret > 0) return ret;
            return defaultValue;
        }
    }
}