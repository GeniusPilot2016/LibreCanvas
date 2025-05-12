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
            textBoxPrompt = new TextBox();
            label1 = new Label();
            buttonCreate = new Button();
            icons = new ImageList(components);
            label2 = new Label();
            numericUpDownWidth = new NumericUpDown();
            labelWidth = new Label();
            numericUpDownHeight = new NumericUpDown();
            labelHeight = new Label();
            ((System.ComponentModel.ISupportInitialize)numericUpDownWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownHeight).BeginInit();
            SuspendLayout();
            // 
            // textBoxPrompt
            // 
            textBoxPrompt.Anchor = AnchorStyles.Top;
            textBoxPrompt.Font = new Font("HarmonyOS Sans", 8.999999F);
            textBoxPrompt.Location = new Point(96, 29);
            textBoxPrompt.Name = "textBoxPrompt";
            textBoxPrompt.Size = new Size(400, 27);
            textBoxPrompt.TabIndex = 0;
            textBoxPrompt.TextChanged += textBoxPrompt_TextChanged;
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
            // buttonCreate
            // 
            buttonCreate.Anchor = AnchorStyles.Top;
            buttonCreate.Enabled = false;
            buttonCreate.Font = new Font("HarmonyOS Sans", 8.999999F);
            buttonCreate.ImageIndex = 0;
            buttonCreate.ImageList = icons;
            buttonCreate.Location = new Point(502, 24);
            buttonCreate.Name = "buttonCreate";
            buttonCreate.Size = new Size(145, 35);
            buttonCreate.TabIndex = 2;
            buttonCreate.Text = "Create Image";
            buttonCreate.TextAlign = ContentAlignment.MiddleRight;
            buttonCreate.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonCreate.UseVisualStyleBackColor = true;
            buttonCreate.Click += button1_Click;
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
            label2.Location = new Point(48, 122);
            label2.Name = "label2";
            label2.Size = new Size(576, 20);
            label2.TabIndex = 1;
            label2.Text = "     Warning: AI image generation may produce inaccurate or inappropriate images.";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numericUpDownWidth
            // 
            numericUpDownWidth.Anchor = AnchorStyles.None;
            numericUpDownWidth.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numericUpDownWidth.Location = new Point(221, 80);
            numericUpDownWidth.Maximum = new decimal(new int[] { 1920, 0, 0, 0 });
            numericUpDownWidth.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            numericUpDownWidth.Name = "numericUpDownWidth";
            numericUpDownWidth.Size = new Size(69, 27);
            numericUpDownWidth.TabIndex = 3;
            numericUpDownWidth.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // labelWidth
            // 
            labelWidth.Anchor = AnchorStyles.None;
            labelWidth.AutoSize = true;
            labelWidth.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelWidth.Location = new Point(164, 82);
            labelWidth.Name = "labelWidth";
            labelWidth.Size = new Size(51, 20);
            labelWidth.TabIndex = 4;
            labelWidth.Text = "Width";
            // 
            // numericUpDownHeight
            // 
            numericUpDownHeight.Anchor = AnchorStyles.None;
            numericUpDownHeight.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numericUpDownHeight.Location = new Point(376, 80);
            numericUpDownHeight.Maximum = new decimal(new int[] { 1920, 0, 0, 0 });
            numericUpDownHeight.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            numericUpDownHeight.Name = "numericUpDownHeight";
            numericUpDownHeight.Size = new Size(69, 27);
            numericUpDownHeight.TabIndex = 3;
            numericUpDownHeight.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // labelHeight
            // 
            labelHeight.Anchor = AnchorStyles.None;
            labelHeight.AutoSize = true;
            labelHeight.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelHeight.Location = new Point(314, 82);
            labelHeight.Name = "labelHeight";
            labelHeight.Size = new Size(56, 20);
            labelHeight.TabIndex = 4;
            labelHeight.Text = "Height";
            // 
            // CreateWithAIForm
            // 
            AcceptButton = buttonCreate;
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            ClientSize = new Size(673, 163);
            Controls.Add(labelHeight);
            Controls.Add(numericUpDownHeight);
            Controls.Add(labelWidth);
            Controls.Add(numericUpDownWidth);
            Controls.Add(buttonCreate);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBoxPrompt);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateWithAIForm";
            ShowIcon = false;
            Text = "Create Image With AI";
            ((System.ComponentModel.ISupportInitialize)numericUpDownWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownHeight).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxPrompt;
        private Label label1;
        private Button buttonCreate;
        private ImageList icons;
        private Label label2;
        private NumericUpDown numericUpDownWidth;
        private Label labelWidth;
        private NumericUpDown numericUpDownHeight;
        private Label labelHeight;
    }
}