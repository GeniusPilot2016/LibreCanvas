namespace _222303026_proje3
{
    partial class CreateWithAIForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateWithAIForm));
            textBox1 = new TextBox();
            label1 = new Label();
            button1 = new Button();
            icons = new ImageList(components);
            label2 = new Label();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top;
            textBox1.Font = new Font("HarmonyOS Sans", 8.999999F);
            textBox1.Location = new Point(96, 29);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(400, 27);
            textBox1.TabIndex = 0;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new Font("HarmonyOS Sans", 8.999999F);
            label1.Location = new Point(29, 33);
            label1.Name = "label1";
            label1.Size = new Size(61, 20);
            label1.TabIndex = 1;
            label1.Text = "Prompt";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top;
            button1.Font = new Font("HarmonyOS Sans", 8.999999F);
            button1.ImageIndex = 0;
            button1.ImageList = icons;
            button1.Location = new Point(502, 24);
            button1.Name = "button1";
            button1.Size = new Size(145, 35);
            button1.TabIndex = 2;
            button1.Text = "Create Image";
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
            icons.Images.SetKeyName(0, "icons8-create-48.png");
            icons.Images.SetKeyName(1, "icons8-warning-48.png");
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom;
            label2.AutoSize = true;
            label2.Font = new Font("HarmonyOS Sans", 8.999999F);
            label2.ImageAlign = ContentAlignment.MiddleLeft;
            label2.ImageIndex = 1;
            label2.ImageList = icons;
            label2.Location = new Point(48, 89);
            label2.Name = "label2";
            label2.Size = new Size(576, 20);
            label2.TabIndex = 1;
            label2.Text = "     Warning: AI image generation may produce inaccurate or inappropriate images.";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // CreateWithAIForm
            // 
            AcceptButton = button1;
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            ClientSize = new Size(673, 130);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateWithAIForm";
            ShowIcon = false;
            Text = "Create Image With AI";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Button button1;
        private ImageList icons;
        private Label label2;
    }
}