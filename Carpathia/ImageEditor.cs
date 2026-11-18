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
using ExifLibrary;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Runtime.Serialization;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Carpathia
{
    public partial class ImageEditor : Form
    {
        bool isModified = false;
        int x = -1, y = -1;
        bool isdrawing = false;
        Color color1 = Color.Black, color2 = Color.White;
        // The button that began the current drawing gesture determines its color.
        private MouseButtons paintingMouseButton = MouseButtons.Left;
        private Color ActivePaintColor => paintingMouseButton == MouseButtons.Right ? color2 : color1;
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
            pixelationOffsetY = Settings1.Default.DefaultPixelationOffsetY,
            cloneStampSize = 11;

        // Clone Stamp samples the active layer at Alt+click and paints its pixels
        // at a fixed offset. A frozen copy per stroke prevents feedback/smearing.
        private Point cloneStampSourcePoint;
        private bool cloneStampHasSource;
        private string cloneStampSourceLayerKey;
        private Bitmap cloneStampStrokeSource;
        private Point cloneStampStrokeOffset;
        private Point? cloneStampPreviousPoint;

        private string CurrentCloneStampLayerKey =>
            activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count
                ? editorLayers[activeLayerIndex].ThumbnailKey : null;

        private bool HasValidCloneStampSource =>
            cloneStampHasSource && cloneStampSourceLayerKey == CurrentCloneStampLayerKey &&
            MainBitmap != null && new Rectangle(Point.Empty, MainBitmap.Size).Contains(cloneStampSourcePoint);

        float textSize = Settings1.Default.DefaultTextSize;
        float zoom = 1,
            gaussianBlurSize = Settings1.Default.DefaultGaussianBlurRadius;
        bool isResizing = false, isSelected = false, backgroundFilling = false,
            isImageCurrentlyCreating = false, isSaved = false;

        // --- Paint-style canvas resize state ---
        private List<Bitmap> resizeOriginalLayers;
        private Size resizeOriginalSize;
        private Point resizeStartCursor;      // screen coordinates
        private Point resizeAnchorScreen;     // screen position of the edge that must not move

        // Resize animation only draws a preview while the mouse is held down.
        // The original layer bitmaps stay untouched until MouseUp, avoiding
        // repeated large allocations and preventing irreversible interim crops.
        private readonly System.Windows.Forms.Timer resizePreviewTimer = new System.Windows.Forms.Timer();
        private readonly Stopwatch resizePreviewClock = new Stopwatch();
        private ResizePreviewOverlay resizePreviewOverlay;
        private double resizePreviewWidth, resizePreviewHeight;
        private int resizeTargetWidth, resizeTargetHeight;
        private double resizePreviewLastMs;
        private const double ResizePreviewResponseMs = 42.0;
        private const int MaxCanvasDimension = 16000;
        // Expanding the canvas always adds transparent pixels on every layer.
        // Transparency checkerboard is a UI preview and is never baked into bitmaps.

        private bool ResizeAffectsLeft =>
            resizeDirection is ResizeDirection.Left or ResizeDirection.TopLeft or ResizeDirection.BottomLeft;
        private bool ResizeAffectsRight =>
            resizeDirection is ResizeDirection.Right or ResizeDirection.TopRight or ResizeDirection.BottomRight;
        private bool ResizeAffectsTop =>
            resizeDirection is ResizeDirection.Top or ResizeDirection.TopLeft or ResizeDirection.TopRight;
        private bool ResizeAffectsBottom =>
            resizeDirection is ResizeDirection.Bottom or ResizeDirection.BottomLeft or ResizeDirection.BottomRight;
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

        // One editable text overlay at a time. The backing layer is changed only
        // on commit, so cancelling an edit cannot leave half-rendered text behind.
        private TextBox activeTextEditor;
        private Font activeTextDocumentFont;
        private PointF activeTextImageLocation;
        private Color activeTextForeground;
        private Color activeTextBackground;
        private bool activeTextFillBackground;
        private bool completingTextEditor;
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
            public bool Locked { get; set; } = false;
            public int Opacity { get; set; } = 100;
            public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
            public string ThumbnailKey { get; }

            public EditorLayer(string name, Bitmap bitmap, string thumbnailKey = null)
            {
                Name = name;
                Bitmap = bitmap;
                ThumbnailKey = thumbnailKey ?? Guid.NewGuid().ToString("N");
            }

            public void Dispose()
            {
                Bitmap?.Dispose();
                Bitmap = null;
            }
        }

        private readonly List<EditorLayer> editorLayers = new();
        // Eye-button changes are intentionally NOT undoable/history actions.
        // Remember them by the layer's stable key so an unrelated full-layer
        // Undo/Redo does not restore an older eye state from its bitmap snapshot.
        private readonly Dictionary<string, bool> untrackedLayerVisibility = new(StringComparer.Ordinal);
        // Lock toggles are also not undoable. Preserve them across unrelated undo/redo.
        private readonly Dictionary<string, bool> untrackedLayerLocks = new(StringComparer.Ordinal);
        private int activeLayerIndex = -1;
        private bool suppressLayerSelectionChanged = false;
        private bool suppressLayerSettingsChanged = false;
        private bool opacityDragInProgress = false;
        private bool opacityDragUndoSaved = false;

        // Indexes intentionally match the combo box order below.
        private enum LayerBlendMode
        {
            Normal, Dissolve,
            Darken, Multiply, ColorBurn, LinearBurn, DarkerColor,
            Lighten, Screen, ColorDodge, LinearDodge, LighterColor,
            Overlay, SoftLight, HardLight, VividLight, LinearLight, PinLight, HardMix,
            Difference, Exclusion, Subtract, Divide,
            Hue, Saturation, Color, Luminosity
        }

        private static readonly string[] LayerBlendModeNames =
        {
            "Normal", "Dissolve",
            "Darken", "Multiply", "Color Burn", "Linear Burn", "Darker Color",
            "Lighten", "Screen", "Color Dodge", "Linear Dodge (Add)", "Lighter Color",
            "Overlay", "Soft Light", "Hard Light", "Vivid Light", "Linear Light", "Pin Light", "Hard Mix",
            "Difference", "Exclusion", "Subtract", "Divide",
            "Hue", "Saturation", "Color", "Luminosity"
        };

        private sealed class LayerPropertiesSnapshot
        {
            public string Name { get; }
            public string Key { get; }
            public bool Visible { get; }
            public bool Locked { get; }
            public int Opacity { get; }
            public LayerBlendMode BlendMode { get; }

            public LayerPropertiesSnapshot(EditorLayer layer)
            {
                Name = layer.Name;
                Key = layer.ThumbnailKey;
                Visible = layer.Visible;
                Locked = layer.Locked;
                Opacity = layer.Opacity;
                BlendMode = layer.BlendMode;
            }
        }

        private List<LayerPropertiesSnapshot> CaptureLayerProperties() =>
            editorLayers.Select(layer => new LayerPropertiesSnapshot(layer)).ToList();

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
            public List<Bitmap> LayerBitmaps { get; }
            public List<LayerPropertiesSnapshot> LayerProperties { get; }
            public int ActiveLayerIndex { get; }
            // The action that this snapshot can undo/redo. Its label travels with the
            // snapshot when it moves between the undo and redo stacks.
            public string HistoryDescription { get; }

            public LayerCanvasState(string layerKey, Bitmap bitmap, Size size,
                List<Bitmap> layerBitmaps = null,
                List<LayerPropertiesSnapshot> layerProperties = null,
                int activeLayerIndex = -1,
                string historyDescription = null)
            {
                LayerKey = layerKey;
                Bitmap = bitmap;
                Size = size;
                LayerBitmaps = layerBitmaps;
                LayerProperties = layerProperties;
                ActiveLayerIndex = activeLayerIndex;
                HistoryDescription = string.IsNullOrWhiteSpace(historyDescription)
                    ? "Edit image" : historyDescription;
            }

            public void Dispose()
            {
                Bitmap?.Dispose();
                if (LayerBitmaps != null)
                {
                    foreach (Bitmap layerBitmap in LayerBitmaps)
                        layerBitmap?.Dispose();
                }
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
            Lasso,
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
            CloneStamp,
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
            InitializeDocumentTabs();
        }
        public ImageEditor(String fileName)
        {
            InitializeComponentAndFont();
            SetInitialValues();
            InitializeSelectionPen(); // Initialize the SelectionPen
            openAFile(fileName);
            InitializeDocumentTabs();
        }
        public ImageEditor(Image image, bool AIGenerated)
        {
            InitializeComponentAndFont();
            SetInitialValues();
            InitializeSelectionPen(); // Initialize the SelectionPen
            createFileWithAIorWebcam(image, AIGenerated);
            InitializeDocumentTabs();
        }
        private Pen SelectionPen;

        private void CropToSelection()
        {
            if (IsActiveLayerLocked) return;
            if (MainBitmap == null || !isSelected || SelectionRectangle.IsEmpty)
            {
                MessageForm.Show("Create a rectangular selection first, then choose Crop.", "Crop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Rectangle r = new Rectangle(originalSelectionRectangleLocation, originalSelectionRectangleSize);
            r.Intersect(new Rectangle(Point.Empty, MainBitmap.Size));
            if (r.Width < 1 || r.Height < 1) return;
            SaveStateForUndo(description: "Crop to selection");
            var cropped = MainBitmap.Clone(r, PixelFormat.Format32bppArgb);
            MainBitmap.Dispose(); MainBitmap = cropped;
            SetAsUnselected(); RefreshCanvasAfterTransform();
        }

        private void RotateCanvas(RotateFlipType transform)
        {
            if (IsActiveLayerLocked) return;
            if (MainBitmap == null) return;
            SaveStateForUndo(description: "Rotate canvas"); MainBitmap.RotateFlip(transform); RefreshCanvasAfterTransform();
        }

        private void FitCanvasToWorkspace()
        {
            if (MainBitmap == null || UIPanel.ClientSize.Width < 1 || UIPanel.ClientSize.Height < 1)
                return;

            float zx = Math.Max(.05f, (UIPanel.ClientSize.Width - 60f) / MainBitmap.Width);
            float zy = Math.Max(.05f, (UIPanel.ClientSize.Height - 60f) / MainBitmap.Height);
            float fitZoom = Math.Min(5f, Math.Min(zx, zy));

            // Fit uses the same animation, but anchors the center of the image
            // rather than preserving the previous viewport position.
            StartZoomAnimation(fitZoom, centerOnImage: true);
        }

        private float CalculateDPIRate()
        {
            int defaultDPI = 96;
            float DPIrate = 1.0f;
            DPIrate = this.CreateGraphics().DpiX / defaultDPI;
            return DPIrate;
        }
        private void RefreshCanvasAfterTransform(bool markModified = true)
        {
            CancelZoomAnimation();
            originalSize = MainBitmap.Size;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
            cloneStampSizeCombobox.Text = cloneStampSize.ToString();
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
            foreach (TabPage page in tabControlWithCloseButtons1.TabPages)
            {
                page.BackColor = Color.Transparent;
                page.ForeColor = SystemColors.ControlText;
            }
            listView1.ApplyListViewTheme(ListView.DefaultBackColor, ListView.DefaultForeColor);
            toolStripLayer.BackgroundImage = Resources.toolstrip_light;
            tableLayoutPanel1.BackColor = SystemColors.Control;
            trackBarOpacity.BackColor = SystemColors.Control;
            comboBoxBlendMode.BackColor = SystemColors.Window;
            comboBoxBlendMode.ForeColor = SystemColors.WindowText;
            listViewHistory.BackColor = SystemColors.Window;
            listViewHistory.ForeColor = SystemColors.WindowText;
            documentTabsDarkTheme = false;
            ApplyDocumentTabTheme();
            label3.BackColor = SystemColors.Window;
            label3.ForeColor = SystemColors.WindowText;
            rulerControl1.BackColor = rulerControl2.BackColor = SystemColors.Window;
            rulerControl1.ForeColor = rulerControl2.ForeColor = SystemColors.WindowText;
            UIPanel.BackColor = Color.Transparent; 
            ApplyWorkspaceTheme(SystemColors.Control);
            UIHelper.ApplyCustomTitleBar(this, false);
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
            foreach (TabPage page in tabControlWithCloseButtons1.TabPages)
            {
                page.BackColor = Color.FromArgb(32, 32, 32);
                page.ForeColor = Color.White;
            }
            listView1.ApplyListViewTheme(Color.Black, Color.White);
            toolStripLayer.BackgroundImage = Resources.toolstrip_dark;
            tableLayoutPanel1.BackColor = Color.FromArgb(32, 32, 32);
            trackBarOpacity.BackColor = Color.FromArgb(32, 32, 32);
            comboBoxBlendMode.BackColor = Color.Black;
            comboBoxBlendMode.ForeColor = Color.White;
            listViewHistory.BackColor = Color.Black;
            listViewHistory.ForeColor = Color.White;
            documentTabsDarkTheme = true;
            ApplyDocumentTabTheme();
            label3.BackColor = Color.Black;
            label3.ForeColor = Color.White;
            rulerControl1.BackColor = rulerControl2.BackColor = Color.Black;
            rulerControl2.ForeColor = rulerControl1.ForeColor = Color.White;
            UIPanel.BackColor = Color.FromArgb(32, 32, 32);
            ApplyWorkspaceTheme(Color.FromArgb(32, 32, 32));
            UIHelper.ApplyCustomTitleBar(this, true);
        }
        private void ApplyWorkspaceTheme(Color background)
        {
            UIPanel.BackColor = background;
            canvasPanel.BackColor = background;
            panelResizer.BackColor = background;
            if (rulerWorkspace != null)
                rulerWorkspace.BackColor = background;
            foreach (TabPage tab in tabControlWithCloseButtons1.TabPages)
                tab.BackColor = background;
            UIPanel.Invalidate(true);
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

            // The zoomed canvas is the scrollable content of UIPanel. WinForms
            // AutoScroll cannot reliably reach the far edges of children
            // anchored to Bottom/Right, or children docked to Fill.
            // Keep the image surface top/left anchored; only UIPanel is docked
            // into the (tab-shared) ruler workspace.
            canvasPanel.Dock = DockStyle.None;
            canvasPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            UIPanel.AutoScroll = true;
            // Ensure the Clone Stamp handlers are attached exactly once even if a
            // Designer revision omitted one of the event subscriptions.
            cloneStampTool.CheckOnClick = true;
            cloneStampTool.CheckedChanged -= cloneStampTool_CheckedChanged;
            cloneStampTool.CheckedChanged += cloneStampTool_CheckedChanged;
            cloneStampTool.Click -= cloneStampTool_Click;
            cloneStampTool.Click += cloneStampTool_Click;
            cloneStampSizeCombobox.SelectedIndexChanged -= toolStripComboBox1_SelectedIndexChanged;
            cloneStampSizeCombobox.SelectedIndexChanged += toolStripComboBox1_SelectedIndexChanged;
            // Keep the mascot within the *visible* UIPanel viewport while dragging.
            MakeDraggable(sunnyTheCat1, UIPanel);
            if (!Settings1.Default.ShowSunnyTheCat)
            {
                sunnyTheCat1.Visible = false;
            }
            tabControl1.Width = (int)(CalculateDPIRate() * tabControl1.Size.Width);
            // PictureBox paints its Image automatically. Its native image pass must
            // fill the same zoomed bounds used for selection and pointer overlays.
            // The custom Paint handler below must ONLY draw overlays; otherwise the
            // original-size PictureBox image remains underneath transparent pixels.
            pictureBoxCanvas.SizeMode = PictureBoxSizeMode.StretchImage;
            // The timer changes only the on-screen preview. The expensive WinForms
            // AutoScroll/control layout is committed once, after the transition.
            zoomAnimationTimer.Interval = 16;
            zoomAnimationTimer.Tick += ZoomAnimationTimer_Tick;
            resizePreviewTimer.Interval = 16;
            resizePreviewTimer.Tick += ResizePreviewTimer_Tick;
            // Animate only the dash phase. 60 ms gives a smooth but lightweight motion.
            marchingAntsTimer.Interval = 60;
            marchingAntsTimer.Tick += MarchingAntsTimer_Tick;
            marchingAntsTimer.Start();
            this.MouseWheel += ImageEditor_MouseWheel;
            // Native WinForms scroll blitting can leave old child-window fragments on
            // screen (including resize handles). Buffer the entire canvas subtree.
            EnableCanvasDoubleBuffering();
            // AutoScroll performs its own child-window movement and invalidation.
            // Forcing a second repaint on every scroll message causes flashing.
            UIPanel.Resize += (s, e) =>
            {
                if (!updatingCanvasZoom)
                    CenterCanvasPanel();
            };
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
            // The Designer's SelectedIndexChanged handles preset sizes. Validate
            // a custom size when the editable combo box loses focus as well.
            cloneStampSizeCombobox.ComboBox.Leave += (s, e) => UpdateCloneStampSize();
            InitializeHistoryPanel();
            InitializeLayerSystem();
            // Set the default selected font to the first one in the list
            if (fontsComboBox.Items.Count > 0)
            {
                fontsComboBox.SelectedIndex = 0;
            }
            toolStripSample.Font = new Font(fontFamilies[fontsComboBox.SelectedIndex], toolStripSample.Font.Size, toolStripSample.Font.Style);

            // Do not change ActiveControl on MouseEnter. WinForms automatically calls
            // ScrollControlIntoView for newly focused child controls, which can
            // snap UIPanel back to its origin after the user scrolls a zoomed image.
            // Forward Ctrl+wheel without stealing focus or modifying scroll offsets.
            pictureBoxCanvas.TabStop = false;
            canvasPanel.TabStop = false;
            UIPanel.TabStop = false;
            pictureBoxCanvas.MouseWheel += ImageEditor_MouseWheel;
            pictureBoxCanvas.MouseDoubleClick += (s, args) =>
            {
                if (args.Button == MouseButtons.Left &&
                    pictureBoxCanvas.Controls.OfType<MovingImageControl>()
                        .Any(f => f.ContainsCanvasPoint(args.Location)))
                    CommitFloatingImagesToActiveLayer();
            };
            canvasPanel.MouseWheel += ImageEditor_MouseWheel;
            UIPanel.MouseWheel += ImageEditor_MouseWheel;

            // A mouse click outside a floating image commits it to the active
            // layer. WinForms sends MouseDown to the innermost child, so attach
            // to each of the canvas's otherwise empty surfaces.
            UIPanel.MouseDown += CommitFloatingImagesOnWorkspaceMouseDown;
            canvasPanel.MouseDown += CommitFloatingImagesOnWorkspaceMouseDown;
            panelResizer.MouseDown += CommitFloatingImagesOnWorkspaceMouseDown;
        }

        private void MarchingAntsTimer_Tick(object sender, EventArgs e)
        {
            bool hasSelection = isSelected &&
                (magicSelectionMask != null || !SelectionRectangle.IsEmpty);
            MovingImageControl[] floating = pictureBoxCanvas.Controls
                .OfType<MovingImageControl>().ToArray();

            // Do no repaint work when nothing is showing ants.
            if (!hasSelection && floating.Length == 0)
                return;

            marchingAntsOffset += 0.75f;
            if (marchingAntsOffset >= 6f)
                marchingAntsOffset -= 6f;

            if (hasSelection)
                pictureBoxCanvas.Invalidate();

            foreach (MovingImageControl f in floating)
                f.AntsOffset = marchingAntsOffset;
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
            RequestSmoothZoom(0.05f * wheelSteps);
        }

        private void ZoomIn() => RequestSmoothZoom(0.05f);

        private void ZoomOut() => RequestSmoothZoom(-0.05f);

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

            // Do not rely on ImageEditor.Designer.cs to hook the eye buttons. If the
            // Designer already subscribed, -= followed by += keeps exactly one handler.
            listView1.LayerVisibilityToggled -= listView1_LayerVisibilityToggled;
            listView1.LayerVisibilityToggled += listView1_LayerVisibilityToggled;
            listView1.LayerLockToggled -= listView1_LayerLockToggled;
            listView1.LayerLockToggled += listView1_LayerLockToggled;

            // Each control reflects only the active layer and never changes the other layers.
            suppressLayerSettingsChanged = true;
            try
            {
                comboBoxBlendMode.DropDownStyle = ComboBoxStyle.DropDownList;
                comboBoxBlendMode.Items.Clear();
                comboBoxBlendMode.Items.AddRange(LayerBlendModeNames);
                trackBarOpacity.Minimum = 0;
                trackBarOpacity.Maximum = 100;
                trackBarOpacity.TickFrequency = 10;
            }
            finally
            {
                suppressLayerSettingsChanged = false;
            }

            comboBoxBlendMode.SelectedIndexChanged -= comboBoxBlendMode_SelectedIndexChanged;
            comboBoxBlendMode.SelectedIndexChanged += comboBoxBlendMode_SelectedIndexChanged;
            trackBarOpacity.ValueChanged -= trackBarOpacity_ValueChanged;
            trackBarOpacity.ValueChanged += trackBarOpacity_ValueChanged;
            trackBarOpacity.MouseDown += (sender, args) =>
            {
                opacityDragInProgress = true;
                opacityDragUndoSaved = false;
            };
            trackBarOpacity.MouseUp += (sender, args) =>
            {
                opacityDragInProgress = false;
                opacityDragUndoSaved = false;
            };
            UpdateActiveLayerSettingsControls();

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


        private void UpdateActiveLayerSettingsControls()
        {
            bool hasLayer = activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count;
            suppressLayerSettingsChanged = true;
            try
            {
                bool editable = hasLayer && !editorLayers[activeLayerIndex].Locked;
                comboBoxBlendMode.Enabled = editable;
                trackBarOpacity.Enabled = editable;
                if (hasLayer)
                {
                    EditorLayer layer = editorLayers[activeLayerIndex];
                    comboBoxBlendMode.SelectedIndex = (int)layer.BlendMode;
                    trackBarOpacity.Value = Math.Clamp(layer.Opacity, 0, 100);
                    labelOpacity.Text = layer.Opacity.ToString() + "%";
                }
                else
                {
                    comboBoxBlendMode.SelectedIndex = -1;
                    trackBarOpacity.Value = 100;
                    labelOpacity.Text = "100%";
                }
            }
            finally
            {
                suppressLayerSettingsChanged = false;
            }
        }

        private void comboBoxBlendMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressLayerSettingsChanged || IsActiveLayerLocked || activeLayerIndex < 0 ||
                activeLayerIndex >= editorLayers.Count)
                return;

            int selectedMode = comboBoxBlendMode.SelectedIndex;
            if (selectedMode < 0 || selectedMode >= LayerBlendModeNames.Length)
                return;

            EditorLayer layer = editorLayers[activeLayerIndex];
            if ((int)layer.BlendMode == selectedMode)
                return;

            SaveStateForUndo(includeAllLayers: true, description: $"Blend mode: {LayerBlendModeNames[selectedMode]}");
            layer.BlendMode = (LayerBlendMode)selectedMode;
            UpdateActiveLayerSettingsControls();
            LayerSettingsModified();
        }

        private void LayerSettingsModified()
        {
            isModified = true;
            isSaved = false;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
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

        // activeLayerPreview is an optional *temporary* bitmap for shape/filter previews.
        // It replaces only the active layer in the composite; other layers retain
        // their own visibility, opacity, and blend mode throughout the preview.
        private Bitmap CreateCompositeBitmap(Bitmap activeLayerPreview = null)
        {
            Size size = MainBitmap?.Size ??
                (editorLayers.Count > 0 ? editorLayers[0].Bitmap.Size : Settings1.Default.DefaultCanvasSize);
            var composite = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height),
                PixelFormat.Format32bppArgb);
            ComposeRegion(composite, new Rectangle(Point.Empty, composite.Size), activeLayerPreview);
            return composite;
        }

        // Shared by full-frame rendering, export and live brush/shape previews.
        // Blend settings belong to EditorLayer, never to MainBitmap or the flattened preview.
        private void ComposeRegion(Bitmap target, Rectangle rectangle, Bitmap activeLayerPreview = null)
        {
            rectangle.Intersect(new Rectangle(Point.Empty, target.Size));
            if (rectangle.IsEmpty)
                return;

            using (Graphics g = Graphics.FromImage(target))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillRectangle(Brushes.Transparent, rectangle);
            }

            for (int i = editorLayers.Count - 1; i >= 0; i--)
            {
                EditorLayer layer = editorLayers[i];
                if (!layer.Visible || layer.Opacity <= 0)
                    continue;
                Bitmap source = (i == activeLayerIndex && activeLayerPreview != null)
                    ? activeLayerPreview : GetLayerBitmapForRender(i);
                if (source == null)
                    continue;

                Rectangle layerRegion = Rectangle.Intersect(rectangle,
                    new Rectangle(Point.Empty, source.Size));
                if (layerRegion.IsEmpty)
                    continue;

                if (layer.BlendMode == LayerBlendMode.Normal)
                    CompositeNormalLayer(target, source, layerRegion, layer.Opacity);
                else
                    CompositeBlendLayer(target, source, layerRegion, layer, i);
            }
        }

        // GDI+ does high-performance standard alpha compositing, including image alpha.
        private static void CompositeNormalLayer(Bitmap destination, Bitmap source,
            Rectangle area, int opacity)
        {
            using (Graphics g = Graphics.FromImage(destination))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                if (opacity >= 100)
                {
                    g.DrawImage(source, area, area.X, area.Y, area.Width, area.Height,
                        GraphicsUnit.Pixel);
                }
                else
                {
                    using (var attributes = new ImageAttributes())
                    {
                        var matrix = new ColorMatrix { Matrix33 = opacity / 100f };
                        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                        g.DrawImage(source, area, area.X, area.Y, area.Width, area.Height,
                            GraphicsUnit.Pixel, attributes);
                    }
                }
            }
        }

        // Accurate source-over blending on straight-alpha 32-bit pixels. Only the
        // requested rectangle is touched, so pen/brush/eraser previews stay responsive.
        private static void CompositeBlendLayer(Bitmap destination, Bitmap source,
            Rectangle area, EditorLayer layer, int layerIndex)
        {
            Bitmap converted = null;
            if (source.PixelFormat != PixelFormat.Format32bppArgb)
            {
                converted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(converted))
                    g.DrawImageUnscaled(source, 0, 0);
                source = converted;
            }

            BitmapData baseData = null;
            BitmapData sourceData = null;
            try
            {
                baseData = destination.LockBits(area, ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                sourceData = source.LockBits(area, ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                int bytesPerRow = checked(area.Width * 4);
                var baseRow = new byte[bytesPerRow];
                var sourceRow = new byte[bytesPerRow];
                float layerOpacity = layer.Opacity / 100f;

                for (int y = 0; y < area.Height; y++)
                {
                    IntPtr baseAddress = IntPtr.Add(baseData.Scan0, y * baseData.Stride);
                    IntPtr sourceAddress = IntPtr.Add(sourceData.Scan0, y * sourceData.Stride);
                    Marshal.Copy(baseAddress, baseRow, 0, bytesPerRow);
                    Marshal.Copy(sourceAddress, sourceRow, 0, bytesPerRow);

                    for (int x = 0; x < area.Width; x++)
                    {
                        int j = x * 4;
                        float sourceAlpha = (sourceRow[j + 3] / 255f) * layerOpacity;
                        if (sourceAlpha <= 0f)
                            continue;

                        if (layer.BlendMode == LayerBlendMode.Dissolve)
                        {
                            if (DissolveRandom(x + area.X, y + area.Y, layerIndex) >= sourceAlpha)
                                continue;
                            sourceAlpha = 1f;
                        }

                        float baseAlpha = baseRow[j + 3] / 255f;
                        float outputAlpha = sourceAlpha + baseAlpha * (1f - sourceAlpha);
                        if (outputAlpha <= 0f)
                            continue;

                        Vector3 backdrop = new Vector3(baseRow[j + 2], baseRow[j + 1], baseRow[j]) / 255f;
                        Vector3 foreground = new Vector3(sourceRow[j + 2], sourceRow[j + 1], sourceRow[j]) / 255f;
                        Vector3 blended = BlendColors(layer.BlendMode, backdrop, foreground);

                        // W3C compositing formula: blend only the overlapping coverage,
                        // preserving source color when the backdrop is transparent.
                        Vector3 premultiplied =
                            sourceAlpha * ((1f - baseAlpha) * foreground + baseAlpha * blended) +
                            (1f - sourceAlpha) * baseAlpha * backdrop;
                        Vector3 result = premultiplied / outputAlpha;

                        baseRow[j] = ColorByte(result.Z);
                        baseRow[j + 1] = ColorByte(result.Y);
                        baseRow[j + 2] = ColorByte(result.X);
                        baseRow[j + 3] = ColorByte(outputAlpha);
                    }

                    Marshal.Copy(baseRow, 0, baseAddress, bytesPerRow);
                }
            }
            finally
            {
                if (sourceData != null) source.UnlockBits(sourceData);
                if (baseData != null) destination.UnlockBits(baseData);
                converted?.Dispose();
            }
        }

        private static byte ColorByte(float value) =>
            (byte)Math.Clamp((int)(Math.Clamp(value, 0f, 1f) * 255f + 0.5f), 0, 255);

        // Stable hash ensures that a dissolve preview matches the exported image
        // and that partial brush repainting doesn't cause random flicker.
        private static float DissolveRandom(int x, int y, int layerIndex)
        {
            uint n = unchecked((uint)(x * 73856093 ^ y * 19349663 ^ layerIndex * 83492791));
            n ^= n >> 13;
            n = unchecked(n * 1274126177u);
            n ^= n >> 16;
            return (n & 0x00ffffff) / 16777216f;
        }

        private static float ClampUnit(float value) => Math.Clamp(value, 0f, 1f);
        private static float Burn(float b, float s) =>
            s <= 0f ? 0f : ClampUnit(1f - (1f - b) / s);
        private static float Dodge(float b, float s) =>
            s >= 1f ? 1f : ClampUnit(b / (1f - s));
        private static float Vivid(float b, float s) =>
            s <= 0.5f ? Burn(b, 2f * s) : Dodge(b, 2f * (s - 0.5f));

        private static float BlendChannel(LayerBlendMode mode, float b, float s)
        {
            switch (mode)
            {
                case LayerBlendMode.Darken: return Math.Min(b, s);
                case LayerBlendMode.Multiply: return b * s;
                case LayerBlendMode.ColorBurn: return Burn(b, s);
                case LayerBlendMode.LinearBurn: return ClampUnit(b + s - 1f);
                case LayerBlendMode.Lighten: return Math.Max(b, s);
                case LayerBlendMode.Screen: return b + s - b * s;
                case LayerBlendMode.ColorDodge: return Dodge(b, s);
                case LayerBlendMode.LinearDodge: return ClampUnit(b + s);
                case LayerBlendMode.Overlay: return b <= 0.5f ? 2f * b * s : 1f - 2f * (1f - b) * (1f - s);
                case LayerBlendMode.SoftLight:
                    return s <= 0.5f
                        ? b - (1f - 2f * s) * b * (1f - b)
                        : b + (2f * s - 1f) *
                            ((b <= 0.25f ? ((16f * b - 12f) * b + 4f) * b : (float)Math.Sqrt(b)) - b);
                case LayerBlendMode.HardLight: return s <= 0.5f ? 2f * b * s : 1f - 2f * (1f - b) * (1f - s);
                case LayerBlendMode.VividLight: return Vivid(b, s);
                case LayerBlendMode.LinearLight: return ClampUnit(b + 2f * s - 1f);
                case LayerBlendMode.PinLight: return s <= 0.5f ? Math.Min(b, 2f * s) : Math.Max(b, 2f * s - 1f);
                case LayerBlendMode.HardMix: return Vivid(b, s) < 0.5f ? 0f : 1f;
                case LayerBlendMode.Difference: return Math.Abs(b - s);
                case LayerBlendMode.Exclusion: return b + s - 2f * b * s;
                case LayerBlendMode.Subtract: return ClampUnit(b - s);
                case LayerBlendMode.Divide: return s <= 0f ? 1f : ClampUnit(b / s);
                default: return s;
            }
        }

        private static Vector3 BlendColors(LayerBlendMode mode, Vector3 backdrop, Vector3 source)
        {
            switch (mode)
            {
                case LayerBlendMode.Normal:
                case LayerBlendMode.Dissolve:
                    return source;
                case LayerBlendMode.DarkerColor:
                    return (backdrop.X + backdrop.Y + backdrop.Z) <=
                        (source.X + source.Y + source.Z) ? backdrop : source;
                case LayerBlendMode.LighterColor:
                    return (backdrop.X + backdrop.Y + backdrop.Z) >=
                        (source.X + source.Y + source.Z) ? backdrop : source;
                case LayerBlendMode.Hue:
                    return SetLum(SetSat(source, Sat(backdrop)), Lum(backdrop));
                case LayerBlendMode.Saturation:
                    return SetLum(SetSat(backdrop, Sat(source)), Lum(backdrop));
                case LayerBlendMode.Color:
                    return SetLum(source, Lum(backdrop));
                case LayerBlendMode.Luminosity:
                    return SetLum(backdrop, Lum(source));
                default:
                    return new Vector3(
                        BlendChannel(mode, backdrop.X, source.X),
                        BlendChannel(mode, backdrop.Y, source.Y),
                        BlendChannel(mode, backdrop.Z, source.Z));
            }
        }

        private static float Lum(Vector3 rgb) => 0.3f * rgb.X + 0.59f * rgb.Y + 0.11f * rgb.Z;
        private static float Sat(Vector3 rgb) =>
            Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z)) - Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));

        private static Vector3 SetSat(Vector3 rgb, float saturation)
        {
            float lowest = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
            float range = Sat(rgb);
            return range <= 0f ? Vector3.Zero :
                (rgb - new Vector3(lowest)) * (saturation / range);
        }

        private static Vector3 SetLum(Vector3 rgb, float luminosity)
        {
            rgb += new Vector3(luminosity - Lum(rgb));
            float min = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
            float max = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
            if (min < 0f)
                rgb = new Vector3(luminosity) +
                    (rgb - new Vector3(luminosity)) * (luminosity / (luminosity - min));
            if (max > 1f)
                rgb = new Vector3(luminosity) +
                    (rgb - new Vector3(luminosity)) * ((1f - luminosity) / (max - luminosity));
            return Vector3.Clamp(rgb, Vector3.Zero, Vector3.One);
        }

        // Preview a temporary edit as the active layer, not as a flattened canvas.
        // Important for shape and filter previews: mode/opacity/eye settings must
        // remain effective while the user is dragging or adjusting the preview.
        private void ShowActiveLayerPreview(Bitmap layerPreview)
        {
            if (layerPreview == null)
            {
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                return;
            }

            Bitmap displayed = editorLayers.Count > 0
                ? CreateCompositeBitmap(layerPreview)
                : new Bitmap(layerPreview);
            Image previous = pictureBoxCanvas.Image;
            pictureBoxCanvas.Image = displayed;

            // Ownership of the persistent composited bitmap belongs to
            // RenderCompositeToCanvas, and filter previews belong to previewBitmap.
            if (previous != null && previous != MainBitmap &&
                previous != compositeCanvasBitmap && previous != previewBitmap &&
                previous != layerPreview && !ReferenceEquals(previous, displayed))
                previous.Dispose();

            pictureBoxCanvas.Invalidate();
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
            Image oldDisplayed = pictureBoxCanvas.Image;

            compositeCanvasBitmap = nextComposite;
            pictureBoxCanvas.Image = compositeCanvasBitmap;
            compositeDirty = false;

            if (oldComposite != null &&
                !ReferenceEquals(oldComposite, MainBitmap) &&
                !ReferenceEquals(oldComposite, compositeCanvasBitmap))
                oldComposite.Dispose();

            // A shape preview can be a separate bitmap. Do not retain it or
            // dispose the layer's actual pixels, the filter preview, or an already
            // disposed previous composite.
            if (oldDisplayed != null && oldDisplayed != oldComposite &&
                oldDisplayed != MainBitmap && oldDisplayed != previewBitmap &&
                oldDisplayed != compositeCanvasBitmap)
                oldDisplayed.Dispose();

            pictureBoxCanvas.Invalidate();
        }

        private void MarkCompositeDirty()
        {
            compositeDirty = true;
            QueueTabThumbnailRefresh();
        }

        private void BeginToolStrokePreview()
        {
            // Keep the canvas displaying the full composite while a tool is used.
            // Never swap pictureBoxCanvas.Image to MainBitmap: doing that temporarily
            // reveals only the active layer and makes hidden/other layers disappear.
            suppressCompositeDuringToolStroke = true;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
        }

        private void EndToolStrokePreview()
        {
            suppressCompositeDuringToolStroke = false;
            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
                untrackedLayerVisibility.Clear(); // Starting a new/opened document.
                untrackedLayerLocks.Clear();
                activeLayerIndex = -1;
                listView1.Items.Clear();
                imageListLayerThumbnails.Images.Clear();
                dirtyLayerThumbnailKeys.Clear();

                compositeCanvasBitmap?.Dispose();
                compositeCanvasBitmap = null;

                if (MainBitmap == null)
                    return;

                AddLayerInternal(initialLayerName, MainBitmap, selectLayer: true);
                UpdateActiveLayerSettingsControls();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                pictureBoxCanvas.Update();
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
            SaveStateForUndo(includeAllLayers: true, description: "Add layer"); // Link layer creation to history

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
            UpdateActiveLayerSettingsControls();
            SetAsUnselected();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
        }

        private void SwitchToLayer(int newIndex)
        {
            if (newIndex < 0 || newIndex >= editorLayers.Count || newIndex == activeLayerIndex)
                return;

            // A source chosen on a different layer must not silently sample new pixels.
            StopCloneStampStroke();
            cloneStampHasSource = false;
            cloneStampSourceLayerKey = null;
            CompleteZoomAnimation();

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
            UpdateActiveLayerSettingsControls();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
            {
                // The grid is only a view of EditorLayer.Visible. Repair an icon that
                // could have become stale after a reorder, restore, or designer event.
                listView1.SetLayerVisibility(e.RowIndex, layer.Visible);
                return;
            }

            // Visibility is a view property, not a history action. In particular,
            // do NOT create an undo snapshot or discard the user's redo stack.
            layer.Visible = e.Visible;
            untrackedLayerVisibility[layer.ThumbnailKey] = e.Visible;
            listView1.SetLayerVisibility(e.RowIndex, layer.Visible);

            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            listView1.InvalidateRow(e.RowIndex);

            isModified = true;
            isSaved = false;
        }

        private bool IsActiveLayerLocked =>
            activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count &&
            editorLayers[activeLayerIndex].Locked;

        private bool AnyLayerLocked => editorLayers.Any(layer => layer.Locked);

        private void listView1_LayerLockToggled(object sender, LayerLockToggledEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= editorLayers.Count)
                return;

            EditorLayer layer = editorLayers[e.RowIndex];
            if (layer.Locked == e.Locked)
            {
                listView1.SetLayerLocked(e.RowIndex, layer.Locked);
                return;
            }

            // Finish any editable text or selection while pixels are still writable.
            if (e.RowIndex == activeLayerIndex && e.Locked)
            {
                CommitActiveTextEditor();
                if (isSelected)
                    SetAsUnselected();
                isdrawing = false;
                if (suppressCompositeDuringToolStroke)
                    EndToolStrokePreview();
                StopCloneStampStroke();
            }

            // Locking is a view/editing permission. It must not create a history row.
            layer.Locked = e.Locked;
            untrackedLayerLocks[layer.ThumbnailKey] = layer.Locked;
            listView1.SetLayerLocked(e.RowIndex, layer.Locked);
            UpdateActiveLayerSettingsControls();
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

            dirtyLayerThumbnailKeys.Add(editorLayers[activeLayerIndex].ThumbnailKey);

            // During a live freehand stroke, rebuilding the full layer composite and
            // restarting the thumbnail timer on every mouse move is extremely costly.
            // The small changed canvas region is refreshed by RefreshLiveStrokeRegion;
            // EndToolStrokePreview performs one full sync/composite when the stroke ends.
            if (suppressCompositeDuringToolStroke)
                return;

            MarkCompositeDirty();

            // Debounce non-stroke edits. The queue is keyed by layer, so switching layers
            // cannot accidentally refresh the wrong thumbnail.
            layerThumbnailRefreshTimer.Stop();
            layerThumbnailRefreshTimer.Start();
        }

        private bool IsFreehandPaintTool(Tools tool)
        {
            return tool == Tools.Brush ||
                   tool == Tools.Pen ||
                   tool == Tools.Eraser ||
                   tool == Tools.Spray ||
                   tool == Tools.CloneStamp;
        }

        private int GetCurrentPaintToolSize()
        {
            switch (selectedTool)
            {
                case Tools.Brush: return Math.Max(1, brushSize);
                case Tools.Pen: return Math.Max(1, penSize);
                case Tools.Eraser: return Math.Max(1, eraserSize);
                case Tools.Spray: return Math.Max(1, sprayToolSize);
                case Tools.CloneStamp: return Math.Max(1, cloneStampSize);
                default: return 1;
            }
        }

        private Rectangle GetLiveStrokeDirtyRectangle(MouseEventArgs e, int previousX, int previousY)
        {
            int cx = (int)Math.Round(e.X / zoom);
            int cy = (int)Math.Round(e.Y / zoom);
            int px = previousX >= 0 ? previousX : cx;
            int py = previousY >= 0 ? previousY : cy;

            // DrawBrush implementations may spread a little outside the nominal radius.
            // Keep a small safety margin so antialiasing/spray particles are fully refreshed.
            int margin = Math.Max(3, GetCurrentPaintToolSize() / 2 + 4);
            int left = Math.Min(px, cx) - margin;
            int top = Math.Min(py, cy) - margin;
            int right = Math.Max(px, cx) + margin + 1;
            int bottom = Math.Max(py, cy) + margin + 1;

            Rectangle dirty = Rectangle.FromLTRB(left, top, right, bottom);
            dirty.Intersect(new Rectangle(Point.Empty, MainBitmap.Size));
            return dirty;
        }

        private void RefreshLiveStrokeRegion(Rectangle imageRect)
        {
            if (imageRect.IsEmpty || MainBitmap == null)
                return;

            // With no layer stack, MainBitmap itself is already the display image.
            if (editorLayers.Count == 0)
            {
                pictureBoxCanvas.Image = MainBitmap;
            }
            else
            {
                // BeginToolStrokePreview creates a current full composite once. Reuse it
                // for the entire stroke and rebuild only the pixels touched by the brush.
                if (compositeCanvasBitmap == null ||
                    compositeCanvasBitmap.Width != MainBitmap.Width ||
                    compositeCanvasBitmap.Height != MainBitmap.Height)
                {
                    RenderCompositeToCanvas(force: true);
                }

                // Same per-layer blend and opacity logic as full rendering/export.
                // Recompute the entire dirty rectangle after erasing, so lower layers show.
                ComposeRegion(compositeCanvasBitmap, imageRect);

                pictureBoxCanvas.Image = compositeCanvasBitmap;
            }

            Rectangle displayRect = new Rectangle(
                (int)Math.Floor(imageRect.X * zoom) - 2,
                (int)Math.Floor(imageRect.Y * zoom) - 2,
                Math.Max(1, (int)Math.Ceiling(imageRect.Width * zoom) + 4),
                Math.Max(1, (int)Math.Ceiling(imageRect.Height * zoom) + 4));

            displayRect.Intersect(pictureBoxCanvas.ClientRectangle);
            if (!displayRect.IsEmpty)
                pictureBoxCanvas.Invalidate(displayRect);
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

        // The canvas, both rulers and panel3 (which contains label3) form a
        // SINGLE shared workspace. ImageEditor keeps one canvas for all tabs;
        // moving the whole host prevents rulers being left in an older tab.
        private bool rulersInitialized;
        private TableLayoutPanel rulerWorkspace;

        private void InitializeRulers()
        {
            if (rulersInitialized)
                return;

            // The first document's TabPage already owns UIPanel at this point.
            // Preserve designer-created controls, including the label3/panel3
            // hierarchy, the Vertical setting and the colors.
            Control originalParent = UIPanel.Parent;
            if (originalParent == null)
                return;

            // A missing Designer serialization of Vertical should not crash
            // the entire editor. Correct the second ruler at runtime if needed.
            if (rulerControl1.Vertical == rulerControl2.Vertical)
                rulerControl2.Vertical = !rulerControl1.Vertical;
            RulerControl horizontal = rulerControl1.Vertical ? rulerControl2 : rulerControl1;
            RulerControl verticalRuler = rulerControl1.Vertical ? rulerControl1 : rulerControl2;

            // Only the LEFT ruler/corner width comes from panel3.
            // panel3 can be taller than the original horizontal ruler;
            // using panel3.Height for the top row made the horizontal ruler
            // unexpectedly thick (up to 60 px) on every document tab.
            int cornerWidth = Math.Clamp(panel3.Width, 20, 60);

            // Capture the horizontal ruler's Designer height BEFORE Dock=Fill.
            // If it was already stretched in the Designer, use the control's
            // standard 26 px thickness (DPI-scaled) instead.
            int defaultRulerHeight = Math.Max(20,
                (int)Math.Round(26.0 * DeviceDpi / 96.0));
            int maximumDesignerHeight = Math.Max(36,
                (int)Math.Round(36.0 * DeviceDpi / 96.0));
            int cornerHeight = horizontal.Height >= 18 &&
                horizontal.Height <= maximumDesignerHeight
                ? horizontal.Height
                : defaultRulerHeight;

            rulerWorkspace = new TableLayoutPanel
            {
                Name = "rulerWorkspace",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UIPanel.BackColor
            };
            rulerWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, cornerWidth));
            rulerWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rulerWorkspace.RowStyles.Add(new RowStyle(SizeType.Absolute, cornerHeight));
            rulerWorkspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            rulerWorkspace.SuspendLayout();
            try
            {
                // Reparent the existing controls (not copies of them).
                // label3 stays in panel3; the image is still inside UIPanel.
                panel3.Dock = DockStyle.Fill;
                panel3.Margin = Padding.Empty;
                horizontal.Dock = DockStyle.Fill;
                horizontal.Margin = Padding.Empty;
                verticalRuler.Dock = DockStyle.Fill;
                verticalRuler.Margin = Padding.Empty;
                UIPanel.Dock = DockStyle.Fill;
                UIPanel.Margin = Padding.Empty;

                rulerWorkspace.Controls.Add(panel3, 0, 0);
                rulerWorkspace.Controls.Add(horizontal, 1, 0);
                rulerWorkspace.Controls.Add(verticalRuler, 0, 1);
                rulerWorkspace.Controls.Add(UIPanel, 1, 1);

                // Attach to the active tab only after all four cells exist.
                rulerWorkspace.Parent = originalParent;
            }
            finally
            {
                rulerWorkspace.ResumeLayout(true);
            }

            rulersInitialized = true;
            InitializeGuideInteractions(horizontal, verticalRuler);

            pictureBoxCanvas.MouseMove += RulerCanvas_MouseMove;
            pictureBoxCanvas.MouseLeave += RulerCanvas_MouseLeave;
            UIPanel.Scroll += (s, e) => UpdateRulers();
            UIPanel.Resize += (s, e) => UpdateRulers();
            canvasPanel.LocationChanged += (s, e) => UpdateRulers();
            pictureBoxCanvas.LocationChanged += (s, e) => UpdateRulers();
            pictureBoxCanvas.SizeChanged += (s, e) => UpdateRulers();
            rulerControl1.SizeChanged += (s, e) => UpdateRulers();
            rulerControl2.SizeChanged += (s, e) => UpdateRulers();

            tabControlWithCloseButtons1.SelectedIndexChanged += (s, e) =>
            {
                ClearRulerMouseMarkers();
                UpdateRulers();
            };
            Shown += (s, e) => UpdateRulers();
            UpdateRulers();
        }

        // Called by every tab creation/switch/reconciliation and tab close.
        // The TabPage never owns only the canvas: it owns the entire ruler
        // workspace. This also keeps live controls out of disposed TabPages.
        private void MoveCanvasWorkspaceTo(Control destination, bool visible = true)
        {
            if (destination == null || destination.IsDisposed)
                return;

            if (rulerWorkspace == null)
            {
                // Initial form setup (before InitializeRulers is called).
                if (!ReferenceEquals(UIPanel.Parent, destination))
                    UIPanel.Parent = destination;
                UIPanel.Dock = DockStyle.Fill;
                UIPanel.Visible = visible;
                return;
            }

            if (!ReferenceEquals(rulerWorkspace.Parent, destination))
            {
                // Guides belong to the document, not to whichever tab owns the controls.
                CancelGuideDrag();
                rulerWorkspace.Parent = destination;
            }

            rulerWorkspace.Dock = DockStyle.Fill;
            rulerWorkspace.Visible = visible;
            UIPanel.Visible = visible;
            ClearRulerMouseMarkers();
        }

        private bool CanvasWorkspaceIsIn(Control parent) =>
            ReferenceEquals(rulerWorkspace?.Parent ?? UIPanel.Parent, parent);

        private void RulerCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MainBitmap == null || !pictureBoxCanvas.IsHandleCreated)
                return;

            // Mouse coordinates are in PictureBox space. Convert them into
            // each ruler's own client coordinates, even when their parents differ.
            Point mouseScreen = pictureBoxCanvas.PointToScreen(e.Location);
            UpdateRulerMouseMarker(rulerControl1, mouseScreen);
            UpdateRulerMouseMarker(rulerControl2, mouseScreen);
        }

        private static void UpdateRulerMouseMarker(RulerControl ruler, Point mouseScreen)
        {
            if (ruler.IsDisposed || !ruler.IsHandleCreated)
                return;

            Point position = ruler.PointToClient(mouseScreen);
            ruler.MouseMarker = ruler.Vertical ? position.Y : position.X;
        }

        private void RulerCanvas_MouseLeave(object sender, EventArgs e) =>
            ClearRulerMouseMarkers();

        private void ClearRulerMouseMarkers()
        {
            rulerControl1.MouseMarker = null;
            rulerControl2.MouseMarker = null;
        }

        private void UpdateRulers()
        {
            try
            {
                if (!rulersInitialized || IsDisposed || Disposing ||
                MainBitmap == null || MainBitmap.Width == 0 || MainBitmap.Height == 0 ||
                !UIPanel.IsHandleCreated || !pictureBoxCanvas.IsHandleCreated)
                    return;

                Point imageOriginScreen;
                double scaleX;
                double scaleY;

                if (zoomAnimationTimer.Enabled)
                {
                    // The zoom animation draws into a temporary overlay; the actual
                    // PictureBox does not resize until the animation completes.
                    // Match exactly the image rectangle in PaintZoomAnimationFrame.
                    int imageWidth = Math.Max(1,
                        (int)Math.Round(MainBitmap.Width * zoomAnimationShown));
                    int imageHeight = Math.Max(1,
                        (int)Math.Round(MainBitmap.Height * zoomAnimationShown));
                    scaleX = (double)imageWidth / MainBitmap.Width;
                    scaleY = (double)imageHeight / MainBitmap.Height;

                    Size viewport = UIPanel.ClientSize;
                    double left = imageWidth + 20 <= viewport.Width
                        ? (viewport.Width - imageWidth) / 2.0
                        : Math.Max(viewport.Width - imageWidth - 10.0,
                            Math.Min(10.0, viewport.Width / 2.0 - zoomAnchorImageX * scaleX));
                    double top = imageHeight + 20 <= viewport.Height
                        ? (viewport.Height - imageHeight) / 2.0
                        : Math.Max(viewport.Height - imageHeight - 10.0,
                            Math.Min(10.0, viewport.Height / 2.0 - zoomAnchorImageY * scaleY));

                    imageOriginScreen = UIPanel.PointToScreen(
                        new Point((int)Math.Round(left), (int)Math.Round(top)));
                }
                else
                {
                    // Effective displayed size, not just the requested zoom:
                    // WinForms rounds PictureBox dimensions to integer pixels.
                    imageOriginScreen = pictureBoxCanvas.PointToScreen(Point.Empty);
                    scaleX = (double)pictureBoxCanvas.Width / MainBitmap.Width;
                    scaleY = (double)pictureBoxCanvas.Height / MainBitmap.Height;
                }

                UpdateSingleRuler(rulerControl1, imageOriginScreen, scaleX, scaleY);
                UpdateSingleRuler(rulerControl2, imageOriginScreen, scaleX, scaleY);
            }
            catch { }
        }

        private static void UpdateSingleRuler(
            RulerControl ruler, Point imageOriginScreen, double scaleX, double scaleY)
        {
            if (ruler.IsDisposed || !ruler.IsHandleCreated)
                return;

            Point zero = ruler.PointToClient(imageOriginScreen);
            ruler.Scale = ruler.Vertical ? scaleY : scaleX;
            ruler.Origin = ruler.Vertical ? zero.Y : zero.X;
        }

        private void CenterCanvasPanel()
        {
            if (UIPanel.IsDisposed || canvasPanel.IsDisposed)
                return;

            // DisplayRectangle is the virtual SCROLLING rectangle. Its size
            // can include a previous centered origin, so recentering a larger
            // image inside it shifts the canvas below/behind the last
            // reachable scrollbar position (the image's bottom gets clipped).
            // Center only an axis that fits. For an overflowing axis, honor
            // WinForms' actual scroll offset (negative when scrolled).
            Size viewport = UIPanel.ClientSize;
            Point scrollOffset = UIPanel.AutoScrollPosition;

            int left = canvasPanel.Width <= viewport.Width
                ? Math.Max(0, (viewport.Width - canvasPanel.Width) / 2)
                : scrollOffset.X;
            int top = canvasPanel.Height <= viewport.Height
                ? Math.Max(0, (viewport.Height - canvasPanel.Height) / 2)
                : scrollOffset.Y;

            Point desiredLocation = new Point(left, top);
            if (canvasPanel.Location != desiredLocation)
                canvasPanel.Location = desiredLocation;

            UpdateRulers();
        }
        private void CenterScrollbars()
        {
            int x = Math.Max(0,
                (canvasPanel.Width - UIPanel.ClientSize.Width) / 2);

            int y = Math.Max(0,
                (canvasPanel.Height - UIPanel.ClientSize.Height) / 2);

            UIPanel.AutoScrollPosition = new Point(x, y);
        }

        // Each tab owns its document data. The designer-created UIPanel (and its
        // child controls, events, resize handles and scrolling machinery) is the
        // live editor surface, moved into the selected tab. WinForms controls
        // cannot belong to two parents; reusing the working control tree avoids
        // fragile, event-less clones while preserving independent documents.
        private sealed class CanvasDocument : IDisposable
        {
            public TabPage Page;
            // Stable key: a tab can be re-created when an unsaved close is cancelled.
            public readonly string TabThumbnailKey = Guid.NewGuid().ToString("N");
            public readonly List<EditorLayer> Layers = new();
            public readonly Dictionary<string, bool> Visibility = new(StringComparer.Ordinal);
            public readonly Dictionary<string, bool> Locks = new(StringComparer.Ordinal);
            public readonly HashSet<string> DirtyThumbnails = new();
            public readonly List<(Point Start, Point End)> SelectionOutline = new();
            // Ruler guides belong to each image/document tab, never to the shared workspace.
            public readonly List<CanvasGuide> Guides = new();
            public Bitmap Bitmap;
            public Bitmap Selected;
            public Bitmap Preview;
            public Stack<LayerCanvasState> Undo = new();
            public Stack<LayerCanvasState> Redo = new();
            public string HistoryBase = "New document";
            public string FilePath = string.Empty;
            public string DisplayName = "Unnamed File";
            public bool Modified, Saved, FileSaved, Generated;
            public int ActiveLayer = -1;
            public float Zoom = 1f;
            public Size CanvasSize;
            public Point ScrollPosition;
            public bool IsSelected;
            public Rectangle SelectionRect;
            public Point SelectionStart;
            public Point SelectionImageLocation;
            public Size SelectionImageSize;
            public BitArray MagicMask;
            public int MagicWidth, MagicHeight;
            public Rectangle MagicBounds;
            public BlurEffect Blur;
            public ArtisticFilters Artistic;
            public BasicFilters Basic;

            public void Dispose()
            {
                foreach (EditorLayer layer in Layers) layer.Dispose();
                Layers.Clear();
                Guides.Clear();
                foreach (LayerCanvasState item in Undo) item.Dispose();
                foreach (LayerCanvasState item in Redo) item.Dispose();
                Undo.Clear();
                Redo.Clear();
                Bitmap?.Dispose();
                Selected?.Dispose();
                Preview?.Dispose();
                Bitmap = Selected = Preview = null;
            }
        }

        private readonly Dictionary<TabPage, CanvasDocument> canvasDocuments = new();
        private CanvasDocument activeCanvasDocument;
        private bool switchingCanvasDocument;
        private bool savingCanvasDocument;
        private bool closeAllDocumentsInProgress;
        private TabPage contextDocumentTab;
        private ContextMenuStrip documentTabMenu;
        private bool approvedDocumentTabRemoval;
        private bool documentTabsDarkTheme;
        private readonly System.Windows.Forms.Timer tabThumbnailRefreshTimer = new System.Windows.Forms.Timer();

        // Thumbnail generation is deferred while painting, but completed before
        // switching documents so each tab always has its own latest image.
        private void QueueTabThumbnailRefresh()
        {
            if (activeCanvasDocument == null || switchingCanvasDocument ||
                IsDisposed || Disposing || MainBitmap == null)
                return;
            tabThumbnailRefreshTimer.Stop();
            tabThumbnailRefreshTimer.Start();
        }

        private void TabThumbnailRefreshTimer_Tick(object sender, EventArgs e)
        {
            tabThumbnailRefreshTimer.Stop();
            if (isdrawing || isResizing || suppressCompositeDuringToolStroke ||
                switchingCanvasDocument)
            {
                QueueTabThumbnailRefresh();
                return;
            }
            RefreshCurrentTabThumbnail();
        }

        // Like the layer list thumbnails, a tab thumbnail shows transparency
        // using a checkerboard, and letterboxes wide/tall images to ImageSize.
        private Bitmap CreateDocumentTabThumbnail(Image source)
        {
            Size size = imageListTabThumbnails.ImageSize;
            int width = Math.Max(1, size.Width);
            int height = Math.Max(1, size.Height);
            var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                const int check = 4;
                using (Brush light = new SolidBrush(Color.FromArgb(240, 240, 240)))
                using (Brush dark = new SolidBrush(Color.FromArgb(205, 205, 205)))
                {
                    for (int y = 0; y < height; y += check)
                        for (int x = 0; x < width; x += check)
                            g.FillRectangle(((x / check + y / check) & 1) == 0 ? light : dark,
                                x, y, Math.Min(check, width - x), Math.Min(check, height - y));
                }
                g.CompositingMode = CompositingMode.SourceOver;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float scale = Math.Min((float)width / source.Width,
                    (float)height / source.Height);
                int drawnWidth = Math.Max(1, Math.Min(width, (int)Math.Round(source.Width * scale)));
                int drawnHeight = Math.Max(1, Math.Min(height, (int)Math.Round(source.Height * scale)));
                g.DrawImage(source,
                    new Rectangle((width - drawnWidth) / 2, (height - drawnHeight) / 2,
                        drawnWidth, drawnHeight),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }
            return thumbnail;
        }

        private void RefreshCurrentTabThumbnail()
        {
            CanvasDocument document = activeCanvasDocument;
            if (document == null || MainBitmap == null || document.Page == null ||
                document.Page.IsDisposed || !canvasDocuments.ContainsKey(document.Page) ||
                imageListTabThumbnails == null)
                return;

            // Use the full visible composition (layer visibility, opacity and
            // blend modes included), not the bitmap of just the selected layer.
            Bitmap temporaryComposite = null;
            try
            {
                Image source = compositeCanvasBitmap;
                if (source == null || compositeDirty || source.Size != MainBitmap.Size)
                {
                    temporaryComposite = CreateCompositeBitmap();
                    source = temporaryComposite;
                }
                if (source == null || source.Width < 1 || source.Height < 1)
                    return;

                using (Bitmap thumbnail = CreateDocumentTabThumbnail(source))
                {
                    string key = document.TabThumbnailKey;
                    imageListTabThumbnails.Images.RemoveByKey(key);
                    imageListTabThumbnails.Images.Add(key, new Bitmap(thumbnail));
                    document.Page.ImageKey = key;
                }
                tabControlWithCloseButtons1.Invalidate();
            }
            finally
            {
                temporaryComposite?.Dispose();
            }
        }

        private void FlushTabThumbnailBeforeSwitch()
        {
            tabThumbnailRefreshTimer.Stop();
            RefreshCurrentTabThumbnail();
        }

        // Set each document TabPage background without changing the tab control.
        // Apply on creation and on theme changes.
        private void ApplyDocumentTabTheme(TabPage page = null)
        {
            Color background = documentTabsDarkTheme
                ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
            Color foreground = documentTabsDarkTheme
                ? Color.White : SystemColors.ControlText;
            var tabs = tabControlWithCloseButtons1;
            if (page != null)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = background;
                page.ForeColor = foreground;
            }
            else
            {
                foreach (TabPage tab in tabs.TabPages)
                {
                    tab.UseVisualStyleBackColor = false;
                    tab.BackColor = background;
                    tab.ForeColor = foreground;
                }
            }
            foreach (TabPage tab in tabs.TabPages) tab.Invalidate();
        }

        private void InitializeDocumentTabs()
        {
            // The ImageList is declared in the user's updated Designer file.
            // Keep its configured ImageSize and use 32-bit images for alpha.
            imageListTabThumbnails.ColorDepth = ColorDepth.Depth32Bit;
            var tabs = tabControlWithCloseButtons1;
            if (!ReferenceEquals(tabs.ImageList, imageListTabThumbnails))
                tabs.ImageList = imageListTabThumbnails;
            tabThumbnailRefreshTimer.Interval = 400;
            tabThumbnailRefreshTimer.Tick -= TabThumbnailRefreshTimer_Tick;
            tabThumbnailRefreshTimer.Tick += TabThumbnailRefreshTimer_Tick;
            switchingCanvasDocument = true;
            try
            {
                TabPage firstPage;
                if (UIPanel.Parent is TabPage existing && existing.Parent == tabs)
                {
                    firstPage = existing;
                }
                else
                {
                    // Put the tab control exactly where UIPanel used to live.
                    // This works whether the canvas is in a normal Panel or a
                    // ToolStripContainer.ContentPanel.
                    Control workspaceParent = UIPanel.Parent;
                    Rectangle bounds = UIPanel.Bounds;
                    DockStyle dock = UIPanel.Dock;
                    AnchorStyles anchor = UIPanel.Anchor;
                    if (tabs.Parent != workspaceParent)
                        tabs.Parent = workspaceParent;
                    tabs.Dock = dock;
                    if (dock == DockStyle.None)
                    {
                        tabs.Bounds = bounds;
                        tabs.Anchor = anchor;
                    }
                    tabs.BringToFront();
                    firstPage = tabs.TabPages.Count > 0
                        ? tabs.TabPages[0] : new TabPage();
                    if (!tabs.TabPages.Contains(firstPage))
                        tabs.TabPages.Add(firstPage);
                    UIPanel.Parent = firstPage; // All nested controls come along.
                    UIPanel.Dock = DockStyle.Fill;
                }
                foreach (TabPage redundant in tabs.TabPages.Cast<TabPage>()
                    .Where(page => page != firstPage).ToArray())
                {
                    // Blank designer placeholder tabs are not documents.
                    if (redundant.Controls.Count == 0)
                        tabs.TabPages.Remove(redundant);
                }
                var document = new CanvasDocument { Page = firstPage };
                activeCanvasDocument = document;
                canvasDocuments.Add(firstPage, document);
                CaptureActiveCanvasDocument();
                UpdateCanvasDocumentTitle(document);
                tabs.SelectedTab = firstPage;
            }
            finally
            {
                switchingCanvasDocument = false;
            }

            ApplyDocumentTabTheme();
            // First document already exists when the constructor reaches here.
            RefreshCurrentTabThumbnail();
            QueueDocumentCanvasLayout(activeCanvasDocument);
            tabs.SelectedIndexChanged += CanvasDocumentTab_SelectedIndexChanged;
            tabs.ControlRemoved += CanvasDocumentTab_ControlRemoved;
            tabs.MouseUp += CanvasDocumentTab_MouseUp;
            documentTabMenu = new ContextMenuStrip();
            var closeItem = new ToolStripMenuItem("Close tab");
            closeItem.Click += (sender, args) => CloseDocumentTab(contextDocumentTab);
            documentTabMenu.Items.Add(closeItem);
            InitializeRulers();
        }

        private static string DocumentTabCaption(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed File";
            if (name.Equals("Unnamed File", StringComparison.OrdinalIgnoreCase))
                return name;
            if (name.Equals("AI Generated File", StringComparison.OrdinalIgnoreCase))
                return name;
            if (name.Equals("Webcam Image", StringComparison.OrdinalIgnoreCase))
                return name;
            return Path.GetFileName(name);
        }

        private void UpdateCanvasDocumentTitle(CanvasDocument document)
        {
            if (document?.Page == null || document.Page.IsDisposed) return;
            string title = DocumentTabCaption(document.DisplayName);
            document.Page.Text = document.Modified ? title + " *" : title;
        }

        private void UpdateCurrentDocumentTitle()
        {
            if (activeCanvasDocument == null) return;
            activeCanvasDocument.DisplayName = labelFileName.Text;
            activeCanvasDocument.Modified = isModified;
            UpdateCanvasDocumentTitle(activeCanvasDocument);
        }

        // Store references, not cloned bitmaps. The inactive document owns them
        // while editorLayers, undoStack, etc. point at a different document.
        private void CaptureActiveCanvasDocument()
        {
            CanvasDocument document = activeCanvasDocument;
            if (document == null) return;
            document.Layers.Clear();
            document.Layers.AddRange(editorLayers);
            document.Visibility.Clear();
            foreach (var pair in untrackedLayerVisibility)
                document.Visibility.Add(pair.Key, pair.Value);
            document.Locks.Clear();
            foreach (var pair in untrackedLayerLocks)
                document.Locks.Add(pair.Key, pair.Value);
            document.DirtyThumbnails.Clear();
            document.DirtyThumbnails.UnionWith(dirtyLayerThumbnailKeys);
            document.Bitmap = MainBitmap;
            document.Selected = SelectedBitmap;
            document.Preview = previewBitmap;
            document.Undo = undoStack;
            document.Redo = redoStack;
            document.ActiveLayer = activeLayerIndex;
            document.CanvasSize = originalSize;
            document.Zoom = zoom;
            document.HistoryBase = historyBaseDescription;
            document.FilePath = openedFilePath;
            document.DisplayName = labelFileName.Text;
            document.Modified = isModified;
            document.Saved = isSaved;
            document.FileSaved = fileSaved;
            document.Generated = aiGenerated;
            document.ScrollPosition = new Point(
                -UIPanel.AutoScrollPosition.X, -UIPanel.AutoScrollPosition.Y);
            document.IsSelected = isSelected;
            document.SelectionRect = SelectionRectangle;
            document.SelectionStart = SelectionStartPoint;
            document.SelectionImageLocation = originalSelectionRectangleLocation;
            document.SelectionImageSize = originalSelectionRectangleSize;
            document.MagicMask = magicSelectionMask;
            document.MagicWidth = magicSelectionMaskWidth;
            document.MagicHeight = magicSelectionMaskHeight;
            document.MagicBounds = magicSelectionBounds;
            document.SelectionOutline.Clear();
            document.SelectionOutline.AddRange(magicSelectionOutlineSegments);
            document.Blur = blurEffect;
            document.Artistic = artisticFilters;
            document.Basic = basicFilters;
            UpdateCanvasDocumentTitle(document);
        }

        // Canvas previews can be regenerated. They are NOT the original layer
        // bitmaps and must never hold a document's only image data.
        private void DetachCanvasPreview()
        {
            pictureBoxCanvas.Image = null;
            compositeCanvasBitmap?.Dispose();
            compositeCanvasBitmap = null;
            compositeDirty = true;
        }

        private void RestoreCanvasDocument(CanvasDocument document)
        {
            activeCanvasDocument = document;
            MainBitmap = document.Bitmap;
            SelectedBitmap = document.Selected;
            previewBitmap = document.Preview;
            undoStack = document.Undo;
            redoStack = document.Redo;
            historyBaseDescription = document.HistoryBase;
            editorLayers.Clear();
            editorLayers.AddRange(document.Layers);
            untrackedLayerVisibility.Clear();
            foreach (var pair in document.Visibility)
                untrackedLayerVisibility.Add(pair.Key, pair.Value);
            untrackedLayerLocks.Clear();
            foreach (var pair in document.Locks)
                untrackedLayerLocks.Add(pair.Key, pair.Value);
            dirtyLayerThumbnailKeys.Clear();
            dirtyLayerThumbnailKeys.UnionWith(document.DirtyThumbnails);
            activeLayerIndex = document.ActiveLayer;
            originalSize = document.CanvasSize;
            zoom = document.Zoom;
            openedFilePath = document.FilePath;
            fileSaved = document.FileSaved;
            isSaved = document.Saved;
            isModified = document.Modified;
            aiGenerated = document.Generated;
            isSelected = document.IsSelected;
            SelectionRectangle = document.SelectionRect;
            SelectionStartPoint = document.SelectionStart;
            originalSelectionRectangleLocation = document.SelectionImageLocation;
            originalSelectionRectangleSize = document.SelectionImageSize;
            magicSelectionMask = document.MagicMask;
            magicSelectionMaskWidth = document.MagicWidth;
            magicSelectionMaskHeight = document.MagicHeight;
            magicSelectionBounds = document.MagicBounds;
            magicSelectionOutlineSegments.Clear();
            magicSelectionOutlineSegments.AddRange(document.SelectionOutline);
            blurEffect = document.Blur;
            artisticFilters = document.Artistic;
            basicFilters = document.Basic;
            labelFileName.Text = document.DisplayName;
            farklıKaydetToolStripMenuItem.Enabled = !string.IsNullOrEmpty(openedFilePath);
            labelSize.Text = MainBitmap != null
                ? $"{MainBitmap.Width} X {MainBitmap.Height}px" : string.Empty;
            labelZoom.Text = $"{Math.Round(zoom * 100)}%";

            suppressLayerSelectionChanged = true;
            // LayerDataGridView derives from DataGridView, not ListView.
            // SuspendLayout is supported by both and avoids invalid BeginUpdate calls.
            listView1.SuspendLayout();
            try
            {
                listView1.Items.Clear();
                imageListLayerThumbnails.Images.Clear();
                for (int i = 0; i < editorLayers.Count; i++)
                {
                    EditorLayer layer = editorLayers[i];
                    Bitmap source = GetLayerBitmapForRender(i);
                    if (source != null)
                        using (Bitmap thumb = CreateLayerThumbnail(source))
                            imageListLayerThumbnails.Images.Add(
                                layer.ThumbnailKey, new Bitmap(thumb));
                    listView1.Items.Add(new ListViewItem(layer.Name)
                    {
                        ImageKey = layer.ThumbnailKey,
                        Tag = layer
                    });
                    listView1.SetLayerVisibility(i, layer.Visible);
                    listView1.SetLayerLocked(i, layer.Locked);
                }
                if (activeLayerIndex >= 0 && activeLayerIndex < listView1.Items.Count)
                {
                    listView1.Items[activeLayerIndex].Selected = true;
                    listView1.Items[activeLayerIndex].Focused = true;
                }
            }
            finally
            {
                listView1.ResumeLayout(performLayout: true);
                listView1.Invalidate();
                suppressLayerSelectionChanged = false;
            }
            UpdateActiveLayerSettingsControls();
            SetZoomedCanvasBounds();
            CenterCanvasPanel();
            if (previewBitmap != null)
                ShowActiveLayerPreview(previewBitmap);
            else
                RenderCompositeToCanvas(force: true);
            ScaleSelection();
            UIPanel.AutoScrollPosition = document.ScrollPosition;
            PositionZoomResizeHandles();
            pictureBoxCanvas.Invalidate();
            UpdateUndoRedoButtons();
            UpdateCurrentDocumentTitle();
            UpdateRulers();
        }

        // A removed TabPage may have changed selection BEFORE or AFTER
        // ControlRemoved. Reconcile after WinForms has finished the entire
        // tab-removal message and the surviving page has been laid out.
        private void QueueDocumentTabReconciliation()
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || Disposing || switchingCanvasDocument) return;
                TabPage page = tabControlWithCloseButtons1.SelectedTab;
                if (page == null || !canvasDocuments.TryGetValue(page, out var document))
                {
                    page = tabControlWithCloseButtons1.TabPages.Cast<TabPage>()
                        .FirstOrDefault(candidate => canvasDocuments.ContainsKey(candidate));
                    if (page == null) return;
                    tabControlWithCloseButtons1.SelectedTab = page;
                    if (!canvasDocuments.TryGetValue(page, out document)) return;
                }

                if (!ReferenceEquals(activeCanvasDocument, document))
                {
                    // A different (surviving) document is still active. Save
                    // its current state before switching to the selected page.
                    switchingCanvasDocument = true;
                    try
                    {
                        if (activeCanvasDocument != null &&
                            canvasDocuments.ContainsKey(activeCanvasDocument.Page))
                        {
                            CompleteZoomAnimation();
                            CommitActiveTextEditor();
                            CommitFloatingImagesToActiveLayer();
                            layerThumbnailRefreshTimer.Stop();
                            SyncActiveLayerFromCanvas();
                            CaptureActiveCanvasDocument();
                            FlushTabThumbnailBeforeSwitch();
                        }
                        DetachCanvasPreview();
                        MoveCanvasWorkspaceTo(page);
                        RestoreCanvasDocument(document);
                    }
                    finally { switchingCanvasDocument = false; }
                }
                else
                {
                    // A selected page can exist without owning the shared
                    // workspace if the old page was disposed by the X button.
                    MoveCanvasWorkspaceTo(page);
                }
                QueueDocumentCanvasLayout(document);
            }));
        }

        // UIPanel's scrolling state and canvas centering depend on the page's
        // final viewport size. Restoring them inside ControlRemoved (or before
        // the replacement page is selected) uses stale/zero bounds.
        private void QueueDocumentCanvasLayout(CanvasDocument document)
        {
            if (document == null || IsDisposed || Disposing || !IsHandleCreated)
                return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || Disposing ||
                    !ReferenceEquals(activeCanvasDocument, document) ||
                    !ReferenceEquals(tabControlWithCloseButtons1.SelectedTab, document.Page) ||
                    !CanvasWorkspaceIsIn(document.Page) || MainBitmap == null)
                    return;

                document.Page.PerformLayout();
                rulerWorkspace?.PerformLayout();
                UIPanel.Visible = true;
                UIPanel.Dock = DockStyle.Fill;
                UIPanel.PerformLayout();
                // Reset the *previous* tab's scroll offset before the size
                // change, then restore the target document's offset.
                UIPanel.AutoScrollPosition = Point.Empty;
                SetZoomedCanvasBounds();
                UIPanel.PerformLayout();
                UIPanel.AutoScrollPosition = document.ScrollPosition;
                CenterCanvasPanel();
                PositionZoomResizeHandles();
                UIPanel.Invalidate(true);
                pictureBoxCanvas.Invalidate();
                UpdateRulers();
            }));
        }

        private void CanvasDocumentTab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (switchingCanvasDocument) return;
            TabPage page = tabControlWithCloseButtons1.SelectedTab;
            if (page == null || !canvasDocuments.TryGetValue(page, out var next) ||
                ReferenceEquals(next, activeCanvasDocument)) return;
            if (savingCanvasDocument || isImageCurrentlyCreating || isResizing || isdrawing)
            {
                // Never allow a task to finish writing into the wrong tab.
                switchingCanvasDocument = true;
                try
                {
                    if (activeCanvasDocument?.Page != null &&
                        tabControlWithCloseButtons1.TabPages.Contains(activeCanvasDocument.Page))
                        tabControlWithCloseButtons1.SelectedTab = activeCanvasDocument.Page;
                }
                finally { switchingCanvasDocument = false; }
                return;
            }
            switchingCanvasDocument = true;
            try
            {
                CompleteZoomAnimation();
                CommitActiveTextEditor();
                CommitFloatingImagesToActiveLayer();
                layerThumbnailRefreshTimer.Stop();
                SyncActiveLayerFromCanvas();
                CaptureActiveCanvasDocument();
                FlushTabThumbnailBeforeSwitch();
                DetachCanvasPreview();
                MoveCanvasWorkspaceTo(next.Page);
                RestoreCanvasDocument(next);
            }
            finally
            {
                switchingCanvasDocument = false;
            }
            QueueDocumentCanvasLayout(next);
            QueueTabThumbnailRefresh();
        }

        private void CreateDocumentTab(Action initialize, string title)
        {
            if (savingCanvasDocument || isImageCurrentlyCreating || isResizing) return;
            CanvasDocument previous = activeCanvasDocument;
            if (previous != null)
            {
                CommitActiveTextEditor();
                CommitFloatingImagesToActiveLayer();
                CompleteZoomAnimation();
                SyncActiveLayerFromCanvas();
                CaptureActiveCanvasDocument();
                FlushTabThumbnailBeforeSwitch();
                DetachCanvasPreview();
            }

            var page = new TabPage(title);
            var document = new CanvasDocument { Page = page };
            switchingCanvasDocument = true;
            try
            {
                canvasDocuments.Add(page, document);
                tabControlWithCloseButtons1.TabPages.Add(page);
                ApplyDocumentTabTheme(page);
                // The document initializer uses the existing canvas controls,
                // but must have fresh stacks/layers before ResetLayers/ResetHistory.
                MoveCanvasWorkspaceTo(page);
                MainBitmap = null;
                SelectedBitmap = null;
                previewBitmap = null;
                editorLayers.Clear();
                untrackedLayerVisibility.Clear();
                untrackedLayerLocks.Clear();
                dirtyLayerThumbnailKeys.Clear();
                imageListLayerThumbnails.Images.Clear();
                undoStack = new Stack<LayerCanvasState>();
                redoStack = new Stack<LayerCanvasState>();
                activeLayerIndex = -1;
                magicSelectionMask = null;
                magicSelectionMaskWidth = magicSelectionMaskHeight = 0;
                magicSelectionBounds = Rectangle.Empty;
                magicSelectionOutlineSegments.Clear();
                SelectionRectangle = Rectangle.Empty;
                originalSelectionRectangleLocation = Point.Empty;
                originalSelectionRectangleSize = Size.Empty;
                isSelected = false;
                isModified = false;
                isSaved = false;
                fileSaved = false;
                blurEffect = BlurEffect.None;
                artisticFilters = ArtisticFilters.None;
                basicFilters = BasicFilters.None;
                zoom = 1f;
                activeCanvasDocument = document;
                initialize();
                CaptureActiveCanvasDocument();
                FlushTabThumbnailBeforeSwitch();
                tabControlWithCloseButtons1.SelectedTab = page;
                UpdateCurrentDocumentTitle();
                QueueDocumentCanvasLayout(document);
            }
            catch
            {
                // A corrupt file must not destroy the previous document.
                CaptureActiveCanvasDocument();
                DetachCanvasPreview();
                document.Dispose();
                canvasDocuments.Remove(page);
                imageListTabThumbnails.Images.RemoveByKey(document.TabThumbnailKey);
                // Keep UIPanel alive before its temporary TabPage is destroyed.
                if (previous != null)
                {
                    MoveCanvasWorkspaceTo(previous.Page);
                    RestoreCanvasDocument(previous);
                }
                else
                {
                    MoveCanvasWorkspaceTo(tabControlWithCloseButtons1.Parent, visible: false);
                    activeCanvasDocument = null;
                }
                tabControlWithCloseButtons1.TabPages.Remove(page);
                page.Dispose();
                if (previous != null)
                    tabControlWithCloseButtons1.SelectedTab = previous.Page;
                throw;
            }
            finally
            {
                switchingCanvasDocument = false;
            }
        }

        // Closing is handled by the existing tab control and the editor's
        // ControlRemoved listener. No changes to TabControlWithCloseButtons.

        private void CanvasDocumentTab_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || documentTabMenu == null) return;
            for (int i = 0; i < tabControlWithCloseButtons1.TabCount; i++)
            {
                if (tabControlWithCloseButtons1.GetTabRect(i).Contains(e.Location))
                {
                    contextDocumentTab = tabControlWithCloseButtons1.TabPages[i];
                    if (canvasDocuments.ContainsKey(contextDocumentTab))
                        documentTabMenu.Show(tabControlWithCloseButtons1, e.Location);
                    return;
                }
            }
        }

        private async Task CloseDocumentTab(TabPage page)
        {
            if (page == null || !canvasDocuments.ContainsKey(page) ||
                savingCanvasDocument || closeAllDocumentsInProgress) return;
            tabControlWithCloseButtons1.SelectedTab = page;
            if (tabControlWithCloseButtons1.SelectedTab != page) return;
            if (isModified)
            {
                DialogResult answer = MessageForm.Show(
                    "Save changes before closing this tab?", "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (answer == DialogResult.Cancel) return;
                if (answer == DialogResult.Yes && !await SaveAsync()) return;
            }
            // Removal is handled by ControlRemoved, including image disposal.
            if (canvasDocuments.ContainsKey(page))
            {
                approvedDocumentTabRemoval = true;
                try
                {
                    tabControlWithCloseButtons1.TabPages.Remove(page);
                    page.Dispose();
                }
                finally { approvedDocumentTabRemoval = false; }
            }
        }

        private void CanvasDocumentTab_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (switchingCanvasDocument || e.Control is not TabPage page ||
                !canvasDocuments.TryGetValue(page, out CanvasDocument document)) return;

            bool wasActive = ReferenceEquals(document, activeCanvasDocument);
            if (wasActive)
            {
                CommitActiveTextEditor();
                CompleteZoomAnimation();
                CommitFloatingImagesToActiveLayer();
                layerThumbnailRefreshTimer.Stop();
                SyncActiveLayerFromCanvas();
                CaptureActiveCanvasDocument();
                FlushTabThumbnailBeforeSwitch();
            }

            // The built-in X button may remove and Dispose the TabPage right
            // after this event. When cancellation is requested, reconstruct the
            // page rather than attaching a live UIPanel to a doomed control.
            if (!approvedDocumentTabRemoval && !closeConfirmed && document.Modified)
            {
                DialogResult answer = MessageForm.Show(
                    $"Close {DocumentTabCaption(document.DisplayName)} without saving?",
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.No)
                {
                    TabPage recovered = new TabPage(page.Text)
                    {
                        ImageKey = document.TabThumbnailKey
                    };
                    ApplyDocumentTabTheme(recovered);
                    switchingCanvasDocument = true;
                    try
                    {
                        canvasDocuments.Remove(page);
                        document.Page = recovered;
                        canvasDocuments.Add(recovered, document);
                        tabControlWithCloseButtons1.TabPages.Add(recovered);
                        if (CanvasWorkspaceIsIn(page))
                            MoveCanvasWorkspaceTo(recovered);
                        tabControlWithCloseButtons1.SelectedTab = recovered;
                    }
                    finally { switchingCanvasDocument = false; }
                    QueueDocumentTabReconciliation();
                    return;
                }
            }

            switchingCanvasDocument = true;
            try
            {
                // Critical: detach the entire workspace before a TabPage is
                // disposed, or WinForms would dispose the rulers and panel3 too.
                // The reconciliation callback moves it into the surviving tab.
                if (CanvasWorkspaceIsIn(page))
                    MoveCanvasWorkspaceTo(tabControlWithCloseButtons1.Parent, visible: false);

                if (wasActive)
                {
                    DetachCanvasPreview();
                    activeCanvasDocument = null;
                    MainBitmap = null;
                    SelectedBitmap = null;
                    previewBitmap = null;
                    editorLayers.Clear();
                    untrackedLayerVisibility.Clear();
                    untrackedLayerLocks.Clear();
                    dirtyLayerThumbnailKeys.Clear();
                    imageListLayerThumbnails.Images.Clear();
                    undoStack = new Stack<LayerCanvasState>();
                    redoStack = new Stack<LayerCanvasState>();
                    activeLayerIndex = -1;
                    isSelected = false;
                    magicSelectionMask = null;
                    magicSelectionMaskWidth = magicSelectionMaskHeight = 0;
                    magicSelectionBounds = Rectangle.Empty;
                    magicSelectionOutlineSegments.Clear();
                    SelectionRectangle = Rectangle.Empty;
                    originalSelectionRectangleLocation = Point.Empty;
                    originalSelectionRectangleSize = Size.Empty;
                    blurEffect = BlurEffect.None;
                    artisticFilters = ArtisticFilters.None;
                    basicFilters = BasicFilters.None;
                    openedFilePath = string.Empty;
                    labelFileName.Text = string.Empty;
                    labelSize.Text = string.Empty;
                    isModified = false;
                    isSaved = true;
                    fileSaved = false;
                    UpdateUndoRedoButtons();
                }
                canvasDocuments.Remove(page);
                imageListTabThumbnails.Images.RemoveByKey(document.TabThumbnailKey);
                tabControlWithCloseButtons1.Invalidate();
                document.Dispose();
            }
            finally { switchingCanvasDocument = false; }

            // At this point TabControl may still be recalculating SelectedTab.
            // It is now safe for it to dispose the old page; choose the next
            // document and fix viewport geometry once the message has ended.
            QueueDocumentTabReconciliation();
        }

        // Delayed layout work must not reposition whichever tab happens to be
        // selected by the time the WinForms message is finally processed.
        private void QueueCanvasCentering()
        {
            CanvasDocument requested = activeCanvasDocument;
            if (!IsHandleCreated) return;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || (requested != null &&
                    !ReferenceEquals(requested, activeCanvasDocument))) return;
                CenterCanvasPanel();
                CenterScrollbars();
            }));
        }

        private void createNewFile()
        {
            CommitActiveTextEditor();
            CancelZoomAnimation();
            listView1.Items.Clear();
            aiGenerated = false;
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            UIPanel.AutoScroll = true;

            canvasPanel.Size = new Size(Settings1.Default.DefaultCanvasSize.Width + 20,
                Settings1.Default.DefaultCanvasSize.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            MainBitmap = new Bitmap(Settings1.Default.DefaultCanvasSize.Width,
                Settings1.Default.DefaultCanvasSize.Height);
            originalSize = MainBitmap.Size;
            ResetLayers("Layer 1");
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = "Unnamed File";
            ResetHistory("New document");
            isSaved = false;
            openedFilePath = string.Empty;
            farklıKaydetToolStripMenuItem.Enabled = false;
            zoom = 1;
            labelZoom.Text = "100%";

            QueueCanvasCentering();
        }

        private void openAFile(string fileName)
        {
            CommitActiveTextEditor();
            CancelZoomAnimation();
            aiGenerated = false;
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            Bitmap image = SecurityGuard.LoadLocalImage(fileName);
            UIPanel.AutoScroll = true;

            canvasPanel.Size = new Size(image.Size.Width + 20, image.Size.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            pictureBoxCanvas.Image = image;
            MainBitmap = new Bitmap(image);
            originalSize = MainBitmap.Size;
            ResetLayers(Path.GetFileName(fileName));
            canvasPanel.Refresh();
            labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height}px";
            labelFileName.Text = fileName;
            ResetHistory("Opened image");
            isSaved = true;
            openedFilePath = fileName;
            farklıKaydetToolStripMenuItem.Enabled = true;
            zoom = 1;
            labelZoom.Text = "100%";

            QueueCanvasCentering();
        }

        private void createFileWithAIorWebcam(Image generatedImage, bool isAIGenerated)
        {
            CommitActiveTextEditor();
            CancelZoomAnimation();
            aiGenerated = isAIGenerated;
            isModified = false;
            SetAsUnselected();
            pictureBoxCanvas.Image = null;
            UIPanel.AutoScroll = true;

            canvasPanel.Size = new Size(generatedImage.Width + 20, generatedImage.Height + 20);
            pictureBoxCanvas.Size = panelResizer.Size;
            UIPanel.AutoScrollMinSize = canvasPanel.Size;

            pictureBoxCanvas.Image = generatedImage;
            MainBitmap = new Bitmap(generatedImage);
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
                    labelFileName.Text = "Webcam Image";
                    break;
            }
            ResetHistory(isAIGenerated ? "AI generated image" : "Webcam image");
            isSaved = false;
            openedFilePath = string.Empty;
            farklıKaydetToolStripMenuItem.Enabled = false;
            zoom = 1;
            labelZoom.Text = "100%";

            QueueCanvasCentering();
        }

        // WinForms uses ScrollWindowEx when moving child controls during scrolling.
        // Force a complete child repaint after the scroll finishes so old canvas
        // pixels and resize handles cannot remain as ghost strips.
        private bool canvasRepaintQueued;
        private bool updatingCanvasZoom;
        // Fixed for the whole gesture; never derive the anchor from a rounded
        // intermediate control position, which makes animated zoom jitter.
        private double zoomAnchorImageX, zoomAnchorImageY;
        private double zoomAnchorViewportX, zoomAnchorViewportY;


        // WinForms AutoScroll moves native child windows. Resizing and scrolling
        // those HWNDs every 16 ms flickers and causes resize handles to oscillate.
        // Animate a lightweight, double-buffered preview ABOVE the viewport, then
        // commit the actual zoom and scrollbar layout once at the final frame.
        private readonly System.Windows.Forms.Timer zoomAnimationTimer = new System.Windows.Forms.Timer();
        private readonly Stopwatch zoomAnimationClock = new Stopwatch();
        private ZoomAnimationOverlay zoomAnimationOverlay;
        private float zoomAnimationFrom;
        private float zoomAnimationTo;
        private float zoomAnimationShown;
        private const int ZoomAnimationDurationMs = 180;

        private sealed class ZoomAnimationOverlay : Control
        {
            private readonly ImageEditor editor;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public float DisplayZoom { get; set; }

            public ZoomAnimationOverlay(ImageEditor owner)
            {
                editor = owner;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                editor.PaintZoomAnimationFrame(e.Graphics, ClientSize, DisplayZoom);
            }

            // Rapid wheel gestures may arrive while the preview is still visible.
            // Retarget the animation rather than dropping the additional input.
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                editor.ImageEditor_MouseWheel(this, e);
            }
        }

        private void RequestSmoothZoom(float delta)
        {
            if (MainBitmap == null || Math.Abs(delta) < 0.00001f)
                return;
            CommitActiveTextEditor();

            // Accumulate wheel/button steps against the TARGET, not the currently
            // displayed frame; consecutive notches must not get lost.
            float baseZoom = zoomAnimationTimer.Enabled ? zoomAnimationTo : zoom;
            float target = Math.Max(0.05f, Math.Min(5f, baseZoom + delta));
            StartZoomAnimation(target);
        }

        private void StartZoomAnimation(float targetZoom, bool centerOnImage = false)
        {
            if (MainBitmap == null || IsDisposed || Disposing)
                return;

            // A resize or guide drag uses live picture-box coordinates.
            // Do not change zoom until either gesture has finished.
            if (isResizing || guideDragging)
                return;

            targetZoom = Math.Max(0.05f, Math.Min(5f, targetZoom));
            if (isdrawing)
            {
                // Editing gestures depend on the actual pictureBox coordinates.
                // Never put a preview in front of an active brush/resize drag.
                CancelZoomAnimation();
                CaptureZoomAnchor();
                zoom = targetZoom;
                if (centerOnImage)
                {
                    zoomAnchorImageX = MainBitmap.Width / 2.0;
                    zoomAnchorImageY = MainBitmap.Height / 2.0;
                }
                UpdatePictureBoxZoom(keepGestureAnchor: true);
                ScaleSelection();
                return;
            }

            bool running = zoomAnimationTimer.Enabled;
            if (!running)
            {
                CaptureZoomAnchor();
                zoomAnimationShown = zoom;
            }

            if (centerOnImage)
            {
                zoomAnchorImageX = MainBitmap.Width / 2.0;
                zoomAnchorImageY = MainBitmap.Height / 2.0;
            }

            if (Math.Abs(targetZoom - zoomAnimationShown) < 0.00001f)
            {
                if (running)
                {
                    zoomAnimationTo = targetZoom;
                    CompleteZoomAnimation();
                }
                else if (centerOnImage)
                    UpdatePictureBoxZoom(keepGestureAnchor: true);
                return;
            }

            zoomAnimationFrom = zoomAnimationShown;
            zoomAnimationTo = targetZoom;

            if (zoomAnimationOverlay == null || zoomAnimationOverlay.IsDisposed)
            {
                zoomAnimationOverlay = new ZoomAnimationOverlay(this);
                Controls.Add(zoomAnimationOverlay);
            }
            UpdateZoomAnimationOverlayBounds();
            zoomAnimationOverlay.DisplayZoom = zoomAnimationShown;
            zoomAnimationOverlay.Visible = true;
            zoomAnimationOverlay.BringToFront();
            zoomAnimationOverlay.Invalidate();
            zoomAnimationClock.Restart();
            zoomAnimationTimer.Start();
            UpdateRulers();
        }

        private void UpdateZoomAnimationOverlayBounds()
        {
            if (zoomAnimationOverlay == null || UIPanel.IsDisposed || !UIPanel.IsHandleCreated)
                return;

            // Use the FORM as the parent, not the AutoScroll panel: the latter
            // would move an overlay child when its own scrollbar position changes.
            Point topLeft = PointToClient(UIPanel.PointToScreen(Point.Empty));
            Rectangle bounds = new Rectangle(topLeft, UIPanel.ClientSize);
            if (zoomAnimationOverlay.Bounds != bounds)
                zoomAnimationOverlay.Bounds = bounds;
        }

        private void ZoomAnimationTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing || MainBitmap == null ||
                pictureBoxCanvas.Image == null)
            {
                CancelZoomAnimation();
                return;
            }

            double t = Math.Min(1.0,
                zoomAnimationClock.Elapsed.TotalMilliseconds / ZoomAnimationDurationMs);
            // Cubic ease-out: quick initial response and a gentle stop.
            double eased = 1.0 - Math.Pow(1.0 - t, 3);
            zoomAnimationShown = (float)(zoomAnimationFrom +
                (zoomAnimationTo - zoomAnimationFrom) * eased);

            UpdateZoomAnimationOverlayBounds();
            if (zoomAnimationOverlay != null && !zoomAnimationOverlay.IsDisposed)
            {
                zoomAnimationOverlay.DisplayZoom = zoomAnimationShown;
                zoomAnimationOverlay.Invalidate();
            }
            labelZoom.Text = $"{Math.Round(zoomAnimationShown * 100)}%";
            UpdateRulers();

            if (t >= 1.0)
                CompleteZoomAnimation();
        }

        private void CompleteZoomAnimation()
        {
            if (!zoomAnimationTimer.Enabled)
                return;

            float target = zoomAnimationTo;
            zoomAnimationTimer.Stop();
            zoomAnimationClock.Stop();
            zoom = target;
            // Layout once underneath the overlay, so only the completed result
            // becomes visible; the canvas and resize handles stay in sync.
            UpdatePictureBoxZoom(keepGestureAnchor: true);
            ScaleSelection();
            CancelZoomAnimation();
        }

        private void CancelZoomAnimation()
        {
            zoomAnimationTimer.Stop();
            zoomAnimationClock.Reset();
            if (zoomAnimationOverlay != null)
            {
                Controls.Remove(zoomAnimationOverlay);
                zoomAnimationOverlay.Dispose();
                zoomAnimationOverlay = null;
            }
            if (MainBitmap != null && !labelZoom.IsDisposed)
                labelZoom.Text = $"{Math.Round(zoom * 100)}%";
            UpdateRulers();
        }

        private void PaintZoomAnimationFrame(Graphics g, Size viewport, float animatedZoom)
        {
            g.Clear(UIPanel.BackColor);
            Image image = pictureBoxCanvas.Image;
            if (MainBitmap == null || image == null || viewport.Width < 1 || viewport.Height < 1)
                return;

            int imageWidth = Math.Max(1, (int)Math.Round(MainBitmap.Width * animatedZoom));
            int imageHeight = Math.Max(1, (int)Math.Round(MainBitmap.Height * animatedZoom));
            double scaleX = (double)imageWidth / MainBitmap.Width;
            double scaleY = (double)imageHeight / MainBitmap.Height;

            // The canvas has a 10px handle gutter on each side. On an axis that
            // fits, center the whole canvas; otherwise keep the original image
            // coordinate at the viewport center, within the scrollable bounds.
            double left = imageWidth + 20 <= viewport.Width
                ? (viewport.Width - imageWidth) / 2.0
                : Math.Max(viewport.Width - imageWidth - 10.0,
                    Math.Min(10.0, viewport.Width / 2.0 - zoomAnchorImageX * scaleX));
            double top = imageHeight + 20 <= viewport.Height
                ? (viewport.Height - imageHeight) / 2.0
                : Math.Max(viewport.Height - imageHeight - 10.0,
                    Math.Min(10.0, viewport.Height / 2.0 - zoomAnchorImageY * scaleY));

            int x = (int)Math.Round(left);
            int y = (int)Math.Round(top);
            Rectangle imageRect = new Rectangle(x, y, imageWidth, imageHeight);
            using (Brush gutter = new SolidBrush(canvasPanel.BackColor))
                g.FillRectangle(gutter, new Rectangle(x - 10, y - 10,
                    imageWidth + 20, imageHeight + 20));
            // Match the canvas transparency preview while zooming.
            if (toolStripButton9.Checked)
            {
                using (TextureBrush checkerboard = new TextureBrush(
                    Resources.transparent_pattern, WrapMode.Tile))
                    g.FillRectangle(checkerboard, imageRect);
            }
            else
            {
                using (Brush imageBackground = new SolidBrush(pictureBoxCanvas.BackColor))
                    g.FillRectangle(imageBackground, imageRect);
            }

            g.InterpolationMode = animatedZoom >= 1f
                ? InterpolationMode.NearestNeighbor
                : InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, imageRect,
                new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // The zoom overlay covers child controls. Render dropped images
            // into that preview too so they never disappear mid-animation.
            // Their 4px handle margin stays SCREEN-sized, not zoom-scaled.
            foreach (MovingImageControl floating in
                pictureBoxCanvas.Controls.OfType<MovingImageControl>().Reverse())
            {
                if (!floating.Visible || floating.IsDisposed || floating.DisplayImage == null)
                    continue;

                PointF logicalLocation = floating.UnzoomedImageLocation;
                SizeF logicalSize = floating.UnzoomedContentSize;
                Rectangle floatingImageRect = new Rectangle(
                    x + (int)Math.Round(logicalLocation.X * scaleX),
                    y + (int)Math.Round(logicalLocation.Y * scaleY),
                    Math.Max(1, (int)Math.Round(logicalSize.Width * scaleX)),
                    Math.Max(1, (int)Math.Round(logicalSize.Height * scaleY)));

                g.InterpolationMode = animatedZoom >= 1f
                    ? InterpolationMode.HighQualityBilinear
                    : InterpolationMode.HighQualityBicubic;
                g.DrawImage(floating.DisplayImage, floatingImageRect);
                Rectangle floatingFrame = Rectangle.Inflate(floatingImageRect, 4, 4);

                SmoothingMode oldSmoothing = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.None;
                using (Pen black = new Pen(Color.Black, 1f))
                using (Pen white = new Pen(Color.White, 1f))
                {
                    white.DashStyle = DashStyle.Custom;
                    white.DashPattern = new float[] { 3f, 3f };
                    white.DashOffset = marchingAntsOffset;
                    g.DrawRectangle(black, floatingFrame);
                    g.DrawRectangle(white, floatingFrame);
                }
                g.SmoothingMode = oldSmoothing;

                using (Brush gripFill = new SolidBrush(Color.White))
                using (Pen gripBorder = new Pen(Color.Black))
                {
                    int[] xx = { floatingFrame.Left, floatingFrame.Left + floatingFrame.Width / 2,
                 floatingFrame.Right };
                    int[] yy = { floatingFrame.Top, floatingFrame.Top + floatingFrame.Height / 2,
                 floatingFrame.Bottom };
                    for (int xi = 0; xi < 3; xi++)
                        for (int yi = 0; yi < 3; yi++)
                            if (xi != 1 || yi != 1)
                            {
                                Rectangle grip = new Rectangle(xx[xi] - 4, yy[yi] - 4, 8, 8);
                                g.FillRectangle(gripFill, grip);
                                g.DrawRectangle(gripBorder, grip);
                            }
                }
            }

            // Paint the floating selection in image-space coordinates, just like
            // pictureBoxCanvas_Paint does after the real layout is updated.
            if (isSelected && magicSelectionMask == null &&
                originalSelectionRectangleSize.Width > 0 && originalSelectionRectangleSize.Height > 0)
            {
                Rectangle selection = new Rectangle(
                    x + (int)Math.Round(originalSelectionRectangleLocation.X * scaleX),
                    y + (int)Math.Round(originalSelectionRectangleLocation.Y * scaleY),
                    Math.Max(1, (int)Math.Round(originalSelectionRectangleSize.Width * scaleX)),
                    Math.Max(1, (int)Math.Round(originalSelectionRectangleSize.Height * scaleY)));
                if (SelectedBitmap != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(SelectedBitmap, selection);
                }
                using (Pen outline = new Pen(Color.Black, 2f))
                using (Pen ants = new Pen(Color.White, 1.5f))
                {
                    ants.DashPattern = new float[] { 3f, 3f };
                    ants.DashOffset = marchingAntsOffset;
                    g.DrawRectangle(outline, selection);
                    g.DrawRectangle(ants, selection);
                }
            }
            else if (magicSelectionMask != null && magicSelectionOutlineSegments.Count > 0)
            {
                using (Pen outline = new Pen(Color.Black, 2f))
                using (Pen ants = new Pen(Color.White, 1.5f))
                {
                    ants.DashPattern = new float[] { 3f, 3f };
                    ants.DashOffset = marchingAntsOffset;
                    foreach (var segment in magicSelectionOutlineSegments)
                    {
                        PointF a = new PointF(x + (float)(segment.Start.X * scaleX),
                                              y + (float)(segment.Start.Y * scaleY));
                        PointF b = new PointF(x + (float)(segment.End.X * scaleX),
                                              y + (float)(segment.End.Y * scaleY));
                        g.DrawLine(outline, a, b);
                        g.DrawLine(ants, a, b);
                    }
                }
            }

            // Keep guide locations synchronized with the temporary zoom overlay.
            DrawRulerGuides(g, imageRect);

            // Preview resize handles so they do not disappear during the animation.
            Point[] handles = {
                new Point(x, y), new Point(x + imageWidth / 2, y),
                new Point(x + imageWidth, y), new Point(x, y + imageHeight / 2),
                new Point(x + imageWidth, y + imageHeight / 2),
                new Point(x, y + imageHeight), new Point(x + imageWidth / 2, y + imageHeight),
                new Point(x + imageWidth, y + imageHeight)
            };
            using (Brush handleFill = new SolidBrush(SystemColors.Window))
            using (Pen handleBorder = new Pen(SystemColors.WindowText))
            {
                foreach (Point handle in handles)
                {
                    Rectangle r = new Rectangle(handle.X - 3, handle.Y - 3, 7, 7);
                    g.FillRectangle(handleFill, r);
                    g.DrawRectangle(handleBorder, r);
                }
            }
        }

        private void CaptureZoomAnchor()
        {
            zoomAnchorViewportX = UIPanel.ClientSize.Width / 2.0;
            zoomAnchorViewportY = UIPanel.ClientSize.Height / 2.0;
            Point origin = UIPanel.PointToClient(pictureBoxCanvas.PointToScreen(Point.Empty));
            zoomAnchorImageX = (zoomAnchorViewportX - origin.X) /
                Math.Max(0.00001, (double)pictureBoxCanvas.Width / Math.Max(1, MainBitmap.Width));
            zoomAnchorImageY = (zoomAnchorViewportY - origin.Y) /
                Math.Max(0.00001, (double)pictureBoxCanvas.Height / Math.Max(1, MainBitmap.Height));
        }

        // Force Windows to redraw child HWNDs as well as the parent. Invalidate()
        // by itself does not clear stale pixels left by ScrollWindowEx on children.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hwnd, IntPtr updateRect,
            IntPtr updateRegion, uint flags);
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_UPDATENOW = 0x0100;


        private static void EnableControlDoubleBuffering(Control control)
        {
            // Panel.DoubleBuffered is protected; use its existing WinForms property.
            var property = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            property?.SetValue(control, true);
        }

        private void EnableCanvasDoubleBuffering()
        {
            // Do not buffer the scrolling parent: it has separate child HWNDs.
            // Buffering it can retain displaced child pixels when scrolling.
            EnableControlDoubleBuffering(canvasPanel);
            EnableControlDoubleBuffering(panelResizer);
            EnableControlDoubleBuffering(pictureBoxCanvas);
            // Do not invalidate UIPanel here: it has separate native child windows.
            // Their ordinary resize/scroll paint messages handle exposed regions.
        }

        private void QueueCanvasRepaint()
        {
            if (canvasRepaintQueued || !UIPanel.IsHandleCreated || UIPanel.IsDisposed)
                return;

            canvasRepaintQueued = true;
            // Defer only until the scroll message has completed. Avoid calling
            // Update() repeatedly on nested windows: doing so inside the scroll
            // blit/layout sequence can paint at two different canvas positions.
            UIPanel.BeginInvoke(new Action(() =>
            {
                canvasRepaintQueued = false;
                if (UIPanel.IsDisposed) return;
                if (UIPanel.IsHandleCreated)
                    RedrawWindow(UIPanel.Handle, IntPtr.Zero, IntPtr.Zero,
                        RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN);
            }));
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
            CommitActiveTextEditor();
            CommitFloatingImagesToActiveLayer();
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
            CommitActiveTextEditor();
            CommitFloatingImagesToActiveLayer();
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
            if (!CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                Logger.Log("No internet connection available.", Logger.LogTypes.Error);
                MessageForm.Show("Please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!HasAnySelection)
            {
                SystemSounds.Beep.Play(); // The eraser needs something selected.
                return;
            }

            try
            {
                isImageCurrentlyCreating = true;
                toolStripAICreateImage.Visible = false;
                canvasPanel.Enabled = false;
                menuStrip1.Enabled = false;
                toolStripTools.Enabled = false;
                toolStripSeparator29.Visible = true;
                labelAIImageErasing.Visible = true;
                progressBarAIImageCreation.Visible = true;

                await TryRunAiOnSelectionAsync(
                    "Remove the object or person in context of the image.", "AI generative erase");
            }
            catch (NullReferenceException)
            {
                Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
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

                    const string selectionPrompt = "Isolate the main object or person precisely and remove the entire background. Return the result as a PNG with a true transparent alpha channel (RGBA). All background pixels must have alpha 0. Do not replace the background with white, black, gray, any solid color, blur, studio backdrop, gradient, or checkerboard pattern. Preserve the subject’s original colors, edges, fine details, hair, and semi-transparent areas.";

                    if (!await TryRunAiOnSelectionAsync(selectionPrompt, "AI remove background"))
                    {
                        var image = await ImageGenerationCore.GenerateImage(
                            "Isolate the object or person and make the background transparent.",
                            MainBitmap, MainBitmap.Width, MainBitmap.Height, cts.Token);
                        if (image != null)
                        {
                            image = RemoveGeneratedCheckerboard(image);
                            ApplyGeneratedImageToCanvas(image, "AI remove background");
                        }
                        else
                        {
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
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

        private bool closeConfirmed;

        private async void kaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SaveAsync();
        }

        private async void farklıKaydetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SaveAsAsync();
        }

        // Save writes to the current file when there is one; otherwise it behaves like Save As.
        private Task<bool> SaveAsync() =>
            string.IsNullOrEmpty(openedFilePath) ? SaveAsAsync() : SaveFileAsync(openedFilePath);

        private async Task<bool> SaveAsAsync()
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return false;

            bool saved = await SaveFileAsync(saveFileDialog1.FileName);
            if (saved)
                farklıKaydetToolStripMenuItem.Enabled = true;
            return saved;
        }

        private async Task<bool> SaveFileAsync(string targetFilePath)
        {
            CommitActiveTextEditor();
            CommitFloatingImagesToActiveLayer();   // a floating image would otherwise be missing from the file
            SyncActiveLayerFromCanvas();
            RefreshActiveLayerThumbnail(force: true);
            fileSaved = false;
            savingCanvasDocument = true;

            string extension = Path.GetExtension(targetFilePath);
            string tempFilePath = Path.Combine(Path.GetDirectoryName(targetFilePath),
                "~" + Guid.NewGuid().ToString("N") + extension);

            bool previousModified = isModified, previousSaved = isSaved;
            labelSaving.Visible = true;
            progressBarSaving.Visible = true;
            toolStripSeparator13.Visible = true;
            progressBarSaving.Value = 0;

            try
            {
                ImageFormat format = ImageFormat.Png;
                string ext = extension.ToLowerInvariant();
                if (ext == ".jpg" || ext == ".jpeg") format = ImageFormat.Jpeg;
                else if (ext == ".bmp") format = ImageFormat.Bmp;

                // Flatten on the UI thread so edits can't race with the background write.
                using (Bitmap flattened = CreateCompositeBitmap())
                {
                    // Edits made while the file is being written set these flags again.
                    isModified = false;
                    isSaved = true;

                    await Task.Run(() =>
                    {
                        flattened.Save(tempFilePath, format);
                        SaveMetaDataToImage(tempFilePath);
                    });
                }

                if (File.Exists(targetFilePath))
                {
                    try { File.Replace(tempFilePath, targetFilePath, null, true); }
                    catch (IOException) { File.Copy(tempFilePath, targetFilePath, true); }
                }
                else
                {
                    File.Move(tempFilePath, targetFilePath);
                }

                openedFilePath = targetFilePath;
                labelFileName.Text = targetFilePath;
                fileSaved = true;
                UpdateCurrentDocumentTitle();
                Logger.Log($"File saved successfully: {targetFilePath}", Logger.LogTypes.Info);
                return true;
            }
            catch (Exception ex)
            {
                isModified = previousModified || isModified;
                isSaved = previousSaved && !isModified;
                fileSaved = false;
                Logger.Log($"An error occurred while saving the file: {ex.Message}", Logger.LogTypes.Error);
                MessageForm.Show($"An error occurred while saving the file: {ex.Message}",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                savingCanvasDocument = false;
                UpdateCurrentDocumentTitle();
                labelSaving.Visible = false;
                progressBarSaving.Visible = false;
                toolStripSeparator13.Visible = false;
                try { if (File.Exists(tempFilePath)) File.Delete(tempFilePath); } catch { }
            }
        }

        // Runs `action` once the user has chosen to save (and the save succeeded) or to discard.
        private async void AskForSavingOrCancel(Action action)
        {
            if (!isModified)
            {
                action();
                return;
            }

            DialogResult result = MessageForm.Show("Do you want to save changes to your image?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            switch (result)
            {
                case DialogResult.Yes:
                    if (await SaveAsync())
                        action();
                    break;
                case DialogResult.No:
                    action();
                    break;
            }
        }

        private async void AskForSavingWithRetry(Action action)
        {
            if (!isModified)
            {
                action();
                return;
            }

            DialogResult result = MessageForm.Show("Do you want to save changes to your image?",
                "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                action();
                return;
            }

            if (await SaveAsync())
            {
                action();
                return;
            }

            // The save failed or was cancelled: let the user choose again (this time with Cancel).
            AskForSavingOrCancel(action);
        }

        private void ImageEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closeConfirmed) return;
            CommitActiveTextEditor();
            if (canvasDocuments.Count == 0)
            {
                if (!isModified) return;
                e.Cancel = true;
                _ = SaveAndCloseAsync();
                return;
            }
            CaptureActiveCanvasDocument();
            if (!canvasDocuments.Values.Any(doc => doc.Modified)) return;
            e.Cancel = true;
            if (!closeAllDocumentsInProgress)
                _ = ConfirmAndCloseAllDocumentsAsync();
        }

        private async Task ConfirmAndCloseAllDocumentsAsync()
        {
            closeAllDocumentsInProgress = true;
            try
            {
                foreach (CanvasDocument doc in canvasDocuments.Values.ToArray())
                {
                    if (!doc.Modified) continue;
                    tabControlWithCloseButtons1.SelectedTab = doc.Page;
                    if (tabControlWithCloseButtons1.SelectedTab != doc.Page) return;
                    DialogResult answer = MessageForm.Show(
                        $"Save changes to {DocumentTabCaption(doc.DisplayName)}?",
                        "Unsaved Changes", MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);
                    if (answer == DialogResult.Cancel) return;
                    if (answer == DialogResult.Yes && !await SaveAsync()) return;
                }
                closeConfirmed = true;
                Close();
            }
            finally
            {
                closeAllDocumentsInProgress = false;
            }
        }

        private async Task SaveAndCloseAsync()
        {
            if (await SaveAsync())
            {
                closeConfirmed = true;
                Close();
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
        public static void MakeDraggable(Control control, Control movementBounds = null)
        {
            bool isMouseDown = false;
            bool hasMoved = false;
            Point dragCursorPoint = Point.Empty;
            Point dragControlPoint = Point.Empty;
            const int DragThreshold = 4; // Distinguish a click from a drag.

            MouseEventHandler onMouseDown = (sender, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                isMouseDown = true;
                hasMoved = false;
                dragCursorPoint = Cursor.Position;
                dragControlPoint = control.Location;
                control.BringToFront();

                if (control is SunnyTheCat cat)
                    cat.WasDragged = false;
            };

            MouseEventHandler onMouseMove = (sender, e) =>
            {
                if (!isMouseDown) return;

                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));

                if (!hasMoved &&
                    (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold))
                {
                    hasMoved = true;
                    if (control is SunnyTheCat cat)
                        cat.WasDragged = true;
                }

                if (!hasMoved) return;

                Point proposed = Point.Add(dragControlPoint, new Size(diff));

                if (movementBounds != null && control.Parent != null)
                {
                    // Convert the visible viewport from its own coordinate space
                    // into the mascot parent's coordinate space. This also works
                    // when the mascot and UIPanel are not direct parent/child controls.
                    Rectangle viewportInParent = control.Parent.RectangleToClient(
                        movementBounds.RectangleToScreen(movementBounds.ClientRectangle));

                    int minX = viewportInParent.Left;
                    int minY = viewportInParent.Top;
                    int maxX = Math.Max(minX, viewportInParent.Right - control.Width);
                    int maxY = Math.Max(minY, viewportInParent.Bottom - control.Height);

                    proposed.X = Math.Clamp(proposed.X, minX, maxX);
                    proposed.Y = Math.Clamp(proposed.Y, minY, maxY);
                }

                control.Location = proposed;
            };

            MouseEventHandler onMouseUp = (sender, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                isMouseDown = false;
                // Keep WasDragged until the next MouseDown, so a trailing Click
                // cannot trigger a meow or Happy animation.
            };

            AttachMouseEvents(control, onMouseDown, onMouseMove, onMouseUp);
        }

        private static void AttachMouseEvents(Control target, MouseEventHandler down, MouseEventHandler move, MouseEventHandler up)
        {
            target.MouseDown += down;
            target.MouseMove += move;
            target.MouseUp += up;

            foreach (Control child in target.Controls)
            {
                AttachMouseEvents(child, down, move, up);
            }
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

        // The resize preview is in form coordinates, not in the scrolling canvas:
        // the real controls and their Win32 scroll positions never change mid-drag.
        private sealed class ResizePreviewOverlay : Control
        {
            private readonly ImageEditor editor;

            public ResizePreviewOverlay(ImageEditor owner)
            {
                editor = owner;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
                TabStop = false;
            }

            // Don't intercept the ongoing handle drag or subsequent mouse input.
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x0084) // WM_NCHITTEST
                {
                    m.Result = new IntPtr(-1); // HTTRANSPARENT
                    return;
                }
                base.WndProc(ref m);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                editor.PaintResizePreview(e.Graphics, ClientSize);
            }
        }

        private void StartResizePreview(Control dragHandle)
        {
            resizeTargetWidth = resizeOriginalSize.Width;
            resizeTargetHeight = resizeOriginalSize.Height;
            resizePreviewWidth = resizeTargetWidth;
            resizePreviewHeight = resizeTargetHeight;
            resizePreviewLastMs = 0;

            if (resizePreviewOverlay == null || resizePreviewOverlay.IsDisposed)
            {
                resizePreviewOverlay = new ResizePreviewOverlay(this);
                Controls.Add(resizePreviewOverlay);
            }

            UpdateResizePreviewBounds();
            resizePreviewOverlay.Visible = true;
            resizePreviewOverlay.BringToFront();
            resizePreviewOverlay.Invalidate();
            resizePreviewClock.Restart();
            resizePreviewTimer.Start();

            // The real resize handle must continue receiving MouseMove and MouseUp,
            // even when the pointer moves beyond the old image rectangle.
            if (dragHandle != null)
                dragHandle.Capture = true;
        }

        private void UpdateResizePreviewBounds()
        {
            if (resizePreviewOverlay == null || resizePreviewOverlay.IsDisposed ||
                UIPanel.IsDisposed || !UIPanel.IsHandleCreated)
                return;

            Point topLeft = PointToClient(UIPanel.PointToScreen(Point.Empty));
            Rectangle desired = new Rectangle(topLeft, UIPanel.ClientSize);
            if (resizePreviewOverlay.Bounds != desired)
                resizePreviewOverlay.Bounds = desired;
        }

        private Size GetResizeSizeAtCursor(Point cursor)
        {
            // Screen coordinates are independent of the moving resize handles.
            int dx = (int)Math.Round((cursor.X - resizeStartCursor.X) / zoom);
            int dy = (int)Math.Round((cursor.Y - resizeStartCursor.Y) / zoom);
            int width = resizeOriginalSize.Width;
            int height = resizeOriginalSize.Height;

            if (ResizeAffectsLeft) width -= dx;
            else if (ResizeAffectsRight) width += dx;
            if (ResizeAffectsTop) height -= dy;
            else if (ResizeAffectsBottom) height += dy;

            return new Size(
                Math.Max(1, Math.Min(MaxCanvasDimension, width)),
                Math.Max(1, Math.Min(MaxCanvasDimension, height)));
        }

        private void ResizePreviewTimer_Tick(object sender, EventArgs e)
        {
            if (!isResizing || MainBitmap == null || resizeOriginalLayers == null ||
                IsDisposed || Disposing || resizePreviewOverlay == null ||
                resizePreviewOverlay.IsDisposed)
            {
                StopResizePreview();
                return;
            }

            // Exponential ease-out that follows the changing cursor target without
            // accumulating the lag of many queued UI timer ticks.
            double nowMs = resizePreviewClock.Elapsed.TotalMilliseconds;
            double dtMs = Math.Max(1.0, Math.Min(100.0, nowMs - resizePreviewLastMs));
            resizePreviewLastMs = nowMs;
            double alpha = 1.0 - Math.Exp(-dtMs / ResizePreviewResponseMs);
            resizePreviewWidth += (resizeTargetWidth - resizePreviewWidth) * alpha;
            resizePreviewHeight += (resizeTargetHeight - resizePreviewHeight) * alpha;

            if (Math.Abs(resizeTargetWidth - resizePreviewWidth) < 0.03)
                resizePreviewWidth = resizeTargetWidth;
            if (Math.Abs(resizeTargetHeight - resizePreviewHeight) < 0.03)
                resizePreviewHeight = resizeTargetHeight;

            UpdateResizePreviewBounds();
            resizePreviewOverlay.Invalidate();
        }

        private void PaintResizePreview(Graphics g, Size viewport)
        {
            g.Clear(UIPanel.BackColor);
            Image source = pictureBoxCanvas.Image ?? MainBitmap;
            if (source == null || resizePreviewOverlay == null ||
                viewport.Width < 1 || viewport.Height < 1)
                return;

            int imageW = Math.Max(1, (int)Math.Round(resizePreviewWidth * zoom));
            int imageH = Math.Max(1, (int)Math.Round(resizePreviewHeight * zoom));
            Point anchor = resizePreviewOverlay.PointToClient(resizeAnchorScreen);
            int imageX = anchor.X - (ResizeAffectsLeft ? imageW : 0);
            int imageY = anchor.Y - (ResizeAffectsTop ? imageH : 0);
            Rectangle imageRect = new Rectangle(imageX, imageY, imageW, imageH);
            Rectangle gutterRect = new Rectangle(imageX - 10, imageY - 10,
                imageW + 20, imageH + 20);

            using (Brush gutter = new SolidBrush(canvasPanel.BackColor))
                g.FillRectangle(gutter, gutterRect);

            // Newly expanded pixels have alpha 0 regardless of the display preference.
            // Do not change or override the user's transparency pattern toggle.
            if (toolStripButton9.Checked)
            {
                using (TextureBrush expansionPattern = new TextureBrush(
                    Resources.transparent_pattern, WrapMode.Tile))
                    g.FillRectangle(expansionPattern, imageRect);
            }
            else
            {
                using (Brush imageBackground = new SolidBrush(pictureBoxCanvas.BackColor))
                    g.FillRectangle(imageBackground, imageRect);
            }

            double xOffset = ResizeAffectsLeft
                ? resizePreviewWidth - resizeOriginalSize.Width : 0.0;
            double yOffset = ResizeAffectsTop
                ? resizePreviewHeight - resizeOriginalSize.Height : 0.0;
            Rectangle originalRect = new Rectangle(
                imageX + (int)Math.Round(xOffset * zoom),
                imageY + (int)Math.Round(yOffset * zoom),
                Math.Max(1, (int)Math.Round(resizeOriginalSize.Width * zoom)),
                Math.Max(1, (int)Math.Round(resizeOriginalSize.Height * zoom)));

            GraphicsState state = g.Save();
            try
            {
                g.SetClip(imageRect, CombineMode.Intersect);
                // Existing pixels are displayed over the current canvas background;
                // their alpha values are preserved in the stored bitmap.
                g.InterpolationMode = zoom >= 1f
                    ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, originalRect,
                    new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
            }
            finally
            {
                g.Restore(state);
            }

            // Draw the same eight resize-handle positions as the real canvas.
            Point[] handles = {
                new Point(imageX, imageY), new Point(imageX + imageW / 2, imageY),
                new Point(imageX + imageW, imageY),
                new Point(imageX, imageY + imageH / 2),
                new Point(imageX + imageW, imageY + imageH / 2),
                new Point(imageX, imageY + imageH),
                new Point(imageX + imageW / 2, imageY + imageH),
                new Point(imageX + imageW, imageY + imageH)
            };
            using (Brush fill = new SolidBrush(SystemColors.Window))
            using (Pen border = new Pen(SystemColors.WindowText))
            {
                foreach (Point point in handles)
                {
                    Rectangle box = new Rectangle(point.X - 3, point.Y - 3, 7, 7);
                    g.FillRectangle(fill, box);
                    g.DrawRectangle(border, box);
                }
            }
        }

        private void StopResizePreview()
        {
            resizePreviewTimer.Stop();
            resizePreviewClock.Reset();
            if (resizePreviewOverlay != null)
            {
                Controls.Remove(resizePreviewOverlay);
                resizePreviewOverlay.Dispose();
                resizePreviewOverlay = null;
            }
        }

        private void resize_MouseDown(object sender, MouseEventArgs e)
        {
            if (AnyLayerLocked) return;
            if (MainBitmap == null || e.Button != MouseButtons.Left || isResizing)
                return;

            CompleteZoomAnimation();

            if (sender == resize_top_left) resizeDirection = ResizeDirection.TopLeft;
            else if (sender == resize_top) resizeDirection = ResizeDirection.Top;
            else if (sender == resize_top_right) resizeDirection = ResizeDirection.TopRight;
            else if (sender == resize_left) resizeDirection = ResizeDirection.Left;
            else if (sender == resize_right) resizeDirection = ResizeDirection.Right;
            else if (sender == resize_bottom_left) resizeDirection = ResizeDirection.BottomLeft;
            else if (sender == resize_bottom) resizeDirection = ResizeDirection.Bottom;
            else if (sender == resize_bottom_right) resizeDirection = ResizeDirection.BottomRight;
            else return;

            SetAsUnselected();
            SyncActiveLayerFromCanvas();
            RenderCompositeToCanvas(force: true);

            // Keep every original layer unchanged during the animated drag.
            // Resizing back outward restores all pixels, including cropped ones.
            DisposeResizeSnapshot();
            resizeOriginalLayers = new List<Bitmap>();
            if (editorLayers.Count > 0)
            {
                foreach (EditorLayer layer in editorLayers)
                    resizeOriginalLayers.Add(new Bitmap(layer.Bitmap));
            }
            else
                resizeOriginalLayers.Add(new Bitmap(MainBitmap));

            resizeOriginalSize = MainBitmap.Size;
            resizeStartCursor = Cursor.Position;
            Point pic = pictureBoxCanvas.PointToScreen(Point.Empty);
            resizeAnchorScreen = new Point(
                ResizeAffectsLeft ? pic.X + pictureBoxCanvas.Width : pic.X,
                ResizeAffectsTop ? pic.Y + pictureBoxCanvas.Height : pic.Y);

            isResizing = true;
            toolStripResize.Visible = true;
            toolStripSeparator16.Visible = true;
            toolStripResize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";
            StartResizePreview(sender as Control);
        }

        private void resize_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || resizeOriginalLayers == null || MainBitmap == null)
                return;

            Size target = GetResizeSizeAtCursor(Cursor.Position);
            if (target.Width == resizeTargetWidth && target.Height == resizeTargetHeight)
                return;

            resizeTargetWidth = target.Width;
            resizeTargetHeight = target.Height;
            toolStripResize.Text = $"{target.Width} X {target.Height}px";
            // The timer eases the visual bounds toward this exact mouse target.
            // No bitmap resizing or scrollbar layout occurs on MouseMove.
            resizePreviewOverlay?.Invalidate();
        }

        private void resize_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isResizing)
                return;

            // A final pointer position may arrive without a matching MouseMove.
            Size finalSize = GetResizeSizeAtCursor(Cursor.Position);
            StopResizePreview();
            try
            {
                if (MainBitmap != null && resizeOriginalLayers != null && !AnyLayerLocked &&
                    finalSize != resizeOriginalSize)
                {
                    // Snapshot history only for actual edits. The animated
                    // preview itself never modifies the document or undo stack.
                    SaveStateForUndo(includeAllLayers: true, description: "Resize canvas");
                    int offsetX = ResizeAffectsLeft
                        ? finalSize.Width - resizeOriginalSize.Width : 0;
                    int offsetY = ResizeAffectsTop
                        ? finalSize.Height - resizeOriginalSize.Height : 0;
                    ApplyCanvasResize(finalSize.Width, finalSize.Height, offsetX, offsetY);

                    foreach (EditorLayer layer in editorLayers)
                        dirtyLayerThumbnailKeys.Add(layer.ThumbnailKey);
                    RefreshDirtyLayerThumbnails();
                    isModified = true;
                    isSaved = false;
                }
            }
            finally
            {
                isResizing = false;
                toolStripResize.Visible = false;
                toolStripSeparator16.Visible = false;
                DisposeResizeSnapshot();
            }

            if (MainBitmap == null)
                return;
            originalSize = MainBitmap.Size;
            labelSize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";
            CenterCanvasPanel();
            PositionZoomResizeHandles();
            RenderCompositeToCanvas(force: true);
            UpdateUndoRedoButtons();
        }

        private void ApplyCanvasResize(int newWidth, int newHeight, int offsetX, int offsetY)
        {
            if (AnyLayerLocked) return;
            int count = resizeOriginalLayers.Count;
            var rebuilt = new List<Bitmap>(count);

            for (int i = 0; i < count; i++)
            {
                Bitmap original = resizeOriginalLayers[i];
                var bmp = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Every newly exposed pixel must remain truly transparent.
                    // This applies to the bottom layer too, regardless of color2.
                    g.Clear(Color.Transparent);

                    // SourceCopy preserves existing pixels, including their alpha,
                    // without blending them with a background colour.
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(original,
                        new Rectangle(offsetX, offsetY, original.Width, original.Height),
                        0, 0, original.Width, original.Height, GraphicsUnit.Pixel);
                }
                rebuilt.Add(bmp);
            }

            // Swap in the new bitmaps.
            if (editorLayers.Count > 0)
            {
                for (int i = 0; i < editorLayers.Count; i++)
                {
                    editorLayers[i].Bitmap?.Dispose();
                    editorLayers[i].Bitmap = rebuilt[i];
                }
                MainBitmap?.Dispose();
                MainBitmap = new Bitmap(editorLayers[activeLayerIndex >= 0 ? activeLayerIndex : 0].Bitmap);
            }
            else
            {
                pictureBoxCanvas.Image = null; // don't leave a disposed bitmap on the control
                MainBitmap?.Dispose();
                MainBitmap = rebuilt[0];
            }

            originalSize = MainBitmap.Size;

            // Resize the nested controls without recentering during the drag.
            updatingCanvasZoom = true;
            try
            {
                SetZoomedCanvasBounds();

                // Keep the edge opposite to the dragged handle fixed on screen.
                Point pic = pictureBoxCanvas.PointToScreen(Point.Empty);
                int anchorX = ResizeAffectsLeft ? pic.X + pictureBoxCanvas.Width : pic.X;
                int anchorY = ResizeAffectsTop ? pic.Y + pictureBoxCanvas.Height : pic.Y;
                canvasPanel.Location = new Point(
                    canvasPanel.Left + (resizeAnchorScreen.X - anchorX),
                    canvasPanel.Top + (resizeAnchorScreen.Y - anchorY));
            }
            finally
            {
                updatingCanvasZoom = false;
            }

            PositionZoomResizeHandles();
            labelSize.Text = $"{MainBitmap.Width} X {MainBitmap.Height}px";
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            this.Invalidate();
            this.Update();
        }

        private void DisposeResizeSnapshot()
        {
            if (resizeOriginalLayers == null)
                return;
            foreach (Bitmap b in resizeOriginalLayers)
                b.Dispose();
            resizeOriginalLayers = null;
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
            if (FinishCanvasGuideDrag(e))
                return;

            if (IsActiveLayerLocked && isdrawing && !IsNonPaintingTool())
            {
                isdrawing = false;
                if (suppressCompositeDuringToolStroke) EndToolStrokePreview();
                StopCloneStampStroke();
                return;
            }
            if (e.Button == MouseButtons.Left)
                foreach (MovingImageControl floating in pictureBoxCanvas.Controls.OfType<MovingImageControl>())
                    if (floating.EndCanvasDrag())
                        return;
            if (lassoActive)
            {
                FinishLasso();
                isdrawing = false;
                return;
            }
            if (!isImageCurrentlyCreating)
            {
                if (isdrawing)
                {
                    // Finish the last segment before the rendering/selection merge.
                    if (selectedTool == Tools.CloneStamp && cloneStampStrokeSource != null)
                        drawIntoCanvas(e);

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
                                    DrawShapes.DrawLineOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Round:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRoundOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Rectangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRectangleOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.RoundedRectangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawRoundedRectangleOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, startPoint, endPointImage, radius);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Triangle:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawTriangleOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, startPoint, endPointImage);
                                }
                                isModified = true; MarkLayerThumbnailDirty();
                                break;
                            case Tools.Hexagon:
                                using (Graphics graphics = Graphics.FromImage(MainBitmap))
                                {
                                    DrawShapes.DrawHexagonOnCanvas(MainBitmap, pictureBoxCanvas, ActivePaintColor, shapeThickness, points, startPoint, endPointImage);
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

                            // A Clone Stamp transfers exact ARGB pixels, including
                            // transparency. SourceOver would leave old pixels behind.
                            if (selectedTool == Tools.CloneStamp)
                            {
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                                g.PixelOffsetMode = PixelOffsetMode.None;
                            }
                            else
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            }

                            // Dönüştürülmüş alanı ana resme çiz
                            g.DrawImage(SelectedBitmap, targetRect, sourceRect, GraphicsUnit.Pixel);
                        }

                        // Clear the SelectedBitmap
                        SelectedBitmap.Dispose();
                        SelectedBitmap = null;

                        // The selection was just committed after EndToolStrokePreview may
                        // already have rendered the old active-layer pixels. Force one final
                        // composite rebuild so the committed selection is immediately visible.
                        MarkCompositeDirty();
                        MarkLayerThumbnailDirty();
                    }

                    // Update the PictureBox with the new bitmap
                    RenderCompositeToCanvas();
                    pictureBoxCanvas.Invalidate();
                    this.Invalidate();
                    this.Update();
                    StopCloneStampStroke();
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
            if (UpdateCanvasGuideDrag(e))
                return;

            if (IsActiveLayerLocked && isdrawing && !IsNonPaintingTool())
            {
                isdrawing = false;
                StopCloneStampStroke();
                if (suppressCompositeDuringToolStroke) EndToolStrokePreview();
                return;
            }
            // Canvas captures the pointer while moving a floating PNG.
            // Do not run drawing/selection tools during that gesture.
            foreach (MovingImageControl floating in pictureBoxCanvas.Controls.OfType<MovingImageControl>())
                if (floating.ContinueCanvasDrag())
                    return;

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
                                int relativeStartX =
    previewStartPoint.X - originalSelectionRectangleLocation.X;

                                int relativeStartY =
                                    previewStartPoint.Y - originalSelectionRectangleLocation.Y;

                                int relativeEndX =
                                    (int)Math.Round(e.X / (double)zoom) -
                                    originalSelectionRectangleLocation.X;

                                int relativeEndY =
                                    (int)Math.Round(e.Y / (double)zoom) -
                                    originalSelectionRectangleLocation.Y;

                                // Use the same pixel dimensions as the source rectangle.
                                // Do not recalculate them from the displayed selection.
                                Bitmap newSelectedBitmap = new Bitmap(
                                    originalSelectionRectangleSize.Width,
                                    originalSelectionRectangleSize.Height);

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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
                                            {
                                                g.DrawLine(pen, relativeStartX, relativeStartY, relativeEndX, relativeEndY);
                                            }
                                            break;
                                        case Tools.Round:
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
                                            {
                                                g.DrawLine(pen, startX, startY, endX, endY);
                                            }
                                            break;
                                        case Tools.Round:
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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
                                            using (Pen pen = new Pen(ActivePaintColor, shapeThickness))
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

                                // The shape preview is composited in place of the active
                                // layer, using that layer's blend mode and opacity.
                                try { ShowActiveLayerPreview(newPreview); }
                                finally { newPreview.Dispose(); }
                            }
                            break;

                        case Tools.Selection:
                            {
                                if (e.Button != MouseButtons.Left || MainBitmap == null)
                                    return;

                                double scale = (double)zoom;

                                if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                                    return;

                                // Convert the selection's edges to image pixels once.
                                // Assumes the image is drawn at (0, 0), scaled by zoom.
                                int left = (int)Math.Max(0, Math.Min(
                                    MainBitmap.Width,
                                    Math.Floor(Math.Min(SelectionStartPoint.X, e.X) / scale)));

                                int top = (int)Math.Max(0, Math.Min(
                                    MainBitmap.Height,
                                    Math.Floor(Math.Min(SelectionStartPoint.Y, e.Y) / scale)));

                                int right = (int)Math.Max(0, Math.Min(
                                    MainBitmap.Width,
                                    Math.Ceiling(Math.Max(SelectionStartPoint.X, e.X) / scale)));

                                int bottom = (int)Math.Max(0, Math.Min(
                                    MainBitmap.Height,
                                    Math.Ceiling(Math.Max(SelectionStartPoint.Y, e.Y) / scale)));

                                if (SelectionStartPoint.X == e.X ||
                                    SelectionStartPoint.Y == e.Y ||
                                    right <= left || bottom <= top)
                                {
                                    SelectionRectangle = Rectangle.Empty;
                                    originalSelectionRectangleLocation = Point.Empty;
                                    originalSelectionRectangleSize = Size.Empty;
                                }
                                else
                                {
                                    // Authoritative bounds for cropping and applying the result.
                                    Rectangle imageRectangle =
                                        Rectangle.FromLTRB(left, top, right, bottom);

                                    originalSelectionRectangleLocation = imageRectangle.Location;
                                    originalSelectionRectangleSize = imageRectangle.Size;

                                    // These bounds are only for displaying the selection.
                                    SelectionRectangle = Rectangle.FromLTRB(
                                        (int)Math.Floor(left * scale),
                                        (int)Math.Floor(top * scale),
                                        (int)Math.Ceiling(right * scale),
                                        (int)Math.Ceiling(bottom * scale));
                                }

                                pictureBoxCanvas.Invalidate();
                                break;
                            }
                        case Tools.Lasso:
                            ExtendLasso(e);
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
            if (IsActiveLayerLocked && !IsNonPaintingTool()) return;
            if (selectedTool == Tools.CloneStamp)
            {
                if (isdrawing && cloneStampStrokeSource != null)
                {
                    Point destination = new Point(
                        (int)Math.Floor(e.X / zoom), (int)Math.Floor(e.Y / zoom));
                    Rectangle dirty = ContinueCloneStampStroke(destination);
                    if (!dirty.IsEmpty)
                    {
                        isModified = true;
                        isSaved = false;
                        MarkLayerThumbnailDirty();
                        if (HasRectSelection && SelectedBitmap != null)
                            pictureBoxCanvas.Invalidate(SelectionRectangle);
                        else
                            RefreshLiveStrokeRegion(dirty);
                    }
                }
                return;
            }
            // Preserve the previous image-space point before DrawBrush/FillGap updates x/y.
            // It is needed to refresh the complete segment touched by this mouse event.
            int previousStrokeX = x;
            int previousStrokeY = y;

            if (!isSelected && MainBitmap == null)
            {
                MainBitmap = new Bitmap(pictureBoxCanvas.Width, pictureBoxCanvas.Height);
            }
            else if (isSelected && SelectedBitmap == null)
            {
                if (SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
                {
                    SelectedBitmap = CreateSelectionWorkingBitmap();
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
                                        DrawBrush(graphics, BrushShapes.DrawCircleBrush, ActivePaintColor, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, ActivePaintColor, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, ActivePaintColor, brushSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
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
                                        DrawBrush(graphics, BrushShapes.DrawSquareBrush, ActivePaintColor, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, ActivePaintColor, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, ActivePaintColor, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
                                            (int)Math.Round((e.Y - SelectionRectangle.Y) / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, penSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
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
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, ActivePaintColor, sprayToolSize, new Point((int)Math.Round((e.X - SelectionRectangle.X) / zoom),
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
                                            DrawBrush(g, BrushShapes.DrawCircleBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 1:
                                            DrawBrush(g, BrushShapes.DrawOilBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 2:
                                            DrawBrush(g, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 3:
                                            DrawBrush(g, BrushShapes.DrawWatercolorBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                    }
                                    break;
                                case Tools.Pen:
                                    switch (comboBoxPenType.SelectedIndex)
                                    {
                                        case 0:
                                            DrawBrush(g, BrushShapes.DrawSquareBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 1:
                                            DrawBrush(g, BrushShapes.DrawMarkerBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 2:
                                            DrawBrush(g, BrushShapes.DrawCrayonBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                            isModified = true; MarkLayerThumbnailDirty();
                                            break;
                                        case 3:
                                            DrawBrush(g, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
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
                                    DrawBrush(g, BrushShapes.DrawSprayBrush, ActivePaintColor, sprayToolSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
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
                                        DrawBrush(graphics, BrushShapes.DrawCircleBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1: // Oil brush
                                        DrawBrush(graphics, BrushShapes.DrawOilBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2: // Calligraphy brush
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3:
                                        DrawBrush(graphics, BrushShapes.DrawWatercolorBrush, ActivePaintColor, brushSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
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
                                        DrawBrush(graphics, BrushShapes.DrawSquareBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 1:
                                        DrawBrush(graphics, BrushShapes.DrawMarkerBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 2:
                                        DrawBrush(graphics, BrushShapes.DrawCrayonBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
                                        isModified = true; MarkLayerThumbnailDirty();
                                        break;
                                    case 3: // Calligraphy pen
                                        DrawBrush(graphics, BrushShapes.DrawCalligraphyBrush, ActivePaintColor, penSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
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
                                DrawBrush(graphics, BrushShapes.DrawSprayBrush, ActivePaintColor, sprayToolSize, new Point((int)Math.Round(e.X / zoom), (int)Math.Round(e.Y / zoom)));
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

            // Freehand tools run on MouseMove. Never rebuild the complete layer stack
            // here while a stroke is active; update only the changed pixels instead.
            if (suppressCompositeDuringToolStroke && isdrawing && IsFreehandPaintTool(selectedTool))
            {
                if (isSelected && SelectedBitmap != null && magicSelectionMask == null)
                {
                    // Rectangular selections are painted as an overlay by the Paint event.
                    // Invalidating is enough; no layer composite changes until MouseUp.
                    pictureBoxCanvas.Invalidate(SelectionRectangle);
                }
                else
                {
                    RefreshLiveStrokeRegion(GetLiveStrokeDirtyRectangle(e, previousStrokeX, previousStrokeY));
                }
            }
            else
            {
                RenderCompositeToCanvas();
                pictureBoxCanvas.Invalidate();
            }
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
                // The PictureBox has already drawn its image in StretchImage mode.
                // Do NOT draw it here again. A second SourceOver DrawImage leaves
                // the PictureBox's original-size image visible through transparent
                // pixels, producing the apparent fixed-size picture behind the canvas.
                // This handler only draws selections, contours and tool cursors.
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
                DrawLassoInProgress(e.Graphics);
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
                    bool shouldShow = selectedTool == Tools.Brush || selectedTool == Tools.Pen ||
                                      selectedTool == Tools.Eraser || selectedTool == Tools.Spray ||
                                      selectedTool == Tools.CloneStamp;
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
                            case Tools.CloneStamp:
                                sizePixels = cloneStampSize;
                                styleIndex = 0;
                                break;
                        }

                        int displaySize = Math.Max(1, (int)Math.Round(sizePixels * zoom));
                        Point center = currentMousePosition;

                        // Use specialized preview renderers to better emulate brush/pen/spray appearance
                        if (selectedTool == Tools.Brush)
                        {
                            DrawBrushPreview(e.Graphics, center, displaySize, styleIndex, ActivePaintColor, zoom);
                        }
                        else if (selectedTool == Tools.Pen)
                        {
                            DrawPenPreview(e.Graphics, center, displaySize, styleIndex, ActivePaintColor, zoom);
                        }
                        else if (selectedTool == Tools.Eraser)
                        {
                            DrawEraserPreview(e.Graphics, center, displaySize, zoom);
                        }
                        else if (selectedTool == Tools.Spray)
                        {
                            DrawSprayPreview(e.Graphics, center, displaySize, ActivePaintColor, zoom);
                        }
                        else if (selectedTool == Tools.CloneStamp)
                        {
                            // A non-destructive cursor and an anchored source marker.
                            int radius = Math.Max(1, displaySize / 2);
                            using (Pen outline = new Pen(Color.White, 1.5f))
                            using (Pen shadow = new Pen(Color.Black, 3f))
                            {
                                e.Graphics.DrawEllipse(shadow, center.X - radius, center.Y - radius,
                                    radius * 2, radius * 2);
                                e.Graphics.DrawEllipse(outline, center.X - radius, center.Y - radius,
                                    radius * 2, radius * 2);
                            }
                            if (HasValidCloneStampSource)
                            {
                                Point source = new Point((int)Math.Round(cloneStampSourcePoint.X * zoom),
                                                         (int)Math.Round(cloneStampSourcePoint.Y * zoom));
                                using (Pen cross = new Pen(Color.Yellow, 1.5f))
                                {
                                    e.Graphics.DrawLine(cross, source.X - 6, source.Y, source.X + 6, source.Y);
                                    e.Graphics.DrawLine(cross, source.X, source.Y - 6, source.X, source.Y + 6);
                                }
                            }
                        }
                    }
                }
            }

            // Paint guidelines AFTER the image and selection overlays; they are
            // UI-only pixels and never modify the image/layers or exported files.
            DrawRulerGuides(e.Graphics,
                new Rectangle(Point.Empty, pictureBoxCanvas.ClientSize));
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

        // listViewHistory is a VIEW of the existing stacks, not a separate history.
        // Undo stack oldest-to-newest = applied actions; redo stack top-to-bottom =
        // actions available ahead of the current state. A row's Tag is its step index.
        private string historyBaseDescription = "New document";
        private bool suppressHistorySelectionChanged;
        private bool historyNavigationInProgress;

        private void InitializeHistoryPanel()
        {
            // Use the eleven icons already configured in the WinForms Designer.
            // Rows use their ImageIndex, not generated icons or number prefixes.
            listViewHistory.SmallImageList = imageListHistoryIcons;
            listViewHistory.LargeImageList = null;
            listViewHistory.View = View.Details;
            listViewHistory.HeaderStyle = ColumnHeaderStyle.None;
            listViewHistory.FullRowSelect = true;
            listViewHistory.MultiSelect = false;
            listViewHistory.HideSelection = false;
            listViewHistory.Columns.Clear();
            listViewHistory.Columns.Add("History");
            UpdateHistoryColumnWidth();

            listViewHistory.Resize -= listViewHistory_Resize;
            listViewHistory.Resize += listViewHistory_Resize;
            listViewHistory.SelectedIndexChanged -= listViewHistory_SelectedIndexChanged;
            listViewHistory.SelectedIndexChanged += listViewHistory_SelectedIndexChanged;
            UpdateHistoryList();
        }

        // Existing imageListHistoryIcons order (as shown in the WinForms Designer):
        //  0 brush, 1 pencil, 2 eraser, 3 bucket, 4 sprayer, 5 add text,
        //  6 diversity (SHAPES), 7 artificial intelligence, 8 file,
        //  9 filter, 10 cut. Do not create/append any icons at runtime.
        private const int HistoryBrushIcon = 0;
        private const int HistoryPencilIcon = 1;
        private const int HistoryEraserIcon = 2;
        private const int HistoryBucketIcon = 3;
        private const int HistorySprayIcon = 4;
        private const int HistoryTextIcon = 5;
        private const int HistoryShapesIcon = 6;
        private const int HistoryAiIcon = 7;
        private const int HistoryFileIcon = 8;
        private const int HistoryFilterIcon = 9;
        private const int HistoryCutIcon = 10;

        private int GetHistoryImageIndex(string description)
        {
            string action = (description ?? string.Empty).Trim().ToLowerInvariant();
            int index;

            // This order matters: e.g. 'Clear layer' is an erase action,
            // 'Cut selection' is a cut action, and 'AI or webcam image' is AI.
            if (action.Contains("ai ") || action.StartsWith("ai") ||
                action.Contains("generat") || action.Contains("background") ||
                action.Contains("restore"))
                index = HistoryAiIcon;
            else if (action.Contains("cut") || action.Contains("crop") ||
                     action.Contains("selection") || action.Contains("select"))
                index = HistoryCutIcon;
            else if (action.Contains("brush"))
                index = HistoryBrushIcon;
            else if (action == "pen" || action.StartsWith("pen ") ||
                     action.Contains("pencil"))
                index = HistoryPencilIcon;
            else if (action.Contains("eras") || action.Contains("clear") ||
                     action.StartsWith("delete selection"))
                index = HistoryEraserIcon;
            else if (action.Contains("bucket") || action == "fill" ||
                     action.StartsWith("fill "))
                index = HistoryBucketIcon;
            else if (action.Contains("spray"))
                index = HistorySprayIcon;
            else if (action.Contains("text"))
                index = HistoryTextIcon;
            else if (action == "line" || action.Contains("ellipse") ||
                     action.Contains("rectangle") || action.Contains("round") ||
                     action.Contains("triangle") || action.Contains("hexagon") ||
                     action.Contains("shape"))
                index = HistoryShapesIcon;   // icons8-diversity-48.png
            else if (action.Contains("filter") || action.Contains("blur") ||
                     action.Contains("pixelat") || action.Contains("grayscale") ||
                     action.Contains("black and white") || action.Contains("blend mode") ||
                     action.Contains("opacity") || action.Contains("mirror") ||
                     action.Contains("rotate") || action.Contains("flash") ||
                     action.Contains("transform"))
                index = HistoryFilterIcon;
            else
                index = HistoryFileIcon;     // new/open document, layers, general edits

            // Handle an incomplete Designer ImageList safely. Normally all 11 exist.
            int count = imageListHistoryIcons?.Images.Count ?? 0;
            if (count == 0) return -1;
            if (index < count) return index;
            return HistoryFileIcon < count ? HistoryFileIcon : 0;
        }

        private void listViewHistory_Resize(object sender, EventArgs e) =>
            UpdateHistoryColumnWidth();

        private void UpdateHistoryColumnWidth()
        {
            if (listViewHistory.Columns.Count > 0)
                listViewHistory.Columns[0].Width = Math.Max(40, listViewHistory.ClientSize.Width - 4);
        }

        private void UpdateHistoryList()
        {
            if (listViewHistory == null || listViewHistory.IsDisposed)
                return;

            int currentStep = undoStack.Count;
            // One list row for *every action* that SaveStateForUndo recorded,
            // including redoable actions. Do not create separate UI-only history
            // entries: clicking any row must remain consistent with Undo/Redo.
            // Stack<T> enumerates top-first; undo therefore needs reversing,
            // whereas redo is already ordered from the next action forward.
            var descriptions = new List<string>(1 + undoStack.Count + redoStack.Count)
            {
                historyBaseDescription
            };
            foreach (LayerCanvasState step in undoStack.Reverse())
                descriptions.Add(step.HistoryDescription);
            foreach (LayerCanvasState step in redoStack)
                descriptions.Add(step.HistoryDescription);

            suppressHistorySelectionChanged = true;
            listViewHistory.BeginUpdate();
            try
            {
                listViewHistory.Items.Clear();
                for (int i = 0; i < descriptions.Count; i++)
                {
                    // No numeric prefix: the image list supplies the leading glyph.
                    var item = new ListViewItem(descriptions[i])
                    {
                        Tag = i,
                        ImageIndex = GetHistoryImageIndex(descriptions[i])
                    };
                    // Rows above the current state are redoable, not discarded.
                    if (i > currentStep)
                        item.ForeColor = SystemColors.GrayText;
                    listViewHistory.Items.Add(item);
                }

                if (currentStep < listViewHistory.Items.Count)
                {
                    var selected = listViewHistory.Items[currentStep];
                    selected.Selected = true;
                    selected.Focused = true;
                    selected.EnsureVisible();
                }
            }
            finally
            {
                listViewHistory.EndUpdate();
                suppressHistorySelectionChanged = false;
            }
        }

        private void listViewHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressHistorySelectionChanged || historyNavigationInProgress ||
                listViewHistory.SelectedItems.Count == 0)
                return;

            if (listViewHistory.SelectedItems[0].Tag is not int targetStep)
                return;

            // Use the same Undo/Redo path as keyboard shortcuts. It restores all
            // affected layer bitmaps and properties, and keeps the stacks coherent.
            historyNavigationInProgress = true;
            try
            {
                while (undoStack.Count > targetStep && undoStack.Count > 0)
                    Undo();
                while (undoStack.Count < targetStep && redoStack.Count > 0)
                    Redo();
            }
            finally
            {
                historyNavigationInProgress = false;
                UpdateHistoryList();
            }
        }

        private void ResetHistory(string initialDescription)
        {
            foreach (LayerCanvasState state in undoStack)
                state.Dispose();
            foreach (LayerCanvasState state in redoStack)
                state.Dispose();
            undoStack.Clear();
            redoStack.Clear();
            historyBaseDescription = initialDescription;
            UpdateUndoRedoButtons();
        }

        private bool EnsureVisibleLayerForTool()
        {
            if (editorLayers.Count == 0)
                return MainBitmap != null;

            if (activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count &&
                editorLayers[activeLayerIndex].Visible)
                return !IsActiveLayerLocked || IsNonPaintingTool();

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

            return !IsActiveLayerLocked || IsNonPaintingTool();
        }

        private bool IsNonPaintingTool() => selectedTool == Tools.Cursor ||
            selectedTool == Tools.Selection || selectedTool == Tools.MagicSelection ||
            selectedTool == Tools.ColorDrop || selectedTool == Tools.Lasso;

        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            // When using the Cursor tool (or Alt), grabbing a guide must not
            // start drawing, commit floating images, or enter selection mode.
            if (TryBeginCanvasGuideDrag(e))
                return;

            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
                return;

            // v3: Floating PNG pixels are painted directly by the canvas.
            // The child control has only a frame/grip region, so interior
            // clicks reach this PictureBox. Route them BEFORE canvas tools or
            // the click-outside commit logic runs.
            MovingImageControl floatingHit = pictureBoxCanvas.Controls
                .OfType<MovingImageControl>()
                .FirstOrDefault(item => item.ContainsCanvasPoint(e.Location));
            if (floatingHit != null)
            {
                if (e.Button == MouseButtons.Right)
                    CommitFloatingImagesToActiveLayer();
                else
                    floatingHit.TryBeginCanvasDrag(e.Location);
                return;
            }

            // Clicking uncovered canvas space commits the floating object first,
            // so the chosen paint/selection tool operates on the merged pixels.
            if (e.Button == MouseButtons.Left)
                CommitFloatingImagesToActiveLayer();

            // Do not set isdrawing or call drawIntoCanvas for text. Creating it
            // on MouseDown (rather than Click) also works after a previous text
            // editor loses focus and is committed by the same mouse gesture.
            if (selectedTool == Tools.Text)
            {
                isdrawing = false;
                if (e.Button == MouseButtons.Left && !isImageCurrentlyCreating &&
                    EnsureVisibleLayerForTool())
                    BeginTextEdit(e.Location);
                return;
            }

            CommitActiveTextEditor();
            paintingMouseButton = e.Button;
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

                if (selectedTool == Tools.CloneStamp)
                {
                    Point point = new Point(
                        (int)Math.Floor(e.X / zoom), (int)Math.Floor(e.Y / zoom));
                    Rectangle canvasBounds = new Rectangle(Point.Empty, MainBitmap.Size);
                    isdrawing = false;
                    if (!canvasBounds.Contains(point))
                        return;

                    // Standard Photoshop/GIMP gesture: Alt+click samples the source.
                    if ((ModifierKeys & Keys.Alt) == Keys.Alt)
                    {
                        StopCloneStampStroke();
                        cloneStampSourcePoint = point;
                        cloneStampSourceLayerKey = CurrentCloneStampLayerKey;
                        cloneStampHasSource = true;
                        currentMousePosition = e.Location;
                        pictureBoxCanvas.Invalidate();
                        return;
                    }
                    if (e.Button != MouseButtons.Left)
                        return;
                    if (!HasValidCloneStampSource)
                    {
                        MessageForm.Show("Alt+click an image pixel to choose a Clone Stamp source, " +
                            "then click and drag to paint.", "Clone Stamp",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    CancelFilters();
                    if (HasRectSelection && SelectedBitmap == null)
                        SelectedBitmap = CreateSelectionWorkingBitmap();
                    // A separate bitmap is essential: cloning across overlapping
                    // regions must read the original pixels throughout the drag.
                    StopCloneStampStroke();
                    SaveStateForUndo(description: "Clone stamp");
                    EnsureCloneStampBitmapFormat();
                    cloneStampStrokeSource = CreateCloneStampSourceSnapshot();
                    cloneStampStrokeOffset = new Point(cloneStampSourcePoint.X - point.X,
                                                       cloneStampSourcePoint.Y - point.Y);
                    cloneStampPreviousPoint = null;
                    isdrawing = true;
                    BeginToolStrokePreview();
                    drawIntoCanvas(e);
                    return;
                }

                // Bucket fills exactly once on MouseDown. The Click event must not
                // save a second undo state or leave the cached composite unchanged.
                if (selectedTool == Tools.Bucket)
                {
                    isdrawing = false;
                    CancelFilters();
                    PerformBucketFill(new Point((int)(e.X / zoom), (int)(e.Y / zoom)),
                        ActivePaintColor);
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
                    case Tools.Lasso:
                        BeginLasso(e);
                        break;
                    default:
                        if (selectedTool == Tools.Brush || selectedTool == Tools.Pen || selectedTool == Tools.Eraser ||
                            selectedTool == Tools.Spray || selectedTool == Tools.Text)
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
            // The bucket is handled by MouseDown (where the mouse button, colour,
            // layer and image coordinates are reliable). Running it here again
            // creates an extra undo snapshot and may fail to refresh the composite.
            if (isImageCurrentlyCreating)
            {
                SystemSounds.Beep.Play();
                return;
            }
            CancelFilters();
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

        private void PerformBucketFill(Point imagePoint, Color replacementColor)
        {
            if (IsActiveLayerLocked) return;
            if (MainBitmap == null || imagePoint.X < 0 || imagePoint.Y < 0 ||
                imagePoint.X >= MainBitmap.Width || imagePoint.Y >= MainBitmap.Height)
                return;

            // Work on a private ARGB copy. Only create a history entry when the
            // fill actually changes pixels, and preserve the original layer data.
            using (Bitmap candidate = new Bitmap(MainBitmap.Width, MainBitmap.Height,
                       PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(candidate))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(MainBitmap, 0, 0);
                }

                // A rectangular selection may have a temporary painted bitmap.
                // Carry that content into the working copy before filling, or a
                // later deselection could paste old pixels over the bucket result.
                if (isSelected && SelectedBitmap != null && !SelectionRectangle.IsEmpty &&
                    originalSelectionRectangleSize.Width > 0 &&
                    originalSelectionRectangleSize.Height > 0)
                {
                    using (Graphics g = Graphics.FromImage(candidate))
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.DrawImage(SelectedBitmap,
                            new Rectangle(originalSelectionRectangleLocation,
                                originalSelectionRectangleSize),
                            new Rectangle(Point.Empty, SelectedBitmap.Size),
                            GraphicsUnit.Pixel);
                    }
                }

                if (!FloodFill(candidate, imagePoint, replacementColor, tolerance))
                    return;

                SaveStateForUndo(description: "Fill");
                Bitmap old = MainBitmap;
                MainBitmap = new Bitmap(candidate);
                old?.Dispose();
                if (isSelected && SelectedBitmap != null)
                {
                    SelectedBitmap.Dispose();
                    SelectedBitmap = null;
                }
                SyncActiveLayerFromCanvas();
                isModified = true;
                isSaved = false;
                MarkLayerThumbnailDirty();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                RefreshActiveLayerThumbnail();
            }
        }

        // Scanline flood-fill with 32-bit ARGB pixel access. Unlike SetPixel and
        // a queue entry for every pixel, it remains practical for large images.
        // The mask and rectangular selection, when present, constrain the fill.
        private bool FloodFill(Bitmap bmp, Point start, Color replacement, int toleranceValue)
        {
            Rectangle bounds = new Rectangle(Point.Empty, bmp.Size);
            if (isSelected && !SelectionRectangle.IsEmpty &&
                originalSelectionRectangleSize.Width > 0 &&
                originalSelectionRectangleSize.Height > 0)
            {
                bounds.Intersect(new Rectangle(originalSelectionRectangleLocation,
                    originalSelectionRectangleSize));
            }
            if (bounds.IsEmpty || !bounds.Contains(start))
                return false;

            bool hasMagicMask = isSelected && magicSelectionMask != null &&
                magicSelectionMaskWidth == bmp.Width &&
                magicSelectionMaskHeight == bmp.Height;
            int width = bmp.Width;
            int height = bmp.Height;
            if (hasMagicMask && !magicSelectionMask[start.Y * width + start.X])
                return false;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = checked(width * 4);
                byte[] rgba = new byte[checked(rowBytes * height)];
                for (int y = 0; y < height; y++)
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride),
                        rgba, y * rowBytes, rowBytes);

                int seedOffset = start.Y * rowBytes + start.X * 4;
                byte oldB = rgba[seedOffset];
                byte oldG = rgba[seedOffset + 1];
                byte oldR = rgba[seedOffset + 2];
                byte oldA = rgba[seedOffset + 3];
                if (oldB == replacement.B && oldG == replacement.G &&
                    oldR == replacement.R && oldA == replacement.A)
                    return false;

                int t = Math.Clamp(toleranceValue, 0, 100);
                double threshold = t * (510.0 / 100.0);
                double thresholdSquared = threshold * threshold;
                var visited = new BitArray(checked(width * height));
                var seeds = new Stack<Point>();
                seeds.Push(start);
                bool changed = false;

                bool Matches(int px, int py)
                {
                    if (px < bounds.Left || px >= bounds.Right ||
                        py < bounds.Top || py >= bounds.Bottom)
                        return false;
                    int idx = py * width + px;
                    if (visited[idx] || (hasMagicMask && !magicSelectionMask[idx]))
                        return false;
                    int offset = py * rowBytes + px * 4;
                    int da = rgba[offset + 3] - oldA;
                    // Transparent pixels have no meaningful RGB values.
                    if (oldA == 0 && rgba[offset + 3] == 0)
                        return true;
                    int db = rgba[offset] - oldB;
                    int dg = rgba[offset + 1] - oldG;
                    int dr = rgba[offset + 2] - oldR;
                    return (double)(da * da + dr * dr + dg * dg + db * db)
                        <= thresholdSquared;
                }

                while (seeds.Count > 0)
                {
                    Point seed = seeds.Pop();
                    if (!Matches(seed.X, seed.Y)) continue;
                    int left = seed.X, right = seed.X;
                    while (left > bounds.Left && Matches(left - 1, seed.Y)) left--;
                    while (right + 1 < bounds.Right && Matches(right + 1, seed.Y)) right++;
                    bool above = false, below = false;
                    for (int px = left; px <= right; px++)
                    {
                        int offset = seed.Y * rowBytes + px * 4;
                        visited[seed.Y * width + px] = true;
                        rgba[offset] = replacement.B;
                        rgba[offset + 1] = replacement.G;
                        rgba[offset + 2] = replacement.R;
                        rgba[offset + 3] = replacement.A;
                        changed = true;

                        bool a = Matches(px, seed.Y - 1);
                        if (a && !above) seeds.Push(new Point(px, seed.Y - 1));
                        above = a;
                        bool b = Matches(px, seed.Y + 1);
                        if (b && !below) seeds.Push(new Point(px, seed.Y + 1));
                        below = b;
                    }
                }

                if (changed)
                    for (int y = 0; y < height; y++)
                        Marshal.Copy(rgba, y * rowBytes,
                            IntPtr.Add(data.Scan0, y * data.Stride), rowBytes);
                return changed;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private void çıkışToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ImageEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            CancelActiveTextEditor();
            CancelZoomAnimation();
            StopResizePreview();
            layerThumbnailRefreshTimer.Stop();
            tabThumbnailRefreshTimer.Stop();
            tabThumbnailRefreshTimer.Tick -= TabThumbnailRefreshTimer_Tick;
            tabThumbnailRefreshTimer.Dispose();
            CaptureActiveCanvasDocument();
            pictureBoxCanvas.Image = null;
            compositeCanvasBitmap?.Dispose();
            compositeCanvasBitmap = null;
            // Inactive documents are not disposed automatically by the WinForms
            // control tree. Release every bitmap, layer and history snapshot.
            foreach (CanvasDocument doc in canvasDocuments.Values)
                doc.Dispose();
            canvasDocuments.Clear();
            activeCanvasDocument = null;
            documentTabMenu?.Dispose();
            CancelActiveTextEditor();
            CancelZoomAnimation();
            zoomAnimationTimer.Tick -= ZoomAnimationTimer_Tick;
            zoomAnimationTimer.Dispose();
            StopResizePreview();
            resizePreviewTimer.Tick -= ResizePreviewTimer_Tick;
            resizePreviewTimer.Dispose();
            DisposeResizeSnapshot();
            marchingAntsTimer.Stop();
            marchingAntsTimer.Tick -= MarchingAntsTimer_Tick;
            marchingAntsTimer.Dispose();
            Application.Exit();
        }

        private void açToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openAFile();
        }
        private void openAFile()
        {
            try
            {
                DialogResult dialogResult = openFileDialog1.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    string file = openFileDialog1.FileName;
                    CreateDocumentTab(() => openAFile(file), Path.GetFileName(file));
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
            CreateDocumentTab(createNewFile, "Unnamed File");
        }

        private void createWithAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateWithAIForm createWithAIForm = new CreateWithAIForm();
            createWithAIForm.ShowDialog();
            var generatedImage = createWithAIForm.GetGeneratedImage();
            if (generatedImage != null)
            {
                CreateDocumentTab(() => createFileWithAIorWebcam(generatedImage, true),
                    "AI Generated File");
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
            // A webcam capture is its own editable document, not a replacement.
            CreateDocumentTab(() => createFileWithAIorWebcam(e.AcceptedImage, false),
                "Webcam Image");
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

        private string DefaultHistoryDescription() => selectedTool switch
        {
            Tools.Brush => "Brush stroke",
            Tools.Pen => "Pen stroke",
            Tools.Eraser => "Erase",
            Tools.Spray => "Spray",
            Tools.CloneStamp => "Clone stamp",
            Tools.Bucket => "Fill",
            Tools.Line => "Line",
            Tools.Round => "Ellipse",
            Tools.Rectangle => "Rectangle",
            Tools.RoundedRectangle => "Rounded rectangle",
            Tools.Triangle => "Triangle",
            Tools.Hexagon => "Hexagon",
            Tools.Text => "Text",
            _ => "Edit image"
        };

        private void SaveStateForUndo(bool includeAllLayers = false, string description = null)
        {
            if (MainBitmap != null &&
                (includeAllLayers ||
                 (activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count)))
            {
                if (undoStack.Count >= MaxStackSize)
                {
                    var tempList = undoStack.Reverse().ToList();
                    if (tempList.Count > 0)
                    {
                        // Once the oldest undoable action is discarded, its resulting
                        // state becomes the new start of the visible timeline.
                        historyBaseDescription = tempList[0].HistoryDescription;
                        tempList[0].Dispose();
                        tempList.RemoveAt(0);
                    }
                    undoStack = new Stack<LayerCanvasState>(tempList);
                }

                string layerKey = activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count
                    ? editorLayers[activeLayerIndex].ThumbnailKey
                    : string.Empty;
                List<Bitmap> layerBitmaps = null;
                if (includeAllLayers)
                {
                    layerBitmaps = editorLayers.Count > 0
                        ? Enumerable.Range(0, editorLayers.Count)
                            .Select(i => new Bitmap(GetLayerBitmapForRender(i))).ToList()
                        : new List<Bitmap> { new Bitmap(MainBitmap) };
                }

                undoStack.Push(new LayerCanvasState(
                    layerKey,
                    new Bitmap(MainBitmap),
                    MainBitmap.Size,
                    layerBitmaps,
                    includeAllLayers ? CaptureLayerProperties() : null,
                    activeLayerIndex,
                    description ?? DefaultHistoryDescription()));

                ClearRedoStack();
            }

            UpdateUndoRedoButtons();
        }

        // When reconstructing a complete layer stack, honor eye-button changes
        // that were made outside the Undo/Redo timeline. Snapshot visibility is
        // used only for layers the user has not explicitly toggled.
        private bool GetLockForRestoredLayer(LayerPropertiesSnapshot properties)
        {
            if (properties != null &&
                untrackedLayerLocks.TryGetValue(properties.Key, out bool currentLock))
                return currentLock;
            return properties?.Locked ?? false;
        }

        private bool GetVisibilityForRestoredLayer(LayerPropertiesSnapshot properties)
        {
            if (properties != null &&
                untrackedLayerVisibility.TryGetValue(properties.Key, out bool currentVisibility))
                return currentVisibility;
            return properties?.Visible ?? true;
        }

        private void RestoreHistoryState(LayerCanvasState state, Stack<LayerCanvasState> oppositeStack)
        {
            if (state == null)
                return;

            if (state.LayerBitmaps != null)
            {
                int currentIndex = activeLayerIndex >= 0 && activeLayerIndex < editorLayers.Count
                    ? activeLayerIndex : 0;
                Bitmap fullCurrentTarget = new Bitmap(MainBitmap ??
                    (editorLayers.Count > 0 ? editorLayers[Math.Clamp(currentIndex, 0, editorLayers.Count - 1)].Bitmap : state.Bitmap));
                List<Bitmap> fullCurrentLayerBitmaps = Enumerable.Range(0, editorLayers.Count)
                    .Select(i => new Bitmap(GetLayerBitmapForRender(i))).ToList();

                oppositeStack.Push(new LayerCanvasState(
                    editorLayers.Count > 0 ? editorLayers[currentIndex].ThumbnailKey : string.Empty,
                    fullCurrentTarget,
                    fullCurrentTarget.Size,
                    fullCurrentLayerBitmaps,
                    CaptureLayerProperties(),
                    activeLayerIndex,
                    state.HistoryDescription));

                suppressLayerSelectionChanged = true;
                try
                {
                    foreach (var layer in editorLayers)
                        layer.Dispose();
                    editorLayers.Clear();
                    listView1.Items.Clear();
                    imageListLayerThumbnails.Images.Clear();
                    dirtyLayerThumbnailKeys.Clear();

                    for (int i = 0; i < state.LayerBitmaps.Count; i++)
                    {
                        LayerPropertiesSnapshot props = state.LayerProperties != null &&
                            i < state.LayerProperties.Count ? state.LayerProperties[i] : null;
                        string layerName = props?.Name ?? $"Layer {i + 1}";
                        var layer = new EditorLayer(layerName, new Bitmap(state.LayerBitmaps[i]), props?.Key)
                        {
                            Visible = GetVisibilityForRestoredLayer(props),
                            Locked = GetLockForRestoredLayer(props),
                            Opacity = props?.Opacity ?? 100,
                            BlendMode = props?.BlendMode ?? LayerBlendMode.Normal
                        };
                        editorLayers.Add(layer);

                        using (Bitmap thumbnail = CreateLayerThumbnail(layer.Bitmap))
                            imageListLayerThumbnails.Images.Add(layer.ThumbnailKey, new Bitmap(thumbnail));

                        var item = new ListViewItem(layer.Name)
                        {
                            ImageKey = layer.ThumbnailKey,
                            Tag = layer
                        };
                        listView1.Items.Add(item);
                        listView1.SetLayerVisibility(i, layer.Visible);
                        listView1.SetLayerLocked(i, layer.Locked);
                    }

                    activeLayerIndex = Math.Max(0, Math.Min(
                        state.ActiveLayerIndex >= 0 ? state.ActiveLayerIndex : currentIndex,
                        editorLayers.Count - 1));
                    if (editorLayers.Count > 0)
                    {
                        MainBitmap?.Dispose();
                        MainBitmap = new Bitmap(editorLayers[activeLayerIndex].Bitmap);
                        listView1.SelectedItems.Clear();
                        listView1.Items[activeLayerIndex].Selected = true;
                        listView1.Items[activeLayerIndex].Focused = true;
                        listView1.Items[activeLayerIndex].EnsureVisible();
                    }
                }
                finally
                {
                    suppressLayerSelectionChanged = false;
                }

                if (editorLayers.Count > 0)
                {
                    originalSize = MainBitmap.Size;
                    SetZoomedCanvasBounds();
                    UIPanel.AutoScrollMinSize = canvasPanel.Size;
                    labelSize.Text = $"{MainBitmap.Width} x {MainBitmap.Height} px";
                }

                RefreshDirtyLayerThumbnails();
                SetAsUnselected();
                CenterCanvasPanel();
                UpdateActiveLayerSettingsControls();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                pictureBoxCanvas.Update();
                state.Dispose();
                return;
            }

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
                currentTarget.Size,
                historyDescription: state.HistoryDescription));

            targetLayer.Bitmap?.Dispose();
            targetLayer.Bitmap = new Bitmap(state.Bitmap);

            if (targetIndex == activeLayerIndex)
            {
                MainBitmap?.Dispose();
                MainBitmap = new Bitmap(state.Bitmap);
                originalSize = MainBitmap.Size;

                // Call SetZoomedCanvasBounds() instead of manually setting partial sizes
                SetZoomedCanvasBounds();

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
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
            geriAlToolStripMenuItem.Enabled = undoStack.Count > 0;
            yineleToolStripMenuItem.Enabled = redoStack.Count > 0;
            UpdateHistoryList();
            UpdateCurrentDocumentTitle();
            QueueTabThumbnailRefresh();
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
                        pictureBoxCanvas.BackgroundImageLayout = ImageLayout.Tile;
                        break;
                    }
            }

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private bool committingFloatingImages;

        private void CommitFloatingImagesOnWorkspaceMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                CommitFloatingImagesToActiveLayer();
        }

        /// <summary>
        /// Flatten all floating MovingImageControl objects into the currently
        /// active bitmap, preserving their exact image-space positions and
        /// sizes (zoom and the 4px resize gutter are NOT written into pixels).
        /// One operation creates one undo snapshot for all floating images.
        /// </summary>
        private bool CommitFloatingImagesToActiveLayer()
        {
            if (IsActiveLayerLocked) return false;
            if (committingFloatingImages || MainBitmap == null ||
                activeLayerIndex < 0 || activeLayerIndex >= editorLayers.Count ||
                isImageCurrentlyCreating)
                return false;

            MovingImageControl[] floatingImages = pictureBoxCanvas.Controls
                .OfType<MovingImageControl>()
                .Where(item => !item.IsDisposed && item.DisplayImage != null)
                .Reverse() // back to front, matching WinForms child z-order
                .ToArray();
            if (floatingImages.Length == 0)
                return false;

            committingFloatingImages = true;
            try
            {
                CompleteZoomAnimation();
                foreach (MovingImageControl floating in floatingImages)
                    floating.CompleteInteraction();

                // Finalize any transformable selection before taking the undo
                // snapshot. Otherwise a selection can be painted over the new image.
                SetAsUnselected();
                SaveStateForUndo(description: "Commit floating image");

                using (Graphics g = Graphics.FromImage(MainBitmap))
                {
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.SetClip(new Rectangle(Point.Empty, MainBitmap.Size));

                    foreach (MovingImageControl floating in floatingImages)
                    {
                        PointF location = floating.UnzoomedImageLocation;
                        SizeF contentSize = floating.UnzoomedContentSize;
                        // RectangleF avoids discarding subpixel placement when an
                        // object is moved/resized under a non-integral zoom.
                        RectangleF destination = new RectangleF(
                            location.X, location.Y,
                            Math.Max(1f / zoom, contentSize.Width),
                            Math.Max(1f / zoom, contentSize.Height));
                        g.DrawImage(floating.DisplayImage, destination);
                    }
                }

                foreach (MovingImageControl floating in floatingImages)
                {
                    Image ownedImage = floating.DisplayImage;
                    pictureBoxCanvas.Controls.Remove(floating);
                    floating.Dispose();
                    // Images dropped from files are owned by these floating
                    // controls; PictureBox.Dispose does not release Image.
                    ownedImage?.Dispose();
                }

                SyncActiveLayerFromCanvas();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                MarkLayerThumbnailDirty();
                RefreshActiveLayerThumbnail();
                isModified = true;
                isSaved = false;
                pictureBoxCanvas.Invalidate();
                pictureBoxCanvas.Update();
                return true;
            }
            finally
            {
                committingFloatingImages = false;
            }
        }
        private void CancelFloatingImages()
        {
            foreach (MovingImageControl floating in
                pictureBoxCanvas.Controls.OfType<MovingImageControl>().ToArray())
            {
                floating.CompleteInteraction();
                Image owned = floating.DisplayImage;
                pictureBoxCanvas.Controls.Remove(floating);
                floating.Dispose();
                owned?.Dispose();
            }
            pictureBoxCanvas.Invalidate();
        }
        private void panel1_Click(object sender, EventArgs e)
        {
            CommitFloatingImagesToActiveLayer();
            SetAsUnselected();
            HideAddTextTextBoxes();
        }
        /// <summary>
        /// Opens a multiline text editor at the clicked *screen* position. Only
        /// CommitActiveTextEditor writes to the document (and creates an undo step).
        /// </summary>
        private void BeginTextEdit(Point displayPoint)
        {
            if (IsActiveLayerLocked) return;
            CommitActiveTextEditor();
            if (MainBitmap == null || isImageCurrentlyCreating)
                return;

            // Selections can keep a pending SelectedBitmap. Finalize that first
            // so text is drawn to the real active layer, not to a disposable copy.
            if (isSelected)
            {
                SetAsUnselected();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
            }

            if (displayPoint.X < 0 || displayPoint.Y < 0 ||
                displayPoint.X >= pictureBoxCanvas.ClientSize.Width ||
                displayPoint.Y >= pictureBoxCanvas.ClientSize.Height)
                return;

            string fontName = fontsComboBox.Text;
            float pointSize = Math.Max(1f, Math.Min(1000f, textSize));
            Font documentFont;
            try
            {
                documentFont = new Font(fontName, pointSize, fontStyle, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                documentFont = new Font(FontFamily.GenericSansSerif, pointSize,
                    FontStyle.Regular, GraphicsUnit.Point);
            }
            activeTextDocumentFont = documentFont;
            activeTextForeground = color1;
            activeTextBackground = color2;
            activeTextFillBackground = backgroundFilling;

            // WinForms text controls are not alpha-transparent. The light backing
            // exists *only* during editing and is never painted into the image.
            int left = Math.Max(0, displayPoint.X);
            int top = Math.Max(0, displayPoint.Y);
            int availableWidth = Math.Max(1, pictureBoxCanvas.ClientSize.Width - left);
            int width = Math.Min(Math.Max(70, (int)Math.Round(260 * zoom)), availableWidth);
            int availableHeight = Math.Max(1, pictureBoxCanvas.ClientSize.Height - top);
            int height = Math.Min(Math.Max(28, (int)Math.Ceiling(pointSize * zoom * 1.8f)), availableHeight);
            activeTextImageLocation = new PointF(left / zoom, top / zoom);

            var editor = new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = false,
                WordWrap = true,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.None,
                Location = new Point(left, top),
                Size = new Size(width, height),
                ForeColor = activeTextForeground,
                BackColor = activeTextFillBackground ? activeTextBackground : SystemColors.Window,
                Font = new Font(documentFont.FontFamily,
                    Math.Max(1f, pointSize * zoom), documentFont.Style, GraphicsUnit.Point),
                TextAlign = textToolAlign == TextToolAlign.Middle ? HorizontalAlignment.Center :
                    textToolAlign == TextToolAlign.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            activeTextEditor = editor;
            editor.TextChanged += ActiveTextEditor_TextChanged;
            editor.LostFocus += ActiveTextEditor_LostFocus;
            editor.KeyDown += ActiveTextEditor_KeyDown;
            pictureBoxCanvas.Controls.Add(editor);
            editor.BringToFront();
            editor.Focus();
        }

        private void ActiveTextEditor_TextChanged(object sender, EventArgs e)
        {
            TextBox editor = activeTextEditor;
            if (editor == null || editor.IsDisposed)
                return;
            // Keep the editor's width fixed so wrapping is predictable. The
            // previous code had three conflicting TextChanged resize handlers.
            int maxHeight = Math.Max(1, pictureBoxCanvas.ClientSize.Height - editor.Top);
            Size measured = TextRenderer.MeasureText(
                string.IsNullOrEmpty(editor.Text) ? "Ag" : editor.Text + " ",
                editor.Font,
                new Size(Math.Max(1, editor.ClientSize.Width - 4), 16000),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            editor.Height = Math.Min(maxHeight, Math.Max(
                (int)Math.Ceiling(editor.Font.Height * 1.5), measured.Height + 8));
        }

        private void ActiveTextEditor_LostFocus(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, activeTextEditor))
                CommitActiveTextEditor();
        }

        private void ActiveTextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelActiveTextEditor();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter && e.Control)
            {
                CommitActiveTextEditor();
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>Flatten the edited text into the selected editable layer.</summary>
        private void CommitActiveTextEditor()
        {
            if (activeTextEditor == null || completingTextEditor)
                return;

            completingTextEditor = true;
            TextBox editor = activeTextEditor;
            activeTextEditor = null; // Prevent reentry via LostFocus/Dispose.
            try
            {
                editor.LostFocus -= ActiveTextEditor_LostFocus;
                editor.TextChanged -= ActiveTextEditor_TextChanged;
                editor.KeyDown -= ActiveTextEditor_KeyDown;

                string content = editor.Text;
                if (!string.IsNullOrWhiteSpace(content) && MainBitmap != null && !IsActiveLayerLocked)
                {
                    SaveStateForUndo(description: "Text edit"); // Exactly one undo step per committed text.
                    float contentWidth = Math.Max(1f, editor.ClientSize.Width / zoom);
                    float contentHeight = Math.Max(1f, editor.ClientSize.Height / zoom);
                    RectangleF layout = new RectangleF(
                        activeTextImageLocation.X, activeTextImageLocation.Y,
                        contentWidth, contentHeight);
                    using (Graphics graphics = Graphics.FromImage(MainBitmap))
                    using (StringFormat format = new StringFormat(StringFormat.GenericDefault))
                    using (SolidBrush foreground = new SolidBrush(activeTextForeground))
                    {
                        graphics.CompositingMode = CompositingMode.SourceOver;
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        graphics.SetClip(new Rectangle(Point.Empty, MainBitmap.Size));
                        format.Alignment = editor.TextAlign == HorizontalAlignment.Center
                            ? StringAlignment.Center : editor.TextAlign == HorizontalAlignment.Right
                            ? StringAlignment.Far : StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Near;
                        if (activeTextFillBackground)
                        {
                            using (SolidBrush background = new SolidBrush(activeTextBackground))
                                graphics.FillRectangle(background, layout);
                        }
                        graphics.DrawString(content, activeTextDocumentFont,
                            foreground, layout, format);
                    }
                    isModified = true;
                    isSaved = false;
                    MarkCompositeDirty();
                    RenderCompositeToCanvas(force: true);
                    MarkLayerThumbnailDirty();
                    pictureBoxCanvas.Invalidate();
                }
            }
            finally
            {
                if (editor.Parent != null)
                    editor.Parent.Controls.Remove(editor);
                editor.Dispose();
                activeTextDocumentFont?.Dispose();
                activeTextDocumentFont = null;
                completingTextEditor = false;
            }
        }

        private void CancelActiveTextEditor()
        {
            if (activeTextEditor == null || completingTextEditor)
                return;
            completingTextEditor = true;
            TextBox editor = activeTextEditor;
            activeTextEditor = null;
            try
            {
                editor.LostFocus -= ActiveTextEditor_LostFocus;
                editor.TextChanged -= ActiveTextEditor_TextChanged;
                editor.KeyDown -= ActiveTextEditor_KeyDown;
                if (editor.Parent != null)
                    editor.Parent.Controls.Remove(editor);
                editor.Dispose();
            }
            finally
            {
                activeTextDocumentFont?.Dispose();
                activeTextDocumentFont = null;
                completingTextEditor = false;
            }
        }

        private void HideAddTextTextBoxes()
        {
            // The old implementation silently removed typed text without
            // drawing it into MainBitmap. All hide paths must finalize first.
            CommitActiveTextEditor();
        }

        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void canvasPanel_Click(object sender, EventArgs e)
        {
            CommitFloatingImagesToActiveLayer();
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
                if (isSelected && SelectedBitmap != null && !SelectionRectangle.IsEmpty && !IsActiveLayerLocked)
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

                if (IsActiveLayerLocked && SelectedBitmap != null)
                {
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
                CancelFilters();   // a filter preview belongs to the selection that was just removed
                RenderCompositeToCanvas();
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
            if (IsActiveLayerLocked) return;
            SetAsUnselected();   // a mirror always flips the whole layer
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.MirrorEffect(b), "Mirror");
        }

        private void flashToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.Flash(b), "Flash filter");

        private void frozenToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.Frozen(b), "Frozen filter");

        private void winterToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.Winter(b), "Winter filter");

        private void blackAndWhiteToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.BlackAndWhite(b), "Black and white");

        private void oldPictureToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.OldImage(b), "Old photo filter");

        private void cherryToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.CherryFilter(b), "Cherry filter");

        private void lightAddToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.LightAdd(b), "Light Add filter");

        private void purpleToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.PurpleEffect(b), "Purple filter");

        private void fogToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.BasicFilters.FogEffect(b), "Fog filter");

        private void chloeToolStripMenuItem_Click(object sender, EventArgs e) =>
            ApplyFilterToSelectionOrLayer(b => Filters.AmbientFilters.Chloe(b), "Chloe filter");

        private void ApplyGrayscaleQuick()
        {
            ApplyFilterToSelectionOrLayer(source =>
            {
                var output = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(output))
                using (var attrs = new ImageAttributes())
                {
                    attrs.SetColorMatrix(new ColorMatrix(new float[][] {
                new float[] {.299f,.299f,.299f,0,0}, new float[] {.587f,.587f,.587f,0,0},
                new float[] {.114f,.114f,.114f,0,0}, new float[] {0,0,0,1,0}, new float[] {0,0,0,0,1}}));
                    g.DrawImage(source, new Rectangle(0, 0, output.Width, output.Height),
                        0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attrs);
                }
                return output;
            }, "Grayscale");
        }
        private void ApplyFilterToSelectionOrLayer(Func<Bitmap, Bitmap> filter, string description)
        {
            if (MainBitmap == null || IsActiveLayerLocked) return;

            CancelFilters();                   // drop any open blur/pixelate/artistic preview
            FlushPendingSelectionBitmap();

            try
            {
                if (HasAnySelection)
                {
                    Rectangle bounds = GetSelectionImageBounds();
                    if (bounds.Width < 1 || bounds.Height < 1) { SystemSounds.Beep.Play(); return; }

                    Bitmap input = MainBitmap.Clone(bounds, PixelFormat.Format32bppArgb);
                    Bitmap result = null;
                    try
                    {
                        result = filter(input);
                        ApplyGeneratedSelectionToCanvas(result, description);   // writes through the mask/rectangle
                    }
                    finally
                    {
                        if (result != null && !ReferenceEquals(result, input)) result.Dispose();
                        input.Dispose();
                    }
                    return;
                }

                // No selection: filter a copy so MainBitmap is untouched until the filter succeeds.
                Bitmap source = new Bitmap(MainBitmap);
                Bitmap filtered;
                try { filtered = filter(source); }
                catch { source.Dispose(); throw; }
                if (!ReferenceEquals(filtered, source)) source.Dispose();

                SaveStateForUndo(description: description);
                Bitmap previous = MainBitmap;
                MainBitmap = filtered;
                previous.Dispose();

                SyncActiveLayerFromCanvas();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                pictureBoxCanvas.Update();
                isModified = true;
                isSaved = false;
                MarkLayerThumbnailDirty();
                RefreshActiveLayerThumbnail(force: true);
            }
            catch (Exception ex)
            {
                Logger.Log("Filter error: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Unable to apply the filter: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // A full-size copy of the layer with `filter` applied through the current selection
        // (or to the whole layer when nothing is selected).
        private Bitmap BuildFilteredLayer(Func<Bitmap, Bitmap> filter)
        {
            Bitmap layer = new Bitmap(MainBitmap);
            try
            {
                if (!HasAnySelection)
                {
                    Bitmap filtered = filter(layer);
                    if (!ReferenceEquals(filtered, layer)) layer.Dispose();
                    return filtered;
                }

                Rectangle bounds = GetSelectionImageBounds();
                if (bounds.Width < 1 || bounds.Height < 1) return layer;

                Bitmap input = MainBitmap.Clone(bounds, PixelFormat.Format32bppArgb);
                Bitmap result = null;
                try
                {
                    result = filter(input);
                    WriteImageThroughSelection(layer, result);
                }
                finally
                {
                    if (result != null && !ReferenceEquals(result, input)) result.Dispose();
                    input.Dispose();
                }
                return layer;
            }
            catch
            {
                layer.Dispose();
                throw;
            }
        }

        private Func<Bitmap, Bitmap> GetActivePreviewFilter()
        {
            switch (artisticFilters)
            {
                case ArtisticFilters.OilPainting:
                    return b => Filters.ArtisticFilters.OilPaintFilter(b,
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity,
                        FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize,
                        (byte)FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold);
                case ArtisticFilters.Cartoon:
                    return b => Filters.ArtisticFilters.CartoonFilter(b,
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity,
                        FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize,
                        (byte)FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold);
            }

            switch (blurEffect)
            {
                case BlurEffect.Pixelate:
                    return b => Filters.BlurringFilters.Pixelate(b,
                        pixelationSize, pixelationOffsetX, pixelationOffsetY);
                case BlurEffect.Gaussian:
                    return b => Filters.BlurringFilters.GaussianBlur(b, gaussianBlurSize);
            }
            return null;
        }

        private void DisposePreviewBitmap()
        {
            Bitmap old = previewBitmap;
            previewBitmap = null;
            if (old != null && !ReferenceEquals(old, MainBitmap))
                old.Dispose();
        }

        // Replaces the old RefreshFiltersPreview.
        private void RefreshFiltersPreview()
        {
            if (MainBitmap == null) return;

            Func<Bitmap, Bitmap> filter = GetActivePreviewFilter();
            if (filter == null)
            {
                if (previewBitmap != null) CancelFilters();
                return;
            }

            try
            {
                FlushPendingSelectionBitmap();
                Bitmap next = BuildFilteredLayer(filter);
                DisposePreviewBitmap();
                previewBitmap = next;
                ShowActiveLayerPreview(previewBitmap);
            }
            catch (Exception ex)
            {
                Logger.Log("Filter preview error: " + ex.Message, Logger.LogTypes.Error);
                CancelFilters();
                MessageForm.Show("Unable to preview the filter: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Replaces the old CancelFilters. Also resets the filter mode and hides the toolbars.
        private void CancelFilters()
        {
            bool hadPreview = previewBitmap != null ||
                artisticFilters != ArtisticFilters.None || blurEffect != BlurEffect.None;

            artisticFilters = ArtisticFilters.None;
            blurEffect = BlurEffect.None;
            toolStripArtisticFilters.Visible = false;
            toolStripPixelate.Visible = false;
            toolStripGaussianBlur.Visible = false;
            DisposePreviewBitmap();

            if (!hadPreview || MainBitmap == null) return;
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
        }

        private void BeginFilterPreview(ArtisticFilters artistic, BlurEffect blur)
        {
            if (MainBitmap == null) return;
            if (IsActiveLayerLocked) { SystemSounds.Beep.Play(); return; }

            artisticFilters = artistic;
            blurEffect = blur;
            toolStripArtisticFilters.Visible = artistic != ArtisticFilters.None;
            toolStripPixelate.Visible = blur == BlurEffect.Pixelate;
            toolStripGaussianBlur.Visible = blur == BlurEffect.Gaussian;
            RefreshFiltersPreview();
        }

        // Shared by the three OK/Apply buttons. Swaps the previewed layer in; the selection is kept.
        private void ApplyFilterPreview(string description)
        {
            if (previewBitmap == null || MainBitmap == null || IsActiveLayerLocked)
            {
                CancelFilters();
                return;
            }

            SaveStateForUndo(description: description);
            Bitmap previous = MainBitmap;
            MainBitmap = previewBitmap;
            previewBitmap = null;
            previous.Dispose();

            artisticFilters = ArtisticFilters.None;
            blurEffect = BlurEffect.None;
            toolStripArtisticFilters.Visible = false;
            toolStripPixelate.Visible = false;
            toolStripGaussianBlur.Visible = false;

            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            isModified = true;
            isSaved = false;
            MarkLayerThumbnailDirty();
            RefreshActiveLayerThumbnail(force: true);
        }

        private void buttonArtisticFiltersCancel_Click(object sender, EventArgs e)
        {
            toolStripArtisticFilters.Visible = false;
            artisticFilters = ArtisticFilters.None;
            CancelFilters();
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
            ArtisticFilters selected = (ArtisticFilters)filterType;
            switch (selected)
            {
                case ArtisticFilters.OilPainting:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold.ToString();
                    break;
                case ArtisticFilters.Cartoon:
                    textBoxArtisticFilterSize.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize.ToString();
                    textBoxArtisticFilterIntensity.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity.ToString();
                    textBoxArtisticFilterThreshold.Text = FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold.ToString();
                    break;
                default:
                    return;
            }
            BeginFilterPreview(selected, BlurEffect.None);
        }

        private void pixellateToolStripMenuItem_Click(object sender, EventArgs e) =>
            BeginFilterPreview(ArtisticFilters.None, BlurEffect.Pixelate);

        private void blurToolStripMenuItem1_Click(object sender, EventArgs e) =>
            BeginFilterPreview(ArtisticFilters.None, BlurEffect.Gaussian);

        private void buttonArtisticFiltersOK_Click(object sender, EventArgs e) =>
            ApplyFilterPreview(artisticFilters == ArtisticFilters.Cartoon ? "Cartoon filter" : "Oil painting filter");

        private void buttonApplyPixelation_Click(object sender, EventArgs e) =>
            ApplyFilterPreview("Pixelate");

        private void buttonApplyGaussianBlur_Click(object sender, EventArgs e) =>
            ApplyFilterPreview("Gaussian blur");
        private bool TryReadFilterNumber(string text, int min, int max, string label, out int value)
        {
            if (!int.TryParse(text, out value))
            {
                Logger.Log($"Invalid {label} value entered for artistic filter: " + text, Logger.LogTypes.Error);
                MessageForm.Show($"Invalid {label} value is entered", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (value < min || value > max)
            {
                Logger.Log($"The {label} value is out of range for artistic filter: " + text, Logger.LogTypes.Error);
                MessageForm.Show($"The {label} value is too big or too small", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void setArtisticFilterSize()
        {
            if (artisticFilters != ArtisticFilters.None)
            {
                bool cartoon = artisticFilters == ArtisticFilters.Cartoon;
                if (TryReadFilterNumber(textBoxArtisticFilterSize.Text, 2, 32, "size", out int value))
                {
                    if (cartoon) FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = value;
                    else FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = value;
                }
                else if (cartoon)
                    FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize = Settings1.Default.DefaultCartoonFilterSize;
                else
                    FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize = Settings1.Default.DefaultOilPaintFilterSize;

                textBoxArtisticFilterSize.Text = (cartoon
                    ? FilterValues.ArtisticFiltersValues.CartoonFilterValues.FilterSize
                    : FilterValues.ArtisticFiltersValues.OilPaintFilterValues.FilterSize).ToString();
            }
            RefreshFiltersPreview();
        }

        private void setArtisticFilterIntensity()
        {
            if (artisticFilters != ArtisticFilters.None)
            {
                bool cartoon = artisticFilters == ArtisticFilters.Cartoon;
                if (TryReadFilterNumber(textBoxArtisticFilterIntensity.Text, 1, 255, "intensity", out int value))
                {
                    if (cartoon) FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = value;
                    else FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = value;
                }
                else if (cartoon)
                    FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity = Settings1.Default.DefaultCartoonFilterIntensity;
                else
                    FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity = Settings1.Default.DefaultOilPaintFilterIntensity;

                textBoxArtisticFilterIntensity.Text = (cartoon
                    ? FilterValues.ArtisticFiltersValues.CartoonFilterValues.Intensity
                    : FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Intensity).ToString();
            }
            RefreshFiltersPreview();
        }

        private void setArtisticFilterThreshold()
        {
            if (artisticFilters != ArtisticFilters.None)
            {
                bool cartoon = artisticFilters == ArtisticFilters.Cartoon;
                if (TryReadFilterNumber(textBoxArtisticFilterThreshold.Text, 0, 255, "threshold", out int value))
                {
                    if (cartoon) FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = value;
                    else FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = value;
                }
                else if (cartoon)
                    FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold = Settings1.Default.DefaultCartoonFilterThreshold;
                else
                    FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold = Settings1.Default.DefaultOilPaintFilterThreshold;

                textBoxArtisticFilterThreshold.Text = (cartoon
                    ? FilterValues.ArtisticFiltersValues.CartoonFilterValues.Threshold
                    : FilterValues.ArtisticFiltersValues.OilPaintFilterValues.Threshold).ToString();
            }
            RefreshFiltersPreview();
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
            Paste();
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void Cut()
        {
            if (IsActiveLayerLocked || MainBitmap == null) return;
            try
            {
                if (!CutOrCopy()) return;

                SaveStateForUndo(description: "Cut selection");
                ClearSelectionPixels(MainBitmap);
                SyncActiveLayerFromCanvas();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                isModified = true;
                isSaved = false;
                MarkLayerThumbnailDirty();
                RefreshActiveLayerThumbnail(force: true);
            }
            catch (Exception ex)
            {
                Logger.Log("Error cutting image: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error cutting image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Copy()
        {
            try
            {
                CutOrCopy();   // copying never changes the document
            }
            catch (Exception ex)
            {
                Logger.Log("Error copying image: " + ex.Message, Logger.LogTypes.Error);
                MessageForm.Show("Error copying image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Paste()
        {
            if (IsActiveLayerLocked) return;
            if (MainBitmap == null)
                return;

            Bitmap bmp = GetClipboardBitmap();
            if (bmp == null)
            {
                Logger.Log("No image in clipboard.", Logger.LogTypes.Error);
                MessageForm.Show("No image in clipboard.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Center of what the user is currently looking at, in canvas (zoomed) coordinates.
            Point viewportCenter = UIPanel.PointToScreen(new Point(
                UIPanel.ClientSize.Width / 2, UIPanel.ClientSize.Height / 2));
            Point center = pictureBoxCanvas.PointToClient(viewportCenter);
            center.X = Math.Max(0, Math.Min(pictureBoxCanvas.Width, center.X));
            center.Y = Math.Max(0, Math.Min(pictureBoxCanvas.Height, center.Y));

            AddFloatingImage(bmp, center);
        }

        /// <summary>
        /// Reads an image from the clipboard, preferring PNG so transparency survives.
        /// Falls back to the normal bitmap format, then to a copied image file.
        /// </summary>
        private Bitmap GetClipboardBitmap()
        {
            try
            {
                System.Windows.Forms.IDataObject data = System.Windows.Forms.Clipboard.GetDataObject();
                if (data == null)
                    return null;

                if (data.GetDataPresent("PNG") && data.GetData("PNG") is MemoryStream pngStream)
                {
                    try
                    {
                        using (pngStream)
                        using (Image png = Image.FromStream(pngStream))
                            return new Bitmap(png);   // copy: the stream must not outlive the image
                    }
                    catch { /* fall through to the next format */ }
                }

                if (System.Windows.Forms.Clipboard.ContainsImage())
                {
                    using (Image clip = System.Windows.Forms.Clipboard.GetImage())
                        if (clip != null)
                            return new Bitmap(clip);
                }

                if (data.GetDataPresent(DataFormats.FileDrop) &&
                    data.GetData(DataFormats.FileDrop) is string[] files &&
                    files.Length > 0 && IsImageFile(files[0]))
                {
                    using (Image loaded = Image.FromFile(files[0]))
                        return new Bitmap(loaded);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading clipboard: " + ex.Message, Logger.LogTypes.Error);
            }
            return null;
        }

        private bool CutOrCopy()
        {
            Bitmap copyBitmap = CreateSelectionBitmap();
            if (copyBitmap == null) return false;

            try
            {
                byte[] pngBytes;
                using (MemoryStream pngStream = new MemoryStream())
                {
                    copyBitmap.Save(pngStream, ImageFormat.Png);
                    pngBytes = pngStream.ToArray();
                }

                DataObject dataObject = new DataObject();
                dataObject.SetData("PNG", false, new MemoryStream(pngBytes));
                dataObject.SetData(DataFormats.Bitmap, true, copyBitmap);
                System.Windows.Forms.Clipboard.SetDataObject(dataObject, true);
                return true;
            }
            finally
            {
                copyBitmap.Dispose();
            }
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            float current = zoomAnimationTimer.Enabled ? zoomAnimationTo : zoom;
            if (current >= 5f - 0.0001f) { SystemSounds.Beep.Play(); return; }
            RequestSmoothZoom(0.05f);
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            float current = zoomAnimationTimer.Enabled ? zoomAnimationTo : zoom;
            if (current <= 0.05f + 0.0001f) { SystemSounds.Beep.Play(); return; }
            RequestSmoothZoom(-0.05f);
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
            sunnyTheCat1.Visible = Settings1.Default.ShowSunnyTheCat;
        }
        // Keep the three nested surfaces in a single coordinate system.
        // The canvas has a 10 px resize-handle gutter on each edge; the resizer
        // and picture box must always have precisely the zoomed image dimensions.
        private void SetZoomedCanvasBounds()
        {
            if (MainBitmap == null) return;
            Size imageSize = new Size(
                Math.Max(1, (int)Math.Round(MainBitmap.Width * zoom)),
                Math.Max(1, (int)Math.Round(MainBitmap.Height * zoom)));

            panelResizer.Size = imageSize;
            pictureBoxCanvas.Size = imageSize;

            // A dropped image is a child of pictureBoxCanvas, so it will not
            // scale automatically when the parent PictureBox is zoomed.
            // Retain its unzoomed image geometry while keeping a fixed 4 px
            // resize border/handle gutter on all four sides at every zoom.
            foreach (MovingImageControl floating in
                pictureBoxCanvas.Controls.OfType<MovingImageControl>())
                floating.ApplyZoom(zoom);

            canvasPanel.Size = new Size(imageSize.Width + 20, imageSize.Height + 20);
            if (panelResizer.Parent == canvasPanel)
                panelResizer.Location = new Point(10, 10);
            if (pictureBoxCanvas.Parent == panelResizer)
                pictureBoxCanvas.Location = Point.Empty;
            UIPanel.AutoScrollMinSize = canvasPanel.Size;
        }

        // Place all eight handle controls at the actual image corners/edge centers.
        // Convert positions into each handle's own parent coordinates, so this
        // works even when handles are not children of canvasPanel.
        private void PositionZoomResizeHandles()
        {
            if (!canvasPanel.IsHandleCreated || !pictureBoxCanvas.IsHandleCreated) return;
            Control[] corners = { resize_top_left, resize_top, resize_top_right,
                resize_left, resize_right, resize_bottom_left, resize_bottom,
                resize_bottom_right };
            int w = pictureBoxCanvas.Width, h = pictureBoxCanvas.Height;
            Point[] points = {
                new Point(0, 0), new Point(w / 2, 0), new Point(w, 0),
                new Point(0, h / 2), new Point(w, h / 2),
                new Point(0, h), new Point(w / 2, h), new Point(w, h)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Control handle = corners[i];
                if (handle == null || handle.Parent == null) continue;
                Point screen = pictureBoxCanvas.PointToScreen(points[i]);
                Point center = handle.Parent.PointToClient(screen);
                Point loc = new Point(center.X - handle.Width / 2,
                                      center.Y - handle.Height / 2);
                if (handle.Location != loc) handle.Location = loc;
            }
        }

        private void RepaintZoomSurface()
        {
            // Do not issue synchronous RDW_UPDATENOW/ERASE on multiple child HWNDs.
            // Each child buffers its own frame. Invalidating once after final
            // layout avoids painting partially-resized intermediate states.
            UIPanel.Invalidate(true);
        }

        private void UpdatePictureBoxZoom(bool keepGestureAnchor = false)
        {
            if (MainBitmap == null || UIPanel.ClientSize.Width <= 0 ||
                UIPanel.ClientSize.Height <= 0)
                return;

            // The image-space coordinate beneath the *old* visible viewport
            // center is the point to keep centered after zooming.
            if (!keepGestureAnchor)
                CaptureZoomAnchor();

            double anchorX = zoomAnchorImageX;
            double anchorY = zoomAnchorImageY;
            bool previouslyFitX = canvasPanel.Width <= UIPanel.ClientSize.Width;
            bool previouslyFitY = canvasPanel.Height <= UIPanel.ClientSize.Height;

            updatingCanvasZoom = true;
            try
            {
                // Update nested controls as a single layout change. Never rely
                // on panelResizer's previous size to size pictureBoxCanvas.
                UIPanel.SuspendLayout();
                canvasPanel.SuspendLayout();
                panelResizer.SuspendLayout();
                try
                {
                    SetZoomedCanvasBounds();
                    pictureBoxCanvas.Invalidate();
                    pictureBoxCanvas.Update();
                }
                finally
                {
                    panelResizer.ResumeLayout(false);
                    canvasPanel.ResumeLayout(false);
                    UIPanel.ResumeLayout(true);
                }

                // Scrollbars may have appeared/disappeared when the canvas grew.
                // Take the viewport center AFTER the new layout, never reuse the
                // previous viewport's center stored by CaptureZoomAnchor().
                int viewportW = UIPanel.ClientSize.Width;
                int viewportH = UIPanel.ClientSize.Height;
                bool fitsX = canvasPanel.Width <= viewportW;
                bool fitsY = canvasPanel.Height <= viewportH;

                // When a previously centered image becomes scrollable, WinForms
                // otherwise retains its positive Left/Top as a virtual origin.
                // Reset only the axes that just crossed into scrollable mode.
                if ((previouslyFitX && !fitsX) || (previouslyFitY && !fitsY))
                {
                    UIPanel.SuspendLayout();
                    try
                    {
                        Point location = canvasPanel.Location;
                        if (previouslyFitX && !fitsX)
                            location.X = UIPanel.AutoScrollPosition.X;
                        if (previouslyFitY && !fitsY)
                            location.Y = UIPanel.AutoScrollPosition.Y;
                        canvasPanel.Location = location;
                    }
                    finally
                    {
                        UIPanel.ResumeLayout(true);
                    }
                    // Normalizing the scroll origin may change scrollbar
                    // visibility; always use the final effective viewport.
                    viewportW = UIPanel.ClientSize.Width;
                    viewportH = UIPanel.ClientSize.Height;
                    fitsX = canvasPanel.Width <= viewportW;
                    fitsY = canvasPanel.Height <= viewportH;
                }

                int insetX = canvasPanel.PointToClient(
                    pictureBoxCanvas.PointToScreen(Point.Empty)).X;
                int insetY = canvasPanel.PointToClient(
                    pictureBoxCanvas.PointToScreen(Point.Empty)).Y;

                // Use effective pixel ratios, not the unrounded requested zoom;
                // pictureBoxCanvas dimensions are integer pixels in WinForms.
                double scaleX = (double)pictureBoxCanvas.Width / MainBitmap.Width;
                double scaleY = (double)pictureBoxCanvas.Height / MainBitmap.Height;
                int maxX = Math.Max(0, canvasPanel.Width - viewportW);
                int maxY = Math.Max(0, canvasPanel.Height - viewportH);
                int desiredX = fitsX ? 0 : Math.Max(0, Math.Min(maxX,
                    (int)Math.Round(insetX + anchorX * scaleX - viewportW / 2.0)));
                int desiredY = fitsY ? 0 : Math.Max(0, Math.Min(maxY,
                    (int)Math.Round(insetY + anchorY * scaleY - viewportH / 2.0)));

                // Scroll only once after sizes/scrollbars settle. WinForms uses
                // positive setter arguments and negative values when reading.
                UIPanel.AutoScrollPosition = new Point(desiredX, desiredY);

                // WinForms may clamp or offset AutoScrollPosition when the child
                // was previously centered. Correct using the ACTUAL image position
                // on screen, not the requested scrollbar value.
                for (int pass = 0; pass < 3; pass++)
                {
                    if (fitsX || fitsY)
                        CenterCanvasPanel();

                    Point actual = UIPanel.PointToClient(
                        pictureBoxCanvas.PointToScreen(Point.Empty));
                    int errorX = fitsX ? 0 : (int)Math.Round(
                        actual.X + anchorX * scaleX - UIPanel.ClientSize.Width / 2.0);
                    int errorY = fitsY ? 0 : (int)Math.Round(
                        actual.Y + anchorY * scaleY - UIPanel.ClientSize.Height / 2.0);
                    if (Math.Abs(errorX) <= 1 && Math.Abs(errorY) <= 1)
                        break;

                    Point current = UIPanel.AutoScrollPosition;
                    int nextX = fitsX ? 0 : Math.Clamp(
                        -current.X + errorX, 0, maxX);
                    int nextY = fitsY ? 0 : Math.Clamp(
                        -current.Y + errorY, 0, maxY);
                    if (nextX == -current.X && nextY == -current.Y)
                        break;
                    UIPanel.AutoScrollPosition = new Point(nextX, nextY);
                }
            }
            finally
            {
                updatingCanvasZoom = false;
            }

            if (canvasPanel.Width <= UIPanel.ClientSize.Width ||
                canvasPanel.Height <= UIPanel.ClientSize.Height)
                CenterCanvasPanel();
            PositionZoomResizeHandles();
            labelZoom.Text = $"{Math.Round(zoom * 100)}%";
            RepaintZoomSurface();
            UpdateRulers();
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
                redoStack.Pop().Dispose(); // Also disposes full-stack layer bitmaps.
            UpdateUndoRedoButtons();
        }

        private bool MaskMatchesCanvas(Bitmap bitmap) =>
    magicSelectionMask != null &&
    magicSelectionMaskWidth == bitmap.Width &&
    magicSelectionMaskHeight == bitmap.Height;

        // Same-size results are copied pixel for pixel; only resized results are resampled.
        private static void DrawIntoRectangle(Graphics g, Image source, Rectangle destination)
        {
            bool sameSize = source.Width == destination.Width && source.Height == destination.Height;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = sameSize ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = sameSize ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
            g.DrawImage(source, destination, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
        }

        // Writes `generated` (scaled to the selection's bounding box) into `target`,
        // only where the selection is: the whole rectangle, or just the mask pixels.
        private void WriteImageThroughSelection(Bitmap target, Image generated)
        {
            Rectangle bounds = GetSelectionImageBounds();
            if (bounds.Width < 1 || bounds.Height < 1) return;

            if (HasMaskSelection)
            {
                if (MaskMatchesCanvas(target))
                    CopyGeneratedThroughMask(target, generated, bounds);
                return;
            }

            using (Graphics g = Graphics.FromImage(target))
                DrawIntoRectangle(g, generated, bounds);
        }

        private void ApplyGeneratedSelectionToCanvas(Image generatedImage, string historyDescription)
        {
            if (IsActiveLayerLocked) return;
            if (generatedImage == null)
                throw new ArgumentNullException(nameof(generatedImage));
            if (MainBitmap == null || !HasAnySelection)
                return;

            Bitmap next = new Bitmap(MainBitmap);
            try
            {
                WriteImageThroughSelection(next, generatedImage);

                SaveStateForUndo(description: historyDescription);
                Bitmap old = MainBitmap;
                MainBitmap = next;
                next = null;
                old?.Dispose();
                SelectedBitmap?.Dispose();
                SelectedBitmap = null;
                SyncActiveLayerFromCanvas();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                MarkLayerThumbnailDirty();
                RefreshActiveLayerThumbnail();
                isModified = true;
                isSaved = false;
                pictureBoxCanvas.Invalidate();
            }
            finally
            {
                next?.Dispose();
            }
        }

        private void CopyGeneratedThroughMask(Bitmap target, Image generated, Rectangle bounds)
        {
            using Bitmap scaled = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.Clear(Color.Transparent);
                DrawIntoRectangle(g, generated, new Rectangle(0, 0, bounds.Width, bounds.Height));
            }

            BitmapData targetData = target.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData scaledData = scaled.LockBits(new Rectangle(Point.Empty, scaled.Size),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = bounds.Width * 4;
                byte[] targetRow = new byte[rowBytes];
                byte[] scaledRow = new byte[rowBytes];

                for (int y = 0; y < bounds.Height; y++)
                {
                    IntPtr targetPtr = IntPtr.Add(targetData.Scan0, y * targetData.Stride);
                    IntPtr scaledPtr = IntPtr.Add(scaledData.Scan0, y * scaledData.Stride);
                    Marshal.Copy(targetPtr, targetRow, 0, rowBytes);
                    Marshal.Copy(scaledPtr, scaledRow, 0, rowBytes);

                    int maskRow = (bounds.Y + y) * magicSelectionMaskWidth + bounds.X;
                    bool changed = false;
                    for (int x = 0; x < bounds.Width; x++)
                    {
                        if (!magicSelectionMask[maskRow + x]) continue;
                        System.Buffer.BlockCopy(scaledRow, x * 4, targetRow, x * 4, 4);
                        changed = true;
                    }

                    if (changed)
                        Marshal.Copy(targetRow, 0, targetPtr, rowBytes);
                }
            }
            finally
            {
                scaled.UnlockBits(scaledData);
                target.UnlockBits(targetData);
            }
        }

        // Copy of the selected rectangle at its real pixel size. The old code derived the size
        // from the zoomed rectangle, which truncates and resamples the pixels at zooms other than 100%.
        private Bitmap CreateSelectionWorkingBitmap()
        {
            Rectangle r = GetSelectionImageBounds();
            return r.Width < 1 || r.Height < 1
                ? null
                : MainBitmap.Clone(r, PixelFormat.Format32bppArgb);
        }

        // Zeroes the pixels of `bmp` (inside or outside the mask). lockRect is the area of bmp
        // to process; maskOrigin is the image coordinate of lockRect's top-left corner.
        private void ZeroMaskedPixels(Bitmap bmp, Rectangle lockRect, Point maskOrigin, bool zeroInside)
        {
            BitmapData data = bmp.LockBits(lockRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = lockRect.Width * 4;
                byte[] row = new byte[rowBytes];
                for (int y = 0; y < lockRect.Height; y++)
                {
                    IntPtr ptr = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(ptr, row, 0, rowBytes);

                    int maskRow = (maskOrigin.Y + y) * magicSelectionMaskWidth + maskOrigin.X;
                    bool changed = false;
                    for (int x = 0; x < lockRect.Width; x++)
                    {
                        if (magicSelectionMask[maskRow + x] != zeroInside) continue;
                        Array.Clear(row, x * 4, 4);
                        changed = true;
                    }

                    if (changed)
                        Marshal.Copy(row, 0, ptr, rowBytes);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        // Clears the selected pixels (rectangle or mask). With no selection, clears the whole bitmap.
        private void ClearSelectionPixels(Bitmap target)
        {
            Rectangle bounds = HasAnySelection
                ? GetSelectionImageBounds()
                : new Rectangle(Point.Empty, target.Size);
            if (bounds.Width < 1 || bounds.Height < 1) return;

            if (HasMaskSelection)
            {
                if (MaskMatchesCanvas(target))
                    ZeroMaskedPixels(target, bounds, bounds.Location, zeroInside: true);
                return;
            }

            using (Graphics g = Graphics.FromImage(target))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillRectangle(Brushes.Transparent, bounds);
            }
        }

        // The selected pixels only (transparent outside a lasso/wand mask), cropped to the
        // selection's bounding box. With no selection, a copy of the whole layer.
        private Bitmap CreateSelectionBitmap()
        {
            if (MainBitmap == null) return null;
            FlushPendingSelectionBitmap();

            Rectangle bounds = HasAnySelection
                ? GetSelectionImageBounds()
                : new Rectangle(Point.Empty, MainBitmap.Size);
            if (bounds.Width < 1 || bounds.Height < 1) return null;

            Bitmap copy = MainBitmap.Clone(bounds, PixelFormat.Format32bppArgb);
            if (HasMaskSelection && MaskMatchesCanvas(MainBitmap))
                ZeroMaskedPixels(copy, new Rectangle(Point.Empty, copy.Size), bounds.Location, zeroInside: false);
            return copy;
        }

        // Ctrl+A
        private void SelectAllCanvas()
        {
            if (MainBitmap == null || isImageCurrentlyCreating) return;

            CompleteZoomAnimation();          // make sure `zoom` is the final value
            CommitActiveTextEditor();
            CommitFloatingImagesToActiveLayer();
            CancelFilters();
            if (lassoActive) CancelLasso();
            SetAsUnselected();                // merges pending edits and clears any previous selection

            originalSelectionRectangleLocation = Point.Empty;
            originalSelectionRectangleSize = MainBitmap.Size;
            SelectionRectangle = new Rectangle(0, 0,
                (int)Math.Round(MainBitmap.Width * zoom),
                (int)Math.Round(MainBitmap.Height * zoom));
            isSelected = true;
            pictureBoxCanvas.Invalidate();
        }

        private void ApplyGeneratedImageToCanvas(Image generatedImage,
            string historyDescription = "AI image edit")
        {
            if (IsActiveLayerLocked) return;
            if (generatedImage == null)
                throw new ArgumentNullException(nameof(generatedImage));

            Bitmap replacement = new Bitmap(generatedImage);
            // Delayed until the AI provider has returned a usable result. Errors,
            // cancellation or null responses must not add phantom history steps.
            SaveStateForUndo(description: historyDescription);
            Bitmap previous = MainBitmap;
            bool canvasSizeChanged = previous == null || previous.Size != replacement.Size;
            MainBitmap = replacement;

            try
            {
                if (canvasSizeChanged)
                {
                    // Some AI providers return a different resolution. Keep the
                    // bitmap, the zoomed controls and the scroll extent in sync.
                    originalSize = replacement.Size;
                    SetZoomedCanvasBounds();
                    CenterCanvasPanel();
                    PositionZoomResizeHandles();
                    labelSize.Text = $"{replacement.Width} X {replacement.Height}px";
                }

                // Persist the generated pixels in the current layer. The thumbnail
                // and visible composite are independent copies of MainBitmap.
                SyncActiveLayerFromCanvas();
                MarkCompositeDirty();
                RenderCompositeToCanvas(force: true);
                pictureBoxCanvas.Invalidate();
                pictureBoxCanvas.Update();
                MarkLayerThumbnailDirty();
                RefreshActiveLayerThumbnail();

                isModified = true;
                isSaved = false;
                pictureBoxCanvas.Invalidate();
            }
            finally
            {
                if (!ReferenceEquals(previous, replacement))
                    previous?.Dispose();
            }
        }

        private async void buttonCreate_Click(object sender, EventArgs e)
        {
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                string systemInstruction = "Fix the selection-editing pipeline by identifying whether the seam comes from coordinate mapping or generated content.\r\n\r\nFirst, bypass generation and reinsert the unchanged input crop through the exact same processing pipeline. Verify that it aligns with the original without displacement, stretching, or visible seams. Correct any failed mapping before continuing.\r\n\r\nTreat surrounding context as protected reference content. Keep the editable mask separate from the context crop. Composite only through the selection mask, preserving all pixels outside it.\r\n\r\nDo not assume that image-to-image generation preserves geometry or honors a mask. Verify that the backend actually forwards a mask and that the selected model supports masked editing. If either is missing, implement a supported inpainting workflow before claiming seamless selection editing.\r\n\r\nRequest edits only within the mask. Preserve the position and shape of structures crossing its boundary. Reject results that shift those structures; feathering cannot repair displaced sunglasses, whiskers, or contours.\r\n\r\nInspect the actual returned image dimensions and reverse recorded crop, resize, and padding transforms before compositing. Never substitute requested dimensions for measured dimensions.";
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
                    if (!await TryRunAiOnSelectionAsync(systemInstruction + "\n" + textBoxPrompt.Text, "AI generate image"))
                    {
                        var image = await ImageGenerationCore.GenerateImage(textBoxPrompt.Text, MainBitmap,
                            MainBitmap.Width, MainBitmap.Height, cts.Token);
                        if (image != null)
                            ApplyGeneratedImageToCanvas(image, "AI generate image");
                        else
                        {
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
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

        private void buttonCancelPixelation_Click(object sender, EventArgs e)
        {
            toolStripPixelate.Visible = false;
            blurEffect = BlurEffect.None;
            CancelFilters();
        }

        private void setPixelationSize()
        {
            try
            {
                if (Convert.ToInt32(textBoxPixelationSize.Text) < 1 || Convert.ToInt32(textBoxPixelationSize.Text) > 9299)
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
                if (Convert.ToInt32(textBoxPixelationOffsetX.Text) < -2944 || Convert.ToInt32(textBoxPixelationOffsetX.Text) > 10602)
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
                if (Convert.ToInt32(textBoxPixelationOffsetY.Text) < -2944 || Convert.ToInt32(textBoxPixelationOffsetY.Text) > 10602)
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

            if (guideDragging && key == Keys.Escape)
            {
                CancelGuideDrag();
                return true;
            }

            if (activeTextEditor != null)
            {
                if (key == Keys.Escape)
                {
                    CancelActiveTextEditor();
                    return true;
                }
                if (key == Keys.Enter && (keyData & Keys.Control) != 0)
                {
                    CommitActiveTextEditor();
                    return true;
                }
                // TextBox keeps Delete, Backspace, arrows, Ctrl+A, Ctrl+Z,
                // and ordinary Enter (for multiline text).
                return base.ProcessCmdKey(ref msg, keyData);
            }
            if (keyData == (Keys.Control | Keys.A) &&
    !(ActiveControl is TextBoxBase) && !(ActiveControl is ComboBox))
            {
                SelectAllCanvas();
                return true;
            }
            // Ctrl+D: commit floating images and deselect the current selection.
            if (keyData == (Keys.Control | Keys.D))
            {
                CommitFloatingImagesToActiveLayer();
                SetAsUnselected();
                HideAddTextTextBoxes();
                return true;
            }
            MovingImageControl topFloating = pictureBoxCanvas.Controls
    .OfType<MovingImageControl>().FirstOrDefault();   // index 0 = frontmost
            if (topFloating != null &&
                !(ActiveControl is TextBoxBase) && !(ActiveControl is ToolStrip))
            {
                if (key == Keys.Enter) { CommitFloatingImagesToActiveLayer(); return true; }

                // Esc, Delete and Backspace all discard the floating image without
                // touching the layer (nothing was committed, so there is nothing to undo).
                if (key == Keys.Escape || key == Keys.Delete || key == Keys.Back)
                {
                    CancelFloatingImages();
                    return true;
                }

                int step = (keyData & Keys.Shift) != 0 ? 10 : 1;
                switch (key)
                {
                    case Keys.Left: topFloating.Nudge(-step, 0); return true;
                    case Keys.Right: topFloating.Nudge(step, 0); return true;
                    case Keys.Up: topFloating.Nudge(0, -step); return true;
                    case Keys.Down: topFloating.Nudge(0, step); return true;
                }
            }
            if (key == Keys.Escape)
            {
                if (lassoActive) { CancelLasso(); return true; }
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
            if (IsActiveLayerLocked) return;
            if (MainBitmap == null ||
                activeLayerIndex < 0 ||
                activeLayerIndex >= editorLayers.Count)
            {
                return;
            }

            // Keep this operation independently undoable for the active layer.
            SaveStateForUndo(description: "Clear layer");

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
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
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
            if (IsActiveLayerLocked || !isSelected || MainBitmap == null) return;

            if (!HasAnySelection)        // a click with the Selection tool that never became a rectangle
            {
                SetAsUnselected();
                return;
            }

            FlushPendingSelectionBitmap();
            SaveStateForUndo(description: "Delete selection");
            ClearSelectionPixels(MainBitmap);
            SetAsUnselected();

            SyncActiveLayerFromCanvas();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            isModified = true;
            isSaved = false;
            MarkLayerThumbnailDirty();
            RefreshActiveLayerThumbnail(force: true);
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
            if (oldIndex >= 0 && newIndex >= 0 && oldIndex < editorLayers.Count &&
                newIndex < editorLayers.Count &&
                (editorLayers[oldIndex].Locked || editorLayers[newIndex].Locked)) return;
            if (oldIndex < 0 || oldIndex >= editorLayers.Count ||
                newIndex < 0 || newIndex >= editorLayers.Count ||
                oldIndex == newIndex)
                return;

            SyncActiveLayerFromCanvas();
            SaveStateForUndo(includeAllLayers: true, description: "Reorder layer"); // Link layer reordering to history

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

            // ListView compatibility rows may have been removed and reinserted.
            // Reapply the authoritative model's visibility to every eye icon.
            for (int i = 0; i < editorLayers.Count; i++)
            {
                listView1.SetLayerVisibility(i, editorLayers[i].Visible);
                listView1.SetLayerLocked(i, editorLayers[i].Locked);
            }

            isModified = true;
            isSaved = false;
            UpdateActiveLayerSettingsControls();
            MarkCompositeDirty();
            RenderCompositeToCanvas(force: true);
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            listView1.Invalidate();
        }

        private void toolStripButtonDeleteLayer_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            if (editorLayers.Count <= 1)
            {
                SystemSounds.Beep.Play();
                return;
            }

            int deleteIndex = listView1.SelectedIndices[0];
            if (deleteIndex < 0 || deleteIndex >= editorLayers.Count ||
                editorLayers[deleteIndex].Locked)
                return;

            SyncActiveLayerFromCanvas();
            SaveStateForUndo(includeAllLayers: true, description: "Delete layer"); // Link layer deletion to history

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
            pictureBoxCanvas.Invalidate();
            pictureBoxCanvas.Update();
            listView1.Invalidate();
        }

        private async void shareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // A real StorageFile is more widely supported by Windows share targets
                // than a bitmap-only, memory-backed DataPackage.
                byte[] pngBytes = CreateShareImagePng();
                // Keep a unique directory per share so the attachment can use the
                // document's visible name without colliding with older shares.
                string shareDirectory = Path.Combine(Path.GetTempPath(), "LibreCanvas", "Share",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(shareDirectory);
                string sharePath = Path.Combine(shareDirectory, GetShareFileName());
                await File.WriteAllBytesAsync(sharePath, pngBytes);
                StorageFile shareFile = await StorageFile.GetFileFromPathAsync(sharePath);

                DataTransferManager manager = DataTransferManagerInterop.GetForWindow(Handle);
                Windows.Foundation.TypedEventHandler<DataTransferManager, DataRequestedEventArgs> handler = null;
                handler = (senderManager, args) =>
                {
                    manager.DataRequested -= handler;
                    DataRequest request = args.Request;
                    try
                    {
                        request.Data.Properties.Title = "LibreCanvas image";
                        request.Data.Properties.Description = "PNG image from LibreCanvas";
                        request.Data.Properties.FileTypes.Add(".png");
                        request.Data.SetStorageItems(new[] { shareFile });
                        request.Data.SetBitmap(RandomAccessStreamReference.CreateFromFile(shareFile));
                    }
                    catch (Exception ex)
                    {
                        request.FailWithDisplayText("Unable to prepare the image for sharing: " + ex.Message);
                    }
                };
                manager.DataRequested += handler;
                try
                {
                    DataTransferManagerInterop.ShowShareUIForWindow(Handle);
                }
                catch
                {
                    manager.DataRequested -= handler;
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageForm.Show("Unable to share the image: " + ex.Message,
                    "Share", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetShareFileName()
        {
            // The label may contain an absolute path, a filename, or a placeholder.
            string displayedName = labelFileName.Text?.Trim() ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(
                displayedName.Replace('\\', '/').Split('/').Last());

            if (string.IsNullOrWhiteSpace(baseName) ||
                baseName.Equals("Unnamed File", StringComparison.OrdinalIgnoreCase) ||
                baseName.Equals("AI Generated File", StringComparison.OrdinalIgnoreCase))
                baseName = "LibreCanvas";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(invalid, '_');

            // Also replace characters forbidden by Windows when built elsewhere.
            foreach (char invalid in "<>:\"/\\|?*")
                baseName = baseName.Replace(invalid, '_');

            baseName = baseName.Trim().TrimEnd('.', ' ');
            if (baseName.Length == 0)
                baseName = "LibreCanvas";
            return baseName + ".png";
        }

        private byte[] CreateShareImagePng()
        {
            Bitmap image = editorLayers.Count > 0
                ? CreateCompositeBitmap()
                : MainBitmap != null
                    ? (Bitmap)MainBitmap.Clone()
                    : throw new InvalidOperationException("No image available to share.");

            using (image)
            using (MemoryStream stream = new())
            {
                image.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static class DataTransferManagerInterop
        {
            private const string RuntimeClassId = "Windows.ApplicationModel.DataTransfer.DataTransferManager";
            private static readonly Guid InterfaceId = new("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8");
            // IID_IDataTransferManager (the runtime class GUID is not the
            // interface IID expected by IDataTransferManagerInterop).
            private static readonly Guid DataTransferManagerId = new("A5CAEE9B-8708-49D1-8D36-67D25A8DA00C");

            public static DataTransferManager GetForWindow(IntPtr windowHandle)
            {
                IDataTransferManagerInterop interop = GetInterop();
                Guid dataTransferManagerId = DataTransferManagerId;
                int result = interop.GetForWindow(windowHandle, ref dataTransferManagerId, out IntPtr managerAbi);
                Marshal.ThrowExceptionForHR(result);
                try
                {
                    return WinRT.MarshalInterface<DataTransferManager>.FromAbi(managerAbi);
                }
                finally
                {
                    Marshal.Release(managerAbi);
                }
            }

            public static void ShowShareUIForWindow(IntPtr windowHandle)
            {
                int result = GetInterop().ShowShareUIForWindow(windowHandle);
                Marshal.ThrowExceptionForHR(result);
            }

            private static IDataTransferManagerInterop GetInterop()
            {
                Guid interfaceId = InterfaceId;
                int result = WindowsCreateString(RuntimeClassId, (uint)RuntimeClassId.Length, out IntPtr runtimeClassId);
                Marshal.ThrowExceptionForHR(result);

                try
                {
                    result = RoGetActivationFactory(runtimeClassId, ref interfaceId, out IDataTransferManagerInterop interop);
                    Marshal.ThrowExceptionForHR(result);
                    return interop;
                }
                finally
                {
                    WindowsDeleteString(runtimeClassId);
                }
            }

            [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
            private static extern int RoGetActivationFactory(
                IntPtr runtimeClassId,
                ref Guid interfaceId,
                [MarshalAs(UnmanagedType.Interface)] out IDataTransferManagerInterop factory);

            [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
            private static extern int WindowsCreateString(
                [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                uint length,
                out IntPtr stringHandle);

            [DllImport("combase.dll", ExactSpelling = true, PreserveSig = true)]
            private static extern int WindowsDeleteString(IntPtr stringHandle);

            [ComImport]
            [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IDataTransferManagerInterop
            {
                [PreserveSig]
                int GetForWindow(
                    IntPtr appWindow,
                    ref Guid interfaceId,
                    out IntPtr dataTransferManager);

                [PreserveSig]
                int ShowShareUIForWindow(IntPtr appWindow);
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            Control tetikleyenKontrol = contextMenuStrip1.SourceControl;

            if (tetikleyenKontrol == pictureBoxCanvas ||
                pictureBoxCanvas.ClientRectangle.Contains(pictureBoxCanvas.PointToClient(Cursor.Position)))
            {
                if ((selectedTool == Tools.Cursor)
                    || (selectedTool == Tools.Selection)
                    || (selectedTool == Tools.Text))
                {
                    return; // If the selected tool is Cursor, allow the context menu to open on the PictureBox
                }
                e.Cancel = true; // Prevent the context menu from opening
            }
        }

        private void textBoxTolerance2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                setMagicSelectionToolTolerance();
            }
        }

        private void textBoxTolerance2_Leave(object sender, EventArgs e)
        {
            setMagicSelectionToolTolerance();
        }

        private bool IsImageFile(string filePath)
        {
            try
            {
                // Validate without retaining a file lock on the dropped image.
                using (Image test = Image.FromFile(filePath))
                    return test.Width > 0 && test.Height > 0;
            }
            catch
            {
                return false;
            }
        }

        private void ImageEditor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ImageEditor_DragDrop(object sender, DragEventArgs e)
        {
            string file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            if (IsImageFile(file))
            {
                Bitmap image;
                using (Image loaded = Image.FromFile(file))
                    image = new Bitmap(loaded);

                Point drop = pictureBoxCanvas.PointToClient(new Point(e.X, e.Y));
                AddFloatingImage(image, drop);
            }
            else
            {
                MessageForm.Show("The dropped file is not a valid image.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void queryTheAIUsageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            queryTheAIUsageToolStripMenuItem.Enabled = false;

            try
            {
                int queryNumber = await ImageGenerationCore.GetRemainingUsage();
                if (queryNumber == -1)
                {
                    MessageForm.Show(
                        $"Failed to retrieve AI usage.",
                        "AI Usage",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                MessageForm.Show(
                    $"Remaining AI usage: {queryNumber}\r\n" +
                    $"\r\nThe AI usage is reset at: \r\n" +
                    $"{DateTime.UtcNow.Date.AddDays(1).ToLocalTime().ToString("yyyy-MM-dd hh:mm tt", System.Globalization.CultureInfo.InvariantCulture)}",
                        "AI Usage",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageForm.Show(
                    $"Failed to retrieve AI usage: {ex.Message}",
                    "AI Usage",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                queryTheAIUsageToolStripMenuItem.Enabled = true;
            }
        }/// <summary>
         /// Places a bitmap on the canvas as a floating, movable/resizable image.
         /// Any image that is already floating is committed first (like placing a
         /// second object in Photoshop). The control takes ownership of the bitmap.
         /// </summary>
        private MovingImageControl AddFloatingImage(Bitmap image, Point displayCenter)
        {
            // Make sure zoom is the final value, not a mid-animation one.
            CompleteZoomAnimation();
            CommitFloatingImagesToActiveLayer();

            MovingImageControl floating = new MovingImageControl(image);
            floating.CommitRequested += (s, _) => CommitFloatingImagesToActiveLayer();
            floating.ApplyZoom(zoom);
            pictureBoxCanvas.Controls.Add(floating);

            floating.FitInside(MainBitmap.Size);        // only shrinks, never enlarges
            floating.CenterAtDisplayPoint(displayCenter);
            floating.BringToFront();
            return floating;
        }

        private async void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripAICreateImage.Visible = false;
            if (!HasAnySelection)
            {
                if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
                {

                    string systemInstruction =
                        "You are performing image outpainting, not adding a frame. " +
                        "The image has been placed on a larger canvas. All blank " +
                        "or transparent areas outside the original image represent " +
                        "missing scenery that must be generated. Extend the actual " +
                        "photograph naturally into every missing area. Continue " +
                        "existing structures, landscapes, textures, lighting, and " +
                        "perspective beyond the original image boundaries. " +
                        "Do not create white borders, picture frames, margins, " +
                        "matting, or blank spaces. The final image must be " +
                        "completely filled with continuous photographic content.";

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
                        var imageTask = ImageGenerationCore.GenerateImage(systemInstruction, MainBitmap,
                                MainBitmap.Width, MainBitmap.Height, cts.Token);
                        var image = await imageTask; // Await the task to get the result
                        if (image != null)
                        {
                            // Rebuild the layer composite; Invalidate() alone
                            // repaints the old cached preview.
                            ApplyGeneratedImageToCanvas(image, "AI expand image");
                        }
                        else
                        {
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                        MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
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
            else
            {
                SystemSounds.Beep.Play(); // Notify the user that the expand feature is not available for selections.
            }
        }

        private async void colorizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                string systemInstruction = "Colorize the image, keeping the original details and textures intact. Ensure that the colors are natural and realistic, enhancing the overall visual appeal of the image.";
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
                    if (!await TryRunAiOnSelectionAsync(systemInstruction, "AI colorize"))
                    {
                        var image = await ImageGenerationCore.GenerateImage(systemInstruction, MainBitmap,
                            MainBitmap.Width, MainBitmap.Height, cts.Token);
                        if (image != null)
                            ApplyGeneratedImageToCanvas(image, "AI colorize");
                        else
                        {
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
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
        private async void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckIfInternetConnectionAvailable.IsInternetAvailable())
            {
                string systemInstruction = "Repair the image, restoring any missing or damaged areas while preserving the original details and textures. Ensure that the restored areas blend seamlessly with the surrounding content, maintaining a natural and realistic appearance.";
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
                    if (!await TryRunAiOnSelectionAsync(systemInstruction, "AI restore"))
                    {
                        var image = await ImageGenerationCore.GenerateImage(systemInstruction, MainBitmap,
                            MainBitmap.Width, MainBitmap.Height, cts.Token);
                        if (image != null)
                            ApplyGeneratedImageToCanvas(image, "AI restore");
                        else
                        {
                            Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                            MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                    MessageForm.Show("The generated image is null. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
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

        private void trackBarOpacity_ValueChanged(object sender, EventArgs e)
        {
            if (suppressLayerSettingsChanged || IsActiveLayerLocked || activeLayerIndex < 0 ||
                activeLayerIndex >= editorLayers.Count)
                return;

            EditorLayer layer = editorLayers[activeLayerIndex];
            int nextOpacity = trackBarOpacity.Value;
            if (layer.Opacity == nextOpacity)
                return;

            // A mouse drag is one history entry, not dozens of gigantic bitmap copies.
            if (!opacityDragInProgress || !opacityDragUndoSaved)
            {
                SaveStateForUndo(includeAllLayers: true, description: $"Layer opacity: {nextOpacity}%");
                if (opacityDragInProgress)
                    opacityDragUndoSaved = true;
            }

            layer.Opacity = nextOpacity;
            labelOpacity.Text = nextOpacity.ToString() + "%";
            LayerSettingsModified();
        }

        private void lassoTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(lassoTool);
        }

        private void lassoTool_CheckedChanged(object sender, EventArgs e)
        {
            if (lassoTool.Checked)
            {
                selectedTool = Tools.Lasso;
            }
        }

        private readonly List<PointF> lassoPoints = new();
        private bool lassoActive = false;

        private void BeginLasso(MouseEventArgs e)
        {
            if (MainBitmap == null) return;
            if (isSelected) SetAsUnselected();

            lassoPoints.Clear();
            lassoPoints.Add(ClampToImage(e.X / zoom, e.Y / zoom));
            lassoActive = true;
            pictureBoxCanvas.Invalidate();
        }

        private PointF ClampToImage(float x, float y) =>
            new PointF(Math.Max(0f, Math.Min(MainBitmap.Width, x)),
                       Math.Max(0f, Math.Min(MainBitmap.Height, y)));

        private void ExtendLasso(MouseEventArgs e)
        {
            if (!lassoActive || MainBitmap == null) return;

            PointF next = ClampToImage(e.X / zoom, e.Y / zoom);
            PointF last = lassoPoints[lassoPoints.Count - 1];
            float dx = (next.X - last.X) * zoom, dy = (next.Y - last.Y) * zoom;
            if (dx * dx + dy * dy < 4f) return;   // ignore sub-2px screen jitter

            lassoPoints.Add(next);
            pictureBoxCanvas.Invalidate();
        }

        private void CancelLasso()
        {
            lassoActive = false;
            lassoPoints.Clear();
            pictureBoxCanvas.Invalidate();
        }

        private void DrawLassoInProgress(Graphics g)
        {
            if (!lassoActive || lassoPoints.Count < 2) return;

            PointF[] pts = lassoPoints.Select(p => new PointF(p.X * zoom, p.Y * zoom)).ToArray();
            GraphicsState state = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.None;
                var (blackPen, whitePen) = CreateMarchingAntsPens();
                using (blackPen)
                using (whitePen)
                {
                    g.DrawLines(blackPen, pts);
                    g.DrawLines(whitePen, pts);
                    // Show where the shape will close.
                    g.DrawLine(blackPen, pts[pts.Length - 1], pts[0]);
                    g.DrawLine(whitePen, pts[pts.Length - 1], pts[0]);
                }
            }
            finally { g.Restore(state); }
        }

        private void FinishLasso()
        {
            lassoActive = false;
            PointF[] pts = lassoPoints.ToArray();
            lassoPoints.Clear();

            if (MainBitmap == null || pts.Length < 3)
            {
                pictureBoxCanvas.Invalidate();
                return;
            }

            int width = MainBitmap.Width, height = MainBitmap.Height;

            using GraphicsPath path = new GraphicsPath(FillMode.Winding);
            path.AddPolygon(pts);

            RectangleF bf = path.GetBounds();
            Rectangle bounds = Rectangle.Intersect(
                Rectangle.FromLTRB((int)Math.Floor(bf.Left), (int)Math.Floor(bf.Top),
                                   (int)Math.Ceiling(bf.Right), (int)Math.Ceiling(bf.Bottom)),
                new Rectangle(0, 0, width, height));
            if (bounds.Width < 1 || bounds.Height < 1)
            {
                pictureBoxCanvas.Invalidate();
                return;
            }

            // The mask uses absolute MainBitmap coordinates, like the Magic Wand mask.
            BitArray mask = new BitArray(width * height);
            int minX = width, minY = height, maxX = -1, maxY = -1, count = 0;

            using (Bitmap raster = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(raster))
                {
                    g.Clear(Color.Black);
                    g.SmoothingMode = SmoothingMode.None;   // hard edges => crisp 1-bit mask
                    g.TranslateTransform(-bounds.X, -bounds.Y);
                    g.FillPath(Brushes.White, path);
                }

                BitmapData data = raster.LockBits(new Rectangle(Point.Empty, raster.Size),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    byte[] row = new byte[bounds.Width * 4];
                    for (int y = 0; y < bounds.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                        for (int x = 0; x < bounds.Width; x++)
                        {
                            if (row[x * 4 + 2] <= 127) continue;   // red channel: white = inside
                            int ax = bounds.X + x, ay = bounds.Y + y;
                            mask[ay * width + ax] = true;
                            count++;
                            if (ax < minX) minX = ax;
                            if (ax > maxX) maxX = ax;
                            if (ay < minY) minY = ay;
                            if (ay > maxY) maxY = ay;
                        }
                    }
                }
                finally { raster.UnlockBits(data); }
            }

            if (count == 0)
            {
                pictureBoxCanvas.Invalidate();
                return;
            }

            magicSelectionMask = mask;
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

        // The lasso and the Magic Wand both use magicSelectionMask, so "mask" covers both.
        private bool HasMaskSelection =>
            isSelected && magicSelectionMask != null && !magicSelectionBounds.IsEmpty;

        private bool HasRectSelection =>
            isSelected && magicSelectionMask == null &&
            SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0;

        private bool HasAnySelection => HasMaskSelection || HasRectSelection;

        // Bounding box of the selection in image (bitmap) pixels.
        private Rectangle GetSelectionImageBounds()
        {
            if (MainBitmap == null) return Rectangle.Empty;
            Rectangle r = HasMaskSelection
                ? magicSelectionBounds
                : new Rectangle(originalSelectionRectangleLocation, originalSelectionRectangleSize);
            r.Intersect(new Rectangle(Point.Empty, MainBitmap.Size));
            return r;
        }

        // A rectangular selection may hold unflushed edits in SelectedBitmap. Merge them into
        // the layer (keeping the selection) so the AI sees, and replaces, the real pixels.
        private void FlushPendingSelectionBitmap()
        {
            if (SelectedBitmap == null || magicSelectionMask != null || MainBitmap == null ||
                originalSelectionRectangleSize.Width <= 0 || originalSelectionRectangleSize.Height <= 0)
                return;

            using (Graphics g = Graphics.FromImage(MainBitmap))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(SelectedBitmap,
                    new Rectangle(originalSelectionRectangleLocation, originalSelectionRectangleSize),
                    new Rectangle(Point.Empty, SelectedBitmap.Size), GraphicsUnit.Pixel);
            }
            SelectedBitmap.Dispose();
            SelectedBitmap = null;
        }

        // Runs an AI edit on the current selection, whatever its type.
        // Returns false when there is no selection, so the caller can edit the whole image.
        private async Task<bool> TryRunAiOnSelectionAsync(string prompt, string historyDescription)
        {
            if (!HasAnySelection) return false;
            if (IsActiveLayerLocked) { SystemSounds.Beep.Play(); return true; }

            FlushPendingSelectionBitmap();
            Rectangle bounds = GetSelectionImageBounds();
            if (bounds.Width < 1 || bounds.Height < 1) { SystemSounds.Beep.Play(); return true; }

            using Bitmap input = MainBitmap.Clone(bounds, PixelFormat.Format32bppArgb);
            var image = await ImageGenerationCore.GenerateImage(prompt, input,
                input.Width, input.Height, cts.Token);

            if (image != null)
                ApplyGeneratedSelectionToCanvas(image, historyDescription);
            else
            {
                Logger.Log("The generated image is null.", Logger.LogTypes.Error);
                MessageForm.Show("The generated image is null. Please try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
        }

        private void tabControlWithCloseButtons1_TabClosed(object sender, TabPageEventArgs e)
        {
            if (tabControlWithCloseButtons1.TabPages.Count == 0)
            {
                CreateDocumentTab(createNewFile, "Unnamed File");
            }
        }

        private void EnsureCloneStampBitmapFormat()
        {
            // LockBits requires a compatible pixel format. Imported images can
            // be 24-bit or indexed; convert just the editable layer when needed.
            if (MainBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var converted = new Bitmap(MainBitmap.Width, MainBitmap.Height,
                    PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(converted))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(MainBitmap, 0, 0);
                }
                MainBitmap.Dispose();
                MainBitmap = converted;
            }
            if (HasRectSelection && SelectedBitmap != null &&
                SelectedBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var converted = new Bitmap(SelectedBitmap.Width, SelectedBitmap.Height,
                    PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(converted))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(SelectedBitmap, 0, 0);
                }
                SelectedBitmap.Dispose();
                SelectedBitmap = converted;
            }
        }

        // Creates a 32-bit, exact-pixel snapshot of the current layer as seen in
        // the selection. Read from this snapshot until MouseUp, not from the
        // progressively edited destination bitmap.
        private Bitmap CreateCloneStampSourceSnapshot()
        {
            var snapshot = new Bitmap(MainBitmap.Width, MainBitmap.Height,
                PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(snapshot))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImageUnscaled(MainBitmap, 0, 0);
                if (HasRectSelection && SelectedBitmap != null)
                {
                    Rectangle selection = GetSelectionImageBounds();
                    if (!selection.IsEmpty)
                        g.DrawImage(SelectedBitmap, selection,
                            new Rectangle(Point.Empty, SelectedBitmap.Size), GraphicsUnit.Pixel);
                }
            }
            return snapshot;
        }

        private void StopCloneStampStroke()
        {
            cloneStampStrokeSource?.Dispose();
            cloneStampStrokeSource = null;
            cloneStampPreviousPoint = null;
        }

        // Paint a continuous sequence of round pixel-copy dabs. The source offset
        // is calculated once at MouseDown, so moving the cursor by (dx, dy)
        // samples the source at the same (dx, dy) displacement.
        private Rectangle ContinueCloneStampStroke(Point destination)
        {
            Rectangle dirty = Rectangle.Empty;
            if (cloneStampStrokeSource == null || MainBitmap == null)
                return dirty;

            Point start = cloneStampPreviousPoint ?? destination;
            double dx = (double)destination.X - start.X;
            double dy = (double)destination.Y - start.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double spacing = Math.Max(1d, cloneStampSize / 4d);
            int steps = Math.Max(1, (int)Math.Ceiling(distance / spacing));

            for (int i = cloneStampPreviousPoint.HasValue ? 1 : 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                Point dab = new Point(
                    (int)Math.Round(start.X + dx * t),
                    (int)Math.Round(start.Y + dy * t));
                Rectangle painted = StampClonePixels(dab);
                if (!painted.IsEmpty)
                    dirty = dirty.IsEmpty ? painted : Rectangle.Union(dirty, painted);
            }
            cloneStampPreviousPoint = destination;
            return dirty;
        }

        // This is an exact ARGB transfer (not a brush-colored blend): transparent
        // source pixels erase the target, and alpha is preserved without halos.
        // A rectangular or Magic Wand/lasso selection restricts writes to its area.
        private Rectangle StampClonePixels(Point center)
        {
            Bitmap source = cloneStampStrokeSource;
            if (source == null || MainBitmap == null)
                return Rectangle.Empty;

            bool rectangularSelection = HasRectSelection && SelectedBitmap != null;
            Bitmap target = rectangularSelection ? SelectedBitmap : MainBitmap;
            Rectangle selectionBounds = rectangularSelection
                ? GetSelectionImageBounds() : new Rectangle(Point.Empty, MainBitmap.Size);

            int size = Math.Max(1, cloneStampSize);
            int left = center.X - (size - 1) / 2;
            int top = center.Y - (size - 1) / 2;
            Rectangle area = new Rectangle(left, top, size, size);
            area.Intersect(new Rectangle(Point.Empty, MainBitmap.Size));
            area.Intersect(selectionBounds);
            // Ensure translated source coordinates are also valid.
            area.Intersect(new Rectangle(-cloneStampStrokeOffset.X, -cloneStampStrokeOffset.Y,
                                         source.Width, source.Height));
            if (area.IsEmpty)
                return Rectangle.Empty;

            Rectangle sourceArea = new Rectangle(
                area.X + cloneStampStrokeOffset.X,
                area.Y + cloneStampStrokeOffset.Y, area.Width, area.Height);
            Rectangle targetArea = new Rectangle(
                area.X - (rectangularSelection ? selectionBounds.X : 0),
                area.Y - (rectangularSelection ? selectionBounds.Y : 0),
                area.Width, area.Height);
            if (targetArea.X < 0 || targetArea.Y < 0 ||
                targetArea.Right > target.Width || targetArea.Bottom > target.Height)
                return Rectangle.Empty;

            BitmapData sourceBits = null;
            BitmapData targetBits = null;
            bool changed = false;
            try
            {
                sourceBits = source.LockBits(sourceArea, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                targetBits = target.LockBits(targetArea, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int rowBytes = area.Width * 4;
                byte[] sourceRow = new byte[rowBytes];
                byte[] targetRow = new byte[rowBytes];
                // Pixel centers lie at integer image coordinates. This also
                // ensures a size-1 brush stamps exactly one image pixel.
                double radius = size / 2d;
                double radiusSquared = radius * radius;
                for (int row = 0; row < area.Height; row++)
                {
                    int imageY = area.Y + row;
                    double yDistance = imageY - center.Y;
                    IntPtr srcPtr = IntPtr.Add(sourceBits.Scan0, row * sourceBits.Stride);
                    IntPtr dstPtr = IntPtr.Add(targetBits.Scan0, row * targetBits.Stride);
                    Marshal.Copy(srcPtr, sourceRow, 0, rowBytes);
                    Marshal.Copy(dstPtr, targetRow, 0, rowBytes);
                    bool rowChanged = false;
                    for (int col = 0; col < area.Width; col++)
                    {
                        int imageX = area.X + col;
                        double xDistance = imageX - center.X;
                        if (xDistance * xDistance + yDistance * yDistance > radiusSquared)
                            continue;
                        if (HasMaskSelection &&
                            (magicSelectionMaskWidth != MainBitmap.Width ||
                             magicSelectionMaskHeight != MainBitmap.Height ||
                             !magicSelectionMask[imageY * magicSelectionMaskWidth + imageX]))
                            continue;

                        int pixel = col * 4;
                        if (sourceRow[pixel] == targetRow[pixel] &&
                            sourceRow[pixel + 1] == targetRow[pixel + 1] &&
                            sourceRow[pixel + 2] == targetRow[pixel + 2] &&
                            sourceRow[pixel + 3] == targetRow[pixel + 3])
                            continue;
                        System.Buffer.BlockCopy(sourceRow, pixel, targetRow, pixel, 4);
                        rowChanged = true;
                    }
                    if (rowChanged)
                    {
                        Marshal.Copy(targetRow, 0, dstPtr, rowBytes);
                        changed = true;
                    }
                }
            }
            finally
            {
                if (targetBits != null) target.UnlockBits(targetBits);
                if (sourceBits != null) source.UnlockBits(sourceBits);
            }
            return changed ? area : Rectangle.Empty;
        }

        private void cloneStampTool_CheckedChanged(object sender, EventArgs e)
        {
            if (cloneStampTool.Checked)
            {
                selectedTool = Tools.CloneStamp;
                toolStripCloneStamp.Visible = true;
            }
            else
            {
                toolStripCloneStamp.Visible = false;
                if (cloneStampStrokeSource != null)
                {
                    // Finishing by clicking another tool must not leave the fast
                    // composite stroke path enabled for the next tool.
                    if (suppressCompositeDuringToolStroke)
                        EndToolStrokePreview();
                    isdrawing = false;
                    StopCloneStampStroke();
                }
            }
            pictureBoxCanvas.Invalidate();
        }

        private void cloneStampTool_Click(object sender, EventArgs e)
        {
            ToolStripButtonsClick(cloneStampTool);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCloneStampSize();
        }

        private void UpdateCloneStampSize()
        {
            string input = cloneStampSizeCombobox.Text;
            if (!int.TryParse(input, out int size) || size < 1 || size > 1024)
            {
                Logger.Log("Invalid clone stamp size: " + input, Logger.LogTypes.Error);
                MessageForm.Show("Enter a Clone Stamp size between 1 and 1024 pixels.",
                    "Clone Stamp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cloneStampSizeCombobox.Text = cloneStampSize.ToString();
                return;
            }
            cloneStampSize = size;
            pictureBoxCanvas.Invalidate();
        }

        private sealed class CanvasGuide
        {
            public bool Vertical { get; }
            public double Position { get; set; }

            public CanvasGuide(bool vertical, double position)
            {
                Vertical = vertical;
                Position = position;
            }
        }

        private bool guidesVisible = true;
        private bool guidesLocked;
        private bool guideDragging;
        private bool guideDragVertical;
        private double guideDragPosition;
        private CanvasGuide guideMoving;
        private CanvasDocument guideDragDocument;
        private Control guideDragSource;
        private ContextMenuStrip guideMenu;
        private ToolStripMenuItem showGuidesItem;
        private ToolStripMenuItem lockGuidesItem;
        private ToolStripMenuItem clearGuidesItem;

        private void InitializeGuideInteractions(RulerControl horizontal, RulerControl vertical)
        {
            // Reuse the Designer-created rulers. Capture remains on the ruler
            // while dragging toward the image, so drawing tools aren't activated.
            foreach (RulerControl ruler in new[] { horizontal, vertical })
            {
                ruler.MouseDown += RulerGuide_MouseDown;
                ruler.MouseMove += RulerGuide_MouseMove;
                ruler.MouseUp += RulerGuide_MouseUp;
                ruler.Cursor = ruler.Vertical ? Cursors.SizeWE : Cursors.SizeNS;
            }

            guideMenu = new ContextMenuStrip();
            showGuidesItem = new ToolStripMenuItem("Show Guides")
            {
                CheckOnClick = true,
                Checked = true
            };
            lockGuidesItem = new ToolStripMenuItem("Lock Guides")
            {
                CheckOnClick = true
            };
            clearGuidesItem = new ToolStripMenuItem("Clear Guides");
            showGuidesItem.CheckedChanged += (_, _) =>
            {
                guidesVisible = showGuidesItem.Checked;
                CancelGuideDrag();
                pictureBoxCanvas.Invalidate();
                zoomAnimationOverlay?.Invalidate();
            };
            lockGuidesItem.CheckedChanged += (_, _) =>
            {
                guidesLocked = lockGuidesItem.Checked;
                if (guidesLocked) CancelGuideDrag();
            };
            clearGuidesItem.Click += (_, _) =>
            {
                if (activeCanvasDocument == null || guidesLocked)
                    return;
                CancelGuideDrag();
                activeCanvasDocument.Guides.Clear();
                pictureBoxCanvas.Invalidate();
                zoomAnimationOverlay?.Invalidate();
            };
            guideMenu.Opening += (_, _) =>
            {
                lockGuidesItem.Enabled = activeCanvasDocument != null;
                clearGuidesItem.Enabled = !guidesLocked &&
                    activeCanvasDocument?.Guides.Count > 0;
            };
            guideMenu.Items.AddRange(new ToolStripItem[]
            {
                showGuidesItem, lockGuidesItem,
                new ToolStripSeparator(), clearGuidesItem
            });
            horizontal.ContextMenuStrip = guideMenu;
            vertical.ContextMenuStrip = guideMenu;
            Disposed += (_, _) => guideMenu?.Dispose();
        }

        private void RulerGuide_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !guidesVisible || guidesLocked ||
                MainBitmap == null || activeCanvasDocument == null ||
                switchingCanvasDocument || isdrawing || isResizing ||
                guideDragging || sender is not RulerControl ruler)
                return;

            // Match the live picture box geometry. Finish any outstanding
            // animation before the gesture so the coordinates stay consistent.
            if (zoomAnimationTimer.Enabled)
                CompleteZoomAnimation();

            guideDragging = true;
            guideDragDocument = activeCanvasDocument;
            guideDragSource = ruler;
            guideMoving = null;
            guideDragVertical = !ruler.Vertical;
            ruler.Capture = true;
            UpdateGuideDragPosition(ruler.PointToScreen(e.Location));
        }

        private void RulerGuide_MouseMove(object sender, MouseEventArgs e)
        {
            if (guideDragging && ReferenceEquals(guideDragSource, sender) &&
                sender is RulerControl ruler)
                UpdateGuideDragPosition(ruler.PointToScreen(e.Location));
        }

        private void RulerGuide_MouseUp(object sender, MouseEventArgs e)
        {
            if (guideDragging && ReferenceEquals(guideDragSource, sender) &&
                sender is RulerControl ruler)
                FinishGuideDrag(ruler.PointToScreen(e.Location), e.Button == MouseButtons.Left);
        }

        // Called before ImageEditor's existing MouseDown handler performs
        // any painting. Guide grab is available with Cursor, or Alt+drag.
        private bool TryBeginCanvasGuideDrag(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || guideDragging || !guidesVisible ||
                guidesLocked || isdrawing || isResizing || isImageCurrentlyCreating ||
                MainBitmap == null || activeCanvasDocument == null ||
                (selectedTool != Tools.Cursor && (ModifierKeys & Keys.Alt) == 0))
                return false;

            // A floating image has priority over any guide passing behind it.
            foreach (MovingImageControl floating in pictureBoxCanvas.Controls.OfType<MovingImageControl>())
                if (floating.ContainsCanvasPoint(e.Location))
                    return false;

            double sx = (double)pictureBoxCanvas.Width / MainBitmap.Width;
            double sy = (double)pictureBoxCanvas.Height / MainBitmap.Height;
            CanvasGuide nearest = null;
            double nearestDistance = 7.0; // Screen pixels regardless of zoom.
            foreach (CanvasGuide guide in activeCanvasDocument.Guides)
            {
                double line = guide.Position * (guide.Vertical ? sx : sy);
                double distance = Math.Abs((guide.Vertical ? e.X : e.Y) - line);
                if (distance < nearestDistance)
                {
                    nearest = guide;
                    nearestDistance = distance;
                }
            }
            if (nearest == null) return false;

            guideDragging = true;
            guideDragDocument = activeCanvasDocument;
            guideDragSource = pictureBoxCanvas;
            guideMoving = nearest;
            guideDragVertical = nearest.Vertical;
            pictureBoxCanvas.Capture = true;
            UpdateGuideDragPosition(pictureBoxCanvas.PointToScreen(e.Location));
            return true;
        }

        private bool UpdateCanvasGuideDrag(MouseEventArgs e)
        {
            if (!guideDragging || !ReferenceEquals(guideDragSource, pictureBoxCanvas))
                return false;
            UpdateGuideDragPosition(pictureBoxCanvas.PointToScreen(e.Location));
            return true;
        }

        private bool FinishCanvasGuideDrag(MouseEventArgs e)
        {
            if (!guideDragging || !ReferenceEquals(guideDragSource, pictureBoxCanvas))
                return false;
            FinishGuideDrag(pictureBoxCanvas.PointToScreen(e.Location),
                e.Button == MouseButtons.Left);
            return true;
        }

        private void UpdateGuideDragPosition(Point screenPoint)
        {
            if (!guideDragging || MainBitmap == null ||
                !ReferenceEquals(guideDragDocument, activeCanvasDocument) ||
                !pictureBoxCanvas.IsHandleCreated)
                return;
            Point local = pictureBoxCanvas.PointToClient(screenPoint);
            double sx = Math.Max(0.000001, (double)pictureBoxCanvas.Width / MainBitmap.Width);
            double sy = Math.Max(0.000001, (double)pictureBoxCanvas.Height / MainBitmap.Height);
            double length = guideDragVertical ? MainBitmap.Width : MainBitmap.Height;
            guideDragPosition = Math.Clamp(
                Math.Round((guideDragVertical ? local.X / sx : local.Y / sy)), 0, length);
            pictureBoxCanvas.Invalidate();
        }

        private static bool ScreenContains(Control control, Point screenPoint) =>
            control != null && !control.IsDisposed && control.IsHandleCreated &&
            control.RectangleToScreen(control.ClientRectangle).Contains(screenPoint);

        private void FinishGuideDrag(Point screenPoint, bool commit)
        {
            if (!guideDragging) return;
            CanvasDocument document = guideDragDocument;
            CanvasGuide existing = guideMoving;
            bool vertical = guideDragVertical;

            // Recalculate for the exact mouse-up location.
            UpdateGuideDragPosition(screenPoint);
            double position = guideDragPosition;
            bool inViewport = ScreenContains(UIPanel, screenPoint);
            bool inImage = ScreenContains(pictureBoxCanvas, screenPoint);
            bool toRuler = ScreenContains(rulerControl1, screenPoint) ||
                           ScreenContains(rulerControl2, screenPoint);

            CancelGuideDrag();
            if (!commit || document == null || !ReferenceEquals(document, activeCanvasDocument) ||
                !guidesVisible || guidesLocked || MainBitmap == null)
                return;

            if (inViewport && inImage)
            {
                // Store pixel positions, not scaled coordinates, per document.
                if (existing != null)
                    existing.Position = position;
                else
                    document.Guides.Add(new CanvasGuide(vertical, position));
            }
            else if (existing != null && toRuler)
            {
                // Photoshop-style: dragging an existing guide back onto a ruler
                // removes it. Dropping elsewhere simply leaves it unchanged.
                document.Guides.Remove(existing);
            }
            pictureBoxCanvas.Invalidate();
            zoomAnimationOverlay?.Invalidate();
        }

        private void CancelGuideDrag()
        {
            if (!guideDragging) return;
            Control source = guideDragSource;
            guideDragging = false;
            guideMoving = null;
            guideDragDocument = null;
            guideDragSource = null;
            if (source != null && !source.IsDisposed && source.Capture)
                source.Capture = false;
            if (!pictureBoxCanvas.IsDisposed)
                pictureBoxCanvas.Invalidate();
            zoomAnimationOverlay?.Invalidate();
        }

        // Render guides as overlay lines only. Never add them to MainBitmap,
        // compositeCanvasBitmap, or any image-export pipeline.
        // imageRect is either PictureBox client bounds or animated image bounds.
        private void DrawRulerGuides(Graphics graphics, Rectangle imageRect)
        {
            if (!guidesVisible || MainBitmap == null || activeCanvasDocument == null ||
                imageRect.Width < 1 || imageRect.Height < 1)
                return;

            var saved = graphics.Save();
            try
            {
                graphics.SetClip(imageRect, CombineMode.Intersect);
                using var contrastPen = new Pen(Color.FromArgb(210, 0, 0, 0), 3f);
                using var guidePen = new Pen(Color.FromArgb(255, 45, 199, 240), 1f)
                {
                    DashStyle = DashStyle.Dash,
                    DashPattern = new[] { 4f, 3f }
                };
                using var previewPen = new Pen(Color.FromArgb(245, 255, 188, 61), 1.5f)
                {
                    DashStyle = DashStyle.Dash,
                    DashPattern = new[] { 5f, 3f }
                };
                double scaleX = (double)imageRect.Width / MainBitmap.Width;
                double scaleY = (double)imageRect.Height / MainBitmap.Height;

                void PaintLine(bool vertical, double position, bool preview)
                {
                    double limit = vertical ? MainBitmap.Width : MainBitmap.Height;
                    if (position < 0 || position > limit) return;
                    float coordinate = (float)(vertical
                        ? imageRect.Left + position * scaleX
                        : imageRect.Top + position * scaleY);
                    PointF a = vertical
                        ? new PointF(coordinate, imageRect.Top)
                        : new PointF(imageRect.Left, coordinate);
                    PointF b = vertical
                        ? new PointF(coordinate, imageRect.Bottom)
                        : new PointF(imageRect.Right, coordinate);
                    if (!preview)
                        graphics.DrawLine(contrastPen, a, b);
                    graphics.DrawLine(preview ? previewPen : guidePen, a, b);
                }

                foreach (CanvasGuide guide in activeCanvasDocument.Guides)
                {
                    // A moving guide is shown only once, in its preview position.
                    if (guideDragging && ReferenceEquals(guideMoving, guide))
                        continue;
                    PaintLine(guide.Vertical, guide.Position, false);
                }
                if (guideDragging &&
                    ReferenceEquals(guideDragDocument, activeCanvasDocument))
                    PaintLine(guideDragVertical, guideDragPosition, true);
            }
            finally
            {
                graphics.Restore(saved);
            }
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

    internal sealed class LayerLockToggledEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public bool Locked { get; }
        public LayerLockToggledEventArgs(int rowIndex, bool locked)
        {
            RowIndex = rowIndex;
            Locked = locked;
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
        private readonly ListView compatibilityList = new();
        private readonly LayerListItemCollection itemCollection;
        private readonly LayerSelectedIndexCollection selectedIndexCollection;
        private readonly LayerSelectedItemCollection selectedItemCollection;
        private readonly LayerColumnCollection layerColumns;
        private readonly HashSet<ListViewItem> hiddenItems = new();
        private readonly HashSet<ListViewItem> lockedItems = new();
        private readonly Image showIcon;
        private readonly Image hideIcon;
        private readonly Image lockIcon;
        private readonly Image unlockIcon;
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
        public event EventHandler<LayerLockToggledEventArgs>? LayerLockToggled;
        public event EventHandler<LayerItemRemovedEventArgs>? LayerItemRemoved;

        public LayerDataGridView()
        {
            showIcon = Resources.icons8_show_48;
            hideIcon = Resources.icons8_hide_48;
            // Exact lock/unlock artwork supplied with this change. Embedded in source
            // so users need only replace ImageEditor.cs (no Designer/resource edits).
            lockIcon = Resources.icons8_lock_48;
            unlockIcon = Resources.icons8_unlock_48;

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

            var lockColumn = new DataGridViewImageColumn
            {
                Name = "Lock",
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
            base.Columns.Add(lockColumn);
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

        public void SetLayerLocked(int rowIndex, bool locked)
        {
            if (rowIndex < 0 || rowIndex >= compatibilityList.Items.Count)
                return;
            ListViewItem item = compatibilityList.Items[rowIndex];
            if (locked) lockedItems.Add(item);
            else lockedItems.Remove(item);
            if (rowIndex < Rows.Count)
                Rows[rowIndex].Cells[1].ToolTipText = locked ? "Unlock layer" : "Lock layer";
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
            lockedItems.Remove(item);
            compatibilityList.Items.RemoveAt(index);

            if (index < base.Rows.Count)
                base.Rows.RemoveAt(index);
        }

        internal void ClearCompatibilityItems()
        {
            foreach (ListViewItem item in compatibilityList.Items)
                LayerItemRemoved?.Invoke(this, new LayerItemRemovedEventArgs(item));

            hiddenItems.Clear();
            lockedItems.Clear();
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
            if (e.RowIndex >= 0 &&
                (e.ColumnIndex == 0 || e.ColumnIndex == 1) &&
                e.RowIndex < compatibilityList.Items.Count && e.Button == MouseButtons.Left)
            {
                ListViewItem item = compatibilityList.Items[e.RowIndex];
                if (e.ColumnIndex == 0)
                {
                    bool nextVisible = hiddenItems.Contains(item);
                    LayerVisibilityToggled?.Invoke(this, new LayerVisibilityToggledEventArgs(e.RowIndex, nextVisible));
                }
                else
                {
                    bool nextLocked = !lockedItems.Contains(item);
                    LayerLockToggled?.Invoke(this, new LayerLockToggledEventArgs(e.RowIndex, nextLocked));
                }
                return; // Neither icon changes which layer is selected.
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
                e.Value = lockedItems.Contains(item) ? lockIcon : unlockIcon;
                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == 2)
            {
                if (smallImageList != null && !string.IsNullOrEmpty(item.ImageKey) &&
                    smallImageList.Images.ContainsKey(item.ImageKey))
                    e.Value = smallImageList.Images[item.ImageKey];
                else
                    e.Value = null;

                e.FormattingApplied = true;
            }
            else if (e.ColumnIndex == 3)
            {
                e.Value = item.Text;
                e.FormattingApplied = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // showIcon and hideIcon are shared Resources bitmaps: do not dispose them.
                lockIcon.Dispose();
                unlockIcon.Dispose();
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