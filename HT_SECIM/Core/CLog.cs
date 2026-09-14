using System;
using System.IO;
using System.Text;

namespace HT_SECIM.Core
{
    public delegate void OnLogWrittenEvent(string line);

    /// <summary>
    /// Uygulama dizinindeki LOG klasorune gunluk dosyalar halinde yazar.
    ///   LOG\actions_2026_09_01.log   -> operatorun yaptigi islemler, gonderilen komutlar
    ///   LOG\error_2026_09_01.log     -> hatalar
    ///   LOG\debug_2026_09_01.log     -> gelistirme loglari (DebugMode acikken)
    /// </summary>
    public static class CLog
    {
        public const string SEPERATOR = "}{";

        private static readonly object fileLock = new object();

        /// <summary> Gelistirme loglari yazilsin mi </summary>
        public static bool DebugMode { set; get; }

        /// <summary> Ayrintili log (engine'den donen her cevap dahil) </summary>
        public static bool DetailLog { set; get; }

        /// <summary> Ekrandaki log konsolu buna abone olur. </summary>
        public static event OnLogWrittenEvent OnLogWritten;

        public static string LogFolder
        {
            get { return Path.Combine(ConfigPaths.RootFolder, "LOG"); }
        }

        #region Yazma

        /// <summary>
        /// Acik kalan dosya akislari. Onceden her satirda dosya acilip kapaniyordu;
        /// tek pakette 200 komut gonderdigimizde bu 300 ms'yi buluyor ve komutlar
        /// gonderilmeden once yasandigi icin goruntuyu geciktiriyordu.
        /// Engine ayni 200 komutu 52 ms'de isliyor - yani darbogaz loglamaydi.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, Yazici> yazicilar =
            new System.Collections.Generic.Dictionary<string, Yazici>();

        private class Yazici
        {
            public string Gun;
            public StreamWriter Akis;
        }

        private static void WriteFile(string prefix, string logText)
        {
            string line = "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "]" + logText;

            try
            {
                lock (fileLock)
                {
                    StreamWriter akis = Akisi(prefix);
                    if (akis != null) akis.WriteLine(line);
                }
            }
            catch
            {
                // Log yazilamamasi uygulamayi durdurmamali (yayin sirasinda kritik).
            }

            if (OnLogWritten != null) OnLogWritten(line);
        }

        /// <summary>
        /// Bu ek icin acik akisi verir; gun degistiyse yenisini acar.
        /// AutoFlush acik: satir aninda diske gidiyor, cokme aninda son satirlar kaybolmuyor.
        /// </summary>
        private static StreamWriter Akisi(string prefix)
        {
            string gun = DateTime.Now.ToString("yyyy_MM_dd");

            Yazici yazici;

            if (yazicilar.TryGetValue(prefix, out yazici))
            {
                if (yazici.Gun == gun) return yazici.Akis;

                // Gece yarisi gecildi, dosyayi degistir.
                try { yazici.Akis.Dispose(); }
                catch { }

                yazicilar.Remove(prefix);
            }

            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);

            string file = Path.Combine(LogFolder, prefix + "_" + gun + ".log");

            StreamWriter akis = new StreamWriter(file, true, new UTF8Encoding(true));
            akis.AutoFlush = true;

            yazici = new Yazici();
            yazici.Gun = gun;
            yazici.Akis = akis;

            yazicilar[prefix] = yazici;

            return akis;
        }

        /// <summary> Uygulama kapanirken acik log dosyalarini birakir. </summary>
        public static void Kapat()
        {
            lock (fileLock)
            {
                foreach (Yazici yazici in yazicilar.Values)
                {
                    try { yazici.Akis.Dispose(); }
                    catch { }
                }

                yazicilar.Clear();
            }
        }

        #endregion

        #region Genel loglar

        public static void Log(string description)
        {
            Log(description, string.Empty, string.Empty);
        }

        public static void Log(string description, string data)
        {
            Log(description, data, string.Empty);
        }

        public static void Log(string description, string data, string engine)
        {
            if (data == null) data = "data null! Set edilmemis";

            string dataPart = data.Length == 0 ? "" : "{" + data + "}";
            string enginePart = engine == null || engine.Length == 0 ? "" : "-->[" + engine + "]";

            WriteFile("actions", "{" + description + "}" + dataPart + enginePart);
        }

        #endregion

        #region Ozel loglar

        /// <summary> Viz Engine'e gonderilen komut. "Reji neye basti" takibi bunun uzerinden. </summary>
        public static void Command(string engine, string command)
        {
            WriteFile("actions", "{KOMUT}{" + command + "}-->[" + engine + "]");
        }

        /// <summary> Engine'den donen cevap. Sadece DetailLog acikken yazilir. </summary>
        public static void Response(string engine, string msg)
        {
            if (!DetailLog) return;
            WriteFile("actions", "{CEVAP}{" + msg + "}<--[" + engine + "]");
        }

        public static void Error(string description, params string[] prmList)
        {
            string data = "";
            if (prmList != null)
                foreach (string st in prmList) data += "[" + st + "]";

            Error(description, data);
        }

        public static void Error(string description, string data = "")
        {
            WriteFile("error", "{" + description + "}{" + data + "}");
        }

        public static void Error(string description, Exception ex)
        {
            Error(description, ex == null ? "" : ex.ToString());
        }

        public static void Debug(string description, string data = "")
        {
            if (!DebugMode) return;
            WriteFile("debug", "{" + description + "}{" + data + "}");
        }

        /// <summary> Ayrintili islem logu (DetailLog acikken). </summary>
        public static void Detail(string description, string data = "")
        {
            if (!DetailLog) return;
            WriteFile("actions", "{" + description + "}{" + data + "}");
        }

        #endregion
    }
}