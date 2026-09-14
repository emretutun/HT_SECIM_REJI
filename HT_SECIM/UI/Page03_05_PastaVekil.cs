using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 3 ve 5 - ittifak ici dagilim, VEKIL SAYISI
    ///   sahne 3: MV_CUMHUR_ITTIFAKI_PASTA_GRAFIK_MV_SAYISI     (4 satir)
    ///   sahne 5: MV_MILLET_ITTIFAKI_PASTA_GRAFIK_MV_SAYISI_V2  (2 satir)
    ///
    /// Iki sahnenin agaci ayni, tek fark satir sayisi ve hangi ittifak.
    /// Satir sayisi commands dosyasindan (SLOT_<no>), ittifak sahne adindan geliyor.
    ///
    /// Bir ittifakin ic dagilimi: pasta grafik + 4 parti satiri (ad / vekil sayisi / renk).
    /// Pasta verisi plugin parametresinden degil, sahnenin paylasimli belleginden okunuyor:
    /// DataStorage "MyData" anahtarina yaziyor, PieChart oradan alip dilimleri ciziyor.
    /// Dilim renklerini de biz yaziyoruz, boylece siralama degisse de parti-renk eslesmesi bozulmuyor.
    ///
    /// Bu sahnede katilim orani yok, sadece acilan sandik var.
    /// </summary>
    public partial class Page03_05_PastaVekil : UserControl, IScenePage
    {
        /// <summary> PieChart plugin'inde dilim numaralari 0'dan basliyor. </summary>
        private const int ILK_DILIM_NO = 0;

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page03_05_PastaVekil()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Vekil sayisi gosteriliyor; sadece MV veri setleri anlamli.
            cmb_secim.Items.Clear();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsMV) cmb_secim.Items.Add(secim);

            if (cmb_secim.Items.Count > 0) cmb_secim.SelectedIndex = 0;

            cmb_ittifak.Items.Clear();
            foreach (string kod in DataService.Ittifaklar())
                cmb_ittifak.Items.Add(new IttifakSecimi(kod));

            IttifakSec(SahneninIttifaki);

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 1;

            SatirlariKur();

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ittifak.SelectedIndexChanged += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary>
        /// Sahnedeki satir slotu sayisi. Sahneden okunamiyor (bos slotlarin adi
        /// yine sira5_... diye duruyor), o yuzden commands dosyasinda tanimli:
        /// once sahneye ozel SLOT_&lt;no&gt;, yoksa genel SLOT.
        /// </summary>
        private int SlotSayisi
        {
            get
            {
                double deger = CommandRepository.Ayar("SLOT", 4);

                if (Scene != null)
                    deger = CommandRepository.Ayar("SLOT_" + Scene.No, deger);

                int slot = (int)deger;

                if (slot < 1) slot = 1;
                if (slot > 8) slot = 8;

                return slot;
            }
        }

        /// <summary>
        /// Panelin hangi ittifakla acilacagi. Sahne adinda ittifak kodu geciyor
        /// (MV_CUMHUR_ITTIFAKI... / MV_MILLET_ITTIFAKI...), oradan okunuyor.
        /// Operator yine combodan degistirebilir.
        /// </summary>
        private string SahneninIttifaki
        {
            get
            {
                string ad = (Scene == null ? "" : Scene.SceneName).ToUpperInvariant();

                foreach (string kod in DataService.Ittifaklar())
                    if (ad.IndexOf(kod, StringComparison.Ordinal) >= 0) return kod;

                return "CUMHUR";
            }
        }

        private void IttifakSec(string kod)
        {
            for (int i = 0; i < cmb_ittifak.Items.Count; i++)
            {
                IttifakSecimi it = cmb_ittifak.Items[i] as IttifakSecimi;
                if (it != null && it.Kod == kod) { cmb_ittifak.SelectedIndex = i; return; }
            }

            if (cmb_ittifak.Items.Count > 0) cmb_ittifak.SelectedIndex = 0;
        }

        /// <summary> Sira satirlarini (sira no + parti + vekil sayisi) kodla uretir. </summary>
        private void SatirlariKur()
        {
            pnl_partiler.SuspendLayout();
            pnl_partiler.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            int slot = SlotSayisi;

            // 6 satirda panele sigmasi icin araligi biraz daraltiyoruz.
            int ust = (slot > 4) ? 8 : 12;
            int aralik = (slot > 4) ? 38 : 46;

            for (int i = 1; i <= slot; i++)
            {
                int y = ust + (i - 1) * aralik;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(10, y + 3);
                lblSira.Size = new Size(28, 20);

                ComboBox cmbParti = new ComboBox();
                cmbParti.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbParti.Font = normal;
                cmbParti.Location = new Point(42, y);
                cmbParti.Size = new Size(230, 22);
                cmbParti.Items.Add("— yok —");
                foreach (Parti parti in DataService.Veri.Partiler) cmbParti.Items.Add(parti);
                cmbParti.SelectedIndex = 0;

                TextBox txtVekil = new TextBox();
                txtVekil.Font = normal;
                txtVekil.Location = new Point(282, y);
                txtVekil.Size = new Size(80, 22);
                txtVekil.TextAlign = HorizontalAlignment.Center;

                Label lblMv = new Label();
                lblMv.Text = "MV";
                lblMv.Font = kalin;
                lblMv.Location = new Point(368, y + 3);
                lblMv.Size = new Size(32, 20);

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

            // Bu sahnede katilim yok, sadece acilan sandik.
            // Ayracin adi sahne 2'dekinden farkli: burada TG_NOKTA_SABIT.
            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", "TG_NOKTA_SABIT", "ASS_PUAN2", basamak);

            List<string> pastaDegerleri = new List<string>();

            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                PartiSatiri satir = satirlar[i];
                Parti parti = satir.SeciliParti;

                // Ittifakta 4'ten az parti varsa (MILLET 3, EMEK 2) bos satiri
                // komple gizliyoruz, ekranda bos kutu kalmasin.
                if (parti == null)
                {
                    komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, false));
                    continue;
                }

                komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, true));

                int vekil = SafeInt(satir.Vekil.Text);

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_parti_isim", parti.Ad));
                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran", vekil.ToString()));

                // Sagdaki satir kutusu image ile boyaniyor (partinin serit gorseli).
                string gorsel = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", parti.VizSatirImage);
                if (gorsel != null) komutlar.Add(gorsel);

                // Pasta dilimi ise sayisal renkle; siralama degisse de eslesme bozulmaz.
                VizYazim.DilimRengi(komutlar, layer, "PieChart", ILK_DILIM_NO + i, parti.Renk);

                pastaDegerleri.Add(vekil.ToString());
            }

            // Pasta dilimleri: sahnenin paylasimli belleginde "MyData" anahtari, virgulle ayrilmis.
            if (Scene != null)
                komutlar.Add(VizYazim.PaylasimliBellek(Scene.FullPath, "MyData",
                    string.Join(",", pastaDegerleri.ToArray())));

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
                    !int.TryParse(cmb_ondalik.SelectedItem.ToString(), out basamak)) return 1;

                return basamak;
            }
        }

        private Secim SeciliSecim
        {
            get { return cmb_secim.SelectedItem as Secim; }
        }

        private string SeciliIttifak
        {
            get
            {
                IttifakSecimi it = cmb_ittifak.SelectedItem as IttifakSecimi;
                return it == null ? "" : it.Kod;
            }
        }

        private void VeridenDoldur()
        {
            Secim secim = SeciliSecim;
            Il il = ilSecici1.SeciliIl;
            string ittifak = SeciliIttifak;

            if (secim == null || il == null || ittifak.Length == 0) return;

            yukleniyor = true;

            txt_baslik_ust.Text = secim.Ad;
            txt_baslik_alt.Text = DataService.IttifakAdi(ittifak) + "/" + il.Ad;

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, il.Plaka);

            if (sonuc == null)
            {
                txt_ass.Text = "";
                BosaltSatirlar();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / plaka " + il.Plaka);
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, Ondalik);

            // Ittifaka bagli partilerin bu ildeki vekil sayilari, buyukten kucuge.
            List<Oy> ittifakOylari = new List<Oy>();

            foreach (Oy oy in sonuc.Oylar)
            {
                Parti parti = DataService.PartiBul(oy.Kod);
                if (parti == null || parti.Ittifak != ittifak) continue;

                ittifakOylari.Add(oy);
            }

            ittifakOylari.Sort(delegate (Oy a, Oy b) { return b.Vekil.CompareTo(a.Vekil); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                PartiSatiri satir = satirlar[i];

                if (i >= ittifakOylari.Count)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Vekil.Text = "0";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(ittifakOylari[i].Kod);
                satir.Vekil.Text = ittifakOylari[i].Vekil.ToString();
            }

            yukleniyor = false;
        }

        private void BosaltSatirlar()
        {
            foreach (PartiSatiri satir in satirlar)
            {
                satir.Parti.SelectedIndex = 0;
                satir.Vekil.Text = "0";
            }
        }

        private void YenidenBicimle()
        {
            yukleniyor = true;
            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), Ondalik);
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

        private static int SafeInt(string metin)
        {
            int deger;
            int.TryParse(metin, out deger);
            return deger;
        }

        /// <summary> Ittifak combo'sunda gosterilecek kayit. </summary>
        private class IttifakSecimi
        {
            public readonly string Kod;

            public IttifakSecimi(string kod) { Kod = kod; }

            public override string ToString() { return DataService.IttifakAdi(Kod); }
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
