using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HT_SECIM.Core
{
    /// <summary>
    /// "TREE GET" ciktisini cozup container'lari yoluyla bulunabilir hale getirir.
    ///
    /// Ornek satirlar:
    ///   {2/3/1 146449 39}          -> ana haritadaki 39 nolu il
    ///   {2/4/1 147787 39 0}        -> vurgu katmanindaki 39 nolu il
    ///   {2/4/1/1 147800 39}        -> onun icindeki gorsel
    ///
    /// Neden lazim: Viz'de container'a "$ad" ile ulasilir ama ayni ad birden
    /// fazla yerde geciyorsa agacta once geleni verir. Sehir haritasi sahnesinde
    /// il adlari hem ana haritada hem vurgu katmaninda ayni; isimle cagirinca
    /// hep ana harita geliyor. Vurgu katmanina ulasmak icin sayisal yol
    /// ("2/4/1") kullanmak gerekiyor, bu sinif da onu buluyor.
    ///
    /// Yollar sahne yeniden yuklenince degisebilir, bu yuzden her HAZIRLA'da
    /// yeniden okunur.
    /// </summary>
    public class SceneTreeIndex
    {
        private static readonly Regex SATIR = new Regex(
            @"\{\s*([\d/]+)\s+(\d+)\s+([^}]*?)\s*\}", RegexOptions.Compiled);

        /// <summary> Isimden sonra gelen acik/kapali bayragi (0 / 1). </summary>
        private static readonly Regex SON_BAYRAK = new Regex(@"\s+[01]$", RegexOptions.Compiled);

        /// <summary> yol -> container adi </summary>
        private readonly Dictionary<string, string> adlar = new Dictionary<string, string>();

        /// <summary> Yol sirasi, ust-alt iliskisini kurarken lazim. </summary>
        private readonly List<string> yollar = new List<string>();

        public int Sayi { get { return adlar.Count; } }

        public void Coz(string treeCiktisi)
        {
            adlar.Clear();
            yollar.Clear();

            if (string.IsNullOrEmpty(treeCiktisi)) return;

            foreach (Match m in SATIR.Matches(treeCiktisi))
            {
                string yol = m.Groups[1].Value;
                string ad = m.Groups[3].Value;

                // "39 0" gibi satirlarda sondaki bayragi at. Container'in adi
                // gercekten "1" olabiliyor ("1 1" -> "1"), o yuzden sadece
                // birden fazla parca varsa kirpiyoruz.
                if (ad.IndexOf(' ') >= 0) ad = SON_BAYRAK.Replace(ad, "");

                if (ad.Length == 0) continue;

                adlar[yol] = ad;
                yollar.Add(yol);
            }
        }

        /// <summary>
        /// Adi verilen ust container'in altindaki container'in yolu. Bulunamazsa null.
        ///
        /// Once dogrudan cocuklara bakar; bulamazsa daha derine iner ve en ustteki
        /// eslesmeyi verir. Iki asamali olmasinin sebebi: sahne 8'de vurgu katmaninin
        /// hem cocugu hem torunu ayni adi tasiyor ("34" icinde yine "34"), orada
        /// dogrudan cocuk dogru olan. Sahne 9'da ise ayrac ("TG_NOKTA_SABIT")
        /// ASS'in torunu, orada derine inmek gerekiyor.
        /// </summary>
        public string YolBul(string ustAd, string cocukAd)
        {
            string ustYol = IlkYol(ustAd);
            if (ustYol == null) return null;

            string onek = ustYol + "/";
            string enYakin = null;
            int enAzDerinlik = int.MaxValue;

            foreach (string yol in yollar)
            {
                if (!yol.StartsWith(onek, StringComparison.Ordinal)) continue;
                if (adlar[yol] != cocukAd) continue;

                // Dogrudan cocuk: onekten sonra baska "/" yok.
                if (yol.IndexOf('/', onek.Length) < 0) return yol;

                int derinlik = Derinlik(yol);
                if (derinlik < enAzDerinlik)
                {
                    enAzDerinlik = derinlik;
                    enYakin = yol;
                }
            }

            return enYakin;
        }

        private static int Derinlik(string yol)
        {
            int sayi = 0;
            foreach (char c in yol) if (c == '/') sayi++;
            return sayi;
        }

        /// <summary> Bir adin agactaki ilk yolu. </summary>
        public string IlkYol(string ad)
        {
            foreach (string yol in yollar)
                if (adlar[yol] == ad) return yol;

            return null;
        }

        /// <summary> Bir ust container'in dogrudan cocuklarinin adlari, sirayla. </summary>
        public List<string> CocukAdlari(string ustAd)
        {
            List<string> liste = new List<string>();

            string ustYol = IlkYol(ustAd);
            if (ustYol == null) return liste;

            string onek = ustYol + "/";

            foreach (string yol in yollar)
            {
                if (!yol.StartsWith(onek, StringComparison.Ordinal)) continue;
                if (yol.IndexOf('/', onek.Length) >= 0) continue;

                liste.Add(adlar[yol]);
            }

            return liste;
        }
    }
}
