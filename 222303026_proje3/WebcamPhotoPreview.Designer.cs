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
            pictureBox1.Location = new Point(10, 10);
            pictureBox1.Margin = new Padding(2, 2, 2, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(750, 424);
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
            buttonAccept.Location = new Point(202, 446);
            buttonAccept.Margin = new Padding(2, 2, 2, 2);
            buttonAccept.Name = "buttonAccept";
            buttonAccept.Size = new Size(102, 34);
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
            buttonDiscard.Location = new Point(452, 450);
            buttonDiscard.Margin = new Padding(2, 2, 2, 2);
            buttonDiscard.Name = "buttonDiscard";
            buttonDiscard.Size = new Size(102, 34);
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
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            CancelButton = buttonDiscard;
            ClientSize = new Size(770, 494);
            Controls.Add(buttonDiscard);
            Controls.Add(buttonAccept);
            Controls.Add(pictureBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2, 2, 2, 2);
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