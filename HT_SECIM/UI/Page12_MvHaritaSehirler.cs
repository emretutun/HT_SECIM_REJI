using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    /// <summary>
    /// Sahne 12 - MV_HARITA_SEHIRLER
    ///
    /// Sahne 8'in mekanizmasi + sahne 11'in parti satirlari.
    /// Bir il seciliyor; solda o ildeki 7 parti (ad, oran, vekil), ustte ilin
    /// cikardigi toplam vekil sayisi, sagda 81 illik harita. Harita her ili
    /// O ILDE KAZANAN partinin rengiyle boyuyor, secilen il ayrica vurgulaniyor.
    ///
    /// Secili ili getirmek sahne 8'deki gibi: ILLER_GELIS'in altinda plaka adli
    /// 81 director var, secilen ilinki oynatiliyor. Vurgu katmanindaki
    /// (TR_HARITA_GENEL_GOZ) il container'i aciliyor, digerleri kapaniyor.
    ///
    /// Bu sahnede isim cakismasi yok: ayraclar TG_NOKTA_SABIT / TG_NOKTA_SABIT_KO
    /// diye ayrilmis, rozetler de siraN_kulak diye. Sadece vurgu katmanindaki
    /// il adlari ana haritayla ayni oldugu icin orada yol kullaniliyor.
    ///
    /// Renkler sahne 8 ile ayni aileden - KOYU palet (Parti.HaritaRenk).
    /// Sahne 11 canli paleti kullaniyor (Parti.HaritaRenkGenel).
    /// </summary>
    public partial class Page12_MvHaritaSehirler : UserControl, IScenePage
    {
        private const int ILK_PLAKA = 1;
        private const int SON_PLAKA = 81;

        /// <summary> Sahnede 7 parti slotu var (SIRA_1 ... SIRA_7). </summary>
        private const int SLOT_SAYISI = 7;

        /// <summary> Secilen ilin buyutulmus kopyalarini tutan katman. </summary>
        private const string VURGU_KATMANI = "TR_HARITA_GENEL_GOZ";

        private readonly List<PartiSatiri> satirlar = new List<PartiSatiri>();
        private bool kuruldu = false;
        private bool yukleniyor = false;

        /// <summary> Haritanin en son hangi secimle boyandigi; bosuna 81 komut gitmesin. </summary>
        private string haritaSecimi = null;

        /// <summary> En son vurgulanan il. Sadece onu kapatmak yetiyor, 81'ini degil. </summary>
        private int vurgulananPlaka = 0;

        public SceneInfo Scene { set; get; }

        public Page12_MvHaritaSehirler()
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

            ilSecici1.Yukle("TR_GENELI");
            ilSecici1.OnIlSecildi += IlSecici_OnIlSecildi;

            cmb_secim.SelectedIndexChanged   += Secim_Degisti;
            cmb_ondalik.SelectedIndexChanged += Ondalik_Degisti;
            btn_doldur.Click                 += Doldur_Click;

            yukleniyor = false;

            VeridenDoldur();
        }

        /// <summary> Parti satirlarini (sira + parti + oran + vekil) kodla uretir. </summary>
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
                cmbParti.Size = new Size(180, 22);
                cmbParti.Items.Add("— yok —");
                foreach (Parti parti in DataService.Veri.Partiler) cmbParti.Items.Add(parti);
                cmbParti.SelectedIndex = 0;

                Label lblYuzde = new Label();
                lblYuzde.Text = "%";
                lblYuzde.Font = kalin;
                lblYuzde.Location = new Point(222, y + 3);
                lblYuzde.Size = new Size(16, 18);

                TextBox txtOran = new TextBox();
                txtOran.Font = normal;
                txtOran.Location = new Point(240, y);
                txtOran.Size = new Size(80, 22);
                txtOran.TextAlign = HorizontalAlignment.Center;

                TextBox txtVekil = new TextBox();
                txtVekil.Font = normal;
                txtVekil.Location = new Point(330, y);
                txtVekil.Size = new Size(60, 22);
                txtVekil.TextAlign = HorizontalAlignment.Center;

                Label lblMv = new Label();
                lblMv.Text = "MV";
                lblMv.Font = kalin;
                lblMv.Location = new Point(396, y + 3);
                lblMv.Size = new Size(30, 18);

                pnl_partiler.Controls.Add(lblSira);
                pnl_partiler.Controls.Add(cmbParti);
                pnl_partiler.Controls.Add(lblYuzde);
                pnl_partiler.Controls.Add(txtOran);
                pnl_partiler.Controls.Add(txtVekil);
                pnl_partiler.Controls.Add(lblMv);

                satirlar.Add(new PartiSatiri(cmbParti, txtOran, txtVekil));
            }

            pnl_partiler.ResumeLayout();
        }

        #endregion

        #region IScenePage

        public void SayfaAcildi()
        {
            Kur();
        }

        /// <summary> Sahne bastan yuklendi: harita ve vurgu tasarim halinde geldi. </summary>
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

            // Ilin cikardigi vekil sayisi (600 degil): veri.json'daki YSK kontenjani.
            Il il = ilSecici1.SeciliIl;
            if (il != null)
                komutlar.Add(VizYazim.Metin(layer, "toplam_mv_sayisi", il.VekilKotasi.ToString()));

            // Ayraclarin adlari bu sahnede ayri, isimle cagirmak guvenli.
            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_ass.Text),
                "ASS_PUAN1", "TG_NOKTA_SABIT", "ASS_PUAN2", basamak);

            VizYazim.SayacliOran(komutlar, layer, DataService.YuzdeParse(txt_katilim.Text),
                "KATILIM_ORANI1", "TG_NOKTA_SABIT_KO", "KATILIM_ORANI2", basamak);

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

                komutlar.Add(VizYazim.Metin(layer, "sira" + no + "_oran",
                    VizYazim.Bicimle(DataService.YuzdeParse(satir.Oran.Text), basamak)));

                komutlar.Add(VizYazim.Metin(layer, "parti" + no + "_mv",
                    SafeInt(satir.Vekil.Text).ToString()));

                // Serit sahne 11'in seti: orada YSP'nin kendi mor seridi var,
                // vizSatirImage'daki set YSP'yi ortak koyu kirmizi seride dusuruyordu
                // ve satirin rozetiyle (mor) haritasiyla (mor) tutmuyordu.
                string serit = VizYazim.Gorsel(layer, "sira" + no + "_parti_renk", parti.VizHaritaSeritImage);
                if (serit != null) komutlar.Add(serit);

                // Rozet ("kulak") her slotta ayri adda, yol gerekmiyor.
                string rozet = VizYazim.Gorsel(layer, "sira" + no + "_kulak", parti.VizMvRozetImage);
                if (rozet != null) komutlar.Add(rozet);
            }

            HaritayiBoya(komutlar, layer);
            VurguyuHazirla(komutlar, layer);

            return komutlar;
        }

        /// <summary>
        /// Secilen ili one cikaran vurgu katmani. Il adlari ana haritayla ayni
        /// oldugu icin "$74" ile bu katmana ulasilamiyor, sayisal yol kullaniliyor.
        /// </summary>
        private void VurguyuHazirla(List<string> komutlar, string layer)
        {
            Il il = ilSecici1.SeciliIl;
            if (il == null || il.Plaka < ILK_PLAKA || il.Plaka > SON_PLAKA) return;

            if (vurgulananPlaka == 0)
            {
                // Sahne yeni yuklendi; hangisinin acik oldugunu bilmiyoruz, hepsini kapat.
                for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
                    komutlar.Add(VizYazim.YolAktif(layer,
                        VizYazim.Yol(VURGU_KATMANI, PlakaAdi(plaka)), false));
            }
            else if (vurgulananPlaka != il.Plaka)
            {
                komutlar.Add(VizYazim.YolAktif(layer,
                    VizYazim.Yol(VURGU_KATMANI, PlakaAdi(vurgulananPlaka)), false));
            }

            vurgulananPlaka = il.Plaka;

            string secili = VizYazim.Yol(VURGU_KATMANI, PlakaAdi(il.Plaka));
            komutlar.Add(VizYazim.YolAktif(layer, secili, true));

            Secim secim = SeciliSecim;
            Parti kazanan = (secim == null) ? null : Kazanan(secim.Kod, il.Plaka);

            if (kazanan != null)
            {
                string renk = VizYazim.YolMaterialRengi(layer, secili + "/1", kazanan.HaritaRenk);
                if (renk != null) komutlar.Add(renk);
            }

            // Ilin kendi director'u: ILLER_GELIS'in altinda, adi plaka.
            string sablon = CommandRepository.Build(CommandRepository.GUNCELLE, Scene);

            if (!string.IsNullOrEmpty(sablon))
                komutlar.Add(sablon.Replace("{director}", PlakaAdi(il.Plaka)));
        }

        /// <summary>
        /// Her ili o ilde kazanan partinin rengiyle boyar.
        /// Kazananlar sadece secim degisince degisir, sehir degisince bosuna
        /// 81 komut gonderilmiyor.
        /// </summary>
        private void HaritayiBoya(List<string> komutlar, string layer)
        {
            Secim secim = SeciliSecim;
            if (secim == null) return;

            if (haritaSecimi == secim.Kod) return;
            haritaSecimi = secim.Kod;

            for (int plaka = ILK_PLAKA; plaka <= SON_PLAKA; plaka++)
            {
                Parti kazanan = Kazanan(secim.Kod, plaka);
                if (kazanan == null) continue;

                string komut = VizYazim.MaterialRengi(layer, PlakaAdi(plaka), kazanan.HaritaRenk);
                if (komut != null) komutlar.Add(komut);
            }
        }

        private static string PlakaAdi(int plaka)
        {
            return plaka.ToString("00");
        }

        /// <summary> Bir ilde en cok oyu alan parti. Kayit yoksa null. </summary>
        private static Parti Kazanan(string secimKod, int plaka)
        {
            IlSonucu sonuc = DataService.SonucBul(secimKod, plaka);
            if (sonuc == null) return null;

            Oy enIyi = null;

            foreach (Oy oy in sonuc.Oylar)
                if (enIyi == null || oy.Oran > enIyi.Oran) enIyi = oy;

            return (enIyi == null) ? null : DataService.PartiBul(enIyi.Kod);
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
                PartiSatiri satir = satirlar[i];

                if (i >= sirali.Count)
                {
                    satir.Parti.SelectedIndex = 0;
                    satir.Oran.Text = "";
                    satir.Vekil.Text = "";
                    continue;
                }

                satir.SeciliParti = DataService.PartiBul(sirali[i].Kod);
                satir.Oran.Text = VizYazim.Bicimle(sirali[i].Oran, basamak);
                satir.Vekil.Text = sirali[i].Vekil.ToString();
            }

            yukleniyor = false;
        }

        private void BosaltSatirlar()
        {
            foreach (PartiSatiri satir in satirlar)
            {
                satir.Parti.SelectedIndex = 0;
                satir.Oran.Text = "";
                satir.Vekil.Text = "";
            }
        }

        private void YenidenBicimle()
        {
            int basamak = Ondalik;

            yukleniyor = true;

            txt_ass.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_ass.Text), basamak);
            txt_katilim.Text = VizYazim.Bicimle(DataService.YuzdeParse(txt_katilim.Text), basamak);

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

        private static int SafeInt(string metin)
        {
            int deger;
            int.TryParse(metin, out deger);
            return deger;
        }

        private class PartiSatiri
        {
            public readonly ComboBox Parti;
            public readonly TextBox Oran;
            public readonly TextBox Vekil;

            public PartiSatiri(ComboBox parti, TextBox oran, TextBox vekil)
            {
                Parti = parti;
                Oran = oran;
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
