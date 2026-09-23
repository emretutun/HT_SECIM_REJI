using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 20 - YILLIK_KARSILASTIRMA_2LI
    /// Sahne 21 - YILLIK_KARSILASTIRMA_3LU
    ///
    /// Tek parti, birkac secim: solda logo ve parti adi, sagda her secim icin bir bar.
    /// Iki sahne ayni kalibi kullaniyor, tek fark bar sayisi ve oran container'larinin
    /// adi. Ikisi de commands dosyasindan geliyor:
    ///
    ///   SLOT_20    = 2                 SLOT_21    = 3
    ///   ORAN_ADI_20 = yil{n}_oran      ORAN_ADI_21 = oran{n}
    ///
    /// Kullanilmayan satir (20'de ucuncu secim) panelde gizleniyor.
    ///
    /// Parti adi bu sahnelerde renklendirilmemis (ne materyal ne texture var),
    /// beyaz kaliyor; yalnizca metni yaziyoruz.
    ///
    /// Barlarin bitis keyframe'leri sahnede "End1", "End2", "End3" diye
    /// adlandirilmis.
    /// </summary>
    public partial class Page20_21_YillikKarsilastirma : UserControl, IScenePage
    {
        /// <summary> Panelde hazir duran en fazla satir sayisi. </summary>
        private const int EN_COK_SATIR = 3;

        /// <summary> Oran container adi commands'ta yoksa kullanilan kalip. </summary>
        private const string VARSAYILAN_ORAN_ADI = "yil{n}_oran";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        private ComboBox[] secimKutulari;
        private TextBox[] yilKutulari;
        private TextBox[] oranKutulari;
        private Control[][] satirKontrolleri;

        public SceneInfo Scene { set; get; }

        public Page20_21_YillikKarsilastirma()
        {
            InitializeComponent();
        }

        #region Sahneye gore sekil

        /// <summary> Bu sahnede kac bar var: commands'taki SLOT_&lt;sahne no&gt;. </summary>
        private int Satir
        {
            get
            {
                double deger = CommandRepository.Ayar("SLOT", 2);

                if (Scene != null)
                    deger = CommandRepository.Ayar("SLOT_" + Scene.No, deger);

                int satir = (int)deger;

                if (satir < 1) satir = 1;
                if (satir > EN_COK_SATIR) satir = EN_COK_SATIR;

                return satir;
            }
        }

        /// <summary>
        /// Oran container'inin adi. 20'de "yil1_oran", 21'de "oran1" - sahne
        /// tasarimlari farkli isimlendirilmis, kalip commands dosyasindan geliyor.
        /// </summary>
        private string OranContainer(int sira)
        {
            string kalip = (Scene == null) ? "" : CommandRepository.Sablon("ORAN_ADI_" + Scene.No);

            if (string.IsNullOrEmpty(kalip)) kalip = VARSAYILAN_ORAN_ADI;

            return kalip.Replace("{n}", sira.ToString());
        }

        #endregion

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            secimKutulari = new ComboBox[] { cmb_secim1, cmb_secim2, cmb_secim3 };
            yilKutulari   = new TextBox[]  { txt_yil1, txt_yil2, txt_yil3 };
            oranKutulari  = new TextBox[]  { txt_oran1, txt_oran2, txt_oran3 };

            satirKontrolleri = new Control[][]
            {
                new Control[] { lbl_secim1, cmb_secim1, lbl_yil1, txt_yil1, lbl_oran1, txt_oran1 },
                new Control[] { lbl_secim2, cmb_secim2, lbl_yil2, txt_yil2, lbl_oran2, txt_oran2 },
                new Control[] { lbl_secim3, cmb_secim3, lbl_yil3, txt_yil3, lbl_oran3, txt_oran3 }
            };

            yukleniyor = true;

            int satir = Satir;

            for (int i = 0; i < EN_COK_SATIR; i++)
            {
                bool gorunur = i < satir;
                foreach (Control kontrol in satirKontrolleri[i]) kontrol.Visible = gorunur;
            }

            // Iki barli sahnede ortadaki bar sagdaki oluyor; etiketi ona gore yaz.
            lbl_secim2.Text = (satir >= 3) ? "2. SEÇİM (ORTADAKİ BAR)" : "2. SEÇİM (SAĞDAKİ BAR)";

            List<Secim> secimler = new List<Secim>();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsMV) secimler.Add(secim);

            for (int i = 0; i < EN_COK_SATIR; i++)
            {
                secimKutulari[i].Items.Clear();
                foreach (Secim secim in secimler) secimKutulari[i].Items.Add(secim);

                // Sahnede soldan saga yeniden eskiye gidiyor; veri az ise sonuncuda kal.
                if (secimler.Count > 0)
                    secimKutulari[i].SelectedIndex = Math.Min(i, secimler.Count - 1);
            }

            cmb_parti.Items.Clear();
            foreach (Parti parti in DataService.Veri.Partiler)
            {
                if (parti.Toplu) continue;
                cmb_parti.Items.Add(parti);
            }
            if (cmb_parti.Items.Count > 0) cmb_parti.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            foreach (ComboBox kutu in secimKutulari) kutu.SelectedIndexChanged += Secim_Degisti;

            cmb_parti.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            FarklariYenile();
            VeridenDoldur();
        }

        /// <summary>
        /// Il listesini ILK IKI secim arasindaki degisime gore renklendirir.
        /// Sahne 21'de ucuncu bir secim daha var ama liste tek renge
        /// boyanabiliyor; olcu en ustteki iki yil.
        /// </summary>
        private void FarklariYenile()
        {
            Parti parti = SeciliParti;

            Secim secim1 = (secimKutulari.Length > 0) ? secimKutulari[0].SelectedItem as Secim : null;
            Secim secim2 = (secimKutulari.Length > 1) ? secimKutulari[1].SelectedItem as Secim : null;

            if (parti == null || secim1 == null || secim2 == null)
            {
                ilSecici1.FarklariTemizle();
                return;
            }

            ilSecici1.FarklariGoster(DataService.Farklar(secim1.Kod, secim2.Kod, parti.Kod));
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
            int satir = Satir;

            Parti parti = SeciliParti;

            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik", txt_baslik.Text));
            komutlar.Add(VizYazim.Metin(layer, "parti_isim_txt", parti == null ? "" : parti.Ad));

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            for (int i = 0; i < satir; i++)
            {
                int sira = i + 1;

                komutlar.Add(VizYazim.Metin(layer, "yil" + sira, yilKutulari[i].Text));

                int oran = DataService.YuzdeParse(oranKutulari[i].Text);

                komutlar.Add(VizYazim.Metin(layer, OranContainer(sira),
                    VizYazim.Bicimle(oran, basamak)));

                VizYazim.BarKomutlari(komutlar, layer, "yil" + sira + "_bar",
                    oran, barMin, barMax, "End" + sira);

                if (parti != null)
                    Gorsel(komutlar, layer, "yil" + sira + "_bar_rn",
                        parti.VizBarImage, "PARTI BAR SERIDI YOK", parti);
            }

            if (parti != null)
                Gorsel(komutlar, layer, "logo", parti.VizKiyasLogoImage, "PARTI KIYAS LOGOSU YOK", parti);

            return komutlar;
        }

        private static void Gorsel(List<string> komutlar, string layer, string container,
            string vizImage, string hataBasligi, Parti parti)
        {
            string komut = VizYazim.Gorsel(layer, container, vizImage);

            if (komut != null) komutlar.Add(komut);
            else CLog.Error(hataBasligi, parti.Kod + " / " + parti.Ad);
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

        private Parti SeciliParti { get { return cmb_parti.SelectedItem as Parti; } }

        private void VeridenDoldur()
        {
            Parti parti = SeciliParti;
            Il il = ilSecici1.SeciliIl;

            if (parti == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            for (int i = 0; i < EN_COK_SATIR; i++)
            {
                Secim secim = secimKutulari[i].SelectedItem as Secim;
                if (secim == null) continue;

                yilKutulari[i].Text = secim.Yil.ToString();

                oranKutulari[i].Text =
                    VizYazim.Bicimle(DataService.Oran(secim.Kod, il.Plaka, parti.Kod), basamak);
            }

            txt_baslik.Text = "MİLLETVEKİLİ SEÇİMİ / " + il.Ad;

            yukleniyor = false;
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            for (int i = 0; i < EN_COK_SATIR; i++)
                oranKutulari[i].Text =
                    VizYazim.Bicimle(DataService.YuzdeParse(oranKutulari[i].Text), basamak);

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

            FarklariYenile();
            VeridenDoldur();
        }

        private void Ondalik_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;
            YenidenBicimle();
        }

        private void Doldur_Click(object sender, EventArgs e)
        {
            // Yeni veri geldiginde basilan dugme bu; il listesindeki farklar
            // da tazelenmeli, yoksa oranlar guncellenirken renkler eski kalir.
            FarklariYenile();
            VeridenDoldur();
        }

        #endregion
    }
}
