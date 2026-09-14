using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 10 - MV_HARITA_ARTAN_AZALAN
    ///
    /// Sahne 7'nin parti hali: bir partinin IKI MILLETVEKILI SECIMI arasindaki
    /// degisimi, 81 il haritada uc renkte.
    ///
    /// Renkler sahne 7'dekinden farkli calisiyor: orada sabitti, burada SECILEN
    /// PARTININ RENGINDEN turetiliyor - parti degisince harita da onun rengine
    /// donuyor. Carpanlar commands dosyasindan ayarlanabiliyor.
    ///
    /// Sahnede isim cakismasi var: sayilar ve etiketleri ayni adi tasiyor
    /// ("artan_il_oran" dort yerde geciyor). Sayi container'larina ust
    /// container'lari (1 / 2 / 3) uzerinden yol ile ulasiliyor.
    /// </summary>
    public partial class Page10_MvHaritaArtanAzalan : UserControl, IScenePage
    {
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Efsanedeki renk kutucuklari: artan / azalan / sabit. </summary>
        private const string RENK_ARTAN_KUTU  = "renk1";
        private const string RENK_AZALAN_KUTU = "renk2";
        private const string RENK_SABIT_KUTU  = "renk3";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page10_MvHaritaArtanAzalan()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            cmb_secim_yeni.Items.Clear();
            cmb_secim_eski.Items.Clear();

            foreach (Secim secim in DataService.Veri.Secimler)
            {
                if (!secim.IsMV) continue;

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

            PartileriYukle();

            cmb_secim_yeni.SelectedIndexChanged += Secim_Degisti;
            cmb_secim_eski.SelectedIndexChanged += Secim_Degisti;
            cmb_parti.SelectedIndexChanged      += Parti_Degisti;
            cmb_ondalik.SelectedIndexChanged    += Ondalik_Degisti;
            btn_doldur.Click                    += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary>
        /// Parti listesi: karsilastirma anlamli olsun diye HER IKI secimde de
        /// oyu bulunan partiler listeleniyor. (YSP 2023'te var, 2018'de yok;
        /// listelemek "oyu sifira dustu" gibi yanlis bir harita uretirdi.)
        /// </summary>
        private void PartileriYukle()
        {
            string oncekiKod = "";
            Parti onceki = cmb_parti.SelectedItem as Parti;
            if (onceki != null) oncekiKod = onceki.Kod;

            cmb_parti.Items.Clear();

            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;

            if (yeni == null || eski == null) return;

            foreach (Parti parti in DataService.Veri.Partiler)
            {
                // "DIGER" gercek bir parti degil, artakalan oylarin toplami;
                // "hangi partinin degisimi" sorusunun cevabi olamaz.
                if (parti.Toplu) continue;

                if (DataService.OyBul(yeni.Kod, 0, parti.Kod) == null) continue;
                if (DataService.OyBul(eski.Kod, 0, parti.Kod) == null) continue;

                cmb_parti.Items.Add(parti);
            }

            for (int i = 0; i < cmb_parti.Items.Count; i++)
            {
                Parti p = cmb_parti.Items[i] as Parti;
                if (p != null && p.Kod == oncekiKod) { cmb_parti.SelectedIndex = i; return; }
            }

            if (cmb_parti.Items.Count > 0) cmb_parti.SelectedIndex = 0;
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

            // --- parti karti ---
            Parti parti = SeciliParti;

            komutlar.Add(VizYazim.Metin(layer, "sira1_parti_isim", parti == null ? "" : parti.Ad));
            komutlar.Add(VizYazim.Metin(layer, "sira1_oran",
                VizYazim.Bicimle(DataService.YuzdeParse(txt_parti_oran.Text), basamak)));

            if (parti != null)
            {
                string logo = VizYazim.Gorsel(layer, "sira1_parti_logo", parti.VizLogoImage);
                if (logo != null) komutlar.Add(logo);
                else CLog.Error("PARTI LOGOSU YOK", parti.Kod + " / " + parti.Ad);
            }

            // --- sayilar ---
            // Sayi container'i ile etiketi ayni adi tasidigi icin ust container
            // (1 / 2 / 3) uzerinden yol ile adresleniyor. sabit_il_oran tek,
            // onda ada gore cagirmak yeterli.
            komutlar.Add(layer + "*TREE*" + VizYazim.Yol("1", "artan_il_oran") +
                         "*GEOM*TEXT SET " + txt_artan.Text);

            komutlar.Add(layer + "*TREE*" + VizYazim.Yol("2", "azalan_il_oran") +
                         "*GEOM*TEXT SET " + txt_azalan.Text);

            komutlar.Add(VizYazim.Metin(layer, "sabit_il_oran", txt_sabit.Text));

            // --- efsane + 81 il, hepsi ayni renk kaynagindan ---
            RenkYaz(komutlar, layer, RENK_ARTAN_KUTU,  Durum.Artan);
            RenkYaz(komutlar, layer, RENK_AZALAN_KUTU, Durum.Azalan);
            RenkYaz(komutlar, layer, RENK_SABIT_KUTU,  Durum.Sabit);

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

        /// <summary>
        /// Artan ve azalan renkleri partinin kendi renginden turetiliyor;
        /// sabit renk partiden bagimsiz.
        /// </summary>
        private string DurumRengi(Durum durum)
        {
            if (durum == Durum.Sabit)
            {
                string sabit = CommandRepository.Sablon("PARTI_SABIT");
                return string.IsNullOrEmpty(sabit) ? "255;255;255" : sabit;
            }

            Parti parti = SeciliParti;
            if (parti == null) return "255;255;255";

            double carpan = (durum == Durum.Artan)
                ? CommandRepository.Ayar("PARTI_ARTAN_CARPAN", 1.0)
                : CommandRepository.Ayar("PARTI_AZALAN_CARPAN", 1.3);

            return VizYazim.RenkTonu(parti.Renk, carpan);
        }

        /// <summary> Partinin bu ildeki oy oraninin iki secim arasindaki farki (x100). </summary>
        private int IlFarki(int plaka)
        {
            Parti parti = SeciliParti;
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;

            if (parti == null || yeni == null || eski == null) return 0;

            Oy oyYeni = DataService.OyBul(yeni.Kod, plaka, parti.Kod);
            Oy oyEski = DataService.OyBul(eski.Kod, plaka, parti.Kod);

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
        private Parti SeciliParti { get { return cmb_parti.SelectedItem as Parti; } }

        private void VeridenDoldur()
        {
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;
            Parti parti = SeciliParti;

            if (yeni == null || eski == null || parti == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_baslik_ust.Text = yeni.Ad;
            txt_baslik_alt.Text = "TÜRKİYE GENELİ";

            IlSonucu geneli = DataService.SonucBul(yeni.Kod, 0);
            txt_ass.Text = (geneli == null) ? "" : VizYazim.Bicimle(geneli.AcilanSandik, basamak);

            txt_parti_oran.Text = VizYazim.Bicimle(DataService.Oran(yeni.Kod, 0, parti.Kod), basamak);

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
            Parti parti = SeciliParti;

            int oranYeni = DataService.Oran(yeni.Kod, plaka, parti.Kod);
            int oranEski = DataService.Oran(eski.Kod, plaka, parti.Kod);

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
            txt_parti_oran.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_parti_oran.Text), basamak);

            IlleriHesapla(basamak);

            yukleniyor = false;
        }

        #endregion

        #region Olaylar

        private void Secim_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;

            yukleniyor = true;
            PartileriYukle();      // secim degisti, ortak parti listesi de degisebilir
            yukleniyor = false;

            VeridenDoldur();
        }

        private void Parti_Degisti(object sender, EventArgs e)
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
