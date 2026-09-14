using System;
using System.Collections.Generic;
using System.IO;

namespace HT_SECIM.Core
{
    public class SceneInfo
    {
        public int No { set; get; }
        public string Name { set; get; }      // ekranda gorunen ad
        public string SubPath { set; get; }   // basepath'e gore alt yol
        public string FullPath { set; get; }  // /HT_SECIM/IC_EKRANLAR/.../SAHNE
        public string Thumbnail { set; get; } // dosya adi (thumbs klasorunde)

        /// <summary>
        /// Kart listesindeki baslik. scenes dosyasindaki "GRUP = ..." satirindan
        /// gelir ve kendisinden sonraki butun sahnelere uygulanir.
        /// </summary>
        public string Grup { set; get; }

        /// <summary> Sahneyi acan director. scenes dosyasinda yazmazsa IN kabul edilir. </summary>
        public string InDirector { set; get; }

        /// <summary> Sahneyi kapatan director. scenes dosyasinda yazmazsa OUT kabul edilir. </summary>
        public string OutDirector { set; get; }

        /// <summary>
        /// Sahnenin kok container'i. HAZIRLA bunun gozunu kapatir, VER acar.
        /// scenes dosyasinda yazmazsa Object kabul edilir.
        /// </summary>
        public string RootContainer { set; get; }

        /// <summary> Sahnenin sadece son parcasi (siyah kutuda gosterilecek) </summary>
        public string SceneName
        {
            get
            {
                if (string.IsNullOrEmpty(FullPath)) return "";
                int i = FullPath.LastIndexOf('/');
                return i < 0 ? FullPath : FullPath.Substring(i + 1);
            }
        }

        /// <summary> thumbs klasorundeki resmin tam yolu </summary>
        public string ThumbnailPath
        {
            get
            {
                if (string.IsNullOrEmpty(Thumbnail)) return "";
                return Path.Combine(ConfigPaths.ThumbsFolder, Thumbnail);
            }
        }

        public override string ToString()
        {
            return No.ToString() + " - " + Name;
        }
    }

    public static class SceneRepository
    {
        public static List<SceneInfo> Scenes = new List<SceneInfo>();

        public static void Load()
        {
            Scenes.Clear();

            string basePath = "";
            string grup = "";
            List<string[]> lines = ConfigReader.ReadLines(ConfigPaths.ScenesFile);

            foreach (string[] p in lines)
            {
                string key = p[0].ToUpperInvariant();

                // basepath = /HT_SECIM/IC_EKRANLAR/
                if (key == "BASEPATH")
                {
                    basePath = p.Length > 1 ? p[1] : "";
                    if (basePath.Length > 0 && !basePath.EndsWith("/"))
                        basePath += "/";
                    continue;
                }

                // GRUP = BAR GRAFIK   -> bundan sonraki sahnelerin listedeki basligi
                if (key == "GRUP")
                {
                    grup = p.Length > 1 ? p[1] : "";
                    continue;
                }

                // SAHNE = no = ad = altyol = thumbnail
                if (key == "SAHNE" && p.Length >= 4)
                {
                    SceneInfo s = new SceneInfo();
                    s.No = SafeInt(p[1]);
                    s.Name = p[2];
                    s.SubPath = p[3];
                    s.Thumbnail = p.Length > 4 ? p[4] : "";
                    s.FullPath = basePath + s.SubPath.TrimStart('/');
                    s.Grup = grup;

                    // Opsiyonel sutunlar: yazilmazsa IN / OUT varsayilir.
                    s.InDirector    = (p.Length > 5 && p[5].Length > 0) ? p[5] : "IN";
                    s.OutDirector   = (p.Length > 6 && p[6].Length > 0) ? p[6] : "OUT";
                    s.RootContainer = (p.Length > 7 && p[7].Length > 0) ? p[7] : "Object";
                    Scenes.Add(s);
                }
            }

            Scenes.Sort(delegate (SceneInfo a, SceneInfo b) { return a.No.CompareTo(b.No); });
        }

        public static SceneInfo GetByNo(int no)
        {
            foreach (SceneInfo s in Scenes)
                if (s.No == no) return s;
            return null;
        }

        private static int SafeInt(string value)
        {
            int ret;
            int.TryParse(value, out ret);
            return ret;
        }
    }
}