using System;
using System.Collections.Generic;

namespace HT_SECIM.Core
{
    /// <summary>
    /// commands dosyasindaki Viz komut sablonlarini tutar.
    /// Komut sozdizimi degisirse kod degil, bin\Debug\commands dosyasi duzenlenir.
    /// </summary>
    public static class CommandRepository
    {
        public const string GUNCELLE = "GUNCELLE";
        public const string GIZLE   = "GIZLE";
        public const string HAZIRLA = "HAZIRLA";
        public const string VER     = "VER";
        public const string AL      = "AL";
        public const string TEMIZLE = "TEMIZLE";

        /// <summary> Sahnelerin yuklendigi katman: MAIN_LAYER / FRONT_LAYER / BACK_LAYER </summary>
        public static string Layer { private set; get; }

        private static readonly Dictionary<string, string> templates =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Load()
        {
            templates.Clear();
            Layer = "MAIN_LAYER";

            foreach (string[] p in ConfigReader.ReadLines(ConfigPaths.CommandsFile))
            {
                if (p.Length < 2) continue;

                string key = p[0].Trim();

                if (string.Equals(key, "LAYER", StringComparison.OrdinalIgnoreCase))
                {
                    if (p[1].Length > 0) Layer = p[1];
                    continue;
                }

                templates[key] = p[1];
            }

            // Dosya eksik ya da bir satiri silinmisse uygulama komutsuz kalmasin.
            Varsayilan(GUNCELLE, "{layer}*STAGE*DIRECTOR*{director} START");
            Varsayilan(GIZLE,   "{layer}*TREE*${root}*ACTIVE SET 0");
            Varsayilan(HAZIRLA, "{layer} SET_OBJECT SCENE*{scene}");
            Varsayilan(VER,     "{layer}*STAGE*DIRECTOR*{in} START");
            Varsayilan(AL,      "{layer}*STAGE*DIRECTOR*{in} CONTINUE");
            Varsayilan(TEMIZLE, "{layer} SET_OBJECT");

            CLog.Log("KOMUT SABLONLARI YUKLENDI", "layer=" + Layer + " / " + templates.Count + " sablon");
        }

        private static void Varsayilan(string key, string sablon)
        {
            if (!templates.ContainsKey(key) || string.IsNullOrEmpty(templates[key]))
                templates[key] = sablon;
        }

        /// <summary> commands dosyasindaki sayisal ayar (BAR_MIN, BAR_MAX ...). </summary>
        public static double Ayar(string key, double varsayilan)
        {
            string deger = Sablon(key);
            if (string.IsNullOrEmpty(deger)) return varsayilan;

            double sonuc;
            if (double.TryParse(deger.Replace(',', '.'),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out sonuc))
                return sonuc;

            CLog.Error("AYAR OKUNAMADI", key + " = " + deger);
            return varsayilan;
        }

        public static string Sablon(string key)
        {
            string t;
            return templates.TryGetValue(key, out t) ? t : "";
        }

        /// <summary> Sablondaki yer tutucularin yerine gercek degerleri koyar. </summary>
        public static string Build(string key, SceneInfo scene)
        {
            string sablon = Sablon(key);
            if (string.IsNullOrEmpty(sablon)) return "";

            string scenePath = (scene == null) ? "" : scene.FullPath;
            string inDir     = (scene == null || string.IsNullOrEmpty(scene.InDirector))  ? "IN"  : scene.InDirector;
            string outDir    = (scene == null || string.IsNullOrEmpty(scene.OutDirector)) ? "OUT" : scene.OutDirector;
            string root      = (scene == null || string.IsNullOrEmpty(scene.RootContainer)) ? "Object" : scene.RootContainer;

            return sablon
                .Replace("{layer}", Layer)
                .Replace("{scene}", scenePath)
                .Replace("{in}", inDir)
                .Replace("{out}", outDir)
                .Replace("{root}", root);
        }
    }
}
