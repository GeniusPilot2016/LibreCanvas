using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Carpathia
{
    /// <summary>
    /// An image control with eight animated resize handles. Resizing changes the
    /// control's bounds; it never destroys or resamples the source image.
    /// Requires pictureBoxCanvas in MovingImageControl.Designer.cs.
    /// Existing resize_* designer controls are reused when present, or created
    /// automatically when absent.
    /// </summary>
    public partial class MovingImageControl : UserControl
    {
        private const int BorderPadding = 4;
        private const int HandleSize = 8;
        private const int MinControlDimension = 32;
        private const int MaxControlDimension = 16008;

        // A value around 18 gives a responsive ease-out without sudden jumps.
        // Timer cadence is approximately 60 fps, but elapsed time determines speed.
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

        // The original image-space geometry never changes when the canvas zooms.
        // The 4 px border is SCREEN-space padding, applied AFTER scaling.
        private float zoomFactor = 1f;
        private SizeF logicalContentSize;
        private PointF logicalImageLocation;
        private bool applyingZoomBounds;

        private bool moving;
        // Track the previous visible area so dragging reveals the freshly painted
        // canvas, instead of leaving fragments of the image at old positions.
        private Rectangle lastVisualBounds;
        private bool refreshingParentCanvas;
        private Control activeMoveSurface;
        private Point moveStartScreen;
        private Point moveStartLocation;

        /// <summary>
        /// Fires once when a user finishes dragging the image.
        /// </summary>
        public event EventHandler MoveCompleted;

        /// <summary>
        /// Display zoom (0.05 to 5). At every zoom the frame adds 4 px per side.
        /// Set this when the parent canvas zoom changes.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return zoomFactor; }
            set { ApplyZoom(value); }
        }

        /// <summary>Content dimensions in original/unzoomed image pixels.</summary>
        [Browsable(false)]
        public SizeF UnzoomedContentSize { get { return logicalContentSize; } }

        /// <summary>
        /// Original-image coordinates of the TOP-LEFT of the displayed image
        /// (not the outer resize border). Useful for zoom animation previews.
        /// </summary>
        [Browsable(false)]
        public PointF UnzoomedImageLocation { get { return logicalImageLocation; } }

        [Browsable(false)]
        public Image DisplayImage { get { return pictureBoxCanvas.Image; } }

        /// <summary>
        /// Raised after an animated resize reaches its exact final bounds.
        /// </summary>
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

            // Match the original +8 px border, subject to room for the handles.
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

            // PictureBox owns the image area; mouse dragging there moves the
            // whole floating object instead of interfering with resize handles.
            pictureBoxCanvas.Cursor = Cursors.SizeAll;
            pictureBoxCanvas.MouseDown += MoveSurface_MouseDown;
            pictureBoxCanvas.MouseMove += MoveSurface_MouseMove;
            pictureBoxCanvas.MouseUp += MoveSurface_MouseUp;
            pictureBoxCanvas.MouseCaptureChanged += MoveSurface_MouseCaptureChanged;
            MouseDown += MoveSurface_MouseDown;
            MouseMove += MoveSurface_MouseMove;
            MouseUp += MoveSurface_MouseUp;
            MouseCaptureChanged += MoveSurface_MouseCaptureChanged;

            resizeTimer.Interval = 16;
            resizeTimer.Tick += ResizeTimer_Tick;
            Disposed += MovingImageControl_Disposed;
            targetBounds = Bounds;
            lastVisualBounds = Bounds;
            UpdateHandleLayout();
        }

        /// <summary>
        /// Synchronize the image object with its parent canvas's zoom, without
        /// losing the unzoomed size/position after repeated zoom-in/out cycles.
        /// </summary>
        public void ApplyZoom(float newZoom)
        {
            if (float.IsNaN(newZoom) || float.IsInfinity(newZoom) ||
                newZoom < 0.05f || newZoom > 5f)
                throw new ArgumentOutOfRangeException(nameof(newZoom),
                    "Zoom must be between 0.05 and 5.");
            if (Math.Abs(zoomFactor - newZoom) < 0.000001f)
                return;

            // Finish an active resize before changing coordinate systems.
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

        /// <summary>
        /// Position the top-left IMAGE pixel at the given location in the
        /// parent PictureBox's CURRENT zoomed coordinate system.
        /// </summary>
        public void SetImageDisplayLocation(Point imageTopLeft)
        {
            Location = new Point(imageTopLeft.X - BorderPadding,
                                 imageTopLeft.Y - BorderPadding);
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

        /// <summary>
        /// Stop any mouse gesture and finish its animation before compositing
        /// this floating image into the underlying document layer.
        /// </summary>
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

        private void MoveSurface_MouseDown(object sender, MouseEventArgs e)
        {
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
            Location = new Point(
                moveStartLocation.X + p.X - moveStartScreen.X,
                moveStartLocation.Y + p.Y - moveStartScreen.Y);
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
            // The names are the same as those in the existing designer.
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
                // All coordinates in UpdateHandleLayout are relative to this control.
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

            // The older designer event handlers remain as compatibility stubs below.
            // Subscribe to this one shared implementation instead.
            handle.MouseDown += ResizeHandle_MouseDown;
            handle.MouseMove += ResizeHandle_MouseMove;
            handle.MouseUp += ResizeHandle_MouseUp;
            handle.MouseCaptureChanged += ResizeHandle_MouseCaptureChanged;
        }

        private void ResizeHandle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !(sender is Control handle) ||
                !resizeHandles.TryGetValue(handle, out ResizeEdges edges))
                return;

            // Snap any previous animation to its exact endpoint before
            // starting a new drag; keep the opposite resize edge stationary.
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

        private void UpdateTargetFromCursor()
        {
            int dx = Cursor.Position.X - dragStartScreen.X;
            int dy = Cursor.Position.Y - dragStartScreen.Y;
            int left = dragStartBounds.Left;
            int top = dragStartBounds.Top;
            int right = dragStartBounds.Right;
            int bottom = dragStartBounds.Bottom;

            // Opposite edges remain fixed. Using the initial SCREEN cursor position
            // avoids drift when an animated handle itself moves under the cursor.
            if ((activeEdges & ResizeEdges.Left) != 0)
                left = Clamp(dragStartBounds.Left + dx,
                             right - MaxControlDimension,
                             right - MinControlDimension);
            if ((activeEdges & ResizeEdges.Right) != 0)
                right = Clamp(dragStartBounds.Right + dx,
                              left + MinControlDimension,
                              left + MaxControlDimension);
            if ((activeEdges & ResizeEdges.Top) != 0)
                top = Clamp(dragStartBounds.Top + dy,
                            bottom - MaxControlDimension,
                            bottom - MinControlDimension);
            if ((activeEdges & ResizeEdges.Bottom) != 0)
                bottom = Clamp(dragStartBounds.Bottom + dy,
                               top + MinControlDimension,
                               top + MaxControlDimension);

            Rectangle next = Rectangle.FromLTRB(left, top, right, bottom);
            if (targetBounds == next)
                return;

            targetBounds = next;
            StartResizeAnimation();
        }

        private void ResizeHandle_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !dragging ||
                !ReferenceEquals(sender, activeHandle))
                return;

            // Capture the exact mouse release position before stopping the drag.
            UpdateTargetFromCursor();
            EndDrag();
        }

        private void ResizeHandle_MouseCaptureChanged(object sender, EventArgs e)
        {
            // Losing capture (Alt+Tab, etc.) should not leave the editor stuck
            // in drag mode. On normal MouseUp, EndDrag already cleared dragging.
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

            // The final few pixels remain animated, but commit even when the
            // current animated frame has already reached the release target.
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

            // Time-based exponential ease-out, independent of system timer jitter.
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

            // Interpolating the edges, rather than Location and Size separately,
            // prevents opposite-edge jitter when resizing from left/top handles.
            if (right - left < MinControlDimension || bottom - top < MinControlDimension)
            {
                // Extremely small intermediate rounding: keep the previous valid
                // bounds until the next frame rather than invert a rectangle.
                return;
            }

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
            base.OnParentChanged(e);
            lastVisualBounds = Bounds;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (resizeSetupComplete && !applyingZoomBounds && !moving &&
                !dragging && !resizeTimer.Enabled)
                UpdateLogicalLocation();
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
            RefreshExposedCanvas();
        }

        private void RefreshExposedCanvas()
        {
            Rectangle current = Bounds;
            Rectangle previous = lastVisualBounds;
            lastVisualBounds = current;

            // The parent PictureBox already owns the up-to-date layer composite.
            // Invalidating only the dirty union repaints exposed pixels immediately
            // without rebuilding or allocating an entire composite per MouseMove.
            Control canvas = Parent;
            if (canvas == null || canvas.IsDisposed || !canvas.IsHandleCreated ||
                refreshingParentCanvas)
                return;

            Rectangle dirty = Rectangle.Union(previous, current);
            dirty.Inflate(8, 8); // cover antialiasing and the 4px handle border
            dirty.Intersect(canvas.ClientRectangle);
            if (dirty.IsEmpty)
                return;

            refreshingParentCanvas = true;
            try
            {
                canvas.Invalidate(dirty, false);
                canvas.Update();
            }
            finally
            {
                refreshingParentCanvas = false;
            }
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

        // Keep the original method names/signatures so existing designer event
        // subscriptions still compile. The fully implemented handlers are bound
        // once to all eight handles by SetupResizeHandles() above.
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

        // Do not override Dispose here: WinForms designer already generates that
        // override in MovingImageControl.Designer.cs.
        private void MovingImageControl_Disposed(object sender, EventArgs e)
        {
            resizeTimer.Stop();
            resizeTimer.Tick -= ResizeTimer_Tick;
            resizeTimer.Dispose();
            pictureBoxCanvas.MouseDown -= MoveSurface_MouseDown;
            pictureBoxCanvas.MouseMove -= MoveSurface_MouseMove;
            pictureBoxCanvas.MouseUp -= MoveSurface_MouseUp;
            pictureBoxCanvas.MouseCaptureChanged -= MoveSurface_MouseCaptureChanged;
            MouseDown -= MoveSurface_MouseDown;
            MouseMove -= MoveSurface_MouseMove;
            MouseUp -= MoveSurface_MouseUp;
            MouseCaptureChanged -= MoveSurface_MouseCaptureChanged;
        }
    }
}
