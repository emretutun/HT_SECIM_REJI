using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 17 - MV_IKILI_KIYASLAMA
    ///
    /// Sahne 16'nin logolu kardesi. Iki taraf yan yana: her tarafta bir PARTI,
    /// kendi secimi, logosu, orani ve bar seridi.
    ///
    /// Her iki taraf da kendi secimini sectigi icin hem "AK PARTI 2023 vs
    /// CHP 2023" hem "AK PARTI 2023 vs AK PARTI 2018" yapilabiliyor.
    ///
    /// Container adlari sahnede biraz yaniltici: soldaki ad kutusunun adi
    /// "DIN_T_-Bold" (font adi gibi), sagdakinin adi "ittifak_isim" ama icine
    /// parti adi yaziliyor. Ikisi de sahnede tekil, karisiklik yok.
    ///
    /// Barlar keyframe'li (ORAN1 / ORAN2 > BAR_VALUE > partiN_bar > Bar > value).
    /// </summary>
    public partial class Page17_MvIkiliKiyaslama : UserControl, IScenePage
    {
        /// <summary> Soldaki parti adi kutusu. Sahnede font adiyla adlandirilmis. </summary>
        private const string SOL_AD_CONTAINER = "DIN_T_-Bold";

        /// <summary> Sagdaki parti adi kutusu. Adi "ittifak" ama parti yaziliyor. </summary>
        private const string SAG_AD_CONTAINER = "ittifak_isim";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page17_MvIkiliKiyaslama()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            cmb_secim1.Items.Clear();
            cmb_secim2.Items.Clear();

            foreach (Secim secim in DataService.Veri.Secimler)
            {
                if (!secim.IsMV) continue;

                cmb_secim1.Items.Add(secim);
                cmb_secim2.Items.Add(secim);
            }

            if (cmb_secim1.Items.Count > 0) cmb_secim1.SelectedIndex = 0;
            if (cmb_secim2.Items.Count > 0) cmb_secim2.SelectedIndex = 0;

            PartileriYukle(cmb_kalem1, "AKP");
            PartileriYukle(cmb_kalem2, "CHP");

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

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

        /// <summary> Parti listesi; toplu kalemler ("DIGER") disarida. </summary>
        private static void PartileriYukle(ComboBox combo, string varsayilanKod)
        {
            combo.Items.Clear();

            foreach (Parti parti in DataService.Veri.Partiler)
            {
                if (parti.Toplu) continue;
                combo.Items.Add(parti);
            }

            for (int i = 0; i < combo.Items.Count; i++)
            {
                Parti p = combo.Items[i] as Parti;
                if (p != null && p.Kod == varsayilanKod) { combo.SelectedIndex = i; return; }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
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

            Parti sol = SeciliParti1;
            Parti sag = SeciliParti2;

            TarafYaz(komutlar, layer, basamak, sol,
                SOL_AD_CONTAINER, "yil1", "parti1_oran", "parti1_bar", "BAR1", "logo1",
                txt_yil1.Text, txt_oran1.Text);

            TarafYaz(komutlar, layer, basamak, sag,
                SAG_AD_CONTAINER, "yil2", "parti2_oran", "parti2_bar", "BAR2", "logo2",
                txt_yil2.Text, txt_oran2.Text);

            return komutlar;
        }

        /// <summary>
        /// adContainer (DIN_T_-Bold / ittifak_isim) logonun tam ustunde duruyor;
        /// icine yazi girince logonun uzerine biniyor. O yuzden gozunu kapatiyoruz,
        /// alt yazi yalnizca yilN container'ina gidiyor.
        /// </summary>
        private static void TarafYaz(List<string> komutlar, string layer, int basamak, Parti parti,
            string adContainer, string yilContainer, string oranContainer,
            string barContainer, string barGorselContainer, string logoContainer,
            string altMetin, string oranMetni)
        {
            komutlar.Add(VizYazim.Aktif(layer, adContainer, false));
            komutlar.Add(VizYazim.Metin(layer, yilContainer, altMetin));

            int oran = DataService.YuzdeParse(oranMetni);
            komutlar.Add(VizYazim.Metin(layer, oranContainer, VizYazim.Bicimle(oran, basamak)));

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            VizYazim.BarKomutlari(komutlar, layer, barContainer, oran, barMin, barMax);

            if (parti == null) return;

            string serit = VizYazim.Gorsel(layer, barGorselContainer, parti.VizBarImage);
            if (serit != null) komutlar.Add(serit);
            else CLog.Error("PARTI BAR SERIDI YOK", parti.Kod + " / " + parti.Ad);

            string logo = VizYazim.Gorsel(layer, logoContainer, parti.VizKiyasLogoImage);
            if (logo != null) komutlar.Add(logo);
            else CLog.Error("PARTI KIYAS LOGOSU YOK", parti.Kod + " / " + parti.Ad);
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

        private Secim SeciliSecim1 { get { return cmb_secim1.SelectedItem as Secim; } }
        private Secim SeciliSecim2 { get { return cmb_secim2.SelectedItem as Secim; } }
        private Parti SeciliParti1 { get { return cmb_kalem1.SelectedItem as Parti; } }
        private Parti SeciliParti2 { get { return cmb_kalem2.SelectedItem as Parti; } }

        private void VeridenDoldur()
        {
            Secim sol = SeciliSecim1;
            Secim sag = SeciliSecim2;
            Parti parti1 = SeciliParti1;
            Parti parti2 = SeciliParti2;
            Il il = ilSecici1.SeciliIl;

            if (sol == null || sag == null || parti1 == null || parti2 == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            // Alt yazi: "AK PARTİ (2023 MV)" - parti adi ve hangi secimden geldigi.
            txt_yil1.Text = AltYazi(parti1, sol);
            txt_yil2.Text = AltYazi(parti2, sag);

            txt_oran1.Text = VizYazim.Bicimle(DataService.Oran(sol.Kod, il.Plaka, parti1.Kod), basamak);
            txt_oran2.Text = VizYazim.Bicimle(DataService.Oran(sag.Kod, il.Plaka, parti2.Kod), basamak);

            // Baslikta yalnizca bolge var; secim bilgisi alt yazilarda.
            txt_baslik.Text = il.Ad;

            yukleniyor = false;
        }

        /// <summary> "AK PARTİ (2023 MV)" </summary>
        private static string AltYazi(Parti parti, Secim secim)
        {
            if (parti == null) return "";
            if (secim == null) return parti.Ad;

            return parti.Ad + " (" + KisaSecim(secim) + ")";
        }

        /// <summary> "2023 MV" / "2018 CB" / "2017 REF" </summary>
        private static string KisaSecim(Secim secim)
        {
            string tip = secim.IsMV ? "MV" : secim.IsCB ? "CB" : secim.IsREF ? "REF" : "";

            if (tip.Length == 0) return secim.Yil.ToString();

            return secim.Yil + " " + tip;
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
