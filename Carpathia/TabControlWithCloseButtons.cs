
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Carpathia
{
    public partial class TabControlWithCloseButtons : UserControl
    {
        private const int LeftMargin = 7;
        private const int RightMargin = 7;
        private const int CloseSize = 12;
        private const int IconTextGap = 4;

        private static readonly Color HeaderTextColor =
            Color.FromArgb(30, 30, 30);

        private int hoveredTab = -1;
        private int hoveredCloseTab = -1;

        public TabControlWithCloseButtons()
        {
            InitializeComponent();

            tabControl1.Appearance = TabAppearance.Normal;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.SizeMode = TabSizeMode.Normal;
            tabControl1.Multiline = false;

            int extra = GetFourSpaceWidth() + CloseSize + 12;

            tabControl1.Padding = new Point(
                Math.Max(6, (extra + 1) / 2),
                tabControl1.Padding.Y
            );

            tabControl1.DrawItem += tabControl1_DrawItem;
            tabControl1.MouseClick += tabControl1_MouseClick;
            tabControl1.MouseLeave += tabControl1_MouseLeave;
            tabControl1.MouseMove += tabControl1_MouseMove;

            tabControl1.SelectedIndexChanged += (s, e) =>
            {
                tabControl1.Invalidate();
                OnSelectedIndexChanged(EventArgs.Empty);
            };
        }

        // =========================================
        // DESIGNER PROPERTIES
        // =========================================

        [Category("Appearance")]
        [Description("The ImageList used for tab icons.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList ImageList
        {
            get => tabControl1.ImageList;
            set
            {
                tabControl1.ImageList = value;
                tabControl1.Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The collection of tab pages.")]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Content)]
        [Editor(
            "System.Windows.Forms.Design.TabPageCollectionEditor, System.Design",
            typeof(System.Drawing.Design.UITypeEditor))]
        public TabControl.TabPageCollection TabPages
        {
            get => tabControl1.TabPages;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public TabPage SelectedTab
        {
            get => tabControl1.SelectedTab;
            set => tabControl1.SelectedTab = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => tabControl1.SelectedIndex;
            set => tabControl1.SelectedIndex = value;
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TabSizeMode SizeMode
        {
            get => tabControl1.SizeMode;
            set => tabControl1.SizeMode = value;
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TabAlignment Alignment
        {
            get => tabControl1.Alignment;
            set => tabControl1.Alignment = value;
        }

        // =========================================
        // EVENTS
        // =========================================

        public event EventHandler SelectedIndexChanged;

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        // =========================================
        // TAB METHODS
        // =========================================

        public void AddTab(TabPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            tabControl1.TabPages.Add(page);
            tabControl1.Invalidate();
        }

        public void RemoveTab(TabPage page)
        {
            if (page == null)
                return;

            tabControl1.TabPages.Remove(page);
            tabControl1.Invalidate();
        }

        public void RemoveTabAt(int index)
        {
            if (index < 0 || index >= tabControl1.TabPages.Count)
                return;

            TabPage page = tabControl1.TabPages[index];

            tabControl1.TabPages.RemoveAt(index);
            page.Dispose();

            tabControl1.Invalidate();
        }

        public void SelectTab(TabPage page)
        {
            tabControl1.SelectTab(page);
        }

        public void SelectTab(int index)
        {
            tabControl1.SelectTab(index);
        }

        // =========================================
        // TEXT MEASUREMENT
        // =========================================

        private int MeasureTextWidth(string text)
        {
            return TextRenderer.MeasureText(
                text,
                tabControl1.Font,
                new Size(10000, 1000),
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine
            ).Width;
        }

        private int GetFourSpaceWidth()
        {
            return Math.Max(
                1,
                MeasureTextWidth("A    B") -
                MeasureTextWidth("AB")
            );
        }

        // =========================================
        // IMAGE LIST
        // =========================================

        private int GetImageIndex(TabPage page)
        {
            if (tabControl1.ImageList == null)
                return -1;

            if (!string.IsNullOrEmpty(page.ImageKey))
            {
                return tabControl1.ImageList.Images.IndexOfKey(
                    page.ImageKey
                );
            }

            return page.ImageIndex;
        }

        // =========================================
        // TAB LAYOUT
        // =========================================

        private void GetLayout(
            int index,
            out Rectangle iconRect,
            out Rectangle textRect,
            out Rectangle closeRect)
        {
            Rectangle tab = tabControl1.GetTabRect(index);
            TabPage page = tabControl1.TabPages[index];

            int x = tab.Left + LeftMargin;

            iconRect = Rectangle.Empty;

            int imageIndex = GetImageIndex(page);

            if (tabControl1.ImageList != null &&
                imageIndex >= 0 &&
                imageIndex < tabControl1.ImageList.Images.Count)
            {
                Size imageSize = tabControl1.ImageList.ImageSize;

                iconRect = new Rectangle(
                    x,
                    tab.Top + (tab.Height - imageSize.Height) / 2,
                    imageSize.Width,
                    imageSize.Height
                );

                x += imageSize.Width + IconTextGap;
            }

            int spaceWidth = GetFourSpaceWidth();
            int desiredWidth = MeasureTextWidth(page.Text);

            int maxTextWidth = Math.Max(
                0,
                tab.Right - RightMargin -
                CloseSize - spaceWidth - x
            );

            int textWidth = Math.Min(
                desiredWidth,
                maxTextWidth
            );

            textRect = new Rectangle(
                x,
                tab.Top,
                textWidth,
                tab.Height
            );

            closeRect = new Rectangle(
                x + textWidth + spaceWidth,
                tab.Top + (tab.Height - CloseSize) / 2,
                CloseSize,
                CloseSize
            );
        }

        // =========================================
        // DRAW TAB
        // =========================================

        private void tabControl1_DrawItem(
            object sender,
            DrawItemEventArgs e)
        {
            if (e.Index < 0 ||
                e.Index >= tabControl1.TabCount)
                return;

            Rectangle tabRect = tabControl1.GetTabRect(e.Index);
            TabPage page = tabControl1.TabPages[e.Index];

            bool selected = e.Index == tabControl1.SelectedIndex;

            TabItemState state = selected
                ? TabItemState.Selected
                : e.Index == hoveredTab
                    ? TabItemState.Hot
                    : TabItemState.Normal;

            if (TabRenderer.IsSupported)
            {
                TabRenderer.DrawTabItem(
                    e.Graphics,
                    tabRect,
                    state
                );
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(
                    selected
                        ? SystemColors.Window
                        : SystemColors.Control))
                {
                    e.Graphics.FillRectangle(brush, tabRect);
                }

                ControlPaint.DrawBorder(
                    e.Graphics,
                    tabRect,
                    SystemColors.ControlDark,
                    ButtonBorderStyle.Solid
                );
            }

            Rectangle iconRect;
            Rectangle textRect;
            Rectangle closeRect;

            GetLayout(
                e.Index,
                out iconRect,
                out textRect,
                out closeRect
            );

            int imageIndex = GetImageIndex(page);

            if (!iconRect.IsEmpty &&
                imageIndex >= 0 &&
                tabControl1.ImageList != null)
            {
                tabControl1.ImageList.Draw(
                    e.Graphics,
                    iconRect.X,
                    iconRect.Y,
                    imageIndex
                );
            }

            if (textRect.Width > 0)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    page.Text,
                    tabControl1.Font,
                    textRect,
                    HeaderTextColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding
                );
            }

            bool closeHovered = e.Index == hoveredCloseTab;

            SmoothingMode oldMode = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (closeHovered)
            {
                Rectangle hoverRect = closeRect;
                hoverRect.Inflate(3, 3);

                using (SolidBrush brush = new SolidBrush(
                    Color.FromArgb(220, 53, 69)))
                {
                    e.Graphics.FillEllipse(brush, hoverRect);
                }
            }

            using (Pen pen = new Pen(
                closeHovered
                    ? Color.White
                    : SystemColors.GrayText,
                1.7f))
            {
                e.Graphics.DrawLine(
                    pen,
                    closeRect.Left + 2,
                    closeRect.Top + 2,
                    closeRect.Right - 2,
                    closeRect.Bottom - 2
                );

                e.Graphics.DrawLine(
                    pen,
                    closeRect.Right - 2,
                    closeRect.Top + 2,
                    closeRect.Left + 2,
                    closeRect.Bottom - 2
                );
            }

            e.Graphics.SmoothingMode = oldMode;
        }

        // =========================================
        // CLOSE BUTTON HIT TEST
        // =========================================

        private int HitTestCloseButton(Point point)
        {
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                Rectangle icon;
                Rectangle text;
                Rectangle close;

                GetLayout(i, out icon, out text, out close);

                close.Inflate(3, 3);

                if (close.Contains(point) &&
                    tabControl1.GetTabRect(i).Contains(point))
                {
                    return i;
                }
            }

            return -1;
        }

        // =========================================
        // MOUSE CLICK
        // =========================================

        private void tabControl1_MouseClick(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            int index = HitTestCloseButton(e.Location);

            if (index < 0)
                return;

            TabPage page = tabControl1.TabPages[index];

            hoveredTab = -1;
            hoveredCloseTab = -1;

            tabControl1.TabPages.Remove(page);
            page.Dispose();

            tabControl1.Invalidate();
        }

        // =========================================
        // MOUSE MOVE
        // =========================================

        private void tabControl1_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            int newHoveredTab = -1;

            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(e.Location))
                {
                    newHoveredTab = i;
                    break;
                }
            }

            int newHoveredClose = HitTestCloseButton(e.Location);

            tabControl1.Cursor = newHoveredClose >= 0
                ? Cursors.Hand
                : Cursors.Default;

            if (newHoveredTab != hoveredTab ||
                newHoveredClose != hoveredCloseTab)
            {
                hoveredTab = newHoveredTab;
                hoveredCloseTab = newHoveredClose;
                tabControl1.Invalidate();
            }
        }

        // =========================================
        // MOUSE LEAVE
        // =========================================

        private void tabControl1_MouseLeave(
            object sender,
            EventArgs e)
        {
            hoveredTab = -1;
            hoveredCloseTab = -1;

            tabControl1.Cursor = Cursors.Default;
            tabControl1.Invalidate();
        }

        // =========================================
        // BACK COLOR
        // =========================================

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            if (tabControl1 == null)
                return;

            tabControl1.BackColor = BackColor;

            foreach (TabPage page in tabControl1.TabPages)
                page.BackColor = BackColor;

            tabControl1.Invalidate();
            Invalidate();
        }

        // =========================================
        // FORE COLOR
        // =========================================

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            if (tabControl1 == null)
                return;

            tabControl1.ForeColor = ForeColor;

            foreach (TabPage page in tabControl1.TabPages)
                page.ForeColor = ForeColor;

            tabControl1.Invalidate();
            Invalidate();
        }
    }
}
