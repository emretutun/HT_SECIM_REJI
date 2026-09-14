using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.Core
{
    /// <summary>
    /// Uygulamanin tek renk ve yazi tipi kaynagi.
    ///
    /// Renkler tek tek Designer dosyalarina yazilsaydi 20'den fazla sayfayi
    /// elle guncellemek gerekirdi. Onun yerine tema calisma aninda kontrol
    /// agacina uygulaniyor: yeni bir sayfa eklendiginde hicbir sey yapmaya
    /// gerek yok, Uygula() onu da boyuyor.
    ///
    /// Kendi rengini korumasi gereken kontroller (VER/AL butonlari, durum
    /// serit ve etiketleri, onizleme resmi) Tag'inde TEMA_DISI tasir.
    /// Yazi tipi degismesin isteyen (log konsolu gibi) MONO tasir.
    ///
    /// Palet karanlik rejide ekran parlatmasin diye koyu; vurgu rengi
    /// Haberturk kirmizisi.
    /// </summary>
    public static class Tema
    {
        /// <summary> Tag'inde bu gecen kontrole hic dokunulmaz. </summary>
        public const string DISI = "TEMA_DISI";

        /// <summary> Tag'inde bu gecen kontrolun yazi tipi degistirilmez. </summary>
        public const string MONO = "MONO";

        public static readonly Color Zemin      = Color.FromArgb(234, 245, 234);
        public static readonly Color Panel      = Color.FromArgb(234, 245, 234);
        public static readonly Color Yuzey      = Color.FromArgb(222, 226, 222);
        public static readonly Color Girdi      = Color.White;
        public static readonly Color Cizgi      = Color.FromArgb(186, 194, 186);

        public static readonly Color Metin      = Color.FromArgb( 24,  26,  24);
        public static readonly Color SolukMetin = Color.FromArgb( 92,  98,  92);

        /// <summary> Sahne kartlarinin durdugu sol panel. </summary>
        public static readonly Color KartPaneli      = Color.FromArgb(120, 150, 190);
        public static readonly Color KartPaneliMetin = Color.White;

        /// <summary> Ust serit. </summary>
        public static readonly Color UstSerit   = Color.Silver;

        /// <summary> Haberturk kirmizisi - secim ve vurgu. </summary>
        public static readonly Color Vurgu      = Color.FromArgb(214,   0,  28);

        // --- sahne durumlari ---
        public static readonly Color Bos        = Color.FromArgb( 88,  92, 100);
        public static readonly Color Hazir      = Color.FromArgb(240, 176,   0);
        public static readonly Color Yayinda    = Color.FromArgb(226,  42,  42);

        // --- eylem butonlari ---
        // VER ile AL ayni yesil olmamali: canli yayinda "ekrana ver" ile
        // "ekrandan al" ayni gorunurse yanlis dugmeye basmak an meselesi.
        public static readonly Color Ver        = Color.FromArgb( 26, 160,  74);
        public static readonly Color Al         = Color.FromArgb(176,  48,  44);
        public static readonly Color HazirlaBtn = Color.FromArgb(214, 158,   0);
        public static readonly Color Pasif      = Color.FromArgb(196, 200, 196);
        public static readonly Color PasifMetin = Color.FromArgb(126, 130, 126);

        public const string YaziTipi = "Segoe UI";

        /// <summary> Kontrol agacini bastan asagi boyar. </summary>
        public static void Uygula(Control kok)
        {
            if (kok == null) return;
            if (Disarida(kok)) return;

            Boya(kok);

            foreach (Control cocuk in kok.Controls) Uygula(cocuk);
        }

        private static bool Disarida(Control kontrol)
        {
            string etiket = kontrol.Tag as string;
            return etiket != null && etiket.Contains(DISI);
        }

        private static bool Monospace(Control kontrol)
        {
            string etiket = kontrol.Tag as string;
            return etiket != null && etiket.Contains(MONO);
        }

        private static void Boya(Control kontrol)
        {
            if (!Monospace(kontrol)) kontrol.Font = Yazi(kontrol.Font);

            if (kontrol is Form)      { kontrol.BackColor = Zemin;  kontrol.ForeColor = Metin; return; }
            if (kontrol is PictureBox) return;

            TextBox kutu = kontrol as TextBox;
            if (kutu != null)
            {
                kutu.BackColor   = Girdi;
                kutu.ForeColor   = kutu.ReadOnly ? SolukMetin : Metin;
                kutu.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            ComboBox combo = kontrol as ComboBox;
            if (combo != null)
            {
                combo.BackColor    = Girdi;
                combo.ForeColor    = Metin;
                combo.FlatStyle    = FlatStyle.Flat;
                return;
            }

            ListBox liste = kontrol as ListBox;
            if (liste != null)
            {
                liste.BackColor   = Girdi;
                liste.ForeColor   = SolukMetin;
                liste.BorderStyle = BorderStyle.None;
                return;
            }

            Button dugme = kontrol as Button;
            if (dugme != null)
            {
                Dugme(dugme, Yuzey, Metin);
                return;
            }

            ListView liste2 = kontrol as ListView;
            if (liste2 != null)
            {
                liste2.BackColor   = Girdi;
                liste2.ForeColor   = Metin;
                liste2.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            Label etiket = kontrol as Label;
            if (etiket != null)
            {
                // Etiketler koyu zeminde okunakli olsun diye tam parlaklikta;
                // SolukMetin yalnizca gercekten ikincil yerlerde kullaniliyor.
                etiket.BackColor = Color.Transparent;
                etiket.ForeColor = Metin;
                return;
            }

            // Panel, FlowLayoutPanel, UserControl ve digerleri
            kontrol.BackColor = Panel;
            kontrol.ForeColor = Metin;
        }

        /// <summary> Butonu duz, cercevesiz ve verilen renkte cizer. </summary>
        public static void Dugme(Button dugme, Color arka, Color yazi)
        {
            dugme.FlatStyle = FlatStyle.Flat;
            dugme.BackColor = arka;
            dugme.ForeColor = yazi;
            dugme.UseVisualStyleBackColor = false;
            dugme.FlatAppearance.BorderSize = 0;
            dugme.FlatAppearance.MouseOverBackColor = Acik(arka, 22);
            dugme.FlatAppearance.MouseDownBackColor = Koyu(arka, 22);
        }

        /// <summary> Ayni boyut ve stilde Segoe UI. </summary>
        public static Font Yazi(Font kaynak)
        {
            if (kaynak == null) return new Font(YaziTipi, 9F);
            if (kaynak.FontFamily.Name == YaziTipi) return kaynak;

            return new Font(YaziTipi, kaynak.Size, kaynak.Style);
        }

        public static Font Yazi(float boyut, FontStyle stil)
        {
            return new Font(YaziTipi, boyut, stil);
        }

        private static Color Acik(Color renk, int miktar)
        {
            return Color.FromArgb(
                System.Math.Min(255, renk.R + miktar),
                System.Math.Min(255, renk.G + miktar),
                System.Math.Min(255, renk.B + miktar));
        }

        private static Color Koyu(Color renk, int miktar)
        {
            return Color.FromArgb(
                System.Math.Max(0, renk.R - miktar),
                System.Math.Max(0, renk.G - miktar),
                System.Math.Max(0, renk.B - miktar));
        }
    }
}
