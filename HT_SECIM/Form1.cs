using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using HT_SECIM.UI;
using System.Collections.Generic;

namespace HT_SECIM
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Kart resimlerini sag tikla atama/silme ozelligi.
        /// Resimler yerlestikten sonra false yapilip derlenir; reji kartlara mudahale edemez.
        /// </summary>
        private static readonly bool RESIM_DUZENLEME = false;

        /// <summary>
        /// Ham komut satiri ve log konsolu.
        ///
        /// Komut kutusu engine'e ne yazilirsa onu gonderiyor; gelistirirken sart
        /// ama yayin makinesinde rejinin oraya yanlislikla bir sey yazmasi ekrani
        /// dusurebilir. O yuzden ikisi de bu bayragin arkasinda.
        ///
        /// Dosyaya loglama bundan bagimsiz, her zaman calisiyor (LOG klasoru).
        /// </summary>
        private static readonly bool GELISTIRICI_MODU = false;

        private static readonly Color BTN_GREEN    = Tema.Ver;
        private static readonly Color BTN_RED      = Tema.Al;
        private static readonly Color BTN_YELLOW   = Tema.HazirlaBtn;
        private static readonly Color BTN_DISABLED = Tema.Pasif;

        private readonly VizEngine engine = new VizEngine();
        private readonly VizCommander commander;
        private readonly List<SceneCard> cards = new List<SceneCard>();
        private readonly ToolTip headTip = new ToolTip();

        /// <summary> Sahne no -> kontrol sayfasi. Sayfalar bir kere olusturulup saklanir,
        /// boylece operatorun girdigi degerler sahne degistirince kaybolmaz. </summary>
        private readonly Dictionary<int, UserControl> pages = new Dictionary<int, UserControl>();

        private Label lbl_page_bos = null;

        private SceneCard selectedCard = null;
        private SceneCard onAirCard = null;

        public Form1()
        {
            InitializeComponent();
            commander = new VizCommander(engine);
        }

        #region Form olaylari

        private void Form1_Load(object sender, EventArgs e)
        {
            CLog.DebugMode = true;
            CLog.DetailLog = true;
            // Konsol gizliyken satirlari kuyruga alip 250 ms'de bir islemek bos is;
            // dosyaya loglama bundan bagimsiz, her zaman calisiyor.
            if (GELISTIRICI_MODU)
            {
                CLog.OnLogWritten += CLog_OnLogWritten;
                LogKonsolunuKur();
            }
            CLog.Log("UYGULAMA BASLADI", Application.ProductVersion);

            SceneRepository.Load();
            EngineRepository.Load();
            CommandRepository.Load();
            ApiAyarlari.Load();

            VeriyiIlkYukle();

            CLog.Log("CONFIG YUKLENDI",
                SceneRepository.Scenes.Count + " sahne / " + EngineRepository.Engines.Count + " engine");

            BuildSceneCards();

            cmb_engine.Items.Clear();
            foreach (EngineInfo en in EngineRepository.Engines)
                cmb_engine.Items.Add(en);

            if (cmb_engine.Items.Count > 0) cmb_engine.SelectedIndex = 0;

            engine.OnConnected += Engine_OnConnected;
            engine.OnDisconnected += Engine_OnDisconnected;

            btn_head_hazirla.Click += btn_head_hazirla_Click;
            btn_head_ver.Click     += btn_head_ver_Click;
            btn_head_al.Click      += btn_head_al_Click;

            UpdateConnectionUI();

            TemayiUygula();
            AramayiKur();
            KlavyeyiKur();
            VeriKaynaginiKur();

            chk_ac.CheckedChanged += NabizKutulari_Degisti;
            chk_hb.CheckedChanged += NabizKutulari_Degisti;

            // Acilista listedeki ilk sahne secili gelsin.
            if (cards.Count > 0) Card_OnCardClicked(cards[0]);
            else UpdateHead();

            // En sona: NabziKur kutulari isaretleyip gerekiyorsa baglaniyor.
            NabziKur();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            logZamanlayici.Stop();
            NabziDurdur();
            VeriKaynagi.Durdur();

            engine.Disconnect();
            CLog.Log("UYGULAMA KAPANDI");
            CLog.Kapat();
        }

        #endregion

        #region Veri kaynagi

        /// <summary>
        /// Acilista veri nereden gelecek.
        ///
        /// API modunda bile once ONBELLEK dosyasi okunuyor: uygulama API
        /// kapaliyken ya da ag yokken acilsa bile son bilinen veriyle
        /// basliyor, bos ekranla karsilasilmiyor. Gercek veri birkac saniye
        /// sonra arka planda gelip uzerine yaziyor.
        /// </summary>
        private void VeriyiIlkYukle()
        {
            if (ApiAyarlari.ApiKullan && File.Exists(ConfigPaths.CacheFile))
            {
                if (DataService.DosyadanYukle(ConfigPaths.CacheFile))
                {
                    CLog.Log("VERI ONBELLEKTEN", ConfigPaths.CacheFile);
                    return;
                }

                CLog.Error("ONBELLEK OKUNAMADI", "veri.json'a donuluyor");
            }

            DataService.Load();
        }

        private void VeriKaynaginiKur()
        {
            lbl_veri.Click += lbl_veri_Click;

            VeriKaynagi.OnYeniVeri += VeriKaynagi_OnYeniVeri;
            VeriKaynagi.OnDurum    += VeriKaynagi_OnDurum;

            VeriKaynagi.Baslat();
            VeriGostergesiniTazele();
        }

        /// <summary>
        /// Arka planda yeni veri cozuldu. Nesneyi yerine koymak index
        /// sozluklerini bastan kuruyor; bu is UI thread'inde yapilmali,
        /// yoksa tam o anda okuyan bir sahne sayfasi yarim tablo gorur.
        /// </summary>
        private void VeriKaynagi_OnYeniVeri(SecimVerisi veri, int surum, DateTime guncelleme)
        {
            SafeInvoke(delegate
            {
                DataService.Uygula(veri);

                yeniVeriVar = true;
                VeriGostergesiniTazele();
            });
        }

        private void VeriKaynagi_OnDurum(KaynakDurumu durum, string aciklama)
        {
            SafeInvoke(VeriGostergesiniTazele);
        }

        /// <summary> Reji henuz tazelemediyse gosterge yaniyor. </summary>
        private bool yeniVeriVar = false;

        /// <summary>
        /// Ust seritteki veri gostergesi.
        ///
        /// Yeni veri geldiginde sayfalar KENDILIGINDEN tazelenmiyor: reji
        /// oranlari elle duzeltmis olabilir, yayindaki ekran kendiliginden
        /// oynamamali. Gosterge yanar, reji hazir oldugunda VERIDEN DOLDUR'a
        /// basar, sonra VER ile yayina yansitir.
        /// </summary>
        private void VeriGostergesiniTazele()
        {
            if (!ApiAyarlari.ApiKullan)
            {
                lbl_veri.Text = "YEREL VERİ";
                lbl_veri.BackColor = Tema.Bos;
                return;
            }

            switch (VeriKaynagi.Durum)
            {
                case KaynakDurumu.Kopuk:
                    lbl_veri.Text = "API YOK";
                    lbl_veri.BackColor = Tema.Al;
                    break;

                case KaynakDurumu.Bagli:
                    if (yeniVeriVar)
                    {
                        lbl_veri.Text = "YENİ VERİ  " + VeriKaynagi.SonGuncelleme.ToString("HH:mm");
                        lbl_veri.BackColor = Tema.Hazir;
                        lbl_veri.ForeColor = Color.Black;
                        return;
                    }

                    lbl_veri.Text = "VERİ GÜNCEL";
                    lbl_veri.BackColor = Tema.Ver;
                    break;

                default:
                    lbl_veri.Text = "VERİ BEKLENİYOR";
                    lbl_veri.BackColor = Tema.Bos;
                    break;
            }

            lbl_veri.ForeColor = Color.White;
        }

        /// <summary> Gostergeye tiklayinca acik sayfa veriden tazelenir. </summary>
        private void lbl_veri_Click(object sender, EventArgs e)
        {
            if (selectedCard == null) return;

            IScenePage sayfa = GetPage(selectedCard.Scene) as IScenePage;
            if (sayfa == null) return;

            sayfa.SayfaAcildi();

            yeniVeriVar = false;
            VeriGostergesiniTazele();

            CLog.Log("SAYFA VERIDEN TAZELENDI", selectedCard.Scene.Name);
        }

        #endregion

        #region Gorunum

        /// <summary>
        /// Koyu temayi butun kontrol agacina uygular, sonra kendi rengini
        /// korumasi gerekenleri elle boyar.
        /// </summary>
        private void TemayiUygula()
        {
            Tema.Uygula(this);

            pnl_left.BackColor = Tema.KartPaneli;
            pnl_sol.BackColor  = Tema.KartPaneli;
            pnl_page.BackColor = Tema.Zemin;
            pnl_top.BackColor  = Tema.UstSerit;
            pnl_head.BackColor = Tema.Yuzey;

            txt_ara.BackColor = Tema.Girdi;
            txt_ara.ForeColor = Tema.Metin;

            lbl_onair.BackColor      = Tema.Bos;
            lbl_onair.ForeColor      = Color.White;
            lbl_head_onair.BackColor = Color.Black;
            lbl_head_no.BackColor    = Tema.Vurgu;
            lbl_head_no.ForeColor    = Color.White;
            lbl_head_scene.BackColor = Color.Black;
            lbl_head_scene.ForeColor = Tema.Vurgu;

            Tema.Dugme(btn_connect, Tema.Yuzey, Tema.Metin);
            Tema.Dugme(btn_head_hazirla, BTN_YELLOW, Color.Black);
            Tema.Dugme(btn_head_ver, BTN_GREEN, Color.White);
            Tema.Dugme(btn_head_al, BTN_RED, Color.White);

            // Ham komut satiri ve log konsolu yayin makinesinde gorunmez.
            pnl_cmd.Visible = GELISTIRICI_MODU;
            lst_log.Visible = GELISTIRICI_MODU;

            if (!GELISTIRICI_MODU) CLog.Log("GELISTIRICI MODU KAPALI", "komut satiri ve log konsolu gizli");
        }

        #endregion

        #region Arama ve klavye

        private void AramayiKur()
        {
            txt_ara.Text = ARAMA_IPUCU;
            txt_ara.ForeColor = Tema.SolukMetin;

            txt_ara.GotFocus  += delegate { IpucunuKaldir(); };
            txt_ara.LostFocus += delegate { IpucunuKoy(); };
            txt_ara.TextChanged += delegate { KartlariSuz(); };
        }

        private const string ARAMA_IPUCU = "Sahne ara  (ad veya numara)";

        private void IpucunuKaldir()
        {
            if (txt_ara.Text != ARAMA_IPUCU) return;

            txt_ara.Text = "";
            txt_ara.ForeColor = Tema.Metin;
        }

        private void IpucunuKoy()
        {
            if (txt_ara.Text.Length > 0) return;

            txt_ara.Text = ARAMA_IPUCU;
            txt_ara.ForeColor = Tema.SolukMetin;
        }

        /// <summary> Arama kutusundaki gercek metin (ipucu yaziysa bos). </summary>
        private string AramaMetni
        {
            get
            {
                string metin = txt_ara.Text.Trim();
                return (metin == ARAMA_IPUCU) ? "" : metin;
            }
        }

        /// <summary>
        /// Arama kutusuna gore kartlari gizler. Bir grubun altinda gorunen
        /// kart kalmadiysa grup basligi da gizlenir.
        /// </summary>
        private void KartlariSuz()
        {
            string arama = AramaMetni.ToUpperInvariant();

            pnl_left.SuspendLayout();

            foreach (SceneCard kart in cards)
            {
                bool uyuyor = arama.Length == 0 || Uyuyor(kart.Scene, arama);
                kart.Visible = uyuyor;
            }

            foreach (KeyValuePair<string, Label> grup in grupBasliklari)
            {
                bool doluMu = false;

                foreach (SceneCard kart in cards)
                {
                    if (kart.Scene.Grup != grup.Key) continue;
                    if (!kart.Visible) continue;

                    doluMu = true;
                    break;
                }

                grup.Value.Visible = doluMu;
            }

            pnl_left.ResumeLayout();
        }

        private static bool Uyuyor(SceneInfo sahne, string aramaBuyuk)
        {
            if (sahne == null) return false;

            if (sahne.No.ToString() == aramaBuyuk) return true;
            if (sahne.Name.ToUpperInvariant().IndexOf(aramaBuyuk, StringComparison.Ordinal) >= 0) return true;
            if (sahne.SceneName.ToUpperInvariant().IndexOf(aramaBuyuk, StringComparison.Ordinal) >= 0) return true;

            return false;
        }

        /// <summary>
        /// Reji fareyle calismaz: F1 HAZIRLA, F2 VER, F3 AL, Ctrl+F arama,
        /// Esc aramayi temizler. Arama kutusunda Enter ilk karti secer.
        /// </summary>
        private void KlavyeyiKur()
        {
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            txt_ara.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Enter) return;

                e.SuppressKeyPress = true;

                foreach (SceneCard kart in cards)
                {
                    if (!kart.Visible) continue;

                    Card_OnCardClicked(kart);
                    break;
                }
            };
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                txt_ara.Focus();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                if (AramaMetni.Length == 0) return;

                txt_ara.Text = "";
                KartlariSuz();
                e.SuppressKeyPress = true;
                return;
            }

            if (selectedCard == null) return;

            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (btn_head_hazirla.Enabled) btn_head_hazirla_Click(sender, EventArgs.Empty);
                    break;

                case Keys.F2:
                    if (btn_head_ver.Enabled) btn_head_ver_Click(sender, EventArgs.Empty);
                    break;

                case Keys.F3:
                    if (btn_head_al.Enabled) btn_head_al_Click(sender, EventArgs.Empty);
                    break;

                default:
                    return;
            }

            e.SuppressKeyPress = true;
        }

        #endregion

        #region Sahne kartlari

        /// <summary> Grup adi -> listedeki basligi. Aramada bosalan grup gizleniyor. </summary>
        private readonly Dictionary<string, Label> grupBasliklari = new Dictionary<string, Label>();

        private void BuildSceneCards()
        {
            pnl_left.SuspendLayout();
            pnl_left.Controls.Clear();
            cards.Clear();
            grupBasliklari.Clear();

            string acikGrup = null;

            foreach (SceneInfo s in SceneRepository.Scenes)
            {
                // scenes dosyasindaki GRUP satiri degistiginde yeni baslik.
                if (!string.IsNullOrEmpty(s.Grup) && s.Grup != acikGrup)
                {
                    acikGrup = s.Grup;
                    GrupBasligiEkle(acikGrup);
                }

                SceneCard card = new SceneCard();
                card.Scene = s;

                card.OnCardClicked += Card_OnCardClicked;
                card.OnVerClicked  += Card_OnVerClicked;
                card.OnAlClicked   += Card_OnAlClicked;
                if (RESIM_DUZENLEME)
                {
                    card.ImageMenuEnabled = true;
                    card.OnImageAssign += Card_OnImageAssign;
                    card.OnImageRemove += Card_OnImageRemove;
                }

                cards.Add(card);
                pnl_left.Controls.Add(card);
            }

            pnl_left.ResumeLayout();
            CLog.Log("SAHNE KARTLARI OLUSTURULDU",
                cards.Count + " kart / " + grupBasliklari.Count + " grup");
        }

        /// <summary>
        /// Kart akisina tam satir kaplayan bir baslik koyar.
        /// FlowLayoutPanel satiri kendiliginden bolmez; basligin onunde ve
        /// arkasinda satir sonu isaretleniyor ki tek basina bir satirda dursun.
        /// </summary>
        private void GrupBasligiEkle(string ad)
        {
            Label baslik = new Label();

            baslik.Text      = ad;
            baslik.AutoSize  = false;
            baslik.Font      = Tema.Yazi(11F, FontStyle.Bold);
            baslik.ForeColor = Tema.KartPaneliMetin;
            baslik.BackColor = Color.Transparent;
            baslik.TextAlign = ContentAlignment.MiddleLeft;
            baslik.Padding   = new Padding(10, 0, 0, 0);
            baslik.Margin    = new Padding(3, 12, 3, 4);
            baslik.Size      = new Size(pnl_left.ClientSize.Width - 24, 26);
            baslik.Tag       = Tema.DISI;

            pnl_left.Controls.Add(baslik);
            pnl_left.SetFlowBreak(baslik, true);

            grupBasliklari[ad] = baslik;
        }

        /// <summary> Karta tiklandi -> sag panel bu sahneye gecer </summary>
        private void Card_OnCardClicked(SceneCard card)
        {
            foreach (SceneCard c in cards) c.IsSelected = (c == card);
            selectedCard = card;

            UpdateHead();
            ShowPage(card.Scene);

            CLog.Log("SAHNE SECILDI", card.Scene.No + " - " + card.Scene.Name);
        }

        /// <summary> HAZIRLA -> sahneyi engine'e yukle + verisini yaz (komutlar sonraki adimlarda) </summary>
        private void PrepareCard(SceneCard card)
        {
            if (card == null || card.State == SceneState.Yayinda) return;

            CLog.Log("HAZIRLA", card.Scene.Name, card.Scene.FullPath);

            // MAIN_LAYER'a tek sahne yuklenebiliyor: yeni sahne oncekini motordan dusuruyor.
            // Diger kartlar "hazir" gorunmeye devam ederse, olmayan container'lara komut
            // gonderilip "failed to process command" hatalari aliniyor.
            DigerKartlariBosalt(card);

            commander.Hazirla(card.Scene);

            // Sahne yuklendikten sonra stage agacini oku: animasyon kanallarinin id'leri
            // her yuklemede degisebiliyor, keyframe hedeflerini yazabilmek icin lazim.
            commander.StageOku();

            // Sahne tasarim halinde geldi: sayfanin "bunu zaten yazmistim" kayitlari
            // artik gecersiz, hepsini bastan yazmasi gerekiyor.
            IScenePage sayfa = GetPage(card.Scene) as IScenePage;
            if (sayfa != null) sayfa.Sifirla();

            VeriGonder(card.Scene);

            card.State = SceneState.Hazir;
        }

        /// <summary> Karttaki VER -> sahne yayina alinir (komutlar sonraki adimlarda) </summary>
        private void Card_OnVerClicked(SceneCard card)
        {
            if (card.State == SceneState.Bos)
            {
                // Sahne motorda yok: yukle, stage'i oku, verileri yaz, sonra goster + IN oynat.
                PrepareCard(card);

                CLog.Log("VER", card.Scene.Name, card.Scene.FullPath);
                commander.Ver(card.Scene);
            }
            else if (card.State == SceneState.Hazir)
            {
                // Sahne yuklu ama ekranda degil. Operator HAZIRLA'dan sonra il degistirmis
                // olabilir; sahne gizli oldugu icin veriyi rahatca tazeleyip aciyoruz.
                VeriGonder(card.Scene);

                CLog.Log("VER", card.Scene.Name, card.Scene.FullPath);
                commander.Ver(card.Scene);
            }
            else
            {
                // Sahne ZATEN YAYINDA. IN'i bastan oynatmak ekrani goz kirptiriyor,
                // onun yerine alt director'leri oynatip sadece degerleri tazeliyoruz.
                // Goz zaten acik, ACTIVE komutuna gerek yok.
                // Veri + tetikleme TEK pakette gidiyor; ayri ayri gonderilince
                // yeni degerler animasyondan once bir an ekranda goruluyor.
                CLog.Log("VER (guncelleme)", card.Scene.Name, card.Scene.FullPath);
                commander.Guncelle(card.Scene, VeriKomutlari(card.Scene));
            }

            SetOnAir(card);
        }

        /// <summary> Karttaki AL -> sahne yayindan cikarilir, bellekte hazir kalir </summary>
        private void Card_OnAlClicked(SceneCard card)
        {
            // Yayinda olmayan bir sahnenin AL'i digerini dusurmesin.
            if (card.State != SceneState.Yayinda) return;

            CLog.Log("AL", card.Scene.Name, card.Scene.FullPath);
            commander.Al(card.Scene);

            // AL sahneyi motordan dusuruyor; kart artik hazir degil, bostan basliyor.
            SetOnAir(null);
            card.State = SceneState.Bos;
        }

        /// <summary>
        /// Motorda tek sahne durabildigi icin, bir sahne yuklenince digerlerinin
        /// durumu BOS'a doner. Yayindaki baska bir sahne varsa yayin gostergesi de duser.
        /// </summary>
        private void DigerKartlariBosalt(SceneCard yuklenen)
        {
            foreach (SceneCard c in cards)
            {
                if (c == yuklenen) continue;
                if (c.State == SceneState.Bos) continue;

                CLog.Detail("KART BOSALTILDI", c.Scene.No + " - " + c.Scene.Name +
                    " (motora " + yuklenen.Scene.Name + " yuklendi)");

                c.State = SceneState.Bos;
            }

            if (onAirCard != null && onAirCard != yuklenen)
            {
                onAirCard = null;
                lbl_onair.BackColor = Tema.Bos;
            }
        }

        private void SetOnAir(SceneCard card)
        {
            // Yayindaki eski sahne bellekte hazir kalir, bostan baslamaz.
            foreach (SceneCard c in cards)
                if (c != card && c.State == SceneState.Yayinda) c.State = SceneState.Hazir;

            if (card != null) card.State = SceneState.Yayinda;

            onAirCard = card;
            lbl_onair.BackColor = (card == null) ? Tema.Bos : Tema.Yayinda;
            UpdateHead();
        }

        /// <summary> Sag tik -> Resim Ata: secilen dosyayi thumbs klasorune dogru adla kopyalar. </summary>
        private void Card_OnImageAssign(SceneCard card)
        {
            if (card == null || card.Scene == null) return;

            if (string.IsNullOrEmpty(card.Scene.Thumbnail))
            {
                MessageBox.Show("Bu sahne icin scenes dosyasinda resim adi tanimli degil.",
                    "Resim Ata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = card.Scene.No + " - " + card.Scene.Name + "  icin resim sec";
                dlg.Filter = "Resim dosyalari|*.png;*.jpg;*.jpeg;*.bmp|Tum dosyalar|*.*";

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    if (!Directory.Exists(ConfigPaths.ThumbsFolder))
                        Directory.CreateDirectory(ConfigPaths.ThumbsFolder);

                    // Kart resmi dosyayi kilitlemedigi icin uzerine yazabiliyoruz.
                    File.Copy(dlg.FileName, card.Scene.ThumbnailPath, true);

                    card.RefreshThumbnail();
                    CLog.Log("RESIM ATANDI", card.Scene.Name + " <- " + dlg.FileName);
                }
                catch (Exception ex)
                {
                    CLog.Error("RESIM ATANAMADI", card.Scene.Name + " | " + ex.Message);
                    MessageBox.Show("Resim kopyalanamadi: " + ex.Message,
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Card_OnImageRemove(SceneCard card)
        {
            if (card == null || card.Scene == null) return;

            try
            {
                if (File.Exists(card.Scene.ThumbnailPath))
                    File.Delete(card.Scene.ThumbnailPath);

                card.RefreshThumbnail();
                CLog.Log("RESIM KALDIRILDI", card.Scene.Name);
            }
            catch (Exception ex)
            {
                CLog.Error("RESIM SILINEMEDI", card.Scene.Name + " | " + ex.Message);
            }
        }

        #endregion

        #region Sahne kontrol sayfalari

        /// <summary> Sahnenin sayfasini dondurur; ilk istekte olusturur. Sayfasi yoksa null. </summary>
        private UserControl GetPage(SceneInfo scene)
        {
            if (scene == null) return null;

            UserControl page;
            if (pages.TryGetValue(scene.No, out page)) return page;

            page = ScenePageFactory.Create(scene);

            if (page != null)
            {
                IScenePage sp = page as IScenePage;
                if (sp != null) sp.Scene = scene;

                page.Dock = DockStyle.Fill;

                // Sayfalarin Designer'inda acik renkler var; tema burada uygulaniyor,
                // boylece yeni bir sayfa eklendiginde renk icin hicbir sey yapmak gerekmiyor.
                Tema.Uygula(page);

                pnl_page.Controls.Add(page);
            }

            pages[scene.No] = page;   // null da saklanir, her seferinde yeniden denenmesin
            return page;
        }

        /// <summary> Sahnenin sayfasinin urettigi komutlar. Sayfasi yoksa bos liste. </summary>
        private List<string> VeriKomutlari(SceneInfo scene)
        {
            IScenePage page = GetPage(scene) as IScenePage;
            if (page == null) return new List<string>();

            List<string> komutlar = page.VeriKomutlari();
            return komutlar ?? new List<string>();
        }

        /// <summary> Sahnenin sayfasindaki degerleri Viz'e yazar. </summary>
        private void VeriGonder(SceneInfo scene)
        {
            List<string> komutlar = VeriKomutlari(scene);
            if (komutlar.Count == 0) return;

            foreach (string komut in komutlar)
                commander.GonderHam(komut);

            CLog.Log("VERI GONDERILDI", komutlar.Count + " komut", scene.FullPath);
        }

        private void ShowPage(SceneInfo scene)
        {
            pnl_page.SuspendLayout();

            UserControl page = GetPage(scene);

            foreach (Control c in pnl_page.Controls)
                c.Visible = (c == page);

            if (page == null)
            {
                EnsurePlaceholder();

                lbl_page_bos.Text = (scene == null)
                    ? ""
                    : scene.No + "  -  " + scene.Name + Environment.NewLine + Environment.NewLine +
                      "Bu sahne icin kontrol paneli henuz hazirlanmadi." + Environment.NewLine +
                      scene.FullPath;

                lbl_page_bos.Visible = true;
                lbl_page_bos.BringToFront();
            }
            else
            {
                if (lbl_page_bos != null) lbl_page_bos.Visible = false;

                page.BringToFront();

                IScenePage sp = page as IScenePage;
                if (sp != null) sp.SayfaAcildi();
            }

            pnl_page.ResumeLayout();
        }

        private void EnsurePlaceholder()
        {
            if (lbl_page_bos != null) return;

            lbl_page_bos = new Label();
            lbl_page_bos.Dock = DockStyle.Fill;
            lbl_page_bos.TextAlign = ContentAlignment.MiddleCenter;
            lbl_page_bos.Font = Tema.Yazi(11F, FontStyle.Regular);
            lbl_page_bos.ForeColor = Tema.SolukMetin;
            lbl_page_bos.BackColor = Color.Transparent;

            pnl_page.Controls.Add(lbl_page_bos);
        }

        #endregion

        #region Sag panel ust serit

        /// <summary> Ust seridi secili sahneye gore gunceller. </summary>
        private void UpdateHead()
        {
            if (selectedCard == null || selectedCard.Scene == null)
            {
                lbl_head_no.Text = "-";
                lbl_head_scene.Text = " -";
                lbl_head_onair.ForeColor = Tema.Bos;

                SetHeadButton(btn_head_hazirla, false, BTN_YELLOW, Color.Black);
                SetHeadButton(btn_head_ver, false, BTN_GREEN, Color.White);
                SetHeadButton(btn_head_al, false, BTN_RED, Color.White);
                return;
            }

            SceneInfo s = selectedCard.Scene;

            lbl_head_no.Text    = s.No.ToString();
            lbl_head_scene.Text = " " + s.SceneName;
            headTip.SetToolTip(lbl_head_scene, s.FullPath);

            bool onAir = (selectedCard.State == SceneState.Yayinda);

            lbl_head_onair.ForeColor = onAir ? Tema.Yayinda : Tema.Bos;

            SetHeadButton(btn_head_hazirla, !onAir, BTN_YELLOW, Color.Black);  // yayindayken yeniden yukleme yok
            SetHeadButton(btn_head_ver, true, BTN_GREEN, Color.White);
            SetHeadButton(btn_head_al, onAir, BTN_RED, Color.White);           // sadece yayindakini dusurebilir
        }

        /// <summary> Devre disi butonun rengi de sonsun, sadece yazisi degil. </summary>
        private static void SetHeadButton(Button button, bool enabled, Color activeColor, Color activeText)
        {
            button.Enabled = enabled;

            Tema.Dugme(button,
                enabled ? activeColor : BTN_DISABLED,
                enabled ? activeText  : Tema.PasifMetin);
        }

        private void btn_head_hazirla_Click(object sender, EventArgs e)
        {
            if (selectedCard == null) return;
            PrepareCard(selectedCard);
            UpdateHead();
        }

        private void btn_head_ver_Click(object sender, EventArgs e)
        {
            if (selectedCard == null) return;
            Card_OnVerClicked(selectedCard);
        }

        private void btn_head_al_Click(object sender, EventArgs e)
        {
            if (selectedCard == null) return;
            Card_OnAlClicked(selectedCard);
            UpdateHead();
        }

        #endregion

        #region Baglanti

        private void cmb_engine_SelectedIndexChanged(object sender, EventArgs e)
        {
            EngineInfo info = cmb_engine.SelectedItem as EngineInfo;
            if (info != null) txt_ip.Text = info.IP;

            // Nabiz thread'i ComboBox'a erisemez; secim alanda tutuluyor.
            seciliEngine = info;
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (engine.isConnected)
            {
                CLog.Log("OPERATOR", "DISCONNECT");
                engine.Disconnect();
            }
            else
            {
                EngineInfo info = cmb_engine.SelectedItem as EngineInfo;
                if (info == null)
                {
                    MessageBox.Show("Once listeden bir engine sec.");
                    return;
                }

                CLog.Log("OPERATOR", "CONNECT " + info.IP + ":" + info.Port);
                engine.Connect(info);
            }

            UpdateConnectionUI();
        }

        /// <summary>
        /// Baglanti kurulunca: istenmisse secili sahneyi motora yukler.
        ///
        /// MAIN_LAYER'a ayni anda TEK sahne yuklenebildigi icin "hangi
        /// sahne" sorusunun tek makul cevabi operatorun sectigi karttir.
        /// Sahne yalnizca HAZIRLANIR, yayina verilmez - ekranda bir sey
        /// gorunmez, kart yesile doner.
        ///
        /// Zaten yayindaki bir kart varsa dokunulmuyor: baglanti yayin
        /// sirasinda tazelenirse ekrandaki grafik sifirlanmasin.
        /// </summary>
        private void Engine_OnConnected(VizEngine en)
        {
            SafeInvoke(delegate
            {
                UpdateConnectionUI();

                if (!CommandRepository.BaglanincaHazirla) return;
                if (selectedCard == null) return;
                if (onAirCard != null) return;
                if (selectedCard.State != SceneState.Bos) return;

                CLog.Log("BAGLANINCA HAZIRLA", selectedCard.Scene.Name);
                PrepareCard(selectedCard);
            });
        }

        private void Engine_OnDisconnected(VizEngine en)
        {
            SafeInvoke(UpdateConnectionUI);
        }

        #region Otomatik baglanti ve nabiz

        /// <summary>
        /// Nabiz zamanlayicisi ARKA PLANDA calisiyor.
        ///
        /// Yoklama komutu cevap beklediginden UI thread'inde olsaydi
        /// baglanti koptugunda arayuz zaman asimi boyunca donardi.
        /// </summary>
        private System.Threading.Timer nabizZamanlayici;
        private volatile bool nabizCalisiyor;
        private int nabizMesgul;

        /// <summary> Arka plandaki nabiz thread'inin okudugu secili engine. </summary>
        private volatile EngineInfo seciliEngine;

        private void NabziKur()
        {
            chk_ac.Checked = EngineRepository.OtomatikBaglan;
            chk_hb.Checked = EngineRepository.Nabiz;

            // Kutu zaten o degerdeyse CheckedChanged tetiklenmez; alanlar
            // burada da doldurulyor ki olay sirasina bagli kalmasin.
            nabizAcOtomatik = chk_ac.Checked;
            nabizHbAcik     = chk_hb.Checked;

            seciliEngine = cmb_engine.SelectedItem as EngineInfo;

            nabizCalisiyor = true;

            int ms = Math.Max(3, EngineRepository.NabizAralik) * 1000;
            nabizZamanlayici = new System.Threading.Timer(NabizTik, null, ms, ms);

            CLog.Log("NABIZ KURULDU", EngineRepository.NabizAralik + " sn");

            // AC isaretliyse ilk tiki beklemeden baglaniliyor.
            if (chk_ac.Checked) OtomatikBaglan();
        }

        private void NabziDurdur()
        {
            nabizCalisiyor = false;
            if (nabizZamanlayici != null) nabizZamanlayici.Dispose();
        }

        /// <summary>
        /// Her tik iki is yapiyor:
        ///
        ///   AC acik ve bagli degilsek  -> baglanmayi dener.
        ///   HB acik ve bagliysak       -> hatti yoklar.
        ///
        /// Yoklama neden gerekli: TCP baglantisi karsi taraf sessizce
        /// olduğunde "bagli" gorunmeye devam edebiliyor. Soket acik ama
        /// motor cevap vermiyorsa uygulama komut gonderdigini sanip
        /// ekrani dondurur. Kucuk bir GET bunu ortaya cikariyor.
        /// </summary>
        private void NabizTik(object durum)
        {
            if (!nabizCalisiyor) return;

            // Onceki tik hala surerse bu tik atlaniyor.
            if (System.Threading.Interlocked.Exchange(ref nabizMesgul, 1) == 1) return;

            try
            {
                if (!engine.isConnected)
                {
                    if (nabizAcOtomatik) OtomatikBaglan();
                    return;
                }

                if (!nabizHbAcik) return;

                // Yan etkisi olmayan, birkac baytlik bir sorgu.
                if (engine.SendAndWait("VERSION GET", 4000) != null) return;

                CLog.Error("NABIZ ALINAMADI", "engine cevap vermiyor, baglanti kapatiliyor");

                engine.Disconnect();
                if (nabizAcOtomatik) OtomatikBaglan();
            }
            catch (Exception ex)
            {
                CLog.Error("NABIZ HATASI", ex.Message);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref nabizMesgul, 0);
            }
        }

        /// <summary>
        /// Kutu durumlari arka plan thread'inden okunacagi icin alanda
        /// tutuluyor; Control.Checked'e baska thread'den erisilemez.
        /// </summary>
        private volatile bool nabizAcOtomatik;
        private volatile bool nabizHbAcik;

        private void NabizKutulari_Degisti(object sender, EventArgs e)
        {
            nabizAcOtomatik = chk_ac.Checked;
            nabizHbAcik     = chk_hb.Checked;

            CLog.Log("BAGLANTI SECENEGI",
                "AC " + (nabizAcOtomatik ? "açık" : "kapalı") +
                " / HB " + (nabizHbAcik ? "açık" : "kapalı"));
        }

        /// <summary>
        /// Listedeki secili engine'e baglanmayi dener.
        /// VizClient BeginConnect kullandigi icin bloklamiyor.
        /// </summary>
        private void OtomatikBaglan()
        {
            EngineInfo secili = seciliEngine;
            if (secili == null) return;

            CLog.Log("OTOMATIK BAGLANTI", secili.IP + ":" + secili.Port);
            engine.Connect(secili);
        }

        #endregion

        private void UpdateConnectionUI()
        {
            bool connected = engine.isConnected;

            btn_connect.Text = connected ? "DISCONNECT" : "CONNECT";

            // Bagli degilken hicbir komut gitmiyor; gri yerine kirmizi, gozden kacmasin.
            lbl_status.Text = connected ? "BAĞLI" : "BAĞLI DEĞİL";
            lbl_status.BackColor = connected ? Tema.Ver : Tema.Al;
            lbl_status.ForeColor = Color.White;

            cmb_engine.Enabled = !connected;
        }

        #endregion

        #region Komut satiri (gecici test)

        private void btn_send_Click(object sender, EventArgs e)
        {
            string command = txt_command.Text.Trim();
            if (command.Length == 0) return;

            engine.SendWithResponse(command);
            txt_command.SelectAll();
            txt_command.Focus();
        }

        private void txt_command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            btn_send_Click(sender, EventArgs.Empty);
        }

        #endregion

        #region Log konsolu

        /// <summary> Konsola yazilmayi bekleyen satirlar. </summary>
        private readonly List<string> logKuyrugu = new List<string>();

        private readonly Timer logZamanlayici = new Timer();

        /// <summary>
        /// Log satirlari dogrudan ListBox'a eklenmiyor, kuyruga giriyor.
        /// Tek pakette 200 komut gonderdigimizde her satir icin ayri ekleme +
        /// kaydirma yapmak ekrani 200 kez ciziyordu ve bu komutlar gonderilmeden
        /// once oldugu icin goruntuyu geciktiriyordu.
        /// </summary>
        private void CLog_OnLogWritten(string line)
        {
            lock (logKuyrugu) logKuyrugu.Add(line);
        }

        private void LogKonsolunuKur()
        {
            logZamanlayici.Interval = 250;
            logZamanlayici.Tick += LogKonsolunuTazele;
            logZamanlayici.Start();
        }

        private void LogKonsolunuTazele(object sender, EventArgs e)
        {
            string[] yeni;

            lock (logKuyrugu)
            {
                if (logKuyrugu.Count == 0) return;

                yeni = logKuyrugu.ToArray();
                logKuyrugu.Clear();
            }

            lst_log.BeginUpdate();

            if (lst_log.Items.Count + yeni.Length > 500) lst_log.Items.Clear();

            lst_log.Items.AddRange(yeni);
            lst_log.TopIndex = lst_log.Items.Count - 1;

            lst_log.EndUpdate();
        }

        /// <summary>
        /// Soket thread'inden gelen olaylari UI thread'ine tasir.
        /// (Bu olmadan "cross-thread operation" hatasi alinir.)
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(action); } catch { }
            }
            else
            {
                action();
            }
        }

        #endregion


    }
}