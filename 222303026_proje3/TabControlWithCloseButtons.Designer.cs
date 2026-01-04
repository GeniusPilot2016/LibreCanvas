namespace Carpathia
{
    partial class TabControlWithCloseButtons
    {
        /// <summary> 
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Bileşen Tasarımcısı üretimi kod

        /// <summary> 
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TabControlWithCloseButtons));
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabIcons = new ImageList(components);
            tabControl1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.ImageList = tabIcons;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Margin = new Padding(4, 4, 4, 4);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(292, 307);
            tabControl1.TabIndex = 0;
            tabControl1.DrawItem += tabControl1_DrawItem;
            tabControl1.MouseClick += tabControl1_MouseClick;
            tabControl1.MouseEnter += tabControl1_MouseEnter;
            tabControl1.MouseLeave += tabControl1_MouseLeave;
            // 
            // tabPage1
            // 
            tabPage1.ImageIndex = 0;
            tabPage1.Location = new Point(4, 55);
            tabPage1.Margin = new Padding(4, 4, 4, 4);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(4, 4, 4, 4);
            tabPage1.Size = new Size(284, 248);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Image";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabIcons
            // 
            tabIcons.ColorDepth = ColorDepth.Depth32Bit;
            tabIcons.ImageStream = (ImageListStreamer)resources.GetObject("tabIcons.ImageStream");
            tabIcons.TransparentColor = Color.Transparent;
            tabIcons.Images.SetKeyName(0, "icons8-image-48.png");
            // 
            // TabControlWithCloseButtons
            // 
            AutoScaleDimensions = new SizeF(8F, 18F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl1);
            Font = new Font("HarmonyOS Sans", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(4, 4, 4, 4);
            Name = "TabControlWithCloseButtons";
            Size = new Size(292, 307);
            Load += TabControlWithCloseButtons_Load;
            tabControl1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private ImageList tabIcons;
    }
}
