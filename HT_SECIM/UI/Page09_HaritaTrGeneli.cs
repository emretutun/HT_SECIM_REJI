using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 9 - CB_HARITA_TR_GENELI
    ///
    /// Turkiye geneli cumhurbaskanligi sonucu. Ustte EN COK OY ALAN IKI ADAY,
    /// altta 81 illik harita. Harita her ili O ILDE KAZANANIN rengiyle boyaniyor -
    /// kazanan ustteki iki adaydan biri olmak zorunda degil; ucuncu bir aday bir
    /// il kazanmissa harita o ili onun renginde gosterir, panelde yine ilk iki
    /// aday durur.
    ///
    /// Renkler sahnenin kendi paleti (adaylardaki haritaRenkGenel):
    ///   ERDOGAN turuncu, KILICDAROGLU kirmizi, digerleri mavi.
    /// Sahne 8 daha koyu bir palet kullaniyor, o yuzden ayri alanda tutuluyor.
    ///
    /// Bir tuzak: ayrac container'i "TG_NOKTA_SABIT" bu sahnede hem katilimda hem
    /// acilan sandikta ayni adi tasiyor. "$ad" ile cagirinca Viz agacta once
    /// geleni veriyor ve digeri hic yazilmiyordu; ikisi de Yol(...) ile
    /// adresleniyor.
    /// </summary>
    public partial class Page09_HaritaTrGeneli : UserControl, IScenePage
    {
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Sahnede iki aday slotu var. </summary>
        private const int SLOT_SAYISI = 2;

        private readonly List<AdaySatiri> satirlar = new List<AdaySatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary> Haritanin en son hangi secimle boyandigi; bosuna 81 komut gitmesin. </summary>
        private string haritaSecimi = null;

        public SceneInfo Scene { set; get; }

        public Page09_HaritaTrGeneli()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            cmb_secim.Items.Clear();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsCB) cmb_secim.Items.Add(secim);

            if (cmb_secim.Items.Count > 0) cmb_secim.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            SatirlariKur();

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        private void SatirlariKur()
        {
            pnl_adaylar.SuspendLayout();
            pnl_adaylar.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            for (int i = 1; i <= SLOT_SAYISI; i++)
            {
                int y = 12 + (i - 1) * 44;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(10, y + 3);
                lblSira.Size = new Size(28, 20);

                ComboBox cmbAday = new ComboBox();
                cmbAday.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbAday.Font = normal;
                cmbAday.Location = new Point(42, y);
                cmbAday.Size = new Size(230, 22);
                cmbAday.Items.Add("— yok —");
                foreach (Aday aday in DataService.Veri.Adaylar) cmbAday.Items.Add(aday);
                cmbAday.SelectedIndex = 0;

                Label lblYuzde = new Label();
                lblYuzde.Text = "%";
                lblYuzde.Font = kalin;
                lblYuzde.Location = new Point(282, y + 3);
                lblYuzde.Size = new Size(18, 20);

                TextBox txtOran = new TextBox();
                txtOran.Font = normal;
                txtOran.Location = new Point(302, y);
                txtOran.Size = new Size(100, 22);
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

        /// <summary> Sahne bastan yuklendi, harita da tasarim halinde geldi. </summary>
        public void Sifirla()
        {
            haritaSecimi = null;
        }

        public List<string> VeriKomutlari()
        {
            List<string> komutlar = new List<string>();

            string layer = CommandRepository.Layer;
            int basamak = Ondalik;

            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik_ust", txt_baslik_ust.Text));
            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik_alt", txt_baslik_alt.Text));

            // Iki ayracin da adi "TG_NOKTA_SABIT"; hangisinin yazildigi belli olsun
            // diye ust container'lariyla birlikte adresleniyor.
            VizYazim.SayacliOranAdres(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", VizYazim.Yol("ASS", "TG_NOKTA_SABIT"), "ASS_PUAN2", basamak);

            VizYazim.SayacliOranAdres(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1", VizYazim.Yol("KATILIM_ORANI", "TG_NOKTA_SABIT"),
                "KATILIM_ORANI2", basamak);

            // --- iki aday ---
            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                AdaySatiri satir = satirlar[i];
                Aday aday = satir.SeciliAday;

                if (aday == null)
                {
                    komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, false));
                    continue;
                }

                komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, true));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_aday_isim",
                    DataService.BasHarfliAd(aday)));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak)));

                // Resim container'inin adi iki slotta farkli: sira1_aday_resim / sira2_resim
                string resimContainer = (no == 1) ? "sira1_aday_resim" : "sira" + no + "_resim";

                string gorsel = VizYazim.Gorsel(layer, resimContainer, aday.VizTurImage);
                if (gorsel != null) komutlar.Add(gorsel);
                else CLog.Error("ADAY TUR GORSELI YOK", aday.Kod + " / " + aday.Ad);
            }

            HaritayiBoya(komutlar, layer);

            return komutlar;
        }

        /// <summary>
        /// Her ili o ilde kazananin rengiyle boyar. Kazananlar sadece secim
        /// degisince degisir, bosuna 81 komut gonderilmiyor.
        /// </summary>
        private void HaritayiBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (haritaSecimi == secim.Kod) return;
            haritaSecimi = secim.Kod;

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Aday kazanan = Kazanan(secim.Kod, plaka);
                if (kazanan == null) continue;

                string komut = VizYazim.MaterialRengi(layer, PlakaAdi(plaka), kazanan.HaritaRenkGenel);
                if (komut != null) komutlar.Add(komut);
            }
        }

        private static string PlakaAdi(int plaka)
        {
            return plaka.ToString("00");
        }

        /// <summary> Bir ilde en cok oyu alan aday. Kayit yoksa null. </summary>
        private static Aday Kazanan(string secimKod, int plaka)
        {
            IlSonucu sonuc = DataService.SonucBul(secimKod, plaka);
            if (sonuc == null) return null;

            Oy enIyi = null;

            foreach (Oy oy in sonuc.Oylar)
                if (enIyi == null || oy.Oran > enIyi.Oran) enIyi = oy;

            return (enIyi == null) ? null : DataService.AdayBul(enIyi.Kod);
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
            if (secim == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_baslik_ust.Text = secim.Ad;
            txt_baslik_alt.Text = "TÜRKİYE GENELİ";

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, 0);

            if (sonuc == null)
            {
                txt_ass.Text = "";
                txt_katilim.Text = "";

                foreach (AdaySatiri satir in satirlar)
                {
                    satir.Aday.SelectedIndex = 0;
                    satir.Oran.Text = "";
                }

                lst_iller.Items.Clear();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / TURKIYE GENELI");
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);
            txt_katilim.Text = VizYazim.Bicimle(sonuc.Katilim, basamak);

            // Panelde en cok oy alan iki aday.
            List<Oy> sirali = new List<Oy>(sonuc.Oylar);
            sirali.Sort(delegate (Oy a, Oy b) { return b.Oran.CompareTo(a.Oran); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                AdaySatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Aday.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    continue;
                }

                satir.SeciliAday = DataService.AdayBul(sirali[i].Kod);
                satir.Oran.Text = VizYazim.Bicimle(sirali[i].Oran, basamak);
            }

            IlleriListele(secim.Kod, basamak);

            yukleniyor = false;
        }

        /// <summary> Soldaki liste: hangi ili kim kazandi, kac oyla. </summary>
        private void IlleriListele(string secimKod, int basamak)
        {
            Dictionary<string, int> sayac = new Dictionary<string, int>();

            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Il il = DataService.IlBul(plaka);
                if (il == null) continue;

                Aday kazanan = Kazanan(secimKod, plaka);
                string ad = (kazanan == null) ? "-" : kazanan.Ad;

                if (kazanan != null)
                {
                    if (sayac.ContainsKey(ad)) sayac[ad]++;
                    else sayac[ad] = 1;
                }

                int oran = (kazanan == null) ? 0 : DataService.Oran(secimKod, plaka, kazanan.Kod);

                lst_iller.Items.Add(plaka.ToString("00") + " " + il.Ad.PadRight(16) +
                                    ad.PadRight(14) + VizYazim.Bicimle(oran, basamak).PadLeft(6));
            }

            lst_iller.EndUpdate();

            List<string> ozet = new List<string>();
            foreach (KeyValuePair<string, int> kv in sayac) ozet.Add(kv.Key + " " + kv.Value);

            lbl_iller.Text = "İL BAZINDA KAZANAN   —   " + string.Join(" / ", ozet.ToArray());
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

            foreach (AdaySatiri satir in satirlar)
            {
                if (satir.Oran.Text.Length == 0) continue;
                satir.Oran.Text = VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak);
            }

            Secim secim = SeciliSecim;
            if (secim != null) IlleriListele(secim.Kod, basamak);

            yukleniyor = false;
        }

        #endregion

        #region Olaylar

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
