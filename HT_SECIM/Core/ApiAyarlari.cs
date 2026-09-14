using System;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Exe'nin yanindaki "api" dosyasi.
    ///
    /// scenes / commands / iplist ile ayni mantik: adres ya da anahtar
    /// degisince yeniden derlemek gerekmiyor, dosya duzenlenip uygulama
    /// yeniden baslatiliyor.
    ///
    /// KAYNAK = JSON yazmak acil cikis kapisi: API yayin gecesi tuhaflik
    /// yaparsa uygulama tamamen veri.json'a doner.
    /// </summary>
    public static class ApiAyarlari
    {
        /// <summary> API mi kullanilacak yoksa yerel veri.json mu. </summary>
        public static bool ApiKullan { private set; get; }

        /// <summary> "http://localhost:5188" - sonunda / olmadan. </summary>
        public static string Adres { private set; get; }

        /// <summary> X-API-KEY basliginda gidecek okuma anahtari. </summary>
        public static string Anahtar { private set; get; }

        /// <summary> Surum sorgulama araligi, saniye. </summary>
        public static int Aralik { private set; get; }

        /// <summary> Tek bir istegin en fazla bekleyecegi sure, saniye. </summary>
        public static int Zamanasimi { private set; get; }

        public static void Load()
        {
            // Dosya yoksa uygulama API'siz calismaya devam etsin.
            ApiKullan  = false;
            Adres      = "";
            Anahtar    = "";
            Aralik     = 25;
            Zamanasimi = 15;

            foreach (string[] p in ConfigReader.ReadLines(ConfigPaths.ApiFile))
            {
                if (p.Length < 2) continue;

                string key = p[0].Trim().ToUpperInvariant();
                string val = p[1].Trim();

                switch (key)
                {
                    case "KAYNAK":
                        ApiKullan = val.Equals("API", StringComparison.OrdinalIgnoreCase);
                        break;

                    case "ADRES":
                        Adres = val.TrimEnd('/');
                        break;

                    case "ANAHTAR":
                        Anahtar = val;
                        break;

                    case "ARALIK":
                        Aralik = SayiOku(val, Aralik, 5, 600);
                        break;

                    case "ZAMANASIMI":
                        Zamanasimi = SayiOku(val, Zamanasimi, 3, 120);
                        break;
                }
            }

            if (ApiKullan && Adres.Length == 0)
            {
                CLog.Error("API ADRESI YOK", "api dosyasinda ADRES bos; veri.json'a donuluyor.");
                ApiKullan = false;
            }

            CLog.Log("API AYARLARI",
                (ApiKullan ? "API: " + Adres + " / " + Aralik + " sn" : "yerel veri.json"));
        }

        public static string SurumAdresi { get { return Adres + "/api/v1/surum"; } }
        public static string VeriAdresi  { get { return Adres + "/api/v1/veri"; } }

        private static int SayiOku(string metin, int varsayilan, int enAz, int enCok)
        {
            int deger;
            if (!int.TryParse(metin, out deger)) return varsayilan;

            if (deger < enAz) return enAz;
            if (deger > enCok) return enCok;

            return deger;
        }
    }
}
