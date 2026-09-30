// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using Carpathia.Properties;
using System.Collections;
using ExifLibrary;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Media;
using System.Runtime.Serialization;
using static System.Windows.Forms.DataFormats;
using System.ComponentModel;

namespace Carpathia
{
    public partial class ImageEditor : Form
    {
        bool isModified = false;
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
            tolerance2 = 50,
            pixelationSize = Settings1.Default.DefaultPixelationSize,
            pixelationOffsetX = Settings1.Default.DefaultPixelationOffsetX,
            pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY;
        float textSize = Settings1.Default.DefaultTextSize;
        float zoom = 1,
            gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
        bool isResizing = false, isSelected = false, backgroundFilling = false,
            isImageCurrentlyCreating = false, isSaved = false;
        private ResizeDirection resizeDirection;
        private BasicFilters basicFilters;
        private ArtisticFilters artisticFilters = ArtisticFilters.None;
        private Point lastMousePos;
        // Current mouse position over the pictureBox (control coordinates)
        private Point currentMousePosition = Point.Empty;
        private bool isMouseOverCanvas = false;
        Size originalSize; // Unzoomed size
        Size originalSelectionRectangleSize; // Unzoomed size
        Point originalSelectionRectangleLocation; // Unzoomed location
        Bitmap MainBitmap;
        Bitmap SelectedBitmap;
        Rectangle SelectionRectangle = new Rectangle();
        // Magic Wand is a mask-based selection. It deliberately does NOT use
        // SelectedBitmap/SelectionRectangle, which belong to transformable rectangular selections.
        private BitArray magicSelectionMask;
        private int magicSelectionMaskWidth;
        private int magicSelectionMaskHeight;
        private Rectangle magicSelectionBounds = Rectangle.Empty;
        private readonly List<(Point Start, Point End)> magicSelectionOutlineSegments = new();
        // Animated marching-ants phase. This changes only the white dash phase;
        // selection geometry and mask coordinates never move.
        private readonly System.Windows.Forms.Timer marchingAntsTimer = new System.Windows.Forms.Timer();
        private float marchingAntsOffset = 0f;
        Point SelectionStartPoint;
        FontStyle fontStyle = FontStyle.Regular;
        private TextToolAlign textToolAlign = TextToolAlign.Left;
        string openedFilePath = string.Empty;
        bool fileSaved = false;
        bool aiGenerated = false;

        // Layer system. MainBitmap always represents the currently selected/editable layer.
        // Each layer keeps its own bitmap snapshot and listView1 shows its live thumbnail
        // through imageListLayerThumbnails.
        private sealed class EditorLayer : IDisposable
        {
            public string Name { get; set; }
            public Bitmap Bitmap { get; set; }
            public bool Visible { get; set; } = true;
            public string ThumbnailKey { get; } = Guid.NewGuid().ToString("N");

            public EditorLayer(string name, Bitmap bitmap)
            {
                Name = name;
                Bitmap = bitmap;
            }

            public void Dispose()
            {
                Bitmap?.Dispose();
                Bitmap = null;
            }
        }

        private readonly List<EditorLayer> editorLayers = new();
        private int activeLayerIndex = -1;
        private bool suppressLayerSelectionChanged = false;

        // Only layers that actually changed are queued for thumbnail regeneration.
        // Selection/reordering/compositing never touches unrelated thumbnails.
        private readonly HashSet<string> dirtyLayerThumbnailKeys = new();
        private readonly System.Windows.Forms.Timer layerThumbnailRefreshTimer = new System.Windows.Forms.Timer();

        // MainBitmap is the editable bitmap of the active layer.
        // compositeCanvasBitmap is only the flattened on-screen preview.
        private Bitmap compositeCanvasBitmap;

        // Performance: avoid rebuilding the full layer composite for every
        // mouse-move while a drawing tool is active.
        private bool compositeDirty = true;
        private bool suppressCompositeDuringToolStroke = false;

        private sealed class LayerCanvasState : IDisposable
        {
            public string LayerKey { get; }
            public Bitmap Bitmap { get; }
            public Size Size { get; }

            public LayerCanvasState(string layerKey, Bitmap bitmap, Size size)
            {
                LayerKey = layerKey;
                Bitmap = bitmap;
                Size = size;
            }

            public void Dispose()
            {
                Bitmap?.Dispose();
            }
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        enum TextToolAlign
        {
            Left,
            Middle,
            Right
        }

        // Helper: draw a soft/realistic brush preview
        private void DrawBrushPreview(Graphics g, Point center, int size, int styleIndex, Color color, float zoom)
        {
            // Uniform round preview: filled circle with selected color and subtle outline
            int r = Math.Max(1, size / 2);
            // Semi-transparent fill using selected color
            using (Brush fill = new SolidBrush(Color.FromArgb(180, color)))
            {
                g.FillEllipse(fill, center.X - r, center.Y - r, r * 2, r * 2);
            }
            // Thin contrasting outline for visibility on any background
            Color outlineColor = Color.FromArgb(220, Color.Black);
            using (Pen outline = new Pen(outlineColor, Math.Max(1, (int)Math.Ceiling(zoom))))
            {
                outline.Alignment = PenAlignment.Center;
                g.DrawEllipse(outline, center.X - r, center.Y - r, r * 2, r * 2);
            }
        }

        // Helper: draw pen preview styles
        private void DrawPenPreview(Graphics g, Point center, int size, int styleIndex, Color color, float zoom)
        {
            // Unified round preview for pen: filled circle in selected color + outline
            int r = Math.Max(1, size / 2);
            using (Brush fill = new SolidBrush(Color.FromArgb(200, color)))
            {
                g.FillEllipse(fill, center.X - r, center.Y - r, r * 2, r * 2);
            }
            using (Pen outline = new Pen(Color.FromArgb(220, Color.Black), Math.Max(1, (int)Math.Ceiling(zoom))))
            {
                outline.Alignment = PenAlignment.Center;
                g.DrawEllipse(outline, center.X - r, center.Y - r, r * 2, r * 2);
            }
        }

        // Helper: draw spray preview (dots)
        private void DrawSprayPreview(Graphics g, Point center, int size, Color color, float zoom)
        {
            // Simplified round preview for spray: filled circle in selected color + dotted outline
            int r = Math.Max(1, size / 2);
            using (Brush fill = new SolidBrush(Color.FromArgb(160, color)))
            {
                g.FillEllipse(fill, center.X - r, center.Y - r, r * 2, r * 2);
            }
            using (Pen outline = new Pen(Color.FromArgb(180, Color.Black), Math.Max(1, (int)Math.Ceiling(zoom))))
            {
                outline.DashStyle = DashStyle.Dot;
                outline.Alignment = PenAlignment.Center;
                g.DrawEllipse(outline, center.X - r, center.Y - r, r * 2, r * 2);
            }
        }

        // Helper: draw eraser preview (dashed white circle with semi-transparent fill)
        private void DrawEraserPreview(Graphics g, Point center, int size, float zoom)
        {
            int r = Math.Max(1, size / 2);
            using (Brush b = new SolidBrush(Color.FromArgb(100, Color.White)))
            {
                g.FillEllipse(b, center.X - r, center.Y - r, r * 2, r * 2);
            }
            using (Pen p = new Pen(Color.FromArgb(210, Color.White), Math.Max(1, (int)Math.Ceiling(zoom))))
            {
                p.DashStyle = DashStyle.Dash;
                g.DrawEllipse(p, center.X - r, center.Y - r, r * 2, r * 2);
            }
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
            UpdateUndoRedoButtons(); // Başlangıçta tuşları güncelle
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

        private void CropToSelection()
        {
            if (MainBitmap == null || !isSelected || SelectionRectangle.IsEmpty)
            {
                MessageForm.Show("Create a rectangular selection first, then choose Crop.", "Crop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Rectangle r = new Rectangle(originalSelectionRectangleLocation, originalSelectionRectangleSize);
            r.Intersect(new Rectangle(Point.Empty, MainBitmap.Size));
            if (r.Width < 1 || r.Height < 1) return;
            SaveStateForUndo();
            var cropped = MainBitmap.Clone(r, PixelFormat.Format32bppArgb);
            MainBitmap.Dispose(); MainBitmap = cropped;
            SetAsUnselected(); RefreshCanvasAfterTransform();
        }

        private void RotateCanvas(RotateFlipType transform)
        {
            if (MainBitmap == null) return;
            SaveStateForUndo(); MainBitmap.RotateFlip(transform); RefreshCanvasAfterTransform();
        }

        private void ApplyGrayscaleQuick()
        {
            if (MainBitmap == null) return;
            SaveStateForUndo();
            var output = new Bitmap(MainBitmap.Width, MainBitmap.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(output)) using (var attrs = new ImageAttributes())
            {
                attrs.SetColorMatrix(new ColorMatrix(new float[][] {
                    new float[] {.299f,.299f,.299f,0,0}, new float[] {.587f,.587f,.587f,0,0},
                    new float[] {.114f,.114f,.114f,0,0}, new float[] {0,0,0,1,0}, new float[] {0,0,0,0,1}}));
                g.DrawImage(MainBitmap, new Rectangle(0, 0, output.Width, output.Height), 0, 0, MainBitmap.Width, MainBitmap.Height, GraphicsUnit.Pixel, attrs);
            }
            MainBitmap.Dispose(); MainBitmap = output; RefreshCanvasAfterTransform();
        }

        private void FitCanvasToWorkspace()
        {
            if (MainBitmap == null || UIPanel.ClientSize.Width < 1 || UIPanel.ClientSize.Height < 1) return;
            float zx = Math.Max(.05f, (UIPanel.ClientSize.Width - 60f) / MainBitmap.Width);
            float zy = Math.Max(.05f, (UIPanel.ClientSize.Height - 60f) / MainBitmap.Height);
            zoom = Math.Min(8f, Math.Min(zx, zy)); RefreshCanvasAfterTransform(false);
        }

        private void RefreshCanvasAfterTransform(bool markModified = true)
        {
            originalSize = MainBitmap.Size;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            canvasPanel.Size = new Size((int)Math.Round(MainBitmap.Width * zoom) + 20, (int)Math.Round(MainBitmap.Height * zoom) + 20);
            pictureBoxCanvas.Size = panelResizer.Size; UIPanel.AutoScrollMinSize = canvasPanel.Size;
            labelSize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px"; labelZoom.Text = $"{Math.Round(zoom * 100)}%";
            if (markModified) { isModified = true; isSaved = false; MarkLayerThumbnailDirty(); RefreshActiveLayerThumbnail(); }
            CenterCanvasPanel(); pictureBoxCanvas.Invalidate(); UpdateUndoRedoButtons();
        }

        private void SetInitialValues()
        {
            toolStripComboBoxAirBrushSize.SelectedItem = sprayToolSize;
            comboBoxBrushSize.SelectedItem = brushSize.ToString();
            comboBoxBrushType.SelectedIndex = Settings1.Default.DefaultBrushStyle;
            comboBoxPenSize.SelectedItem = penSize.ToString();
            comboBoxPenType.SelectedIndex = Settings1.Default.DefaultPenStyle;
            comboBoxEraserSize.SelectedItem = eraserSize.ToString();
            textBoxTolerance.Text = tolerance.ToString();
            textBoxTolerance2.Text = tolerance2.ToString();
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
            foreach (TabPage pages in tabControl1.TabPages)
            {
                pages.BackColor = TabPage.DefaultBackColor;
                pages.ForeColor = TabPage.DefaultForeColor;
            }
            listView1.ApplyListViewTheme(ListView.DefaultBackColor, ListView.DefaultForeColor);
            toolStrip1.BackgroundImage = Resources.toolstrip_light;
            TitleBarHelper.ApplyCustomTitleBar(this, false);
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
            foreach (TabPage pages in tabControl1.TabPages)
            {
                pages.BackColor = Color.FromArgb(32, 32, 32);
                pages.ForeColor = Color.White;
            }
            listView1.ApplyListViewTheme(Color.Black, Color.White);
            toolStrip1.BackgroundImage = Resources.toolstrip_dark;
            TitleBarHelper.ApplyCustomTitleBar(this, true);
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
            // Animate only the dash phase. 60 ms gives a smooth but lightweight motion.
            marchingAntsTimer.Interval = 60;
            marchingAntsTimer.Tick += MarchingAntsTimer_Tick;
            marchingAntsTimer.Start();
            this.MouseWheel += ImageEditor_MouseWheel;
            SystemThemeUtility.RegisterForm(this);
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
            InitializeLayerSystem();
            // Set the default selected font to the first one in the list
            if (fontsComboBox.Items.Count > 0)
            {
                fontsComboBox.SelectedIndex = 0;
            }
            toolStripSample.Font = new Font(fontFamilies[fontsComboBox.SelectedIndex], toolStripSample.Font.Size, toolStripSample.Font.Style);

            // --- BEGIN: focus & mouse-wheel forwarding fix (prevents Ctrl+Wheel zoom being swallowed when scrollbars present) ---
            // Make sure controls that can be hovered receive focus so MouseWheel events reach the form handler when Ctrl is pressed.
            // PictureBox and panels may handle mouse wheel for scrolling; forwarding ensures consistent zoom behavior.
            try
            {
                // Allow these controls to receive focus
                pictureBoxCanvas.TabStop = true;
                canvasPanel.TabStop = true;
                UIPanel.TabStop = true;

                // When mouse enters the canvas or panels, give focus to that control (so MouseWheel events bubble predictably).
                pictureBoxCanvas.MouseEnter += (s, ev) => this.ActiveControl = pictureBoxCanvas;
                canvasPanel.MouseEnter += (s, ev) => this.ActiveControl = canvasPanel;
                UIPanel.MouseEnter += (s, ev) => this.ActiveControl = UIPanel;

                // Forward MouseWheel from inner controls to the same handler (some controls consume wheel for scrolling).
                pictureBoxCanvas.MouseWheel += ImageEditor_MouseWheel;
                canvasPanel.MouseWheel += ImageEditor_MouseWheel;
                UIPanel.MouseWheel += ImageEditor_MouseWheel;
            }
            catch
            {
                // Defensive: if designer-generated controls are not yet initialized this will fail silently.
            }
            // --- END ---
        }

        private void MarchingAntsTimer_Tick(object sender, EventArgs e)
        {
            // Do no repaint work while there is no visible selection.
            if (!isSelected || (magicSelectionMask == null && SelectionRectangle.IsEmpty))
                return;

            // The white pen uses a {3,3} pattern. Advance the phase and wrap it
            // so the value stays small even if the editor remains open for hours.
            marchingAntsOffset += 0.75f;
            if (marchingAntsOffset >= 6f)
                marchingAntsOffset -= 6f;

            pictureBoxCanvas.Invalidate();
        }

        private void ImageEditor_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!ModifierKeys.HasFlag(Keys.Control) || e.Delta == 0)
                return;

            // Prevent a child control's Ctrl+wheel from also being processed
            // by its parent scroll container.
            if (e is HandledMouseEventArgs handledMouseEvent)
                handledMouseEvent.Handled = true;

            // Keep the original zoom/layout system intact, but make wheel zoom
            // finer. 120 is one standard wheel notch; precision touchpads can
            // provide smaller deltas and therefore get proportionally smaller
            // zoom changes.
            float wheelSteps = e.Delta / 120f;
            float requestedZoom = zoom + (0.05f * wheelSteps);

            zoom = Math.Max(0.05f, Math.Min(5f, requestedZoom));
            UpdatePictureBoxZoom();
            ScaleSelection();
        }

        private void ZoomIn()
        {
            zoom += 0.05f;
            if (zoom > 5f) zoom = 5f;
            UpdatePictureBoxZoom();
            ScaleSelection();
        }

        private void ZoomOut()
        {
            zoom -= 0.05f;
            if (zoom < 0.05f) zoom = 0.05f;
            UpdatePictureBoxZoom();
            ScaleSelection();
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
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
        private void InitializeLayerSystem()
        {
            // Compact layer list: one layer per row with a small thumbnail on the left
            // and the layer name on the right. imageListLayerThumbnails remains the
            // single thumbnail source for listView1.
            imageListLayerThumbnails.ImageSize = new Size(40, 40);
            imageListLayerThumbnails.ColorDepth = ColorDepth.Depth32Bit;

            listView1.LargeImageList = null;
            listView1.SmallImageList = imageListLayerThumbnails;
            listView1.View = View.Details;
            listView1.HeaderStyle = ColumnHeaderStyle.None;
            listView1.FullRowSelect = true;
            listView1.MultiSelect = false;
            listView1.HideSelection = false;

            listView1.Columns.Clear();
            listView1.Columns.Add("Layer");
            UpdateLayerListColumnWidth();

            listView1.Resize -= listView1_LayerListResize;
            listView1.Resize += listView1_LayerListResize;
            listView1.SelectedIndexChanged -= listView1_SelectedIndexChanged_Layers;
            listView1.SelectedIndexChanged += listView1_SelectedIndexChanged_Layers;

            // Refresh thumbnails after edits finish. MouseUp covers brush/pen/eraser/shape
            // workflows; other editing commands call MarkLayerThumbnailDirty and refresh
            // through the shared canvas refresh/undo/redo paths below.
            pictureBoxCanvas.MouseUp -= pictureBoxCanvas_MouseUp_LayerThumbnail;
            pictureBoxCanvas.MouseUp += pictureBoxCanvas_MouseUp_LayerThumbnail;

            layerThumbnailRefreshTimer.Stop();
            layerThumbnailRefreshTimer.Interval = 300;
            layerThumbnailRefreshTimer.Tick -= layerThumbnailRefreshTimer_Tick;
            layerThumbnailRefreshTimer.Tick += layerThumbnailRefreshTimer_Tick;
        }

        private void listView1_LayerListResize(object sender, EventArgs e)
        {
            UpdateLayerListColumnWidth();
        }

        private void UpdateLayerListColumnWidth()
        {
            if (listView1.Columns.Count == 0)
                return;

            // Keep the single details column stretched across the ListView so each
            // layer is visually a full-width row instead of a tiled icon entry.
            int width = Math.Max(40, listView1.ClientSize.Width - 4);
            listView1.Columns[0].Width = width;
        }


        private int FindLayerIndexByKey(string layerKey)
        {
            if (string.IsNullOrEmpty(layerKey))
                return -1;

            for (int i = 0; i < editorLayers.Count; i++)
            {
                if (editorLayers[i].ThumbnailKey == layerKey)
                    return i;
            }

            return -1;
        }

        private Bitmap GetLayerBitmapForRender(int index)
        {
            if (index < 0 || index >= editorLayers.Count)
                return null;

            // MainBitmap may contain edits that have not yet been copied back into
            // EditorLayer.Bitmap, so always use it for the active layer.
            if (index == activeLayerIndex && MainBitmap != null)
                return MainBitmap;

            return editorLayers[index].Bitmap;
        }

        private Bitmap CreateCompositeBitmap()
        {
            Size canvasSize;

            if (MainBitmap != null)
                canvasSize = MainBitmap.Size;
            else if (editorLayers.Count > 0 && editorLayers[0].Bitmap != null)
                canvasSize = editorLayers[0].Bitmap.Size;
            else
                canvasSize = Settings1.Default.DefaultCanvasSize;

            var composite = new Bitmap(
                Math.Max(1, canvasSize.Width),
                Math.Max(1, canvasSize.Height),
                PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(composite))
            {
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceOver;
                // Layers are normally canvas-sized and drawn unscaled. Expensive
                // interpolation/smoothing settings do not improve DrawImageUnscaled.
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.SmoothingMode = SmoothingMode.HighSpeed;

                // Photoshop-style layer order: row 0 is the topmost layer.
                // Draw from the bottom of the list toward the top.
                for (int i = editorLayers.Count - 1; i >= 0; i--)
                {
                    // Photoshop-style visibility: hiding a layer only removes it from
                    // the composite. The layer bitmap remains intact and editable.
                    if (!editorLayers[i].Visible)
                        continue;

                    Bitmap layerBitmap = GetLayerBitmapForRender(i);
                    if (layerBitmap == null)
                        continue;

                    g.DrawImageUnscaled(layerBitmap, 0, 0);
                }
            }

            return composite;
        }

        private void RenderCompositeToCanvas(bool force = false)
        {
            if (editorLayers.Count == 0)
            {
                pictureBoxCanvas.Image = MainBitmap;
                pictureBoxCanvas.Invalidate();
                compositeDirty = false;
                return;
            }

            if (!force && !compositeDirty)
            {
                pictureBoxCanvas.Invalidate();
                return;
            }

            Bitmap nextComposite = CreateCompositeBitmap();
            Bitmap oldComposite = compositeCanvasBitmap;

            compositeCanvasBitmap = nextComposite;
            pictureBoxCanvas.Image = compositeCanvasBitmap;
            compositeDirty = false;

            if (oldComposite != null &&
                !ReferenceEquals(oldComposite, MainBitmap) &&
                !ReferenceEquals(oldComposite, compositeCanvasBitmap))
            {
                oldComposite.Dispose();
            }

            pictureBoxCanvas.Invalidate();
        }

        private void MarkCompositeDirty()
        {
            compositeDirty = true;
        }

        private void BeginToolStrokePreview()
        {
            // Keep the canvas displaying the full composite while a tool is used.
            // Never swap pictureBoxCanvas.Image to MainBitmap: doing that temporarily
            // reveals only the active layer and makes hidden/other layers disappear.
            suppressCompositeDuringToolStroke = true;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
        }

        private void EndToolStrokePreview()
        {
            suppressCompositeDuringToolStroke = false;
            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
        }

        private void RefreshLayerThumbnail(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= editorLayers.Count)
                return;

            EditorLayer layer = editorLayers[layerIndex];
            Bitmap source = GetLayerBitmapForRender(layerIndex);

            if (source == null)
                return;

            // Persist only this layer before generating its thumbnail.
            if (layerIndex == activeLayerIndex && MainBitmap != null)
            {
                layer.Bitmap?.Dispose();
                layer.Bitmap = new Bitmap(MainBitmap);
                source = layer.Bitmap;
            }

            imageListLayerThumbnails.Images.RemoveByKey(layer.ThumbnailKey);

            using (Bitmap thumbnail = CreateLayerThumbnail(source))
                imageListLayerThumbnails.Images.Add(layer.ThumbnailKey, new Bitmap(thumbnail));

            if (layerIndex < listView1.Items.Count)
            {
                listView1.Items[layerIndex].ImageKey = layer.ThumbnailKey;
                listView1.Items[layerIndex].Text = layer.Name;
            }

            dirtyLayerThumbnailKeys.Remove(layer.ThumbnailKey);
            listView1.Invalidate();
        }

        private void RefreshLayerThumbnailByKey(string layerKey)
        {
            int index = FindLayerIndexByKey(layerKey);
            if (index >= 0)
                RefreshLayerThumbnail(index);
        }

        private void RefreshDirtyLayerThumbnails()
        {
            if (dirtyLayerThumbnailKeys.Count == 0)
                return;

            string[] changedKeys = dirtyLayerThumbnailKeys.ToArray();
            foreach (string key in changedKeys)
                RefreshLayerThumbnailByKey(key);
        }

        private void ResetLayers(string initialLayerName)
        {
            suppressLayerSelectionChanged = true;
            try
            {
                foreach (EditorLayer layer in editorLayers)
                    layer.Dispose();

                editorLayers.Clear();
                activeLayerIndex = -1;
                listView1.Items.Clear();
                imageListLayerThumbnails.Images.Clear();
                dirtyLayerThumbnailKeys.Clear();

                compositeCanvasBitmap?.Dispose();
                compositeCanvasBitmap = null;

                if (MainBitmap == null)
                    return;

                AddLayerInternal(initialLayerName, MainBitmap, selectLayer: true);
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
            }
            finally
            {
                suppressLayerSelectionChanged = false;
            }
        }

        private void AddLayerInternal(string layerName, Bitmap source, bool selectLayer)
        {
            if (source == null)
                return;

            var layer = new EditorLayer(layerName, new Bitmap(source));
            editorLayers.Add(layer);

            using (Bitmap thumbnail = CreateLayerThumbnail(source))
                imageListLayerThumbnails.Images.Add(layer.ThumbnailKey, new Bitmap(thumbnail));

            var item = new ListViewItem(layer.Name)
            {
                ImageKey = layer.ThumbnailKey,
                Tag = layer
            };
            listView1.Items.Add(item);

            if (selectLayer)
            {
                activeLayerIndex = editorLayers.Count - 1;
                item.Selected = true;
                item.Focused = true;
            }
        }

        // Add a transparent layer directly above the currently active layer,
        // matching Photoshop's stacking behavior.
        private void AddNewLayer()
        {
            if (MainBitmap == null)
                return;

            SyncActiveLayerFromCanvas();

            int nextNumber = editorLayers.Count + 1;
            var blank = new Bitmap(MainBitmap.Width, MainBitmap.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(blank))
                g.Clear(Color.Transparent);

            var layer = new EditorLayer($"Layer {nextNumber}", new Bitmap(blank));
            using (Bitmap thumbnail = CreateLayerThumbnail(blank))
                imageListLayerThumbnails.Images.Add(layer.ThumbnailKey, new Bitmap(thumbnail));

            var item = new ListViewItem(layer.Name)
            {
                ImageKey = layer.ThumbnailKey,
                Tag = layer
            };

            int insertIndex = activeLayerIndex >= 0 ? activeLayerIndex : 0;
            editorLayers.Insert(insertIndex, layer);
            listView1.Items.Insert(insertIndex, item);

            activeLayerIndex = insertIndex;

            MainBitmap?.Dispose();
            MainBitmap = new Bitmap(layer.Bitmap);
            originalSize = MainBitmap.Size;

            suppressLayerSelectionChanged = true;
            try
            {
                listView1.SelectedItems.Clear();
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
            finally
            {
                suppressLayerSelectionChanged = false;
            }

            blank.Dispose();
            SetAsUnselected();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);

            isModified = true;
            isSaved = false;
        }

        private void SyncActiveLayerFromCanvas()
        {
            if (MainBitmap == null || activeLayerIndex < 0 || activeLayerIndex >= editorLayers.Count)
                return;

            EditorLayer layer = editorLayers[activeLayerIndex];
            layer.Bitmap?.Dispose();
            layer.Bitmap = new Bitmap(MainBitmap);
            MarkCompositeDirty();
        }

        private void SwitchToLayer(int newIndex)
        {
            if (newIndex < 0 || newIndex >= editorLayers.Count || newIndex == activeLayerIndex)
                return;

            // Persist the previous active layer, but do not regenerate its thumbnail
            // just because the user selected another row.
            SyncActiveLayerFromCanvas();

            activeLayerIndex = newIndex;
            EditorLayer layer = editorLayers[activeLayerIndex];

            MainBitmap?.Dispose();
            MainBitmap = new Bitmap(layer.Bitmap);
            originalSize = MainBitmap.Size;

            canvasPanel.Size = new Size(
                (int)Math.Round(MainBitmap.Width * zoom) + 20,
                (int)Math.Round(MainBitmap.Height * zoom) + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            UIPanel.AutoScrollMinSize = canvasPanel.Size;
            labelSize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";

            SetAsUnselected();
            CenterCanvasPanel();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
        }

        private void listView1_SelectedIndexChanged_Layers(object sender, EventArgs e)
        {
            if (suppressLayerSelectionChanged || listView1.SelectedIndices.Count == 0)
                return;

            int selectedIndex = listView1.SelectedIndices[0];
            SwitchToLayer(selectedIndex);
        }

        private void listView1_LayerVisibilityToggled(object sender, LayerVisibilityToggledEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= editorLayers.Count)
                return;

            // Persist unsaved pixels first. Visibility itself never changes the bitmap.
            if (e.RowIndex == activeLayerIndex)
                SyncActiveLayerFromCanvas();

            EditorLayer layer = editorLayers[e.RowIndex];
            if (layer.Visible == e.Visible)
                return;

            layer.Visible = e.Visible;
            listView1.SetLayerVisibility(e.RowIndex, layer.Visible);

            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            listView1.InvalidateRow(e.RowIndex);

            isModified = true;
            isSaved = false;
        }

        private void pictureBoxCanvas_MouseUp_LayerThumbnail(object sender, MouseEventArgs e)
        {
            // The main MouseUp handler ends the fast stroke/composite path.
            // This subscriber only updates the thumbnail of the layer that changed.
            RefreshActiveLayerThumbnail();
        }

        private void MarkLayerThumbnailDirty()
        {
            if (activeLayerIndex < 0 || activeLayerIndex >= editorLayers.Count)
                return;

            // The active layer changed, so the flattened preview must be rebuilt too.
            // This keeps all currently visible layers on-screen while drawing.
            MarkCompositeDirty();
            dirtyLayerThumbnailKeys.Add(editorLayers[activeLayerIndex].ThumbnailKey);

            // Debounce rapid edits. The queue is keyed by layer, so switching layers
            // cannot accidentally refresh the wrong thumbnail.
            layerThumbnailRefreshTimer.Stop();
            layerThumbnailRefreshTimer.Start();
        }

        private void layerThumbnailRefreshTimer_Tick(object sender, EventArgs e)
        {
            layerThumbnailRefreshTimer.Stop();
            RefreshDirtyLayerThumbnails();
        }

        private void RefreshActiveLayerThumbnail(bool force = false)
        {
            if (activeLayerIndex < 0 || activeLayerIndex >= editorLayers.Count)
                return;

            string key = editorLayers[activeLayerIndex].ThumbnailKey;

            if (!force && !dirtyLayerThumbnailKeys.Contains(key))
                return;

            RefreshLayerThumbnail(activeLayerIndex);
        }

        private Bitmap CreateLayerThumbnail(Image source)
        {
            Size size = imageListLayerThumbnails.ImageSize;
            if (size.Width < 16 || size.Height < 16)
                size = new Size(40, 40);

            var thumbnail = new Bitmap(
                size.Width,
                size.Height,
                PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                g.Clear(Color.Transparent);

                // Photoshop-style transparency checkerboard.
                // This is drawn only into the thumbnail preview and never changes
                // the actual layer bitmap.
                const int checkerSize = 5;
                using (Brush lightBrush = new SolidBrush(Color.FromArgb(238, 238, 238)))
                using (Brush darkBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
                {
                    for (int y = 0; y < size.Height; y += checkerSize)
                    {
                        for (int x = 0; x < size.Width; x += checkerSize)
                        {
                            bool light = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                            g.FillRectangle(
                                light ? lightBrush : darkBrush,
                                x,
                                y,
                                Math.Min(checkerSize, size.Width - x),
                                Math.Min(checkerSize, size.Height - y));
                        }
                    }
                }

                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.SmoothingMode = SmoothingMode.HighSpeed;
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighSpeed;

                float scale = Math.Min(
                    (float)size.Width / source.Width,
                    (float)size.Height / source.Height);

                int width = Math.Max(1, (int)Math.Round(source.Width * scale));
                int height = Math.Max(1, (int)Math.Round(source.Height * scale));
                int left = (size.Width - width) / 2;
                int top = (size.Height - height) / 2;

                // Transparent portions of the layer reveal the checkerboard.
                g.DrawImage(
                    source,
                    new Rectangle(left, top, width, height),
                    0,
                    0,
                    source.Width,
                    source.Height,
                    GraphicsUnit.Pixel);
            }

            return thumbnail;
        }

        private void createNewFile()
        {
            listView1.Items.Clear();
            aiGenerated = false; // Reset AI generated flag
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            // panel1'in AutoScroll özelliğini true yaparak kaydırma çubuklarını etkinleştiriyoruz
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarını ayarlıyoruz
            canvasPanel.Size = new Size(Settings1.Default.DefaultCanvasSize.Width + 20,
                Settings1.Default.DefaultCanvasSize.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliğini canvasPanel'in boyutlarına ayarlıyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleştiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            canvasPanel.Refresh();
            MainBitmap = new Bitmap(Settings1.Default.DefaultCanvasSize.Width,
                Settings1.Default.DefaultCanvasSize.Height);
            originalSize = MainBitmap.Size;
            ResetLayers("Layer 1");
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = "Unnamed File";
            undoStack.Clear();
            redoStack.Clear();
            geriAlToolStripMenuItem.Enabled = false;
            yineleToolStripMenuItem.Enabled = false;
            isSaved = false;
            openedFilePath = string.Empty;
            farklıKaydetToolStripMenuItem.Enabled = false;
            zoom = 1;
            labelZoom.Text = "100%";
        }
        private void openAFile(string fileName)
        {
            aiGenerated = false; // Reset AI generated flag
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            Bitmap image = SecurityGuard.LoadLocalImage(fileName);
            // panel1'in AutoScroll özelliğini true yaparak kaydırma çubuklarını etkinleştiriyoruz
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarını ayarlıyoruz
            canvasPanel.Size = new Size(image.Size.Width + 20, image.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliğini canvasPanel'in boyutlarına ayarlıyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleştiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            MainBitmap = new Bitmap(image);
            originalSize = MainBitmap.Size;
            ResetLayers(Path.GetFileName(fileName));
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = fileName;
            undoStack.Clear();
            redoStack.Clear();
            geriAlToolStripMenuItem.Enabled = false;
            yineleToolStripMenuItem.Enabled = false;
            isSaved = true;
            openedFilePath = fileName;
            farklıKaydetToolStripMenuItem.Enabled = true;
            zoom = 1;
            labelZoom.Text = "100%";
        }
        private void createFileWithAIorWebcam(Image generatedImage, bool isAIGenerated)
        {
            aiGenerated = isAIGenerated;
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            Image image = generatedImage;
            // panel1'in AutoScroll özelliğini true yaparak kaydırma çubuklarını etkinleştiriyoruz
            UIPanel.AutoScroll = true;

            // canvasPanel'in boyutlarını ayarlıyoruz
            canvasPanel.Size = new Size(generatedImage.Width + 20, generatedImage.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;

            // panel1'in AutoScrollMinSize özelliğini canvasPanel'in boyutlarına ayarlıyoruz
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            // Paneli merkezi konumda yerleştiriyoruz
            CenterCanvasPanel();

            // Paneli hemen yenile (Refresh kullan)
            pictureBoxCanvas.Image = image;
            MainBitmap = new Bitmap(image);
            originalSize = MainBitmap.Size;
            ResetLayers("Layer 1");
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
            isSaved = false;
            openedFilePath = string.Empty;
            farklıKaydetToolStripMenuItem.Enabled = false;
            zoom = 1;
            labelZoom.Text = "100%";
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

        private void hakkındaToolStripMenuItem_Click(object sender, EventArgs e)
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
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                try
                {
                    if (isSelected && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                    {
                        SaveStateForUndo();
                        isImageCurrentlyCreating = true;
                        toolStripAICreateImage.Visible = false;
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
                            var image = await ImageGenerationCore.GenerateImage("Remove the object or person in context of the image.",
                                SelectedBitmap, SelectedBitmap.Width, SelectedBitmap.Height, cts.Token);

                            if (image != null)
                            {
                                SelectedBitmap = (Bitmap)image;
                                MergeMainBitmapWithSelected();
                                isModified = true; MarkLayerThumbnailDirty(); // Mark the image as modified
                            }
                            else
                            {
                                Undo();
                                ClearRedoStack();
                                Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                                MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    Undo();
                    ClearRedoStack();
                    Logger.Log("An error occurred: " + ex.Message, Logger.LogTypes.Error);
                    MessageForm.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            else
            {
                Logger.Log("No internet connection available.", Logger.LogTypes.Error);
                MessageForm.Show("Please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void removeBackgroundToolStripMenuItem2_Click(object sender, EventArgs e)
        {

            toolStripAICreateImage.Visible = false;
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
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
                        var imageTask = ImageGenerationCore.GenerateImage("Isolate the main object or person precisely and remove the entire background. Return the result as a PNG with a true transparent alpha channel (RGBA). All background pixels must have alpha 0. Do not replace the background with white, black, gray, any solid color, blur, studio backdrop, gradient, or checkerboard pattern. Preserve the subject’s original colors, edges, fine details, hair, and semi-transparent areas.",
                            SelectedBitmap, SelectedBitmap.Width, SelectedBitmap.Height, cts.Token);
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
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        var imageTask = ImageGenerationCore.GenerateImage("Isolate the object or person and make the background transparent.",
                            MainBitmap, MainBitmap.Width, MainBitmap.Height, cts.Token);
                        var image = await imageTask; // Await the task to get the result
                        if (image != null)
                        {
                            image = RemoveGeneratedCheckerboard(image);
                            MainBitmap = new Bitmap(image); // Convert Image to Bitmap  
                            RenderCompositeToCanvas();
                            MarkLayerThumbnailDirty();
                            RefreshActiveLayerThumbnail();
                            pictureBoxCanvas.Invalidate();
                        }
                        else
                        {
                            Undo();
                            ClearRedoStack();
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                }
                catch (NullReferenceException)
                {
                    Undo();
                    ClearRedoStack();
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    Undo();
                    ClearRedoStack();
                    Logger.Log("An error occurred: " + ex.Message, Logger.LogTypes.Error);
                    MessageForm.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            else
            {
                Logger.Log("No internet connection available.", Logger.LogTypes.Error);
                MessageForm.Show("Please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private Bitmap RemoveGeneratedCheckerboard(Image source)
        {
            Bitmap bitmap = new Bitmap(
                source.Width,
                source.Height,
                PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }

            int width = bitmap.Width;
            int height = bitmap.Height;

            // Find the most common neutral gray/white shades on the outer border.
            int[] histogram = new int[256];

            void Sample(Color c)
            {
                int max = Math.Max(c.R, Math.Max(c.G, c.B));
                int min = Math.Min(c.R, Math.Min(c.G, c.B));

                // Checkerboard is essentially grayscale.
                if (max - min <= 25)
                {
                    int gray = (c.R + c.G + c.B) / 3;

                    // Ignore very dark colors to reduce the chance of
                    // considering subject outlines/background shadows.
                    if (gray >= 110)
                        histogram[gray]++;
                }
            }

            for (int x = 0; x < width; x++)
            {
                Sample(bitmap.GetPixel(x, 0));
                Sample(bitmap.GetPixel(x, height - 1));
            }

            for (int y = 0; y < height; y++)
            {
                Sample(bitmap.GetPixel(0, y));
                Sample(bitmap.GetPixel(width - 1, y));
            }

            // Find the two strongest gray shades.
            int firstShade = -1;
            int secondShade = -1;
            int firstCount = -1;
            int secondCount = -1;

            for (int i = 110; i < 256; i++)
            {
                if (histogram[i] > firstCount)
                {
                    secondCount = firstCount;
                    secondShade = firstShade;

                    firstCount = histogram[i];
                    firstShade = i;
                }
                else if (histogram[i] > secondCount)
                {
                    secondCount = histogram[i];
                    secondShade = i;
                }
            }

            if (firstShade < 0)
                return bitmap;

            if (secondShade < 0)
                secondShade = firstShade;

            const int grayTolerance = 40;
            const int saturationTolerance = 35;

            bool IsCheckerPixel(Color c)
            {
                if (c.A == 0)
                    return true;

                int max = Math.Max(c.R, Math.Max(c.G, c.B));
                int min = Math.Min(c.R, Math.Min(c.G, c.B));

                // Must still be close to neutral gray.
                if (max - min > saturationTolerance)
                    return false;

                int gray = (c.R + c.G + c.B) / 3;

                bool shade1 =
                    Math.Abs(gray - firstShade) <= grayTolerance;

                bool shade2 =
                    Math.Abs(gray - secondShade) <= grayTolerance;

                // Also accept common near-white checkerboard cells.
                bool nearWhite =
                    c.R >= 205 &&
                    c.G >= 205 &&
                    c.B >= 205 &&
                    max - min <= saturationTolerance;

                return shade1 || shade2 || nearWhite;
            }

            bool[,] visited = new bool[width, height];
            Queue<Point> queue = new Queue<Point>();

            void AddIfBackground(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                    return;

                if (visited[x, y])
                    return;

                Color c = bitmap.GetPixel(x, y);

                if (!IsCheckerPixel(c))
                    return;

                visited[x, y] = true;
                queue.Enqueue(new Point(x, y));
            }

            // Start only from the outside of the image.
            // Therefore gray/white details INSIDE the subject aren't
            // automatically erased.
            for (int x = 0; x < width; x++)
            {
                AddIfBackground(x, 0);
                AddIfBackground(x, height - 1);
            }

            for (int y = 0; y < height; y++)
            {
                AddIfBackground(0, y);
                AddIfBackground(width - 1, y);
            }

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                AddIfBackground(p.X - 1, p.Y);
                AddIfBackground(p.X + 1, p.Y);
                AddIfBackground(p.X, p.Y - 1);
                AddIfBackground(p.X, p.Y + 1);
            }

            // Actually make the detected checkerboard transparent.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[x, y])
                        continue;

                    Color c = bitmap.GetPixel(x, y);

                    bitmap.SetPixel(
                        x,
                        y,
                        Color.FromArgb(0, c.R, c.G, c.B));
                }
            }

            return bitmap;
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
            save();
        }
        private void save()
        {
            if (!isSaved)
            {
                saveFileAs();
            }
            else
            {
                saveFile(openedFilePath);
            }
        }
        private async void saveFile(string targetFilePath)
        {
            SyncActiveLayerFromCanvas();
            RefreshActiveLayerThumbnail(force: true);
            fileSaved = false;
            string tempFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath), Path.GetRandomFileName());
            string backupFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath), Path.GetRandomFileName());

            labelSaving.Visible = true;
            progressBarSaving.Visible = true;
            toolStripSeparator13.Visible = true;
            progressBarSaving.Value = 0;

            try
            {
                ImageFormat format = ImageFormat.Png;
                string ext = Path.GetExtension(targetFilePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg")
                    format = ImageFormat.Jpeg;
                else if (ext == ".bmp")
                    format = ImageFormat.Bmp;

                await Task.Run(() =>
                {
                    try
                    {
                        using (Bitmap flattened = CreateCompositeBitmap())
                        {
                            flattened.Save(targetFilePath, format);
                        }
                        SaveMetaDataToImage(targetFilePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Save operation error: {ex.Message}", Logger.LogTypes.Error);
                        throw;
                    }
                });

                if (File.Exists(tempFilePath))
                {
                    File.Replace(tempFilePath, targetFilePath, backupFilePath, true);
                    isSaved = true;
                    fileSaved = true;
                    Logger.Log($"File saved successfully: {targetFilePath}", Logger.LogTypes.Info);
                }
            }
            catch (Exception ex)
            {
                fileSaved = false;
                Logger.Log($"An error occurred while saving the file: {ex.Message}", Logger.LogTypes.Error);
                MessageForm.Show($"An error occurred while saving the file: {ex.Message}",
                                "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                labelSaving.Visible = false;
                progressBarSaving.Visible = false;
                toolStripSeparator13.Visible = false;

                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                    if (File.Exists(backupFilePath))
                        File.Delete(backupFilePath);
                }
                catch { }
            }
        }

        private void SaveMetaDataToImage(string filePath)
        {
            try
            {
                var file = ImageFile.FromFile(filePath);
                string programName = "LibreCanvas " + GetInformations.GetVersionAndStatus() +
                    (aiGenerated == true ? " (AI Generated)" : string.Empty);
                file.Properties.Set(ExifTag.Software, programName);
                file.Properties.Set(ExifTag.Artist, Environment.UserName);
                file.Save(filePath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving metadata: {ex.Message}", Logger.LogTypes.Error);
            }
        }

        private void saveFileAs()
        {
            try
            {
                saveFileDialog1.Filter = "PNG Files|*.png|JPEG Files|*.jpg|Bitmap Files|*.bmp";
                DialogResult dialogResult = saveFileDialog1.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    saveFile(saveFileDialog1.FileName);
                    farklıKaydetToolStripMenuItem.Enabled = true;
                }
            }
            catch
            {
                fileSaved = false; // Ensure the flag is false if an exception occurs
            }
        }

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
            // Panelin yeni boyutlarını al
            int panelLeft = canvasPanel.Left;
            int panelTop = canvasPanel.Top;
            int panelWidth = canvasPanel.Width;
            int panelHeight = canvasPanel.Height;

            // Tutmaçların konumlarını panelin boyutlarına göre güncelle
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
            RenderCompositeToCanvas();
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

                // Minimum boyutları kontrol et
                newWidth = Math.Max(newWidth, 1);
                newHeight = Math.Max(newHeight, 1);

                // Bitmap'i yeniden boyutlandır
                Bitmap newBitmap = new Bitmap((int)Math.Round((newWidth - 20) / zoom), (int)Math.Round((newHeight - 20) / zoom));
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.DrawImage(MainBitmap, 0, 0);
                }
                MainBitmap = newBitmap;

                // Yeni boyutları uygula
                canvasPanel.Size = new Size(newWidth, newHeight);

                // panel1'in AutoScrollMinSize özelliğini güncelle
                UIPanel.AutoScrollMinSize = canvasPanel.Size;

                // Paneli merkezi konumda yerleştiriyoruz
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

            // Resizing işlemi başladığında
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
                toolStripMagicSelection.Visible = true;
            }
            else
            {
                toolStripMagicSelection.Visible = false;
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
                    if (suppressCompositeDuringToolStroke)
                        EndToolStrokePreview();

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
                        // Convert end point to image (bitmap) coordinates using current zoom
                        Point endPointImage = new Point((int)(e.X / zoom), (int)(e.Y / zoom));

                        switch (selectedTool)
                        {
                            case Tools.Line:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawLineOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Round:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRoundOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Rectangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRectangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.RoundedRectangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRoundedRectangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, endPointImage, radius);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Triangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawTriangleOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Hexagon:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawHexagonOnCanvas(MainBitmap, pictureBoxCanvas, color1, shapeThickness, points, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
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
                            // Seçim koordinatlarını orijinal bitmap koordinat sistemine dönüştür
                            Rectangle targetRect = new Rectangle(
                                (int)(originalSelectionRectangleLocation.X),
                                (int)(originalSelectionRectangleLocation.Y),
                                (int)(originalSelectionRectangleSize.Width),
                                (int)(originalSelectionRectangleSize.Height));

                            // Hedef dikdörtgenin ana bitmap sınırları içinde kalmasını sağla
                            targetRect = EnsureRectWithinBounds(targetRect, MainBitmap.Size);

                            // Kaynak dikdörtgenin de SelectedBitmap sınırları içinde kalmasını sağla
                            Rectangle sourceRect = new Rectangle(0, 0,
                                Math.Min(SelectedBitmap.Width, targetRect.Width),
                                Math.Min(SelectedBitmap.Height, targetRect.Height));

                            // Yüksek kalite ayarlarını kullan
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // Dönüştürülmüş alanı ana resme çiz
                            g.DrawImage(SelectedBitmap, targetRect, sourceRect, GraphicsUnit.Pixel);
                        }

                        // Clear the SelectedBitmap
                        SelectedBitmap.Dispose();
                        SelectedBitmap = null;
                    }

                    // Update the PictureBox with the new bitmap
                    RenderCompositeToCanvas();
                    pictureBoxCanvas.Invalidate();
                }
            }
        }

        // Yardımcı metot: Dikdörtgenin belirtilen sınırlar içinde kalmasını sağlar
        private Rectangle EnsureRectWithinBounds(Rectangle rect, Size bounds)
        {
            Rectangle result = new Rectangle(rect.Location, rect.Size);

            // X koordinatını kontrol et
            if (result.X < 0)
                result.X = 0;
            if (result.X + result.Width > bounds.Width)
                result.Width = Math.Max(0, bounds.Width - result.X);

            // Y koordinatını kontrol et
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
                // Track mouse position for the brush tip preview
                currentMousePosition = e.Location;
                isMouseOverCanvas = true;
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

                                // Koordinatları zoom'a göre ölçekle
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
                // If not currently drawing, redraw to update the brush tip cursor
                if (!isdrawing)
                {
                    pictureBoxCanvas.Invalidate();
                }
            }
        }
        private void DrawMarchingAntsRectangle(Graphics graphics, Rectangle rect)
        {
            GraphicsState state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;

                var (blackPen, whitePen) = CreateMarchingAntsPens();
                using (blackPen)
                using (whitePen)
                {
                    float inset = Math.Max(1f, Math.Max(blackPen.Width, whitePen.Width) / 2f);
                    RectangleF visibleRect = rect;

                    if (visibleRect.Left <= 0f)
                    {
                        float oldRight = visibleRect.Right;
                        visibleRect.X = inset;
                        visibleRect.Width = Math.Max(0f, oldRight - visibleRect.X);
                    }
                    if (visibleRect.Top <= 0f)
                    {
                        float oldBottom = visibleRect.Bottom;
                        visibleRect.Y = inset;
                        visibleRect.Height = Math.Max(0f, oldBottom - visibleRect.Y);
                    }
                    if (visibleRect.Right >= pictureBoxCanvas.ClientSize.Width)
                        visibleRect.Width = Math.Max(0f, pictureBoxCanvas.ClientSize.Width - inset - visibleRect.X);
                    if (visibleRect.Bottom >= pictureBoxCanvas.ClientSize.Height)
                        visibleRect.Height = Math.Max(0f, pictureBoxCanvas.ClientSize.Height - inset - visibleRect.Y);

                    if (visibleRect.Width > 0f && visibleRect.Height > 0f)
                    {
                        graphics.DrawRectangle(blackPen, visibleRect.X, visibleRect.Y, visibleRect.Width, visibleRect.Height);
                        graphics.DrawRectangle(whitePen, visibleRect.X, visibleRect.Y, visibleRect.Width, visibleRect.Height);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
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
                    SelectedBitmap = new Bitmap(
                        (int)(SelectionRectangle.Width / zoom),
                        (int)(SelectionRectangle.Height / zoom));

                    using (Graphics g = Graphics.FromImage(SelectedBitmap))
                    {
                        g.Clear(Color.Transparent);
                        Rectangle sourceRect = new Rectangle(
                            originalSelectionRectangleLocation.X,
                            originalSelectionRectangleLocation.Y,
                            originalSelectionRectangleSize.Width,
                            originalSelectionRectangleSize.Height);
                        g.DrawImage(MainBitmap,
                            new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height),
                            sourceRect,
                            GraphicsUnit.Pixel);
                    }
                }
            }
            if (isSelected && SelectedBitmap != null)
            {
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
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
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
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Eraser:
                                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                DrawBrush(graphics, BrushShapes.DrawCircleBrush, Color.FromArgb(0, 0, 0, 0), eraserSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Spray:
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                isModified = true; MarkLayerThumbnailDirty();
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
            else if (magicSelectionMask != null && isSelected && SelectedBitmap == null)
            {
                // Draw into MainBitmap but clipped to the magic selection mask.
                using (Graphics g = Graphics.FromImage(MainBitmap))
                {
                    GraphicsState gs = g.Save();
                    try
                    {
                        // Build clip region from mask runs (in image coordinates)
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            int width = magicSelectionMaskWidth;
                            int height = magicSelectionMaskHeight;
                            for (int yy = 0; yy < height; yy++)
                            {
                                int runStart = -1;
                                for (int xx = 0; xx < width; xx++)
                                {
                                    if (magicSelectionMask[yy * width + xx])
                                    {
                                        if (runStart < 0) runStart = xx;
                                    }
                                    else if (runStart >= 0)
                                    {
                                        // The magic mask uses absolute MainBitmap coordinates. Do not add the bounds offset again.
                                        path.AddRectangle(new Rectangle(runStart, yy, xx - runStart, 1));
                                        runStart = -1;
                                    }
                                }
                                if (runStart >= 0)
                                {
                                    // The magic mask uses absolute MainBitmap coordinates. Do not add the bounds offset again.
                                    path.AddRectangle(new Rectangle(runStart, yy, width - runStart, 1));
                                }
                            }

                            g.SetClip(new Region(path), CombineMode.Replace);

                            // Perform the same drawing logic but onto MainBitmap with image coordinates
                            switch (selectedTool)
                            {
                                case Tools.Brush:
                                    switch (comboBoxBrushType.SelectedIndex)
                                    {
                                        case 0:
                                            DrawBrush(g, BrushShapes.DrawCircleBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 1:
                                            DrawBrush(g, BrushShapes.DrawOilBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 2:
                                            DrawBrush(g, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 3:
                                            DrawBrush(g, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                    }
                                    break;
                                case Tools.Pen:
                                    switch (comboBoxPenType.SelectedIndex)
                                    {
                                        case 0:
                                            DrawBrush(g, BrushShapes.DrawSquareBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 1:
                                            DrawBrush(g, BrushShapes.DrawMarkerBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 2:
                                            DrawBrush(g, BrushShapes.DrawCrayonBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 3:
                                            DrawBrush(g, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                    }
                                    break;
                                case Tools.Eraser:
                                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                    DrawBrush(g, BrushShapes.DrawCircleBrush, Color.FromArgb(0, 0, 0, 0), eraserSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                    isModified = true; MarkLayerThumbnailDirty();
                                    break;
                                case Tools.Spray:
                                    DrawBrush(g, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                    isModified = true; MarkLayerThumbnailDirty();
                                    break;
                                case Tools.ColorDrop:
                                    Color pixelColor = MainBitmap.GetPixel((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom));
                                    color1 = pixelColor;
                                    foregroundColorButton.BackColor = pixelColor;
                                    break;
                            }
                        }
                        x = (int)Math.Round(e.X / zoom);
                        y = (int)Math.Round(e.Y / zoom);
                    }
                    finally
                    {
                        g.Restore(gs);
                        g.ResetClip();
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
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, color1, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
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
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, color1, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Tools.Eraser:
                                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                DrawBrush(graphics, BrushShapes.DrawCircleBrush, Color.FromArgb(0, 0, 0, 0), eraserSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Spray:
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, color1, sprayToolSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                isModified = true; MarkLayerThumbnailDirty();
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

            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
        }
        private (Pen blackPen, Pen whitePen) CreateMarchingAntsPens()
        {
            // Thicker black base + slightly thinner white dashes.
            // Keeping the white stroke narrower leaves a visible black edge and
            // makes the marching ants readable on both light and dark pixels.
            Pen blackPen = new Pen(Color.Black, 3f)
            {
                DashStyle = DashStyle.Solid,
                DashCap = DashCap.Flat,
                Alignment = PenAlignment.Center
            };

            Pen whitePen = new Pen(Color.White, 2f)
            {
                DashStyle = DashStyle.Custom,
                DashPattern = new float[] { 3f, 3f },
                DashOffset = marchingAntsOffset,
                DashCap = DashCap.Flat,
                Alignment = PenAlignment.Center
            };

            return (blackPen, whitePen);
        }

        private PointF KeepAntPointVisibleAtCanvasEdge(PointF point, float displayWidth, float displayHeight, float strokeWidth)
        {
            // A centered GDI+ pen drawn exactly at 0 or at width/height has half
            // of its stroke clipped by the PictureBox. Only move points that are
            // on the OUTER canvas boundary; interior magic-selection edges stay
            // at their exact coordinates.
            float inset = Math.Max(1f, strokeWidth / 2f);

            if (point.X <= 0f)
                point.X = inset;
            else if (point.X >= displayWidth)
                point.X = Math.Max(inset, displayWidth - inset);

            if (point.Y <= 0f)
                point.Y = inset;
            else if (point.Y >= displayHeight)
                point.Y = Math.Max(inset, displayHeight - inset);

            return point;
        }
        private void pictureBoxCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxCanvas.Image != null)
            {
                // Draw the SelectedBitmap if it exists
                if (SelectedBitmap != null && !SelectionRectangle.IsEmpty && magicSelectionMask == null)
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

                // Draw the actual Magic Wand contour instead of its rectangular bounds.
                if (magicSelectionMask != null)
                {
                    DrawMagicSelectionOutline(e.Graphics);
                }
                else if (!SelectionRectangle.IsEmpty)
                {
                    DrawMarchingAntsRectangle(e.Graphics, SelectionRectangle);
                }

                // Draw brush/pen/eraser/spray cursor preview when not drawing
                if (!isdrawing && MainBitmap != null && isMouseOverCanvas)
                {
                    bool shouldShow = selectedTool == Tools.Brush || selectedTool == Tools.Pen || selectedTool == Tools.Eraser || selectedTool == Tools.Spray;
                    if (shouldShow)
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        int sizePixels = brushSize;
                        int styleIndex = 0;
                        switch (selectedTool)
                        {
                            case Tools.Brush:
                                sizePixels = brushSize;
                                styleIndex = comboBoxBrushType.SelectedIndex;
                                break;
                            case Tools.Pen:
                                sizePixels = penSize;
                                styleIndex = comboBoxPenType.SelectedIndex;
                                break;
                            case Tools.Eraser:
                                sizePixels = eraserSize;
                                styleIndex = 0;
                                break;
                            case Tools.Spray:
                                sizePixels = sprayToolSize;
                                styleIndex = 0;
                                break;
                        }

                        int displaySize = Math.Max(1, (int)Math.Round(sizePixels * zoom));
                        Point center = currentMousePosition;

                        // Use specialized preview renderers to better emulate brush/pen/spray appearance
                        if (selectedTool == Tools.Brush)
                        {
                            DrawBrushPreview(e.Graphics, center, displaySize, styleIndex, color1, zoom);
                        }
                        else if (selectedTool == Tools.Pen)
                        {
                            DrawPenPreview(e.Graphics, center, displaySize, styleIndex, color1, zoom);
                        }
                        else if (selectedTool == Tools.Eraser)
                        {
                            DrawEraserPreview(e.Graphics, center, displaySize, zoom);
                        }
                        else if (selectedTool == Tools.Spray)
                        {
                            DrawSprayPreview(e.Graphics, center, displaySize, color1, zoom);
                        }
                    }
                }
            }
        }

        private void DrawBrush(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point location)
        {
            // Note: callers pass coordinates in image (bitmap) pixel space already
            // Do not apply any additional zoom scaling here to avoid double-scaling
            if (x == -1 && y == -1)
            {
                // Set the initial position without drawing
                x = location.X;
                y = location.Y;
                drawAction(graphics, color, size, location);
            }
            else
            {
                // Draw and fill the gap between the previous and current positions
                FillGap(graphics, drawAction, color, size, new Point(x, y), location);
            }
        }

        private void FillGap(Graphics graphics, Action<Graphics, Color, int, Point> drawAction, Color color, int size, Point start, Point end)
        {

            // start and end are expected to be in image (bitmap) pixel coordinates already.
            // Do not apply zoom scaling here.

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
        private Stack<LayerCanvasState> undoStack = new Stack<LayerCanvasState>();
        private Stack<LayerCanvasState> redoStack = new Stack<LayerCanvasState>();

        private bool EnsureVisibleLayerForTool()
        {
            if (editorLayers.Count == 0)
                return MainBitmap != null;

            if (activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count &&
                editorLayers[activeLayerIndex].Visible)
                return true;

            // A hidden layer must never become the target of a canvas tool. Pick the
            // topmost visible layer, keep the hidden layer hidden, and make the actual
            // edit target explicit in the Layers grid.
            int visibleIndex = -1;
            for (int i = 0; i < editorLayers.Count; i++)
            {
                if (editorLayers[i].Visible)
                {
                    visibleIndex = i;
                    break;
                }
            }

            if (visibleIndex < 0)
                return false;

            SwitchToLayer(visibleIndex);

            suppressLayerSelectionChanged = true;
            try
            {
                listView1.SelectedItems.Clear();
                if (visibleIndex < listView1.Items.Count)
                {
                    ListViewItem item = listView1.Items[visibleIndex];
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                }
            }
            finally
            {
                suppressLayerSelectionChanged = false;
            }

            return true;
        }

        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isImageCurrentlyCreating)
            {
                // Photoshop-style targeting: tools operate on a visible layer. If the
                // selected layer is hidden, move editing to the topmost visible layer
                // without turning the hidden layer back on. If every layer is hidden,
                // ignore the tool action instead of revealing or modifying anything.
                if (!EnsureVisibleLayerForTool())
                {
                    isdrawing = false;
                    RenderCompositeToCanvas(force: true);
                    return;
                }

                isdrawing = true;
                x = -1;
                y = -1;
                CancelFilters();

                if (selectedTool == Tools.Brush ||
                    selectedTool == Tools.Pen ||
                    selectedTool == Tools.Eraser ||
                    selectedTool == Tools.Spray ||
                    selectedTool == Tools.Line ||
                    selectedTool == Tools.Round ||
                    selectedTool == Tools.Rectangle ||
                    selectedTool == Tools.RoundedRectangle ||
                    selectedTool == Tools.Triangle ||
                    selectedTool == Tools.Hexagon)
                {
                    BeginToolStrokePreview();
                }

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

                    case Tools.MagicSelection:
                        // Magic Wand is a click operation, not a drag/draw operation.
                        // Select only the contiguous area whose colour is within the current tolerance.
                        Point magicPoint = new Point((int)(e.X / zoom), (int)(e.Y / zoom));
                        CreateMagicSelection(magicPoint, tolerance2);
                        isdrawing = false; // Prevent MouseUp from treating the selection as a paint stroke.
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
            isMouseOverCanvas = false;
            pictureBoxCanvas.Invalidate();
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
                            // MouseEventArgs'den tıklama konumunu alın  
                            // MouseEventArgs kullanarak tıklama konumunu alın
                            MouseEventArgs me = (MouseEventArgs)e;
                            int clickedX = me.X;
                            int clickedY = me.Y;
                            // Yeni bir TextBox oluşturun  
                            TextBox textBox = new TextBox
                            {
                                MaximumSize = Size.Empty, // Maksimum boyut  
                                AutoSize = false, // Otomatik boyutlandırmayı devre dışı bırakın  
                                Multiline = true, // Çok satırlı metin desteği  
                                WordWrap = true, // Metni sarmayı etkinleştirin  
                                Font = new Font(fontsComboBox.Text, textSize, fontStyle), // Yazı tipi ayarı  
                                BorderStyle = BorderStyle.FixedSingle // Kenarlık stili 
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

                            // TextBox'ı pictureBoxCanvas'a ekleyin  
                            pictureBoxCanvas.Controls.Add(textBox);

                            // TextBox'ı odaklayın  
                            textBox.Focus();

                            // TextBox'ın metni değiştikçe boyutunu ayarlayın  
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
                                    isModified = true; MarkLayerThumbnailDirty();
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
                                RenderCompositeToCanvas();
                                pictureBoxCanvas.Invalidate(); // Force a redraw  
                                isModified = true; MarkLayerThumbnailDirty();
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
                            MouseEventArgs me = (MouseEventArgs)e; // EventArgs yerine MouseEventArgs kullanımı  
                                                                   // Fix for CS0246: 'Location' türü veya ad alanı adı bulunamadı  
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
                            FloodFill(MainBitmap, adjustedLocation, color1, tolerance);
                            isModified = true; MarkLayerThumbnailDirty();
                        }
                        break;
                }
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
        private void DrawMagicSelectionOutline(Graphics graphics)
        {
            if (magicSelectionMask == null || magicSelectionOutlineSegments.Count == 0 || magicSelectionBounds.IsEmpty)
                return;

            GraphicsState state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;

                var (blackPen, whitePen) = CreateMarchingAntsPens();
                using (blackPen)
                using (whitePen)
                {
                    // Outline segments are already stored in absolute MainBitmap coordinates.
                    // Adding magicSelectionBounds.Left/Top here would offset the contour a second time.
                    float displayWidth = magicSelectionMaskWidth * zoom;
                    float displayHeight = magicSelectionMaskHeight * zoom;
                    float outerStrokeWidth = Math.Max(blackPen.Width, whitePen.Width);

                    foreach (var segment in magicSelectionOutlineSegments)
                    {
                        PointF a = new PointF(segment.Start.X * zoom, segment.Start.Y * zoom);
                        PointF b = new PointF(segment.End.X * zoom, segment.End.Y * zoom);

                        if (float.IsNaN(a.X) || float.IsNaN(a.Y) || float.IsNaN(b.X) || float.IsNaN(b.Y) ||
                            float.IsInfinity(a.X) || float.IsInfinity(a.Y) || float.IsInfinity(b.X) || float.IsInfinity(b.Y))
                            continue;

                        // Keep canvas-border ants fully visible instead of letting
                        // half of the centered pen get clipped outside the control.
                        a = KeepAntPointVisibleAtCanvasEdge(a, displayWidth, displayHeight, outerStrokeWidth);
                        b = KeepAntPointVisibleAtCanvasEdge(b, displayWidth, displayHeight, outerStrokeWidth);

                        graphics.DrawLine(blackPen, a, b);
                        graphics.DrawLine(whitePen, a, b);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }
        private void BuildMagicSelectionOutlineSegments()
        {
            magicSelectionOutlineSegments.Clear();
            if (magicSelectionMask == null || magicSelectionMaskWidth <= 0 || magicSelectionMaskHeight <= 0)
                return;

            int width = magicSelectionMaskWidth;
            int height = magicSelectionMaskHeight;

            bool SelectedAt(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                    return false;
                return magicSelectionMask[y * width + x];
            }

            for (int y = 0; y <= height; y++)
            {
                int runStart = -1;
                for (int x = 0; x < width; x++)
                {
                    bool edge = SelectedAt(x, y - 1) != SelectedAt(x, y);
                    if (edge && runStart < 0)
                        runStart = x;
                    else if (!edge && runStart >= 0)
                    {
                        magicSelectionOutlineSegments.Add((new Point(runStart, y), new Point(x, y)));
                        runStart = -1;
                    }
                }
                if (runStart >= 0)
                    magicSelectionOutlineSegments.Add((new Point(runStart, y), new Point(width, y)));
            }

            for (int x = 0; x <= width; x++)
            {
                int runStart = -1;
                for (int y = 0; y < height; y++)
                {
                    bool edge = SelectedAt(x - 1, y) != SelectedAt(x, y);
                    if (edge && runStart < 0)
                        runStart = y;
                    else if (!edge && runStart >= 0)
                    {
                        magicSelectionOutlineSegments.Add((new Point(x, runStart), new Point(x, y)));
                        runStart = -1;
                    }
                }
                if (runStart >= 0)
                    magicSelectionOutlineSegments.Add((new Point(x, runStart), new Point(x, height)));
            }
        }

        private void CreateMagicSelection(Point seed, int selectionTolerance)
        {
            if (MainBitmap == null || seed.X < 0 || seed.Y < 0 ||
                seed.X >= MainBitmap.Width || seed.Y >= MainBitmap.Height)
                return;

            if (isSelected)
                SetAsUnselected();

            Color target = MainBitmap.GetPixel(seed.X, seed.Y);
            int width = MainBitmap.Width;
            int height = MainBitmap.Height;
            BitArray selected = new BitArray(width * height);
            Stack<Point> seeds = new Stack<Point>();
            seeds.Push(seed);

            int minX = width, minY = height, maxX = -1, maxY = -1;
            int selectedCount = 0;

            // Magic Wand tolerance is explicitly 0..100.
            // 0 = exact colour only; 100 = every colour matches.
            selectionTolerance = Math.Clamp(selectionTolerance, 0, 100);

            bool Matches(int px, int py)
            {
                if (selectionTolerance >= 100)
                    return true;

                return IsColorWithinTolerance(MainBitmap.GetPixel(px, py), target, selectionTolerance);
            }

            while (seeds.Count > 0)
            {
                Point seedPoint = seeds.Pop();
                int sy = seedPoint.Y;
                int sx = seedPoint.X;
                int seedIndex = sy * width + sx;
                if (selected[seedIndex] || !Matches(sx, sy))
                    continue;

                int left = sx;
                while (left > 0)
                {
                    int nx = left - 1;
                    if (selected[sy * width + nx] || !Matches(nx, sy))
                        break;
                    left = nx;
                }

                int right = sx;
                while (right + 1 < width)
                {
                    int nx = right + 1;
                    if (selected[sy * width + nx] || !Matches(nx, sy))
                        break;
                    right = nx;
                }

                bool spanAbove = false;
                bool spanBelow = false;
                for (int x = left; x <= right; x++)
                {
                    int index = sy * width + x;
                    if (!selected[index])
                    {
                        selected[index] = true;
                        selectedCount++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (sy < minY) minY = sy;
                        if (sy > maxY) maxY = sy;
                    }

                    if (sy > 0)
                    {
                        int aboveIndex = (sy - 1) * width + x;
                        bool aboveMatches = !selected[aboveIndex] && Matches(x, sy - 1);
                        if (aboveMatches && !spanAbove)
                        {
                            seeds.Push(new Point(x, sy - 1));
                            spanAbove = true;
                        }
                        else if (!aboveMatches)
                        {
                            spanAbove = false;
                        }
                    }

                    if (sy + 1 < height)
                    {
                        int belowIndex = (sy + 1) * width + x;
                        bool belowMatches = !selected[belowIndex] && Matches(x, sy + 1);
                        if (belowMatches && !spanBelow)
                        {
                            seeds.Push(new Point(x, sy + 1));
                            spanBelow = true;
                        }
                        else if (!belowMatches)
                        {
                            spanBelow = false;
                        }
                    }
                }
            }

            if (selectedCount == 0)
                return;

            magicSelectionMask = selected;
            magicSelectionMaskWidth = width;
            magicSelectionMaskHeight = height;
            magicSelectionBounds = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
            BuildMagicSelectionOutlineSegments();

            SelectedBitmap?.Dispose();
            SelectedBitmap = null;
            SelectionRectangle = Rectangle.Empty;
            originalSelectionRectangleLocation = Point.Empty;
            originalSelectionRectangleSize = Size.Empty;
            isSelected = true;

            pictureBoxCanvas.Invalidate();
        }

        private void FloodFill(Bitmap bmp, Point pt, Color replacementColor, int tolerance)
        {
            // Validate input
            if (bmp == null || pt.X < 0 || pt.X >= bmp.Width || pt.Y < 0 || pt.Y >= bmp.Height)
            {
                return;
            }

            // Get the target color from the starting point
            Color targetColor = bmp.GetPixel(pt.X, pt.Y);

            // If the target color is the same as the replacement color, or the colors are too similar, do nothing
            if (IsColorWithinTolerance(targetColor, replacementColor, tolerance))
            {
                return;
            }

            // Use a Queue instead of a Stack to process pixels in a breadth-first manner
            Queue<Point> pixels = new Queue<Point>();
            pixels.Enqueue(pt);

            // Determine the bounds for the flood fill operation
            Rectangle bounds;
            if (isSelected && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
            {
                bounds = new Rectangle(
                    (int)(originalSelectionRectangleLocation.X),
                    (int)(originalSelectionRectangleLocation.Y),
                    (int)(originalSelectionRectangleSize.Width),
                    (int)(originalSelectionRectangleSize.Height));
            }
            else
            {
                bounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
            }

            // Use a HashSet to keep track of visited pixels
            HashSet<Point> visited = new HashSet<Point>();

            while (pixels.Count > 0)
            {
                Point a = pixels.Dequeue();

                // Check if the pixel is within the bounds and has not been visited
                if (a.X >= bounds.Left && a.X < bounds.Right &&
                    a.Y >= bounds.Top && a.Y < bounds.Bottom &&
                    !visited.Contains(a))
                {
                    Color pixelColor = bmp.GetPixel(a.X, a.Y);

                    // Apply tolerance check
                    if (IsColorWithinTolerance(pixelColor, targetColor, tolerance))
                    {
                        bmp.SetPixel(a.X, a.Y, replacementColor);
                        visited.Add(a); // Mark the pixel as visited

                        // Enqueue adjacent pixels
                        pixels.Enqueue(new Point(a.X - 1, a.Y));
                        pixels.Enqueue(new Point(a.X + 1, a.Y));
                        pixels.Enqueue(new Point(a.X, a.Y - 1));
                        pixels.Enqueue(new Point(a.X, a.Y + 1));
                    }
                }
            }

            pictureBoxCanvas.Invalidate(); // Refresh the main picture box
            return;
        }

        private bool IsColorWithinTolerance(Color color1, Color color2, int tolerance)
        {
            // Calculate the color difference
            int aDiff = Math.Abs(color1.A - color2.A);
            int rDiff = Math.Abs(color1.R - color2.R);
            int gDiff = Math.Abs(color1.G - color2.G);
            int bDiff = Math.Abs(color1.B - color2.B);

            // Calculate the overall difference
            double difference = Math.Sqrt(aDiff * aDiff + rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

            // The maximum difference between two colors
            double maxDifference = Math.Sqrt(50000);

            // Normalize the difference to a range between 0 and 1
            double normalizedDifference = difference / maxDifference;

            // Check if the normalized difference is within the tolerance
            return normalizedDifference <= (tolerance / 100.0);
        }

        private void çıkışToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ImageEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            marchingAntsTimer.Stop();
            marchingAntsTimer.Tick -= MarchingAntsTimer_Tick;
            marchingAntsTimer.Dispose();
            Application.Exit();
        }

        private void açToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AskForSavingOrCancel(new Action(() => openAFile()));
        }
        private void openAFile()
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
                Logger.Log("File not found: " + openFileDialog1.FileName, Logger.LogTypes.Error);
                MessageForm.Show("The selected file was not found. Please check the file path.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Logger.Log("Error opening file: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("An error occurred while opening the file: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void yeniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AskForSavingOrCancel(new Action(() => createNewFile()));
        }
        private void AskForSavingOrCancel(Action action)
        {
            if (isModified)
            {
                DialogResult result = MessageForm.Show("Do you want to save changes to your image?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                switch (result)
                {
                    case DialogResult.Yes:
                        save();
                        if (isSaved)
                        {
                            action();
                        }
                        break;
                    case DialogResult.No:
                        action();
                        break;
                    default:
                        return;
                }
            }
            else
            {
                action();
            }
        }
        int retryCount = 0;
        private void AskForSavingWithRetry(Action action)
        {
            if (isModified)
            {
                DialogResult result = MessageForm.Show("Do you want to save changes to your image?", "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                switch (result)
                {
                    case DialogResult.Yes:
                        save();
                        if (isSaved)
                        {
                            retryCount = 0; // Reset the retry count
                            action();
                        }
                        else
                        {
                            if (retryCount == 3)
                            {
                                Logger.Log("The action will be executed without saving after 3 attempts to save.", Logger.LogTypes.Warning);
                                retryCount = 0; // Reset the retry count
                                action();
                            }
                            else
                            {
                                retryCount++;
                                AskForSavingOrCancel(action);
                            }
                        }
                        break;
                    case DialogResult.No:
                        action();
                        break;
                }
            }
            else
            {
                action();
            }
        }

        private void createWithAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateWithAIForm createWithAIForm = new CreateWithAIForm();
            createWithAIForm.ShowDialog();
            var generatedImage = createWithAIForm.GetGeneratedImage();
            if (generatedImage != null)
            {
                AskForSavingWithRetry(new Action(() => createFileWithAIorWebcam(generatedImage, true)));
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
            AskForSavingWithRetry(new Action(() => createFileWithAIorWebcam(e.AcceptedImage, false)));
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
                Logger.Log("Invalid size value entered for spray tool: " + comboBoxSpraySize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                sprayToolSize = Settings1.Default.DefaultSpraySize;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for spray tool: " + comboBoxSpraySize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for brush: " + comboBoxBrushSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                brushSize = 11;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for brush: " + comboBoxBrushSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for eraser: " + comboBoxEraserSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                eraserSize = Settings1.Default.DefaultEraserSize;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for eraser: " + comboBoxEraserSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for pen: " + comboBoxPenSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                penSize = Settings1.Default.DefaultPenSize;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for pen: " + comboBoxPenSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for shape thickness: " + comboBoxShapeThickness.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                shapeThickness = Settings1.Default.DefaultShapeSize;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for shape thickness: " + comboBoxShapeThickness.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for text tool: " + fontSizeComboBox.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                textSize = Settings1.Default.DefaultTextSize;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for text tool: " + fontSizeComboBox.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for shape radius: " + textBoxRadius.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                radius = Settings1.Default.DefaultRadiusSize;
                textBoxRadius.Text = radius.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for shape radius: " + textBoxRadius.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for shape points: " + textBoxPoints.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                points = Settings1.Default.DefaultPointsCount;
                textBoxPoints.Text = points.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for shape points: " + textBoxPoints.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private Tools selectedTool; // Rename the variable from `currentTool` to `selectedTool`

        private void SaveStateForUndo()
        {
            if (MainBitmap != null &&
                activeLayerIndex >= 0 &&
                activeLayerIndex < editorLayers.Count)
            {
                if (undoStack.Count >= MaxStackSize)
                {
                    var tempList = undoStack.Reverse().ToList();
                    if (tempList.Count > 0)
                    {
                        tempList[0].Dispose();
                        tempList.RemoveAt(0);
                    }
                    undoStack = new Stack<LayerCanvasState>(tempList);
                }

                string layerKey = editorLayers[activeLayerIndex].ThumbnailKey;
                undoStack.Push(new LayerCanvasState(
                    layerKey,
                    new Bitmap(MainBitmap),
                    MainBitmap.Size));

                ClearRedoStack();
            }

            UpdateUndoRedoButtons();
        }

        private void RestoreHistoryState(LayerCanvasState state, Stack<LayerCanvasState> oppositeStack)
        {
            if (state == null)
                return;

            int targetIndex = FindLayerIndexByKey(state.LayerKey);
            if (targetIndex < 0)
            {
                state.Dispose();
                return;
            }

            EditorLayer targetLayer = editorLayers[targetIndex];

            Bitmap currentTarget;
            if (targetIndex == activeLayerIndex && MainBitmap != null)
                currentTarget = new Bitmap(MainBitmap);
            else
                currentTarget = new Bitmap(targetLayer.Bitmap);

            oppositeStack.Push(new LayerCanvasState(
                targetLayer.ThumbnailKey,
                currentTarget,
                currentTarget.Size));

            targetLayer.Bitmap?.Dispose();
            targetLayer.Bitmap = new Bitmap(state.Bitmap);

            if (targetIndex == activeLayerIndex)
            {
                MainBitmap?.Dispose();
                MainBitmap = new Bitmap(state.Bitmap);
                originalSize = MainBitmap.Size;

                canvasPanel.Size = new Size(
                    (int)Math.Round(MainBitmap.Width * zoom) + 20,
                    (int)Math.Round(MainBitmap.Height * zoom) + 20);
                pictureBoxCanvas.Size = panelResizer.Size;
                UIPanel.AutoScrollMinSize = canvasPanel.Size;
                labelSize.Text = $"{MainBitmap.Width} x {MainBitmap.Height} px";
            }

            // Undo/redo changes only this layer, so only this thumbnail is rebuilt.
            dirtyLayerThumbnailKeys.Add(targetLayer.ThumbnailKey);
            RefreshLayerThumbnail(targetIndex);

            SetAsUnselected();
            CenterCanvasPanel();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);

            state.Dispose();
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                LayerCanvasState previousState = undoStack.Pop();
                RestoreHistoryState(previousState, redoStack);
                isModified = undoStack.Count > 0;
            }

            UpdateUndoRedoButtons();
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                LayerCanvasState nextState = redoStack.Pop();
                RestoreHistoryState(nextState, undoStack);
                isModified = true;
                isSaved = false;
            }

            UpdateUndoRedoButtons();
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
            // Undo tuşunu yığın doluysa etkinleştir, boşsa devre dışı bırak
            geriAlToolStripMenuItem.Enabled = undoStack.Count > 0;

            // Redo tuşunu yığın doluysa etkinleştir, boşsa devre dışı bırak
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
                // Seçim kaldırılırken mevcut içeriği koruyarak işlemi gerçekleştirin
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

                        // Seçili alanı MainBitmap üzerine çizin
                        Rectangle sourceRect = new Rectangle(0, 0, SelectedBitmap.Width, SelectedBitmap.Height);
                        Rectangle destRect = new Rectangle(
                            originalSelectionRectangleLocation.X,
                            originalSelectionRectangleLocation.Y,
                            originalSelectionRectangleSize.Width,
                            originalSelectionRectangleSize.Height);

                        // MainBitmap'in mevcut içeriğini koruyarak seçili alanı güncelle
                        g.DrawImage(SelectedBitmap, destRect, sourceRect, GraphicsUnit.Pixel);
                    }

                    // SelectedBitmap'i serbest bırakın
                    MergeMainBitmapWithSelected();
                    SelectedBitmap.Dispose();
                    SelectedBitmap = null;
                }

                // Seçim durumunu sıfırlayın
                SelectionRectangle = Rectangle.Empty;
                isSelected = false;
                magicSelectionMask = null;
                magicSelectionMaskWidth = 0;
                magicSelectionMaskHeight = 0;
                magicSelectionBounds = Rectangle.Empty;
                magicSelectionOutlineSegments.Clear();
                if (artisticFilters == ArtisticFilters.None || blurEffect == BlurEffect.None)
                {
                    RenderCompositeToCanvas();
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
                Logger.Log("Invalid size value entered for bucket tool tolerance: " + textBoxTolerance.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tolerance = Settings1.Default.DefaultBucketTolerance;
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for bucket tool tolerance: " + textBoxTolerance.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                tolerance = Settings1.Default.DefaultBucketTolerance;
            }
            finally
            {
                textBoxTolerance.Text = tolerance.ToString();
            }
        }
        private void setMagicSelectionToolTolerance()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBoxTolerance2.Text))
                    throw new FormatException();

                int value = Convert.ToInt32(textBoxTolerance2.Text);

                // Magic Wand accepts the complete 0..100 range.
                // 0 = exact colour match, 100 = select everything connected
                // from the clicked pixel (which is the whole canvas because every colour matches).
                if (value < 0 || value > 100)
                    throw new OverflowException();

                tolerance2 = value;
            }
            catch (FormatException)
            {
                Logger.Log(
                    "Invalid tolerance value entered for magic selection tool: " + textBoxTolerance2.Text,
                    Logger.LogTypes.Error);
                MessageForm.Show(
                    "Invalid tolerance value is entered. Enter a value from 0 to 100.",
                    string.Empty,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (OverflowException)
            {
                Logger.Log(
                    "Magic selection tolerance must be between 0 and 100: " + textBoxTolerance2.Text,
                    Logger.LogTypes.Error);
                MessageForm.Show(
                    "Magic selection tolerance must be between 0 and 100.",
                    string.Empty,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Restore/show the last valid Magic Wand value only.
                textBoxTolerance2.Text = tolerance2.ToString();
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
            RenderCompositeToCanvas();
            labelSize.Text = $"{MainBitmap.Width} x {MainBitmap.Height} px";
            pictureBoxCanvas.Invalidate();
            CenterCanvasPanel();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            toolStripArtisticFilters.Visible = false;
            isModified = true; MarkLayerThumbnailDirty();
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

            RenderCompositeToCanvas();
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
                Logger.Log("Invalid size value entered for artistic filter size: " + textBoxArtisticFilterSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Size value too big or too small for artistic filter size: " + textBoxArtisticFilterSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid intensity value entered for artistic filter intensity: " + textBoxArtisticFilterIntensity.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid intensity value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Intensity value too big or too small for artistic filter intensity: " + textBoxArtisticFilterIntensity.Text, Logger.LogTypes.Error);
                MessageForm.Show("Intensity value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid threshold value entered for artistic filter threshold: " + textBoxArtisticFilterThreshold.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid threshold value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Threshold value too big or too small for artistic filter threshold: " + textBoxArtisticFilterThreshold.Text, Logger.LogTypes.Error);
                MessageForm.Show("Threshold value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("No image to print.", Logger.LogTypes.Error);
                MessageForm.Show("No image to print.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("No image to preview.", Logger.LogTypes.Error);
                MessageForm.Show("No image to preview.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                isModified = true; MarkLayerThumbnailDirty();
            }
            else
            {
                Logger.Log("No image in clipboard.", Logger.LogTypes.Error);
                MessageForm.Show("No image in clipboard.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            try
            {
                CutOrCopy();
                isModified = true; MarkLayerThumbnailDirty();
            }
            catch (Exception ex)
            {
                Logger.Log("Error copying image: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error copying image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CutOrCopy()
        {
            Bitmap copyBitmap;

            if (magicSelectionMask != null && !magicSelectionBounds.IsEmpty)
            {
                copyBitmap = new Bitmap(magicSelectionBounds.Width, magicSelectionBounds.Height, PixelFormat.Format32bppArgb);
                for (int y = magicSelectionBounds.Top; y < magicSelectionBounds.Bottom; y++)
                {
                    for (int x = magicSelectionBounds.Left; x < magicSelectionBounds.Right; x++)
                    {
                        int mx = x - magicSelectionBounds.Left;
                        int my = y - magicSelectionBounds.Top;

                        // magicSelectionMask is indexed in absolute MainBitmap coordinates.
                        // mx/my are only the destination coordinates inside copyBitmap.
                        if (x >= 0 && y >= 0 && x < magicSelectionMaskWidth && y < magicSelectionMaskHeight &&
                            magicSelectionMask[y * magicSelectionMaskWidth + x])
                        {
                            copyBitmap.SetPixel(mx, my, MainBitmap.GetPixel(x, y));
                        }
                    }
                }
            }
            else
            {
                copyBitmap = new Bitmap(MainBitmap.Width, MainBitmap.Height, PixelFormat.Format32bppArgb);
                using Graphics g = Graphics.FromImage(copyBitmap);
                g.Clear(Color.Transparent);
                g.DrawImageUnscaled(MainBitmap, 0, 0);
            }

            DataObject dataObject = new DataObject();
            using (MemoryStream pngStream = new MemoryStream())
            {
                copyBitmap.Save(pngStream, ImageFormat.Png);
                dataObject.SetData("PNG", false, pngStream);
            }
            dataObject.SetData(DataFormats.Bitmap, true, copyBitmap);
            Clipboard.SetDataObject(dataObject, true);
            copyBitmap.Dispose();
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            try
            {
                SaveStateForUndo();
                CutOrCopy();
                if (magicSelectionMask != null && !magicSelectionBounds.IsEmpty)
                {
                    for (int y = magicSelectionBounds.Top; y < magicSelectionBounds.Bottom; y++)
                    {
                        for (int x = magicSelectionBounds.Left; x < magicSelectionBounds.Right; x++)
                        {
                            if (magicSelectionMask[y * magicSelectionMaskWidth + x])
                                MainBitmap.SetPixel(x, y, Color.Transparent);
                        }
                    }
                    SetAsUnselected();
                }
                else
                {
                    MainBitmap = new Bitmap(MainBitmap.Width, MainBitmap.Height);
                }
                RenderCompositeToCanvas();
                pictureBoxCanvas.Invalidate();
                isModified = true; MarkLayerThumbnailDirty();
            }
            catch (Exception ex)
            {
                Logger.Log("Error cutting image: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error cutting image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            isModified = true; MarkLayerThumbnailDirty();
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
            if (MainBitmap == null) return;

            // Preserve the original canvas sizing/centering behavior.
            canvasPanel.Size = new Size(
                (int)(originalSize.Width * zoom) + 20,
                (int)(originalSize.Height * zoom) + 20);

            pictureBoxCanvas.Size = panelResizer.Size;

            // The scroll extent must describe the NEW canvas size, not the
            // previous zoom step's size.
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            CenterCanvasPanel();
            pictureBoxCanvas.Invalidate();
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
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                try
                {
                    SaveStateForUndo();
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
                        var imageTask = ImageGenerationCore.GenerateImage(textBoxPrompt.Text, SelectedBitmap,
                            SelectedBitmap.Width, SelectedBitmap.Height, cts.Token);
                        var image = await imageTask; // Await the task to get the result
                        if (image != null)
                        {
                            SelectedBitmap = new Bitmap(image); // Convert Image to Bitmap  
                            MergeMainBitmapWithSelected();
                            isModified = true; MarkLayerThumbnailDirty();
                        }
                        else
                        {
                            Undo();
                            ClearRedoStack();
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        var imageTask = ImageGenerationCore.GenerateImage(textBoxPrompt.Text, MainBitmap,
                            MainBitmap.Width, MainBitmap.Height, cts.Token);
                        var image = await imageTask; // Await the task to get the result
                        if (image != null)
                        {
                            MainBitmap = new Bitmap(image); // Convert Image to Bitmap  
                            pictureBoxCanvas.Invalidate();
                            isModified = true; MarkLayerThumbnailDirty();
                        }
                        else
                        {
                            Undo();
                            ClearRedoStack();
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Undo();
                    ClearRedoStack();
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    Undo();
                    ClearRedoStack();
                    Logger.Log("Error during AI image creation: " + ex.Message, Logger.LogTypes.Error);
                    MessageForm.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            else
            {
                Logger.Log("No internet connection available.", Logger.LogTypes.Error);
                MessageForm.Show("Please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            toolStripPixelate.Visible = false;
            isModified = true; MarkLayerThumbnailDirty();
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
                Logger.Log("Invalid size value entered for pixelation size: " + textBoxPixelationSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationSize = Settings1.Default.DefaultPixelationSize;
                textBoxPixelationSize.Text = pixelationSize.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for pixelation size: " + textBoxPixelationSize.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for pixelation offset X: " + textBoxPixelationOffsetX.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetX = Settings1.Default.DefaultPixelationOffsetX;
                textBoxPixelationOffsetX.Text = pixelationOffsetX.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for pixelation offset X: " + textBoxPixelationOffsetX.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid size value entered for pixelation offset Y: " + textBoxPixelationOffsetY.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid size value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY;
                textBoxPixelationOffsetY.Text = pixelationOffsetY.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Size value too big or too small for pixelation offset Y: " + textBoxPixelationOffsetY.Text, Logger.LogTypes.Error);
                MessageForm.Show("Size value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Logger.Log("Invalid radius value entered for Gaussian blur radius: " + textboxGaussianBlurRadius.Text, Logger.LogTypes.Error);
                MessageForm.Show("Invalid radius value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
                textboxGaussianBlurRadius.Text = gaussianBlurSize.ToString();
            }
            catch (OverflowException)
            {
                Logger.Log("Radius value too big or too small for Gaussian blur radius: " + textboxGaussianBlurRadius.Text, Logger.LogTypes.Error);
                MessageForm.Show("Radius value is too big or too small", String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            RenderCompositeToCanvas();
            pictureBoxCanvas.Invalidate();
            toolStripGaussianBlur.Visible = false;
            isModified = true; MarkLayerThumbnailDirty();
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
            if (e.KeyCode == Keys.Enter)
            {
                setShapeRadius();
            }
        }

        private void textBoxPoints_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setShapePoints();
            }
        }

        private void farklıKaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileAs();
        }

        private void cropTool_Click(object sender, EventArgs e)
        {
            CropToSelection();
        }

        private void rotateTool_Click(object sender, EventArgs e)
        {
            RotateCanvas(RotateFlipType.Rotate90FlipNone);
        }

        private void flipHTool_Click(object sender, EventArgs e)
        {
            RotateCanvas(RotateFlipType.RotateNoneFlipX);
        }

        private void flipVTool_Click(object sender, EventArgs e)
        {
            RotateCanvas(RotateFlipType.RotateNoneFlipY);
        }

        private void grayscaleTool_Click(object sender, EventArgs e)
        {
            ApplyGrayscaleQuick();
        }

        private void fitTool_Click(object sender, EventArgs e)
        {
            FitCanvasToWorkspace();
        }

        private void ImageEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isModified)
            {
                DialogResult result = MessageForm.Show("Do you want to save changes to your image?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                switch (result)
                {
                    case DialogResult.Yes:
                        save();
                        if (!fileSaved)
                        {
                            e.Cancel = true;
                        }
                        break;
                    case DialogResult.No:
                        // Proceed with closing without saving
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true; // Cancel closing
                        break;
                }
            }
        }

        private void toolStripTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setMagicSelectionToolTolerance();
            }
        }

        private void toolStripTextBox1_Leave_1(object sender, EventArgs e)
        {
            setMagicSelectionToolTolerance();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;

            if (key == Keys.Escape)
            {
                if (isSelected)
                {
                    SetAsUnselected();
                    pictureBoxCanvas.Invalidate();
                }

                return true;
            }

            if (key == Keys.Delete)
            {
                if (isSelected)
                {
                    DeleteCurrentSelection();
                }
                else
                {
                    // Photoshop-style Delete behavior:
                    // without an active selection, clear the entire active layer
                    // to transparency while leaving every other layer untouched.
                    ClearActiveLayer();
                }

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void ClearActiveLayer()
        {
            if (MainBitmap == null ||
                activeLayerIndex < 0 ||
                activeLayerIndex >= editorLayers.Count)
            {
                return;
            }

            // Keep this operation independently undoable for the active layer.
            SaveStateForUndo();

            // Clear every pixel of the selected/active layer to transparent.
            // SourceCopy is important here: normal SourceOver drawing with a
            // transparent brush would not actually erase existing pixels.
            using (Graphics g = Graphics.FromImage(MainBitmap))
            {
                g.CompositingMode =
                    System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.Clear(Color.Transparent);
            }

            // Persist only the active layer, then rebuild the visible stack.
            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);

            // Only the layer that was cleared needs a new thumbnail.
            MarkLayerThumbnailDirty();
            RefreshActiveLayerThumbnail();

            SetAsUnselected();

            isModified = true;
            isSaved = false;

            pictureBoxCanvas.Invalidate();
            UpdateUndoRedoButtons();
        }

        private void DeleteCurrentSelection()
        {
            if (!isSelected || MainBitmap == null)
                return;

            SaveStateForUndo();

            // Magic selection
            if (magicSelectionMask != null &&
                magicSelectionMaskWidth > 0 &&
                magicSelectionMaskHeight > 0 &&
                !magicSelectionBounds.IsEmpty)
            {
                Rectangle bounds = magicSelectionBounds;

                bounds.Intersect(
                    new Rectangle(0, 0, MainBitmap.Width, MainBitmap.Height));

                for (int y = bounds.Top; y < bounds.Bottom; y++)
                {
                    for (int x = bounds.Left; x < bounds.Right; x++)
                    {
                        int index = y * magicSelectionMaskWidth + x;

                        if (index >= 0 &&
                            index < magicSelectionMask.Length &&
                            magicSelectionMask[index])
                        {
                            MainBitmap.SetPixel(x, y, Color.Transparent);
                        }
                    }
                }
            }
            // Rectangular selection
            else if (!SelectionRectangle.IsEmpty)
            {
                Rectangle rect = new Rectangle(
                    originalSelectionRectangleLocation,
                    originalSelectionRectangleSize);

                rect.Intersect(
                    new Rectangle(Point.Empty, MainBitmap.Size));

                if (!rect.IsEmpty)
                {
                    using (Graphics g = Graphics.FromImage(MainBitmap))
                    {
                        g.CompositingMode =
                            System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                        using (Brush brush =
                            new SolidBrush(Color.Transparent))
                        {
                            g.FillRectangle(brush, rect);
                        }
                    }
                }
            }

            SetAsUnselected();

            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);

            isModified = true;
            isSaved = false;

            MarkLayerThumbnailDirty();
            RefreshActiveLayerThumbnail();

            pictureBoxCanvas.Invalidate();
            UpdateUndoRedoButtons();
        }

        private Image CreateThumbnail(Image originalImage)
        {
            if (originalImage == null)
                return null;
            if (originalImage.Width > originalImage.Height)
            {
                int newHeight = (int)(originalImage.Height * 64 / originalImage.Width);
                return originalImage.GetThumbnailImage(64, newHeight, null, IntPtr.Zero);
            }
            else
            {
                int newWidth = (int)(originalImage.Width * 64 / originalImage.Height);
                return originalImage.GetThumbnailImage(newWidth, 64, null, IntPtr.Zero);
            }
        }

        private void toolStripButtonAddLayer_Click(object sender, EventArgs e)
        {
            AddNewLayer();
            isModified = true;
            isSaved = false;
        }

        private void toolStripButtonMoveToUp_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            int oldIndex = listView1.SelectedIndices[0];
            if (oldIndex <= 0 || oldIndex >= editorLayers.Count)
                return;

            MoveLayer(oldIndex, oldIndex - 1);
        }

        private void toolStripButtonMoveToDown_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            int oldIndex = listView1.SelectedIndices[0];
            if (oldIndex < 0 || oldIndex >= editorLayers.Count - 1)
                return;

            MoveLayer(oldIndex, oldIndex + 1);
        }

        private void MoveLayer(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= editorLayers.Count ||
                newIndex < 0 || newIndex >= editorLayers.Count ||
                oldIndex == newIndex)
                return;

            // Persist edits before changing stack order. Reordering changes the
            // composite only; it must not regenerate any thumbnail.
            SyncActiveLayerFromCanvas();

            EditorLayer layer = editorLayers[oldIndex];
            editorLayers.RemoveAt(oldIndex);
            editorLayers.Insert(newIndex, layer);

            suppressLayerSelectionChanged = true;
            try
            {
                ListViewItem item = listView1.Items[oldIndex];
                listView1.Items.RemoveAt(oldIndex);
                listView1.Items.Insert(newIndex, item);

                activeLayerIndex = newIndex;
                listView1.SelectedItems.Clear();
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
            finally
            {
                suppressLayerSelectionChanged = false;
            }

            isModified = true;
            isSaved = false;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            listView1.Invalidate();
        }

        private void toolStripButtonDeleteLayer_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            // Keep at least one editable layer in every document.
            if (editorLayers.Count <= 1)
            {
                SystemSounds.Beep.Play();
                return;
            }

            int deleteIndex = listView1.SelectedIndices[0];
            if (deleteIndex < 0 || deleteIndex >= editorLayers.Count)
                return;

            SyncActiveLayerFromCanvas();

            EditorLayer deletedLayer = editorLayers[deleteIndex];
            string thumbnailKey = deletedLayer.ThumbnailKey;
            dirtyLayerThumbnailKeys.Remove(thumbnailKey);

            suppressLayerSelectionChanged = true;
            try
            {
                editorLayers.RemoveAt(deleteIndex);
                listView1.Items.RemoveAt(deleteIndex);
                imageListLayerThumbnails.Images.RemoveByKey(thumbnailKey);
                deletedLayer.Dispose();

                int newIndex = Math.Min(deleteIndex, editorLayers.Count - 1);

                // Force SwitchToLayer to load the newly selected layer rather than
                // treating the numerically identical index as the old active one.
                activeLayerIndex = -1;
                SwitchToLayer(newIndex);

                listView1.SelectedItems.Clear();
                listView1.Items[newIndex].Selected = true;
                listView1.Items[newIndex].Focused = true;
                listView1.Items[newIndex].EnsureVisible();
            }
            finally
            {
                suppressLayerSelectionChanged = false;
            }

            isModified = true;
            isSaved = false;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            listView1.Invalidate();
        }
    }

    internal sealed class LayerVisibilityToggledEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public bool Visible { get; }

        public LayerVisibilityToggledEventArgs(int rowIndex, bool visible)
        {
            RowIndex = rowIndex;
            Visible = visible;
        }
    }

    internal sealed class LayerItemRemovedEventArgs : EventArgs
    {
        public ListViewItem? Item { get; }

        public LayerItemRemovedEventArgs(ListViewItem? item)
        {
            Item = item;
        }
    }

    // A real DataGridView with a small compatibility facade for the existing layer code.
    // ImageEditor.cs can keep using its current ListView-style Items/SelectedIndices API,
    // while the actual UI is rendered as a grid with a per-row visibility button.
    internal sealed class LayerDataGridView : DataGridView
    {
        private const string ShowIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAAIDUlEQVR4nO2X21Mb9xXH1aZ/QzN9qdPYAQQIMFiIm7m5bVIn6dR1JmkynXYmbZzxpJPkwW1mmo6naSaxHT/lodM+daaBGEyNL9jYdYKRBNJedEUX0HWlXe1qhSRQHjpT3KnN6Zyfd8UKtJjYuH7RmTnDANr9fb7nfM/v95PBUIta1KIWtXjcIctyoyDKb16K5i/+wVPkX3UW1w7bV+90zZTA8mUJfmRdufPyXHHt9648fymSmxTF7BuyLBsfK3Q6LRtFMftJRsymxiN5eHF+FdpnvoL9MyVo+7IEbV+UoPUm5iq0/HMVWm6sgun6CsnnZoowFs6BKGU5UZTPSJJU/38D5yWpRxSlG6Ikgyedg1ecq9B+66sdwzdPr0DTtSI0XS3C0dkCuDkZ8F2ZTHZaFMWuRwfO898RROmcKGYB84v4MgzbSw8M34g5VQDLdAGuRXKKCGldEKVRjlt+clfhBUH6fiYj5TIK/I34MpgR/CHhjVcKYLxcgOYrebiOIpT3CxmpyPPi87sCnxKEd3hBvKvCe1I5GLKVdg2+AfNSHsxTeWCT90TgWrwgrguC+N5DwXNp4QNeELG15KWYD+N5Pfj6i3mom8zDkZu58jqYQkaCVIo/+UDwcS59PJUWyEvU6o8t5avC/8b3L8jfvgvLa3fhLccydL//Z+j/2esw+MPnYOAHz0LPS7+Ejt99Ck3jCV34uslleObCMnwe3OgCrp3mM5BMpo59LfholDMnufR/8GFVAKbeVongauQLRRgeHoahoSEYHByEgYEBOHjwIPT19UH34CFoOXNZF/6ZfyzD8NVKAegALsXfTiQS7TuCnwB4IppILiRTPKQFsSzAmszp2qZCQD5fBu/v7y/D9/b2Qnd3N1i6uqD548mq8PsmciRnI4qNNgRAPJEKWq3Wb91XQDgSORZLcIACeI2A0/6CruffcuSgUCgS+BMnThDwavBdXV3Q2dkJHb39UD8Srwq/93wOPnTKZQG4PpcWIJ5MwVI0+utt4QHgm6FILI4CuE0CfkEVdQfW/N6nBBSBVWgVvKenpwL+wIED0NHRAc1vn60Kv3c8By/dyFYVsBiNR5FRV4A3EOgPR2JQrQOH7Su6u43lyGsEFIFVaC24xWKpgN+/fz+Ynj1aFf7pMRkGL+t1IA7+UKh3GwHBs+GlaFmAdga6Z/S3Skv/IIFVE6G14GazuQK+ra0NWsxdVeExG8/LlTOw0QHwBcNndAV4/EF7SBXApckWVimg+j5v7hsgoJtTC97e3k7gW1tboaWlBZo7LFXhv3dOhsbxLbsQYVqMxMAbCM9uI2AhF1yMQDSehASXhpRGwGHriu4h1f7CywRUmwitBceqI7zJZILm5mYwHvpxVfinPs/CwMVN54AiAN3hCQSzugJYr/92ILwEkViCtKx8kGUk+Pl8UfeENb17loCqicBqquBYdRW+sbER6o6fqgr/1GgWfjqtOYn5DCkmFhWL6/EH1rYTsOYPhkmr4pt2olPevP71YCwOrZ3dBFSbCK0Fb2pqIvANbQdg79+iVeH3jGbhA8fWAcaiYnHdvoV/6wpwur05byAEOAebbWSNy9vebRo/ugAmBVZNFVoFNxqN0GA0wr6T53Xh94xI5CAj9hHEsn2wqFhc1uPXt9Ac47K6/QHQ2ggrgF3gMxL5JrXdxcz44QQ0dXSWgcvQDQ0k61s77gt/8MI9yxL7pIUK+3gXgkC53DO6AmxO9hPG48OtigyMuhupXTgXku9/q/wsBvXHP4b64Reh3tRGsm7wBdj35kfb2mbPiATf/UyCUZ9UMbxoZdw+F0KL4PItgJ1yndYVYKXpPgfrgS1dUM4E7MLR2fy2V2K964HewO7RwD8/JZE1sOOk+skUqX4Iqx8IAe32gs3BdusKwGPa5mRipAubZ0GxkispQff07sObxiSgYop1sPpcesP7SvXnaDYCAN/QFUC64GDfcDBunHbSNnwBvkgr4uqiTL4G7hb80yMSTIW2WmdJ2Xk8xPtesFPMrwz3i4mJiSdsFLtAub3kQXwB3kHK86CIuL6UBcvU7lT+cmATvLJt4uCiE9ARdorxIZthJzHPMB12irmND6L38EXVRLAJCX5yM/fgnr8iARvf8HwZHn2/FCWbCVpnnnGtWR1sm+HrhM3JHJujXcB6/VtEoJ3IYPMZMtyjCzIMTe0cHrfKEe9G1TmE59LENlh5hPcr8A7WvTPrVBVBMSe1ItBOOBM42FgpXBQXRwis4q2IBH9yynD0epZcifFWaRzPQv9kFo5cy8If5yS4tXiv4vhMSqk67jZYGCwQsY0G3kaz7z8Q/EYn2LftFHsX7YQzgYONZwRWChctC0nxBIh0RRGkTfXv+BluE3g0ngT8HoIFwkK5vH60zbqdYn5r2I2Yc7KHbBQj4z6MZwRWCBfDRbVCEAjthXCYnJLq7ygU/x/XgC9GYqTqaBmPP0AGdp5mCzYHfdiwmzFD00/aKGYEK4OWwsX8ihD0LJ6WKAahEK5a4v9wQNEqYRU8tEiuCIpl1u1O9u9Op/PbhkcVczRtsVHsNVwMhZCOBEIEBMUgFApCwIpcipITFT+DNsRnPAq40+VZt9PslM3pMj8y8C1CWLbOTjGn5xlXknb7QBWDUOhjn5rBMPmJf8NK42cQGq3iYN2JOYo5ZaWofYbHGQ6Hqx63ujma/auT9cxSrJejXZ4S7fHdYTy+/9Iub4lyezmHy31rnnb9Zd7JvI7PGGpRi1rUohaGxxz/A17+jsA6t7ttAAAAAElFTkSuQmCC";
        private const string HideIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAYAAABXAvmHAAAACXBIWXMAAAsTAAALEwEAmpwYAAAJfElEQVR4nO2ZeVCU5x3Ht9V4QLxyaKbTBBM8qjkgHkWaq7HttDV1EqPWeOIFigiiqWYm7aRJO21M2ul0OvGKR+QSNOKBAooILOz17sGyu8Au7L3vu7vcpv8Unajfzu9hF3hhF4kSM2R8Zn7Dy+6+z34/z+/7+z3P7kokD8aDMXQDkuUjhKjYLG9UTAZdS4bbEEj81FhQ0PWwg/BGxWQEAYYlBCTLR/SF8EbFnoTktZGS7zOEz+eb7eZ9W85ams/8SdvqWilv7Vwkbb+5oLQDcVc68Nvytpu/r2ztfF/d7Dpr9ufzvDfR5/PN+k4hnE7fLJ73furhvY48czMWV7VjTuk1vFjagdgrHYgt6UDMZYp2vHCpHS8Ut+P5ojYWvyltRW6tH7zgtfO87xNBEGbeNwh3XcMrPC8U84IPWqcfK+TtmHP12qDFP1fYhmcvtuLZC61YWtYCjd0Hmsvj8RbyPL/g24fYmAre6UFJYxMWSjvuWvxsioIWxBW24KLZH4AQbrt5Idtub5oypBBC/K9LekPUJ6RhQUnbPYufdb4Fs8614LnzzSgiCN7Lwu0RWl0u/o0hAXC43TtcDvctYeuunixMjcXJ5Tsx/3LbPYv/CcXZZswvaAZn64Lw8F643Pxtt5t/757E253uj1xunlILj4tHP4hl6Zh7qfWexc8804wZ+c1YctnPxAfD7RHgcLg+uCvxjXZnssPpZpPQZLQyeSY/slbtEUHkLUtHmuYrNF+/habOW9ii/C+m7LMj4kMjRr6nxUN7NIj4cw0m/6cRM/L9YcXPyG/C9NNNyDH2ZIHe2+nywGZzJH0j8RaLfb7N7rxBNwcBKKhVzitpR9ZKMYRv627g65ug4fnqBka8y2HkLhVG7lRgZLqcxUPpcox6V4kfH3aEFT/9yyYsvCAGIAfYHa7rVqt1zqDEnwJGWKy2GpvDBaeb7wYot/m7C3bO5bZ+EB0pexiE59qNLsE7qjAqTYpRaRXi2CHFk5/bQoqfdsrPoswcsFEPABqtDmN5efmdTwS1ZnNSg9UOAnD1AtirbxEV7IvFbch8Z7cIQkj6A97cb2RCR6eWYfT2UoxOuYIxKSWBuMIeG5tegeg8X0jx0Sf9+Kvc1w1A7293utFoc6DeYtk8oHgAPzSZGxoJwN4HYJ2itV+3iS1qReYKMcTnP1uPyG0lGLPtMsYmF2NschHGbi3sCrpOLmbPTfmnIaT46Dw/lhV7QwLUWRotpDEsgM5geLXW3IBQGVgkbQvZbWIutmDfwmQRxOEF6zAu6QIitlxAZNJ5RCadC8R5RGwpQMTWixi/pyKk+Gdyffj5uXAZaITeZHppAADjP2rrLd0AvWsgvjR8q4zYVYXPXt4sgjgStxoTN57Gw5tP4+FNXwaC/s9HZOJZRKYUhhRPMfukT1wDPRlAtbH2k7AAWr1RagoC2J2shYkBQvf5MelSRGwrwWcvbRBD/HQlHknIwfj1J7piQy7GbTzZBbP1fEjxT5/wYXZevy7ENNWZG6Az1JYNAFDjN9aZYWm0wWp3wtELYFF5W9hNavz7cozZdgnjtlzAvvgEEcTR+Svw2NovMHFdBiasy8SEhGwG8sjOwpDip+Z48dqZPvtAAIDcoTUYvWEBOJ3+uqG2HuYGK0tZ90bmEbCmqjXsDvvEv4ysQCOSCjBh02nsX7BGBHFs3nI8vuowJq05iolrj2NCQhae+JsipPip2V68XdhrJ3Z52GLSotLiavWGzoEAOvXGWpaqxj6d6GNdc9jjwczTPkRuv4TIxHPMHpMScnAgbqUYYu5STH7nICatPoJJG3PwdJYnpPiobC8+kvUvYFpUWlxNdc3/wgLINTq/zmAC1UHQRsE6KG/0DXi2eeqgBZFJZ5nHx6/PxqNrjuHA/BViiDlvY/Kqg3jy3zVhxUdlCWwjY/Zx8932oUWlxeW0+vAWqlSpyzV6A3rbiFaAsuDyCOyT1EAHs6j9dRi/JZ/5fOLaL/D4ykM4OG+ZCCLzV4mYns2HFf/K6S7LMvs43SL76GqMUKg1pWEBKuTcpyptNbUqVjDBbhQs5hMm3x1PldNyBfzo7wo8lpqPR9cdw5TVh3AoXmynrMVpmJbF9xP/VKaA7GpBVLxkZWqfNaY6qKtrIFWo94YFKFcqX5ZxWvTLQmBPoCwsLWse8Egc6ngwI9eLY0vER/GsxamIzvCIxL9RILD3oIyz1bc52OqbaPUNJig1OlTIuPiwALRNV8hVDSwLfWqBJqSJ1TYB8YWDFx9sldNzBBztA5H5u1REH3cz8c/nClA0BKxDq2939ng/sPqVSs4M4AdhAVgWZFyiTKWhamdpowloot4QF+p87GPgYMUHC3ZatoCjb/WHmH7cjQJTf+vUBzqPlnlfB6lCtUlyp3Hq1KkRFQquRqHRsRtpAjqDdNdDAKKo3ou4gsGLDxZsdCaPI30gTKtT4Xa4e8QH2iYVLjmBHCFVqKpJm2Qwo0qlmitVqK7TjeQ9migUBGcV8NZl/6DFBwv2mQwPjry5UwTBr98Ou9XeJZ58X29hzYSsU6VSd5bLuFjJNxkVclVSpVINTqfvB0F2YoXt8rDizq7x4fWCwYkPtspsjQd8khjCk5ACS30DE68PiJdxmsFZJySEQvVBVS8IshPVBBU2rRRlg/YJAqGMXDUL+Ivch6VFXnYkplPlrDwvXs33YslFLz6sFHC1rqvL0D0OmxPuxHQRhHNtMvR6Y7f4CiX3x7sS35MJLk2q4G6RnagmqLBpjyCPUja6QRwuZi2WlQBQ7wg+Tq8haPK61eZAg8UK1+Y0EYRtVRJkctVtqUK1WzIUo1LO/aJCofJRH6Y9grxJ2aAPP71BSBDZi8RR2AMR/J9A6Xl6Ld1jabSxjJqMdbBtSBFBuOb9snJIvxUvVSqnVChUWVUq9W2ylFZvYD4lEPIs7ZYEQ6JIXKig56hAqZYI3lhnZj2ejggarR6WtVvxrX+1X6lUxlUouIsyTsNAWEYMJiaEYEgUAZFAUdRb2I5KryEb0j3ami6vy9Xa21IlV6AsU8YJUTGZ9+X3iUqOmyFVqPZWqdQ2paYaQRgSRQVfHQxjLftLj7GV1huYaKopGaexVipUH5crFNPEv9PdJ4jgkMnUM6nVVSrVB+WctkzB6exKtbZDqa2+qdJWf61U6zoUGp1dptZcrVKqD1TJVRvpHkmY8Z1ADPXA9wZiamy2GCImQzKcBvpA0LVkuA0E7ETih9XPvg+GZBiM/wNKJ0CTsEYylAAAAABJRU5ErkJggg==";

        private readonly ListView compatibilityList = new();
        private readonly LayerListItemCollection itemCollection;
        private readonly LayerSelectedIndexCollection selectedIndexCollection;
        private readonly LayerSelectedItemCollection selectedItemCollection;
        private readonly LayerColumnCollection layerColumns;
        private readonly HashSet<ListViewItem> hiddenItems = new();
        private readonly Image showIcon;
        private readonly Image hideIcon;
        private bool syncingSelection;
        private ImageList? smallImageList;
        private ImageList? largeImageList;
        private bool useCompatibleStateImageBehavior;
        private View view = View.Details;
        private ColumnHeaderStyle headerStyle = ColumnHeaderStyle.None;
        private bool fullRowSelect = true;
        private bool hideSelection;

        public event EventHandler? SelectedIndexChanged;
        public event EventHandler<LayerVisibilityToggledEventArgs>? LayerVisibilityToggled;
        public event EventHandler<LayerItemRemovedEventArgs>? LayerItemRemoved;

        public LayerDataGridView()
        {
            showIcon = DecodeIcon(ShowIconBase64);
            hideIcon = DecodeIcon(HideIconBase64);

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AutoGenerateColumns = false;
            BackgroundColor = SystemColors.Window;
            BorderStyle = BorderStyle.Fixed3D;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersVisible = false;
            RowHeadersVisible = false;
            ReadOnly = true;
            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            RowTemplate.Height = 68;

            var visibilityColumn = new DataGridViewImageColumn
            {
                Name = "Visibility",
                HeaderText = string.Empty,
                Width = 32,
                MinimumWidth = 32,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Resizable = DataGridViewTriState.False
            };

            var thumbnailColumn = new DataGridViewImageColumn
            {
                Name = "Thumbnail",
                HeaderText = string.Empty,
                Width = 70,
                MinimumWidth = 70,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Resizable = DataGridViewTriState.False
            };

            var textColumn = new DataGridViewTextBoxColumn
            {
                Name = "Layer",
                HeaderText = "Layer",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 40,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };

            base.Columns.Add(visibilityColumn);
            base.Columns.Add(thumbnailColumn);
            base.Columns.Add(textColumn);

            layerColumns = new LayerColumnCollection(this, textColumn);
            itemCollection = new LayerListItemCollection(this, compatibilityList);
            selectedIndexCollection = new LayerSelectedIndexCollection(compatibilityList);
            selectedItemCollection = new LayerSelectedItemCollection(this, compatibilityList);

            compatibilityList.MultiSelect = false;
            compatibilityList.HideSelection = false;
            compatibilityList.CreateControl();
            compatibilityList.SelectedIndexChanged += CompatibilityList_SelectedIndexChanged;

            CellFormatting += LayerDataGridView_CellFormatting;
            SelectionChanged += LayerDataGridView_SelectionChanged;
        }

        public LayerListItemCollection Items => itemCollection;
        public LayerSelectedIndexCollection SelectedIndices => selectedIndexCollection;
        public LayerSelectedItemCollection SelectedItems => selectedItemCollection;
        public new LayerColumnCollection Columns => layerColumns;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList? SmallImageList
        {
            get => smallImageList;
            set { smallImageList = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList? LargeImageList
        {
            get => largeImageList;
            set => largeImageList = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseCompatibleStateImageBehavior
        {
            get => useCompatibleStateImageBehavior;
            set => useCompatibleStateImageBehavior = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public View View
        {
            get => view;
            set => view = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ColumnHeaderStyle HeaderStyle
        {
            get => headerStyle;
            set
            {
                headerStyle = value;
                ColumnHeadersVisible = value != ColumnHeaderStyle.None;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool FullRowSelect
        {
            get => fullRowSelect;
            set
            {
                fullRowSelect = value;
                SelectionMode = value
                    ? DataGridViewSelectionMode.FullRowSelect
                    : DataGridViewSelectionMode.CellSelect;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HideSelection
        {
            get => hideSelection;
            set
            {
                hideSelection = value;
                compatibilityList.HideSelection = value;
            }
        }

        // DataGridView does not inherit ListView row colors from BackColor/ForeColor.
        // Apply the same palette the old listView1 used to every layer-grid surface.
        public void ApplyListViewTheme(Color backColor, Color foreColor)
        {
            BackColor = backColor;
            ForeColor = foreColor;
            BackgroundColor = backColor;
            GridColor = backColor;

            DefaultCellStyle.BackColor = backColor;
            DefaultCellStyle.ForeColor = foreColor;
            DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            RowsDefaultCellStyle.BackColor = backColor;
            RowsDefaultCellStyle.ForeColor = foreColor;
            RowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            RowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            AlternatingRowsDefaultCellStyle.BackColor = backColor;
            AlternatingRowsDefaultCellStyle.ForeColor = foreColor;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

            ColumnHeadersDefaultCellStyle.BackColor = backColor;
            ColumnHeadersDefaultCellStyle.ForeColor = foreColor;
            RowHeadersDefaultCellStyle.BackColor = backColor;
            RowHeadersDefaultCellStyle.ForeColor = foreColor;

            Invalidate();
        }

        public void SetLayerVisibility(int rowIndex, bool visible)
        {
            if (rowIndex < 0 || rowIndex >= compatibilityList.Items.Count)
                return;

            ListViewItem item = compatibilityList.Items[rowIndex];
            if (visible)
                hiddenItems.Remove(item);
            else
                hiddenItems.Add(item);

            InvalidateRow(rowIndex);
        }

        internal void AddCompatibilityItem(ListViewItem item, int? insertIndex = null)
        {
            if (insertIndex.HasValue)
                compatibilityList.Items.Insert(insertIndex.Value, item);
            else
                compatibilityList.Items.Add(item);

            int index = insertIndex ?? (compatibilityList.Items.Count - 1);
            base.Rows.Insert(index, 1);
            base.Rows[index].Height = 68;
            InvalidateRow(index);
        }

        internal void RemoveCompatibilityItemAt(int index)
        {
            if (index < 0 || index >= compatibilityList.Items.Count)
                return;

            ListViewItem item = compatibilityList.Items[index];
            LayerItemRemoved?.Invoke(this, new LayerItemRemovedEventArgs(item));
            hiddenItems.Remove(item);
            compatibilityList.Items.RemoveAt(index);

            if (index < base.Rows.Count)
                base.Rows.RemoveAt(index);
        }

        internal void ClearCompatibilityItems()
        {
            foreach (ListViewItem item in compatibilityList.Items)
                LayerItemRemoved?.Invoke(this, new LayerItemRemovedEventArgs(item));

            hiddenItems.Clear();
            compatibilityList.Items.Clear();
            base.Rows.Clear();
        }

        private void CompatibilityList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (syncingSelection)
                return;

            syncingSelection = true;
            try
            {
                ClearSelection();
                if (compatibilityList.SelectedIndices.Count > 0)
                {
                    int index = compatibilityList.SelectedIndices[0];
                    if (index >= 0 && index < base.Rows.Count)
                    {
                        base.Rows[index].Selected = true;
                        if (FirstDisplayedScrollingRowIndex < 0 ||
                            index < FirstDisplayedScrollingRowIndex ||
                            index >= FirstDisplayedScrollingRowIndex + DisplayedRowCount(false))
                            FirstDisplayedScrollingRowIndex = index;
                    }
                }
            }
            finally
            {
                syncingSelection = false;
            }

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LayerDataGridView_SelectionChanged(object? sender, EventArgs e)
        {
            if (syncingSelection)
                return;

            syncingSelection = true;
            try
            {
                foreach (ListViewItem item in compatibilityList.Items)
                    item.Selected = false;

                if (SelectedRows.Count > 0)
                {
                    int index = SelectedRows[0].Index;
                    if (index >= 0 && index < compatibilityList.Items.Count)
                    {
                        compatibilityList.Items[index].Selected = true;
                        compatibilityList.Items[index].Focused = true;
                    }
                }
            }
            finally
            {
                syncingSelection = false;
            }

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            // Photoshop behavior: clicking the eye toggles visibility without making
            // that row the active editing layer. Selection is handled only by the
            // thumbnail/name cells.
            if (e.RowIndex >= 0 && e.ColumnIndex == 0 &&
                e.RowIndex < compatibilityList.Items.Count && e.Button == MouseButtons.Left)
            {
                ListViewItem item = compatibilityList.Items[e.RowIndex];
                bool nextVisible = hiddenItems.Contains(item);
                LayerVisibilityToggled?.Invoke(this, new LayerVisibilityToggledEventArgs(e.RowIndex, nextVisible));
                return;
            }

            base.OnCellMouseDown(e);
        }

        private void LayerDataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= compatibilityList.Items.Count)
                return;

            ListViewItem item = compatibilityList.Items[e.RowIndex];

            if (e.ColumnIndex == 0)
            {
                // Visible layer = open eye. Hidden layer = crossed-out eye.
                e.Value = hiddenItems.Contains(item) ? hideIcon : showIcon;
                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == 1)
            {
                if (smallImageList != null && !string.IsNullOrEmpty(item.ImageKey) &&
                    smallImageList.Images.ContainsKey(item.ImageKey))
                    e.Value = smallImageList.Images[item.ImageKey];
                else
                    e.Value = null;

                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == 2)
            {
                e.Value = item.Text;
                e.FormattingApplied = true;
            }
        }

        private static Image DecodeIcon(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var stream = new System.IO.MemoryStream(bytes);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                showIcon.Dispose();
                hideIcon.Dispose();
                compatibilityList.Dispose();
            }
            base.Dispose(disposing);
        }

        internal sealed class LayerListItemCollection
        {
            private readonly LayerDataGridView owner;
            private readonly ListView list;

            internal LayerListItemCollection(LayerDataGridView owner, ListView list)
            {
                this.owner = owner;
                this.list = list;
            }

            public int Count => list.Items.Count;
            public ListViewItem this[int index] => list.Items[index];
            public void Add(ListViewItem item) => owner.AddCompatibilityItem(item);
            public void Insert(int index, ListViewItem item) => owner.AddCompatibilityItem(item, index);
            public void RemoveAt(int index) => owner.RemoveCompatibilityItemAt(index);
            public void Clear() => owner.ClearCompatibilityItems();
        }

        internal sealed class LayerSelectedIndexCollection
        {
            private readonly ListView list;
            internal LayerSelectedIndexCollection(ListView list) => this.list = list;
            public int Count => list.SelectedIndices.Count;
            public int this[int index] => list.SelectedIndices[index];
        }

        internal sealed class LayerSelectedItemCollection
        {
            private readonly LayerDataGridView owner;
            private readonly ListView list;

            internal LayerSelectedItemCollection(LayerDataGridView owner, ListView list)
            {
                this.owner = owner;
                this.list = list;
            }

            public int Count => list.SelectedItems.Count;
            public ListViewItem this[int index] => list.SelectedItems[index];

            public void Clear()
            {
                owner.syncingSelection = true;
                try
                {
                    foreach (ListViewItem item in list.Items)
                        item.Selected = false;
                    owner.ClearSelection();
                }
                finally
                {
                    owner.syncingSelection = false;
                }

                owner.SelectedIndexChanged?.Invoke(owner, EventArgs.Empty);
            }
        }

        internal sealed class LayerColumnCollection
        {
            private readonly LayerDataGridView owner;
            private readonly DataGridViewTextBoxColumn textColumn;
            private bool hasLayerColumn;

            internal LayerColumnCollection(LayerDataGridView owner, DataGridViewTextBoxColumn textColumn)
            {
                this.owner = owner;
                this.textColumn = textColumn;
            }

            public int Count => hasLayerColumn ? 1 : 0;
            public LayerColumn this[int index]
            {
                get
                {
                    if (!hasLayerColumn || index != 0)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return new LayerColumn(owner, textColumn);
                }
            }

            public LayerColumn Add(string text)
            {
                hasLayerColumn = true;
                textColumn.HeaderText = text;
                return new LayerColumn(owner, textColumn);
            }

            public void AddRange(ColumnHeader[] headers)
            {
                hasLayerColumn = headers != null && headers.Length > 0;
                if (hasLayerColumn)
                    textColumn.HeaderText = headers[0].Text;
            }

            public void Clear()
            {
                hasLayerColumn = false;
                textColumn.HeaderText = string.Empty;
            }
        }

        internal sealed class LayerColumn
        {
            private readonly LayerDataGridView owner;
            private readonly DataGridViewTextBoxColumn textColumn;

            internal LayerColumn(LayerDataGridView owner, DataGridViewTextBoxColumn textColumn)
            {
                this.owner = owner;
                this.textColumn = textColumn;
            }

            public int Width
            {
                get => textColumn.Width;
                set
                {
                    // The old ListView width represented the full row. Keep the visibility
                    // button and 64px thumbnail fixed, then give the remaining space to text.
                    int available = Math.Max(40, value - 102);
                    textColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    textColumn.Width = available;
                    textColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    owner.Invalidate();
                }
            }
        }
    }

}