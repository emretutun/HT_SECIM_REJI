using HT_SECIM.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace HT_SECIM.Data
{
    /// <summary>
    /// veri.json'u okur ve sahne sayfalarinin ihtiyac duydugu sorgulari saglar.
    /// API'ye gecildiginde sadece Load() metodu degisecek, disari acilan arayuz ayni kalacak.
    /// </summary>
    public static class DataService
    {
        public static SecimVerisi Veri = new SecimVerisi();

        private static readonly Dictionary<int, Il> illerByPlaka = new Dictionary<int, Il>();
        private static readonly Dictionary<string, Parti> partilerByKod = new Dictionary<string, Parti>();
        private static readonly Dictionary<string, Aday> adaylarByKod = new Dictionary<string, Aday>();
        private static readonly Dictionary<string, Secim> secimlerByKod = new Dictionary<string, Secim>();

        /// <summary> secimKod + plaka -> IlSonucu </summary>
        private static readonly Dictionary<string, IlSonucu> sonuclar = new Dictionary<string, IlSonucu>();

        public static bool Yuklendi { private set; get; }

        #region Yukleme

        public static bool Load()
        {
            return DosyadanYukle(ConfigPaths.DataFile);
        }

        /// <summary>
        /// Disaridan gelen veriyi yerine koyar ve index'leri yeniden kurar.
        ///
        /// UI THREAD'INDE CAGRILMALI. Index sozlukleri temizlenip yeniden
        /// dolduruluyor; arada bir sahne sayfasi okursa yarim tabloyla
        /// karsilasir. Cozme isi arka planda yapilip buraya hazir nesne gelir.
        /// </summary>
        public static void Uygula(SecimVerisi yeni)
        {
            if (yeni == null) return;

            Veri = yeni;
            IndexleriKur();
            Yuklendi = true;

            CLog.Log("VERI DEGISTI",
                Veri.Iller.Count + " il / " +
                Veri.Partiler.Count + " parti / " +
                Veri.Secimler.Count + " secim");
        }

        /// <summary> Verilen JSON dosyasindan yukler. </summary>
        public static bool DosyadanYukle(string path)
        {
            Yuklendi = false;

            if (!File.Exists(path))
            {
                CLog.Error("VERI DOSYASI YOK", path);
                Veri = new SecimVerisi();
                IndexleriKur();
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                SecimVerisi okunan = JsonConvert.DeserializeObject<SecimVerisi>(json);

                Veri = okunan ?? new SecimVerisi();
                IndexleriKur();

                Yuklendi = true;
                CLog.Log("VERI YUKLENDI",
                    Veri.Iller.Count + " il / " +
                    Veri.Partiler.Count + " parti / " +
                    Veri.Adaylar.Count + " aday / " +
                    Veri.Secimler.Count + " secim");

                return true;
            }
            catch (Exception ex)
            {
                CLog.Error("VERI OKUNAMADI", path + " | " + ex.Message);
                Veri = new SecimVerisi();
                IndexleriKur();
                return false;
            }
        }

        private static void IndexleriKur()
        {
            illerByPlaka.Clear();
            partilerByKod.Clear();
            adaylarByKod.Clear();
            secimlerByKod.Clear();
            sonuclar.Clear();

            foreach (Il il in Veri.Iller)
                illerByPlaka[il.Plaka] = il;

            foreach (Parti p in Veri.Partiler)
                if (!string.IsNullOrEmpty(p.Kod)) partilerByKod[p.Kod] = p;

            foreach (Aday a in Veri.Adaylar)
                if (!string.IsNullOrEmpty(a.Kod)) adaylarByKod[a.Kod] = a;

            foreach (Secim s in Veri.Secimler)
            {
                if (string.IsNullOrEmpty(s.Kod)) continue;

                secimlerByKod[s.Kod] = s;

                foreach (IlSonucu sonuc in s.Sonuclar)
                    sonuclar[SonucKey(s.Kod, sonuc.Plaka)] = sonuc;
            }
        }

        private static string SonucKey(string secimKod, int plaka)
        {
            return secimKod + "#" + plaka;
        }

        #endregion

        #region Sorgular

        public static Il IlBul(int plaka)
        {
            Il il;
            return illerByPlaka.TryGetValue(plaka, out il) ? il : null;
        }

        public static string IlAdi(int plaka)
        {
            Il il = IlBul(plaka);
            return il == null ? "" : il.Ad;
        }

        public static Parti PartiBul(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;

            Parti p;
            return partilerByKod.TryGetValue(kod, out p) ? p : null;
        }

        public static Aday AdayBul(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;

            Aday a;
            return adaylarByKod.TryGetValue(kod, out a) ? a : null;
        }

        public static Secim SecimBul(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;

            Secim s;
            return secimlerByKod.TryGetValue(kod, out s) ? s : null;
        }

        public static Grup GrupBul(string kod)
        {
            foreach (Grup g in Veri.Gruplar)
                if (g.Kod == kod) return g;

            return null;
        }

        /// <summary> Bir gruptaki illeri, JSON'daki sirayla dondurur. </summary>
        public static List<Il> GruptakiIller(string grupKod)
        {
            List<Il> liste = new List<Il>();

            Grup grup = GrupBul(grupKod);
            if (grup == null) return liste;

            foreach (int plaka in grup.Plakalar)
            {
                Il il = IlBul(plaka);
                if (il != null) liste.Add(il);
            }

            return liste;
        }

        public static IlSonucu SonucBul(string secimKod, int plaka)
        {
            IlSonucu s;
            return sonuclar.TryGetValue(SonucKey(secimKod, plaka), out s) ? s : null;
        }

        /// <summary> Bir adayin/partinin belirli ildeki oyu. Yoksa null. </summary>
        public static Oy OyBul(string secimKod, int plaka, string kod)
        {
            IlSonucu sonuc = SonucBul(secimKod, plaka);
            if (sonuc == null) return null;

            foreach (Oy oy in sonuc.Oylar)
                if (oy.Kod == kod) return oy;

            return null;
        }

        /// <summary> Oy orani (x100). Kayit yoksa 0. </summary>
        public static int Oran(string secimKod, int plaka, string kod)
        {
            Oy oy = OyBul(secimKod, plaka, kod);
            return oy == null ? 0 : oy.Oran;
        }

        /// <summary> Milletvekili sayisi. Kayit yoksa 0. </summary>
        public static int Vekil(string secimKod, int plaka, string kod)
        {
            Oy oy = OyBul(secimKod, plaka, kod);
            return oy == null ? 0 : oy.Vekil;
        }

        #endregion

        #region Ittifaklar

        /// <summary>
        /// Veride tanimli ittifak kodlari.
        /// Once "ittifaklar" bolumundeki sira, o bolum bos ise partilerden cikarilan sira.
        /// </summary>
        public static List<string> Ittifaklar()
        {
            List<string> liste = new List<string>();

            foreach (Ittifak it in Veri.Ittifaklar)
                if (!string.IsNullOrEmpty(it.Kod) && !liste.Contains(it.Kod)) liste.Add(it.Kod);

            if (liste.Count > 0) return liste;

            foreach (Parti p in Veri.Partiler)
            {
                if (string.IsNullOrEmpty(p.Ittifak)) continue;
                if (liste.Contains(p.Ittifak)) continue;

                liste.Add(p.Ittifak);
            }

            return liste;
        }

        /// <summary> Referandum secenegi kaydi (EVET / HAYIR). Tanimli degilse null. </summary>
        public static Secenek SecenekBul(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;

            foreach (Secenek s in Veri.Secenekler)
                if (s.Kod == kod) return s;

            return null;
        }

        /// <summary>
        /// Secimin ekranda gosterilecek etiketi: "2018 MILLETVEKILI SECIMI".
        /// Adin icinde yil zaten geciyorsa (basta ya da sonda) tekrarlanmasin diye
        /// once ayikliyor, sonra basa ekliyor.
        /// </summary>
        public static string SecimEtiketi(Secim secim)
        {
            if (secim == null) return "";
            if (secim.Yil <= 0) return secim.Ad;

            string ad = (secim.Ad == null) ? "" : secim.Ad;
            string yil = secim.Yil.ToString();

            List<string> parcalar = new List<string>();

            foreach (string parca in ad.Split(new char[] { ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (parca == yil) continue;
                parcalar.Add(parca);
            }

            return (yil + " " + string.Join(" ", parcalar.ToArray())).Trim();
        }

        /// <summary> Ittifak kaydi. Tanimli degilse null. </summary>
        public static Ittifak IttifakBul(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;

            foreach (Ittifak it in Veri.Ittifaklar)
                if (it.Kod == kod) return it;

            return null;
        }

        /// <summary> Bir ittifaka bagli partiler. </summary>
        public static List<Parti> IttifakPartileri(string ittifak)
        {
            List<Parti> liste = new List<Parti>();

            if (string.IsNullOrEmpty(ittifak)) return liste;

            foreach (Parti p in Veri.Partiler)
                if (p.Ittifak == ittifak) liste.Add(p);

            return liste;
        }

        /// <summary>
        /// Ekranda gosterilecek ittifak adi. Veriden gelir; kayit yoksa
        /// eski gomulu adlar, o da yoksa kodun kendisi.
        /// </summary>
        public static string IttifakAdi(string kod)
        {
            Ittifak it = IttifakBul(kod);
            if (it != null && !string.IsNullOrEmpty(it.Ad)) return it.Ad;

            switch (kod)
            {
                case "CUMHUR": return "CUMHUR İTTİFAKI";
                case "MILLET": return "MİLLET İTTİFAKI";
                case "EMEK":   return "EMEK VE ÖZGÜRLÜK İTTİFAKI";
                case "ATA":    return "ATA İTTİFAKI";
                default:       return kod;
            }
        }

        /// <summary> Ittifaka bagli partilerin milletvekili toplami (Turkiye geneli). </summary>
        public static int IttifakVekilToplami(string secimKod, int plaka, string ittifak)
        {
            IlSonucu sonuc = SonucBul(secimKod, plaka);
            if (sonuc == null || string.IsNullOrEmpty(ittifak)) return 0;

            int toplam = 0;

            foreach (Oy oy in sonuc.Oylar)
            {
                Parti parti = PartiBul(oy.Kod);
                if (parti != null && parti.Ittifak == ittifak) toplam += oy.Vekil;
            }

            return toplam;
        }

        #endregion

        #region Ittifak hesaplari

        /// <summary> Ittifaka bagli partilerin oy oranlari toplami (x100). </summary>
        public static int IttifakOrani(string secimKod, int plaka, string ittifak)
        {
            IlSonucu sonuc = SonucBul(secimKod, plaka);
            if (sonuc == null || string.IsNullOrEmpty(ittifak)) return 0;

            int toplam = 0;

            foreach (Oy oy in sonuc.Oylar)
                if (IttifakiniBul(secimKod, oy.Kod) == ittifak) toplam += oy.Oran;

            return toplam;
        }

        /// <summary> Ittifaka bagli partilerin milletvekili toplami. </summary>
        public static int IttifakVekil(string secimKod, int plaka, string ittifak)
        {
            IlSonucu sonuc = SonucBul(secimKod, plaka);
            if (sonuc == null || string.IsNullOrEmpty(ittifak)) return 0;

            int toplam = 0;

            foreach (Oy oy in sonuc.Oylar)
                if (IttifakiniBul(secimKod, oy.Kod) == ittifak) toplam += oy.Vekil;

            return toplam;
        }

        /// <summary> CB secimlerinde kod bir aday, MV secimlerinde bir partidir. </summary>
        private static string IttifakiniBul(string secimKod, string kod)
        {
            Secim secim = SecimBul(secimKod);

            if (secim != null && secim.IsCB)
            {
                Aday aday = AdayBul(kod);
                if (aday != null) return aday.Ittifak;
            }

            Parti parti = PartiBul(kod);
            return parti == null ? "" : parti.Ittifak;
        }

        #endregion

        #region Bicimlendirme

        /// <summary>
        /// "RECEP TAYYIP ERDOGAN" -> "R. T. ERDOGAN"
        /// Bazi sahneler adayi bas harfli gosteriyor. Soyadi oldugu gibi kalir,
        /// onceki adlarin bas harfi alinir. tamAd yoksa kisa ad dondurulur.
        /// </summary>
        public static string BasHarfliAd(Aday aday)
        {
            if (aday == null) return "";

            string tam = aday.TamAd;
            if (string.IsNullOrEmpty(tam)) return aday.Ad;

            string[] parcalar = tam.Trim().Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parcalar.Length < 2) return tam;

            string sonuc = "";

            for (int i = 0; i < parcalar.Length - 1; i++)
                sonuc += parcalar[i].Substring(0, 1) + ". ";

            return sonuc + parcalar[parcalar.Length - 1];
        }

        /// <summary> 5089 -> "50,89" </summary>
        public static string Yuzde(int deger)
        {
            return (deger / 100).ToString() + "," + Math.Abs(deger % 100).ToString("00");
        }

        /// <summary> "50,89" veya "50.89" -> 5089. Operatorun elle girdigi degeri okumak icin. </summary>
        public static int YuzdeParse(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return 0;

            metin = metin.Trim().Replace('.', ',');

            int ayrac = metin.IndexOf(',');
            if (ayrac < 0)
            {
                int tam;
                int.TryParse(metin, out tam);
                return tam * 100;
            }

            int tamKisim, ondalik;
            int.TryParse(metin.Substring(0, ayrac), out tamKisim);

            string ondalikMetin = (metin.Substring(ayrac + 1) + "00").Substring(0, 2);
            int.TryParse(ondalikMetin, out ondalik);

            return tamKisim * 100 + ondalik;
        }

        /// <summary> "255;170;0" -> Color. Bozuksa gri doner. </summary>
        public static Color RenkParse(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return Color.Gray;

            string[] parcalar = metin.Split(';');
            if (parcalar.Length != 3) return Color.Gray;

            int r, g, b;
            if (!int.TryParse(parcalar[0].Trim(), out r)) return Color.Gray;
            if (!int.TryParse(parcalar[1].Trim(), out g)) return Color.Gray;
            if (!int.TryParse(parcalar[2].Trim(), out b)) return Color.Gray;

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b)));
        }

        #endregion
    }
}
