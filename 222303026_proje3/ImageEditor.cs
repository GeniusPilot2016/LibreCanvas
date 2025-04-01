using System.Drawing.Text;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class ImageEditor : Form
    {
        int x = -1, y = -1;
        bool isdrawing = false;
        static String[] drawType = { "Cursor", "Selection", "Magic Selection", "Brush", "Pen", "Eraser", "Bucket", "Spray", "Line", "Round", "Rectangle", "Rounded Rectangle", "Triangle", "Hexagon", "Text", "Color Drop" };
        String currentTool = drawType[0];
        Pen brushes;
        Color color1 = Color.Black, color2 = Color.White;
        int brushSize = 11, penSize = 9, eraserSize = 11, shapeThickness = 11, textSize = 9;
        int zoom = 100;
        bool isResizing = false;
        private ResizeDirection resizeDirection;
        private Point lastMousePos;
        Bitmap bitmap;

        private enum ResizeDirection
        {
            None,
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        Panel[] resizeHandles;
        FontFamily[] fontFamilies;
        InstalledFontCollection installedFontCollection = new InstalledFontCollection();

        public ImageEditor(int width = 800, int height = 600)
        {
            InitializeComponent();
            comboBoxBrushType.SelectedIndex = 0;
            comboBoxPenType.SelectedIndex = 0;
            brushSize = Convert.ToInt32(comboBoxBrushSize.SelectedItem);
            eraserSize = Convert.ToInt32(comboBoxEraserSize.SelectedItem);
            penSize = Convert.ToInt32(comboBoxPenSize.SelectedItem);
            shapeThickness = Convert.ToInt32(comboBoxShapeThickness.SelectedItem);

            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            panel1.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(width + 20, height + 20);

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            panel1.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            canvasPanel.Refresh();
            bitmap = new Bitmap(width, height);
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            fontFamilies = installedFontCollection.Families;
            for (int i = 0; i < fontFamilies.Length; i++)
            {
                fontsComboBox.Items.Add(fontFamilies[i].Name);
            }
        }
        private void CenterCanvasPanel()
        {
            int centerX = (panel1.ClientSize.Width - canvasPanel.Width) / 2;
            int centerY = (panel1.ClientSize.Height - canvasPanel.Height) / 2;
            canvasPanel.Location = new Point(Math.Max(centerX, 0), Math.Max(centerY, 0));
        }

        private void removeObjectToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void foregroundColorButton_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = color1;
            DialogResult dialogResult = colorDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                color1 = colorDialog1.Color;
                foregroundColorButton.BackColor = color1;
            }
        }

        private void backgroundColorButton_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = color2;
            DialogResult dialogResult = colorDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                color2 = colorDialog1.Color;
                backgroundColorButton.BackColor = color2;
            }
        }

        private void hakkýndaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }
        private void ToolStripButtonsClick(ToolStripButton toolStripButton)
        {
            // Uncheck all ToolStripMenuItems when the ToolStripButton is checked
            if (toolStripButton.Checked)
            {
                foreach (ToolStripItem item in drawShapeTool.DropDownItems)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        menuItem.Checked = false;
                    }
                }
            }

            // Uncheck all ToolStripButtons in the same ToolStrip
            if (toolStripButton.Owner is ToolStrip toolStrip)
            {
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripButton button && button != toolStripButton) // Avoid unchecking the button that called the function.
                    {
                        button.Checked = false;
                    }
                }
            }
        }

        private void ToolStripMenuItemsClick(object sender)
        {
            ToolStripMenuItem clickedMenuItem = (ToolStripMenuItem)sender;

            // Uncheck other ToolStripMenuItems
            foreach (ToolStripItem item in clickedMenuItem.Owner.Items)
            {
                if (item is ToolStripMenuItem menuItem2 && menuItem2 != clickedMenuItem)
                {
                    menuItem2.Checked = false;
                }
            }

            // Uncheck all ToolStripButtons in the same ToolStrip
            if (clickedMenuItem.Owner is ToolStripDropDown dropDown)
            {
                ToolStrip toolStrip = dropDown.OwnerItem.GetCurrentParent();
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripButton button)
                    {
                        button.Checked = false;
                    }
                }
            }
        }

        private void Control_Clicked(object sender, EventArgs e)
        {
        }

        private void selectTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(selectTool);
        }

        private void magicSelectTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(magicSelectTool);
        }

        private void brushTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(brushTool);
        }

        private void eraserTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(eraserTool);
        }

        private void bucketTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(bucketTool);
        }

        private void sprayTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(sprayTool);
        }

        private void lineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void roundToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void rectangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void roundedRectangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void triangleToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void hexagonToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(addTextTool);
        }

        private void createImageToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void generativeEraserToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void removeBackgroundToolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(colorDropTool);
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void mouseTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(mouseTool);
        }

        private void drawShapeTool_Click(object sender, EventArgs e)
        {

        }

        private void createWithAITool_Click(object sender, EventArgs e)
        {

        }


        private void kaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                pictureBoxCanvas.Image.Save(saveFileDialog1.FileName);
            }
        }

        private void addTextTool_CheckedChanged(object sender, EventArgs e)
        {
            if (addTextTool.Checked)
            {
                currentTool = drawType[14];
                toolStripText.Visible = true;
            }
            else
            {
                toolStripText.Visible = false;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        /*private void PositionResizeHandles()
        {
            // Panelin yeni boyutlarýný al
            int panelLeft = canvasPanel.Left;
            int panelTop = canvasPanel.Top;
            int panelWidth = canvasPanel.Width;
            int panelHeight = canvasPanel.Height;

            // Tutmaçlarýn konumlarýný panelin boyutlarýna göre güncelle
            resize_top_left.Location = new Point(panelLeft - resize_top_left.Width / 2, panelTop - resize_top_left.Height / 2);
            resize_top.Location = new Point(panelLeft + (panelWidth / 2) - resize_top.Width / 2, panelTop - resize_top.Height / 2);
            resize_top_right.Location = new Point(panelLeft + panelWidth - resize_top_right.Width / 2, panelTop - resize_top_right.Height / 2);

            resize_left.Location = new Point(panelLeft - resize_left.Width / 2, panelTop + (panelHeight / 2) - resize_left.Height / 2);
            resize_right.Location = new Point(panelLeft + panelWidth - resize_right.Width / 2, panelTop + (panelHeight / 2) - resize_right.Height / 2);

            resize_bottom_left.Location = new Point(panelLeft - resize_bottom_left.Width / 2, panelTop + panelHeight - resize_bottom_left.Height / 2);
            resize_bottom.Location = new Point(panelLeft + (panelWidth / 2) - resize_bottom.Width / 2, panelTop + panelHeight - resize_bottom.Height / 2);
            resize_bottom_right.Location = new Point(panelLeft + panelWidth - resize_bottom_right.Width / 2, panelTop + panelHeight - resize_bottom_right.Height / 2);
        }*/

        private void resize_MouseUp(object sender, MouseEventArgs e)
        {
            labelSize.Text = $"{canvasPanel.Width} X {canvasPanel.Height}px";
            panel1.AutoScrollMinSize = canvasPanel.Size;
            toolStripResize.Visible = false;
            toolStripSeparator16.Visible = false;
            isResizing = false;
        }

        private void resize_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                int deltaX = e.X - lastMousePos.X;
                int deltaY = e.Y - lastMousePos.Y;

                int newWidth = canvasPanel.Width;
                int newHeight = canvasPanel.Height;

                switch (resizeDirection)
                {
                    case ResizeDirection.TopLeft:
                        newWidth = canvasPanel.Width - deltaX;
                        newHeight = canvasPanel.Height - deltaY;
                        canvasPanel.Location = new Point(canvasPanel.Left + deltaX, canvasPanel.Top + deltaY);
                        break;
                    case ResizeDirection.Top:
                        newHeight = canvasPanel.Height - deltaY;
                        canvasPanel.Location = new Point(canvasPanel.Left, canvasPanel.Top + deltaY);
                        break;
                    case ResizeDirection.TopRight:
                        newWidth = canvasPanel.Width + deltaX;
                        newHeight = canvasPanel.Height - deltaY;
                        canvasPanel.Location = new Point(canvasPanel.Left, canvasPanel.Top + deltaY);
                        break;
                    case ResizeDirection.Left:
                        newWidth = canvasPanel.Width - deltaX;
                        canvasPanel.Location = new Point(canvasPanel.Left + deltaX, canvasPanel.Top);
                        break;
                    case ResizeDirection.Right:
                        newWidth = canvasPanel.Width + deltaX;
                        break;
                    case ResizeDirection.BottomLeft:
                        newWidth = canvasPanel.Width - deltaX;
                        newHeight = canvasPanel.Height + deltaY;
                        canvasPanel.Location = new Point(canvasPanel.Left + deltaX, canvasPanel.Top);
                        break;
                    case ResizeDirection.Bottom:
                        newHeight = canvasPanel.Height + deltaY;
                        break;
                    case ResizeDirection.BottomRight:
                        newWidth = canvasPanel.Width + deltaX;
                        newHeight = canvasPanel.Height + deltaY;
                        break;
                }

                // Minimum boyutlarý kontrol et
                newWidth = Math.Max(newWidth, 1);
                newHeight = Math.Max(newHeight, 1);

                // Yeni boyutlarý uygula
                canvasPanel.Size = new Size(newWidth, newHeight);

                // panel1'in AutoScrollMinSize özelliðini güncelle
                panel1.AutoScrollMinSize = canvasPanel.Size;

                // Bitmap'i yeniden boyutlandýr
                Bitmap newBitmap = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.DrawImage(bitmap, 0, 0);
                }
                bitmap = newBitmap;

                // Paneli merkezi konumda yerleþtiriyoruz
                CenterCanvasPanel();

                // Paneli hemen yenile (Refresh kullan)
                canvasPanel.Refresh();

                lastMousePos = e.Location;
                toolStripResize.Text = $"{canvasPanel.Width} X {canvasPanel.Height}px";
            }
        }

        private void resize_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender == resize_top_left)
                resizeDirection = ResizeDirection.TopLeft;
            else if (sender == resize_top)
                resizeDirection = ResizeDirection.Top;
            else if (sender == resize_top_right)
                resizeDirection = ResizeDirection.TopRight;
            else if (sender == resize_left)
                resizeDirection = ResizeDirection.Left;
            else if (sender == resize_right)
                resizeDirection = ResizeDirection.Right;
            else if (sender == resize_bottom_left)
                resizeDirection = ResizeDirection.BottomLeft;
            else if (sender == resize_bottom)
                resizeDirection = ResizeDirection.Bottom;
            else if (sender == resize_bottom_right)
                resizeDirection = ResizeDirection.BottomRight;

            // Resizing iþlemi baþladýðýnda
            isResizing = true;
            toolStripResize.Visible = true;
            toolStripSeparator16.Visible = true;
            toolStripResize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            lastMousePos = e.Location;
        }
        private void brushTool_CheckedChanged(object sender, EventArgs e)
        {
            if (brushTool.Checked)
            {
                currentTool = drawType[3];
                toolStripBrush.Visible = true;
            }
            else
            {
                toolStripBrush.Visible = false;
            }
        }

        private void penTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(penTool);
        }

        private void penTool_CheckedChanged(object sender, EventArgs e)
        {
            if (penTool.Checked)
            {
                currentTool = drawType[4];
                toolStripPen.Visible = true;
            }
            else
            {
                toolStripPen.Visible = false;
            }
        }

        private void fontsComboBox_Click(object sender, EventArgs e)
        {

        }

        private void fontsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            toolStripSample.Font = new Font(fontFamilies[fontsComboBox.SelectedIndex], 12);
        }

        private void mouseTool_CheckedChanged(object sender, EventArgs e)
        {
            if (mouseTool.Checked)
            {
                currentTool = drawType[0];
            }
        }

        private void selectTool_CheckedChanged(object sender, EventArgs e)
        {
            if (selectTool.Checked)
            {
                currentTool = drawType[1];
            }
        }

        private void magicSelectTool_CheckedChanged(object sender, EventArgs e)
        {
            if (magicSelectTool.Checked)
            {
                currentTool = drawType[2];
            }
        }

        private void eraserTool_CheckedChanged(object sender, EventArgs e)
        {
            if (eraserTool.Checked)
            {
                currentTool = drawType[5];
                toolStripEraser.Visible = true;
            }
            else
            {
                toolStripEraser.Visible = false;
            }
        }

        private void bucketTool_CheckedChanged(object sender, EventArgs e)
        {
            if (bucketTool.Checked)
            {
                currentTool = drawType[6];
            }
        }

        private void sprayTool_CheckedChanged(object sender, EventArgs e)
        {
            if (sprayTool.Checked)
            {
                currentTool = drawType[7];
            }
        }

        private void lineToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (lineToolStripMenuItem.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[8];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void roundToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (roundToolStripMenuItem1.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[9];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void rectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (rectangleToolStripMenuItem.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[10];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void roundedRectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (roundedRectangleToolStripMenuItem.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[11];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void triangleToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (triangleToolStripMenuItem1.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[12];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void hexagonToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (hexagonToolStripMenuItem1.Checked)
            {
                toolStripShapes.Visible = true;
                currentTool = drawType[13];
            }
            else
            {
                toolStripShapes.Visible = false;
            }
        }

        private void colorDropTool_CheckedChanged(object sender, EventArgs e)
        {
            if (colorDropTool.Checked)
            {
                currentTool = drawType[15];
            }
        }

        private void pictureBoxCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isdrawing == true)
            {
                isdrawing = false;
                x = -1;
                y = -1;
            }
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isdrawing)
            {
                drawIntoCanvas(e);
            }
            int x = e.X;
            int y = e.Y;
            if ((x < 0 || y < 0) || (x > pictureBoxCanvas.Width || y > pictureBoxCanvas.Height))
            {
                toolStripSeparator15.Visible = false;
                labelCanvasPositon.Visible = false;
            }
            else
            {
                toolStripSeparator15.Visible = true;
                labelCanvasPositon.Visible = true;
                labelCanvasPositon.Text = $"{x}, {y}px";
            }
        }
        private void drawIntoCanvas(MouseEventArgs e)
        {
            if (bitmap == null)
            {
                // Bitmap nesnesi oluþturulmamýþsa, oluþtur
                bitmap = new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                if (isdrawing)
                {
                    switch (currentTool)
                    {
                        case "Brush":
                            using (Pen brushes = new Pen(color1, brushSize))
                            {
                                if (x == -1 && y == -1)
                                {
                                    graphics.FillEllipse(new SolidBrush(color1), e.X, e.Y, brushSize, brushSize);
                                }
                                else
                                {
                                    graphics.FillEllipse(new SolidBrush(color1), e.X, e.Y, brushSize, brushSize);
                                    FillGap(graphics, new SolidBrush(color1), x, y, e.X, e.Y, brushSize);
                                }
                                x = e.X;
                                y = e.Y;
                            }
                            break;
                        case "Pen":
                            using (Pen brushes = new Pen(color1, penSize))
                            {
                                if (x == -1 && y == -1)
                                {
                                    graphics.FillRectangle(new SolidBrush(color1), e.X, e.Y, penSize, penSize);
                                }
                                else
                                {
                                    graphics.FillRectangle(new SolidBrush(color1), e.X, e.Y, penSize, penSize);
                                    FillGap(graphics, new SolidBrush(color1), x, y, e.X, e.Y, penSize);
                                }
                                x = e.X;
                                y = e.Y;
                            }
                            break;
                        case "Eraser":
                            using (Pen brushes = new Pen(color2, eraserSize))
                            {
                                if (x == -1 && y == -1)
                                {
                                    graphics.FillEllipse(new SolidBrush(color2), e.X, e.Y, eraserSize, eraserSize);
                                }
                                else
                                {
                                    graphics.FillEllipse(new SolidBrush(color2), e.X, e.Y, eraserSize, eraserSize);
                                    FillGap(graphics, new SolidBrush(color2), x, y, e.X, e.Y, eraserSize);
                                }
                                x = e.X;
                                y = e.Y;
                            }
                            break;
                        // Diðer araçlar için case bloklarý ekleyin
                        default:
                            break;
                    }
                }
            }

            // pictureBoxCanvas kontrolünü yenile
            pictureBoxCanvas.Image = bitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void FillGap(Graphics graphics, Brush brush, int x1, int y1, int x2, int y2, int size)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (x1 != x2 || y1 != y2)
            {
                graphics.FillEllipse(brush, x1, y1, size, size);
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isdrawing = true;
            x = e.X;
            y = e.Y;
            drawIntoCanvas(e); // Tek týklamada nokta çizmek için
        }

        private void pictureBoxCanvas_MouseLeave(object sender, EventArgs e)
        {
            toolStripSeparator15.Visible = false;
            labelCanvasPositon.Visible = false;
        }

        private void labelSize_Click(object sender, EventArgs e)
        {

        }

        private void pictureBoxCanvas_Click(object sender, EventArgs e)
        {

        }

        private void çýkýþToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
