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
            button2 = new Button();
            openFileDialog1 = new OpenFileDialog();
            contextMenuStrip1 = new ContextMenuStrip(components);
            createFileFromScratchToolStripMenuItem = new ToolStripMenuItem();
            createWithAIToolStripMenuItem = new ToolStripMenuItem();
            createWithWebcamToolStripMenuItem = new ToolStripMenuItem();
            button3 = new Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBox1.Enabled = false;
            listBox1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listBox1.FormattingEnabled = true;
            listBox1.Items.AddRange(new object[] { "There's no recently opened file" });
            listBox1.Location = new Point(11, 300);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(555, 144);
            listBox1.TabIndex = 2;
            listBox1.Click += listBox1_Click;
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
            panel1.Size = new Size(579, 261);
            panel1.TabIndex = 4;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Properties.Resources.artfusion_icon;
            pictureBox1.Location = new Point(69, 63);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(142, 141);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("HarmonyOS Sans", 13.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(216, 157);
            label3.Name = "label3";
            label3.Size = new Size(146, 30);
            label3.TabIndex = 1;
            label3.Text = "Version 0.8.0";
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
            button1.Location = new Point(46, 467);
            button1.Name = "button1";
            button1.Size = new Size(158, 43);
            button1.TabIndex = 5;
            button1.Text = "Create New File";
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // icons
            // 
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.ImageStream = (ImageListStreamer)resources.GetObject("icons.ImageStream");
            icons.TransparentColor = Color.Transparent;
            icons.Images.SetKeyName(0, "icons8-add-file-48.png");
            icons.Images.SetKeyName(1, "icons8-open-file-48.png");
            icons.Images.SetKeyName(2, "icons8-create-48.png");
            icons.Images.SetKeyName(3, "icons8-webcam-48.png");
            icons.Images.SetKeyName(4, "icons8-artificial-intelligence-48.png");
            icons.Images.SetKeyName(5, "icons8-settings-48.png");
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom;
            button2.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button2.ImageIndex = 1;
            button2.ImageList = icons;
            button2.Location = new Point(210, 467);
            button2.Name = "button2";
            button2.Size = new Size(158, 43);
            button2.TabIndex = 5;
            button2.Text = "Open File";
            button2.TextAlign = ContentAlignment.MiddleRight;
            button2.TextImageRelation = TextImageRelation.ImageBeforeText;
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { createFileFromScratchToolStripMenuItem, createWithAIToolStripMenuItem, createWithWebcamToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(252, 82);
            // 
            // createFileFromScratchToolStripMenuItem
            // 
            createFileFromScratchToolStripMenuItem.Image = Properties.Resources.icons8_add_file_48;
            createFileFromScratchToolStripMenuItem.Name = "createFileFromScratchToolStripMenuItem";
            createFileFromScratchToolStripMenuItem.Size = new Size(251, 26);
            createFileFromScratchToolStripMenuItem.Text = "Create File From Scratch";
            createFileFromScratchToolStripMenuItem.Click += createFileFromScratchToolStripMenuItem_Click;
            // 
            // createWithAIToolStripMenuItem
            // 
            createWithAIToolStripMenuItem.Image = Properties.Resources.icons8_artificial_intelligence_48;
            createWithAIToolStripMenuItem.Name = "createWithAIToolStripMenuItem";
            createWithAIToolStripMenuItem.Size = new Size(251, 26);
            createWithAIToolStripMenuItem.Text = "Create With AI";
            createWithAIToolStripMenuItem.Click += createWithAIToolStripMenuItem_Click;
            // 
            // createWithWebcamToolStripMenuItem
            // 
            createWithWebcamToolStripMenuItem.Image = Properties.Resources.icons8_webcam_48;
            createWithWebcamToolStripMenuItem.Name = "createWithWebcamToolStripMenuItem";
            createWithWebcamToolStripMenuItem.Size = new Size(251, 26);
            createWithWebcamToolStripMenuItem.Text = "Create With Webcam";
            createWithWebcamToolStripMenuItem.Click += createWithWebcamToolStripMenuItem_Click;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button3.ImageIndex = 5;
            button3.ImageList = icons;
            button3.Location = new Point(375, 467);
            button3.Name = "button3";
            button3.Size = new Size(158, 43);
            button3.TabIndex = 5;
            button3.Text = "Settings";
            button3.TextAlign = ContentAlignment.MiddleRight;
            button3.TextImageRelation = TextImageRelation.ImageBeforeText;
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(579, 519);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(label1);
            Controls.Add(listBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            Text = "ArtFusion";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contextMenuStrip1.ResumeLayout(false);
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
        private Button button2;
        private OpenFileDialog openFileDialog1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem createFileFromScratchToolStripMenuItem;
        private ToolStripMenuItem createWithAIToolStripMenuItem;
        private ToolStripMenuItem createWithWebcamToolStripMenuItem;
        private Button button3;
    }
}