using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Carpathia
{
    /// <summary>
    /// A floating (placed) image that behaves like Photoshop / Paint.NET / GIMP:
    ///  - Resizing tracks the cursor immediately (no easing lag).
    ///  - Corner handles keep the aspect ratio; hold Shift to scale freely.
    ///  - Edge handles scale one axis; hold Shift to keep the aspect ratio.
    ///  - Hold Alt to resize around the center.
    ///  - Moving can overhang the canvas, but part of the image always stays
    ///    reachable; Shift locks movement to one axis.
    ///  - Right-click / double-click raise CommitRequested; Nudge() moves by image pixels.
    /// Requires pictureBoxCanvas in MovingImageControl.Designer.cs.
    /// </summary>
    public partial class MovingImageControl : UserControl
    {
        private const int BorderPadding = 4;
        private const int HandleSize = 8;
        // Smallest control: 16x16 content px plus the border, so handles never overlap.
        private const int MinControlDimension = 24;
        private const int MaxControlDimension = 16008;
        // Image pixels that must remain on the canvas while moving (screen px).
        private const int MinVisiblePx = 16;

        // Only used by AnimateToSize(); mouse resizing is immediate.
        private const double ResizeResponsePerSecond = 18.0;

        [Flags]
        private enum ResizeEdges
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }

        private readonly Dictionary<Control, ResizeEdges> resizeHandles =
            new Dictionary<Control, ResizeEdges>();
        private readonly System.Windows.Forms.Timer resizeTimer = new System.Windows.Forms.Timer();
        private readonly Stopwatch resizeClock = new Stopwatch();

        private Control activeHandle;
        private ResizeEdges activeEdges = ResizeEdges.None;
        private Rectangle dragStartBounds;
        private Point dragStartScreen;
        private Rectangle targetBounds;
        private bool dragging;
        private bool updatingHandleLayout;
        private bool resizeSetupComplete;

        private float zoomFactor = 1f;
        private SizeF logicalContentSize;
        private PointF logicalImageLocation;
        private bool applyingZoomBounds;

        private bool moving;
        private Rectangle lastVisualBounds;
        private bool refreshingParentCanvas;
        private Control activeMoveSurface;
        // Canvas draws the floating pixels in its own paint pass. This control
        // owns only the selection outline and eight resize handles.
        private PictureBox backgroundCanvas;
        private Point moveStartScreen;
        private Point moveStartLocation;

        /// <summary>Fires once when a user finishes dragging the image.</summary>
        public event EventHandler MoveCompleted;

        /// <summary>Raised on double-click or right-click so the editor can commit the image.</summary>
        public event EventHandler CommitRequested;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return zoomFactor; }
            set { ApplyZoom(value); }
        }

        [Browsable(false)]
        public SizeF UnzoomedContentSize { get { return logicalContentSize; } }

        [Browsable(false)]
        public PointF UnzoomedImageLocation { get { return logicalImageLocation; } }

        [Browsable(false)]
        public Image DisplayImage { get { return pictureBoxCanvas.Image; } }

        public event EventHandler ResizeAnimationCompleted;

        public MovingImageControl(Image image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            InitializeComponent();

            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            MinimumSize = new Size(MinControlDimension, MinControlDimension);
            MaximumSize = new Size(MaxControlDimension, MaxControlDimension);

            pictureBoxCanvas.Dock = DockStyle.None;
            pictureBoxCanvas.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pictureBoxCanvas.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxCanvas.Image = image;
            pictureBoxCanvas.TabStop = false;
            // A child PictureBox cannot alpha-blend with its grandparent's image.
            // Keep it only as the image holder for the existing editor/commit API.
            // Drawing and mouse interaction are performed by this UserControl.
            pictureBoxCanvas.Visible = false;
            Cursor = Cursors.SizeAll;
            // A floating image must not steal focus from the editor or cause
            // AutoScroll to reposition the viewport on MouseDown.
            TabStop = false;

            Size = new Size(
                Clamp(image.Width + BorderPadding * 2,
                      MinControlDimension, MaxControlDimension),
                Clamp(image.Height + BorderPadding * 2,
                      MinControlDimension, MaxControlDimension));

            SetupResizeHandles();
            logicalContentSize = new SizeF(
                Size.Width - BorderPadding * 2,
                Size.Height - BorderPadding * 2);
            UpdateLogicalLocation();

            MouseDown += MoveSurface_MouseDown;
            MouseMove += MoveSurface_MouseMove;
            MouseUp += MoveSurface_MouseUp;
            MouseCaptureChanged += MoveSurface_MouseCaptureChanged;
            MouseDoubleClick += MoveSurface_MouseDoubleClick;

            resizeTimer.Interval = 16;
            resizeTimer.Tick += ResizeTimer_Tick;
            Disposed += MovingImageControl_Disposed;
            targetBounds = Bounds;
            lastVisualBounds = Bounds;
            UpdateHandleLayout();
        }

        // The parent PictureBox is the only surface that draws image pixels.
        // Rendering translucent pixels inside a moving native child HWND causes
        // the rectangle/ghosting visible on the canvas. Parent painting ensures
        // the floating alpha is blended with the actual underlying image once.
        private void BackgroundCanvas_Paint(object sender, PaintEventArgs e)
        {
            Image floating = DisplayImage;
            if (floating == null || !Visible || IsDisposed)
                return;

            Rectangle destination = new Rectangle(
                Left + BorderPadding, Top + BorderPadding,
                Math.Max(1, Width - BorderPadding * 2),
                Math.Max(1, Height - BorderPadding * 2));
            if (!e.ClipRectangle.IntersectsWith(destination))
                return;

            GraphicsState state = e.Graphics.Save();
            try
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.DrawImage(floating, destination,
                    new Rectangle(0, 0, floating.Width, floating.Height),
                    GraphicsUnit.Pixel);
            }
            finally
            {
                e.Graphics.Restore(state);
            }
        }

        // The interior of this control has no window region, so the actual
        // canvas (including its floating PNG) remains visible without copying
        // the background into a child window. Only the frame/grips are native.
        private void UpdateInteractionRegion()
        {
            if (!resizeSetupComplete || Width < MinControlDimension ||
                Height < MinControlDimension || IsDisposed)
                return;

            Rectangle image = new Rectangle(BorderPadding, BorderPadding,
                Width - BorderPadding * 2, Height - BorderPadding * 2);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRectangle(new Rectangle(image.Left, image.Top, image.Width, 1));
                path.AddRectangle(new Rectangle(image.Left, image.Bottom - 1, image.Width, 1));
                path.AddRectangle(new Rectangle(image.Left, image.Top, 1, image.Height));
                path.AddRectangle(new Rectangle(image.Right - 1, image.Top, 1, image.Height));
                foreach (Control handle in resizeHandles.Keys)
                    path.AddRectangle(handle.Bounds);
                Region old = Region;
                Region = new Region(path);
                old?.Dispose();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do not fill the control with a rectangular backdrop.
            // All opaque content is the frame or child resize-handle panels.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle imageRect = new Rectangle(BorderPadding, BorderPadding,
                Math.Max(1, ClientSize.Width - BorderPadding * 2),
                Math.Max(1, ClientSize.Height - BorderPadding * 2));
            using (Pen frame = new Pen(Color.FromArgb(160, SystemColors.Highlight)))
                e.Graphics.DrawRectangle(frame, imageRect.X, imageRect.Y,
                    imageRect.Width - 1, imageRect.Height - 1);
            base.OnPaint(e);
        }

        /// <summary>
        /// Canvas-side hit testing is required because the transparent interior
        /// is intentionally not part of this child control's window region.
        /// </summary>
        public bool ContainsCanvasPoint(Point point)
        {
            return !IsDisposed && Visible && Bounds.Contains(point);
        }

        // Called from ImageEditor.pictureBoxCanvas_MouseDown/Move/Up, before
        // regular paint-tool processing. Canvas capture keeps movement smooth
        // even when the pointer crosses holes in the interaction region.
        public bool TryBeginCanvasDrag(Point point)
        {
            if (moving || dragging || backgroundCanvas == null ||
                !ContainsCanvasPoint(point))
                return false;

            FinishPendingResize();
            BringToFront();
            moving = true;
            activeMoveSurface = backgroundCanvas;
            moveStartScreen = Cursor.Position;
            moveStartLocation = Location;
            backgroundCanvas.Capture = true;
            return true;
        }

        public bool ContinueCanvasDrag()
        {
            if (!moving || !ReferenceEquals(activeMoveSurface, backgroundCanvas))
                return false;
            MoveToCursor();
            return true;
        }

        public bool EndCanvasDrag()
        {
            if (!moving || !ReferenceEquals(activeMoveSurface, backgroundCanvas))
                return false;
            MoveToCursor();
            EndMove();
            return true;
        }

        private void BackgroundCanvas_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (moving && ReferenceEquals(activeMoveSurface, backgroundCanvas))
                EndMove();
        }

        public void ApplyZoom(float newZoom)
        {
            if (float.IsNaN(newZoom) || float.IsInfinity(newZoom) ||
                newZoom < 0.05f || newZoom > 5f)
                throw new ArgumentOutOfRangeException(nameof(newZoom),
                    "Zoom must be between 0.05 and 5.");
            if (Math.Abs(zoomFactor - newZoom) < 0.000001f)
                return;

            if (dragging)
                EndDrag();
            FinishPendingResize();
            if (moving)
                EndMove();

            zoomFactor = newZoom;
            int imageWidth = Math.Max(1,
                (int)Math.Round(logicalContentSize.Width * zoomFactor));
            int imageHeight = Math.Max(1,
                (int)Math.Round(logicalContentSize.Height * zoomFactor));
            int screenImageX = (int)Math.Round(logicalImageLocation.X * zoomFactor);
            int screenImageY = (int)Math.Round(logicalImageLocation.Y * zoomFactor);
            Rectangle updated = new Rectangle(
                screenImageX - BorderPadding,
                screenImageY - BorderPadding,
                Clamp(imageWidth + 2 * BorderPadding,
                    MinControlDimension, MaxControlDimension),
                Clamp(imageHeight + 2 * BorderPadding,
                    MinControlDimension, MaxControlDimension));

            applyingZoomBounds = true;
            try
            {
                Bounds = updated;
                targetBounds = updated;
            }
            finally
            {
                applyingZoomBounds = false;
            }
            UpdateHandleLayout();
        }

        public void SetImageDisplayLocation(Point imageTopLeft)
        {
            Location = ClampLocationToParent(new Point(imageTopLeft.X - BorderPadding,
                                                       imageTopLeft.Y - BorderPadding));
        }

        /// <summary>Center the image on a point in the parent's (zoomed) coordinates.</summary>
        public void CenterAtDisplayPoint(Point center)
        {
            Location = ClampLocationToParent(new Point(center.X - Width / 2,
                                                       center.Y - Height / 2));
            UpdateLogicalLocation();
        }

        /// <summary>
        /// Shrink (never enlarge) so the image fits inside the canvas, keeping its aspect
        /// ratio. canvasImageSize is the document size in unzoomed image pixels.
        /// </summary>
        public void FitInside(Size canvasImageSize)
        {
            float s = Math.Min(1f, Math.Min(
                canvasImageSize.Width / Math.Max(1f, logicalContentSize.Width),
                canvasImageSize.Height / Math.Max(1f, logicalContentSize.Height)));
            if (s >= 1f)
                return;

            logicalContentSize = new SizeF(logicalContentSize.Width * s,
                                           logicalContentSize.Height * s);
            int w = Math.Max(1, (int)Math.Round(logicalContentSize.Width * zoomFactor));
            int h = Math.Max(1, (int)Math.Round(logicalContentSize.Height * zoomFactor));
            applyingZoomBounds = true;
            try
            {
                Size = new Size(Clamp(w + BorderPadding * 2, MinControlDimension, MaxControlDimension),
                                Clamp(h + BorderPadding * 2, MinControlDimension, MaxControlDimension));
            }
            finally { applyingZoomBounds = false; }
            targetBounds = Bounds;
            UpdateHandleLayout();
        }

        /// <summary>Move by whole IMAGE pixels (arrow keys). Always at least 1 screen px.</summary>
        public void Nudge(int dxImagePx, int dyImagePx)
        {
            if (dragging || moving) return;
            FinishPendingResize();
            int dx = dxImagePx == 0 ? 0 : (int)Math.Round(dxImagePx * zoomFactor);
            int dy = dyImagePx == 0 ? 0 : (int)Math.Round(dyImagePx * zoomFactor);
            if (dxImagePx != 0 && dx == 0) dx = Math.Sign(dxImagePx);
            if (dyImagePx != 0 && dy == 0) dy = Math.Sign(dyImagePx);
            Location = ClampLocationToParent(new Point(Left + dx, Top + dy));
            targetBounds = Bounds;
            UpdateLogicalLocation();
        }

        private void UpdateLogicalLocation()
        {
            logicalImageLocation = new PointF(
                (Left + BorderPadding) / zoomFactor,
                (Top + BorderPadding) / zoomFactor);
        }

        private void CommitLogicalGeometry()
        {
            UpdateLogicalLocation();
            logicalContentSize = new SizeF(
                Math.Max(1, Width - BorderPadding * 2) / zoomFactor,
                Math.Max(1, Height - BorderPadding * 2) / zoomFactor);
        }

        private void FinishPendingResize()
        {
            if (!resizeTimer.Enabled)
                return;
            resizeTimer.Stop();
            resizeClock.Reset();
            Bounds = targetBounds;
            CommitLogicalGeometry();
        }

        public void CompleteInteraction()
        {
            if (moving)
            {
                MoveToCursor();
                EndMove();
            }
            if (dragging)
            {
                UpdateTargetFromCursor();
                EndDrag();
            }
            FinishPendingResize();
            CommitLogicalGeometry();
        }

        // Allow the image to overhang the canvas, but keep MinVisiblePx of it on screen.
        private Point ClampLocationToParent(Point controlLocation)
        {
            Control parent = Parent;
            if (parent == null)
                return controlLocation;

            int imgW = Math.Max(1, Width - BorderPadding * 2);
            int imgH = Math.Max(1, Height - BorderPadding * 2);
            int visW = Math.Min(MinVisiblePx, imgW);
            int visH = Math.Min(MinVisiblePx, imgH);

            int imageLeft = controlLocation.X + BorderPadding;
            int imageTop = controlLocation.Y + BorderPadding;
            imageLeft = Math.Max(visW - imgW, Math.Min(parent.Width - visW, imageLeft));
            imageTop = Math.Max(visH - imgH, Math.Min(parent.Height - visH, imageTop));
            return new Point(imageLeft - BorderPadding, imageTop - BorderPadding);
        }

        private void RaiseCommitRequested()
        {
            // Deferred: the editor disposes this control while handling the event.
            if (IsHandleCreated)
                BeginInvoke((MethodInvoker)(() =>
                {
                    if (!IsDisposed)
                        CommitRequested?.Invoke(this, EventArgs.Empty);
                }));
            else
                CommitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MoveSurface_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                RaiseCommitRequested();
        }

        private void MoveSurface_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RaiseCommitRequested();
                return;
            }
            if (e.Button != MouseButtons.Left || moving || dragging ||
                !(sender is Control source))
                return;

            FinishPendingResize();
            BringToFront();
            moving = true;
            activeMoveSurface = source;
            moveStartScreen = Cursor.Position;
            moveStartLocation = Location;
            source.Capture = true;
        }

        private void MoveSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (!moving || !ReferenceEquals(sender, activeMoveSurface))
                return;
            MoveToCursor();
        }

        private void MoveToCursor()
        {
            Point p = Cursor.Position;
            int dx = p.X - moveStartScreen.X;
            int dy = p.Y - moveStartScreen.Y;

            // Shift: constrain to the dominant axis, like every image editor.
            if ((ModifierKeys & Keys.Shift) != 0)
            {
                if (Math.Abs(dx) >= Math.Abs(dy)) dy = 0;
                else dx = 0;
            }

            Point nextLocation = ClampLocationToParent(
                new Point(moveStartLocation.X + dx, moveStartLocation.Y + dy));
            if (Location != nextLocation)
                Location = nextLocation;
            targetBounds = Bounds;
        }

        private void MoveSurface_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !moving ||
                !ReferenceEquals(sender, activeMoveSurface))
                return;
            MoveToCursor();
            EndMove();
        }

        private void MoveSurface_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (moving && ReferenceEquals(sender, activeMoveSurface))
                EndMove();
        }

        private void EndMove()
        {
            if (!moving)
                return;
            Control captured = activeMoveSurface;
            moving = false;
            activeMoveSurface = null;
            if (captured != null && captured.Capture)
                captured.Capture = false;
            UpdateLogicalLocation();
            MoveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void SetupResizeHandles()
        {
            RegisterHandle("resize_top", ResizeEdges.Top, Cursors.SizeNS);
            RegisterHandle("resize_bottom", ResizeEdges.Bottom, Cursors.SizeNS);
            RegisterHandle("resize_left", ResizeEdges.Left, Cursors.SizeWE);
            RegisterHandle("resize_right", ResizeEdges.Right, Cursors.SizeWE);
            RegisterHandle("resize_top_left", ResizeEdges.Top | ResizeEdges.Left, Cursors.SizeNWSE);
            RegisterHandle("resize_top_right", ResizeEdges.Top | ResizeEdges.Right, Cursors.SizeNESW);
            RegisterHandle("resize_bottom_left", ResizeEdges.Bottom | ResizeEdges.Left, Cursors.SizeNESW);
            RegisterHandle("resize_bottom_right", ResizeEdges.Bottom | ResizeEdges.Right, Cursors.SizeNWSE);
            resizeSetupComplete = true;
        }

        private void RegisterHandle(string name, ResizeEdges edges, Cursor cursor)
        {
            Control[] found = Controls.Find(name, true);
            Control handle;

            if (found.Length != 0)
            {
                handle = found[0];
                if (handle.Parent != this)
                    handle.Parent = this;
            }
            else
            {
                var newHandle = new Panel
                {
                    Name = name,
                    BackColor = SystemColors.Highlight,
                    BorderStyle = BorderStyle.FixedSingle,
                    TabStop = false
                };
                Controls.Add(newHandle);
                handle = newHandle;
            }

            handle.Dock = DockStyle.None;
            handle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            handle.Cursor = cursor;
            handle.TabStop = false;
            handle.Size = new Size(HandleSize, HandleSize);
            handle.BringToFront();
            resizeHandles[handle] = edges;

            handle.MouseDown += ResizeHandle_MouseDown;
            handle.MouseMove += ResizeHandle_MouseMove;
            handle.MouseUp += ResizeHandle_MouseUp;
            handle.MouseCaptureChanged += ResizeHandle_MouseCaptureChanged;
        }

        private void ResizeHandle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RaiseCommitRequested();
                return;
            }
            if (e.Button != MouseButtons.Left || !(sender is Control handle) ||
                !resizeHandles.TryGetValue(handle, out ResizeEdges edges))
                return;

            FinishPendingResize();
            dragging = true;
            activeHandle = handle;
            activeEdges = edges;
            dragStartScreen = Cursor.Position;
            dragStartBounds = Bounds;
            targetBounds = Bounds;
            handle.Capture = true;
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging || !ReferenceEquals(sender, activeHandle))
                return;

            UpdateTargetFromCursor();
        }

        /// <summary>
        /// Computes the new bounds from the cursor and applies them IMMEDIATELY.
        /// Opposite edge stays fixed; aspect lock / center-resize follow modifier keys.
        /// </summary>
        private void UpdateTargetFromCursor()
        {
            int dx = Cursor.Position.X - dragStartScreen.X;
            int dy = Cursor.Position.Y - dragStartScreen.Y;

            bool left = (activeEdges & ResizeEdges.Left) != 0;
            bool right = (activeEdges & ResizeEdges.Right) != 0;
            bool top = (activeEdges & ResizeEdges.Top) != 0;
            bool bottom = (activeEdges & ResizeEdges.Bottom) != 0;
            bool horizontal = left || right;
            bool vertical = top || bottom;
            bool corner = horizontal && vertical;

            Keys mods = ModifierKeys;
            bool shift = (mods & Keys.Shift) != 0;
            bool fromCenter = (mods & Keys.Alt) != 0;
            // Corners keep aspect by default, Shift frees them; edges are free, Shift locks.
            bool keepAspect = corner ? !shift : shift;

            // Work with the CONTENT rectangle (the control minus its screen-space border).
            Rectangle start = new Rectangle(
                dragStartBounds.X + BorderPadding, dragStartBounds.Y + BorderPadding,
                dragStartBounds.Width - BorderPadding * 2,
                dragStartBounds.Height - BorderPadding * 2);
            int minContent = MinControlDimension - BorderPadding * 2;
            int maxContent = MaxControlDimension - BorderPadding * 2;
            int mult = fromCenter ? 2 : 1;

            double w = start.Width + (right ? dx * mult : left ? -dx * mult : 0);
            double h = start.Height + (bottom ? dy * mult : top ? -dy * mult : 0);
            w = Math.Max(minContent, Math.Min(maxContent, w));
            h = Math.Max(minContent, Math.Min(maxContent, h));

            if (keepAspect && start.Width > 0 && start.Height > 0)
            {
                double aspect = (double)start.Width / start.Height;
                if (corner)
                {
                    // Follow the axis the user has dragged proportionally further.
                    double sx = w / start.Width, sy = h / start.Height;
                    double scale = Math.Abs(sx - 1) >= Math.Abs(sy - 1) ? sx : sy;
                    w = start.Width * scale;
                    h = w / aspect;
                }
                else if (horizontal) h = w / aspect;
                else w = h * aspect;

                // Keep both sides inside the allowed range without breaking the ratio.
                double lo = Math.Max(minContent / w, minContent / h);
                if (lo > 1) { w *= lo; h *= lo; }
                double hi = Math.Min(maxContent / w, maxContent / h);
                if (hi < 1) { w *= hi; h *= hi; }
            }

            int newW = (int)Math.Round(w);
            int newH = (int)Math.Round(h);

            int x, y;
            if (fromCenter)
            {
                x = start.X + (start.Width - newW) / 2;
                y = start.Y + (start.Height - newH) / 2;
            }
            else
            {
                // The edge/corner opposite the grabbed handle stays put. When an edge handle
                // drives a proportional resize, the other axis grows around its center.
                x = left ? start.Right - newW : start.X;
                y = top ? start.Bottom - newH : start.Y;
                if (!horizontal) x = start.X + (start.Width - newW) / 2;
                if (!vertical) y = start.Y + (start.Height - newH) / 2;
            }

            Rectangle next = new Rectangle(
                x - BorderPadding, y - BorderPadding,
                newW + BorderPadding * 2, newH + BorderPadding * 2);

            if (targetBounds == next && Bounds == next)
                return;

            if (resizeTimer.Enabled)
                resizeTimer.Stop();
            targetBounds = next;
            Bounds = next;   // immediate: the image follows the cursor 1:1
        }

        private void ResizeHandle_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !dragging ||
                !ReferenceEquals(sender, activeHandle))
                return;

            UpdateTargetFromCursor();
            EndDrag();
        }

        private void ResizeHandle_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (dragging && ReferenceEquals(sender, activeHandle))
                EndDrag();
        }

        private void EndDrag()
        {
            Control captured = activeHandle;
            dragging = false;
            activeHandle = null;
            activeEdges = ResizeEdges.None;
            if (captured != null && captured.Capture)
                captured.Capture = false;

            if (targetBounds == Bounds)
            {
                CommitLogicalGeometry();
                ResizeAnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
                StartResizeAnimation();
        }

        /// <summary>
        /// Animate the control's size from code, keeping its top-left corner fixed.
        /// The width/height include the 8-pixel resize-handle border.
        /// </summary>
        public void AnimateToSize(Size newSize)
        {
            if (dragging)
                EndDrag();

            targetBounds = new Rectangle(
                Left, Top,
                Clamp(newSize.Width, MinControlDimension, MaxControlDimension),
                Clamp(newSize.Height, MinControlDimension, MaxControlDimension));
            StartResizeAnimation();
        }

        private void StartResizeAnimation()
        {
            if (targetBounds == Bounds || IsDisposed || Disposing)
                return;

            if (!resizeTimer.Enabled)
            {
                resizeClock.Restart();
                resizeTimer.Start();
            }
        }

        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing)
            {
                resizeTimer.Stop();
                return;
            }

            double elapsedSeconds = Math.Min(0.1, resizeClock.Elapsed.TotalSeconds);
            resizeClock.Restart();
            if (elapsedSeconds <= 0)
                elapsedSeconds = 0.016;

            double blend = 1.0 - Math.Exp(-ResizeResponsePerSecond * elapsedSeconds);
            Rectangle current = Bounds;
            Rectangle target = targetBounds;

            if (IsNear(current, target))
            {
                Bounds = target;
                resizeTimer.Stop();
                if (!dragging)
                {
                    CommitLogicalGeometry();
                    ResizeAnimationCompleted?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            int left = Approach(current.Left, target.Left, blend);
            int top = Approach(current.Top, target.Top, blend);
            int right = Approach(current.Right, target.Right, blend);
            int bottom = Approach(current.Bottom, target.Bottom, blend);

            if (right - left < MinControlDimension || bottom - top < MinControlDimension)
                return;

            Bounds = Rectangle.FromLTRB(left, top, right, bottom);
        }

        private static int Approach(int from, int to, double blend)
        {
            if (Math.Abs(to - from) <= 1)
                return to;
            int result = from + (int)Math.Round((to - from) * blend);
            return result == from ? from + Math.Sign(to - from) : result;
        }

        private static bool IsNear(Rectangle a, Rectangle b)
        {
            return Math.Abs(a.Left - b.Left) <= 1 &&
                   Math.Abs(a.Top - b.Top) <= 1 &&
                   Math.Abs(a.Right - b.Right) <= 1 &&
                   Math.Abs(a.Bottom - b.Bottom) <= 1;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        protected override void OnParentChanged(EventArgs e)
        {
            if (backgroundCanvas != null)
            {
                backgroundCanvas.Paint -= BackgroundCanvas_Paint;
                backgroundCanvas.MouseCaptureChanged -= BackgroundCanvas_MouseCaptureChanged;
                if (!backgroundCanvas.IsDisposed)
                    backgroundCanvas.Invalidate(lastVisualBounds);
            }
            base.OnParentChanged(e);
            backgroundCanvas = Parent as PictureBox;
            if (backgroundCanvas != null)
            {
                backgroundCanvas.Paint += BackgroundCanvas_Paint;
                backgroundCanvas.MouseCaptureChanged += BackgroundCanvas_MouseCaptureChanged;
                backgroundCanvas.Invalidate(Bounds);
            }
            lastVisualBounds = Bounds;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (resizeSetupComplete && !applyingZoomBounds && !moving &&
                !dragging && !resizeTimer.Enabled)
                UpdateLogicalLocation();
            // Queue the new image paint; NEVER call Update() here. Calling
            // Update() inside MouseMove enters WinForms paint/layout while a
            // captured mouse is moving and makes the image judder.
            Invalidate();
            RefreshExposedCanvas();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (resizeSetupComplete)
            {
                UpdateHandleLayout();
                if (!applyingZoomBounds && !dragging && !resizeTimer.Enabled)
                    CommitLogicalGeometry();
            }
            Invalidate();
            RefreshExposedCanvas();
        }

        private void RefreshExposedCanvas()
        {
            Rectangle current = Bounds;
            Rectangle previous = lastVisualBounds;
            lastVisualBounds = current;

            Control canvas = Parent;
            if (previous == current || canvas == null || canvas.IsDisposed ||
                !canvas.IsHandleCreated || refreshingParentCanvas)
                return;

            // The parent now owns ALL floating-image pixels: repaint the old
            // AND new position. Queue the WM_PAINT; never call Update/Refresh
            // from inside mouse movement (the cause of the v1 shaking).
            Rectangle dirty = Rectangle.Union(previous, current);
            dirty.Inflate(2, 2);
            dirty.Intersect(canvas.ClientRectangle);
            if (!dirty.IsEmpty)
                canvas.Invalidate(dirty, false);
        }

        private void UpdateHandleLayout()
        {
            if (updatingHandleLayout || pictureBoxCanvas == null)
                return;

            updatingHandleLayout = true;
            try
            {
                int width = Math.Max(MinControlDimension, Width);
                int height = Math.Max(MinControlDimension, Height);
                pictureBoxCanvas.Bounds = new Rectangle(
                    BorderPadding, BorderPadding,
                    Math.Max(1, width - BorderPadding * 2),
                    Math.Max(1, height - BorderPadding * 2));

                foreach (KeyValuePair<Control, ResizeEdges> entry in resizeHandles)
                {
                    ResizeEdges edges = entry.Value;
                    int x = (edges & ResizeEdges.Left) != 0 ? 0 :
                            (edges & ResizeEdges.Right) != 0 ? width - HandleSize :
                            (width - HandleSize) / 2;
                    int y = (edges & ResizeEdges.Top) != 0 ? 0 :
                            (edges & ResizeEdges.Bottom) != 0 ? height - HandleSize :
                            (height - HandleSize) / 2;
                    entry.Key.Bounds = new Rectangle(x, y, HandleSize, HandleSize);
                }
                UpdateInteractionRegion();
            }
            finally
            {
                updatingHandleLayout = false;
            }
        }

        private void MovingImageControl_Load(object sender, EventArgs e)
        {
            UpdateHandleLayout();
        }

        // Kept so existing designer event subscriptions still compile.
        private void resize_top_MouseDown(object sender, MouseEventArgs e) { }
        private void resize_top_MouseUp(object sender, MouseEventArgs e) { }
        private void resize_top_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_bottom_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_right_Move(object sender, EventArgs e) { }
        private void resize_left_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_bottom_right_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_top_right_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_top_left_MouseMove(object sender, MouseEventArgs e) { }
        private void resize_bottom_left_MouseMove(object sender, MouseEventArgs e) { }

        private void MovingImageControl_Disposed(object sender, EventArgs e)
        {
            if (backgroundCanvas != null)
            {
                backgroundCanvas.Paint -= BackgroundCanvas_Paint;
                backgroundCanvas.MouseCaptureChanged -= BackgroundCanvas_MouseCaptureChanged;
                if (!backgroundCanvas.IsDisposed)
                    backgroundCanvas.Invalidate(lastVisualBounds);
                backgroundCanvas = null;
            }
            resizeTimer.Stop();
            resizeTimer.Tick -= ResizeTimer_Tick;
            resizeTimer.Dispose();
            MouseDown -= MoveSurface_MouseDown;
            MouseMove -= MoveSurface_MouseMove;
            MouseUp -= MoveSurface_MouseUp;
            MouseCaptureChanged -= MoveSurface_MouseCaptureChanged;
            MouseDoubleClick -= MoveSurface_MouseDoubleClick;
        }
    }
}