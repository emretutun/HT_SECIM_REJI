using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 13 - MV_MECLIS_TR_GENELI
    ///
    /// Meclis oturma semasi: 600 koltuk + en cok vekil cikaran 7 parti.
    /// Koltuklar sahnede "1" ... "600" diye numarali, her birinin kendi
    /// MATERIAL'i var. Vekil sayisina gore buyukten kucuge doldurulur:
    /// en cok vekili olan parti 1. koltuktan baslar, sonraki onun bittigi
    /// yerden devam eder. "DIGER" gibi toplu kalemler en sona birakilir.
    ///
    /// Satirdaki sayi oy orani degil VEKIL SAYISI - sahnedeki sabit yazi "mv".
    /// Container adi "..._oran" ama icerigi vekil.
    ///
    /// Koltuk renkleri Parti.MeclisRenk'ten geliyor - harita sahnelerininkinden
    /// ayri. Orada renk yanindaki satir seridiyle uyusmak zorunda ve serit
    /// gorseli olmayan partiler ortak koyu kirmizi seride dusuyor; mecliste ise
    /// 600 koltuk yan yana ve birbirine yakin uc kirmizi ayirt edilemiyordu.
    ///
    /// 600 koltuk 600 komut demek; kazananlar sadece secim degisince degistigi
    /// icin sehir/veri tazelemede tekrar gonderilmiyor.
    /// </summary>
    public partial class Page13_MvMeclis : UserControl, IScenePage
    {
        /// <summary> Sahnedeki koltuk sayisi. </summary>
        private const int KOLTUK_SAYISI = 600;

        /// <summary> Sahnede 7 parti slotu var (SIRA_1 ... SIRA_7). </summary>
        private const int SLOT_SAYISI = 7;

        /// <summary> Koltuklari tutan ust container. </summary>
        private const string MECLIS_CONTAINER = "meclis";

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary> Koltuklarin en son hangi secimle boyandigi. </summary>
        private string koltukSecimi = null;

        public SceneInfo Scene { set; get; }

        public Page13_MvMeclis()
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
                cmbParti.Size = new Size(220, 22);
                cmbParti.Items.Add("— yok —");
                foreach (Parti parti in DataService.Veri.Partiler) cmbParti.Items.Add(parti);
                cmbParti.SelectedIndex = 0;

                TextBox txtVekil = new TextBox();
                txtVekil.Font = normal;
                txtVekil.Location = new Point(262, y);
                txtVekil.Size = new Size(70, 22);
                txtVekil.TextAlign = HorizontalAlignment.Center;

                Label lblMv = new Label();
                lblMv.Text = "MV";
                lblMv.Font = kalin;
                lblMv.Location = new Point(338, y + 3);
                lblMv.Size = new Size(30, 18);

                pnl_partiler.Controls.Add(lblSira);
                pnl_partiler.Controls.Add(cmbParti);
                pnl_partiler.Controls.Add(txtVekil);
                pnl_partiler.Controls.Add(lblMv);

                satirlar.Add(new PartiSatiri(cmbParti, txtVekil));
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

                // Yanindaki sabit yazi "mv": bu alan vekil sayisi.
                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    SafeInt(satir.Vekil.Text).ToString()));

                string serit = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", parti.VizHaritaSeritImage);
                if (serit != null) komutlar.Add(serit);
            }

            KoltuklariBoya(komutlar, layer);

            return komutlar;
        }

        /// <summary>
        /// 600 koltugu partilere gore boyar. Koltuk numarasi sirayla ilerler:
        /// en cok vekili olan parti 1. koltuktan baslar, digerleri arkasindan
        /// gelir, toplu kalemler ("DIGER") en sona kalir.
        /// Artan koltuk olursa (veri eksikse) sahnedeki hali birakilir.
        /// </summary>
        private void KoltuklariBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (koltukSecimi == secim.Kod) return;
            koltukSecimi = secim.Kod;

            int koltuk = 1;

            foreach (Oy oy in KoltukSirasi(secim.Kod))
            {
                Parti parti = DataService.PartiBul(oy.Kod);
                if (parti == null) continue;

                for (int i = 0; i < oy.Vekil && koltuk <= KOLTUK_SAYISI; i++, koltuk++)
                {
                    string komut = VizYazim.YolMaterialRengi(layer,
                        VizYazim.Yol(MECLIS_CONTAINER, koltuk.ToString()), parti.MeclisRenk);

                    if (komut != null) komutlar.Add(komut);
                }
            }

            if (koltuk <= KOLTUK_SAYISI)
                CLog.Error("KOLTUK ACIKTA KALDI",
                    (KOLTUK_SAYISI - koltuk + 1) + " koltuk / " + secim.Kod);
        }

        /// <summary>
        /// Koltuklarin doldurulma sirasi: vekil sayisina gore buyukten kucuge,
        /// toplu kalemler ("DIGER") en sonda.
        /// </summary>
        private static List<Oy> KoltukSirasi(string secimKod)
        {
            List<Oy> liste = new List<Oy>();

            IlSonucu geneli = DataService.SonucBul(secimKod, 0);
            if (geneli == null) return liste;

            foreach (Oy oy in geneli.Oylar)
                if (oy.Vekil > 0) liste.Add(oy);

            // Esitlikte de hep ayni sirayi versin: List.Sort kararli degil,
            // TIP ile SAADET'in ikisi de 3 vekilliyken sira her calistirmada
            // degisiyordu. Once vekil, sonra oy orani, sonra kod.
            liste.Sort(delegate (Oy a, Oy b)
            {
                bool aToplu = TopluMu(a.Kod);
                bool bToplu = TopluMu(b.Kod);

                if (aToplu != bToplu) return aToplu ? 1 : -1;   // toplu kalemler sona

                if (a.Vekil != b.Vekil) return b.Vekil.CompareTo(a.Vekil);
                if (a.Oran != b.Oran) return b.Oran.CompareTo(a.Oran);

                return string.CompareOrdinal(a.Kod, b.Kod);
            });

            return liste;
        }

        private static bool TopluMu(string partiKod)
        {
            Parti parti = DataService.PartiBul(partiKod);
            return parti != null && parti.Toplu;
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
                foreach (PartiSatiri satir in satirlar)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Vekil.Text = "";
                }
                lst_iller.Items.Clear();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / TURKIYE GENELI");
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);

            // Meclis semasi: slotlar VEKIL sayisina gore siralanir.
            List<Oy> sirali = KoltukSirasi(secim.Kod);

            for (int i = 0; i < satirlar.Count; i++)
            {
                PartiSatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Vekil.Text = "";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(sirali[i].Kod);
                satir.Vekil.Text = sirali[i].Vekil.ToString();
            }

            KoltuklariListele(secim.Kod);

            yukleniyor = false;
        }

        /// <summary> Soldaki liste: hangi koltuk araligi hangi partide. </summary>
        private void KoltuklariListele(string secimKod)
        {
            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();

            int koltuk = 1;
            int toplam = 0;

            foreach (Oy oy in KoltukSirasi(secimKod))
            {
                Parti parti = DataService.PartiBul(oy.Kod);
                string ad = (parti == null) ? oy.Kod : parti.Ad;

                int son = koltuk + oy.Vekil - 1;
                if (son > KOLTUK_SAYISI) son = KOLTUK_SAYISI;

                lst_iller.Items.Add(ad.PadRight(16) +
                                    oy.Vekil.ToString().PadLeft(4) + " MV   " +
                                    "koltuk " + koltuk + "-" + son);

                koltuk = son + 1;
                toplam += oy.Vekil;
            }

            lst_iller.EndUpdate();

            lbl_iller.Text = "KOLTUK DAĞILIMI   —   toplam " + toplam + " / " + KOLTUK_SAYISI;
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

        private class PartiSatiri
        {
            public readonly ComboBox Parti;
            public readonly TextBox Vekil;

            public PartiSatiri(ComboBox parti, TextBox vekil)
            {
                Parti = parti;
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
