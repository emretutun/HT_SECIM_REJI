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

        public IlSecici()
        {
            InitializeComponent();

            lst_iller.SelectedIndexChanged += Lst_iller_SelectedIndexChanged;
        }

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

            foreach (Il il in iller)
            {
                ListViewItem satir = new ListViewItem(il.Plaka.ToString());
                satir.SubItems.Add(il.Ad);
                satir.Tag = il;
                lst_iller.Items.Add(satir);
            }

            lst_iller.EndUpdate();

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

        private void Grup_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            GrupSec((string)btn.Tag);
        }

        private void Lst_iller_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lst_iller.SelectedItems.Count == 0) return;

            Il il = lst_iller.SelectedItems[0].Tag as Il;
            if (il == null) return;

            seciliIl = il;

            if (OnIlSecildi != null) OnIlSecildi(il);
        }
    }
}
