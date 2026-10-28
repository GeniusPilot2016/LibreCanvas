namespace Carpathia
{
    partial class MovingImageControl
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
            resize_bottom_left = new Panel();
            resize_bottom_right = new Panel();
            resize_right = new Panel();
            resize_bottom = new Panel();
            resize_left = new Panel();
            resize_top_left = new Panel();
            resize_top_right = new Panel();
            resize_top = new Panel();
            pictureBoxCanvas = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBoxCanvas).BeginInit();
            SuspendLayout();
            // 
            // resize_bottom_left
            // 
            resize_bottom_left.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            resize_bottom_left.BackColor = Color.White;
            resize_bottom_left.BorderStyle = BorderStyle.FixedSingle;
            resize_bottom_left.Cursor = Cursors.SizeNESW;
            resize_bottom_left.Location = new Point(-1, 199);
            resize_bottom_left.Name = "resize_bottom_left";
            resize_bottom_left.Size = new Size(10, 10);
            resize_bottom_left.TabIndex = 12;
            resize_bottom_left.MouseDown += resize_top_MouseDown;
            resize_bottom_left.MouseMove += resize_bottom_left_MouseMove;
            resize_bottom_left.MouseUp += resize_top_MouseUp;
            // 
            // resize_bottom_right
            // 
            resize_bottom_right.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resize_bottom_right.BackColor = Color.White;
            resize_bottom_right.BorderStyle = BorderStyle.FixedSingle;
            resize_bottom_right.Cursor = Cursors.SizeNWSE;
            resize_bottom_right.Location = new Point(199, 199);
            resize_bottom_right.Name = "resize_bottom_right";
            resize_bottom_right.Size = new Size(10, 10);
            resize_bottom_right.TabIndex = 13;
            resize_bottom_right.MouseDown += resize_top_MouseDown;
            resize_bottom_right.MouseMove += resize_bottom_right_MouseMove;
            resize_bottom_right.MouseUp += resize_top_MouseUp;
            // 
            // resize_right
            // 
            resize_right.Anchor = AnchorStyles.Right;
            resize_right.BackColor = Color.White;
            resize_right.BorderStyle = BorderStyle.FixedSingle;
            resize_right.Cursor = Cursors.SizeWE;
            resize_right.Location = new Point(199, 99);
            resize_right.Name = "resize_right";
            resize_right.Size = new Size(10, 10);
            resize_right.TabIndex = 14;
            resize_right.MouseDown += resize_top_MouseDown;
            resize_right.MouseUp += resize_top_MouseUp;
            resize_right.Move += resize_right_Move;
            // 
            // resize_bottom
            // 
            resize_bottom.Anchor = AnchorStyles.Bottom;
            resize_bottom.BackColor = Color.White;
            resize_bottom.BorderStyle = BorderStyle.FixedSingle;
            resize_bottom.Cursor = Cursors.SizeNS;
            resize_bottom.Location = new Point(99, 199);
            resize_bottom.Name = "resize_bottom";
            resize_bottom.Size = new Size(10, 10);
            resize_bottom.TabIndex = 15;
            resize_bottom.MouseDown += resize_top_MouseDown;
            resize_bottom.MouseMove += resize_bottom_MouseMove;
            resize_bottom.MouseUp += resize_top_MouseUp;
            // 
            // resize_left
            // 
            resize_left.Anchor = AnchorStyles.Left;
            resize_left.BackColor = Color.White;
            resize_left.BorderStyle = BorderStyle.FixedSingle;
            resize_left.Cursor = Cursors.SizeWE;
            resize_left.Location = new Point(-1, 99);
            resize_left.Name = "resize_left";
            resize_left.Size = new Size(10, 10);
            resize_left.TabIndex = 16;
            resize_left.MouseDown += resize_top_MouseDown;
            resize_left.MouseMove += resize_left_MouseMove;
            resize_left.MouseUp += resize_top_MouseUp;
            // 
            // resize_top_left
            // 
            resize_top_left.BackColor = Color.White;
            resize_top_left.BorderStyle = BorderStyle.FixedSingle;
            resize_top_left.Cursor = Cursors.SizeNWSE;
            resize_top_left.Location = new Point(-1, -1);
            resize_top_left.Name = "resize_top_left";
            resize_top_left.Size = new Size(10, 10);
            resize_top_left.TabIndex = 17;
            resize_top_left.MouseDown += resize_top_MouseDown;
            resize_top_left.MouseMove += resize_top_left_MouseMove;
            resize_top_left.MouseUp += resize_top_MouseUp;
            // 
            // resize_top_right
            // 
            resize_top_right.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            resize_top_right.BackColor = Color.White;
            resize_top_right.BorderStyle = BorderStyle.FixedSingle;
            resize_top_right.Cursor = Cursors.SizeNESW;
            resize_top_right.Location = new Point(199, -1);
            resize_top_right.Name = "resize_top_right";
            resize_top_right.Size = new Size(10, 10);
            resize_top_right.TabIndex = 18;
            resize_top_right.MouseDown += resize_top_MouseDown;
            resize_top_right.MouseMove += resize_top_right_MouseMove;
            resize_top_right.MouseUp += resize_top_MouseUp;
            // 
            // resize_top
            // 
            resize_top.Anchor = AnchorStyles.Top;
            resize_top.BackColor = Color.White;
            resize_top.BorderStyle = BorderStyle.FixedSingle;
            resize_top.Cursor = Cursors.SizeNS;
            resize_top.Location = new Point(99, -1);
            resize_top.Name = "resize_top";
            resize_top.Size = new Size(10, 10);
            resize_top.TabIndex = 19;
            resize_top.MouseDown += resize_top_MouseDown;
            resize_top.MouseMove += resize_top_MouseMove;
            resize_top.MouseUp += resize_top_MouseUp;
            // 
            // pictureBoxCanvas
            // 
            pictureBoxCanvas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBoxCanvas.BackColor = Color.Transparent;
            pictureBoxCanvas.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxCanvas.Cursor = Cursors.SizeAll;
            pictureBoxCanvas.Location = new Point(4, 4);
            pictureBoxCanvas.MinimumSize = new Size(1, 1);
            pictureBoxCanvas.Name = "pictureBoxCanvas";
            pictureBoxCanvas.Size = new Size(200, 200);
            pictureBoxCanvas.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxCanvas.TabIndex = 11;
            pictureBoxCanvas.TabStop = false;
            pictureBoxCanvas.MouseDown += resize_top_MouseDown;
            pictureBoxCanvas.MouseUp += resize_top_MouseUp;
            // 
            // MovingImageControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            Controls.Add(resize_bottom_left);
            Controls.Add(resize_bottom_right);
            Controls.Add(resize_right);
            Controls.Add(resize_bottom);
            Controls.Add(resize_left);
            Controls.Add(resize_top_left);
            Controls.Add(resize_top_right);
            Controls.Add(resize_top);
            Controls.Add(pictureBoxCanvas);
            Name = "MovingImageControl";
            Size = new Size(208, 208);
            ((System.ComponentModel.ISupportInitialize)pictureBoxCanvas).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel resize_bottom_left;
        private Panel resize_bottom_right;
        private Panel resize_right;
        private Panel resize_bottom;
        private Panel resize_left;
        private Panel resize_top_left;
        private Panel resize_top_right;
        private Panel resize_top;
        private PictureBox pictureBoxCanvas;
    }
}
