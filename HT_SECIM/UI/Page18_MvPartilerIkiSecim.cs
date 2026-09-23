using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 18 - MV_PARTILER_IKI_SECIM_KIYAS
    ///
    /// Iki parti, her biri icin iki secimin oy orani: toplam dort bar.
    /// Secimler iki tarafta ortak, partiler farkli. Ornek:
    /// AK PARTI 2023 / AK PARTI 2018  ---  CHP 2023 / CHP 2018
    ///
    /// Sahne duzeni (soldan saga): logo1, parti1'in iki bari, parti2'nin iki bari,
    /// logo2. Yil etiketleri barlarin altinda, parti adlari en distaki kutularda.
    ///
    /// Barlarin bitis keyframe'leri sahnede "End1" (birinci bar) ve "End2" (ikinci
    /// bar) diye adlandirilmis; iki tarafta da ayni adlar kullaniliyor. Keyframe
    /// kanal id'siyle adreslendigi icin karisma olmuyor.
    /// </summary>
    public partial class Page18_MvPartilerIkiSecim : UserControl, IScenePage
    {
        /// <summary> Birinci (soldaki) barin bitis keyframe adi. </summary>
        private const string KEYFRAME_BAR1 = "End1";

        /// <summary> Ikinci (sagdaki) barin bitis keyframe adi. </summary>
        private const string KEYFRAME_BAR2 = "End2";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page18_MvPartilerIkiSecim()
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

            // Sahnedeki duzen "yeni yil / eski yil"; listedeki ilk ve son secim.
            if (cmb_secim1.Items.Count > 0) cmb_secim1.SelectedIndex = 0;
            if (cmb_secim2.Items.Count > 0) cmb_secim2.SelectedIndex = cmb_secim2.Items.Count - 1;

            PartileriYukle(cmb_parti1, "AKP");
            PartileriYukle(cmb_parti2, "CHP");

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim1.SelectedIndexChanged  += Secim_Degisti;
            cmb_secim2.SelectedIndexChanged  += Secim_Degisti;
            cmb_parti1.SelectedIndexChanged  += Secim_Degisti;
            cmb_parti2.SelectedIndexChanged  += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            FarklariYenile();
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

            // Yil etiketleri iki tarafta da ayni, ikisine de ayni metin gidiyor.
            komutlar.Add(VizYazim.Metin(layer, "parti1_yil1", txt_yil1.Text));
            komutlar.Add(VizYazim.Metin(layer, "parti2_yil1", txt_yil1.Text));
            komutlar.Add(VizYazim.Metin(layer, "parti1_yil2", txt_yil2.Text));
            komutlar.Add(VizYazim.Metin(layer, "parti2_yil2", txt_yil2.Text));

            TarafYaz(komutlar, layer, basamak, SeciliParti1, "1",
                txt_p1y1.Text, txt_p1y2.Text);

            TarafYaz(komutlar, layer, basamak, SeciliParti2, "2",
                txt_p2y1.Text, txt_p2y2.Text);

            return komutlar;
        }

        /// <summary>
        /// Bir tarafin butun alanlari. Container adlari "parti1_..." / "parti2_..."
        /// diye ayni kalibi izledigi icin taraf numarasi metne gomuluyor.
        /// </summary>
        private static void TarafYaz(List<string> komutlar, string layer, int basamak,
            Parti parti, string taraf, string oran1Metni, string oran2Metni)
        {
            string on = "parti" + taraf;

            komutlar.Add(VizYazim.Metin(layer, on + "_isim", parti == null ? "" : parti.Ad));

            int oran1 = DataService.YuzdeParse(oran1Metni);
            int oran2 = DataService.YuzdeParse(oran2Metni);

            komutlar.Add(VizYazim.Metin(layer, on + "_yil1_oran", VizYazim.Bicimle(oran1, basamak)));
            komutlar.Add(VizYazim.Metin(layer, on + "_yil2_oran", VizYazim.Bicimle(oran2, basamak)));

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            VizYazim.BarKomutlari(komutlar, layer, on + "_bar1", oran1, barMin, barMax, KEYFRAME_BAR1);
            VizYazim.BarKomutlari(komutlar, layer, on + "_bar2", oran2, barMin, barMax, KEYFRAME_BAR2);

            if (parti == null) return;

            // Bir partinin iki bari da ayni serit gorselini kullaniyor.
            Gorsel(komutlar, layer, on + "_bar1_rn", parti.VizBarImage, "PARTI BAR SERIDI YOK", parti);
            Gorsel(komutlar, layer, on + "_bar2_rn", parti.VizBarImage, "PARTI BAR SERIDI YOK", parti);
            Gorsel(komutlar, layer, "logo" + taraf, parti.VizKiyasLogoImage, "PARTI KIYAS LOGOSU YOK", parti);
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

        private Secim SeciliSecim1 { get { return cmb_secim1.SelectedItem as Secim; } }
        private Secim SeciliSecim2 { get { return cmb_secim2.SelectedItem as Secim; } }
        private Parti SeciliParti1 { get { return cmb_parti1.SelectedItem as Parti; } }
        private Parti SeciliParti2 { get { return cmb_parti2.SelectedItem as Parti; } }

        private void VeridenDoldur()
        {
            Secim yil1 = SeciliSecim1;
            Secim yil2 = SeciliSecim2;
            Parti parti1 = SeciliParti1;
            Parti parti2 = SeciliParti2;
            Il il = ilSecici1.SeciliIl;

            if (yil1 == null || yil2 == null || parti1 == null || parti2 == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            // Sahnede barlarin altinda yalnizca yil yaziyor.
            txt_yil1.Text = yil1.Yil.ToString();
            txt_yil2.Text = yil2.Yil.ToString();

            txt_p1y1.Text = VizYazim.Bicimle(DataService.Oran(yil1.Kod, il.Plaka, parti1.Kod), basamak);
            txt_p1y2.Text = VizYazim.Bicimle(DataService.Oran(yil2.Kod, il.Plaka, parti1.Kod), basamak);
            txt_p2y1.Text = VizYazim.Bicimle(DataService.Oran(yil1.Kod, il.Plaka, parti2.Kod), basamak);
            txt_p2y2.Text = VizYazim.Bicimle(DataService.Oran(yil2.Kod, il.Plaka, parti2.Kod), basamak);

            txt_baslik.Text = "MİLLETVEKİLİ SEÇİMİ / " + il.Ad;

            yukleniyor = false;
        }

        /// <summary>
        /// Il listesini SOLDAKI partinin iki secim arasindaki degisimine
        /// gore renklendirir. Sagdaki parti de ayni yillarla gosteriliyor
        /// ama liste tek bir renge boyanabildigi icin olcu birincisi.
        /// </summary>
        private void FarklariYenile()
        {
            Secim yil1 = SeciliSecim1;
            Secim yil2 = SeciliSecim2;
            Parti parti1 = SeciliParti1;

            if (yil1 == null || yil2 == null || parti1 == null)
            {
                ilSecici1.FarklariTemizle();
                return;
            }

            ilSecici1.FarklariGoster(DataService.Farklar(yil1.Kod, yil2.Kod, parti1.Kod));
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_p1y1.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_p1y1.Text), basamak);
            txt_p1y2.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_p1y2.Text), basamak);
            txt_p2y1.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_p2y1.Text), basamak);
            txt_p2y2.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_p2y2.Text), basamak);

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
