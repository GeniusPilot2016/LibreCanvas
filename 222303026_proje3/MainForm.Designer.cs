namespace _222303026_proje3
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            listBox1 = new ListBox();
            label1 = new Label();
            panel1 = new Panel();
            pictureBox1 = new PictureBox();
            label3 = new Label();
            label2 = new Label();
            button1 = new Button();
            icons = new ImageList(components);
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBox1.Enabled = false;
            listBox1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listBox1.FormattingEnabled = true;
            listBox1.Items.AddRange(new object[] { "There's no recently opened file" });
            listBox1.Location = new Point(12, 300);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(555, 164);
            listBox1.TabIndex = 2;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(203, 277);
            label1.Name = "label1";
            label1.Size = new Size(166, 20);
            label1.TabIndex = 3;
            label1.Text = "Recently Opened Files";
            // 
            // panel1
            // 
            panel1.BackgroundImage = Properties.Resources.pexels_dreamypixel_547115;
            panel1.BackgroundImageLayout = ImageLayout.Stretch;
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(579, 262);
            panel1.TabIndex = 4;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Properties.Resources.artfusion_icon;
            pictureBox1.Location = new Point(68, 63);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(142, 142);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("HarmonyOS Sans", 13.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(216, 158);
            label3.Name = "label3";
            label3.Size = new Size(146, 30);
            label3.TabIndex = 1;
            label3.Text = "Version 0.1.0";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("HarmonyOS Sans", 35.9999962F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(200, 79);
            label2.Name = "label2";
            label2.Size = new Size(314, 79);
            label2.TabIndex = 0;
            label2.Text = "ArtFusion";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ImageIndex = 0;
            button1.ImageList = icons;
            button1.Location = new Point(211, 470);
            button1.Name = "button1";
            button1.Size = new Size(158, 42);
            button1.TabIndex = 5;
            button1.Text = "Create New File";
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            // 
            // icons
            // 
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.ImageStream = (ImageListStreamer)resources.GetObject("icons.ImageStream");
            icons.TransparentColor = Color.Transparent;
            icons.Images.SetKeyName(0, "icons8-add-file-48.png");
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(579, 519);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(label1);
            Controls.Add(listBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "ArtFusion";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ListBox listBox1;
        private Label label1;
        private Panel panel1;
        private Label label2;
        private Label label3;
        private PictureBox pictureBox1;
        private Button button1;
        private ImageList icons;
    }
}