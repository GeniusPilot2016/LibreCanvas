using _222303026_proje3.Properties;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Media;
using System.Media;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class ImageEditor : Form
    {
        int x = -1, y = -1;
        bool isdrawing = false;
        Color color1 = Color.Black, color2 = Color.White;
        int brushSize = 11, penSize = 9, eraserSize = 11, sprayToolSize = 11, shapeThickness = 11, radius = 9, points = 6, tolerance = 50;
        float textSize = 9;
        float zoom = 1;
        bool isResizing = false, isSelected = false;
        private ResizeDirection resizeDirection;
        private BasicFilters basicFilters;
        private ArtisticFilters artisticFilters;
        private Point lastMousePos;
        Size originalSize; // Unzoomed size
        Size originalSelectionRectangleSize; // Unzoomed size
        Point originalSelectionRectangleLocation; // Unzoomed location
        Bitmap MainBitmap;
        Bitmap SelectedBitmap;
        Rectangle SelectionRectangle = new Rectangle();
        Point SelectionStartPoint;
        enum BasicFilters
        {
            None,
            Mirror,
            Flash,
            Frozen,
            Winter,
            BlackAndWhite,
            OldPhoto,
            Cherry,
            LightAdd,
            Purple,
            Fog
        }
        enum ArtisticFilters
        {
            None,
            OilPainting,
            Cartoon
        }
        enum Tools
        {
            Cursor,
            Selection,
            MagicSelection,
            Brush,
            Pen,
            Eraser,
            Bucket,
            Spray,
            Line,
            Round,
            Rectangle,
            RoundedRectangle,
            Triangle,
            Hexagon,
            Text,
            ColorDrop
        }
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
            InitializeSelectionPen(); // Initialize the SelectionPen
            createNewFile();
            UpdateUndoRedoButtons(); // Baþlangýçta tuþlarý güncelle
        }
        public ImageEditor(String fileName)
        {
            InitializeComponentAndFont();
            InitializeSelectionPen(); // Initialize the SelectionPen
            openAFile(fileName);
        }
        public ImageEditor(Image image, bool AIGenerated)
        {
            InitializeComponentAndFont();
            InitializeSelectionPen(); // Initialize the SelectionPen
            createFileWithAIorWebcam(image, AIGenerated);
        }
        private Pen SelectionPen;

        private void InitializeSelectionPen()
        {
            // Create a black pen for the black dashes
            Pen blackPen = new Pen(Color.Black, 2)
            {
                DashStyle = DashStyle.Custom,
                DashPattern = new float[] { 4, 4 }
            };

            // Create a white pen for the white dashes
            Pen whitePen = new Pen(Color.White, 2)
            {
                DashStyle = DashStyle.Custom,
                DashPattern = new float[] { 4, 4 }
            };

            // Store the pens for use in the drawing logic
            SelectionPen = blackPen; // Use blackPen as the default
        }

        private void InitializeComponentAndFont()
        {
            InitializeComponent();
            if (fontsComboBox.Items.Count > 0)
            {
                fontsComboBox.SelectedIndex = 0;
            }
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
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(Settings1.Default.DefaultCanvasSize.Width + 20,
                Settings1.Default.DefaultCanvasSize.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            canvasPanel.Refresh();
            MainBitmap = new Bitmap(Settings1.Default.DefaultCanvasSize.Width,
                Settings1.Default.DefaultCanvasSize.Height);
            originalSize = MainBitmap.Size;
            pictureBoxCanvas.Image = MainBitmap;
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = "Unnamed File";
            undoStack.Clear();
            redoStack.Clear();
            geriAlToolStripMenuItem.Enabled = false;
            yineleToolStripMenuItem.Enabled = false;
        }
        private void openAFile(string fileName)
        {
            pictureBoxCanvas.Image = null;
            Image image;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                image = Image.FromStream(fs);
                // Use the image
            }
            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(image.Size.Width + 20, image.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            MainBitmap = new Bitmap(image);
            originalSize = MainBitmap.Size;
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = fileName;
            undoStack.Clear();
            redoStack.Clear();
            geriAlToolStripMenuItem.Enabled = false;
            yineleToolStripMenuItem.Enabled = false;
        }
        private void createFileWithAIorWebcam(Image generatedImage, bool isAIGenerated)
        {
            pictureBoxCanvas.Image = null;
            Image image = generatedImage;
            // panel1'in AutoScroll özelliðini true yaparak kaydýrma çubuklarýný etkinleþtiriyoruz
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarýný ayarlýyoruz
            canvasPanel.Size = new Size(generatedImage.Width + 20, generatedImage.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliðini canvasPanel'in boyutlarýna ayarlýyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleþtiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            MainBitmap = new Bitmap(image);
            originalSize = MainBitmap.Size;
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
            undoStack.Clear();
            redoStack.Clear();
            geriAlToolStripMenuItem.Enabled = false;
            yineleToolStripMenuItem.Enabled = false;
        }
        private void CenterCanvasPanel()
        {
            int centerX = (UIPanel.ClientSize.Width - canvasPanel.Width) / 2;
            int centerY = (UIPanel.ClientSize.Height - canvasPanel.Height) / 2;
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


        private async void kaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "PNG Files|*.png|JPEG Files|*.jpg|Bitmap Files|*.bmp";
            DialogResult dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                string targetFilePath = saveFileDialog1.FileName;
                string tempFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath), Path.GetRandomFileName());
                string backupFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath), Path.GetRandomFileName());

                labelSaving.Visible = true;
                progressBarSaving.Visible = true;
                toolStripSeparator13.Visible = true;

                progressBarSaving.Value = 0;

                try
                {
                    // Save the file to a temporary location in a background thread
                    await Task.Run(() =>
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            // Save the bitmap to memory
                            MainBitmap.Save(memoryStream, ImageFormat.Png);
                            byte[] imageData = memoryStream.ToArray();

                            // Write the file in chunks to the temporary file
                            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                int totalBytes = imageData.Length;
                                int chunkSize = 4096; // 4 KB
                                int bytesWritten = 0;

                                while (bytesWritten < totalBytes)
                                {
                                    int bytesToWrite = Math.Min(chunkSize, totalBytes - bytesWritten);
                                    fileStream.Write(imageData, bytesWritten, bytesToWrite);
                                    bytesWritten += bytesToWrite;

                                    // Update the progress bar
                                    int progress = (int)((bytesWritten / (float)totalBytes) * 100);
                                    Invoke(new Action(() =>
                                    {
                                        progressBarSaving.Value = progress;
                                    }));
                                }
                            }
                        }
                    });

                    // Replace the target file with the temporary file
                    File.Replace(tempFilePath, targetFilePath, backupFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while saving the file: {ex.Message}",
                                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Clean up the temporary and backup files
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }

                    // Hide saving indicators
                    labelSaving.Visible = false;
                    progressBarSaving.Visible = false;
                    toolStripSeparator13.Visible = false;
                }
            }
        }

        // Helper method to check if a file is locked
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If we can open the file, it's not locked
                }
                return false;
            }
            catch (IOException)
            {
                return true; // File is locked
            }
        }

        // Example of updated references to the renamed variable `selectedTool`

        private void addTextTool_CheckedChanged(object sender, EventArgs e)
        {
            if (addTextTool.Checked)
            {
                selectedTool = Tools.Text; // Use `selectedTool` instead of `currentTool`
                toolStripText.Visible = true;
            }
            else
            {
                toolStripText.Visible = false;
                HideAddTextTextBoxes();
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
            originalSize = MainBitmap.Size;
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            labelSize.Text = $"{canvasPanel.Width} X {canvasPanel.Height}px";
            UIPanel.AutoScrollMinSize = canvasPanel.Size;
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
                        newHeight = canvasPanel.Height;
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

                // Bitmap'i yeniden boyutlandýr
                Bitmap newBitmap = new Bitmap((int)Math.Round((newWidth - 20) / zoom), (int)Math.Round((newHeight - 20) / zoom));
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.DrawImage(MainBitmap, 0, 0);
                }
                MainBitmap = newBitmap;

                // Yeni boyutlarý uygula
                canvasPanel.Size = new Size(newWidth, newHeight);

                // panel1'in AutoScrollMinSize özelliðini güncelle
                UIPanel.AutoScrollMinSize = canvasPanel.Size;

                // Paneli merkezi konumda yerleþtiriyoruz
                CenterCanvasPanel();

                // Paneli hemen yenile (Refresh kullan)
                canvasPanel.Refresh();

                lastMousePos = e.Location;
                toolStripResize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";
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
            SaveStateForUndo();
            isResizing = true;
            toolStripResize.Visible = true;
            toolStripSeparator16.Visible = true;
            toolStripResize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";
            lastMousePos = e.Location;
        }
        private void brushTool_CheckedChanged(object sender, EventArgs e)
        {
            if (brushTool.Checked)
            {
                selectedTool = Tools.Brush;
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
                selectedTool = Tools.Pen;
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
                selectedTool = Tools.Cursor; // Use `selectedTool` instead of `currentTool`
            }
        }

        private void selectTool_CheckedChanged(object sender, EventArgs e)
        {
            if (selectTool.Checked)
            {
                selectedTool = Tools.Selection;
            }
        }

        private void magicSelectTool_CheckedChanged(object sender, EventArgs e)
        {
            if (magicSelectTool.Checked)
            {
                selectedTool = Tools.MagicSelection;
            }
        }

        private void eraserTool_CheckedChanged(object sender, EventArgs e)
        {
            if (eraserTool.Checked)
            {
                selectedTool = Tools.Eraser;
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
                selectedTool = Tools.Bucket;
                toolStripBucketTool.Visible = true;
            }
            else
            {
                toolStripBucketTool.Visible = false;
            }
        }

        private void sprayTool_CheckedChanged(object sender, EventArgs e)
        {
            if (sprayTool.Checked)
            {
                selectedTool = Tools.Spray;
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
                selectedTool = Tools.Line;
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
                selectedTool = Tools.Round;
            }
            UpdateToolStripVisibility();
        }

        private void rectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (rectangleToolStripMenuItem.Checked)
            {
                selectedTool = Tools.Rectangle;
            }
            UpdateToolStripVisibility();
        }

        private void roundedRectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (roundedRectangleToolStripMenuItem.Checked)
            {
                selectedTool = Tools.RoundedRectangle;
            }
            UpdateToolStripVisibility();
        }

        private void triangleToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (triangleToolStripMenuItem1.Checked)
            {
                selectedTool = Tools.Triangle;
            }
            UpdateToolStripVisibility();
        }

        private void hexagonToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (hexagonToolStripMenuItem1.Checked)
            {
                selectedTool = Tools.Hexagon;
            }
            UpdateToolStripVisibility();
        }

        private void colorDropTool_CheckedChanged(object sender, EventArgs e)
        {
            if (colorDropTool.Checked)
            {
                selectedTool = Tools.ColorDrop;
            }
        }

        private void pictureBoxCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isdrawing)
            {
                isdrawing = false;

                switch (selectedTool)
                {
                    case Tools.Line:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawLineOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case Tools.Round:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawRoundOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case Tools.Rectangle:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawRectangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case Tools.RoundedRectangle:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawRoundedRectangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location, radius);
                        }
                        break;
                    case Tools.Triangle:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawTriangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, e.Location);
                        }
                        break;
                    case Tools.Hexagon:
                        using (Graphics graphics = Graphics.FromImage(MainBitmap))
                        {
                            DrawShapes.DrawHexagonOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, points, startPoint, e.Location);
                        }
                        break;
                    default:
                        drawIntoCanvas(e);
                        break;
                }
                // Update the PictureBox with the new bitmap
                pictureBoxCanvas.Image = MainBitmap;
                pictureBoxCanvas.Invalidate();
            }
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (isdrawing)
            {
                switch (selectedTool)
                {
                    case Tools.Line:
                        pictureBoxCanvas.Image = ShapePreviews.LinePreview(
                            MainBitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.Round:
                        pictureBoxCanvas.Image = ShapePreviews.RoundPreview(
                            MainBitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)
                        );
                        pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.Rectangle:
                        pictureBoxCanvas.Image = ShapePreviews.RectanglePreview(
                            MainBitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.RoundedRectangle:
                        pictureBoxCanvas.Image = ShapePreviews.RoundedRectanglePreview(
                            MainBitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom), radius
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.Triangle:
                        pictureBoxCanvas.Image = ShapePreviews.TrianglePreview(
                            MainBitmap, color1, shapeThickness, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)
                        ); pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.Hexagon:
                        pictureBoxCanvas.Image = ShapePreviews.HexagonPreview(
                            MainBitmap, color1, shapeThickness, points, previewStartPoint.X, previewStartPoint.Y, (int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom));
                        pictureBoxCanvas.Invalidate(); // Update the PictureBox to show the preview
                        break;
                    case Tools.Selection:
                        if (e.Button != MouseButtons.Left)
                        {
                            return;
                        }
                        if (selectedTool == Tools.Selection && e.Button == MouseButtons.Left)
                        {
                            Point SelectionEndPoint = e.Location;
                            SelectionRectangle.Location = new Point(
                                Math.Min(SelectionStartPoint.X, SelectionEndPoint.X),
                                Math.Min(SelectionStartPoint.Y, SelectionEndPoint.Y));
                            SelectionRectangle.Size = new Size(
                                Math.Abs(SelectionStartPoint.X - SelectionEndPoint.X),
                                Math.Abs(SelectionStartPoint.Y - SelectionEndPoint.Y));
                            // Koordinatlarý zoom'a göre ölçekle
                            originalSelectionRectangleLocation = new Point(
                                (int)(SelectionRectangle.X / zoom),
                                (int)(SelectionRectangle.Y / zoom));
                            originalSelectionRectangleSize = new Size(
                                (int)(SelectionRectangle.Width / zoom),
                                (int)(SelectionRectangle.Height / zoom));
                            pictureBoxCanvas.Invalidate();
                        }
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
            if (MainBitmap == null)
            {
                MainBitmap = new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            }

            using (Graphics graphics = Graphics.FromImage(MainBitmap))
            {
                if (isdrawing)
                {
                    if (isSelected && !SelectionRectangle.IsEmpty)
                    {
                        // Koordinatlarý zoom'a göre ölçekle
                        Point scaledLocation = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                        if (!SelectionRectangle.Contains(scaledLocation))
                        {
                            return; // Seçim dikdörtgeninin dýþýndaki çizimleri yoksay
                        }
                    }

                    switch (selectedTool)
                    {
                        case Tools.Brush:
                            DrawBrush(graphics, BrushShapes.DrawCircleBrush, color1, brushSize, new Point((int)(e.X / zoom), (int)(e.Y / zoom)));
                            break;
                            // Diðer araçlar için benzer þekilde zoom'u uygula
                    }

                    x = (int)(e.X / zoom);
                    y = (int)(e.Y / zoom);
                }
            }

            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }
        private void pictureBoxCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxCanvas.Image != null)
            {
                if (SelectionRectangle != null &&
                    SelectionRectangle.Width > 0 &&
                    SelectionRectangle.Height > 0)
                {
                    // Draw the black dashes
                    using (Pen blackPen = new Pen(Color.Black, 2)
                    {
                        DashStyle = DashStyle.Custom,
                        DashPattern = new float[] { 4, 4 }
                    })
                    {
                        e.Graphics.DrawRectangle(blackPen, SelectionRectangle);
                    }

                    // Draw the white dashes slightly offset
                    using (Pen whitePen = new Pen(Color.White, 2)
                    {
                        DashStyle = DashStyle.Custom,
                        DashPattern = new float[] { 4, 4 }
                    })
                    {
                        Rectangle offsetRectangle = new Rectangle(
                            SelectionRectangle.X + 2,
                            SelectionRectangle.Y + 2,
                            SelectionRectangle.Width - 4,
                            SelectionRectangle.Height - 4
                        );
                        e.Graphics.DrawRectangle(whitePen, offsetRectangle);
                    }
                }
            }
        }

        private void DrawBrush(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point location)
        {
            // Fýrça boyutunu zoom'a göre ölçekle
            int scaledSize = (int)(size / zoom);

            if (!SelectionRectangle.IsEmpty)
            {
                int halfSize = scaledSize / 2;
                Rectangle brushBounds = new Rectangle(
                    (int)Math.Round((double)location.X - halfSize),
                    (int)Math.Round((double)location.Y - halfSize),
                    scaledSize,
                    scaledSize
                );

                if (!SelectionRectangle.IntersectsWith(brushBounds))
                {
                    return; // Fýrça seçim alanýnýn dýþýndaysa çizimi atla
                }

                Rectangle clippedBounds = Rectangle.Intersect(SelectionRectangle, brushBounds);
                if (clippedBounds.IsEmpty)
                {
                    return; // Kesiþim boþsa çizimi atla
                }

                location = new Point(
                    Math.Max(location.X, SelectionRectangle.Left + halfSize),
                    Math.Max(location.Y, SelectionRectangle.Top + halfSize)
                );
            }

            if (x == -1 && y == -1)
            {
                drawAction(graphics, color, scaledSize, location);
            }
            else
            {
                FillGap(graphics, drawAction, color, scaledSize, new Point(x, y), location);
            }
        }

        private void FillGap(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point start, Point end)
        {
            // Baþlangýç ve bitiþ noktalarýný zoom'a göre ölçekle
            start = new Point((int)(start.X / zoom), (int)(start.Y / zoom));
            end = new Point((int)(end.X / zoom), (int)(end.Y / zoom));

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
        private Stack<CanvasState> undoStack = new Stack<CanvasState>();
        private Stack<CanvasState> redoStack = new Stack<CanvasState>();
        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isdrawing = true;
            x = e.X;
            y = e.Y;
            CancelArtisticFilters();
            switch (selectedTool)
            {
                case Tools.Line:
                    SaveStateForUndo();
                    startPoint = new Point((int)(e.X/zoom), (int)(e.Y/zoom));
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.Round:
                    SaveStateForUndo();
                    startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.Rectangle:
                    SaveStateForUndo();
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.RoundedRectangle:
                    SaveStateForUndo();
                    startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.Triangle:
                    SaveStateForUndo();
                    startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.Hexagon:
                    SaveStateForUndo();
                    startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    previewBitmap = new Bitmap(MainBitmap);
                    break;
                case Tools.Selection:
                    isSelected = true;
                    SelectionStartPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                    pictureBoxCanvas.Invalidate();
                    break;
                default:
                    if (selectedTool == Tools.Brush || selectedTool == Tools.Pen || selectedTool == Tools.Eraser ||
                        selectedTool == Tools.Spray || selectedTool == Tools.Bucket || selectedTool == Tools.Line ||
                        selectedTool == Tools.Round || selectedTool == Tools.Rectangle || selectedTool == Tools.RoundedRectangle ||
                        selectedTool == Tools.Triangle || selectedTool == Tools.Hexagon || selectedTool == Tools.Text)
                    {
                        SaveStateForUndo();
                    }
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
            switch (selectedTool)
            {
                case Tools.Text:
                    {
                        SaveStateForUndo();
                        // MouseEventArgs'den týklama konumunu alýn  
                        // MouseEventArgs kullanarak týklama konumunu alýn
                        MouseEventArgs me = (MouseEventArgs)e;
                        int clickedX = me.X;
                        int clickedY = me.Y;
                        // Yeni bir TextBox oluþturun  
                        TextBox textBox = new TextBox
                        {
                            MaximumSize = Size.Empty, // Maksimum boyut  
                            AutoSize = false, // Otomatik boyutlandýrmayý devre dýþý býrakýn  
                            Multiline = true, // Çok satýrlý metin desteði  
                            WordWrap = true, // Metni sarmayý etkinleþtirin  
                            Font = new Font(fontsComboBox.Text, textSize), // Yazý tipi ayarý  
                            BorderStyle = BorderStyle.FixedSingle // Kenarlýk stili  
                        };
                        textBox.Location = new Point(
                            Math.Min(clickedX, pictureBoxCanvas.Width - textBox.Width),
                            Math.Min(clickedY, pictureBoxCanvas.Height - textBox.Height)
                        );

                        // TextBox'ý pictureBoxCanvas'a ekleyin  
                        pictureBoxCanvas.Controls.Add(textBox);

                        // TextBox'ý odaklayýn  
                        textBox.Focus();

                        // TextBox'ýn metni deðiþtikçe boyutunu ayarlayýn  
                        textBox.TextChanged += (s, args) =>
                        {
                            // Measure the size of the text, including multi-line text  
                            Size textSize = TextRenderer.MeasureText(
                                textBox.Text,
                                textBox.Font,
                                new Size(textBox.Width, int.MaxValue), // Allow wrapping by setting a maximum height  
                                TextFormatFlags.WordBreak // Enable word wrapping  
                            );

                            // Adjust the TextBox's height based on the measured size  
                            textBox.Height = textSize.Height + 5; // Add padding  
                        };
                        textBox.TextChanged += (s, args) =>
                        {
                            using (Graphics g = textBox.CreateGraphics())
                            {
                                // Measure the size of the text, including multi-line text  
                                SizeF textSize = g.MeasureString(textBox.Text, textBox.Font);

                                // Adjust the TextBox's width and height based on the measured size  
                                textBox.Width = Math.Max((int)textSize.Width + 10, textBox.MinimumSize.Width); // Add padding for width  
                                textBox.Height = Math.Max((int)textSize.Height + 10, textBox.MinimumSize.Height); // Add padding for height  
                            }
                        };

                        textBox.LostFocus += (s, args) =>
                        {
                            // Draw the TextBox content onto the bitmap  
                            using (Graphics graphics = Graphics.FromImage(MainBitmap))
                            {
                                graphics.DrawString(textBox.Text, textBox.Font, new SolidBrush(color1), textBox.Location);
                            }

                            // Remove the TextBox from the canvas  
                            pictureBoxCanvas.Controls.Remove(textBox);

                            // Update the PictureBox with the updated bitmap  
                            pictureBoxCanvas.Image = MainBitmap;
                            pictureBoxCanvas.Invalidate(); // Force a redraw  
                        };
                        textBox.TextChanged += (s, args) =>
                        {
                            using (Graphics g = textBox.CreateGraphics())
                            {
                                // Measure the size of the text, including multi-line text  
                                SizeF textSize = g.MeasureString(textBox.Text, textBox.Font, textBox.Width);

                                // Adjust the TextBox's width and height based on the measured size  
                                textBox.Width = Math.Max((int)textSize.Width + 10, textBox.MinimumSize.Width);
                                textBox.Height = Math.Max((int)textSize.Height + 10, textBox.MinimumSize.Height);
                            }
                        };
                        textBox.TextChanged += (s, args) =>
                        {
                            // Measure the size of the text, including multi-line text  
                            Size textSize = TextRenderer.MeasureText(
                                textBox.Text,
                                textBox.Font,
                                new Size(textBox.Width, int.MaxValue), // Allow wrapping by setting a maximum height  
                                TextFormatFlags.WordBreak // Enable word wrapping  
                            );

                            // Adjust the TextBox's height based on the measured size  
                            textBox.Height = Math.Max(textSize.Height + 5, textBox.MinimumSize.Height); // Add padding  
                        };

                        // TextBox'tan odak kaybolduðunda iþlemi tamamlayýn  
                        textBox.LostFocus += (s, args) =>
                        {
                            // TextBox içeriðini bitmap'e çiz  
                            using (Graphics graphics = Graphics.FromImage(MainBitmap))
                            {
                                graphics.DrawString(textBox.Text, textBox.Font, new SolidBrush(color1), textBox.Location);
                            }

                            // TextBox'ý kaldýr  
                            pictureBoxCanvas.Controls.Remove(textBox);

                            // Canvas'ý güncelle  
                            pictureBoxCanvas.Image = MainBitmap;
                            pictureBoxCanvas.Invalidate();
                        };
                    }
                    break;
                case Tools.Bucket:
                    {
                        SaveStateForUndo();
                        MouseEventArgs me = (MouseEventArgs)e; // EventArgs yerine MouseEventArgs kullanýmý  
                                                               // Fix for CS0246: 'Location' türü veya ad alaný adý bulunamadý  
                                                               // The issue occurs because 'Location' is not a valid type.  
                                                               // The correct type to use here is 'Point', which represents a location in a two-dimensional plane.  

                        // Replace the problematic line:  
                        // Location unzoomedLocation = me.Location;  

                        // With the following corrected line:  
                        Point unzoomedLocation = me.Location;
                        // Adjust the location based on the zoom level
                        int adjustedX = (int)(unzoomedLocation.X / zoom);
                        int adjustedY = (int)(unzoomedLocation.Y / zoom);
                        Point adjustedLocation = new Point(adjustedX, adjustedY);
                        FloodFill(MainBitmap, adjustedLocation, MainBitmap.GetPixel(adjustedX, adjustedY), color1, tolerance);
                    }
                    break;
            }
        }
        private void FloodFill(Bitmap bitmap, Point point, Color targetColor, Color replacementColor, int tolerance)
        {
            if (targetColor.ToArgb() == replacementColor.ToArgb()) return;

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int stride = data.Stride;
            IntPtr scan0 = data.Scan0;

            byte[] pixels = new byte[stride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(scan0, pixels, 0, pixels.Length);

            Stack<Point> pixelsToCheck = new Stack<Point>();
            pixelsToCheck.Push(point);

            byte targetR = targetColor.R;
            byte targetG = targetColor.G;
            byte targetB = targetColor.B;

            int replacementArgb = replacementColor.ToArgb();

            while (pixelsToCheck.Count > 0)
            {
                Point pt = pixelsToCheck.Pop();
                int x = pt.X;
                int y = pt.Y;

                // Check bounds first
                if (x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
                    continue;

                // Check if the pixel is within the selection rectangle when isSelected is true
                if (isSelected && !SelectionRectangle.IsEmpty && !SelectionRectangle.Contains(pt))
                    continue;

                int index = (y * stride) + (x * bytesPerPixel);

                byte currentB = pixels[index];
                byte currentG = pixels[index + 1];
                byte currentR = pixels[index + 2];

                int pixelArgb = 0;
                if (bytesPerPixel == 4)
                {
                    pixelArgb = BitConverter.ToInt32(pixels, index);
                }
                else if (bytesPerPixel == 3)
                {
                    pixelArgb = (255 << 24) | (currentR << 16) | (currentG << 8) | currentB;
                }

                if (pixelArgb == replacementArgb)
                {
                    continue;
                }

                int diffR = Math.Abs(currentR - targetR);
                int diffG = Math.Abs(currentG - targetG);
                int diffB = Math.Abs(currentB - targetB);

                int colorDifferenceThreshold = (int)((tolerance / 100.0) * 765);

                if ((diffR + diffG + diffB) <= colorDifferenceThreshold)
                {
                    if (bytesPerPixel == 4)
                    {
                        BitConverter.GetBytes(replacementArgb).CopyTo(pixels, index);
                    }
                    else if (bytesPerPixel == 3)
                    {
                        pixels[index] = (byte)(replacementArgb & 0xFF);
                        pixels[index + 1] = (byte)((replacementArgb >> 8) & 0xFF);
                        pixels[index + 2] = (byte)((replacementArgb >> 16) & 0xFF);
                    }

                    pixelsToCheck.Push(new Point(x + 1, y));
                    pixelsToCheck.Push(new Point(x - 1, y));
                    pixelsToCheck.Push(new Point(x, y + 1));
                    pixelsToCheck.Push(new Point(x, y - 1));
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, scan0, pixels.Length);
            bitmap.UnlockBits(data);

            if (pictureBoxCanvas != null)
            {
                pictureBoxCanvas.Refresh();
            }
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
                if (Settings1.Default.RecentFiles.Contains(file) == false)
                {
                    Settings1.Default.RecentFiles.Add(file);
                    Settings1.Default.Save();
                }
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
                textSize = Settings1.Default.DefaultTextSize;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                textSize = Settings1.Default.DefaultTextSize;
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

        // Fix for CS0118: 'ImageEditor.currentTool' bir tür öðesidir ancak deðiþken olarak kullanýlýr

        // The issue occurs because `currentTool` is both an enum type and a variable name in the code.
        // To resolve this, we need to rename the variable to avoid the conflict with the enum type.

        private Tools selectedTool; // Rename the variable from `currentTool` to `selectedTool`

        private void SaveStateForUndo()
        {
            if (MainBitmap != null)
            {
                if (undoStack.Count >= MaxStackSize)
                {
                    // Remove the oldest state
                    var tempList = undoStack.ToList();
                    tempList.RemoveAt(0);
                    undoStack = new Stack<CanvasState>(tempList);
                }
                undoStack.Push(new CanvasState(new Bitmap(MainBitmap), new Size(MainBitmap.Width, MainBitmap.Height)));
                redoStack.Clear(); // Clear redo stack on new action
            }
            UpdateUndoRedoButtons(); // Update buttons
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new CanvasState(new Bitmap(MainBitmap), originalSize)); // Save current state to redo stack
                var previousState = undoStack.Pop(); // Get the last state
                MainBitmap = previousState.Bitmap;
                canvasPanel.Size = new Size((int)Math.Round(previousState.MainBitmapSize.Width * zoom) + 20, (int)Math.Round(previousState.MainBitmapSize.Height * zoom) + 20);
                pictureBoxCanvas.Size = panelResizer.Size;
                pictureBoxCanvas.Image = MainBitmap;
                pictureBoxCanvas.Invalidate();
                CenterCanvasPanel(); // Center the canvas panel
                originalSize = MainBitmap.Size; // Update original size
                labelSize.Text = $"{MainBitmap.Width} x {MainBitmap.Height} px"; // Update size label
            }
            UpdateUndoRedoButtons(); // Update buttons
        }
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new CanvasState(new Bitmap(MainBitmap), MainBitmap.Size)); // Save current state to undo stack
                var nextState = redoStack.Pop(); // Get the next state
                MainBitmap = nextState.Bitmap;
                canvasPanel.Size = new Size((int)Math.Round(nextState.MainBitmapSize.Width * zoom) + 20, (int)Math.Round(nextState.MainBitmapSize.Height * zoom) + 20);
                pictureBoxCanvas.Size = panelResizer.Size;
                pictureBoxCanvas.Image = MainBitmap;
                pictureBoxCanvas.Invalidate();
                originalSize = MainBitmap.Size; // Update original size
                labelSize.Text = $"{MainBitmap.Width} x {MainBitmap.Height} px"; // Update size label
                CenterCanvasPanel(); // Center the canvas panel
            }
            UpdateUndoRedoButtons(); // Update buttons
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

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            switch (toolStripButton9.Checked)
            {
                case false:
                    {
                        pictureBoxCanvas.BackgroundImage = null;
                        break;
                    }
                case true:
                    {
                        pictureBoxCanvas.BackgroundImage = Resources.transparent_pattern;
                        break;
                    }
            }

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Click(object sender, EventArgs e)
        {
            SetAsUnselected();
            HideAddTextTextBoxes();
        }
        private void HideAddTextTextBoxes()
        {
            foreach (Control control in pictureBoxCanvas.Controls)
            {
                if (control is TextBox textBox)
                {
                    pictureBoxCanvas.Controls.Remove(textBox);
                }
            }
        }

        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void canvasPanel_Click(object sender, EventArgs e)
        {
            SetAsUnselected();
            HideAddTextTextBoxes();
        }

        private void resize_top_Click(object sender, EventArgs e)
        {
            SetAsUnselected();
            HideAddTextTextBoxes();
        }
        private void SetAsUnselected()
        {
            isSelected = false;
            SelectionRectangle = Rectangle.Empty; // Clear the selection rectangle
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate(); // Redraw the canvas
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBoxTolerance.Text))
                {
                    throw new FormatException();
                }
                else if (Convert.ToInt32(textBoxTolerance.Text) <= 0 || Convert.ToInt32(textBoxTolerance.Text) > 100)
                {
                    throw new OverflowException();
                }
                else
                {
                    tolerance = Convert.ToInt32(textBoxTolerance.Text);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tolerance = 50;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tolerance = 50;
            }
            finally
            {
                textBoxTolerance.Text = tolerance.ToString();
            }
        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void mirrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            basicFilters = BasicFilters.Mirror;
            MainBitmap = Filters.BasicFilters.MirrorEffect(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void flashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            MainBitmap = Filters.BasicFilters.Flash(MainBitmap);
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void frozenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.Frozen(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void winterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.Winter(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void blackAndWhiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.BlackAndWhite(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void oldPictureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.OldImage(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void cherryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.CherryFilter(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void lightAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.LightAdd(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void purpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.PurpleEffect(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void fogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.BasicFilters.FogEffect(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void RefreshArtisticFiltersPreview()
        {
            switch (artisticFilters)
            {
                case ArtisticFilters.OilPainting:
                    pictureBoxCanvas.Image = Filters.ArtisticFilters.OilPaintFilter(MainBitmap, FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity,
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize, (byte)FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold);
                    break;
                case ArtisticFilters.Cartoon:
                    pictureBoxCanvas.Image = Filters.ArtisticFilters.CartoonFilter(MainBitmap, FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity,
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize, (byte)FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold);
                    break;
            }
            pictureBoxCanvas.Invalidate();
        }
        private void buttonArtisticFiltersOK_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            MainBitmap = pictureBoxCanvas.Image as Bitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
            toolStripArtisticFilters.Visible = false;
        }

        private void buttonArtisticFiltersCancel_Click(object sender, EventArgs e)
        {
            CancelArtisticFilters();
        }
        private void CancelArtisticFilters()
        {
            toolStripArtisticFilters.Visible = false;
            artisticFilters = ArtisticFilters.None;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Refresh();
        }

        private void cartoonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeArtisticFilters(ArtisticFilters.Cartoon);
        }

        private void oilPaintingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeArtisticFilters(ArtisticFilters.OilPainting);
        }
        private void InitializeArtisticFilters(Enum filterType)
        {
            toolStripArtisticFilters.Visible = true;
            artisticFilters = (ArtisticFilters)filterType;
            switch (artisticFilters)
            {
                case ArtisticFilters.OilPainting:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold.ToString();
                    RefreshArtisticFiltersPreview();
                    break;
                case ArtisticFilters.Cartoon:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold.ToString();
                    RefreshArtisticFiltersPreview();
                    break;
                default:
                    break;
            }
        }

        private void textBoxArtisticFilterSize_Leave(object sender, EventArgs e)
        {

        }

        private void textBoxArtisticFilterIntensity_Leave(object sender, EventArgs e)
        {

        }

        private void textBoxArtisticFilterThreshold_Leave(object sender, EventArgs e)
        {

        }

        private void textBoxArtisticFilterSize_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textBoxArtisticFilterSize.Text))
                {
                    switch (artisticFilters)
                    {
                        case ArtisticFilters.OilPainting:
                            if (Convert.ToInt32(textBoxArtisticFilterSize.Text) > 2 && Convert.ToInt32(textBoxArtisticFilterSize.Text) < 32)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Convert.ToInt32(textBoxArtisticFilterSize.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                        case ArtisticFilters.Cartoon:
                            if (Convert.ToInt32(textBoxArtisticFilterSize.Text) > 2 && Convert.ToInt32(textBoxArtisticFilterSize.Text) < 32)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Convert.ToInt32(textBoxArtisticFilterSize.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = 5;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = 5;
                        break;
                }
                textBoxArtisticFilterSize.Text = "5";
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = 5;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = 5;
                        break;
                }
                textBoxArtisticFilterSize.Text = "5";
            }
            finally
            {
                RefreshArtisticFiltersPreview();
            }
        }

        private void textBoxArtisticFilterIntensity_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textBoxArtisticFilterIntensity.Text))
                {
                    switch (artisticFilters)
                    {
                        case ArtisticFilters.OilPainting:
                            if (Convert.ToInt32(textBoxArtisticFilterIntensity.Text) > 0 && Convert.ToInt32(textBoxArtisticFilterIntensity.Text) < 256)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = Convert.ToInt32(textBoxArtisticFilterIntensity.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                        case ArtisticFilters.Cartoon:
                            if (Convert.ToInt32(textBoxArtisticFilterIntensity.Text) > 0 && Convert.ToInt32(textBoxArtisticFilterSize.Text) < 256)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = Convert.ToInt32(textBoxArtisticFilterIntensity.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = 10;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = 10;
                        break;
                }
                textBoxArtisticFilterIntensity.Text = "10";
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = 10;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = 10;
                        break;
                }
                textBoxArtisticFilterIntensity.Text = "10";
            }
            finally
            {
                RefreshArtisticFiltersPreview();
            }
        }

        private void textBoxArtisticFilterThreshold_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textBoxArtisticFilterThreshold.Text))
                {
                    switch (artisticFilters)
                    {
                        case ArtisticFilters.OilPainting:
                            if (Convert.ToInt32(textBoxArtisticFilterThreshold.Text) >= 0 && Convert.ToInt32(textBoxArtisticFilterThreshold.Text) < 256)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = Convert.ToInt32(textBoxArtisticFilterThreshold.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                        case ArtisticFilters.Cartoon:
                            if (Convert.ToInt32(textBoxArtisticFilterThreshold.Text) >= 0 && Convert.ToInt32(textBoxArtisticFilterThreshold.Text) < 256)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = Convert.ToInt32(textBoxArtisticFilterThreshold.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = 50;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = 50;
                        break;
                }
                textBoxArtisticFilterThreshold.Text = "10";
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = 50;
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = 50;
                        break;
                }
                textBoxArtisticFilterThreshold.Text = "10";
            }
            finally
            {
                RefreshArtisticFiltersPreview();
            }
        }

        private void printImage_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Bitmap bmp = MainBitmap;
            if (bmp != null)
            {
                // Calculate the position to center the image on the page
                float x = e.MarginBounds.Left + (e.MarginBounds.Width - bmp.Width) / 2f;
                float y = e.MarginBounds.Top + (e.MarginBounds.Height - bmp.Height) / 2f;

                // Draw the image at its original size
                e.Graphics.DrawImage(bmp, x, y, bmp.Width, bmp.Height);
            }
            else
            {
                MessageBox.Show("No image to print.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printImageDialog.Document = printImage;
            if (printImageDialog.ShowDialog() == DialogResult.OK)
            {
                printImage.Print();
            }
        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printPreviewDialog1.Document = printImage;
            if (MainBitmap != null)
            {
                printPreviewDialog1.ShowDialog();
            }
            else
            {
                MessageBox.Show("No image to preview.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Paste_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            Bitmap bmp = Clipboard.GetImage() as Bitmap;
            if (bmp != null)
            {
                pictureBoxCanvas.Image = bmp;
                canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
                pictureBoxCanvas.Invalidate();
                CenterCanvasPanel();
            }
            else
            {
                MessageBox.Show("No image in clipboard.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            try
            {
                CutOrCopy();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error copying image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CutOrCopy()
        {
            // Create a copy with the same pixel format as the original bitmap
            // or explicitly use 32bppArgb if needed for transparency
            Bitmap copyBitmap = new Bitmap(MainBitmap.Width, MainBitmap.Height, PixelFormat.Format32bppArgb);

            // Set up color attributes to preserve transparency
            ImageAttributes imageAttributes = new ImageAttributes();

            // Create a color matrix that preserves alpha channel (the 4th row)
            ColorMatrix colorMatrix = new ColorMatrix(new float[][] {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        });

            imageAttributes.SetColorMatrix(colorMatrix);

            using (Graphics g = Graphics.FromImage(copyBitmap))
            {
                // Set high quality settings to prevent quality loss
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Clear with transparent color first
                g.Clear(Color.Transparent);

                // Draw the original bitmap using the color matrix to preserve alpha
                g.DrawImage(MainBitmap,
                    new Rectangle(0, 0, MainBitmap.Width, MainBitmap.Height),
                    0, 0, MainBitmap.Width, MainBitmap.Height,
                    GraphicsUnit.Pixel,
                    imageAttributes);
            }

            // Use PNG format specifically for the clipboard to preserve transparency
            // This is a key part of the solution
            DataObject dataObject = new DataObject();

            // Add both PNG and standard bitmap format
            using (MemoryStream pngStream = new MemoryStream())
            {
                copyBitmap.Save(pngStream, ImageFormat.Png);
                dataObject.SetData("PNG", false, pngStream);
            }

            // Also include the standard bitmap format for compatibility
            dataObject.SetData(DataFormats.Bitmap, true, copyBitmap);

            // Set the data object to clipboard
            Clipboard.SetDataObject(dataObject, true);
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            try
            {
                SaveStateForUndo();
                CutOrCopy();
                MainBitmap = new Bitmap(MainBitmap.Width, MainBitmap.Height);
                pictureBoxCanvas.Image = MainBitmap;
                pictureBoxCanvas.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cutting image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chloeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            MainBitmap = Filters.AmbientFilters.Chloe(MainBitmap);
            canvasPanel.Size = new Size(MainBitmap.Size.Width + 20, MainBitmap.Size.Height + 20);
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (zoom < 5)
            {
                zoom += 0.05f;
                UpdatePictureBoxZoom();
                ScaleSelection();
            }
            else
            {
                zoom = 5;
                SystemSounds.Beep.Play();
            }
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (zoom > 0.05f)
            {
                zoom -= 0.05f;
                UpdatePictureBoxZoom();
                ScaleSelection();
            }
            else
            {
                zoom = 0.05f;
                SystemSounds.Beep.Play();
            }
        }
        private void ScaleSelection()
        {
            if(isSelected && (SelectionRectangle != Rectangle.Empty))
            {
                SelectionRectangle.Location = new Point((int)Math.Round(originalSelectionRectangleLocation.X * zoom),
                    (int)Math.Round(originalSelectionRectangleLocation.X * zoom));
                SelectionRectangle.Size = new Size((int)Math.Round(originalSelectionRectangleSize.Width * zoom),
                    (int)Math.Round(originalSelectionRectangleSize.Height * zoom));
                pictureBoxCanvas.Invalidate();
            }
        }
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        private void UpdatePictureBoxZoom()
        {
            UIPanel.AutoScrollMinSize = canvasPanel.Size;
            if (MainBitmap == null) return;

            canvasPanel.Size = new Size((int)(originalSize.Width * zoom) + 20, (int)(originalSize.Height * zoom) + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            CenterCanvasPanel();
            pictureBoxCanvas.Invalidate(); // Yeniden çizim için tetikleyin
            labelZoom.Text = $"{(int)Math.Round(zoom * 100)}%";
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
