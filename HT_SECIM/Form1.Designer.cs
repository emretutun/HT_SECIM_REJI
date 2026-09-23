namespace HT_SECIM
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnl_top = new System.Windows.Forms.Panel();
            this.lbl_status = new System.Windows.Forms.Label();
            this.lbl_veri = new System.Windows.Forms.Label();
            this.cmb_engine = new System.Windows.Forms.ComboBox();
            this.chk_ac = new System.Windows.Forms.CheckBox();
            this.chk_hb = new System.Windows.Forms.CheckBox();
            this.btn_connect = new System.Windows.Forms.Button();
            this.txt_ip = new System.Windows.Forms.TextBox();
            this.lbl_onair = new System.Windows.Forms.Label();
            this.pnl_cmd = new System.Windows.Forms.Panel();
            this.txt_command = new System.Windows.Forms.TextBox();
            this.btn_send = new System.Windows.Forms.Button();
            this.lst_log = new System.Windows.Forms.ListBox();
            this.pnl_sol = new System.Windows.Forms.Panel();
            this.pnl_left = new System.Windows.Forms.FlowLayoutPanel();
            this.txt_ara = new System.Windows.Forms.TextBox();
            this.pnl_right = new System.Windows.Forms.Panel();
            this.pnl_page = new System.Windows.Forms.Panel();
            this.pnl_head = new System.Windows.Forms.Panel();
            this.lbl_head_no = new System.Windows.Forms.Label();
            this.lbl_head_scene = new System.Windows.Forms.Label();
            this.btn_head_al = new System.Windows.Forms.Button();
            this.btn_head_ver = new System.Windows.Forms.Button();
            this.btn_head_hazirla = new System.Windows.Forms.Button();
            this.lbl_head_onair = new System.Windows.Forms.Label();
            this.pnl_top.SuspendLayout();
            this.pnl_cmd.SuspendLayout();
            this.pnl_sol.SuspendLayout();
            this.pnl_right.SuspendLayout();
            this.pnl_head.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_top
            // 
            this.pnl_top.BackColor = System.Drawing.Color.Silver;
            this.pnl_top.Controls.Add(this.lbl_veri);
            this.pnl_top.Controls.Add(this.chk_hb);
            this.pnl_top.Controls.Add(this.chk_ac);
            this.pnl_top.Controls.Add(this.lbl_status);
            this.pnl_top.Controls.Add(this.cmb_engine);
            this.pnl_top.Controls.Add(this.btn_connect);
            this.pnl_top.Controls.Add(this.txt_ip);
            this.pnl_top.Controls.Add(this.lbl_onair);
            this.pnl_top.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_top.Location = new System.Drawing.Point(0, 0);
            this.pnl_top.Name = "pnl_top";
            this.pnl_top.Size = new System.Drawing.Size(1552, 64);
            this.pnl_top.TabIndex = 0;
            // 
            // lbl_status
            // 
            this.lbl_status.BackColor = System.Drawing.Color.Gray;
            this.lbl_status.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_status.ForeColor = System.Drawing.Color.White;
            this.lbl_status.Location = new System.Drawing.Point(790, 14);
            this.lbl_status.Name = "lbl_status";
            this.lbl_status.Size = new System.Drawing.Size(180, 38);
            this.lbl_status.TabIndex = 4;
            this.lbl_status.Tag = "TEMA_DISI";
            this.lbl_status.Text = "BAGLI DEGIL";
            this.lbl_status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chk_ac
            // 
            this.chk_ac.AutoSize = true;
            this.chk_ac.Font = new System.Drawing.Font("Arial", 9.75F);
            this.chk_ac.Location = new System.Drawing.Point(1212, 14);
            this.chk_ac.Name = "chk_ac";
            this.chk_ac.Size = new System.Drawing.Size(150, 17);
            this.chk_ac.TabIndex = 5;
            this.chk_ac.Text = "AC  otomatik bağlan";
            this.chk_ac.UseVisualStyleBackColor = true;
            // 
            // chk_hb
            // 
            this.chk_hb.AutoSize = true;
            this.chk_hb.Font = new System.Drawing.Font("Arial", 9.75F);
            this.chk_hb.Location = new System.Drawing.Point(1212, 36);
            this.chk_hb.Name = "chk_hb";
            this.chk_hb.Size = new System.Drawing.Size(150, 17);
            this.chk_hb.TabIndex = 6;
            this.chk_hb.Text = "HB  nabız";
            this.chk_hb.UseVisualStyleBackColor = true;
            //
            // lbl_veri
            //
            this.lbl_veri.BackColor = System.Drawing.Color.Gray;
            this.lbl_veri.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lbl_veri.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_veri.ForeColor = System.Drawing.Color.White;
            this.lbl_veri.Location = new System.Drawing.Point(985, 14);
            this.lbl_veri.Name = "lbl_veri";
            this.lbl_veri.Size = new System.Drawing.Size(210, 38);
            this.lbl_veri.TabIndex = 5;
            this.lbl_veri.Tag = "TEMA_DISI";
            this.lbl_veri.Text = "VERİ";
            this.lbl_veri.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmb_engine
            // 
            this.cmb_engine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_engine.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmb_engine.FormattingEnabled = true;
            this.cmb_engine.Location = new System.Drawing.Point(552, 20);
            this.cmb_engine.Name = "cmb_engine";
            this.cmb_engine.Size = new System.Drawing.Size(220, 26);
            this.cmb_engine.TabIndex = 3;
            this.cmb_engine.SelectedIndexChanged += new System.EventHandler(this.cmb_engine_SelectedIndexChanged);
            // 
            // btn_connect
            // 
            this.btn_connect.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_connect.Location = new System.Drawing.Point(400, 14);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(140, 38);
            this.btn_connect.TabIndex = 2;
            this.btn_connect.Text = "CONNECT";
            this.btn_connect.UseVisualStyleBackColor = true;
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // txt_ip
            // 
            this.txt_ip.Font = new System.Drawing.Font("Arial", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_ip.Location = new System.Drawing.Point(164, 13);
            this.txt_ip.Name = "txt_ip";
            this.txt_ip.ReadOnly = true;
            this.txt_ip.Size = new System.Drawing.Size(220, 39);
            this.txt_ip.TabIndex = 1;
            this.txt_ip.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lbl_onair
            // 
            this.lbl_onair.BackColor = System.Drawing.Color.Gray;
            this.lbl_onair.Font = new System.Drawing.Font("Arial", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_onair.ForeColor = System.Drawing.Color.White;
            this.lbl_onair.Location = new System.Drawing.Point(8, 12);
            this.lbl_onair.Name = "lbl_onair";
            this.lbl_onair.Size = new System.Drawing.Size(150, 40);
            this.lbl_onair.TabIndex = 0;
            this.lbl_onair.Tag = "TEMA_DISI";
            this.lbl_onair.Text = "ONAIR";
            this.lbl_onair.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnl_cmd
            // 
            this.pnl_cmd.Controls.Add(this.txt_command);
            this.pnl_cmd.Controls.Add(this.btn_send);
            this.pnl_cmd.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_cmd.Location = new System.Drawing.Point(0, 64);
            this.pnl_cmd.Name = "pnl_cmd";
            this.pnl_cmd.Size = new System.Drawing.Size(1552, 34);
            this.pnl_cmd.TabIndex = 1;
            // 
            // txt_command
            // 
            this.txt_command.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_command.Location = new System.Drawing.Point(0, 0);
            this.txt_command.Name = "txt_command";
            this.txt_command.Size = new System.Drawing.Size(1442, 20);
            this.txt_command.TabIndex = 1;
            this.txt_command.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txt_command_KeyDown);
            // 
            // btn_send
            // 
            this.btn_send.Dock = System.Windows.Forms.DockStyle.Right;
            this.btn_send.Location = new System.Drawing.Point(1442, 0);
            this.btn_send.Name = "btn_send";
            this.btn_send.Size = new System.Drawing.Size(110, 34);
            this.btn_send.TabIndex = 0;
            this.btn_send.Text = "GÖNDER";
            this.btn_send.UseVisualStyleBackColor = true;
            this.btn_send.Click += new System.EventHandler(this.btn_send_Click);
            // 
            // lst_log
            // 
            this.lst_log.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lst_log.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lst_log.FormattingEnabled = true;
            this.lst_log.ItemHeight = 14;
            this.lst_log.Location = new System.Drawing.Point(0, 563);
            this.lst_log.Name = "lst_log";
            this.lst_log.Size = new System.Drawing.Size(1552, 130);
            this.lst_log.TabIndex = 2;
            this.lst_log.Tag = "MONO";
            // 
            // pnl_sol
            // 
            this.pnl_sol.Controls.Add(this.pnl_left);
            this.pnl_sol.Controls.Add(this.txt_ara);
            this.pnl_sol.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnl_sol.Location = new System.Drawing.Point(0, 98);
            this.pnl_sol.Name = "pnl_sol";
            this.pnl_sol.Padding = new System.Windows.Forms.Padding(0, 7, 0, 0);
            this.pnl_sol.Size = new System.Drawing.Size(900, 465);
            this.pnl_sol.TabIndex = 3;
            // 
            // pnl_left
            // 
            this.pnl_left.AutoScroll = true;
            this.pnl_left.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_left.Location = new System.Drawing.Point(0, 34);
            this.pnl_left.Name = "pnl_left";
            this.pnl_left.Padding = new System.Windows.Forms.Padding(4);
            this.pnl_left.Size = new System.Drawing.Size(900, 431);
            this.pnl_left.TabIndex = 1;
            // 
            // txt_ara
            // 
            this.txt_ara.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_ara.Dock = System.Windows.Forms.DockStyle.Top;
            this.txt_ara.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txt_ara.Location = new System.Drawing.Point(0, 7);
            this.txt_ara.Name = "txt_ara";
            this.txt_ara.Size = new System.Drawing.Size(900, 27);
            this.txt_ara.TabIndex = 0;
            // 
            // pnl_right
            // 
            this.pnl_right.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(245)))), ((int)(((byte)(234)))));
            this.pnl_right.Controls.Add(this.pnl_page);
            this.pnl_right.Controls.Add(this.pnl_head);
            this.pnl_right.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_right.Location = new System.Drawing.Point(900, 98);
            this.pnl_right.Margin = new System.Windows.Forms.Padding(6);
            this.pnl_right.Name = "pnl_right";
            this.pnl_right.Size = new System.Drawing.Size(652, 465);
            this.pnl_right.TabIndex = 4;
            // 
            // pnl_page
            // 
            this.pnl_page.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(245)))), ((int)(((byte)(234)))));
            this.pnl_page.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_page.Location = new System.Drawing.Point(0, 66);
            this.pnl_page.Name = "pnl_page";
            this.pnl_page.Size = new System.Drawing.Size(652, 399);
            this.pnl_page.TabIndex = 1;
            // 
            // pnl_head
            // 
            this.pnl_head.BackColor = System.Drawing.Color.Gainsboro;
            this.pnl_head.Controls.Add(this.lbl_head_no);
            this.pnl_head.Controls.Add(this.lbl_head_scene);
            this.pnl_head.Controls.Add(this.btn_head_al);
            this.pnl_head.Controls.Add(this.btn_head_ver);
            this.pnl_head.Controls.Add(this.btn_head_hazirla);
            this.pnl_head.Controls.Add(this.lbl_head_onair);
            this.pnl_head.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_head.Location = new System.Drawing.Point(0, 0);
            this.pnl_head.Name = "pnl_head";
            this.pnl_head.Size = new System.Drawing.Size(652, 66);
            this.pnl_head.TabIndex = 0;
            // 
            // lbl_head_no
            // 
            this.lbl_head_no.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_head_no.BackColor = System.Drawing.Color.Maroon;
            this.lbl_head_no.Font = new System.Drawing.Font("Arial", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_head_no.ForeColor = System.Drawing.Color.White;
            this.lbl_head_no.Location = new System.Drawing.Point(570, 6);
            this.lbl_head_no.Name = "lbl_head_no";
            this.lbl_head_no.Size = new System.Drawing.Size(76, 54);
            this.lbl_head_no.TabIndex = 5;
            this.lbl_head_no.Tag = "TEMA_DISI";
            this.lbl_head_no.Text = "0";
            this.lbl_head_no.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_head_scene
            // 
            this.lbl_head_scene.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_head_scene.BackColor = System.Drawing.Color.Black;
            this.lbl_head_scene.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_head_scene.ForeColor = System.Drawing.Color.Red;
            this.lbl_head_scene.Location = new System.Drawing.Point(6, 38);
            this.lbl_head_scene.Name = "lbl_head_scene";
            this.lbl_head_scene.Size = new System.Drawing.Size(556, 24);
            this.lbl_head_scene.TabIndex = 4;
            this.lbl_head_scene.Tag = "TEMA_DISI";
            this.lbl_head_scene.Text = "-";
            this.lbl_head_scene.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_head_al
            // 
            this.btn_head_al.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(176)))), ((int)(((byte)(80)))));
            this.btn_head_al.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_head_al.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_head_al.ForeColor = System.Drawing.Color.Black;
            this.btn_head_al.Location = new System.Drawing.Point(384, 4);
            this.btn_head_al.Name = "btn_head_al";
            this.btn_head_al.Size = new System.Drawing.Size(130, 30);
            this.btn_head_al.TabIndex = 3;
            this.btn_head_al.Tag = "TEMA_DISI";
            this.btn_head_al.Text = "AL";
            this.btn_head_al.UseVisualStyleBackColor = false;
            // 
            // btn_head_ver
            // 
            this.btn_head_ver.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(176)))), ((int)(((byte)(80)))));
            this.btn_head_ver.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_head_ver.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_head_ver.ForeColor = System.Drawing.Color.Black;
            this.btn_head_ver.Location = new System.Drawing.Point(248, 4);
            this.btn_head_ver.Name = "btn_head_ver";
            this.btn_head_ver.Size = new System.Drawing.Size(130, 30);
            this.btn_head_ver.TabIndex = 2;
            this.btn_head_ver.Tag = "TEMA_DISI";
            this.btn_head_ver.Text = "VER";
            this.btn_head_ver.UseVisualStyleBackColor = false;
            // 
            // btn_head_hazirla
            // 
            this.btn_head_hazirla.BackColor = System.Drawing.Color.Yellow;
            this.btn_head_hazirla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_head_hazirla.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_head_hazirla.ForeColor = System.Drawing.Color.Black;
            this.btn_head_hazirla.Location = new System.Drawing.Point(112, 4);
            this.btn_head_hazirla.Name = "btn_head_hazirla";
            this.btn_head_hazirla.Size = new System.Drawing.Size(130, 30);
            this.btn_head_hazirla.TabIndex = 1;
            this.btn_head_hazirla.Tag = "TEMA_DISI";
            this.btn_head_hazirla.Text = "HAZIRLA";
            this.btn_head_hazirla.UseVisualStyleBackColor = false;
            // 
            // lbl_head_onair
            // 
            this.lbl_head_onair.BackColor = System.Drawing.Color.Black;
            this.lbl_head_onair.Font = new System.Drawing.Font("Arial", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_head_onair.ForeColor = System.Drawing.Color.Red;
            this.lbl_head_onair.Location = new System.Drawing.Point(6, 4);
            this.lbl_head_onair.Name = "lbl_head_onair";
            this.lbl_head_onair.Size = new System.Drawing.Size(100, 30);
            this.lbl_head_onair.TabIndex = 0;
            this.lbl_head_onair.Tag = "TEMA_DISI";
            this.lbl_head_onair.Text = "ONAIR";
            this.lbl_head_onair.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1552, 693);
            this.Controls.Add(this.pnl_right);
            this.Controls.Add(this.pnl_sol);
            this.Controls.Add(this.lst_log);
            this.Controls.Add(this.pnl_cmd);
            this.Controls.Add(this.pnl_top);
            this.MinimumSize = new System.Drawing.Size(1280, 720);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HABERTÜRK  İÇ EKRANLAR";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnl_top.ResumeLayout(false);
            this.pnl_top.PerformLayout();
            this.pnl_cmd.ResumeLayout(false);
            this.pnl_cmd.PerformLayout();
            this.pnl_sol.ResumeLayout(false);
            this.pnl_sol.PerformLayout();
            this.pnl_right.ResumeLayout(false);
            this.pnl_head.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_top;
        private System.Windows.Forms.Label lbl_onair;
        private System.Windows.Forms.TextBox txt_ip;
        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.Label lbl_status;
        private System.Windows.Forms.Label lbl_veri;
        private System.Windows.Forms.ComboBox cmb_engine;
        private System.Windows.Forms.CheckBox chk_ac;
        private System.Windows.Forms.CheckBox chk_hb;
        private System.Windows.Forms.Panel pnl_cmd;
        private System.Windows.Forms.Button btn_send;
        private System.Windows.Forms.TextBox txt_command;
        private System.Windows.Forms.ListBox lst_log;
        private System.Windows.Forms.Panel pnl_sol;
        private System.Windows.Forms.TextBox txt_ara;
        private System.Windows.Forms.FlowLayoutPanel pnl_left;
        private System.Windows.Forms.Panel pnl_right;
        private System.Windows.Forms.Panel pnl_head;
        private System.Windows.Forms.Label lbl_head_onair;
        private System.Windows.Forms.Button btn_head_al;
        private System.Windows.Forms.Button btn_head_ver;
        private System.Windows.Forms.Button btn_head_hazirla;
        private System.Windows.Forms.Label lbl_head_no;
        private System.Windows.Forms.Panel pnl_page;
        private System.Windows.Forms.Label lbl_head_scene;
    }
}

