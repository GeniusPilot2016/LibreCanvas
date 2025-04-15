using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO.Enumeration;
using System.Windows.Forms;
using System.Windows.Forms.Design;

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
        int brushSize = 11, penSize = 9, eraserSize = 11, sprayToolSize = 11, shapeThickness = 11, radius = 9, points = 6;
        float textSize = 9;
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

        public ImageEditor()
        {
            InitializeComponentAndFont();
            createNewFile();
            UpdateUndoRedoButtons(); // Baþlangýçta tuþlarý güncelle
        }
        public ImageEditor(String fileName)
        {
            InitializeComponentAndFont();
            openAFile(fileName);
        }
        public ImageEditor(Image image, bool AIGenerated)
        {
            InitializeComponentAndFont();
            createFileWithAIorWebcam(image, AIGenerated);
        }
        private void InitializeComponentAndFont()
        {
            InitializeComponent();
            comboBoxBrushType.SelectedIndex = 0;
            comboBoxPenType.SelectedIndex = 0;
            brushSize = Convert.ToInt32(comboBoxBrushSize.SelectedItem);
            eraserSize = Convert.ToInt32(comboBoxEraserSize.SelectedItem);
            penSize = Convert.ToInt32(comboBoxPenSize.SelectedItem);
            shapeThickness = Convert.ToInt32(comboBoxShapeThickness.SelectedItem);
            fontFamilies = installedFontCollection.Families;
            for (int i = 0; i < fontFamilies.Length; i++)
            {
                fontsComboBox.Items.Add(fontFamilies[i].Name);
            }
        }
        private void createNewFile()
        {
            pictureBoxCanvas.Image = null;
            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            panel1.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(820, 620);

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            panel1.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            canvasPanel.Refresh();
            bitmap = new Bitmap(800, 600);
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = "Unnamed File";
        }
        private void openAFile(string fileName)
        {
            pictureBoxCanvas.Image = null;
            Image image = Image.FromFile(fileName);
            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            panel1.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = image.Size;

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            panel1.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            bitmap = new Bitmap(image);
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = fileName;
        }
        private void createFileWithAIorWebcam(Image generatedImage, bool isAIGenerated)
        {
            pictureBoxCanvas.Image = null;
            Image image = generatedImage;
            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            panel1.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(generatedImage.Width + 20, generatedImage.Height + 20);

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            panel1.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            bitmap = new Bitmap(image);
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            switch (isAIGenerated)
            {
                case true:
                    labelFileName.Text = "AI Generated File";
                    break;
                case false:
                    labelFileName.Text = "Unnamed File";
                    break;
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
            if (lineToolStripMenuItem.Checked == false)
            {
                lineToolStripMenuItem.Checked = true;
            }
        }

        private void roundToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
            if (roundToolStripMenuItem1.Checked == false)
            {
                roundToolStripMenuItem1.Checked = true;
            }
        }

        private void rectangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
            if (rectangleToolStripMenuItem.Checked == false)
            {
                rectangleToolStripMenuItem.Checked = true;
            }
        }

        private void roundedRectangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
            if (roundedRectangleToolStripMenuItem.Checked == false)
            {
                roundedRectangleToolStripMenuItem.Checked = true;
            }
        }

        private void triangleToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
            if (triangleToolStripMenuItem1.Checked == false)
            {
                triangleToolStripMenuItem1.Checked = true;
            }
        }

        private void hexagonToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripMenuItemsClick(sender);
            if (hexagonToolStripMenuItem1.Checked == false)
            {
                hexagonToolStripMenuItem1.Checked = true;
            }
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
            saveFileDialog1.Filter = "PNG Files|*.png|JPEG Files|*.jpg|Bitmap Files|*.bmp";
            DialogResult dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                bitmap.Save(saveFileDialog1.FileName);
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
                toolStripSpray.Visible = true;
            }
            else
            {
                toolStripSpray.Visible = false;
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
        private void UpdateToolStripVisibility()
        {
            toolStripShapes.Visible = lineToolStripMenuItem.Checked ||
                                      roundToolStripMenuItem1.Checked ||
                                      rectangleToolStripMenuItem.Checked ||
                                      roundedRectangleToolStripMenuItem.Checked ||
                                      triangleToolStripMenuItem1.Checked ||
                                      hexagonToolStripMenuItem1.Checked;

            Debug.WriteLine($"Round Checked: {roundToolStripMenuItem1.Checked}, ToolStrip Visible: {toolStripShapes.Visible}");

            labelRadius.Visible = roundedRectangleToolStripMenuItem.Checked;
            textBoxRadius.Visible = roundedRectangleToolStripMenuItem.Checked;
            toolStripSeparator26.Visible = roundedRectangleToolStripMenuItem.Checked;

            labelPoint.Visible = hexagonToolStripMenuItem1.Checked;
            textBoxPoints.Visible = hexagonToolStripMenuItem1.Checked;
            toolStripSeparator24.Visible = hexagonToolStripMenuItem1.Checked;
        }
        private void roundToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (roundToolStripMenuItem1.Checked)
            {
                currentTool = drawType[9];
            }
            UpdateToolStripVisibility();
        }

        private void rectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (rectangleToolStripMenuItem.Checked)
            {
                currentTool = drawType[10];
            }
            UpdateToolStripVisibility();
        }

        private void roundedRectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (roundedRectangleToolStripMenuItem.Checked)
            {
                currentTool = drawType[11];
            }
            UpdateToolStripVisibility();
        }

        private void triangleToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (triangleToolStripMenuItem1.Checked)
            {
                currentTool = drawType[12];
            }
            UpdateToolStripVisibility();
        }

        private void hexagonToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (hexagonToolStripMenuItem1.Checked)
            {
                currentTool = drawType[13];
            }
            UpdateToolStripVisibility();
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
            if (isdrawing)
            {
                isdrawing = false;

                switch (currentTool)
                {
                    case "Line":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawLineOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case "Round":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawRoundOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case "Rectangle":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawRectangleOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case "Rounded Rectangle":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawRoundedRectangleOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location, radius);
                        }
                        break;
                    case "Triangle":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawTriangleOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case "Hexagon":
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            DrawShapes.DrawHexagonOnCanvas(bitmap, pictureBoxCanvas, color1, shapeThickness, points, startPoint, e.Location);
                        }
                        break;
                    default:
                        drawIntoCanvas(e);
                        break;
                }
                // Update the PictureBox with the new bitmap
                pictureBoxCanvas.Image = bitmap;
                pictureBoxCanvas.Invalidate();
            }
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (isdrawing)
            {
                switch (currentTool)
                {
                    case "Line":
                        pictureBoxCanvas.Image = ShapePreviews.LinePreview(
                            bitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case "Round":
                        pictureBoxCanvas.Image = ShapePreviews.RoundPreview(
                            bitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y
                        );
                        pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case "Rectangle":
                        pictureBoxCanvas.Image = ShapePreviews.RectanglePreview(
                            bitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case "Rounded Rectangle":
                        pictureBoxCanvas.Image = ShapePreviews.RoundedRectanglePreview(
                            bitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y, radius
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case "Triangle":
                        pictureBoxCanvas.Image = ShapePreviews.TrianglePreview(
                            bitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case "Hexagon":
                        pictureBoxCanvas.Image = ShapePreviews.HexagonPreview(
                            bitmap, color1, shapeThickness, points, previewStartPoint.X, previewStartPoint.Y, e.X, e.Y);
                        pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    default:
                        drawIntoCanvas(e);
                        break;
                }
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
                bitmap = new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                if (isdrawing)
                {
                    switch (currentTool)
                    {
                        case "Brush":
                            switch (comboBoxBrushType.SelectedIndex)
                            {
                                case 0: // Regular brush
                                    DrawBrush(graphics, BrushShapes.DrawCircleBrush, color1, brushSize, new Point(e.X, e.Y));
                                    break;
                                case 1: // Oil brush
                                    DrawBrush(graphics, BrushShapes.DrawOilBrush, color1, brushSize, new Point(e.X, e.Y));
                                    break;
                                case 2: // Calligraphy brush
                                    DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point(e.X, e.Y));
                                    break;
                                case 3:
                                    DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point(e.X, e.Y));
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "Pen":
                            switch (comboBoxPenType.SelectedIndex)
                            {
                                case 0: // Regular pen
                                    DrawBrush(graphics, BrushShapes.DrawSquareBrush, color1, penSize, new Point(e.X, e.Y));
                                    break;
                                case 1:
                                    DrawBrush(graphics, BrushShapes.DrawMarkerBrush, color1, penSize, new Point(e.X, e.Y));
                                    break;
                                case 2:
                                    DrawBrush(graphics, BrushShapes.DrawCrayonBrush, color1, penSize, new Point(e.X, e.Y));
                                    break;
                                case 3: // Calligraphy pen
                                    DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point(e.X, e.Y));
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "Eraser":
                            DrawBrush(graphics, BrushShapes.DrawCircleBrush, color2, eraserSize, new Point(e.X, e.Y));
                            break;
                        case "Spray":
                            DrawBrush(graphics, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point(e.X, e.Y));
                            break;
                        default:
                            break;
                    }

                    x = e.X;
                    y = e.Y;
                }
            }

            pictureBoxCanvas.Image = bitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void DrawBrush(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point location)
        {
            if (x == -1 && y == -1)
            {
                drawAction(graphics, color, size, location);
            }
            else
            {
                FillGap(graphics, drawAction, color, size, new Point(x, y), location);
            }
        }

        private void FillGap(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point start, Point end)
        {
            int dx = Math.Abs(end.X - start.X);
            int dy = Math.Abs(end.Y - start.Y);
            int sx = start.X < end.X ? 1 : -1;
            int sy = start.Y < end.Y ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                drawAction(graphics, color, size, start);

                if (start.X == end.X && start.Y == end.Y) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    start.X += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    start.Y += sy;
                }
            }
        }

        private Point startPoint;
        private Point previewStartPoint; // Origin of the preview shape
        private Bitmap previewBitmap;    // Temporary bitmap for previewing shapes
        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();
        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isdrawing = true;
            x = e.X;
            y = e.Y;
            switch (currentTool)
            {
                case "Line":
                    SaveStateForUndo();
                    startPoint = e.Location;
                    previewStartPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                case "Round":
                    SaveStateForUndo();
                    startPoint = e.Location;
                    previewStartPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                case "Rectangle":
                    SaveStateForUndo();
                    previewStartPoint = e.Location;
                    startPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                case "Rounded Rectangle":
                    SaveStateForUndo();
                    startPoint = e.Location;
                    previewStartPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                case "Triangle":
                    SaveStateForUndo();
                    startPoint = e.Location;
                    previewStartPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                case "Hexagon":
                    SaveStateForUndo();
                    startPoint = e.Location;
                    previewStartPoint = e.Location;
                    previewBitmap = new Bitmap(bitmap);
                    break;
                default:
                    SaveStateForUndo();
                    drawIntoCanvas(e); // To draw immediately on mouse down
                    break;
            }
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
            Application.Exit();
        }

        private void ImageEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void açToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "PNG Files|*.png|JPEG Files|*.jpg|Bitmap Files|*.bmp";
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                openAFile(file);
            }
        }

        private void yeniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createNewFile();
        }

        private void createWithAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateWithAIForm createWithAIForm = new CreateWithAIForm();
            createWithAIForm.ShowDialog();
            var generatedImage = createWithAIForm.GetGeneratedImage();
            if (generatedImage != null)
            {
                createFileWithAIorWebcam(generatedImage, true);
            }
        }

        private void createWithWebcamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TakePhotoFromWebcam takePhotoFromWebcam = new TakePhotoFromWebcam();
            takePhotoFromWebcam.PhotoAccepted += TakePhotoFromWebcam_PhotoAccepted;
            takePhotoFromWebcam.ShowDialog(this);
        }

        private void TakePhotoFromWebcam_PhotoAccepted(object sender, ImageAcceptedEventArgs e)
        {
            // Directly update the canvas with the accepted image
            createFileWithAIorWebcam(e.AcceptedImage, false);
        }

        private void comboBoxSpraySize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                sprayToolSize = Convert.ToInt32(comboBoxSpraySize.Text);
                if (sprayToolSize <= 0)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                sprayToolSize = 11;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                sprayToolSize = 11;
            }
            finally
            {
                comboBoxSpraySize.Text = sprayToolSize.ToString();
            }
        }

        private void comboBoxSpraySize_SelectedIndexChanged(object sender, EventArgs e)
        {
            sprayToolSize = Convert.ToInt32(comboBoxSpraySize.SelectedItem);
        }

        private void comboBoxBrushSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            brushSize = Convert.ToInt32(comboBoxBrushSize.SelectedItem);
        }

        private void comboBoxBrushSize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                brushSize = Convert.ToInt32(comboBoxBrushSize.Text);
                if (brushSize <= 0)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                brushSize = 11;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                brushSize = 11;
            }
            finally
            {
                comboBoxBrushSize.Text = brushSize.ToString();
            }
        }

        private void comboBoxEraserSize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                eraserSize = Convert.ToInt32(comboBoxEraserSize.Text);
                if (eraserSize <= 0)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                eraserSize = 11;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                eraserSize = 11;
            }
            finally
            {
                comboBoxEraserSize.Text = eraserSize.ToString();
            }
        }

        private void comboBoxEraserSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            eraserSize = Convert.ToInt32(comboBoxEraserSize.SelectedItem);
        }

        private void comboBoxPenSize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                penSize = Convert.ToInt32(comboBoxPenSize.Text);
                if (penSize <= 0)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                penSize = 9;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                penSize = 9;
            }
            finally
            {
                comboBoxPenSize.Text = penSize.ToString();
            }
        }

        private void comboBoxPenSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            penSize = Convert.ToInt32(comboBoxPenSize.SelectedItem);
        }

        private void comboBoxShapeThickness_TextChanged(object sender, EventArgs e)
        {
            try
            {
                shapeThickness = Convert.ToInt32(comboBoxShapeThickness.Text);
                if (shapeThickness <= 0)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                shapeThickness = 11;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                shapeThickness = 11;
            }
            finally
            {
                comboBoxShapeThickness.Text = shapeThickness.ToString();
            }
        }

        private void comboBoxShapeThickness_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void fontSizeComboBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                textSize = (float)Convert.ToDouble(fontSizeComboBox.Text);
                if (textSize < 8)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                textSize = 9;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                textSize = 9;
            }
            finally
            {
                fontSizeComboBox.Text = textSize.ToString();
            }
        }

        private void fontSizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            textSize = (float)Convert.ToDouble(fontSizeComboBox.SelectedItem);
        }

        private void textBoxRadius_Click(object sender, EventArgs e)
        {

        }

        private void textBoxRadius_TextChanged(object sender, EventArgs e)
        {
            try
            {
                radius = Convert.ToInt32(textBoxRadius.Text);
                if (radius < 1 || radius > 255)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                radius = 9;
                textBoxRadius.Text = radius.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                radius = 9;
                textBoxRadius.Text = radius.ToString();
            }
        }

        private void textBoxPoints_TextChanged(object sender, EventArgs e)
        {
            try
            {
                points = Convert.ToInt32(textBoxPoints.Text);
                if (points < 5 || points > 255)
                {
                    throw new OverflowException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                points = 6;
                textBoxPoints.Text = points.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                points = 6;
                textBoxPoints.Text = points.ToString();
            }
        }
        private const int MaxStackSize = 20;

        private void SaveStateForUndo()
        {
            if (bitmap != null)
            {
                if (undoStack.Count >= MaxStackSize)
                {
                    // En eski durumu kaldýr
                    var tempList = undoStack.ToList();
                    tempList.RemoveAt(0);
                    undoStack = new Stack<Bitmap>(tempList);
                }
                undoStack.Push(new Bitmap(bitmap));
                redoStack.Clear(); // Yeni iþlemde redo yýðýný temizlenir
            }
            UpdateUndoRedoButtons(); // Tuþlarý güncelle
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new Bitmap(bitmap)); // Mevcut durumu redo yýðýnýna kaydet
                bitmap = undoStack.Pop(); // Son durumu geri yükle
                pictureBoxCanvas.Image = bitmap;
                pictureBoxCanvas.Invalidate();
            }
            UpdateUndoRedoButtons(); // Tuþlarý güncelle
        }
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new Bitmap(bitmap)); // Mevcut durumu undo yýðýnýna kaydet
                bitmap = redoStack.Pop(); // Sonraki durumu geri yükle
                pictureBoxCanvas.Image = bitmap;
                pictureBoxCanvas.Invalidate();
            }
            UpdateUndoRedoButtons(); // Tuþlarý güncelle
        }

        private void geriAlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void yineleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redo();
        }
        private void UpdateUndoRedoButtons()
        {
            // Undo tuþunu yýðýn doluysa etkinleþtir, boþsa devre dýþý býrak
            geriAlToolStripMenuItem.Enabled = undoStack.Count > 0;

            // Redo tuþunu yýðýn doluysa etkinleþtir, boþsa devre dýþý býrak
            yineleToolStripMenuItem.Enabled = redoStack.Count > 0;
        }
    }
    public partial class CreateWithAIForm : Form
    {
        public Image GetGeneratedImage()
        {
            return image;
        }
    }
    
}
