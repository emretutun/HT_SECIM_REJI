using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 15 - CBYILLIK_KARSILASTIRMA_2LI
    ///
    /// Bir cumhurbaskani adayinin IKI SECIMDEKI oyu yan yana iki barda.
    /// Ustte adin fotografi, altta yillar ve oranlar.
    ///
    /// Aday listesi HER IKI secimde de oyu olanlarla sinirli: 2018'de aday
    /// olmayan birini listelemek "oyu sifira dustu" gibi yanlis bir bar uretirdi.
    ///
    /// Barlar Bar plugin'i ile suruluyor ve stage'de value kanali KEYFRAME'LI
    /// (ORAN1 > BAR_VALUE > bar1 > Bar > value). Keyframe'in End degeri
    /// yazilmazsa animasyon bitince bar sahnedeki eski boyuna doner -
    /// sahne 1 ve 2'deki durumun aynisi, BarKomutlari bunu hallediyor.
    /// </summary>
    public partial class Page15_CbYillikKarsilastirma : UserControl, IScenePage
    {
        private bool kuruldu = false;
        private bool yukleniyor = false;

        public SceneInfo Scene { set; get; }

        public Page15_CbYillikKarsilastirma()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            cmb_secim_yeni.Items.Clear();
            cmb_secim_eski.Items.Clear();

            foreach (Secim secim in DataService.Veri.Secimler)
            {
                if (!secim.IsCB) continue;

                cmb_secim_yeni.Items.Add(secim);
                cmb_secim_eski.Items.Add(secim);
            }

            // Veride yeniler once geliyor: ilk kayit soldaki, ikincisi sagdaki.
            if (cmb_secim_yeni.Items.Count > 0) cmb_secim_yeni.SelectedIndex = 0;
            if (cmb_secim_eski.Items.Count > 1) cmb_secim_eski.SelectedIndex = 1;
            else if (cmb_secim_eski.Items.Count > 0) cmb_secim_eski.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            AdaylariYukle();

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim_yeni.SelectedIndexChanged += Secim_Degisti;
            cmb_secim_eski.SelectedIndexChanged += Secim_Degisti;
            cmb_aday.SelectedIndexChanged       += Aday_Degisti;
            cmb_ondalik.SelectedIndexChanged    += Ondalik_Degisti;
            btn_doldur.Click                    += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary>
        /// Listeye giren aday iki sarti birden saglar:
        ///   - her iki secimde de oyu var (2018'de aday olmayan "oyu sifira dustu"
        ///     gibi yanlis bir bar uretirdi)
        ///   - bu sahnenin fotograf klasorunde gorseli var; yoksa ekranda onceki
        ///     adayin fotografi kalirdi
        /// Havuza yeni fotograf eklenince o aday kendiliginden listeye gelir.
        /// </summary>
        private void AdaylariYukle()
        {
            string oncekiKod = "";
            Aday onceki = cmb_aday.SelectedItem as Aday;
            if (onceki != null) oncekiKod = onceki.Kod;

            cmb_aday.Items.Clear();

            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;

            if (yeni == null || eski == null) return;

            foreach (Aday aday in DataService.Veri.Adaylar)
            {
                if (string.IsNullOrEmpty(aday.VizKarsilastirmaImage)) continue;

                if (DataService.OyBul(yeni.Kod, 0, aday.Kod) == null) continue;
                if (DataService.OyBul(eski.Kod, 0, aday.Kod) == null) continue;

                cmb_aday.Items.Add(aday);
            }

            for (int i = 0; i < cmb_aday.Items.Count; i++)
            {
                Aday a = cmb_aday.Items[i] as Aday;
                if (a != null && a.Kod == oncekiKod) { cmb_aday.SelectedIndex = i; return; }
            }

            if (cmb_aday.Items.Count > 0) cmb_aday.SelectedIndex = 0;
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

            Aday aday = SeciliAday;

            komutlar.Add(VizYazim.Metin(layer, "aday_isim", aday == null ? "" : aday.Ad));

            if (aday != null)
            {
                string foto = VizYazim.Gorsel(layer, "aday_resim", aday.VizKarsilastirmaImage);
                if (foto != null) komutlar.Add(foto);
                else CLog.Error("ADAY KARSILASTIRMA FOTOGRAFI YOK", aday.Kod + " / " + aday.Ad);
            }

            komutlar.Add(VizYazim.Metin(layer, "YIL_1", txt_yil1.Text));
            komutlar.Add(VizYazim.Metin(layer, "YIL_2", txt_yil2.Text));

            int oran1 = DataService.YuzdeParse(txt_oran1.Text);
            int oran2 = DataService.YuzdeParse(txt_oran2.Text);

            komutlar.Add(VizYazim.Metin(layer, "oran1", VizYazim.Bicimle(oran1, basamak)));
            komutlar.Add(VizYazim.Metin(layer, "oran2", VizYazim.Bicimle(oran2, basamak)));

            double barMin = CommandRepository.Ayar("BAR_MIN", 0);
            double barMax = CommandRepository.Ayar("BAR_MAX", 100);

            VizYazim.BarKomutlari(komutlar, layer, "bar1", oran1, barMin, barMax);
            VizYazim.BarKomutlari(komutlar, layer, "bar2", oran2, barMin, barMax);

            // Bar gorseli adayin partisinin dikey seridi; partisiz adayda
            // (bagimsiz) adayin kendi bar gorseli kullaniliyor.
            string barGorsel = BarGorseli(aday);

            if (!string.IsNullOrEmpty(barGorsel))
            {
                komutlar.Add(VizYazim.Gorsel(layer, "BAR1rn", barGorsel));
                komutlar.Add(VizYazim.Gorsel(layer, "BAR2rn", barGorsel));
            }
            else if (aday != null)
            {
                CLog.Error("ADAY BAR GORSELI YOK", aday.Kod + " / " + aday.Ad);
            }

            return komutlar;
        }

        private static string BarGorseli(Aday aday)
        {
            if (aday == null) return null;

            Parti parti = DataService.PartiBul(aday.Parti);
            if (parti != null && !string.IsNullOrEmpty(parti.VizBarImage)) return parti.VizBarImage;

            return aday.VizBarImage;
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

        private Secim SeciliYeni { get { return cmb_secim_yeni.SelectedItem as Secim; } }
        private Secim SeciliEski { get { return cmb_secim_eski.SelectedItem as Secim; } }
        private Aday  SeciliAday { get { return cmb_aday.SelectedItem as Aday; } }

        private void VeridenDoldur()
        {
            Secim yeni = SeciliYeni;
            Secim eski = SeciliEski;
            Aday aday = SeciliAday;
            Il il = ilSecici1.SeciliIl;

            if (yeni == null || eski == null || aday == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_yil1.Text = yeni.Yil.ToString();
            txt_yil2.Text = eski.Yil.ToString();

            txt_oran1.Text = VizYazim.Bicimle(DataService.Oran(yeni.Kod, il.Plaka, aday.Kod), basamak);
            txt_oran2.Text = VizYazim.Bicimle(DataService.Oran(eski.Kod, il.Plaka, aday.Kod), basamak);

            // Sahnedeki bicim: "ERDOĞAN (2023 / 2018) - TÜRKİYE GENELİ"
            txt_baslik.Text = aday.Ad + " (" + yeni.Yil + " / " + eski.Yil + ") - " + il.Ad;

            yukleniyor = false;
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

            yukleniyor = true;
            AdaylariYukle();       // secim degisti, ortak aday listesi de degisebilir
            yukleniyor = false;

            VeridenDoldur();
        }

        private void Aday_Degisti(object sender, EventArgs e)
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
