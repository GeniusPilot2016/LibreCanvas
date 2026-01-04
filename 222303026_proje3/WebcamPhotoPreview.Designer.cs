namespace Carpathia
{
    partial class WebcamPhotoPreview
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WebcamPhotoPreview));
            pictureBox1 = new PictureBox();
            buttonAccept = new Button();
            icons = new ImageList(components);
            buttonDiscard = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(938, 530);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // buttonAccept
            // 
            buttonAccept.Anchor = AnchorStyles.Bottom;
            buttonAccept.DialogResult = DialogResult.OK;
            buttonAccept.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonAccept.ImageIndex = 1;
            buttonAccept.ImageList = icons;
            buttonAccept.Location = new Point(253, 558);
            buttonAccept.Name = "buttonAccept";
            buttonAccept.Size = new Size(127, 43);
            buttonAccept.TabIndex = 0;
            buttonAccept.Text = "Accept";
            buttonAccept.TextAlign = ContentAlignment.MiddleRight;
            buttonAccept.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonAccept.UseVisualStyleBackColor = true;
            buttonAccept.Click += buttonAccept_Click;
            // 
            // icons
            // 
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.ImageStream = (ImageListStreamer)resources.GetObject("icons.ImageStream");
            icons.TransparentColor = Color.Transparent;
            icons.Images.SetKeyName(0, "icons8-cancel-48.png");
            icons.Images.SetKeyName(1, "icons8-accept-48.png");
            // 
            // buttonDiscard
            // 
            buttonDiscard.Anchor = AnchorStyles.Bottom;
            buttonDiscard.DialogResult = DialogResult.Cancel;
            buttonDiscard.Font = new Font("HarmonyOS Sans", 8.999999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonDiscard.ImageIndex = 0;
            buttonDiscard.ImageList = icons;
            buttonDiscard.Location = new Point(565, 563);
            buttonDiscard.Name = "buttonDiscard";
            buttonDiscard.Size = new Size(127, 43);
            buttonDiscard.TabIndex = 1;
            buttonDiscard.Text = "Discard";
            buttonDiscard.TextAlign = ContentAlignment.MiddleRight;
            buttonDiscard.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonDiscard.UseVisualStyleBackColor = true;
            buttonDiscard.Click += buttonDiscard_Click;
            // 
            // WebcamPhotoPreview
            // 
            AcceptButton = buttonAccept;
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            CancelButton = buttonDiscard;
            ClientSize = new Size(962, 618);
            Controls.Add(buttonDiscard);
            Controls.Add(buttonAccept);
            Controls.Add(pictureBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "WebcamPhotoPreview";
            ShowIcon = false;
            Text = "Preview";
            FormClosed += WebcamPhotoPreview_FormClosed;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBox1;
        private Button buttonAccept;
        private ImageList icons;
        private Button buttonDiscard;
    }
}