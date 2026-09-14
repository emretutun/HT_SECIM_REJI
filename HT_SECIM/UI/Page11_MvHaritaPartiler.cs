using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 11 - MV_HARITA_PARTILER_TR_GENELI
    ///
    /// Turkiye geneli milletvekili sonucu: en cok oy alan 7 parti ve 81 illik
    /// harita. Harita her ili O ILDE KAZANAN partinin rengiyle boyaniyor;
    /// kazanan ilk yedinin icinde olmak zorunda degil.
    ///
    /// Her parti satirinda dort sey var: ad, oy orani, vekil sayisi ve iki
    /// gorsel - yatay renk seridi (siraN_parti_renk) ile vekil sayisinin
    /// arkasindaki rozet (ak_parti_mv).
    ///
    /// Iki isim cakismasi kodda cozuluyor, sahneye dokunulmuyor:
    ///   - "TG_NOKTA_SABIT" hem katilimda hem acilan sandikta -> Yol(...)
    ///   - "ak_parti_mv" yedi slotta da ayni adda -> SIRA_N uzerinden Yol(...)
    /// </summary>
    public partial class Page11_MvHaritaPartiler : UserControl, IScenePage
    {
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Sahnede 7 parti slotu var (SIRA_1 ... SIRA_7). </summary>
        private const int SLOT_SAYISI = 7;

        /// <summary> Vekil rozetinin container adi - yedi slotta da ayni. </summary>
        private const string ROZET_CONTAINER = "ak_parti_mv";

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary> Haritanin en son hangi secimle boyandigi; bosuna 81 komut gitmesin. </summary>
        private string haritaSecimi = null;

        public SceneInfo Scene { set; get; }

        public Page11_MvHaritaPartiler()
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
                if (secim.IsMV) cmb_secim.Items.Add(secim);

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

        /// <summary> Parti satirlarini (sira + parti + oran + vekil) kodla uretir. </summary>
        private void SatirlariKur()
        {
            pnl_partiler.SuspendLayout();
            pnl_partiler.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            for (int i = 1; i <= SLOT_SAYISI; i++)
            {
                int y = 8 + (i - 1) * 28;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(8, y + 3);
                lblSira.Size = new Size(24, 18);

                ComboBox cmbParti = new ComboBox();
                cmbParti.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbParti.Font = normal;
                cmbParti.Location = new Point(34, y);
                cmbParti.Size = new Size(180, 22);
                cmbParti.Items.Add("— yok —");
                foreach (Parti parti in DataService.Veri.Partiler) cmbParti.Items.Add(parti);
                cmbParti.SelectedIndex = 0;

                Label lblYuzde = new Label();
                lblYuzde.Text = "%";
                lblYuzde.Font = kalin;
                lblYuzde.Location = new Point(222, y + 3);
                lblYuzde.Size = new Size(16, 18);

                TextBox txtOran = new TextBox();
                txtOran.Font = normal;
                txtOran.Location = new Point(240, y);
                txtOran.Size = new Size(80, 22);
                txtOran.TextAlign = HorizontalAlignment.Center;

                TextBox txtVekil = new TextBox();
                txtVekil.Font = normal;
                txtVekil.Location = new Point(330, y);
                txtVekil.Size = new Size(60, 22);
                txtVekil.TextAlign = HorizontalAlignment.Center;

                Label lblMv = new Label();
                lblMv.Text = "MV";
                lblMv.Font = kalin;
                lblMv.Location = new Point(396, y + 3);
                lblMv.Size = new Size(30, 18);

                pnl_partiler.Controls.Add(lblSira);
                pnl_partiler.Controls.Add(cmbParti);
                pnl_partiler.Controls.Add(lblYuzde);
                pnl_partiler.Controls.Add(txtOran);
                pnl_partiler.Controls.Add(txtVekil);
                pnl_partiler.Controls.Add(lblMv);

                satirlar.Add(new PartiSatiri(cmbParti, txtOran, txtVekil));
            }

            pnl_partiler.ResumeLayout();
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

            // Toplam vekil sayisi anayasal olarak sabit, commands dosyasindan.
            komutlar.Add(VizYazim.Metin(layer, "toplam_mv_sayisi", ToplamVekil.ToString()));

            // Iki ayracin da adi "TG_NOKTA_SABIT"; ust container'lariyla adresleniyor.
            VizYazim.SayacliOranAdres(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", VizYazim.Yol("ASS", "TG_NOKTA_SABIT"), "ASS_PUAN2", basamak);

            VizYazim.SayacliOranAdres(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1", VizYazim.Yol("KATILIM_ORANI", "TG_NOKTA_SABIT"),
                "KATILIM_ORANI2", basamak);

            // --- 7 parti ---
            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                PartiSatiri satir = satirlar[i];
                Parti parti = satir.SeciliParti;

                if (parti == null)
                {
                    komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, false));
                    continue;
                }

                komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, true));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_parti_isim", parti.Ad));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak)));

                komutlar.Add(VizYazim.Metin(layer, "parti" + no + "_mv", SafeInt(satir.Vekil.Text).ToString()));

                string serit = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", parti.VizHaritaSeritImage);
                if (serit != null) komutlar.Add(serit);

                // Rozet container'i yedi slotta da ayni adda; SIRA_N uzerinden yol ile.
                string rozet = VizYazim.YolGorsel(layer,
                    VizYazim.Yol("SIRA_" + no, ROZET_CONTAINER), parti.VizMvRozetImage);

                if (rozet != null) komutlar.Add(rozet);
            }

            HaritayiBoya(komutlar, layer);

            return komutlar;
        }

        /// <summary>
        /// Her ili o ilde kazanan partinin rengiyle boyar.
        /// Kazananlar sadece secim degisince degisir, bosuna 81 komut gitmiyor.
        /// </summary>
        private void HaritayiBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (haritaSecimi == secim.Kod) return;
            haritaSecimi = secim.Kod;

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Parti kazanan = Kazanan(secim.Kod, plaka);
                if (kazanan == null) continue;

                // Bu sahne TR geneli ailesinden: canli palet.
                string komut = VizYazim.MaterialRengi(layer, PlakaAdi(plaka), kazanan.HaritaRenkGenel);
                if (komut != null) komutlar.Add(komut);
            }
        }

        private static string PlakaAdi(int plaka)
        {
            return plaka.ToString("00");
        }

        /// <summary> Bir ilde en cok oyu alan parti. Kayit yoksa null. </summary>
        private static Parti Kazanan(string secimKod, int plaka)
        {
            IlSonucu sonuc = DataService.SonucBul(secimKod, plaka);
            if (sonuc == null) return null;

            Oy enIyi = null;

            foreach (Oy oy in sonuc.Oylar)
                if (enIyi == null || oy.Oran > enIyi.Oran) enIyi = oy;

            return (enIyi == null) ? null : DataService.PartiBul(enIyi.Kod);
        }

        #endregion

        #region Veriden doldurma

        private int ToplamVekil
        {
            get { return (int)CommandRepository.Ayar("TOPLAM_MV", 600); }
        }

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

                foreach (PartiSatiri satir in satirlar)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    satir.Vekil.Text = "";
                }

                lst_iller.Items.Clear();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / TURKIYE GENELI");
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);
            txt_katilim.Text = VizYazim.Bicimle(sonuc.Katilim, basamak);

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
                    satir.Vekil.Text = "";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(sirali[i].Kod);
                satir.Oran.Text = VizYazim.Bicimle(sirali[i].Oran, basamak);
                satir.Vekil.Text = sirali[i].Vekil.ToString();
            }

            IlleriListele(secim.Kod, basamak);

            yukleniyor = false;
        }

        /// <summary> Soldaki liste: hangi ili hangi parti kazandi, kac oyla. </summary>
        private void IlleriListele(string secimKod, int basamak)
        {
            Dictionary<string, int> sayac = new Dictionary<string, int>();

            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Il il = DataService.IlBul(plaka);
                if (il == null) continue;

                Parti kazanan = Kazanan(secimKod, plaka);
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

            lbl_iller.Text = "İL BAZINDA KAZANAN PARTİ   —   " + string.Join(" / ", ozet.ToArray());
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

            foreach (PartiSatiri satir in satirlar)
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

        private static int SafeInt(string metin)
        {
            int deger;
            int.TryParse(metin, out deger);
            return deger;
        }

        private class PartiSatiri
        {
            public readonly ComboBox Parti;
            public readonly TextBox Oran;
            public readonly TextBox Vekil;

            public PartiSatiri(ComboBox parti, TextBox oran, TextBox vekil)
            {
                Parti = parti;
                Oran = oran;
                Vekil = vekil;
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
