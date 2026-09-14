using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Uygulamanin yanindaki (bin\Debug) config dosyalarinin yollari.
    /// </summary>
    public static class ConfigPaths
    {
        public static string RootFolder
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string ScenesFile { get { return Path.Combine(RootFolder, "scenes"); } }
        public static string IpListFile { get { return Path.Combine(RootFolder, "iplist"); } }
        public static string DataFile { get { return Path.Combine(RootFolder, "veri.json"); } }
        public static string CommandsFile { get { return Path.Combine(RootFolder, "commands"); } }

        /// <summary> API adresi ve anahtari: bin\Debug\api </summary>
        public static string ApiFile { get { return Path.Combine(RootFolder, "api"); } }

        /// <summary>
        /// API'den gelen son saglam veri. Uygulama API kapaliyken acilirsa
        /// bununla baslar; yoksa veri.json'a duser.
        /// </summary>
        public static string CacheFile { get { return Path.Combine(RootFolder, "veri_cache.json"); } }

        /// <summary> Sahne onizleme resimlerinin klasoru: bin\Debug\thumbs\ </summary>
        public static string ThumbsFolder { get { return Path.Combine(RootFolder, "thumbs"); } }
    }
}