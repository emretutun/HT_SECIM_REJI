using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 7 - CB_HARITA_ARTAN_AZALAN
    ///
    /// Bir cumhurbaskani adayinin IKI SECIM ARASINDAKI degisimi.
    /// 81 il haritada uc renge boyaniyor: oyu artan / azalan / degismeyen.
    /// Solda o adayin karti (isim, oran, fotograf) ve uc sayac duruyor.
    ///
    /// Iller sahnede plaka adiyla duruyor: $01 ... $81
    /// Her ilin kendi MATERIAL'i var, MATERIAL*COLOR ile tek tek boyaniyor.
    /// Renkler ve "sabit" esigi commands dosyasindan geliyor.
    ///
    /// Keyframe'li tek alan acilan sandik sayaclari; artan/azalan/sabit
    /// sayilari duz metin.
    /// </summary>
    public partial class Page07_HaritaArtanAzalan : UserControl, IScenePage
    {
        /// <summary> Haritadaki il container'lari 01'den 81'e plaka adiyla duruyor. </summary>
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Efsanedeki renk kutucuklari: artan / azalan / sabit. </summary>
        private const string RENK_ARTAN_KUTU  = "renk1";
        private const string RENK_AZALAN_KUTU = "renk2";
        private const string RENK_SABIT_KUTU  = "renk3";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page07_HaritaArtanAzalan()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Bu sahne cumhurbaskani adayi gosteriyor; sadece CB veri setleri.
            List<Secim> cbSecimleri = new List<Secim>();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsCB) cbSecimleri.Add(secim);

            cmb_secim_yeni.Items.Clear();
            cmb_secim_eski.Items.Clear();

            foreach (Secim secim in cbSecimleri)
            {
                cmb_secim_yeni.Items.Add(secim);
                cmb_secim_eski.Items.Add(secim);
            }

            // Veride yeniler once geliyor: ilk kayit yeni secim, ikincisi eski.
            if (cmb_secim_yeni.Items.Count > 0) cmb_secim_yeni.SelectedIndex = 0;
            if (cmb_secim_eski.Items.Count > 1) cmb_secim_eski.SelectedIndex = 1;
            else if (cmb_secim_eski.Items.Count > 0) cmb_secim_eski.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            AdaylariYukle();

            cmb_secim_yeni.SelectedIndexChanged += Secim_Degisti;
            cmb_secim_eski.SelectedIndexChanged += Secim_Degisti;
            cmb_aday.SelectedIndexChanged       += Aday_Degisti;
            cmb_ondalik.SelectedIndexChanged    += Ondalik_Degisti;
            btn_doldur.Click                    += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary>
        /// Aday listesi: karsilastirma anlamli olsun diye HER IKI secimde de
        /// oyu bulunan adaylar listeleniyor. (2023'te KILICDAROGLU var ama
        /// 2018'de yok; oyle bir adayi listelemek "oyu sifira dustu" gibi
        /// yanlis bir harita uretirdi.)
        /// </summary>
        private void AdaylariYukle()
        {
            string oncekiKod = "";
            Aday onceki = cmb_aday.SelectedItem as Aday;
            if (onceki != null) oncekiKod = onceki.Kod;

            cmb_aday.Items.Clear();

            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;

            if (yeni == null || eski == null) return;

            foreach (Aday aday in DataService.Veri.Adaylar)
            {
                if (DataService.OyBul(yeni.Kod, 0, aday.Kod) == null) continue;
                if (DataService.OyBul(eski.Kod, 0, aday.Kod) == null) continue;

                cmb_aday.Items.Add(aday);
            }

            // Secim degisince operatorun sectigi aday listede kaldiysa korunsun.
            for (int i = 0; i < cmb_aday.Items.Count; i++)
            {
                Aday a = cmb_aday.Items[i] as Aday;
                if (a != null && a.Kod == oncekiKod) { cmb_aday.SelectedIndex = i; return; }
            }

            if (cmb_aday.Items.Count > 0) cmb_aday.SelectedIndex = 0;
        }

        #endregion

        #region IScenePage

        public void SayfaAcildi()
        {
            Kur();
        }

        /// <summary> Bu sayfa komut elemek icin durum tutmuyor; yapacak sey yok. </summary>
        public void Sifirla()
        {
        }

        public List<string> VeriKomutlari()
        {
            List<string> komutlar = new List<string>();

            string layer = CommandRepository.Layer;
            int basamak = Ondalik;

            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik_ust", txt_baslik_ust.Text));
            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik_alt", txt_baslik_alt.Text));

            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", "TG_NOKTA_SABIT", "ASS_PUAN2", basamak);

            // --- aday karti ---
            Aday aday = SeciliAday;

            komutlar.Add(VizYazim.Metin(layer, "sira1_aday_isim", aday == null ? "" : aday.Ad));
            komutlar.Add(VizYazim.Metin(layer, "sira1_oran",
                VizYazim.Bicimle(DataService.YuzdeParse(txt_aday_oran.Text), basamak)));

            if (aday != null)
            {
                string gorsel = VizYazim.Gorsel(layer, "sira1_aday_resim", aday.VizHaritaImage);
                if (gorsel != null) komutlar.Add(gorsel);
                else CLog.Error("ADAY HARITA GORSELI YOK", aday.Kod + " / " + aday.Ad);
            }

            // --- sayaclar ---
            komutlar.Add(VizYazim.Metin(layer, "artan_il_oran_val",  txt_artan.Text));
            komutlar.Add(VizYazim.Metin(layer, "azalan_il_oran_val", txt_azalan.Text));
            komutlar.Add(VizYazim.Metin(layer, "sabit_il_oran_val",  txt_sabit.Text));

            // --- efsane kutucuklari ---
            // Harita ile ayni kaynaktan boyaniyor, ikisi asla kaymaz.
            RenkYaz(komutlar, layer, RENK_ARTAN_KUTU,  Durum.Artan);
            RenkYaz(komutlar, layer, RENK_AZALAN_KUTU, Durum.Azalan);
            RenkYaz(komutlar, layer, RENK_SABIT_KUTU,  Durum.Sabit);

            // --- 81 il ---
            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
                RenkYaz(komutlar, layer, PlakaContainer(plaka), IlDurumu(plaka));

            return komutlar;
        }

        private static string PlakaContainer(int plaka)
        {
            return plaka.ToString("00");
        }

        private void RenkYaz(List<string> komutlar, string layer, string container, Durum durum)
        {
            string komut = VizYazim.MaterialRengi(layer, container, DurumRengi(durum));
            if (komut != null) komutlar.Add(komut);
        }

        #endregion

        #region Karsilastirma

        private enum Durum { Artan, Azalan, Sabit }

        /// <summary> Farkin mutlak degeri bu sinirin altindaysa il "degismedi" sayilir. </summary>
        private int SabitEsik
        {
            get { return (int)CommandRepository.Ayar("SABIT_ESIK", 10); }
        }

        private static string DurumRengi(Durum durum)
        {
            switch (durum)
            {
                case Durum.Artan:  return CommandRepository.Sablon("HARITA_ARTAN");
                case Durum.Azalan: return CommandRepository.Sablon("HARITA_AZALAN");
                default:           return CommandRepository.Sablon("HARITA_SABIT");
            }
        }

        /// <summary> Adayin bu ildeki oy oraninin iki secim arasindaki farki (x100). </summary>
        private int IlFarki(int plaka)
        {
            Aday aday = SeciliAday;
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;

            if (aday == null || yeni == null || eski == null) return 0;

            Oy oyYeni = DataService.OyBul(yeni.Kod, plaka, aday.Kod);
            Oy oyEski = DataService.OyBul(eski.Kod, plaka, aday.Kod);

            // Iki secimin birinde ilin kaydi yoksa fark hesaplanamaz;
            // "degismedi" sayip haritayi yanlis boyamamis oluyoruz.
            if (oyYeni == null || oyEski == null) return 0;

            return oyYeni.Oran - oyEski.Oran;
        }

        private Durum IlDurumu(int plaka)
        {
            int fark = IlFarki(plaka);

            if (Math.Abs(fark) < SabitEsik) return Durum.Sabit;

            return (fark > 0) ? Durum.Artan : Durum.Azalan;
        }

        #endregion

        #region Veriden doldurma

        private int Ondalik
        {
            get
            {
                int basamak;
                if (cmb_ondalik.SelectedItem == null ||
                    !int.TryParse(cmb_ondalik.SelectedItem.ToString(), out basamak)) return 2;

                return basamak;
            }
        }

        private Secim SeciliYeni { get { return cmb_secim_yeni.SelectedItem as Secim; } }
        private Secim SeciliEski { get { return cmb_secim_eski.SelectedItem as Secim; } }
        private Aday  SeciliAday { get { return cmb_aday.SelectedItem as Aday; } }

        private void VeridenDoldur()
        {
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;
            Aday aday = SeciliAday;

            if (yeni == null || eski == null || aday == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_baslik_ust.Text = yeni.Ad;
            txt_baslik_alt.Text = "TÜRKİYE GENELİ";

            IlSonucu geneli = DataService.SonucBul(yeni.Kod, 0);
            txt_ass.Text = (geneli == null) ? "" : VizYazim.Bicimle(geneli.AcilanSandik, basamak);

            txt_aday_oran.Text = VizYazim.Bicimle(DataService.Oran(yeni.Kod, 0, aday.Kod), basamak);

            IlleriHesapla(basamak);

            yukleniyor = false;
        }

        /// <summary> 81 ili tarar, sayaclari ve soldaki listeyi doldurur. </summary>
        private void IlleriHesapla(int basamak)
        {
            int artan = 0, azalan = 0, sabit = 0;

            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Il il = DataService.IlBul(plaka);
                if (il == null) continue;

                Durum durum = IlDurumu(plaka);
                int fark = IlFarki(plaka);

                if (durum == Durum.Artan) artan++;
                else if (durum == Durum.Azalan) azalan++;
                else sabit++;

                lst_iller.Items.Add(SatirYazisi(plaka, il, fark, durum, basamak));
            }

            lst_iller.EndUpdate();

            txt_artan.Text = artan.ToString();
            txt_azalan.Text = azalan.ToString();
            txt_sabit.Text = sabit.ToString();

            lbl_iller.Text = "İL BAZINDA DEĞİŞİM   —   ARTAN " + artan +
                             " / AZALAN " + azalan + " / SABİT " + sabit;
        }

        private string SatirYazisi(int plaka, Il il, int fark, Durum durum, int basamak)
        {
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;
            Aday aday = SeciliAday;

            int oranYeni = DataService.Oran(yeni.Kod, plaka, aday.Kod);
            int oranEski = DataService.Oran(eski.Kod, plaka, aday.Kod);

            string isaret = (durum == Durum.Artan) ? "+" : (durum == Durum.Azalan) ? "-" : "=";

            return plaka.ToString("00") + " " + il.Ad.PadRight(16) +
                   VizYazim.Bicimle(oranEski, basamak).PadLeft(6) + " > " +
                   VizYazim.Bicimle(oranYeni, basamak).PadLeft(6) + "   " +
                   isaret + VizYazim.Bicimle(Math.Abs(fark), basamak);
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_aday_oran.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_aday_oran.Text), basamak);

            IlleriHesapla(basamak);

            yukleniyor = false;
        }

        #endregion

        #region Olaylar

        private void Secim_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;

            yukleniyor = true;
            AdaylariYukle();       // secim degisti, ortak aday listesi de degisebilir
            yukleniyor = false;

            VeridenDoldur();
        }

        private void Aday_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;
            VeridenDoldur();
        }

        private void Ondalik_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;
            YenidenBicimle();
        }

        private void Doldur_Click(object sender, EventArgs e)
        {
            VeridenDoldur();
        }

        #endregion
    }
}
