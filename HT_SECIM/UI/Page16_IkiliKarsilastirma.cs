using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 16 - IKILI_KARSILASTIRMA
    ///
    /// Iki taraf yan yana: solda bir REFERANDUM secenegi (EVET / HAYIR),
    /// sagda bir ITTIFAK. Her tarafin adi, alt yazisi, orani ve bar seridi var.
    ///
    /// Her iki taraf da kendi secimini secebiliyor, boylece
    /// "2017 EVET vs 2018 CUMHUR" da yapilabilir, "2023 CUMHUR vs 2018 CUMHUR" da.
    ///
    /// Barlar keyframe'li (ORAN1 / ORAN2 > BAR_VALUE > barN > Bar > value);
    /// BarKomutlari hem anlik degeri hem keyframe'in End'ini yaziyor.
    ///
    /// Ittifak adi sahnede iki satir ("CUMHUR" / "ITTIFAKI"). Tek satir gonderince
    /// tasiyor, o yuzden son kelimeden once satir basi konuyor.
    /// </summary>
    public partial class Page16_IkiliKarsilastirma : UserControl, IScenePage
    {
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page16_IkiliKarsilastirma()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Sol: referandum secimleri. Sag: ittifaklarin oldugu secimler (MV).
            cmb_secim1.Items.Clear();
            cmb_secim2.Items.Clear();

            foreach (Secim secim in DataService.Veri.Secimler)
            {
                if (secim.IsREF) cmb_secim1.Items.Add(secim);
                if (secim.IsMV)  cmb_secim2.Items.Add(secim);
            }

            if (cmb_secim1.Items.Count > 0) cmb_secim1.SelectedIndex = 0;
            if (cmb_secim2.Items.Count > 0) cmb_secim2.SelectedIndex = cmb_secim2.Items.Count - 1;

            // Sol kalem: EVET / HAYIR
            cmb_kalem1.Items.Clear();
            foreach (Secenek s in DataService.Veri.Secenekler) cmb_kalem1.Items.Add(s);
            if (cmb_kalem1.Items.Count > 0) cmb_kalem1.SelectedIndex = 0;

            // Sag kalem: ittifaklar
            cmb_kalem2.Items.Clear();
            foreach (string kod in DataService.Ittifaklar())
            {
                Ittifak it = DataService.IttifakBul(kod);
                if (it != null) cmb_kalem2.Items.Add(it);
            }
            if (cmb_kalem2.Items.Count > 0) cmb_kalem2.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 1;

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim1.SelectedIndexChanged  += Secim_Degisti;
            cmb_secim2.SelectedIndexChanged  += Secim_Degisti;
            cmb_kalem1.SelectedIndexChanged  += Secim_Degisti;
            cmb_kalem2.SelectedIndexChanged  += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
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

            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik", txt_baslik.Text));

            Secenek secenek = SeciliSecenek;
            Ittifak ittifak = SeciliIttifak;

            // --- sol taraf ---
            komutlar.Add(VizYazim.Metin(layer, "ittifak_isim_1",
                secenek == null ? "" : secenek.Ad));

            komutlar.Add(VizYazim.Metin(layer, "yil1", txt_yil1.Text));

            int oran1 = DataService.YuzdeParse(txt_oran1.Text);
            komutlar.Add(VizYazim.Metin(layer, "parti1_oran", VizYazim.Bicimle(oran1, basamak)));

            // --- sag taraf ---
            komutlar.Add(VizYazim.Metin(layer, "ittifak_isim_2",
                ittifak == null ? "" : IkiSatir(ittifak.Ad)));

            komutlar.Add(VizYazim.Metin(layer, "yil2", txt_yil2.Text));

            int oran2 = DataService.YuzdeParse(txt_oran2.Text);
            komutlar.Add(VizYazim.Metin(layer, "parti2_oran", VizYazim.Bicimle(oran2, basamak)));

            // --- barlar ---
            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            VizYazim.BarKomutlari(komutlar, layer, "bar1", oran1, barMin, barMax);
            VizYazim.BarKomutlari(komutlar, layer, "bar2", oran2, barMin, barMax);

            BarGorseli(komutlar, layer, "BAR1rn",
                secenek == null ? null : secenek.VizBarImage,
                secenek == null ? "" : secenek.Kod);

            BarGorseli(komutlar, layer, "BAR2rn",
                ittifak == null ? null : ittifak.VizBarImage,
                ittifak == null ? "" : ittifak.Kod);

            return komutlar;
        }

        private static void BarGorseli(List<string> komutlar, string layer,
            string container, string vizImage, string etiket)
        {
            string komut = VizYazim.Gorsel(layer, container, vizImage);

            if (komut != null) komutlar.Add(komut);
            else if (etiket.Length > 0) CLog.Error("BAR SERIDI YOK", etiket);
        }

        /// <summary>
        /// "CUMHUR ITTIFAKI" -> "CUMHUR\nITTIFAKI"
        /// Sahnede bu alan iki satir tasarlanmis; tek satir gonderilince tasiyor.
        /// Son kelimeden once boluyoruz, tek kelimelik adlara dokunmuyoruz.
        /// </summary>
        private static string IkiSatir(string ad)
        {
            if (string.IsNullOrEmpty(ad)) return "";

            int son = ad.LastIndexOf(' ');
            if (son <= 0) return ad;

            return ad.Substring(0, son) + "\n" + ad.Substring(son + 1);
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

        private Secim SeciliSecim1 { get { return cmb_secim1.SelectedItem as Secim; } }
        private Secim SeciliSecim2 { get { return cmb_secim2.SelectedItem as Secim; } }
        private Secenek SeciliSecenek { get { return cmb_kalem1.SelectedItem as Secenek; } }
        private Ittifak SeciliIttifak { get { return cmb_kalem2.SelectedItem as Ittifak; } }

        private void VeridenDoldur()
        {
            Secim sol = SeciliSecim1;
            Secim sag = SeciliSecim2;
            Secenek secenek = SeciliSecenek;
            Ittifak ittifak = SeciliIttifak;
            Il il = ilSecici1.SeciliIl;

            if (sol == null || sag == null || secenek == null || ittifak == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_yil1.Text = DataService.SecimEtiketi(sol);
            txt_yil2.Text = DataService.SecimEtiketi(sag);

            txt_oran1.Text = VizYazim.Bicimle(
                DataService.Oran(sol.Kod, il.Plaka, secenek.Kod), basamak);

            txt_oran2.Text = VizYazim.Bicimle(
                DataService.IttifakOrani(sag.Kod, il.Plaka, ittifak.Kod), basamak);

            // Sahnedeki bicim: "REFERANDUM / MİLLETVEKİLİ"
            txt_baslik.Text = BaslikParcasi(sol) + " / " + BaslikParcasi(sag);

            yukleniyor = false;
        }

        /// <summary> Basliktaki kisa ad: "ANAYASA REFERANDUMU" -> "REFERANDUM" gibi degil,
        /// secimin tipine gore tek kelimelik etiket. </summary>
        private static string BaslikParcasi(Secim secim)
        {
            if (secim == null) return "";
            if (secim.IsREF) return "REFERANDUM";
            if (secim.IsMV)  return "MİLLETVEKİLİ";
            if (secim.IsCB)  return "CUMHURBAŞKANLIĞI";

            return secim.Ad;
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_oran1.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_oran1.Text), basamak);
            txt_oran2.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_oran2.Text), basamak);

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
    }
}
