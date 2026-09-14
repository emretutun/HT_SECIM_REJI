using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HT_SECIM.Core
{
    /// <summary>
    /// "STAGE GET ALL" ciktisini cozup animasyon kanallarinin id'lerini bulur.
    ///
    /// Ornek satirlar:
    ///   { 0/0/2   CONTAINER    #1345372 "KATILIM_ORANI2" 1 0 4 0 {} }
    ///   { 0/0/2/0 FUNC_PLUGIN  #1345398 "Counter"        1 0 5 0 {} }
    ///   { 0/0/2/0/0 CChannelInt #1345399 "number"       -1 0 6 1 {} }
    ///
    /// Kanal id'si bulununca keyframe'e su sekilde yazilabiliyor:
    ///   #1345399*KEY*End*VALUE SET 92
    ///
    /// Id'ler sahne motorda durdugu surece sabit; sahne yeniden yuklenirse degisebilir,
    /// bu yuzden her HAZIRLA'da yeniden okunur.
    /// </summary>
    public class StageIndex
    {
        private static readonly Regex SATIR = new Regex(
            @"([\d/]+)\s+([A-Za-z_]+)\s+#(\d+)\s+""([^""]*)""",
            RegexOptions.Compiled);

        // Isimden sonraki ilk sayi, satirin dopesheet'te acik olup olmadigini gosterir:
        //   1 = acik (altindakiler ciktida var), 0 = kapali, -1 = zaten yaprak.
        private static readonly Regex KAPALI_SATIR = new Regex(
            @"[\d/]+\s+([A-Za-z_]+)\s+#(\d+)\s+""([^""]*)""\s+0\s",
            RegexOptions.Compiled);

        public const string DIRECTOR_TIPI = "CDirectorTree";

        /// <summary> Dopesheet'te kapali duran bir stage satiri. </summary>
        public class KapaliSatir
        {
            public string Tip;
            public int Id;
            public string Ad;

            public bool DirectorMu { get { return Tip == DIRECTOR_TIPI; } }
        }

        /// <summary>
        /// "STAGE GET ALL" yalnizca acik satirlarin altini dokuyor; kapali bir
        /// CONTAINER'in altindaki Counter/number kanali ciktida hic gorunmez.
        ///
        /// DIRECTOR satirlari "#id*OPEN SET 1" ile acilabiliyor.
        /// CONTAINER satirlari acilamiyor: o id sahne agacindaki container'i gosteriyor,
        /// "#id*OPEN SET 1" onun katlanmasini degistiriyor, dopesheet satirini degil
        /// (OPEN GET 1 donuyor ama satir yine kapali kaliyor - denendi).
        /// Onlarin Artist'te elle acilip sahnenin kaydedilmis olmasi gerekiyor.
        /// </summary>
        public static List<KapaliSatir> KapaliSatirlar(string stageCiktisi)
        {
            List<KapaliSatir> liste = new List<KapaliSatir>();

            if (string.IsNullOrEmpty(stageCiktisi)) return liste;

            foreach (Match m in KAPALI_SATIR.Matches(stageCiktisi))
            {
                KapaliSatir satir = new KapaliSatir();
                satir.Tip = m.Groups[1].Value;
                satir.Id = int.Parse(m.Groups[2].Value);
                satir.Ad = m.Groups[3].Value;

                liste.Add(satir);
            }

            return liste;
        }

        /// <summary> "container|kanal" -> kanal id'si </summary>
        private readonly Dictionary<string, int> kanallar =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary> Kok director'un (IN) altindaki director adlari, sahnedeki sirayla. </summary>
        private readonly List<string> altDirectorler = new List<string>();

        public int KanalSayisi { get { return kanallar.Count; } }

        public List<string> AltDirectorler { get { return altDirectorler; } }

        public void Coz(string stageCiktisi)
        {
            kanallar.Clear();
            altDirectorler.Clear();

            if (string.IsNullOrEmpty(stageCiktisi)) return;

            // Kok director'un hemen altindakiler: yolu "0/<n>" olan CDirectorTree'ler.
            // Ayni ad birden fazla kez gecebiliyor (sahne 1'de dort tane ADAY_1 var),
            // isimle oynatildigi icin tekrarlari atiyoruz.
            foreach (Match m in Regex.Matches(stageCiktisi,
                @"0/\d+\s+CDirectorTree\s+#\d+\s+""([^""]*)"""))
            {
                string ad = m.Groups[1].Value;
                if (ad.Length == 0) continue;
                if (altDirectorler.Contains(ad)) continue;

                altDirectorler.Add(ad);
            }

            // Once tum dugumleri yola gore topla.
            Dictionary<string, Dugum> dugumler = new Dictionary<string, Dugum>();

            foreach (Match m in SATIR.Matches(stageCiktisi))
            {
                Dugum d = new Dugum();
                d.Yol = m.Groups[1].Value;
                d.Tip = m.Groups[2].Value;
                d.Id = int.Parse(m.Groups[3].Value);
                d.Ad = m.Groups[4].Value;

                dugumler[d.Yol] = d;
            }

            // Her kanal icin en yakin ust CONTAINER'i bul.
            foreach (Dugum d in dugumler.Values)
            {
                if (!d.Tip.StartsWith("CChannel", StringComparison.Ordinal)) continue;

                string container = UstContainerAdi(dugumler, d.Yol);
                if (container == null) continue;

                kanallar[Anahtar(container, d.Ad)] = d.Id;
            }
        }

        /// <summary> Container adi + kanal adi -> kanal id'si. Bulunamazsa 0. </summary>
        public int KanalId(string container, string kanal)
        {
            int id;
            return kanallar.TryGetValue(Anahtar(container, kanal), out id) ? id : 0;
        }

        public bool Var(string container, string kanal)
        {
            return KanalId(container, kanal) > 0;
        }

        /// <summary> Tesis edilen tum kanallar, log icin. </summary>
        public string Ozet()
        {
            List<string> parcalar = new List<string>();
            foreach (KeyValuePair<string, int> kv in kanallar)
                parcalar.Add(kv.Key + "=#" + kv.Value);

            return string.Join(", ", parcalar.ToArray());
        }

        private static string Anahtar(string container, string kanal)
        {
            return container + "|" + kanal;
        }

        private static string UstContainerAdi(Dictionary<string, Dugum> dugumler, string yol)
        {
            string mevcut = yol;

            while (true)
            {
                int son = mevcut.LastIndexOf('/');
                if (son < 0) return null;

                mevcut = mevcut.Substring(0, son);

                Dugum ust;
                if (!dugumler.TryGetValue(mevcut, out ust)) continue;

                if (ust.Tip == "CONTAINER") return ust.Ad;
            }
        }

        private class Dugum
        {
            public string Yol;
            public string Tip;
            public int Id;
            public string Ad;
        }
    }
}
