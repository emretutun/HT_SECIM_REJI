using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 14 - MV_MECLIS_TR_GENELI_ITTIFAK
    ///
    /// Sahne 13'un ittifak hali. Ayni 600 koltukluk meclis semasi, ama satirlar
    /// parti degil ITTIFAK ve 5 slot var.
    ///
    /// Ittifakin vekil sayisi veride ayrica tutulmuyor; partilerin uzerindeki
    /// "ittifak" kodundan toplaniyor (AKP + MHP + YRP + BBP = CUMHUR gibi).
    /// Boylece parti sonuclari degisince ittifak kendiliginden dogru kalir.
    ///
    /// Ad, logo ve koltuk rengi veri.json'daki "ittifaklar" bolumunden geliyor.
    ///
    /// Vekili olmayan ittifak da gosteriliyor (0 yazar) - tanimli oldugu surece.
    /// Karsiligi olmayan slotlar gizleniyor.
    /// </summary>
    public partial class Page14_MvMeclisIttifak : UserControl, IScenePage
    {
        /// <summary> Sahnedeki koltuk sayisi. </summary>
        private const int KOLTUK_SAYISI = 600;

        /// <summary> Sahnede 5 ittifak slotu var (SIRA_1 ... SIRA_5). </summary>
        private const int SLOT_SAYISI = 5;

        /// <summary> Koltuklari tutan ust container. </summary>
        private const string MECLIS_CONTAINER = "meclis";

        private readonly List<IttifakSatiri> satirlar = new List<IttifakSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary> Koltuklarin en son hangi secimle boyandigi. </summary>
        private string koltukSecimi = null;

        public SceneInfo Scene { set; get; }

        public Page14_MvMeclisIttifak()
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

        private void SatirlariKur()
        {
            pnl_partiler.SuspendLayout();
            pnl_partiler.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            for (int i = 1; i <= SLOT_SAYISI; i++)
            {
                int y = 8 + (i - 1) * 32;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(8, y + 3);
                lblSira.Size = new Size(24, 18);

                ComboBox cmbIttifak = new ComboBox();
                cmbIttifak.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbIttifak.Font = normal;
                cmbIttifak.Location = new Point(34, y);
                cmbIttifak.Size = new Size(260, 22);
                cmbIttifak.Items.Add("— yok —");
                foreach (string kod in DataService.Ittifaklar())
                    cmbIttifak.Items.Add(new IttifakSecimi(kod));
                cmbIttifak.SelectedIndex = 0;

                TextBox txtVekil = new TextBox();
                txtVekil.Font = normal;
                txtVekil.Location = new Point(302, y);
                txtVekil.Size = new Size(70, 22);
                txtVekil.TextAlign = HorizontalAlignment.Center;

                Label lblMv = new Label();
                lblMv.Text = "MV";
                lblMv.Font = kalin;
                lblMv.Location = new Point(378, y + 3);
                lblMv.Size = new Size(30, 18);

                pnl_partiler.Controls.Add(lblSira);
                pnl_partiler.Controls.Add(cmbIttifak);
                pnl_partiler.Controls.Add(txtVekil);
                pnl_partiler.Controls.Add(lblMv);

                satirlar.Add(new IttifakSatiri(cmbIttifak, txtVekil));
            }

            pnl_partiler.ResumeLayout();
        }

        #endregion

        #region IScenePage

        public void SayfaAcildi()
        {
            Kur();
        }

        /// <summary> Sahne bastan yuklendi: koltuklar da tasarim halinde geldi. </summary>
        public void Sifirla()
        {
            koltukSecimi = null;
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

            // --- 5 ittifak slotu ---
            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                IttifakSatiri satir = satirlar[i];
                Ittifak ittifak = satir.SeciliIttifak;

                // Karsiligi olmayan slot gizleniyor; vekili 0 olan ittifak gosteriliyor.
                if (ittifak == null)
                {
                    komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, false));
                    continue;
                }

                komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, true));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_parti_isim", ittifak.Ad));
                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    SafeInt(satir.Vekil.Text).ToString()));

                string logo = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", ittifak.VizLogoImage);
                if (logo != null) komutlar.Add(logo);
                else CLog.Error("ITTIFAK LOGOSU YOK", ittifak.Kod + " / " + ittifak.Ad);
            }

            KoltuklariBoya(komutlar, layer);

            return komutlar;
        }

        /// <summary>
        /// 600 koltugu ittifaklara gore boyar. En cok vekili olan 1. koltuktan
        /// baslar, digerleri arkasindan gelir.
        /// </summary>
        private void KoltuklariBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (koltukSecimi == secim.Kod) return;
            koltukSecimi = secim.Kod;

            int koltuk = 1;

            foreach (IttifakSonuc sonuc in KoltukSirasi(secim.Kod))
            {
                for (int i = 0; i < sonuc.Vekil && koltuk <= KOLTUK_SAYISI; i++, koltuk++)
                {
                    string komut = VizYazim.YolMaterialRengi(layer,
                        VizYazim.Yol(MECLIS_CONTAINER, koltuk.ToString()), sonuc.Kayit.Renk);

                    if (komut != null) komutlar.Add(komut);
                }
            }

            if (koltuk <= KOLTUK_SAYISI)
                CLog.Error("KOLTUK ACIKTA KALDI",
                    (KOLTUK_SAYISI - koltuk + 1) + " koltuk / " + secim.Kod);
        }

        #endregion

        #region Ittifak hesabi

        private class IttifakSonuc
        {
            public Ittifak Kayit;
            public int Vekil;
            public int Oran;
        }

        /// <summary>
        /// Ittifaklarin vekil ve oy toplami, buyukten kucuge.
        /// Toplamlar partilerin uzerindeki ittifak kodundan hesaplaniyor.
        /// </summary>
        private static List<IttifakSonuc> KoltukSirasi(string secimKod)
        {
            List<IttifakSonuc> liste = new List<IttifakSonuc>();

            foreach (string kod in DataService.Ittifaklar())
            {
                Ittifak kayit = DataService.IttifakBul(kod);
                if (kayit == null) continue;

                IttifakSonuc sonuc = new IttifakSonuc();
                sonuc.Kayit = kayit;
                sonuc.Vekil = DataService.IttifakVekilToplami(secimKod, 0, kod);
                sonuc.Oran = DataService.IttifakOrani(secimKod, 0, kod);

                liste.Add(sonuc);
            }

            // Esitlikte de hep ayni sira: once vekil, sonra oy orani, sonra kod.
            liste.Sort(delegate (IttifakSonuc a, IttifakSonuc b)
            {
                if (a.Vekil != b.Vekil) return b.Vekil.CompareTo(a.Vekil);
                if (a.Oran != b.Oran) return b.Oran.CompareTo(a.Oran);

                return string.CompareOrdinal(a.Kayit.Kod, b.Kayit.Kod);
            });

            return liste;
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

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, 0);

            if (sonuc == null)
            {
                txt_baslik_alt.Text = "TÜRKİYE GENELİ";
                txt_ass.Text = "";

                foreach (IttifakSatiri satir in satirlar)
                {
                    satir.Ittifak.SelectedIndex = 0;
                    satir.Vekil.Text = "";
                }
                lst_iller.Items.Clear();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / TURKIYE GENELI");
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);

            List<IttifakSonuc> sirali = KoltukSirasi(secim.Kod);

            int toplam = 0;
            foreach (IttifakSonuc x in sirali) toplam += x.Vekil;

            // Sahnedeki bicim: "TÜRKİYE GENELİ / Toplam 600 MV"
            txt_baslik_alt.Text = "TÜRKİYE GENELİ / Toplam " + toplam + " MV";

            for (int i = 0; i < satirlar.Count; i++)
            {
                IttifakSatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Ittifak.SelectedIndex = 0;
                    satir.Vekil.Text = "";
                    continue;
                }

                satir.SeciliIttifak = sirali[i].Kayit;
                satir.Vekil.Text = sirali[i].Vekil.ToString();
            }

            KoltuklariListele(secim.Kod);

            yukleniyor = false;
        }

        /// <summary> Soldaki liste: hangi koltuk araligi hangi ittifakta. </summary>
        private void KoltuklariListele(string secimKod)
        {
            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();

            int koltuk = 1;
            int toplam = 0;

            foreach (IttifakSonuc x in KoltukSirasi(secimKod))
            {
                string satir = x.Kayit.Ad.PadRight(28) + x.Vekil.ToString().PadLeft(4) + " MV";

                if (x.Vekil > 0)
                {
                    int son = koltuk + x.Vekil - 1;
                    if (son > KOLTUK_SAYISI) son = KOLTUK_SAYISI;

                    satir += "   koltuk " + koltuk + "-" + son;
                    koltuk = son + 1;
                }

                lst_iller.Items.Add(satir);
                toplam += x.Vekil;
            }

            // Hangi parti hangi ittifakta, operator gorebilsin.
            lst_iller.Items.Add("");
            foreach (string kod in DataService.Ittifaklar())
            {
                List<string> adlar = new List<string>();
                foreach (Parti p in DataService.IttifakPartileri(kod)) adlar.Add(p.Ad);

                lst_iller.Items.Add(kod + ": " + string.Join(" + ", adlar.ToArray()));
            }

            lst_iller.EndUpdate();

            lbl_iller.Text = "İTTİFAK KOLTUK DAĞILIMI   —   toplam " + toplam + " / " + KOLTUK_SAYISI;
        }

        private void YenidenBicimle()
        {
            yukleniyor = true;
            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), Ondalik);
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

        /// <summary> Combo'da gosterilecek ittifak kaydi. </summary>
        private class IttifakSecimi
        {
            public readonly string Kod;

            public IttifakSecimi(string kod) { Kod = kod; }

            public override string ToString() { return DataService.IttifakAdi(Kod); }
        }

        private class IttifakSatiri
        {
            public readonly ComboBox Ittifak;
            public readonly TextBox Vekil;

            public IttifakSatiri(ComboBox ittifak, TextBox vekil)
            {
                Ittifak = ittifak;
                Vekil = vekil;
            }

            public Ittifak SeciliIttifak
            {
                get
                {
                    IttifakSecimi secim = Ittifak.SelectedItem as IttifakSecimi;
                    return (secim == null) ? null : DataService.IttifakBul(secim.Kod);
                }
                set
                {
                    if (value == null) { Ittifak.SelectedIndex = 0; return; }

                    for (int i = 0; i < Ittifak.Items.Count; i++)
                    {
                        IttifakSecimi liste = Ittifak.Items[i] as IttifakSecimi;
                        if (liste != null && liste.Kod == value.Kod) { Ittifak.SelectedIndex = i; return; }
                    }

                    Ittifak.SelectedIndex = 0;
                }
            }
        }
    }
}
