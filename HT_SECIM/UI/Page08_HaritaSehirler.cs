using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 8 - CB_HARITA_SEHIRLER
    ///
    /// Bir il seciliyor; solda o ildeki 4 cumhurbaskani adayinin sonucu,
    /// sagda Turkiye haritasi. Harita her ili O ILDE KAZANAN adayin rengiyle
    /// boyaniyor (butun ulke, secilen ilden bagimsiz), secilen il ise
    /// kendi animasyonuyla one cikiyor.
    ///
    /// Secili ili getirmek: sahnede ILLER_GELIS director'unun altinda 81 il
    /// director'u var, adlari plaka. Director'e ic ice olsa da adiyla
    /// ulasiliyor, o yuzden ISTANBUL icin:
    ///     {layer}*STAGE*DIRECTOR*34 START
    ///
    /// ILLER_GELIS'in kendisi commands dosyasindaki GUNCELLE_ATLA listesinde;
    /// ust director oynatilsa 81 ilin hepsi ayni anda gelirdi.
    ///
    /// Harita illeri sahnede plaka adiyla: $01 ... $81
    /// Ayni adlar vurgu katmaninda (TR_HARITA_GENEL_GOZ) da var ama Viz isimle
    /// cagirinca agacta once geleni, yani ana haritayi veriyor - bize de o lazim.
    /// </summary>
    public partial class Page08_HaritaSehirler : UserControl, IScenePage
    {
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Sahnede 4 aday slotu var (SIRA_1 ... SIRA_4). </summary>
        private const int SLOT_SAYISI = 4;

        /// <summary> Secilen ilin buyutulmus kopyalarini tutan katman. </summary>
        private const string VURGU_KATMANI = "TR_HARITA_GENEL_GOZ";

        private readonly List<AdaySatiri> satirlar = new List<AdaySatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary>
        /// Haritanin en son hangi secimle boyandigi. Kazananlar sadece secim
        /// degisince degisir; sehir degisince 81 rengi tekrar gondermek bosuna.
        /// Sahne yeniden yuklenirse Sifirla() bunu bosaltiyor.
        /// </summary>
        private string haritaSecimi = null;

        /// <summary> En son vurgulanan il. Sadece onu kapatmak yetiyor, 81'ini degil. </summary>
        private int vurgulananPlaka = 0;

        public SceneInfo Scene { set; get; }

        public Page08_HaritaSehirler()
        {
            InitializeComponent();
        }

        #region Kurulum

        private void Kur()
        {
            if (kuruldu) return;
            kuruldu = true;

            yukleniyor = true;

            // Cumhurbaskani adaylari gosteriliyor; sadece CB veri setleri.
            cmb_secim.Items.Clear();
            foreach (Secim secim in DataService.Veri.Secimler)
                if (secim.IsCB) cmb_secim.Items.Add(secim);

            if (cmb_secim.Items.Count > 0) cmb_secim.SelectedIndex = 0;

            cmb_ondalik.Items.Clear();
            cmb_ondalik.Items.AddRange(new object[] { "0", "1", "2", "3" });
            cmb_ondalik.SelectedIndex = 2;

            SatirlariKur();

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary> Aday satirlarini (sira no + aday + oran) kodla uretir. </summary>
        private void SatirlariKur()
        {
            pnl_adaylar.SuspendLayout();
            pnl_adaylar.Controls.Clear();
            satirlar.Clear();

            Font kalin = new Font("Arial", 10F, FontStyle.Bold);
            Font normal = new Font("Arial", 9.5F, FontStyle.Regular);

            for (int i = 1; i <= SLOT_SAYISI; i++)
            {
                int y = 10 + (i - 1) * 44;

                Label lblSira = new Label();
                lblSira.Text = i + ".";
                lblSira.Font = kalin;
                lblSira.Location = new Point(10, y + 3);
                lblSira.Size = new Size(28, 20);

                ComboBox cmbAday = new ComboBox();
                cmbAday.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbAday.Font = normal;
                cmbAday.Location = new Point(42, y);
                cmbAday.Size = new Size(230, 22);
                cmbAday.Items.Add("— yok —");
                foreach (Aday aday in DataService.Veri.Adaylar) cmbAday.Items.Add(aday);
                cmbAday.SelectedIndex = 0;

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

                pnl_adaylar.Controls.Add(lblSira);
                pnl_adaylar.Controls.Add(cmbAday);
                pnl_adaylar.Controls.Add(lblYuzde);
                pnl_adaylar.Controls.Add(txtOran);

                satirlar.Add(new AdaySatiri(cmbAday, txtOran));
            }

            pnl_adaylar.ResumeLayout();
        }

        #endregion

        #region IScenePage

        public void SayfaAcildi()
        {
            Kur();
        }

        /// <summary>
        /// Sahne motora yeni yuklendi: tasarim halinde geldigi icin ne haritayi
        /// boyamis ne de bir ili vurgulamis sayiliriz. Bir sonraki gonderimde
        /// hepsi bastan yazilsin.
        /// </summary>
        public void Sifirla()
        {
            haritaSecimi = null;
            vurgulananPlaka = 0;
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

            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1_VAL", "TG_NOKTA_SABIT_VAL", "KATILIM_ORANI2_VAL", basamak);

            // --- aday slotlari ---
            for (int i = 0; i < satirlar.Count; i++)
            {
                int no = i + 1;
                AdaySatiri satir = satirlar[i];
                Aday aday = satir.SeciliAday;

                if (aday == null)
                {
                    komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, false));
                    continue;
                }

                komutlar.Add(VizYazim.Aktif(layer, "SIRA_" + no, true));

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_aday_isim", aday.Ad));
                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak)));

                string gorsel = VizYazim.Gorsel(layer, "sira" + no + "_aday_resim", aday.VizSehirImage);
                if (gorsel != null) komutlar.Add(gorsel);
                else CLog.Error("ADAY SEHIR GORSELI YOK", aday.Kod + " / " + aday.Ad);
            }

            // --- harita: her il o ilde kazananin rengiyle ---
            HaritayiBoya(komutlar, layer);

            // --- secili ilin vurgusu + animasyonu ---
            VurguyuHazirla(komutlar, layer);

            return komutlar;
        }

        /// <summary> Sahnedeki container ve director adlari iki haneli plaka. </summary>
        private static string PlakaAdi(int plaka)
        {
            return plaka.ToString("00");
        }

        /// <summary>
        /// Secilen ili one cikaran vurgu katmani.
        ///
        /// TR_HARITA_GENEL_GOZ'un altinda 81 ilin buyutulmus kopyasi var ve
        /// hepsinin gozu kapali duruyor; sadece secilen ilinki aciliyor.
        /// Il adlari ana haritayla ayni oldugu icin "$74" ile bu katmana
        /// ulasilamiyor (Viz agacta once geleni, yani ana haritayi veriyor),
        /// bu yuzden sayisal yol kullaniliyor.
        ///
        /// Sirasiyla: hepsini kapat, secileni ac, icindeki gorseli kazananin
        /// rengine boya, sonunda o ilin director'unu oynat.
        /// </summary>
        private void VurguyuHazirla(List<string> komutlar, string layer)
        {
            Il il = ilSecici1.SeciliIl;
            if (il == null || il.Plaka < ILK_PLAKA || il.Plaka > SON_PLAKA) return;

            if (vurgulananPlaka == 0)
            {
                // Sahne yeni yuklendi; hangisinin acik oldugunu bilmiyoruz, hepsini kapat.
                for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
                    komutlar.Add(VizYazim.YolAktif(layer, VizYazim.Yol(VURGU_KATMANI, PlakaAdi(plaka)), false));
            }
            else if (vurgulananPlaka != il.Plaka)
            {
                // Sadece bir onceki ili kapatmak yeterli.
                komutlar.Add(VizYazim.YolAktif(layer,
                    VizYazim.Yol(VURGU_KATMANI, PlakaAdi(vurgulananPlaka)), false));
            }

            vurgulananPlaka = il.Plaka;

            string secili = VizYazim.Yol(VURGU_KATMANI, PlakaAdi(il.Plaka));

            komutlar.Add(VizYazim.YolAktif(layer, secili, true));

            // Vurgunun icindeki gorsel ust container'in ilk cocugu.
            Secim secim = SeciliSecim;
            Aday kazanan = (secim == null) ? null : Kazanan(secim.Kod, il.Plaka);

            if (kazanan != null)
            {
                string renk = VizYazim.YolMaterialRengi(layer, secili + "/1", kazanan.HaritaRenk);
                if (renk != null) komutlar.Add(renk);
            }

            // Ilin kendi director'u: ILLER_GELIS'in altinda, adi plaka.
            // Ic ice olsa da director'e adiyla ulasiliyor.
            string sablon = CommandRepository.Build(CommandRepository.GUNCELLE, Scene);

            if (!string.IsNullOrEmpty(sablon))
                komutlar.Add(sablon.Replace("{director}", PlakaAdi(il.Plaka)));
        }

        /// <summary>
        /// Her ili o ilde kazananin rengiyle boyar - 81 komut.
        /// Kazananlar sadece secim degisince degisir; ayni secimle sehir
        /// degistirildiginde tekrar gondermeye gerek yok.
        /// </summary>
        private void HaritayiBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (haritaSecimi == secim.Kod) return;
            haritaSecimi = secim.Kod;

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Aday kazanan = Kazanan(secim.Kod, plaka);
                if (kazanan == null) continue;

                string komut = VizYazim.MaterialRengi(layer, PlakaAdi(plaka), kazanan.HaritaRenk);
                if (komut != null) komutlar.Add(komut);
            }
        }

        /// <summary> Bir ilde en cok oyu alan aday. Kayit yoksa null. </summary>
        private static Aday Kazanan(string secimKod, int plaka)
        {
            IlSonucu sonuc = DataService.SonucBul(secimKod, plaka);
            if (sonuc == null) return null;

            Oy enIyi = null;

            foreach (Oy oy in sonuc.Oylar)
                if (enIyi == null || oy.Oran > enIyi.Oran) enIyi = oy;

            return (enIyi == null) ? null : DataService.AdayBul(enIyi.Kod);
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
            Il il = ilSecici1.SeciliIl;

            if (secim == null || il == null) return;

            yukleniyor = true;

            int basamak = Ondalik;

            txt_baslik_ust.Text = secim.Ad;
            txt_baslik_alt.Text = il.Ad;

            IlSonucu sonuc = DataService.SonucBul(secim.Kod, il.Plaka);

            if (sonuc == null)
            {
                txt_ass.Text = "";
                txt_katilim.Text = "";
                BosaltSatirlar();

                yukleniyor = false;
                CLog.Error("SONUC BULUNAMADI", secim.Kod + " / plaka " + il.Plaka);
                return;
            }

            txt_ass.Text = VizYazim.Bicimle(sonuc.AcilanSandik, basamak);
            txt_katilim.Text = VizYazim.Bicimle(sonuc.Katilim, basamak);

            // Slotlar siralamadir: en cok oy alan 1. sirada.
            List<Oy> sirali = new List<Oy>(sonuc.Oylar);
            sirali.Sort(delegate (Oy a, Oy b) { return b.Oran.CompareTo(a.Oran); });

            for (int i = 0; i < satirlar.Count; i++)
            {
                AdaySatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Aday.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    continue;
                }

                satir.SeciliAday = DataService.AdayBul(sirali[i].Kod);
                satir.Oran.Text = VizYazim.Bicimle(sirali[i].Oran, basamak);
            }

            yukleniyor = false;
        }

        private void BosaltSatirlar()
        {
            foreach (AdaySatiri satir in satirlar)
            {
                satir.Aday.SelectedIndex = 0;
                satir.Oran.Text = "";
            }
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

            foreach (AdaySatiri satir in satirlar)
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

        private class AdaySatiri
        {
            public readonly ComboBox Aday;
            public readonly TextBox Oran;

            public AdaySatiri(ComboBox aday, TextBox oran)
            {
                Aday = aday;
                Oran = oran;
            }

            public Aday SeciliAday
            {
                get { return Aday.SelectedItem as Aday; }
                set
                {
                    if (value == null) { Aday.SelectedIndex = 0; return; }

                    for (int i = 0; i < Aday.Items.Count; i++)
                    {
                        Aday liste = Aday.Items[i] as Aday;
                        if (liste != null && liste.Kod == value.Kod) { Aday.SelectedIndex = i; return; }
                    }

                    Aday.SelectedIndex = 0;
                }
            }
        }
    }
}
