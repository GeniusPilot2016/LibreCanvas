namespace Carpathia
{
    partial class SplashScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreen));
            panel1 = new Panel();
            progressBar1 = new ProgressBar();
            labelStatus = new Label();
            pictureBox1 = new PictureBox();
            labelVersion = new Label();
            label3 = new Label();
            progressTimer = new System.Windows.Forms.Timer(components);
            button1 = new Button();
            toolTip1 = new ToolTip(components);
            buttonMinimize = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            panel1.BackColor = Color.Transparent;
            panel1.Location = new Point(838, 390);
            panel1.Name = "panel1";
            panel1.Size = new Size(30, 30);
            panel1.TabIndex = 0;
            panel1.MouseDown += splash_MouseDown;
            panel1.MouseMove += splash_MouseMove;
            panel1.MouseUp += splash_MouseUp;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(12, 385);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(844, 23);
            progressBar1.TabIndex = 1;
            progressBar1.MouseDown += splash_MouseDown;
            progressBar1.MouseMove += splash_MouseMove;
            progressBar1.MouseUp += splash_MouseUp;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelStatus.BackColor = Color.Transparent;
            labelStatus.Font = new Font("HarmonyOS Sans", 11.9999981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelStatus.Location = new Point(12, 316);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(844, 55);
            labelStatus.TabIndex = 2;
            labelStatus.Text = "ArtFusion is starting...";
            labelStatus.TextAlign = ContentAlignment.BottomLeft;
            labelStatus.MouseDown += splash_MouseDown;
            labelStatus.MouseMove += splash_MouseMove;
            labelStatus.MouseUp += splash_MouseUp;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = Properties.Resources.artfusion_icon;
            pictureBox1.Location = new Point(23, 62);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(225, 225);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            pictureBox1.MouseDown += splash_MouseDown;
            pictureBox1.MouseMove += splash_MouseMove;
            pictureBox1.MouseUp += splash_MouseUp;
            // 
            // labelVersion
            // 
            labelVersion.Anchor = AnchorStyles.None;
            labelVersion.AutoSize = true;
            labelVersion.BackColor = Color.Transparent;
            labelVersion.Font = new Font("HarmonyOS Sans", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelVersion.Location = new Point(254, 215);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new Size(237, 49);
            labelVersion.TabIndex = 4;
            labelVersion.Text = "Version 0.0.0";
            labelVersion.MouseDown += splash_MouseDown;
            labelVersion.MouseMove += splash_MouseMove;
            labelVersion.MouseUp += splash_MouseUp;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.None;
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("HarmonyOS Sans", 72F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(234, 88);
            label3.Name = "label3";
            label3.Size = new Size(504, 127);
            label3.TabIndex = 4;
            label3.Text = "ArtFusion";
            label3.MouseDown += splash_MouseDown;
            label3.MouseMove += splash_MouseMove;
            label3.MouseUp += splash_MouseUp;
            // 
            // progressTimer
            // 
            progressTimer.Interval = 200;
            progressTimer.Tick += progressTimer_Tick;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.BackColor = Color.Transparent;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatAppearance.MouseDownBackColor = Color.Red;
            button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(194, 0, 159);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 162);
            button1.Location = new Point(823, 0);
            button1.Name = "button1";
            button1.Size = new Size(45, 30);
            button1.TabIndex = 5;
            button1.Text = "⨉";
            toolTip1.SetToolTip(button1, "Close");
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // buttonMinimize
            // 
            buttonMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMinimize.BackColor = Color.Transparent;
            buttonMinimize.FlatAppearance.BorderSize = 0;
            buttonMinimize.FlatAppearance.MouseDownBackColor = Color.FromArgb(128, 0, 106);
            buttonMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(194, 0, 159);
            buttonMinimize.FlatStyle = FlatStyle.Flat;
            buttonMinimize.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 162);
            buttonMinimize.Location = new Point(776, 0);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(45, 30);
            buttonMinimize.TabIndex = 5;
            buttonMinimize.Text = "─";
            toolTip1.SetToolTip(buttonMinimize, "Minimize");
            buttonMinimize.UseVisualStyleBackColor = false;
            buttonMinimize.Click += buttonMinimize_Click;
            // 
            // SplashScreen
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            BackgroundImage = Properties.Resources.artfusion_splash_background;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(868, 420);
            Controls.Add(buttonMinimize);
            Controls.Add(button1);
            Controls.Add(label3);
            Controls.Add(pictureBox1);
            Controls.Add(labelVersion);
            Controls.Add(labelStatus);
            Controls.Add(progressBar1);
            Controls.Add(panel1);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SplashScreen";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ArtFusion";
            Shown += SplashScreen_Shown;
            MouseDown += splash_MouseDown;
            MouseMove += splash_MouseMove;
            MouseUp += splash_MouseUp;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private ProgressBar progressBar1;
        private Label labelStatus;
        private PictureBox pictureBox1;
        private Label labelVersion;
        private Label label3;
        private System.Windows.Forms.Timer progressTimer;
        private Button button1;
        private ToolTip toolTip1;
        private Button buttonMinimize;
    }
}