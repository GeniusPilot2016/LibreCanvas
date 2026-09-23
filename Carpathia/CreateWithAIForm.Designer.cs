namespace Carpathia
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
            labelPrompt = new Label();
            buttonCreate = new Button();
            icons = new ImageList(components);
            label2 = new Label();
            numericUpDownWidth = new NumericUpDown();
            labelWidth = new Label();
            numericUpDownHeight = new NumericUpDown();
            labelHeight = new Label();
            progressBarImageCreating = new ProgressBar();
            labelImageCreating = new Label();
            buttonUploadImage = new Button();
            labelUploadedImage = new Label();
            fileBrowser = new OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)numericUpDownWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownHeight).BeginInit();
            SuspendLayout();
            // 
            // textBoxPrompt
            // 
            textBoxPrompt.Anchor = AnchorStyles.Top;
            textBoxPrompt.Font = new Font("HarmonyOS Sans", 8.999999F);
            textBoxPrompt.Location = new Point(70, 18);
            textBoxPrompt.Margin = new Padding(2);
            textBoxPrompt.Name = "textBoxPrompt";
            textBoxPrompt.Size = new Size(321, 23);
            textBoxPrompt.TabIndex = 0;
            textBoxPrompt.TextChanged += textBoxPrompt_TextChanged;
            // 
            // labelPrompt
            // 
            labelPrompt.Anchor = AnchorStyles.Top;
            labelPrompt.AutoSize = true;
            labelPrompt.Font = new Font("HarmonyOS Sans", 8.999999F);
            labelPrompt.Location = new Point(16, 21);
            labelPrompt.Margin = new Padding(2, 0, 2, 0);
            labelPrompt.Name = "labelPrompt";
            labelPrompt.Size = new Size(46, 16);
            labelPrompt.TabIndex = 1;
            labelPrompt.Text = "Prompt";
            // 
            // buttonCreate
            // 
            buttonCreate.Anchor = AnchorStyles.Top;
            buttonCreate.Enabled = false;
            buttonCreate.Font = new Font("HarmonyOS Sans", 8.999999F);
            buttonCreate.ImageIndex = 0;
            buttonCreate.ImageList = icons;
            buttonCreate.Location = new Point(394, 14);
            buttonCreate.Margin = new Padding(2);
            buttonCreate.Name = "buttonCreate";
            buttonCreate.Size = new Size(116, 28);
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
            icons.Images.SetKeyName(2, "icons8-attachment-48.png");
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom;
            label2.AutoSize = true;
            label2.Font = new Font("HarmonyOS Sans", 8.999999F);
            label2.ImageAlign = ContentAlignment.MiddleLeft;
            label2.ImageIndex = 1;
            label2.ImageList = icons;
            label2.Location = new Point(34, 130);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(457, 16);
            label2.TabIndex = 1;
            label2.Text = "        Warning: AI image generator may produce inaccurate or inappropriate images.";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numericUpDownWidth
            // 
            numericUpDownWidth.Anchor = AnchorStyles.Top;
            numericUpDownWidth.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numericUpDownWidth.Location = new Point(187, 62);
            numericUpDownWidth.Margin = new Padding(2);
            numericUpDownWidth.Maximum = new decimal(new int[] { 1920, 0, 0, 0 });
            numericUpDownWidth.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            numericUpDownWidth.Name = "numericUpDownWidth";
            numericUpDownWidth.Size = new Size(55, 23);
            numericUpDownWidth.TabIndex = 3;
            numericUpDownWidth.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // labelWidth
            // 
            labelWidth.Anchor = AnchorStyles.Top;
            labelWidth.AutoSize = true;
            labelWidth.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelWidth.Location = new Point(142, 64);
            labelWidth.Margin = new Padding(2, 0, 2, 0);
            labelWidth.Name = "labelWidth";
            labelWidth.Size = new Size(40, 16);
            labelWidth.TabIndex = 4;
            labelWidth.Text = "Width";
            // 
            // numericUpDownHeight
            // 
            numericUpDownHeight.Anchor = AnchorStyles.Top;
            numericUpDownHeight.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numericUpDownHeight.Location = new Point(312, 62);
            numericUpDownHeight.Margin = new Padding(2);
            numericUpDownHeight.Maximum = new decimal(new int[] { 1920, 0, 0, 0 });
            numericUpDownHeight.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            numericUpDownHeight.Name = "numericUpDownHeight";
            numericUpDownHeight.Size = new Size(55, 23);
            numericUpDownHeight.TabIndex = 3;
            numericUpDownHeight.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // labelHeight
            // 
            labelHeight.Anchor = AnchorStyles.Top;
            labelHeight.AutoSize = true;
            labelHeight.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelHeight.Location = new Point(262, 64);
            labelHeight.Margin = new Padding(2, 0, 2, 0);
            labelHeight.Name = "labelHeight";
            labelHeight.Size = new Size(44, 16);
            labelHeight.TabIndex = 4;
            labelHeight.Text = "Height";
            // 
            // progressBarImageCreating
            // 
            progressBarImageCreating.Anchor = AnchorStyles.Bottom;
            progressBarImageCreating.Location = new Point(126, 159);
            progressBarImageCreating.Margin = new Padding(4);
            progressBarImageCreating.MarqueeAnimationSpeed = 10;
            progressBarImageCreating.Name = "progressBarImageCreating";
            progressBarImageCreating.Size = new Size(385, 16);
            progressBarImageCreating.Style = ProgressBarStyle.Marquee;
            progressBarImageCreating.TabIndex = 5;
            progressBarImageCreating.Visible = false;
            // 
            // labelImageCreating
            // 
            labelImageCreating.Anchor = AnchorStyles.Bottom;
            labelImageCreating.AutoSize = true;
            labelImageCreating.Font = new Font("HarmonyOS Sans", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelImageCreating.Location = new Point(16, 159);
            labelImageCreating.Margin = new Padding(4, 0, 4, 0);
            labelImageCreating.Name = "labelImageCreating";
            labelImageCreating.Size = new Size(100, 16);
            labelImageCreating.TabIndex = 6;
            labelImageCreating.Text = "Image is creating";
            labelImageCreating.Visible = false;
            // 
            // buttonUploadImage
            // 
            buttonUploadImage.Anchor = AnchorStyles.None;
            buttonUploadImage.Font = new Font("HarmonyOS Sans", 8.999999F);
            buttonUploadImage.ImageIndex = 2;
            buttonUploadImage.ImageList = icons;
            buttonUploadImage.Location = new Point(151, 94);
            buttonUploadImage.Name = "buttonUploadImage";
            buttonUploadImage.Size = new Size(108, 29);
            buttonUploadImage.TabIndex = 7;
            buttonUploadImage.Text = "Upload";
            buttonUploadImage.TextAlign = ContentAlignment.MiddleRight;
            buttonUploadImage.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonUploadImage.UseVisualStyleBackColor = true;
            buttonUploadImage.Click += button1_Click_1;
            // 
            // labelUploadedImage
            // 
            labelUploadedImage.Anchor = AnchorStyles.None;
            labelUploadedImage.AutoSize = true;
            labelUploadedImage.Font = new Font("HarmonyOS Sans", 8.999999F);
            labelUploadedImage.Location = new Point(265, 98);
            labelUploadedImage.Name = "labelUploadedImage";
            labelUploadedImage.Size = new Size(126, 16);
            labelUploadedImage.TabIndex = 8;
            labelUploadedImage.Text = "No image is uploaded";
            // 
            // CreateWithAIForm
            // 
            AcceptButton = buttonCreate;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            ClientSize = new Size(528, 188);
            Controls.Add(labelUploadedImage);
            Controls.Add(buttonUploadImage);
            Controls.Add(labelImageCreating);
            Controls.Add(progressBarImageCreating);
            Controls.Add(labelHeight);
            Controls.Add(numericUpDownHeight);
            Controls.Add(labelWidth);
            Controls.Add(numericUpDownWidth);
            Controls.Add(buttonCreate);
            Controls.Add(label2);
            Controls.Add(labelPrompt);
            Controls.Add(textBoxPrompt);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateWithAIForm";
            ShowIcon = false;
            Text = "Create Image With AI";
            FormClosed += CreateWithAIForm_FormClosed;
            Load += CreateWithAIForm_Load;
            ((System.ComponentModel.ISupportInitialize)numericUpDownWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownHeight).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxPrompt;
        private Label labelPrompt;
        private Button buttonCreate;
        private ImageList icons;
        private Label label2;
        private NumericUpDown numericUpDownWidth;
        private Label labelWidth;
        private NumericUpDown numericUpDownHeight;
        private Label labelHeight;
        private ProgressBar progressBarImageCreating;
        private Label labelImageCreating;
        private Button buttonUploadImage;
        private Label labelUploadedImage;
        private OpenFileDialog fileBrowser;
    }
}