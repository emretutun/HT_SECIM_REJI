using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Iki secim arasindaki farkin operator listesinde nasil gosterilecegi.
    ///
    /// Fark degerleri her yerde oran x100 tutuluyor: 833 = 8,33 puan.
    ///
    /// RENK NEDEN SABIT ESIGE GORE DEGIL DE OLCEGE GORE:
    /// Olculdu - ayni adayin 2023/2018 farki en cok 29,66 puan, ama
    /// 2023/2015 farki 42,87 puana cikiyor. Sabit bir tavan koysaydik
    /// ikinci karsilastirmada 20 puanin ustundeki butun iller ayni
    /// koyulukta gorunur, Malatya'nin -42,87'si ile -21 ayirt edilemezdi.
    /// Bu yuzden olcek her karsilastirmada yeniden kuruluyor: o listedeki
    /// EN BUYUK fark en koyu ton oluyor, gerisi ona oranlaniyor.
    ///
    /// Bunun bedeli, ayni -8 puanin bir karsilastirmada koyu digerinde
    /// soluk gorunmesi. Onu FARK sutunundaki sayi karsiliyor: renk goz
    /// icin, sayi gercek icin.
    /// </summary>
    public static class FarkRenk
    {
        /// <summary> Farkin mutlak degeri bunun altindaysa il "degismedi" sayilir. </summary>
        public static int SabitEsik
        {
            get { return (int)CommandRepository.Ayar("SABIT_ESIK", 10); }
        }

        /// <summary>
        /// Olcegin inebilecegi en kucuk deger (varsayilan 5 puan).
        ///
        /// Olmazsa, hicbir ilin kayda deger degismedigi bir karsilastirmada
        /// en buyuk fark 0,30 puan olsa bile bazi iller kapkara boyanir ve
        /// ekranda olmayan bir hareket varmis gibi gorunur.
        /// </summary>
        public static int Taban
        {
            get { return (int)CommandRepository.Ayar("FARK_TABAN", 500); }
        }

        /// <summary> Tam doygunlukta zeminin beyazdan ne kadar uzaklasacagi. </summary>
        private const double EN_KOYU = 0.72;

        /// <summary>
        /// Listedeki en buyuk mutlak fark. Renkler buna oranlanir.
        /// Taban'in altina inmez.
        /// </summary>
        public static int Olcek(IEnumerable<int> farklar)
        {
            int enBuyuk = Taban;

            if (farklar != null)
            {
                foreach (int fark in farklar)
                {
                    int mutlak = Math.Abs(fark);
                    if (mutlak > enBuyuk) enBuyuk = mutlak;
                }
            }

            return enBuyuk;
        }

        /// <summary>
        /// Satir zemini. Artan yesile, azalan kirmiziya dogru;
        /// ne kadar degismisse o kadar koyu.
        /// </summary>
        public static Color Zemin(int fark, int olcek)
        {
            if (Math.Abs(fark) < SabitEsik) return Tema.Girdi;
            if (olcek <= 0) return Tema.Girdi;

            double k = (double)Math.Abs(fark) / olcek;
            if (k > 1) k = 1;

            // Yazinin okunur kalmasi icin en koyu ton siniri.
            k *= EN_KOYU;

            return Karistir(Tema.Girdi, fark > 0 ? Tema.Ver : Tema.Al, k);
        }

        /// <summary>
        /// Kullaniciya gosterilen sayilar virgullu.
        ///
        /// Makinenin kulturune birakilmiyor: VizYazim da ayni sekilde tr-TR
        /// kullaniyor ve ORAN kutulari "37,41" yaziyor. Kulturden gelseydi
        /// ayni ekranda ORAN virgullu, FARK noktali cikardi.
        /// </summary>
        private static readonly CultureInfo TR = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary> FARK sutunu: "+8,33" / "-8,33". Karsilastirilamiyorsa "—". </summary>
        public static string Metin(int fark)
        {
            return (fark / 100.0).ToString("+0.00;-0.00;0.00", TR);
        }

        /// <summary> Iki secimden birinde kalem ya da il yoksa gosterilecek isaret. </summary>
        public const string YOK = "—";

        private static Color Karistir(Color a, Color b, double k)
        {
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * k),
                (int)Math.Round(a.G + (b.G - a.G) * k),
                (int)Math.Round(a.B + (b.B - a.B) * k));
        }
    }
}
