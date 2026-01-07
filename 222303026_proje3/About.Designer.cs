namespace Carpathia
{
    partial class About
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
            components = new System.ComponentModel.Container();
            ListViewItem listViewItem1 = new ListViewItem(new string[] { "GeniusPilot2016", "Designing and programming" }, -1);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            panel1 = new Panel();
            pictureBox1 = new PictureBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            label4 = new Label();
            buttonVisitIcons8 = new Button();
            icons = new ImageList(components);
            buttonViewLicenseText = new Button();
            buttonForkMeOnGithub = new Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackgroundImage = Properties.Resources.librecanvas_pattern;
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 2, 3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(554, 169);
            panel1.TabIndex = 7;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Properties.Resources.librecanvas_icon;
            pictureBox1.Location = new Point(57, 29);
            pictureBox1.Margin = new Padding(3, 2, 3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(124, 106);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.None;
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("HarmonyOS Sans", 13.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(185, 95);
            label3.Name = "label3";
            label3.Size = new Size(123, 25);
            label3.TabIndex = 1;
            label3.Text = "Version 0.0.0";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.None;
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("HarmonyOS Sans", 35.9999962F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(176, 36);
            label2.Name = "label2";
            label2.Size = new Size(307, 64);
            label2.TabIndex = 0;
            label2.Text = "LibreCanvas";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom;
            label1.AutoSize = true;
            label1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(232, 319);
            label1.Name = "label1";
            label1.Size = new Size(80, 16);
            label1.TabIndex = 6;
            label1.Text = "Contributors";
            // 
            // listView1
            // 
            listView1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            listView1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listView1.Items.AddRange(new ListViewItem[] { listViewItem1 });
            listView1.Location = new Point(10, 336);
            listView1.Margin = new Padding(3, 2, 3, 2);
            listView1.Name = "listView1";
            listView1.Size = new Size(533, 183);
            listView1.TabIndex = 8;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Name";
            columnHeader1.Width = 250;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Task";
            columnHeader2.Width = 350;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Top;
            label4.AutoSize = true;
            label4.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.Location = new Point(79, 187);
            label4.Name = "label4";
            label4.Size = new Size(371, 80);
            label4.TabIndex = 9;
            label4.Text = resources.GetString("label4.Text");
            // 
            // buttonVisitIcons8
            // 
            buttonVisitIcons8.Anchor = AnchorStyles.Bottom;
            buttonVisitIcons8.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonVisitIcons8.ImageIndex = 0;
            buttonVisitIcons8.ImageList = icons;
            buttonVisitIcons8.Location = new Point(57, 279);
            buttonVisitIcons8.Margin = new Padding(3, 2, 3, 2);
            buttonVisitIcons8.Name = "buttonVisitIcons8";
            buttonVisitIcons8.Size = new Size(136, 26);
            buttonVisitIcons8.TabIndex = 10;
            buttonVisitIcons8.Text = "Visit icons8.com";
            buttonVisitIcons8.TextAlign = ContentAlignment.MiddleRight;
            buttonVisitIcons8.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonVisitIcons8.UseVisualStyleBackColor = true;
            buttonVisitIcons8.Click += buttonVisitIcons8_Click;
            // 
            // icons
            // 
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.ImageStream = (ImageListStreamer)resources.GetObject("icons.ImageStream");
            icons.TransparentColor = Color.Transparent;
            icons.Images.SetKeyName(0, "icons8-icons8-48.png");
            icons.Images.SetKeyName(1, "icons8-license-48.png");
            icons.Images.SetKeyName(2, "icons8-github-48.png");
            // 
            // buttonViewLicenseText
            // 
            buttonViewLicenseText.Anchor = AnchorStyles.Bottom;
            buttonViewLicenseText.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonViewLicenseText.ImageIndex = 1;
            buttonViewLicenseText.ImageList = icons;
            buttonViewLicenseText.Location = new Point(198, 279);
            buttonViewLicenseText.Margin = new Padding(3, 2, 3, 2);
            buttonViewLicenseText.Name = "buttonViewLicenseText";
            buttonViewLicenseText.Size = new Size(144, 26);
            buttonViewLicenseText.TabIndex = 10;
            buttonViewLicenseText.Text = "View License Text";
            buttonViewLicenseText.TextAlign = ContentAlignment.MiddleRight;
            buttonViewLicenseText.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonViewLicenseText.UseVisualStyleBackColor = true;
            buttonViewLicenseText.Click += buttonViewLicenseText_Click;
            // 
            // buttonForkMeOnGithub
            // 
            buttonForkMeOnGithub.Anchor = AnchorStyles.Bottom;
            buttonForkMeOnGithub.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonForkMeOnGithub.ImageIndex = 2;
            buttonForkMeOnGithub.ImageList = icons;
            buttonForkMeOnGithub.Location = new Point(347, 279);
            buttonForkMeOnGithub.Margin = new Padding(3, 2, 3, 2);
            buttonForkMeOnGithub.Name = "buttonForkMeOnGithub";
            buttonForkMeOnGithub.Size = new Size(153, 26);
            buttonForkMeOnGithub.TabIndex = 10;
            buttonForkMeOnGithub.Text = "Fork Me on GitHub";
            buttonForkMeOnGithub.TextAlign = ContentAlignment.MiddleRight;
            buttonForkMeOnGithub.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonForkMeOnGithub.UseVisualStyleBackColor = true;
            buttonForkMeOnGithub.Click += buttonForkMeOnGithub_Click;
            // 
            // About
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new Size(554, 527);
            Controls.Add(buttonForkMeOnGithub);
            Controls.Add(listView1);
            Controls.Add(panel1);
            Controls.Add(buttonViewLicenseText);
            Controls.Add(label1);
            Controls.Add(label4);
            Controls.Add(buttonVisitIcons8);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "About";
            ShowInTaskbar = false;
            Text = "About";
            Load += About_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private PictureBox pictureBox1;
        private Label label3;
        private Label label2;
        private Label label1;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private Label label4;
        private Button buttonVisitIcons8;
        private Button buttonViewLicenseText;
        private Button buttonForkMeOnGithub;
        private ImageList icons;
    }
}