using HT_SECIM.Core;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    public delegate void SceneCardEvent(SceneCard card);

    /// <summary> Sahnenin engine uzerindeki durumu </summary>
    public enum SceneState
    {
        /// <summary> Engine'e yuklenmedi </summary>
        Bos = 0,

        /// <summary> Yuklendi ve verisi yazildi, ekranda gorunmuyor (HAZIRLA) </summary>
        Hazir = 1,

        /// <summary> Yayinda (VER) </summary>
        Yayinda = 2
    }

    public partial class SceneCard : UserControl
    {
        // Kartin sol kenarindaki serit ve ON etiketi ayni durumu gosterir:
        // serit uzaktan, ON etiketi yakindan okunur.
        private static readonly Color BOS_COLOR     = Tema.Bos;
        private static readonly Color HAZIR_COLOR   = Tema.Hazir;
        private static readonly Color YAYINDA_COLOR = Tema.Yayinda;

        // Kart, acik renkli panelin uzerinde koyu bir doseme olarak duruyor;
        // bu yuzden kendi renkleri temanin yuzey renklerinden bagimsiz.
        private static readonly Color KART_ZEMIN  = Color.FromArgb(44, 44, 52);
        private static readonly Color KART_SERIT  = Color.FromArgb(34, 34, 40);
        private static readonly Color KART_RESIM  = Color.FromArgb(70, 70, 78);

        private static readonly Color SELECTED_COLOR = Tema.Vurgu;
        private static readonly Color NORMAL_COLOR   = KART_ZEMIN;

        /// <summary> Karta tiklandi -> sag panel bu sahneye gecsin </summary>
        public event SceneCardEvent OnCardClicked;

        /// <summary> Karttaki VER butonu </summary>
        public event SceneCardEvent OnVerClicked;

        /// <summary> Karttaki AL butonu </summary>
        public event SceneCardEvent OnAlClicked;

        /// <summary> Sag tik -> Resim Ata </summary>
        public event SceneCardEvent OnImageAssign;

        /// <summary> Sag tik -> Resmi Kaldir </summary>
        public event SceneCardEvent OnImageRemove;

        private SceneInfo scene;
        private SceneState state = SceneState.Bos;
        private bool isSelected = false;

        public SceneCard()
        {
            InitializeComponent();

            // Kartin her yeri secim icin tiklanabilir (VER/AL butonlari haric).
            this.Click += Card_Click;
            pic_thumb.Click += Card_Click;
            lbl_name.Click += Card_Click;
            lbl_no.Click += Card_Click;
            lbl_on.Click += Card_Click;

            btn_ver.Click += Ver_Click;
            btn_al.Click += Al_Click;

            pic_thumb.BackColor = KART_RESIM;

            // Durum seridi ve ON etiketi ayni yerden boyaniyor; baslangic rengi
            // Designer'da degil burada veriliyor ki tek kaynak olsun.
            State = SceneState.Bos;
            IsSelected = false;
        }

        /// <summary>
        /// Sag tik menusunu (Resim Ata / Kaldir) acar veya tamamen kaldirir.
        /// Resimler bir kere yerlestikten sonra kapatilir, reji yanlislikla silemez.
        /// </summary>
        public bool ImageMenuEnabled
        {
            get { return this.ContextMenuStrip != null; }
            set
            {
                if (value) BuildContextMenu();
                else ClearContextMenu();
            }
        }

        private void ClearContextMenu()
        {
            this.ContextMenuStrip      = null;
            pic_thumb.ContextMenuStrip = null;
            lbl_name.ContextMenuStrip  = null;
            lbl_no.ContextMenuStrip    = null;
        }

        private void BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("Resim Ata...", null, delegate
            {
                if (OnImageAssign != null) OnImageAssign(this);
            });

            menu.Items.Add("Resmi Kaldir", null, delegate
            {
                if (OnImageRemove != null) OnImageRemove(this);
            });

            this.ContextMenuStrip      = menu;
            pic_thumb.ContextMenuStrip = menu;
            lbl_name.ContextMenuStrip  = menu;
            lbl_no.ContextMenuStrip    = menu;
        }

        #region Ozellikler

        public SceneInfo Scene
        {
            get { return scene; }
            set { scene = value; ApplyScene(); }
        }

        /// <summary> Sahnenin engine uzerindeki durumu: BOS / HAZIR / YAYINDA </summary>
        public SceneState State
        {
            get { return state; }
            set
            {
                state = value;

                switch (state)
                {
                    case SceneState.Yayinda:
                        lbl_on.BackColor = YAYINDA_COLOR;
                        lbl_on.ForeColor = Color.White;
                        break;

                    case SceneState.Hazir:
                        lbl_on.BackColor = HAZIR_COLOR;
                        lbl_on.ForeColor = Color.Black;
                        break;

                    default:
                        lbl_on.BackColor = BOS_COLOR;
                        lbl_on.ForeColor = Color.White;
                        break;
                }

                pnl_durum.BackColor = lbl_on.BackColor;
            }
        }

        /// <summary> Bu sahne yayinda mi </summary>
        public bool IsOnAir
        {
            get { return state == SceneState.Yayinda; }
        }

        /// <summary> Sag panelde acik olan sahne mi </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;

                // Secili kart, kontrollerin arasindaki bosluklardan gorunen
                // cerceve rengiyle belli oluyor.
                this.BackColor          = isSelected ? SELECTED_COLOR : NORMAL_COLOR;
                this.lbl_name.BackColor = isSelected ? SELECTED_COLOR : KART_SERIT;
                this.lbl_name.ForeColor = Color.White;
            }
        }

        #endregion

        private void ApplyScene()
        {
            if (scene == null)
            {
                lbl_no.Text = "";
                lbl_name.Text = "";
                return;
            }

            lbl_no.Text = scene.No.ToString();
            lbl_name.Text = " " + scene.Name;

            string tip = scene.Name + Environment.NewLine + scene.FullPath;
            toolTip.SetToolTip(lbl_name, tip);
            toolTip.SetToolTip(pic_thumb, tip);

            LoadThumbnail();
        }

        /// <summary> Resim dosyasi disaridan degistiginde cagrilir. </summary>
        public void RefreshThumbnail()
        {
            if (scene != null) LoadThumbnail();
        }

        private void LoadThumbnail()
        {
            if (pic_thumb.Image != null)
            {
                pic_thumb.Image.Dispose();
                pic_thumb.Image = null;
            }

            string path = scene.ThumbnailPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                // Dosyayi kilitlemeden yukle, boylece operator resmi degistirebilir.
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image kaynak = Image.FromStream(fs))
                {
                    // Ekran goruntuleri tam boy geliyor; bellekte kart boyutunda tutuyoruz.
                    // (21 adet 1920x1080 bitmap yuzlerce MB eder, yayin makinesinde gereksiz yuk.)
                    pic_thumb.Image = Kucult(kaynak, pic_thumb.Width, pic_thumb.Height);
                }
            }
            catch (Exception ex)
            {
                CLog.Error("THUMBNAIL YUKLENEMEDI", path + " | " + ex.Message);
            }
        }

        private static Bitmap Kucult(Image kaynak, int genislik, int yukseklik)
        {
            if (genislik < 1) genislik = 1;
            if (yukseklik < 1) yukseklik = 1;

            Bitmap hedef = new Bitmap(genislik, yukseklik);

            using (Graphics g = Graphics.FromImage(hedef))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(kaynak, new Rectangle(0, 0, genislik, yukseklik));
            }

            return hedef;
        }

        #region Olaylar

        private void Card_Click(object sender, EventArgs e)
        {
            if (OnCardClicked != null) OnCardClicked(this);
        }

        private void Ver_Click(object sender, EventArgs e)
        {
            if (OnVerClicked != null) OnVerClicked(this);
        }

        private void Al_Click(object sender, EventArgs e)
        {
            if (OnAlClicked != null) OnAlClicked(this);
        }

        #endregion
    }
}