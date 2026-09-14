using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 4 ve 6 - ittifak ici dagilim, OY ORANI
    ///   sahne 4: MV_CUMHUR_ITTIFAKI_PASTA_GRAFIK_OY_ORANI     (4 satir)
    ///   sahne 6: MV_MILLET_ITTIFAKI_PASTA_GRAFIK_OY_ORANI_V2  (2 satir)
    ///
    /// Sahne 3/5'in ayni agaci; tek fark satirlarda vekil sayisi degil OY ORANI olmasi.
    /// Satir sayisi commands dosyasindan (SLOT_&lt;no&gt;), ittifak sahne adindan geliyor.
    /// Satir sayisi commands dosyasindan (SLOT_&lt;no&gt;), ittifak sahne adindan geliyor.
    /// Satirin sonundaki "%" sahnede sabit duruyor, biz yazmiyoruz
    /// (sahne 3'te ayni yerde "MV" yazili).
    ///
    /// Pasta yine paylasimli bellekten besleniyor: DataStorage "MyData" anahtarini
    /// okuyor, PieChart dilimleri oradan ciziyor. Dilim aclari oransal oldugu icin
    /// oranlari x100 tam sayi gonderiyoruz - hem virgul karismiyor
    /// (MyData zaten virgulle ayriliyor) hem sonuc birebir ayni.
    /// </summary>
    public partial class Page04_06_PastaOran : UserControl, IScenePage
    {
        /// <summary> PieChart plugin'inde dilim numaralari 0'dan basliyor. </summary>
        private const int ILK_DILIM_NO = 0;

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page04_06_PastaOran()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Parti oylari gosteriliyor; sadece MV veri setleri anlamli.
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
            cmb_ondalik.SelectedIndex = 2;

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
        /// Sahnedeki satir slotu sayisi. commands dosyasinda tanimli:
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
        /// Panelin hangi ittifakla acilacagi; sahne adindaki ittifak kodundan okunuyor.
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

        /// <summary> Sira satirlarini (sira no + parti + oran) kodla uretir. </summary>
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

            // Bu sahnede katilim yok, sadece acilan sandik. Sayaclar Counter*number ile
            // suruluyor, ayrica keyframe'in End degeri yaziliyor - yoksa director
            // oynayinca sahnedeki eski deger geri gelir.
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

                int oran = DataService.YuzdeParse(satir.Oran.Text);

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_parti_isim", parti.Ad));
                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    VizYazim.Bicimle(oran, basamak)));

                string gorsel = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", parti.VizSatirImage);
                if (gorsel != null) komutlar.Add(gorsel);

                // Dilim rengi partinin kendi rengi; siralama degisse de eslesme bozulmaz.
                VizYazim.DilimRengi(komutlar, layer, "PieChart", ILK_DILIM_NO + i, parti.Renk);

                pastaDegerleri.Add(oran.ToString());
            }

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
                    !int.TryParse(cmb_ondalik.SelectedItem.ToString(), out basamak)) return 2;

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

            int basamak = Ondalik;

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);

            // Ittifaka bagli partilerin bu ildeki oy oranlari, buyukten kucuge.
            List<Oy> ittifakOylari = new List<Oy>();

            foreach (Oy oy in sonuc.Oylar)
            {
                Parti parti = DataService.PartiBul(oy.Kod);
                if (parti == null || parti.Ittifak != ittifak) continue;

                ittifakOylari.Add(oy);
            }

            ittifakOylari.Sort(delegate (Oy a, Oy b) { return b.Oran.CompareTo(a.Oran); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                PartiSatiri satir = satirlar[i];

                if (i >= ittifakOylari.Count)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(ittifakOylari[i].Kod);
                satir.Oran.Text = VizYazim.Bicimle(ittifakOylari[i].Oran, basamak);
            }

            yukleniyor = false;
        }

        private void BosaltSatirlar()
        {
            foreach (PartiSatiri satir in satirlar)
            {
                satir.Parti.SelectedIndex = 0;
                satir.Oran.Text = "";
            }
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);

            foreach (PartiSatiri satir in satirlar)
            {
                if (satir.Oran.Text.Length == 0) continue;
                satir.Oran.Text = VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak);
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
