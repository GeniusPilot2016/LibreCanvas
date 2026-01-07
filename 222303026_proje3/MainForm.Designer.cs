namespace Carpathia
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
            listBox1.Location = new Point(10, 225);
            listBox1.Margin = new Padding(3, 2, 3, 2);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(486, 100);
            listBox1.TabIndex = 2;
            listBox1.Click += listBox1_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(178, 208);
            label1.Name = "label1";
            label1.Size = new Size(135, 16);
            label1.TabIndex = 3;
            label1.Text = "Recently Opened Files";
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
            panel1.Size = new Size(507, 196);
            panel1.TabIndex = 4;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Properties.Resources.librecanvas_icon;
            pictureBox1.Location = new Point(40, 45);
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
            label3.Location = new Point(169, 116);
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
            label2.Location = new Point(158, 56);
            label2.Name = "label2";
            label2.Size = new Size(307, 64);
            label2.TabIndex = 0;
            label2.Text = "LibreCanvas";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ImageIndex = 0;
            button1.ImageList = icons;
            button1.Location = new Point(40, 350);
            button1.Margin = new Padding(3, 2, 3, 2);
            button1.Name = "button1";
            button1.Size = new Size(138, 32);
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
            button2.Location = new Point(184, 350);
            button2.Margin = new Padding(3, 2, 3, 2);
            button2.Name = "button2";
            button2.Size = new Size(138, 32);
            button2.TabIndex = 5;
            button2.Text = "Open File";
            button2.TextAlign = ContentAlignment.MiddleRight;
            button2.TextImageRelation = TextImageRelation.ImageBeforeText;
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { createFileFromScratchToolStripMenuItem, createWithAIToolStripMenuItem, createWithWebcamToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(213, 82);
            // 
            // createFileFromScratchToolStripMenuItem
            // 
            createFileFromScratchToolStripMenuItem.Image = Properties.Resources.icons8_add_file_48;
            createFileFromScratchToolStripMenuItem.Name = "createFileFromScratchToolStripMenuItem";
            createFileFromScratchToolStripMenuItem.Size = new Size(212, 26);
            createFileFromScratchToolStripMenuItem.Text = "Create File From Scratch";
            createFileFromScratchToolStripMenuItem.Click += createFileFromScratchToolStripMenuItem_Click;
            // 
            // createWithAIToolStripMenuItem
            // 
            createWithAIToolStripMenuItem.Image = Properties.Resources.icons8_artificial_intelligence_48;
            createWithAIToolStripMenuItem.Name = "createWithAIToolStripMenuItem";
            createWithAIToolStripMenuItem.Size = new Size(212, 26);
            createWithAIToolStripMenuItem.Text = "Create With AI";
            createWithAIToolStripMenuItem.Click += createWithAIToolStripMenuItem_Click;
            // 
            // createWithWebcamToolStripMenuItem
            // 
            createWithWebcamToolStripMenuItem.Image = Properties.Resources.icons8_webcam_48;
            createWithWebcamToolStripMenuItem.Name = "createWithWebcamToolStripMenuItem";
            createWithWebcamToolStripMenuItem.Size = new Size(212, 26);
            createWithWebcamToolStripMenuItem.Text = "Create With Webcam";
            createWithWebcamToolStripMenuItem.Click += createWithWebcamToolStripMenuItem_Click;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button3.ImageIndex = 5;
            button3.ImageList = icons;
            button3.Location = new Point(328, 350);
            button3.Margin = new Padding(3, 2, 3, 2);
            button3.Name = "button3";
            button3.Size = new Size(138, 32);
            button3.TabIndex = 5;
            button3.Text = "Settings";
            button3.TextAlign = ContentAlignment.MiddleRight;
            button3.TextImageRelation = TextImageRelation.ImageBeforeText;
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(507, 389);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(label1);
            Controls.Add(listBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            Text = "LibreCanvas";
            FormClosed += MainForm_FormClosed;
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