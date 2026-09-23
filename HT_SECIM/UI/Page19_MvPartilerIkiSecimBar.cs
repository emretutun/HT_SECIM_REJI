using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 19 - MV_PARTILER_IKI_SECIM_KIYAS_BAR_GRAFIK
    ///
    /// Sahne 18'in bes partilik hali: bes parti, her biri icin iki secimin oy orani,
    /// toplam on bar. Logo yok; parti adi barlarin ustunde yaziyor.
    ///
    /// Parti adinin rengi sahnede materyalden degil TEXTURE'dan geliyor - hem de
    /// barin kullandigi seridin aynisi. O yuzden "partiN_isim"e hem metni hem
    /// bar seridini gonderiyoruz, boylece parti degisince adin rengi de degisiyor.
    ///
    /// Barlarin bitis keyframe'leri 18'deki gibi: birinci bar "End1", ikinci bar
    /// "End2". Bes partide de ayni adlar; keyframe kanal id'siyle adreslendigi
    /// icin karisma olmuyor.
    /// </summary>
    public partial class Page19_MvPartilerIkiSecimBar : UserControl, IScenePage
    {
        /// <summary> Sahnedeki parti sutunu sayisi. </summary>
        private const int SUTUN = 5;

        private const string KEYFRAME_BAR1 = "End1";
        private const string KEYFRAME_BAR2 = "End2";

        private bool kuruldu = false;
        private bool yukleniyor = false;

        private ComboBox[] partiKutulari;
        private TextBox[] oran1Kutulari;
        private TextBox[] oran2Kutulari;

        public SceneInfo Scene { set; get; }

        public Page19_MvPartilerIkiSecimBar()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            partiKutulari = new ComboBox[] { cmb_parti1, cmb_parti2, cmb_parti3, cmb_parti4, cmb_parti5 };
            oran1Kutulari = new TextBox[] { txt_p1y1, txt_p2y1, txt_p3y1, txt_p4y1, txt_p5y1 };
            oran2Kutulari = new TextBox[] { txt_p1y2, txt_p2y2, txt_p3y2, txt_p4y2, txt_p5y2 };

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
            if (cmb_secim2.Items.Count > 0) cmb_secim2.SelectedIndex = cmb_secim2.Items.Count - 1;

            foreach (ComboBox kutu in partiKutulari) PartileriYukle(kutu);

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim1.SelectedIndexChanged  += Secim_Degisti;
            cmb_secim2.SelectedIndexChanged  += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            // Parti kutusu degisince yalnizca o satirin oranlari yenilenir;
            // digerlerine dokunulmaz, yoksa rejinin elle girdigi deger silinir.
            foreach (ComboBox kutu in partiKutulari) kutu.SelectedIndexChanged += Parti_Degisti;

            yukleniyor = false;

            FarklariYenile();
            VeridenDoldur();
        }

        /// <summary>
        /// Il listesini BIRINCI partinin iki secim arasindaki degisimine gore
        /// renklendirir. Sahnede bes parti var ama liste tek renge boyanabiliyor;
        /// olcu, barlarin en ustundeki parti.
        /// </summary>
        private void FarklariYenile()
        {
            Secim yil1 = SeciliSecim1;
            Secim yil2 = SeciliSecim2;
            Parti parti1 = (partiKutulari.Length > 0) ? partiKutulari[0].SelectedItem as Parti : null;

            if (yil1 == null || yil2 == null || parti1 == null)
            {
                ilSecici1.FarklariTemizle();
                return;
            }

            ilSecici1.FarklariGoster(DataService.Farklar(yil1.Kod, yil2.Kod, parti1.Kod));
        }

        /// <summary> Parti listesi; toplu kalemler ("DIGER") disarida. </summary>
        private static void PartileriYukle(ComboBox combo)
        {
            combo.Items.Clear();

            foreach (Parti parti in DataService.Veri.Partiler)
            {
                if (parti.Toplu) continue;
                combo.Items.Add(parti);
            }
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

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            komutlar.Add(VizYazim.Metin(layer, "sahne_baslik", txt_baslik.Text));

            for (int i = 0; i < SUTUN; i++)
            {
                string on = "parti" + (i + 1);
                Parti parti = partiKutulari[i].SelectedItem as Parti;

                komutlar.Add(VizYazim.Metin(layer, on + "_isim", parti == null ? "" : parti.Ad));

                komutlar.Add(VizYazim.Metin(layer, on + "_yil1_val", txt_yil1.Text));
                komutlar.Add(VizYazim.Metin(layer, on + "_yil2_val", txt_yil2.Text));

                int oran1 = DataService.YuzdeParse(oran1Kutulari[i].Text);
                int oran2 = DataService.YuzdeParse(oran2Kutulari[i].Text);

                komutlar.Add(VizYazim.Metin(layer, on + "_yil1_oran", VizYazim.Bicimle(oran1, basamak)));
                komutlar.Add(VizYazim.Metin(layer, on + "_yil2_oran", VizYazim.Bicimle(oran2, basamak)));

                VizYazim.BarKomutlari(komutlar, layer, on + "_yil1_bar", oran1, barMin, barMax, KEYFRAME_BAR1);
                VizYazim.BarKomutlari(komutlar, layer, on + "_yil2_bar", oran2, barMin, barMax, KEYFRAME_BAR2);

                if (parti == null) continue;

                // Ayni serit uc yere gidiyor: iki bar ve parti adinin rengi.
                Gorsel(komutlar, layer, on + "_yil1_bar_rn", parti);
                Gorsel(komutlar, layer, on + "_yil2_bar_rn", parti);
                Gorsel(komutlar, layer, on + "_isim", parti);
            }

            return komutlar;
        }

        private static void Gorsel(List<string> komutlar, string layer, string container, Parti parti)
        {
            string komut = VizYazim.Gorsel(layer, container, parti.VizBarImage);

            if (komut != null) komutlar.Add(komut);
            else CLog.Error("PARTI BAR SERIDI YOK", parti.Kod + " / " + parti.Ad);
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

        private void VeridenDoldur()
        {
            Secim yil1 = SeciliSecim1;
            Secim yil2 = SeciliSecim2;
            Il il = ilSecici1.SeciliIl;

            if (yil1 == null || yil2 == null || il == null) return;

            yukleniyor = true;

            txt_yil1.Text = yil1.Yil.ToString();
            txt_yil2.Text = yil2.Yil.ToString();
            txt_baslik.Text = "MİLLETVEKİLİ SEÇİMİ / " + il.Ad;

            // Sutunlar 1. secimde en cok oy alan bes partiyle doluyor.
            List<Parti> enIyiler = EnCokOyAlanlar(yil1.Kod, il.Plaka, SUTUN);

            for (int i = 0; i < SUTUN; i++)
            {
                if (i < enIyiler.Count) Sec(partiKutulari[i], enIyiler[i]);
                SatirDoldur(i);
            }

            yukleniyor = false;
        }

        /// <summary>
        /// Bir sutunun iki oranini secili partiden yeniden hesaplar.
        /// Parti kutusu degisince de bu calisir, diger sutunlara dokunmadan.
        /// </summary>
        private void SatirDoldur(int sutun)
        {
            Secim yil1 = SeciliSecim1;
            Secim yil2 = SeciliSecim2;
            Il il = ilSecici1.SeciliIl;
            Parti parti = partiKutulari[sutun].SelectedItem as Parti;

            if (yil1 == null || yil2 == null || il == null || parti == null) return;

            int basamak = Ondalik;

            oran1Kutulari[sutun].Text =
                VizYazim.Bicimle(DataService.Oran(yil1.Kod, il.Plaka, parti.Kod), basamak);

            oran2Kutulari[sutun].Text =
                VizYazim.Bicimle(DataService.Oran(yil2.Kod, il.Plaka, parti.Kod), basamak);
        }

        /// <summary>
        /// Secilen secim ve ilde oyu en yuksek partiler.
        /// Esitlikte kod'a gore siralaniyor; boylece ayni veri hep ayni sirayi veriyor.
        /// </summary>
        private static List<Parti> EnCokOyAlanlar(string secimKod, int plaka, int adet)
        {
            List<Parti> liste = new List<Parti>();

            foreach (Parti parti in DataService.Veri.Partiler)
            {
                if (parti.Toplu) continue;
                liste.Add(parti);
            }

            liste.Sort(delegate (Parti a, Parti b)
            {
                int oa = DataService.Oran(secimKod, plaka, a.Kod);
                int ob = DataService.Oran(secimKod, plaka, b.Kod);

                if (oa != ob) return ob.CompareTo(oa);

                return string.CompareOrdinal(a.Kod, b.Kod);
            });

            if (liste.Count > adet) liste.RemoveRange(adet, liste.Count - adet);

            return liste;
        }

        private static void Sec(ComboBox combo, Parti parti)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                Parti p = combo.Items[i] as Parti;
                if (p != null && p.Kod == parti.Kod) { combo.SelectedIndex = i; return; }
            }
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            for (int i = 0; i < SUTUN; i++)
            {
                oran1Kutulari[i].Text =
                    VizYazim.Bicimle(DataService.YuzdeParse(oran1Kutulari[i].Text), basamak);

                oran2Kutulari[i].Text =
                    VizYazim.Bicimle(DataService.YuzdeParse(oran2Kutulari[i].Text), basamak);
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

            FarklariYenile();
            VeridenDoldur();
        }

        private void Parti_Degisti(object sender, EventArgs e)
        {
            if (yukleniyor) return;

            int sutun = Array.IndexOf(partiKutulari, sender as ComboBox);
            if (sutun < 0) return;

            yukleniyor = true;
            SatirDoldur(sutun);
            yukleniyor = false;

            // Liste birinci partiye gore boyaniyor; digerleri rengi degistirmez.
            if (sutun == 0) FarklariYenile();
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
