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

        /// <summary> Uygulama acilir acilmaz secili engine'e baglanilsin mi (AC). </summary>
        public static bool OtomatikBaglan { private set; get; }

        /// <summary> Baglanti belirli araliklarla yoklansin mi (HB). </summary>
        public static bool Nabiz { private set; get; }

        /// <summary> Nabiz yoklamasinin araligi, saniye. </summary>
        public static int NabizAralik { private set; get; }

        public static void Load()
        {
            Engines.Clear();

            OtomatikBaglan = false;
            Nabiz          = false;
            NabizAralik    = 10;

            List<string[]> lines = ConfigReader.ReadLines(ConfigPaths.IpListFile);

            foreach (string[] p in lines)
            {
                string anahtar = p[0].ToUpperInvariant();

                if (anahtar == "OTOMATIK_BAGLAN") { OtomatikBaglan = (p[1].Trim() == "1"); continue; }
                if (anahtar == "NABIZ")           { Nabiz          = (p[1].Trim() == "1"); continue; }

                if (anahtar == "NABIZ_ARALIK")
                {
                    // 3 saniyenin altina inilmiyor: daha sik yoklamanin
                    // faydasi yok, her tik motora bir sorgu demek.
                    int sn = SafeInt(p[1].Trim(), NabizAralik);
                    NabizAralik = sn < 3 ? 3 : sn;
                    continue;
                }

                // ENGINE = ad = ip = port
                if (anahtar != "ENGINE") continue;
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