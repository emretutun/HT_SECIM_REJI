using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 1 - BAR_GRAFIK_MILLETVEKILI_V2
    /// Cumhurbaskanligi bar grafigi: baslik, sehir, acilan sandik, katilim ve 4 aday.
    /// Aday slotlari SIRALAMA'dir: 1 = en cok oy alan.
    /// </summary>
    public partial class Page01_BarGrafik : UserControl, IScenePage
    {
        /// <summary> Sahnede 4 aday slotu var (ADAY_1 ... ADAY_4). </summary>
        private const int ADAY_SAYISI = 4;

        private static readonly CultureInfo TR = CultureInfo.GetCultureInfo("tr-TR");

        private readonly List<AdaySatiri> satirlar = new List<AdaySatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page01_BarGrafik()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Bu sahne cumhurbaskanligi grafigi; sadece CB veri setleri listelensin.
            cmb_secim.Items.Clear();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsCB) cmb_secim.Items.Add(secim);

            if (cmb_secim.Items.Count > 0) cmb_secim.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;      // varsayilan: 2 hane

            SatirlariKur();

            ilSecici1.Yukle("ALFABE");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary> Aday satirlarini (sira no + aday + oran) kodla uretir. </summary>
        private void SatirlariKur()
        {
            pnl_adaylar.SuspendLayout();
            pnl_adaylar.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 11F, FontStyle.Bold);
            Font normal = new Font("Arial", 10F, FontStyle.Regular);

            for (int i = 1; i <= ADAY_SAYISI; i++)
            {
                int y = 12 + (i - 1) * 48;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(10, y + 3);
                lblSira.Size = new Size(28, 22);
                lblSira.TextAlign = ContentAlignment.MiddleLeft;

                ComboBox cmbAday = new ComboBox();
                cmbAday.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbAday.Font = normal;
                cmbAday.Location = new Point(42, y);
                cmbAday.Size = new Size(230, 24);
                cmbAday.Items.Add("— yok —");
                foreach (Aday aday in DataService.Veri.Adaylar) cmbAday.Items.Add(aday);
                cmbAday.SelectedIndex = 0;

                Label lblYuzde = new Label();
                lblYuzde.Text = "%";
                lblYuzde.Font = kalin;
                lblYuzde.Location = new Point(282, y + 3);
                lblYuzde.Size = new Size(18, 22);

                TextBox txtOran = new TextBox();
                txtOran.Font = normal;
                txtOran.Location = new Point(302, y);
                txtOran.Size = new Size(110, 24);
                txtOran.TextAlign = HorizontalAlignment.Center;

                pnl_adaylar.Controls.Add(lblSira);
                pnl_adaylar.Controls.Add(cmbAday);
                pnl_adaylar.Controls.Add(lblYuzde);
                pnl_adaylar.Controls.Add(txtOran);

                satirlar.Add(new AdaySatiri(cmbAday, txtOran));
            }

            pnl_adaylar.ResumeLayout();
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

            komutlar.Add(VizYazim.Metin(layer, "BASLIK_UST", txt_baslik.Text));
            komutlar.Add(VizYazim.Metin(layer, "sehir_ad", txt_sehir.Text));

            // Acilan sandik ve katilim tam / ayrac / ondalik olarak uce bolunmus,
            // tam ve ondalik parcalar Counter plugin'iyle suruluyor.
            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", "TG_NOKTA_SABIT_VAL", "ASS_PUAN2", basamak);

            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1", "TG_NOKTA_SABIT", "KATILIM_ORANI2", basamak);

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                AdaySatiri satir = satirlar[i];

                Aday aday = satir.SeciliAday;
                int oran = DataService.YuzdeParse(satir.Oran.Text);

                komutlar.Add(VizYazim.Metin(layer, "aday" + no + "_isim", aday == null ? "" : aday.Ad));
                komutlar.Add(VizYazim.Metin(layer, "aday" + no + "_oran", "%" + Bicimle(oran, basamak)));
                VizYazim.BarKomutlari(komutlar, layer, "aday_bar" + no, oran, barMin, barMax);

                // Barin rengi: adayin bar gorseli
                if (aday != null)
                {
                    string gorsel = VizYazim.Gorsel(layer, "BAR" + no, aday.VizBarImage);
                    if (gorsel != null) komutlar.Add(gorsel);
                }
            }

            return komutlar;
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

        private Secim SeciliSecim
        {
            get { return cmb_secim.SelectedItem as Secim; }
        }

        /// <summary> Secili il ve secim setinden alanlari doldurur. </summary>
        private void VeridenDoldur()
        {
            Secim secim = SeciliSecim;
            Il il = ilSecici1.SeciliIl;

            if (secim == null || il == null) return;

            yukleniyor = true;

            txt_baslik.Text = secim.Ad;
            txt_sehir.Text = il.Ad;

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, il.Plaka);

            if (sonuc == null)
            {
                txt_ass.Text = "";
                txt_katilim.Text = "";

                foreach (AdaySatiri satir in satirlar)
                {
                    satir.Aday.SelectedIndex = 0;
                    satir.Oran.Text = "";
                }

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / plaka " + il.Plaka);
                return;
            }

            int basamak = Ondalik;

            txt_ass.Text = Bicimle(sonuc.AcilanSandik, basamak);
            txt_katilim.Text = Bicimle(sonuc.Katilim, basamak);

            // Slotlar siralamadir: en cok oy alan 1. sirada.
            List<Oy> siralı = new List<Oy>(sonuc.Oylar);
            siralı.Sort(delegate (Oy a, Oy b) { return b.Oran.CompareTo(a.Oran); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                AdaySatiri satir = satirlar[i];

                if (i >= siralı.Count)
                {
                    satir.Aday.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    continue;
                }

                satir.SeciliAday = DataService.AdayBul(siralı[i].Kod);
                satir.Oran.Text = Bicimle(siralı[i].Oran, basamak);
            }

            yukleniyor = false;
        }

        /// <summary> Ondalik hane degisince ekrandaki degerleri yeniden bicimlendirir. </summary>
        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

            foreach (AdaySatiri satir in satirlar)
            {
                if (satir.Oran.Text.Length == 0) continue;
                satir.Oran.Text = Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak);
            }

            yukleniyor = false;
        }

        #endregion

        #region Olaylar

        private void IlSecici_OnIlSecildi(Il il)
        {
            if (yukleniyor) return;
            VeridenDoldur();
        }

        private void Secim_Degisti(object sender, EventArgs e)
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

        #region Yardimcilar

        private static string Bicimle(int oranX100, int basamak)
        {
            return VizYazim.Bicimle(oranX100, basamak);
        }

        #endregion

        /// <summary> Bir aday satirindaki kontrolleri bir arada tutar. </summary>
        private class AdaySatiri
        {
            public readonly ComboBox Aday;
            public readonly TextBox Oran;

            public AdaySatiri(ComboBox aday, TextBox oran)
            {
                Aday = aday;
                Oran = oran;
            }

            public Aday SeciliAday
            {
                get { return Aday.SelectedItem as Aday; }
                set
                {
                    if (value == null) { Aday.SelectedIndex = 0; return; }

                    for (int i = 0; i < Aday.Items.Count; i++)
                    {
                        Aday liste = Aday.Items[i] as Aday;
                        if (liste != null && liste.Kod == value.Kod) { Aday.SelectedIndex = i; return; }
                    }

                    Aday.SelectedIndex = 0;
                }
            }
        }
    }
}
