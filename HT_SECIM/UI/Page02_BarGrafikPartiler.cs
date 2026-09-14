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
    /// Sahne 2 - BAR_GRAFIK_PARTILER
    /// Milletvekili bar grafigi: iki satir baslik, acilan sandik, katilim ve 8 parti.
    /// Parti slotlari SIRALAMA'dir: 1 = en cok oy alan.
    /// Sehir ayri bir alan degil; alt baslikta "<IL> / Toplam <N> MV" olarak yaziliyor.
    /// </summary>
    public partial class Page02_BarGrafikPartiler : UserControl, IScenePage
    {
        /// <summary> Sahnede 8 parti slotu var (parti_1 ... parti_8). </summary>
        private const int PARTI_SAYISI = 8;

        private static readonly CultureInfo TR = CultureInfo.GetCultureInfo("tr-TR");

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page02_BarGrafikPartiler()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Bu sahne milletvekili grafigi; sadece MV veri setleri listelensin.
            cmb_secim.Items.Clear();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsMV) cmb_secim.Items.Add(secim);

            if (cmb_secim.Items.Count > 0) cmb_secim.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            SatirlariKur();

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary> Parti satirlarini (sira no + parti + oran) kodla uretir. </summary>
        private void SatirlariKur()
        {
            pnl_partiler.SuspendLayout();
            pnl_partiler.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            for (int i = 1; i <= PARTI_SAYISI; i++)
            {
                int y = 10 + (i - 1) * 38;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(10, y + 3);
                lblSira.Size = new Size(28, 20);

                ComboBox cmbParti = new ComboBox();
                cmbParti.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbParti.Font = normal;
                cmbParti.Location = new Point(42, y);
                cmbParti.Size = new Size(220, 22);
                cmbParti.Items.Add("— yok —");
                foreach (Parti parti in DataService.Veri.Partiler) cmbParti.Items.Add(parti);
                cmbParti.SelectedIndex = 0;

                Label lblYuzde = new Label();
                lblYuzde.Text = "%";
                lblYuzde.Font = kalin;
                lblYuzde.Location = new Point(272, y + 3);
                lblYuzde.Size = new Size(18, 20);

                TextBox txtOran = new TextBox();
                txtOran.Font = normal;
                txtOran.Location = new Point(292, y);
                txtOran.Size = new Size(100, 22);
                txtOran.TextAlign = HorizontalAlignment.Center;

                pnl_partiler.Controls.Add(lblSira);
                pnl_partiler.Controls.Add(cmbParti);
                pnl_partiler.Controls.Add(lblYuzde);
                pnl_partiler.Controls.Add(txtOran);

                satirlar.Add(new PartiSatiri(cmbParti, txtOran));
            }

            pnl_partiler.ResumeLayout();
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
                "ASS_PUAN1", "TG_NOKTA_SABIT_VAL", "ASS_PUAN2", basamak);

            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1", "TG_NOKTA_SABIT", "KATILIM_ORANI2", basamak);

            // Bar olcegi sahneye ozel; commands dosyasindan okunur.
            double barMin = SahneAyari("BAR_MIN", 0);
            double barMax = SahneAyari("BAR_MAX", 100);

            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                PartiSatiri satir = satirlar[i];

                Parti parti = satir.SeciliParti;
                int oran = DataService.YuzdeParse(satir.Oran.Text);

                komutlar.Add(VizYazim.Metin(layer, "parti" + no + "_ad", parti == null ? "" : parti.Ad));
                komutlar.Add(VizYazim.Metin(layer, "parti" + no + "_oran", "%" + Bicimle(oran, basamak)));
                VizYazim.BarKomutlari(komutlar, layer, "parti_bar" + no, oran, barMin, barMax);

                if (parti != null)
                {
                    string gorsel = VizYazim.Gorsel(layer, "BAR" + no, parti.VizBarImage);
                    if (gorsel != null) komutlar.Add(gorsel);
                }
            }

            return komutlar;
        }

        /// <summary> Once sahneye ozel ayari (BAR_MIN_2), yoksa geneli (BAR_MIN) kullanir. </summary>
        private double SahneAyari(string anahtar, double varsayilan)
        {
            double genel = CommandRepository.Ayar(anahtar, varsayilan);

            if (Scene == null) return genel;
            return CommandRepository.Ayar(anahtar + "_" + Scene.No, genel);
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

        private void VeridenDoldur()
        {
            Secim secim = SeciliSecim;
            Il il = ilSecici1.SeciliIl;

            if (secim == null || il == null) return;

            yukleniyor = true;

            txt_baslik_ust.Text = secim.Ad;

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, il.Plaka);

            if (sonuc == null)
            {
                txt_baslik_alt.Text = il.Ad;
                txt_ass.Text = "";
                txt_katilim.Text = "";

                foreach (PartiSatiri satir in satirlar)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Oran.Text = "";
                }

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / plaka " + il.Plaka);
                return;
            }

            int basamak = Ondalik;

            txt_ass.Text = Bicimle(sonuc.AcilanSandik, basamak);
            txt_katilim.Text = Bicimle(sonuc.Katilim, basamak);

            // Alt baslik: "<IL> / Toplam <N> MV" - vekil sayisi veriden toplanir.
            int toplamVekil = 0;
            foreach (Oy oy in sonuc.Oylar) toplamVekil += oy.Vekil;

            txt_baslik_alt.Text = (toplamVekil > 0)
                ? il.Ad + " / Toplam " + toplamVekil + " MV"
                : il.Ad;

            // Slotlar siralamadir: en cok oy alan 1. sirada.
            List<Oy> sirali = new List<Oy>(sonuc.Oylar);
            sirali.Sort(delegate (Oy a, Oy b) { return b.Oran.CompareTo(a.Oran); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                PartiSatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(sirali[i].Kod);
                satir.Oran.Text = Bicimle(sirali[i].Oran, basamak);
            }

            yukleniyor = false;
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

            foreach (PartiSatiri satir in satirlar)
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

        private class PartiSatiri
        {
            public readonly ComboBox Parti;
            public readonly TextBox Oran;

            public PartiSatiri(ComboBox parti, TextBox oran)
            {
                Parti = parti;
                Oran = oran;
            }

            public Parti SeciliParti
            {
                get { return Parti.SelectedItem as Parti; }
                set
                {
                    if (value == null) { Parti.SelectedIndex = 0; return; }

                    for (int i = 0; i < Parti.Items.Count; i++)
                    {
                        Parti liste = Parti.Items[i] as Parti;
                        if (liste != null && liste.Kod == value.Kod) { Parti.SelectedIndex = i; return; }
                    }

                    Parti.SelectedIndex = 0;
                }
            }
        }
    }
}
