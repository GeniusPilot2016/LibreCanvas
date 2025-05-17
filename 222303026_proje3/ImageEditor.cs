using _222303026_proje3.Properties;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Imaging.Effects;
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
        int brushSize = Settings1.Default.DefaultBrushSize,
            penSize = Settings1.Default.DefaultPenSize,
            eraserSize = Settings1.Default.DefaultEraserSize,
            sprayToolSize = Settings1.Default.DefaultSpraySize,
            shapeThickness = Settings1.Default.DefaultShapeSize,
            radius = Settings1.Default.DefaultRadiusSize,
            points = Settings1.Default.DefaultPointsCount,
            tolerance = Settings1.Default.DefaultBucketTolerance,
            pixelationSize = Settings1.Default.DefaultPixelationSize,
            pixelationOffsetX = Settings1.Default.DefaultPixelationOffsetX,
            pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY;
        float textSize = Settings1.Default.DefaultTextSize;
        float zoom = 1,
            gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
        bool isResizing = false, isSelected = false, backgroundFilling = false,
            isImageCurrentlyCreating = false;
        private ResizeDirection resizeDirection;
        private BasicFilters basicFilters;
        private ArtisticFilters artisticFilters = ArtisticFilters.None;
        private Point lastMousePos;
        Size originalSize; // Unzoomed size
        Size originalSelectionRectangleSize; // Unzoomed size
        Point originalSelectionRectangleLocation; // Unzoomed location
        Bitmap MainBitmap;
        Bitmap SelectedBitmap;
        Rectangle SelectionRectangle = new Rectangle();
        Point SelectionStartPoint;
        FontStyle fontStyle = FontStyle.Regular;
        private TextToolAlign textToolAlign = TextToolAlign.Left;
        enum TextToolAlign
        {
            Left,
            Middle,
            Right
        }
        private BlurEffect blurEffect = BlurEffect.None;
        enum BlurEffect
        {
            None,
            Gaussian,
            Pixelate,
        }
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
            SetInitialValues();
            InitializeSelectionPen(); // Initialize the SelectionPen
            createNewFile();
            UpdateUndoRedoButtons(); // Baþlangýçta tuþlarý güncelle
        }
        public ImageEditor(String fileName)
        {
            InitializeComponentAndFont();
            SetInitialValues();
            InitializeSelectionPen(); // Initialize the SelectionPen
            openAFile(fileName);
        }
        public ImageEditor(Image image, bool AIGenerated)
        {
            InitializeComponentAndFont();
            SetInitialValues();
            InitializeSelectionPen(); // Initialize the SelectionPen
            createFileWithAIorWebcam(image, AIGenerated);
        }
        private Pen SelectionPen;

        private void SetInitialValues()
        {
            toolStripComboBoxAirBrushSize.SelectedItem = sprayToolSize;
            comboBoxBrushSize.SelectedItem = brushSize.ToString();
            comboBoxBrushType.SelectedIndex = Settings1.Default.DefaultBrushStyle;
            comboBoxPenSize.SelectedItem = penSize.ToString();
            comboBoxPenType.SelectedIndex = Settings1.Default.DefaultPenStyle;
            comboBoxEraserSize.SelectedItem = eraserSize.ToString();
            textBoxTolerance.Text = tolerance.ToString();
            comboBoxShapeThickness.SelectedItem = shapeThickness.ToString();
            textBoxRadius.Text = radius.ToString();
            textBoxPoints.Text = points.ToString();
            fontSizeComboBox.SelectedItem = textSize.ToString();
            textBoxPixelationSize.Text = pixelationSize.ToString();
            textBoxPixelationOffsetX.Text = pixelationOffsetX.ToString();
            textBoxPixelationOffsetY.Text = pixelationOffsetY.ToString();
            textboxGaussianBlurRadius.Text = gaussianBlurSize.ToString();
        }
        private void SetTheme()
        {
            switch (Settings1.Default.PreferredTheme)
            {
                case 0: // System theme
                    if (CheckSystemTheme.IsDarkTheme())
                    {
                        DarkTheme();
                    }
                    else
                    {
                        LightTheme();
                    }
                    break;
                case 1: // Light theme
                    LightTheme();
                    break;
                case 2: // Dark theme
                    DarkTheme();
                    break;
            }
        }
        private void LightTheme()
        {
            this.BackColor = Form.DefaultBackColor;
            this.ForeColor = Form.DefaultForeColor;
            panelResizer.BackColor = Panel.DefaultBackColor;
            canvasPanel.BackColor = Panel.DefaultBackColor;
            menuStrip1.BackgroundImage = Resources.toolstrip_light;
            menuStrip1.BackColor = Color.Transparent;
            menuStrip1.ForeColor = MenuStrip.DefaultForeColor;
            foreach (ToolStrip toolStrip in toolStripContainer1.LeftToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_light;
                toolStrip.BackColor = ToolStrip.DefaultBackColor;
                toolStrip.ForeColor = ToolStrip.DefaultForeColor;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripButton button && (item != foregroundColorButton)
                        && (item != backgroundColorButton))
                    {
                        button.BackColor = Color.Transparent;
                        button.ForeColor = ToolStrip.DefaultForeColor;
                    }
                }
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.RightToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_light;
                toolStrip.BackColor = Color.Transparent;
                toolStrip.ForeColor = ToolStrip.DefaultForeColor;
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.TopToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_light;
                toolStrip.BackColor = Color.Transparent;
                toolStrip.ForeColor = ToolStrip.DefaultForeColor;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripComboBox comboBox)
                    {
                        comboBox.BackColor = SystemColors.Window;
                        comboBox.ForeColor = SystemColors.WindowText;
                    }
                    if (item is ToolStripTextBox textBox)
                    {
                        textBox.BackColor = SystemColors.Window;
                        textBox.ForeColor = SystemColors.WindowText;
                    }
                }
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.BottomToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_light;
                toolStrip.BackColor = Color.Transparent;
                toolStrip.ForeColor = ToolStrip.DefaultForeColor;
            }
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            panelResizer.BackColor = Color.FromArgb(32, 32, 32);
            canvasPanel.BackColor = Color.FromArgb(32, 32, 32);
            menuStrip1.BackgroundImage = Resources.toolstrip_dark;
            menuStrip1.BackColor = Color.Black;
            menuStrip1.ForeColor = Color.White;
            foreach (ToolStrip toolStrip in toolStripContainer1.LeftToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_dark;
                toolStrip.BackColor = Color.Black;
                toolStrip.ForeColor = Color.White;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripButton button && (item != foregroundColorButton)
                        && (item != backgroundColorButton))
                    {
                        button.BackColor = Color.Black;
                        button.ForeColor = Color.White;
                    }
                }
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.RightToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_dark;
                toolStrip.BackColor = Color.Black;
                toolStrip.ForeColor = Color.White;
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.TopToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_dark;
                toolStrip.BackColor = Color.Black;
                toolStrip.ForeColor = Color.White;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripComboBox comboBox)
                    {
                        comboBox.BackColor = Color.Black;
                        comboBox.ForeColor = Color.White;
                    }
                    if (item is ToolStripTextBox textBox)
                    {
                        textBox.BackColor = Color.Black;
                        textBox.ForeColor = Color.White;
                    }
                }
            }
            foreach (ToolStrip toolStrip in toolStripContainer1.BottomToolStripPanel.Controls.OfType<ToolStrip>())
            {
                toolStrip.BackgroundImage = Resources.toolstrip_dark;
                toolStrip.BackColor = Color.Black;
                toolStrip.ForeColor = Color.White;
            }
        }
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
            SetTheme();
            SetFonts();
            // Set the default selected font to the first one in the list
            if (fontsComboBox.Items.Count > 0)
            {
                fontsComboBox.SelectedIndex = 0;
            }
            toolStripSample.Font = new Font(fontFamilies[fontsComboBox.SelectedIndex], toolStripSample.Font.Size, toolStripSample.Font.Style);
        }
        private void SetFonts()
        {
            UIFonts uiFonts = new UIFonts();
            foreach (Control control in Controls)
            {
                control.Font = uiFonts.SetUIFont(control.Font.Size, control.Font.Style);
                foreach (Control topToolStripPanelControls in toolStripContainer1.TopToolStripPanel.Controls)
                {
                    topToolStripPanelControls.Font = uiFonts.SetUIFont(topToolStripPanelControls.Font.Size, topToolStripPanelControls.Font.Style);
                    foreach (ToolStripItem item in menuStrip1.Items)
                    {
                        if (item is ToolStripMenuItem menuItem)
                        {
                            menuItem.Font = uiFonts.SetUIFont(menuItem.Font.Size, menuItem.Font.Style);
                        }
                    }
                    foreach (ToolStrip toolStrip in toolStripContainer1.TopToolStripPanel.Controls.OfType<ToolStrip>())
                    {
                        foreach (ToolStripItem items in toolStrip.Items) // Fix: Use the correct instance of ToolStrip
                        {
                            items.Font = uiFonts.SetUIFont(items.Font.Size, items.Font.Style);
                        }
                    }
                }
                foreach (Control bottomToolStripPanelControls in toolStripContainer1.BottomToolStripPanel.Controls)
                {
                    bottomToolStripPanelControls.Font = uiFonts.SetUIFont(bottomToolStripPanelControls.Font.Size, bottomToolStripPanelControls.Font.Style);
                    foreach (ToolStrip toolStrip in toolStripContainer1.BottomToolStripPanel.Controls.OfType<ToolStrip>())
                    {
                        foreach (ToolStripItem items in toolStrip.Items) // Fix: Use the correct instance of ToolStrip
                        {
                            items.Font = uiFonts.SetUIFont(items.Font.Size, items.Font.Style);
                        }
                    }
                }
                foreach (Control leftToolStripPanelControls in toolStripContainer1.LeftToolStripPanel.Controls)
                {
                    leftToolStripPanelControls.Font = uiFonts.SetUIFont(leftToolStripPanelControls.Font.Size, leftToolStripPanelControls.Font.Style);
                    foreach (ToolStrip toolStrip in toolStripContainer1.LeftToolStripPanel.Controls.OfType<ToolStrip>())
                    {
                        foreach (ToolStripItem items in toolStrip.Items) // Fix: Use the correct instance of ToolStrip
                        {
                            items.Font = uiFonts.SetUIFont(items.Font.Size, items.Font.Style);
                        }
                    }
                }
                foreach (Control rightToolStripPanelControls in toolStripContainer1.RightToolStripPanel.Controls)
                {
                    rightToolStripPanelControls.Font = uiFonts.SetUIFont(rightToolStripPanelControls.Font.Size, rightToolStripPanelControls.Font.Style);
                    foreach (ToolStrip toolStrip in toolStripContainer1.RightToolStripPanel.Controls.OfType<ToolStrip>())
                    {
                        foreach (ToolStripItem items in toolStrip.Items) // Fix: Use the correct instance of ToolStrip
                        {
                            items.Font = uiFonts.SetUIFont(items.Font.Size, items.Font.Style);
                        }
                    }
                }
            }
        }
        private void createNewFile()
        {
            SetAsUnselected();
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
            SetAsUnselected();
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
            SetAsUnselected();
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
            toolStripAICreateImage.Visible = true;
        }

        private async void generativeEraserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isImageCurrentlyCreating = true;
            toolStripAICreateImage.Visible = false; 
            SaveStateForUndo();
            try
            {
                if (isSelected && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                {
                    if (SelectedBitmap == null)
                    {
                        SelectedBitmap = new Bitmap(
                            (int)(SelectionRectangle.Width / zoom),
                            (int)(SelectionRectangle.Height / zoom));
                        using (Graphics g = Graphics.FromImage(SelectedBitmap))
                        {
                            g.Clear(Color.Transparent);
                            Rectangle sourceRect = new Rectangle(
                                (int)(originalSelectionRectangleLocation.X),
                                (int)(originalSelectionRectangleLocation.Y),
                                (int)(originalSelectionRectangleSize.Width),
                                (int)(originalSelectionRectangleSize.Height));
                            g.DrawImage(MainBitmap,
                                new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                                sourceRect,
                                GraphicsUnit.Pixel);
                        }
                        canvasPanel.Enabled = false;
                        menuStrip1.Enabled = false;
                        toolStripTools.Enabled = false;
                        toolStripSeparator29.Visible = true;
                        labelAIImageErasing.Visible = true;
                        progressBarAIImageCreation.Visible = true;
                        var image = await CreateAIImages.EditImage(
    SelectedBitmap, "Remove the object or person in context of the image.");

                        if (image != null)
                        {
                            SelectedBitmap = (Bitmap)image;
                            MergeMainBitmapWithSelected();
                        }
                        else
                        {
                            Undo();
                            ClearRedoStack(); 
                            MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    SystemSounds.Beep.Play(); // Play a beep sound when the image is not selected
                }
            }
            catch (NullReferenceException)
            {
                Undo();
                ClearRedoStack();
                MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Undo();
                ClearRedoStack();
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isImageCurrentlyCreating = false;
                pictureBoxCanvas.Invalidate();
                canvasPanel.Enabled = true;
                menuStrip1.Enabled = true;
                toolStripTools.Enabled = true;
                toolStripSeparator29.Visible = false;
                labelAIImageErasing.Visible = false;
                progressBarAIImageCreation.Visible = false;
            }
        }

        private async void removeBackgroundToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            toolStripAICreateImage.Visible = false;
            try
            {
                isImageCurrentlyCreating = true;
                canvasPanel.Enabled = false;
                menuStrip1.Enabled = false;
                toolStripTools.Enabled = false;
                toolStripSeparator29.Visible = true;
                labelBackgroundRemoving.Visible = true;
                progressBarAIImageCreation.Visible = true;
                SaveStateForUndo();
                if (isSelected && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                {
                    if (SelectedBitmap == null)
                    {
                        SelectedBitmap = new Bitmap(
                            (int)(SelectionRectangle.Width / zoom),
                            (int)(SelectionRectangle.Height / zoom));
                        using (Graphics g = Graphics.FromImage(SelectedBitmap))
                        {
                            g.Clear(Color.Transparent);
                            Rectangle sourceRect = new Rectangle(
                                (int)(originalSelectionRectangleLocation.X),
                                (int)(originalSelectionRectangleLocation.Y),
                                (int)(originalSelectionRectangleSize.Width),
                                (int)(originalSelectionRectangleSize.Height));
                            g.DrawImage(MainBitmap,
                                new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                                sourceRect,
                                GraphicsUnit.Pixel);
                        }
                    }
                    var imageTask = CreateAIImages.EditImage(SelectedBitmap, "Isolate the object or person and make the background transparent.");
                    var image = await imageTask; // Await the task to get the result
                    if (image != null)
                    {
                        SelectedBitmap = new Bitmap(image); // Convert Image to Bitmap  
                        MergeMainBitmapWithSelected();
                    }
                    else
                    {
                        Undo();
                        ClearRedoStack();
                        MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    var imageTask = CreateAIImages.EditImage(MainBitmap, "Isolate the object or person and make the background transparent.");
                    var image = await imageTask; // Await the task to get the result
                    if (image != null)
                    {
                        MainBitmap = new Bitmap(image); // Convert Image to Bitmap  
                        pictureBoxCanvas.Image = MainBitmap;
                        pictureBoxCanvas.Invalidate();
                    }
                    else
                    {
                        Undo();
                        ClearRedoStack();
                        MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                
            }
            catch (NullReferenceException)
            {
                Undo();
                ClearRedoStack();
                MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Undo();
                ClearRedoStack();
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isImageCurrentlyCreating = false;
                pictureBoxCanvas.Invalidate();
                canvasPanel.Enabled = true;
                menuStrip1.Enabled = true;
                toolStripTools.Enabled = true;
                toolStripSeparator29.Visible = false;
                labelBackgroundRemoving.Visible = false;
                progressBarAIImageCreation.Visible = false;
            }
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
                    // Check if the temporary file exists
                    if (File.Exists(tempFilePath))
                    {   // Save the file to a temporary location in a background thread
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
                    else
                    {
                        // Save directly to the target file if the temporary file is missing
                        await Task.Run(() =>
                        {
                            string newFilePath = saveFileDialog1.FileName;
                            // Save the bitmap to the specified file
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                // Save the bitmap to memory
                                MainBitmap.Save(memoryStream, ImageFormat.Png);
                                byte[] imageData = memoryStream.ToArray();

                                // Write the file in chunks to the specified file
                                using (FileStream fileStream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    int totalBytes = imageData.Length;
                                    int chunkSize = 4096; // 4 KB
                                    int bytesWritten = 0;

                                    while (bytesWritten < totalBytes)
                                    {
                                        int bytesToWrite = Math.Min(chunkSize, totalBytes - bytesWritten);
                                        fileStream.Write(imageData, bytesWritten, bytesToWrite);
                                        bytesWritten += bytesToWrite;

                                        // Optionally, update a progress bar if needed
                                        int progress = (int)((bytesWritten / (float)totalBytes) * 100);
                                        Invoke(new Action(() =>
                                        {
                                            progressBarSaving.Value = progress;
                                        }));
                                    }
                                }
                            }
                        });
                        MainBitmap.Save(targetFilePath, ImageFormat.Png);
                    }

                    // Clean up the temporary and backup files
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while saving the file: {ex.Message}",
                                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
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
            if (!isImageCurrentlyCreating)
            {
                if (isdrawing)
                {
                    isdrawing = false;

                    if (isSelected && !SelectionRectangle.IsEmpty &&
                        (selectedTool == Tools.Line || selectedTool == Tools.Round ||
                         selectedTool == Tools.Rectangle || selectedTool == Tools.RoundedRectangle ||
                         selectedTool == Tools.Triangle || selectedTool == Tools.Hexagon))
                    {
                        // The shape preview has already updated the SelectedBitmap
                        // Just mark as invalidate to ensure the PictureBox updates
                        pictureBoxCanvas.Invalidate();
                    }
                    else
                    {
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
                    }

                    // Merge the SelectedBitmap into the MainBitmap
                    if (SelectedBitmap != null && isSelected)
                    {
                        using (Graphics g = Graphics.FromImage(MainBitmap))
                        {
                            // Seçim koordinatlarýný orijinal bitmap koordinat sistemine dönüþtür
                            Rectangle targetRect = new Rectangle(
                                (int)(originalSelectionRectangleLocation.X),
                                (int)(originalSelectionRectangleLocation.Y),
                                (int)(originalSelectionRectangleSize.Width),
                                (int)(originalSelectionRectangleSize.Height));

                            // Hedef dikdörtgenin ana bitmap sýnýrlarý içinde kalmasýný saðla
                            targetRect = EnsureRectWithinBounds(targetRect, MainBitmap.Size);

                            // Kaynak dikdörtgenin de SelectedBitmap sýnýrlarý içinde kalmasýný saðla
                            Rectangle sourceRect = new Rectangle(0, 0,
                                Math.Min(SelectedBitmap.Width, targetRect.Width),
                                Math.Min(SelectedBitmap.Height, targetRect.Height));

                            // Yüksek kalite ayarlarýný kullan
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // Dönüþtürülmüþ alaný ana resme çiz
                            g.DrawImage(SelectedBitmap, targetRect, sourceRect, GraphicsUnit.Pixel);
                        }

                        // Clear the SelectedBitmap
                        SelectedBitmap.Dispose();
                        SelectedBitmap = null;
                    }

                    // Update the PictureBox with the new bitmap
                    pictureBoxCanvas.Image = MainBitmap;
                    pictureBoxCanvas.Invalidate();
                }
            }
        }

        // Yardýmcý metot: Dikdörtgenin belirtilen sýnýrlar içinde kalmasýný saðlar
        private Rectangle EnsureRectWithinBounds(Rectangle rect, Size bounds)
        {
            Rectangle result = new Rectangle(rect.Location, rect.Size);

            // X koordinatýný kontrol et
            if (result.X < 0)
                result.X = 0;
            if (result.X + result.Width > bounds.Width)
                result.Width = Math.Max(0, bounds.Width - result.X);

            // Y koordinatýný kontrol et
            if (result.Y < 0)
                result.Y = 0;
            if (result.Y + result.Height > bounds.Height)
                result.Height = Math.Max(0, bounds.Height - result.Y);

            return result;
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isImageCurrentlyCreating)
            {
                if (isdrawing)
                {
                    switch (selectedTool)
                    {
                        case Tools.Line:
                        case Tools.Round:
                        case Tools.Rectangle:
                        case Tools.RoundedRectangle:
                        case Tools.Triangle:
                        case Tools.Hexagon:
                            if (isSelected && !SelectionRectangle.IsEmpty)
                            {
                                // Calculate relative coordinates
                                int relativeStartX = previewStartPoint.X - (int)(SelectionRectangle.X / zoom);
                                int relativeStartY = previewStartPoint.Y - (int)(SelectionRectangle.Y / zoom);
                                int relativeEndX = (int)Math.Round(e.X / zoom) - (int)(SelectionRectangle.X / zoom);
                                int relativeEndY = (int)Math.Round(e.Y / zoom) - (int)(SelectionRectangle.Y / zoom);

                                // Create a completely fresh bitmap rather than modifying the existing one
                                Bitmap newSelectedBitmap = new Bitmap(
                                    (int)(SelectionRectangle.Width / zoom),
                                    (int)(SelectionRectangle.Height / zoom));

                                // Copy the original content from MainBitmap to the new bitmap
                                using (Graphics g = Graphics.FromImage(newSelectedBitmap))
                                {
                                    g.Clear(Color.Transparent);

                                    // Source rectangle from main bitmap
                                    Rectangle sourceRect = new Rectangle(
                                        (int)(originalSelectionRectangleLocation.X),
                                        (int)(originalSelectionRectangleLocation.Y),
                                        (int)(originalSelectionRectangleSize.Width),
                                        (int)(originalSelectionRectangleSize.Height));

                                    // Draw the portion from MainBitmap
                                    g.DrawImage(MainBitmap,
                                        new Rectangle(0, 0, newSelectedBitmap.Width, newSelectedBitmap.Height),
                                        sourceRect,
                                        GraphicsUnit.Pixel);

                                    // Now draw the shape directly on this fresh bitmap
                                    // This way we don't need a separate preview bitmap
                                    switch (selectedTool)
                                    {
                                        case Tools.Line:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                g.DrawLine(pen, relativeStartX, relativeStartY, relativeEndX, relativeEndY);
                                            }
                                            break;
                                        case Tools.Round:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(relativeStartX, relativeEndX),
                                                    Math.Min(relativeStartY, relativeEndY),
                                                    Math.Abs(relativeEndX - relativeStartX),
                                                    Math.Abs(relativeEndY - relativeStartY)
                                                );
                                                g.DrawEllipse(pen, rect);
                                            }
                                            break;
                                        case Tools.Rectangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(relativeStartX, relativeEndX),
                                                    Math.Min(relativeStartY, relativeEndY),
                                                    Math.Abs(relativeEndX - relativeStartX),
                                                    Math.Abs(relativeEndY - relativeStartY)
                                                );
                                                g.DrawRectangle(pen, rect);
                                            }
                                            break;
                                        case Tools.RoundedRectangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(relativeStartX, relativeEndX),
                                                    Math.Min(relativeStartY, relativeEndY),
                                                    Math.Abs(relativeEndX - relativeStartX),
                                                    Math.Abs(relativeEndY - relativeStartY)
                                                );

                                                GraphicsPath path = new GraphicsPath();
                                                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                                                path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
                                                path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
                                                path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
                                                path.CloseFigure();

                                                g.DrawPath(pen, path);
                                            }
                                            break;
                                        case Tools.Triangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Point[] points = new Point[]
                                                {
                                    new Point(relativeStartX, relativeEndY),
                                    new Point(relativeEndX, relativeEndY),
                                    new Point((relativeStartX + relativeEndX) / 2, relativeStartY)
                                                };
                                                g.DrawPolygon(pen, points);
                                            }
                                            break;
                                        case Tools.Hexagon:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Point[] hexagonPoints = new Point[points];
                                                double angle = 2 * Math.PI / points;
                                                for (int i = 0; i < points; i++)
                                                {
                                                    hexagonPoints[i] = new Point(
                                                        (int)(relativeStartX + (relativeEndX - relativeStartX) * Math.Cos(i * angle)),
                                                        (int)(relativeStartY + (relativeEndY - relativeStartY) * Math.Sin(i * angle))
                                                    );
                                                }
                                                g.DrawPolygon(pen, hexagonPoints);
                                            }
                                            break;
                                    }
                                }

                                // Dispose of the old bitmap before assigning the new one
                                if (SelectedBitmap != null)
                                {
                                    SelectedBitmap.Dispose();
                                }

                                // Set the new bitmap
                                SelectedBitmap = newSelectedBitmap;

                                pictureBoxCanvas.Invalidate();
                                if (blurEffect != BlurEffect.None && artisticFilters != ArtisticFilters.None)
                                {
                                    RefreshFiltersPreview();
                                }
                            }
                            else
                            {
                                // For drawing on the main canvas
                                // Create a new bitmap for the preview
                                Bitmap newPreview = new Bitmap(MainBitmap.Width, MainBitmap.Height);

                                using (Graphics g = Graphics.FromImage(newPreview))
                                {
                                    // Draw the original bitmap first
                                    g.DrawImage(MainBitmap, 0, 0);

                                    // Now draw the shape on top
                                    int startX = previewStartPoint.X;
                                    int startY = previewStartPoint.Y;
                                    int endX = (int)Math.Round(e.X / zoom);
                                    int endY = (int)Math.Round(e.Y / zoom);

                                    switch (selectedTool)
                                    {
                                        case Tools.Line:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                g.DrawLine(pen, startX, startY, endX, endY);
                                            }
                                            break;
                                        case Tools.Round:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(startX, endX),
                                                    Math.Min(startY, endY),
                                                    Math.Abs(endX - startX),
                                                    Math.Abs(endY - startY)
                                                );
                                                g.DrawEllipse(pen, rect);
                                            }
                                            break;
                                        case Tools.Rectangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(startX, endX),
                                                    Math.Min(startY, endY),
                                                    Math.Abs(endX - startX),
                                                    Math.Abs(endY - startY)
                                                );
                                                g.DrawRectangle(pen, rect);
                                            }
                                            break;
                                        case Tools.RoundedRectangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Rectangle rect = new Rectangle(
                                                    Math.Min(startX, endX),
                                                    Math.Min(startY, endY),
                                                    Math.Abs(endX - startX),
                                                    Math.Abs(endY - startY)
                                                );

                                                GraphicsPath path = new GraphicsPath();
                                                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                                                path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
                                                path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
                                                path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
                                                path.CloseFigure();

                                                g.DrawPath(pen, path);
                                            }
                                            break;
                                        case Tools.Triangle:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Point[] points = new Point[]
                                                {
                                    new Point(startX, endY),
                                    new Point(endX, endY),
                                    new Point((startX + endX) / 2, startY)
                                                };
                                                g.DrawPolygon(pen, points);
                                            }
                                            break;
                                        case Tools.Hexagon:
                                            using (Pen pen = new Pen(color1, shapeThickness))
                                            {
                                                Point[] hexagonPoints = new Point[points];
                                                double angle = 2 * Math.PI / points;
                                                for (int i = 0; i < points; i++)
                                                {
                                                    hexagonPoints[i] = new Point(
                                                        (int)(startX + (endX - startX) * Math.Cos(i * angle)),
                                                        (int)(startY + (endY - startY) * Math.Sin(i * angle))
                                                    );
                                                }
                                                g.DrawPolygon(pen, hexagonPoints);
                                            }
                                            break;
                                    }
                                }

                                // Swap out the image instead of modifying existing
                                var oldImage = pictureBoxCanvas.Image;
                                pictureBoxCanvas.Image = newPreview;

                                // Dispose of old image if not the main bitmap
                                if (oldImage != null && oldImage != MainBitmap)
                                {
                                    oldImage.Dispose();
                                }

                                pictureBoxCanvas.Invalidate();
                            }
                            break;

                        case Tools.Selection:
                            // Selection code remains the same
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

                // Cursor position handling remains the same
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
        }
        private void drawIntoCanvas(MouseEventArgs e)
        {
            if (!isSelected && MainBitmap == null)
            {
                MainBitmap = new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            }
            else if (isSelected && SelectedBitmap == null)
            {
                if (SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                {
                    SelectedBitmap = new Bitmap((int)(SelectionRectangle.Width / zoom), (int)(SelectionRectangle.Height / zoom));
                }
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    return; // Or handle the case where SelectedBitmap is not initialized
                }
                using (Graphics graphics = Graphics.FromImage(SelectedBitmap))
                {

                    if (isdrawing)
                    {
                        switch (selectedTool)
                        {
                            case Tools.Brush:
                                switch (comboBoxBrushType.SelectedIndex)
                                {
                                    case 0: // Regular brush
                                        DrawBrush(graphics, BrushShapes.DrawCircleBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Pen:
                                switch (comboBoxPenType.SelectedIndex)
                                {
                                    case 0: // Regular pen
                                        DrawBrush(graphics, BrushShapes.DrawSquareBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Eraser:
                                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                DrawBrush(graphics, BrushShapes.DrawCircleBrush, Color.FromArgb(0, 0, 0, 0), eraserSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                break;
                            case Tools.Spray:
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                break;
                            case Tools.ColorDrop:
                                Color pixelColor = MainBitmap.GetPixel((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom));
                                color1 = pixelColor;
                                foregroundColorButton.BackColor = pixelColor;
                                break;
                            default:
                                break;
                        }

                        x = (int)Math.Round((e.X - SelectionRectangle.X) / zoom);
                        y = (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom);
                    }
                }
            }
            else
            {
                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                {
                    if (isdrawing)
                    {
                        switch (selectedTool)
                        {
                            case Tools.Brush:
                                switch (comboBoxBrushType.SelectedIndex)
                                {
                                    case 0: // Regular brush
                                        DrawBrush(graphics, BrushShapes.DrawCircleBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Pen:
                                switch (comboBoxPenType.SelectedIndex)
                                {
                                    case 0: // Regular pen
                                        DrawBrush(graphics, BrushShapes.DrawSquareBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Eraser:
                                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                DrawBrush(graphics, BrushShapes.DrawCircleBrush, Color.FromArgb(0, 0, 0, 0), eraserSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                break;
                            case Tools.Spray:
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                break;
                            case Tools.ColorDrop:
                                Color pixelColor = MainBitmap.GetPixel((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom));
                                color1 = pixelColor;
                                foregroundColorButton.BackColor = pixelColor;
                                break;
                            default:
                                break;
                        }

                        x = (int)Math.Round(e.X / zoom);
                        y = (int)Math.Round(e.Y / zoom);
                    }
                }
            }

            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }
        private void pictureBoxCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxCanvas.Image != null)
            {
                // Draw the SelectedBitmap if it exists
                if (SelectedBitmap != null && !SelectionRectangle.IsEmpty)
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    e.Graphics.DrawImage(
                        SelectedBitmap,
                        SelectionRectangle,
                        new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                        GraphicsUnit.Pixel);
                }

                // Draw the selection rectangle with dashed lines
                if (!SelectionRectangle.IsEmpty)
                {
                    using (Pen blackPen = new Pen(Color.Black, 2)
                    {
                        DashStyle = DashStyle.Custom,
                        DashPattern = new float[] { 4, 4 }
                    })
                    {
                        e.Graphics.DrawRectangle(blackPen, SelectionRectangle);
                    }

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
            // Scale the location based on the zoom level
            Point scaledLocation = new Point((int)Math.Round(location.X / zoom), (int)Math.Round(location.Y / zoom));

            if (x == -1 && y == -1)
            {
                // Set the initial position without drawing
                x = scaledLocation.X;
                y = scaledLocation.Y;
                drawAction(graphics, color, size, scaledLocation);
            }
            else
            {
                // Draw and fill the gap between the previous and current positions
                FillGap(graphics, drawAction, color, size, new Point(x, y), location);
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
            if(!isImageCurrentlyCreating)
            {
                isdrawing = true;
                x = -1;
                y = -1;
                CancelFilters();
                switch (selectedTool)
                {
                    case Tools.Line:
                    case Tools.Round:
                    case Tools.Rectangle:
                    case Tools.RoundedRectangle:
                    case Tools.Triangle:
                    case Tools.Hexagon:
                        SaveStateForUndo();

                        // Whether in a selection or on main canvas, store the starting point
                        startPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                        previewStartPoint = startPoint;

                        // If there's a selection and we're inside it, setup for drawing in the selection
                        if (isSelected && !SelectionRectangle.IsEmpty && SelectionRectangle.Contains(e.Location))
                        {
                            // If we had a SelectedBitmap, dispose of it
                            if (SelectedBitmap != null)
                            {
                                SelectedBitmap.Dispose();
                                SelectedBitmap = null;
                            }

                            // Create a fresh bitmap for the selection
                            SelectedBitmap = new Bitmap(
                                (int)(SelectionRectangle.Width / zoom),
                                (int)(SelectionRectangle.Height / zoom));

                            using (Graphics g = Graphics.FromImage(SelectedBitmap))
                            {
                                g.Clear(Color.Transparent);

                                // Copy the content from MainBitmap for the selection area
                                Rectangle sourceRect = new Rectangle(
                                    (int)(originalSelectionRectangleLocation.X),
                                    (int)(originalSelectionRectangleLocation.Y),
                                    (int)(originalSelectionRectangleSize.Width),
                                    (int)(originalSelectionRectangleSize.Height));

                                g.DrawImage(MainBitmap,
                                    new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                                    sourceRect,
                                    GraphicsUnit.Pixel);
                            }
                        }
                        break;

                    case Tools.Selection:
                        if (isSelected && !SelectionRectangle.Contains(e.Location))
                        {
                            SetAsUnselected();
                        }
                        isSelected = true;
                        SelectionStartPoint = e.Location;
                        pictureBoxCanvas.Invalidate();
                        break;

                    default:
                        if (selectedTool == Tools.Brush || selectedTool == Tools.Pen || selectedTool == Tools.Eraser ||
                            selectedTool == Tools.Spray || selectedTool == Tools.Bucket || selectedTool == Tools.Text)
                        {
                            SaveStateForUndo();
                        }
                        drawIntoCanvas(e); // To draw immediately on mouse down
                        break;
                }
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
        private StringAlignment GetHorizontalAlignment(HorizontalAlignment alignment)
        {
            return alignment switch
            {
                HorizontalAlignment.Left => StringAlignment.Near,
                HorizontalAlignment.Center => StringAlignment.Center,
                HorizontalAlignment.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            };
        }

        private PointF GetAlignedTextPosition(TextBox textBox, float zoom, Rectangle selectionRectangle)
        {
            // Calculate the base position
            float x = (float)Math.Round(textBox.Location.X / zoom) - selectionRectangle.X;
            float y = (float)Math.Round(textBox.Location.Y / zoom) - selectionRectangle.Y;

            // Measure the size of the text
            using (Graphics g = Graphics.FromImage(MainBitmap))
            {
                SizeF textSize = g.MeasureString(textBox.Text, textBox.Font);

                // Adjust the x-coordinate based on the alignment
                switch (textBox.TextAlign)
                {
                    case HorizontalAlignment.Center:
                        x += textSize.Width / 2;
                        break;
                    case HorizontalAlignment.Right:
                        x += textSize.Width;
                        break;
                }
            }

            return new PointF(x, y);
        }

        private void pictureBoxCanvas_Click(object sender, EventArgs e)
        {
            if (!isImageCurrentlyCreating)
            {
                toolStripArtisticFilters.Visible = false;
                toolStripPixelate.Visible = false;
                toolStripGaussianBlur.Visible = false;
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
                                Font = new Font(fontsComboBox.Text, textSize, fontStyle), // Yazý tipi ayarý  
                                BorderStyle = BorderStyle.FixedSingle // Kenarlýk stili 
                            };
                            switch (textToolAlign)
                            {
                                case TextToolAlign.Left:
                                    textBox.TextAlign = HorizontalAlignment.Left;
                                    break;
                                case TextToolAlign.Middle:
                                    textBox.TextAlign = HorizontalAlignment.Center;
                                    break;
                                case TextToolAlign.Right:
                                    textBox.TextAlign = HorizontalAlignment.Right;
                                    break;
                                default:
                                    textBox.TextAlign = HorizontalAlignment.Left;
                                    break;
                            }
                            if (backgroundFilling == true)
                            {
                                textBox.BackColor = color2;
                            }
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
                                if (isSelected && !SelectionRectangle.IsEmpty && SelectedBitmap != null)
                                {
                                    // Draw the TextBox content onto the bitmap  
                                    using (Graphics graphics = Graphics.FromImage(SelectedBitmap))
                                    {
                                        if (backgroundFilling && !string.IsNullOrEmpty(textBox.Text))
                                        {
                                            // Draw background rectangle
                                            RectangleF backgroundRect = new RectangleF(
                                                (int)Math.Round(textBox.Location.X / zoom) - SelectionRectangle.X,
                                                (int)Math.Round(textBox.Location.Y / zoom) - SelectionRectangle.Y,
                                                textBox.Width / zoom,
                                                textBox.Height / zoom
                                            );
                                            using (Brush backgroundBrush = new SolidBrush(color2))
                                            {
                                                graphics.FillRectangle(backgroundBrush, backgroundRect);
                                            }
                                        }

                                        // Draw the text
                                        graphics.DrawString(
                                        textBox.Text,
                                        textBox.Font,
                                        new SolidBrush(color1),
                                        GetAlignedTextPosition(textBox, zoom, SelectionRectangle),
                                        new StringFormat
                                        {
                                            Alignment = GetHorizontalAlignment(textBox.TextAlign),
                                            LineAlignment = StringAlignment.Near // Adjust for vertical alignment if needed
                                        });

                                    }
                                    MergeMainBitmapWithSelected();
                                }
                                else
                                {
                                    // Draw the TextBox content onto the bitmap  
                                    using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                    {
                                        if (backgroundFilling && !string.IsNullOrEmpty(textBox.Text))
                                        {
                                            // Draw background rectangle
                                            RectangleF backgroundRect = new RectangleF(
                                                (int)Math.Round(textBox.Location.X / zoom),
                                                (int)Math.Round(textBox.Location.Y / zoom),
                                                textBox.Width / zoom,
                                                textBox.Height / zoom
                                            );
                                            using (Brush backgroundBrush = new SolidBrush(color2))
                                            {
                                                graphics.FillRectangle(backgroundBrush, backgroundRect);
                                            }
                                        }

                                        // Draw the text
                                        graphics.DrawString(
                                        textBox.Text,
                                        textBox.Font,
                                        new SolidBrush(color1),
                                        GetAlignedTextPosition(textBox, zoom, SelectionRectangle),
                                        new StringFormat
                                        {
                                            Alignment = GetHorizontalAlignment(textBox.TextAlign),
                                            LineAlignment = StringAlignment.Near // Adjust for vertical alignment if needed
                                        });

                                    }
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
            else
            {
                SystemSounds.Beep.Play();
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
            try
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
            catch (FileNotFoundException)
            {
                MessageBox.Show("The selected file was not found. Please check the file path.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the file: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                sprayToolSize = Settings1.Default.DefaultSpraySize;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                sprayToolSize = Settings1.Default.DefaultSpraySize;
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
                eraserSize = Settings1.Default.DefaultEraserSize;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                eraserSize = Settings1.Default.DefaultEraserSize;
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
                penSize = Settings1.Default.DefaultPenSize;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                penSize = Settings1.Default.DefaultPenSize;
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
                shapeThickness = Settings1.Default.DefaultShapeSize;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                shapeThickness = Settings1.Default.DefaultShapeSize;
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

        private void setShapeRadius()
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
                radius = Settings1.Default.DefaultRadiusSize;
                textBoxRadius.Text = radius.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                radius = Settings1.Default.DefaultRadiusSize;
                textBoxRadius.Text = radius.ToString();
            }
        }
        private void setShapePoints()
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
                points = Settings1.Default.DefaultPointsCount;
                textBoxPoints.Text = points.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                points = Settings1.Default.DefaultPointsCount;
                textBoxPoints.Text = points.ToString();
            }
        }
        private void textBoxRadius_Leave(object sender, EventArgs e)
        {
            setShapeRadius();
        }

        private void textBoxPoints_Leave(object sender, EventArgs e)
        {
            setShapePoints();
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
                SelectedBitmap = null; // Clear selected bitmap
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
                SelectedBitmap = null; // Clear selected bitmap
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
        private void MergeMainBitmapWithSelected()
        {

        }
        private void SetAsUnselected()
        {
            if (!isImageCurrentlyCreating)
            {
                // Seçim kaldýrýlýrken mevcut içeriði koruyarak iþlemi gerçekleþtirin
                if (isSelected && SelectedBitmap != null && !SelectionRectangle.IsEmpty)
                {
                    if (artisticFilters != ArtisticFilters.None)
                    {
                        CancelFilters();
                    }
                    if (blurEffect != BlurEffect.None)
                    {
                        CancelFilters();
                    }
                    toolStripArtisticFilters.Visible = false;
                    toolStripPixelate.Visible = false;
                    toolStripGaussianBlur.Visible = false;
                    using (Graphics g = Graphics.FromImage(MainBitmap))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // Seçili alaný MainBitmap üzerine çizin
                        Rectangle sourceRect = new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height);
                        Rectangle destRect = new Rectangle(
                            originalSelectionRectangleLocation.X,
                            originalSelectionRectangleLocation.Y,
                            originalSelectionRectangleSize.Width,
                            originalSelectionRectangleSize.Height);

                        // MainBitmap'in mevcut içeriðini koruyarak seçili alaný güncelle
                        g.DrawImage(SelectedBitmap, destRect, sourceRect, GraphicsUnit.Pixel);
                    }

                    // SelectedBitmap'i serbest býrakýn
                    MergeMainBitmapWithSelected();
                    SelectedBitmap.Dispose();
                    SelectedBitmap = null;
                }

                // Seçim durumunu sýfýrlayýn
                SelectionRectangle = Rectangle.Empty;
                isSelected = false;
                if (artisticFilters == ArtisticFilters.None || blurEffect == BlurEffect.None)
                {
                    pictureBoxCanvas.Image = MainBitmap;
                }
                else
                {
                    pictureBoxCanvas.Image = previewBitmap;
                }
                pictureBoxCanvas.Invalidate();
            }
        }

        private void setBucketToolTolerance()
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
                tolerance = Settings1.Default.DefaultBucketTolerance;
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tolerance = Settings1.Default.DefaultBucketTolerance;
            }
            finally
            {
                textBoxTolerance.Text = tolerance.ToString();
            }
        }
        private void toolStripTextBox1_Leave(object sender, EventArgs e)
        {
            setBucketToolTolerance();
        }

        private void mirrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetAsUnselected();
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
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
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.Flash(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.Flash(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }


        private void frozenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.Frozen(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.Frozen(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void winterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.Winter(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.Winter(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void blackAndWhiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.BlackAndWhite(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.BlackAndWhite(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void oldPictureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.OldImage(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.OldImage(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void cherryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.CherryFilter(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.CherryFilter(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void lightAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.LightAdd(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.LightAdd(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void purpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.PurpleEffect(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.PurpleEffect(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }

        private void fogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            if (artisticFilters != ArtisticFilters.None)
            {
                artisticFilters = ArtisticFilters.None;
            }
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.BasicFilters.FogEffect(SelectedBitmap);
            }
            else
            {
                MainBitmap = Filters.BasicFilters.FogEffect(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
        }
        private void RefreshFiltersPreview()
        {
            previewBitmap = new Bitmap(MainBitmap);
            if (isSelected)
            {
                // Create SelectedBitmap if it doesn't exist
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(previewBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }

                // Create a temporary copy of SelectedBitmap to apply filter
                Bitmap filteredBitmap;

                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        filteredBitmap = Filters.ArtisticFilters.OilPaintFilter(SelectedBitmap,
                            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity,
                            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize,
                            (byte)FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold);
                        break;
                    case ArtisticFilters.Cartoon:
                        filteredBitmap = Filters.ArtisticFilters.CartoonFilter(SelectedBitmap,
                            FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity,
                            FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize,
                            (byte)FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold);
                        break;
                    default:
                        filteredBitmap = new Bitmap(SelectedBitmap);
                        switch (blurEffect)
                        {
                            case BlurEffect.Pixelate:
                                filteredBitmap = Filters.BlurringFilters.Pixelate(filteredBitmap,
                                    pixelationSize, pixelationOffsetX, pixelationOffsetY);
                                break;
                            case BlurEffect.Gaussian:
                                filteredBitmap = Filters.BlurringFilters.GaussianBlur(filteredBitmap,
                                    gaussianBlurSize);
                                break;
                        }
                        break;
                }

                // Update SelectedBitmap with filtered result
                SelectedBitmap.Dispose();
                SelectedBitmap = filteredBitmap;

                // Keep MainBitmap as the image but update the display to show selection
                pictureBoxCanvas.Image = previewBitmap;
            }
            else
            {
                // For the entire image
                Bitmap filteredBitmap = null;

                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        filteredBitmap = Filters.ArtisticFilters.OilPaintFilter(MainBitmap,
                            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity,
                            FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize,
                            (byte)FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold);
                        break;
                    case ArtisticFilters.Cartoon:
                        filteredBitmap = Filters.ArtisticFilters.CartoonFilter(MainBitmap,
                            FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity,
                            FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize,
                            (byte)FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold);
                        break;
                    default:
                        switch (blurEffect)
                        {
                            case BlurEffect.Pixelate:
                                filteredBitmap = Filters.BlurringFilters.Pixelate(MainBitmap,
                                    pixelationSize, pixelationOffsetX, pixelationOffsetY);
                                break;
                            case BlurEffect.Gaussian:
                                filteredBitmap = Filters.BlurringFilters.GaussianBlur(MainBitmap,
                                    gaussianBlurSize);
                                break;
                            default:
                                filteredBitmap = new Bitmap(MainBitmap);
                                break;
                        }
                        break;
                }

                previewBitmap = filteredBitmap;
                pictureBoxCanvas.Image = previewBitmap;
            }

            pictureBoxCanvas.Invalidate();
        }
        private void buttonArtisticFiltersOK_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            MainBitmap = previewBitmap;
            artisticFilters = ArtisticFilters.None;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            toolStripArtisticFilters.Visible = false;
        }

        private void buttonArtisticFiltersCancel_Click(object sender, EventArgs e)
        {
            toolStripArtisticFilters.Visible = false;
            artisticFilters = ArtisticFilters.None;
            CancelFilters();
        }
        private void CancelFilters()
        {
            if (previewBitmap != null && previewBitmap != MainBitmap)
            {
                previewBitmap.Dispose();
                previewBitmap = null;
            }

            if (isSelected && SelectedBitmap != null)
            {
                SelectedBitmap.Dispose();
                SelectedBitmap = null;

                if (!SelectionRectangle.IsEmpty && MainBitmap != null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));

                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));

                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
            }

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
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            toolStripArtisticFilters.Visible = true;
            artisticFilters = (ArtisticFilters)filterType;
            switch (artisticFilters)
            {
                case ArtisticFilters.OilPainting:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold.ToString();
                    RefreshFiltersPreview();
                    break;
                case ArtisticFilters.Cartoon:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold.ToString();
                    RefreshFiltersPreview();
                    break;
                default:
                    break;
            }
        }
        private void setArtisticFilterSize()
        {
            try
            {
                if (!string.IsNullOrEmpty(textBoxArtisticFilterSize.Text))
                {
                    switch (artisticFilters)
                    {
                        case ArtisticFilters.OilPainting:
                            if (Convert.ToInt32(textBoxArtisticFilterSize.Text) > 1 && Convert.ToInt32(textBoxArtisticFilterSize.Text) < 33)
                            {
                                FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Convert.ToInt32(textBoxArtisticFilterSize.Text);
                            }
                            else
                            {
                                throw new OverflowException();
                            }
                            break;
                        case ArtisticFilters.Cartoon:
                            if (Convert.ToInt32(textBoxArtisticFilterSize.Text) > 1 && Convert.ToInt32(textBoxArtisticFilterSize.Text) < 33)
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
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Settings1.Default.DefaultOilPaintFilterSize;
                        textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = Settings1.Default.DefaultCartoonFilterSize;
                        textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize.ToString();
                        break;
                }
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Settings1.Default.DefaultOilPaintFilterSize;
                        textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = Settings1.Default.DefaultCartoonFilterSize;
                        textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize.ToString();
                        break;
                }
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void setArtisticFilterIntensity()
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
                MessageBox.Show("Invalid intensity value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = Settings1.Default.DefaultOilPaintFilterIntensity;
                        textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = Settings1.Default.DefaultCartoonFilterIntensity;
                        textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity.ToString();
                        break;
                }
            }
            catch (OverflowException)
            {
                MessageBox.Show("Intensity value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = Settings1.Default.DefaultOilPaintFilterIntensity;
                        textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = Settings1.Default.DefaultCartoonFilterIntensity;
                        textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity.ToString();
                        break;
                }
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void setArtisticFilterThreshold()
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
                MessageBox.Show("Invalid threshold value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = Settings1.Default.DefaultOilPaintFilterThreshold;
                        textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = Settings1.Default.DefaultCartoonFilterThreshold;
                        textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold.ToString();
                        break;
                }
            }
            catch (OverflowException)
            {
                MessageBox.Show("Threshold value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                switch (artisticFilters)
                {
                    case ArtisticFilters.OilPainting:
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = Settings1.Default.DefaultOilPaintFilterThreshold;
                        textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold.ToString();
                        break;
                    case ArtisticFilters.Cartoon:
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = Settings1.Default.DefaultCartoonFilterThreshold;
                        textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold.ToString();
                        break;
                }
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void textBoxArtisticFilterSize_Leave(object sender, EventArgs e)
        {
            setArtisticFilterSize();
        }

        private void textBoxArtisticFilterIntensity_Leave(object sender, EventArgs e)
        {
            setArtisticFilterIntensity();
        }

        private void textBoxArtisticFilterThreshold_Leave(object sender, EventArgs e)
        {
            setArtisticFilterThreshold();
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
            if (blurEffect != BlurEffect.None)
            {
                blurEffect = BlurEffect.None;
            }
            if (isSelected)
            {
                if (SelectedBitmap == null)
                {
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));
                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            (int)(originalSelectionRectangleLocation.X),
                            (int)(originalSelectionRectangleLocation.Y),
                            (int)(originalSelectionRectangleSize.Width),
                            (int)(originalSelectionRectangleSize.Height));
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
                SelectedBitmap = Filters.AmbientFilters.Chloe(MainBitmap);
            }
            else
            {
                MainBitmap = Filters.AmbientFilters.Chloe(MainBitmap);
            }
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
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
                SystemSounds.Beep.Play(); // Play a beep sound when the zoom limit is reached
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
                SystemSounds.Beep.Play(); // Play a beep sound when the zoom limit is reached
            }
        }
        private void ScaleSelection()
        {
            if (isSelected && (SelectionRectangle != Rectangle.Empty))
            {
                SelectionRectangle.Location = new Point(
                    (int)Math.Round(originalSelectionRectangleLocation.X * zoom),
                    (int)Math.Round(originalSelectionRectangleLocation.Y * zoom)
                );
                SelectionRectangle.Size = new Size(
                    (int)Math.Round(originalSelectionRectangleSize.Width * zoom),
                    (int)Math.Round(originalSelectionRectangleSize.Height * zoom)
                );

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

        private void buttonBold_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonBold.Checked)
            {
                fontStyle |= FontStyle.Bold;
            }
            else
            {
                fontStyle &= ~FontStyle.Bold;
            }
        }

        private void buttonItalic_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonItalic.Checked)
            {
                fontStyle |= FontStyle.Italic;
            }
            else
            {
                fontStyle &= ~FontStyle.Italic;
            }
        }

        private void buttonUnderline_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonUnderline.Checked)
            {
                fontStyle |= FontStyle.Underline;
            }
            else
            {
                fontStyle &= ~FontStyle.Underline;
            }
        }

        private void buttonStrikeout_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonStrikeout.Checked)
            {
                fontStyle |= FontStyle.Strikeout;
            }
            else
            {
                fontStyle &= ~FontStyle.Strikeout;
            }
        }

        private void buttonAlignLeft_Click(object sender, EventArgs e)
        {
            textToolAlign = TextToolAlign.Left;
            buttonAlignLeft.Checked = true;
            buttonAlignMiddle.Checked = false;
            buttonAlignRight.Checked = false;
        }

        private void buttonAlignMiddle_Click(object sender, EventArgs e)
        {
            textToolAlign = TextToolAlign.Middle;
            buttonAlignMiddle.Checked = true;
            buttonAlignLeft.Checked = false;
            buttonAlignRight.Checked = false;
        }

        private void buttonAlignRight_Click(object sender, EventArgs e)
        {
            textToolAlign = TextToolAlign.Right;
            buttonAlignRight.Checked = true;
            buttonAlignLeft.Checked = false;
            buttonAlignMiddle.Checked = false;
        }

        private void buttonBackgroundFilling_CheckedChanged(object sender, EventArgs e)
        {
            backgroundFilling = buttonBackgroundFilling.Checked;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            toolStripAICreateImage.Visible = false;
        }
        private void ClearRedoStack()
        {
            while (redoStack.Count > 0)
            {
                var state = redoStack.Pop();
                state.Bitmap.Dispose();
                UpdateUndoRedoButtons();
            }
        }
        private async void buttonCreate_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            try
            {
                isImageCurrentlyCreating = true;
                canvasPanel.Enabled = false;
                menuStrip1.Enabled = false;
                toolStripTools.Enabled = false;
                toolStripSeparator29.Visible = true;
                labelCreatingImage.Visible = true;
                progressBarAIImageCreation.Visible = true;
                buttonCreate.Enabled = false;
                textBoxPrompt.Enabled = false;
                labelPrompt.Enabled = false;
                buttonClose.Enabled = false;
                if (isSelected && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                {
                    if (SelectedBitmap == null)
                    {
                        SelectedBitmap = new Bitmap(
                            (int)(SelectionRectangle.Width / zoom),
                            (int)(SelectionRectangle.Height / zoom));
                        using (Graphics g = Graphics.FromImage(SelectedBitmap))
                        {
                            g.Clear(Color.Transparent);
                            Rectangle sourceRect = new Rectangle(
                                (int)(originalSelectionRectangleLocation.X),
                                (int)(originalSelectionRectangleLocation.Y),
                                (int)(originalSelectionRectangleSize.Width),
                                (int)(originalSelectionRectangleSize.Height));
                            g.DrawImage(MainBitmap,
                                new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                                sourceRect,
                                GraphicsUnit.Pixel);
                        }
                    }
                    var imageTask = CreateAIImages.EditImage(SelectedBitmap, textBoxPrompt.Text);
                    var image = await imageTask; // Await the task to get the result
                    if (image != null)
                    {
                        SelectedBitmap = new Bitmap(image); // Convert Image to Bitmap  
                        MergeMainBitmapWithSelected();
                    }
                    else
                    {
                        Undo();
                        ClearRedoStack(); 
                        MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    var imageTask = CreateAIImages.EditImage(MainBitmap, textBoxPrompt.Text);
                    var image = await imageTask; // Await the task to get the result
                    if (image != null)
                    {
                        MainBitmap = new Bitmap(image); // Convert Image to Bitmap  
                        pictureBoxCanvas.Invalidate();
                    }
                    else
                    {
                        Undo();
                        ClearRedoStack(); 
                        MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }   
                
            }
            catch (NullReferenceException)
            {
                Undo();
                ClearRedoStack(); 
                MessageBox.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Undo();
                ClearRedoStack();
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isImageCurrentlyCreating = false;
                pictureBoxCanvas.Invalidate();
                canvasPanel.Enabled = true;
                menuStrip1.Enabled = true;
                toolStripTools.Enabled = true;
                toolStripSeparator29.Visible = false;
                labelCreatingImage.Visible = false;
                if (!string.IsNullOrEmpty(textBoxPrompt.Text))
                {
                    buttonCreate.Enabled = true;
                }
                textBoxPrompt.Enabled = true;
                labelPrompt.Enabled = true;
                buttonClose.Enabled = true;
                progressBarAIImageCreation.Visible = false;
                toolStripAICreateImage.Visible = false;
                textBoxPrompt.Clear();
            }
        }

        private void textBoxPrompt_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxPrompt.Text))
            {
                buttonCreate.Enabled = true;
            }
            else
            {
                buttonCreate.Enabled = false;
            }
        }

        private void buttonApplyPixelation_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            blurEffect = BlurEffect.None;
            MainBitmap = previewBitmap;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            toolStripPixelate.Visible = false;
        }

        private void buttonCancelPixelation_Click(object sender, EventArgs e)
        {
            toolStripPixelate.Visible = false;
            blurEffect = BlurEffect.None;
            CancelFilters();
        }

        private void pixellateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripPixelate.Visible = true;
            blurEffect = BlurEffect.Pixelate;
            RefreshFiltersPreview();
        }

        private void blurToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            toolStripGaussianBlur.Visible = true;
            blurEffect = BlurEffect.Gaussian;
            RefreshFiltersPreview();
        }

        private void setPixelationSize()
        {
            try
            {
                if (Convert.ToInt32(textBoxPixelationSize.Text) < 1 && Convert.ToInt32(textBoxPixelationSize.Text) > 9299)
                {
                    throw new OverflowException();
                }
                pixelationSize = Convert.ToInt32(textBoxPixelationSize.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationSize = Settings1.Default.DefaultPixelationSize;
                textBoxPixelationSize.Text = pixelationSize.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationSize = Settings1.Default.DefaultPixelationSize;
                textBoxPixelationSize.Text = pixelationSize.ToString();
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void setPixelateOffsetX()
        {
            try
            {
                if (Convert.ToInt32(textBoxPixelationOffsetX.Text) < -2944 && Convert.ToInt32(textBoxPixelationOffsetX.Text) > 10602)
                {
                    throw new OverflowException();
                }
                pixelationOffsetX = Convert.ToInt32(textBoxPixelationOffsetX.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetX = Settings1.Default.DefaultPixelationOffsetX;
                textBoxPixelationOffsetX.Text = pixelationOffsetX.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetX = Settings1.Default.DefaultPixelationOffsetX;
                textBoxPixelationOffsetX.Text = pixelationOffsetX.ToString();
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void setPixelateOffsetY()
        {
            try
            {
                if (Convert.ToInt32(textBoxPixelationOffsetY.Text) < -2944 && Convert.ToInt32(textBoxPixelationOffsetY.Text) > 10602)
                {
                    throw new OverflowException();
                }
                pixelationOffsetY = Convert.ToInt32(textBoxPixelationOffsetY.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY;
                textBoxPixelationOffsetY.Text = pixelationOffsetY.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Size value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY;
                textBoxPixelationOffsetY.Text = pixelationOffsetY.ToString();
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void textBoxPixelationSize_Leave(object sender, EventArgs e)
        {
            setPixelationSize();
        }

        private void textBoxPixelationOffsetX_Leave(object sender, EventArgs e)
        {
            setPixelateOffsetX();
        }

        private void textBoxPixelationOffsetY_Leave(object sender, EventArgs e)
        {
            setPixelateOffsetY();
        }

        private void textboxGaussianBlurRadius_Leave(object sender, EventArgs e)
        {
            setGaussianBlurRadius();
        }
        private void setGaussianBlurRadius()
        {
            try
            {
                if (float.Parse(textboxGaussianBlurRadius.Text) < 0 || float.Parse(textboxGaussianBlurRadius.Text) > 564.37)
                {
                    throw new OverflowException();
                }
                gaussianBlurSize = float.Parse(textboxGaussianBlurRadius.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid radius value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
                textboxGaussianBlurRadius.Text = gaussianBlurSize.ToString();
            }
            catch (OverflowException)
            {
                MessageBox.Show("Radius value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
                textboxGaussianBlurRadius.Text = gaussianBlurSize.ToString();
            }
            finally
            {
                RefreshFiltersPreview();
            }
        }
        private void buttonApplyGaussianBlur_Click(object sender, EventArgs e)
        {
            SaveStateForUndo();
            blurEffect = BlurEffect.None;
            MainBitmap = previewBitmap;
            pictureBoxCanvas.Image = MainBitmap;
            pictureBoxCanvas.Invalidate();
            toolStripGaussianBlur.Visible = false;
        }

        private void buttonCancelGaussianBlur_Click(object sender, EventArgs e)
        {
            toolStripGaussianBlur.Visible = false;
            blurEffect = BlurEffect.None;
            CancelFilters();
        }

        private void textboxGaussianBlurRadius_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setGaussianBlurRadius();
            }
        }

        private void textBoxArtisticFilterSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setArtisticFilterSize();
            }
        }

        private void textBoxArtisticFilterIntensity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setArtisticFilterIntensity();
            }
        }

        private void textBoxArtisticFilterThreshold_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setArtisticFilterThreshold();
            }
        }

        private void textBoxTolerance_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setBucketToolTolerance();
            }
        }

        private void textBoxPixelationSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setPixelationSize();
            }
        }

        private void textBoxPixelationOffsetX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setPixelateOffsetX();
            }
        }

        private void textBoxPixelationOffsetY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setPixelateOffsetY();
            }
        }

        private void textBoxRadius_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                setShapeRadius();
            }
        }

        private void textBoxPoints_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                setShapePoints();
            }
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
