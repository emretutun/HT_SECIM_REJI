using HT_SECIM.Core;
using HT_SECIM.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HT_SECIM.UI
{
    public delegate void IlSecildiEvent(Il il);

    /// <summary>
    /// Kategori butonlari + il listesi. Bircok sahne sayfasinda tekrar kullanilir.
    /// Kategoriler veri.json'daki "gruplar" bolumunden uretilir.
    /// </summary>
    public partial class IlSecici : UserControl
    {
        // Grup butonlari calisma aninda uretiliyor; tema agaci gezerken henuz
        // ortada olmadiklari icin renklerini buradan aliyorlar.
        private static readonly Color BTN_NORMAL = Tema.Yuzey;
        private static readonly Color BTN_AKTIF  = Tema.Vurgu;

        /// <summary> Listeden bir il secildi. </summary>
        public event IlSecildiEvent OnIlSecildi;

        private readonly List<Button> grupButonlari = new List<Button>();
        private string aktifGrupKod = "";
        private Il seciliIl = null;

        /// <summary>
        /// plaka -> iki secim arasindaki fark (oran x100). Karsilastirma
        /// sayfalari dolduruyor; digerlerinde null kalir ve liste sade gorunur.
        /// </summary>
        private Dictionary<int, int> farklar = null;

        /// <summary> Renklerin oranlandigi en buyuk fark. </summary>
        private int farkOlcek = 0;

        // ------------------------------------------------------------ siralama

        /// <summary> Grubun kendi sirasi. Ucuncu tiklamada buna donuluyor. </summary>
        private readonly List<ListViewItem> varsayilanSira = new List<ListViewItem>();

        private const int SUTUN_PLAKA = 0, SUTUN_AD = 1, SUTUN_FARK = 2;

        /// <summary> Hangi sutuna gore siralaniyor. -1 = varsayilan sira. </summary>
        private int siraliSutun = -1;

        /// <summary> 1 artan, 2 azalan. </summary>
        private int siraYonu = 0;

        /// <summary> Liste programla yeniden kuruluyor; secim olaylari susturuluyor. </summary>
        private bool sessiz = false;

        public IlSecici()
        {
            InitializeComponent();

            lst_iller.SelectedIndexChanged += Lst_iller_SelectedIndexChanged;
            lst_iller.Resize += (s, e) => SutunlariOlcekle();
            lst_iller.ColumnClick += Lst_iller_ColumnClick;
        }

        /// <summary>
        /// Sutun genisliklerini listenin o anki genisligine gore kurar.
        ///
        /// Sabit genislik yazilmiyor: ClientSize kayan cubuk gorununce
        /// daraliyor ve toplam genislik tam oturdugunda FARK sutununun son
        /// karakteri cubugun altinda kaliyordu - "-32,90" ekranda "-32,9"
        /// gorunuyordu. Il adi ne kalirsa onu aliyor.
        /// </summary>
        private void SutunlariOlcekle()
        {
            if (olcekleniyor) return;

            olcekleniyor = true;
            try
            {
                int kullanilabilir = lst_iller.ClientSize.Width;
                if (kullanilabilir <= 0) return;

                // Bu kontrol sayfadan sayfaya farkli genislikte duruyor;
                // en darinda (sahne 15) il adina ancak yer kaliyor. Plaka ve
                // fark sutunlari o yuzden icerigin gerektirdigi kadar dar:
                // "81 ▲" ve "-29,66" sigacak kadar.
                const int PLAKA = 50;

                // Karsilastirma yapmayan sayfalarda sutun bos duracagina
                // hic gorunmesin; yer il adina kalsin.
                int fark = (farklar == null) ? 0 : 76;

                // Kalan her sey il adinin: "KAHRAMANMARAŞ" en uzunu.
                int il = kullanilabilir - PLAKA - fark - 4;
                if (il < 90) il = 90;

                columnHeader1.Width = PLAKA;
                columnHeader2.Width = il;
                columnHeader3.Width = fark;
            }
            finally { olcekleniyor = false; }
        }

        private bool olcekleniyor = false;

        #region Ozellikler

        public Il SeciliIl
        {
            get { return seciliIl; }
        }

        public int SeciliPlaka
        {
            get { return seciliIl == null ? -1 : seciliIl.Plaka; }
        }

        public string AktifGrup
        {
            get { return aktifGrupKod; }
        }

        #endregion

        /// <summary>
        /// Kategori butonlarini ve ilk grubun listesini kurar.
        /// Veri yuklendikten sonra cagrilmali.
        /// </summary>
        public void Yukle(string acilisGrupKod)
        {
            pnl_gruplar.SuspendLayout();
            pnl_gruplar.Controls.Clear();
            grupButonlari.Clear();

            foreach (Grup grup in DataService.Veri.Gruplar)
            {
                Button btn = new Button();
                btn.Text = grup.Ad;
                btn.Tag = grup.Kod;
                btn.Width = pnl_gruplar.ClientSize.Width - 12;
                btn.Height = 48;
                btn.Margin = new Padding(0, 0, 0, 4);
                btn.Font = Tema.Yazi(9.75F, FontStyle.Bold);
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Click += Grup_Click;

                Tema.Dugme(btn, BTN_NORMAL, Tema.Metin);

                grupButonlari.Add(btn);
                pnl_gruplar.Controls.Add(btn);
            }

            pnl_gruplar.ResumeLayout();

            string ilk = acilisGrupKod;
            if (string.IsNullOrEmpty(ilk) && DataService.Veri.Gruplar.Count > 0)
                ilk = DataService.Veri.Gruplar[0].Kod;

            GrupSec(ilk);
        }

        /// <summary> Bir kategoriyi acar ve il listesini doldurur. </summary>
        public void GrupSec(string grupKod)
        {
            aktifGrupKod = grupKod;

            foreach (Button btn in grupButonlari)
            {
                bool aktif = ((string)btn.Tag == grupKod);
                Tema.Dugme(btn, aktif ? BTN_AKTIF : BTN_NORMAL, aktif ? Color.White : Tema.Metin);
            }

            List<Il> iller = DataService.GruptakiIller(grupKod);

            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();
            varsayilanSira.Clear();

            foreach (Il il in iller)
            {
                ListViewItem satir = new ListViewItem(il.Plaka.ToString());
                satir.SubItems.Add(il.Ad);
                satir.SubItems.Add("");      // FARK - karsilastirma varsa dolar
                satir.Tag = il;

                varsayilanSira.Add(satir);   // grubun kendi sirasi
                lst_iller.Items.Add(satir);
            }

            lst_iller.EndUpdate();

            // Kayan cubuk bu doldurmayla gelmis ya da gitmis olabilir.
            SutunlariOlcekle();

            // Grup degisince liste bastan kuruluyor; renkler de yeniden.
            FarklariUygula();

            // Operatorun kurdugu siralama grup degisince de korunuyor.
            SiralamayiUygula();

            // Onceki secim bu grupta da varsa korunsun, yoksa ilk satir secilsin.
            if (seciliIl != null && PlakaSec(seciliIl.Plaka)) return;
            if (lst_iller.Items.Count > 0) lst_iller.Items[0].Selected = true;
        }

        /// <summary> Listede plakayi secer. Bulamazsa false doner. </summary>
        public bool PlakaSec(int plaka)
        {
            foreach (ListViewItem satir in lst_iller.Items)
            {
                Il il = satir.Tag as Il;
                if (il == null || il.Plaka != plaka) continue;

                satir.Selected = true;
                satir.EnsureVisible();
                return true;
            }

            return false;
        }

        #region Fark renkleri

        /// <summary>
        /// Iki secim arasindaki farklari listede gosterir: artan yesil,
        /// azalan kirmizi, ne kadar degismisse o kadar koyu.
        ///
        /// Sozlukte OLMAYAN plaka karsilastirilamiyor demektir (kalem o
        /// secimde yok, ya da il o secimde yok) - o satir bos ve renksiz
        /// kalir. Eksik veriyi sifir sayip "-45 puan dusmus" gostermek
        /// operatoru yanlis yonlendirirdi.
        /// </summary>
        public void FarklariGoster(Dictionary<int, int> yeniFarklar)
        {
            farklar = yeniFarklar;
            farkOlcek = (yeniFarklar == null) ? 0 : FarkRenk.Olcek(yeniFarklar.Values);

            SutunlariOlcekle();      // FARK sutunu acilir ya da kapanir
            FarklariUygula();

            // Aday ya da secim degisince FARK degerleri de degisti.
            if (siraliSutun == SUTUN_FARK && farklar == null)
            {
                siraliSutun = -1;
                siraYonu = 0;
            }

            SiralamayiUygula();
        }

        /// <summary> Renkleri ve FARK sutununu bosaltir. </summary>
        public void FarklariTemizle()
        {
            FarklariGoster(null);
        }

        private void FarklariUygula()
        {
            lst_iller.BeginUpdate();

            foreach (ListViewItem satir in lst_iller.Items)
            {
                Il il = satir.Tag as Il;

                int fark = 0;
                bool varMi = false;

                if (farklar != null && il != null)
                    varMi = farklar.TryGetValue(il.Plaka, out fark);

                if (farklar == null)
                {
                    satir.SubItems[2].Text = "";
                    satir.BackColor = Tema.Girdi;
                }
                else if (!varMi)
                {
                    satir.SubItems[2].Text = FarkRenk.YOK;
                    satir.BackColor = Tema.Girdi;
                }
                else
                {
                    satir.SubItems[2].Text = FarkRenk.Metin(fark);
                    satir.BackColor = FarkRenk.Zemin(fark, farkOlcek);
                }

                satir.ForeColor = Tema.Metin;
            }

            lst_iller.EndUpdate();
        }

        #endregion

        #region Siralama

        /// <summary>
        /// Baslik tiklamasi: artan -> azalan -> grubun kendi sirasi.
        ///
        /// Ucuncu tiklamada varsayilana donmek onemli: operator FARK'a
        /// gore sirayi bozduktan sonra plaka sirasina elle geri donmenin
        /// baska yolu olmazdi.
        /// </summary>
        private void Lst_iller_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Karsilastirma yoksa FARK sutunu kapali; siralanacak bir sey de yok.
            if (e.Column == SUTUN_FARK && farklar == null) return;

            if (e.Column == siraliSutun)
            {
                siraYonu++;
                if (siraYonu > 2) { siraYonu = 0; siraliSutun = -1; }
            }
            else
            {
                siraliSutun = e.Column;
                siraYonu = 1;
            }

            SiralamayiUygula();
        }

        private void SiralamayiUygula()
        {
            if (varsayilanSira.Count == 0) return;

            List<ListViewItem> sirali = new List<ListViewItem>();

            if (siraliSutun < 0 || siraYonu == 0)
            {
                sirali.AddRange(varsayilanSira);
            }
            else
            {
                // Karsilastirilamayan iller ("—") hep en altta kalsin; yonu
                // ters cevirmek onlari tepeye tasimasin.
                List<ListViewItem> olanlar = new List<ListViewItem>();
                List<ListViewItem> olmayanlar = new List<ListViewItem>();

                foreach (ListViewItem satir in varsayilanSira)
                {
                    if (siraliSutun == SUTUN_FARK && !FarkiVar(satir)) olmayanlar.Add(satir);
                    else olanlar.Add(satir);
                }

                olanlar.Sort(Karsilastir);
                if (siraYonu == 2) olanlar.Reverse();

                sirali.AddRange(olanlar);
                sirali.AddRange(olmayanlar);
            }

            Il onceki = seciliIl;

            sessiz = true;
            lst_iller.BeginUpdate();
            lst_iller.Items.Clear();
            lst_iller.Items.AddRange(sirali.ToArray());
            lst_iller.EndUpdate();
            sessiz = false;

            BasliklariIsaretle();

            // Siralama il secimini degistirmiyor; secili olan secili kalsin.
            if (onceki != null)
            {
                sessiz = true;
                PlakaSec(onceki.Plaka);
                sessiz = false;
            }
        }

        private int Karsilastir(ListViewItem a, ListViewItem b)
        {
            Il ia = a.Tag as Il;
            Il ib = b.Tag as Il;

            if (siraliSutun == SUTUN_AD)
                return string.Compare(ia == null ? "" : ia.Ad, ib == null ? "" : ib.Ad,
                                      StringComparison.CurrentCulture);

            if (siraliSutun == SUTUN_FARK)
            {
                // Artan = en cok dusen ilk. Operator once "nerede kaybetmis"
                // diye bakiyor.
                return Fark(a).CompareTo(Fark(b));
            }

            return (ia == null ? 0 : ia.Plaka).CompareTo(ib == null ? 0 : ib.Plaka);
        }

        private bool FarkiVar(ListViewItem satir)
        {
            Il il = satir.Tag as Il;
            return farklar != null && il != null && farklar.ContainsKey(il.Plaka);
        }

        private int Fark(ListViewItem satir)
        {
            Il il = satir.Tag as Il;

            int deger;
            if (farklar != null && il != null && farklar.TryGetValue(il.Plaka, out deger)) return deger;

            return 0;
        }

        /// <summary>
        /// Siralanan sutunun basligina ok koyuyor. ListView kendi basina
        /// ok cizmedigi icin isaret basligin metnine ekleniyor.
        /// </summary>
        private void BasliklariIsaretle()
        {
            string[] adlar = { "PLK", "İL", "FARK" };
            ColumnHeader[] basliklar = { columnHeader1, columnHeader2, columnHeader3 };

            for (int i = 0; i < basliklar.Length; i++)
            {
                if (i == siraliSutun && siraYonu == 1) basliklar[i].Text = adlar[i] + " ▲";
                else if (i == siraliSutun && siraYonu == 2) basliklar[i].Text = adlar[i] + " ▼";
                else basliklar[i].Text = adlar[i];
            }
        }

        #endregion

        private void Grup_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            GrupSec((string)btn.Tag);
        }

        private void Lst_iller_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Siralama listeyi bastan kuruyor; aradaki secim degisiklikleri
            // gercek bir il secimi degil, sayfaya haber verilmemeli.
            if (sessiz) return;

            if (lst_iller.SelectedItems.Count == 0) return;

            Il il = lst_iller.SelectedItems[0].Tag as Il;
            if (il == null) return;

            seciliIl = il;

            if (OnIlSecildi != null) OnIlSecildi(il);
        }
    }
}
