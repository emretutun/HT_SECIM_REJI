namespace HT_SECIM.UI
{
    partial class IlSecici
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnl_gruplar = new System.Windows.Forms.FlowLayoutPanel();
            this.lst_iller = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // pnl_gruplar
            // 
            this.pnl_gruplar.AutoScroll = true;
            this.pnl_gruplar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(253)))), ((int)(((byte)(246)))), ((int)(((byte)(227)))));
            this.pnl_gruplar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnl_gruplar.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnl_gruplar.Location = new System.Drawing.Point(0, 0);
            this.pnl_gruplar.Name = "pnl_gruplar";
            this.pnl_gruplar.Padding = new System.Windows.Forms.Padding(4);
            this.pnl_gruplar.Size = new System.Drawing.Size(130, 470);
            this.pnl_gruplar.TabIndex = 0;
            this.pnl_gruplar.WrapContents = false;
            // 
            // lst_iller
            // 
            this.lst_iller.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lst_iller.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lst_iller.Font = new System.Drawing.Font("Arial Narrow", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lst_iller.FullRowSelect = true;
            this.lst_iller.GridLines = true;
            this.lst_iller.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lst_iller.HideSelection = false;
            this.lst_iller.Location = new System.Drawing.Point(130, 0);
            this.lst_iller.MultiSelect = false;
            this.lst_iller.Name = "lst_iller";
            this.lst_iller.Size = new System.Drawing.Size(300, 470);
            this.lst_iller.TabIndex = 1;
            this.lst_iller.UseCompatibleStateImageBehavior = false;
            this.lst_iller.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "PLK";
            this.columnHeader1.Width = 45;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "İL";
            this.columnHeader2.Width = 220;
            // 
            // IlSecici
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lst_iller);
            this.Controls.Add(this.pnl_gruplar);
            this.Name = "IlSecici";
            this.Size = new System.Drawing.Size(430, 470);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel pnl_gruplar;
        private System.Windows.Forms.ListView lst_iller;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
    }
}
