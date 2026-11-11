using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Carpathia
{
    // IMPORTANT: This is a REAL TabControl, not a UserControl that contains one.
    // WinForms' built-in TabControl designer can therefore direct Toolbox drops
    // to SelectedTab.Controls and serialize them into the hosting Form.
    // Do NOT attach ParentControlDesigner or a custom GetParentForComponent here.
    [DefaultProperty("TabPages")]
    public partial class TabControlWithCloseButtons : TabControl
    {
        private const int LeftMargin = 7;
        private const int RightMargin = 7;
        private const int CloseSize = 12;
        private const int IconTextGap = 4;

        private static readonly Color HeaderTextColor = Color.FromArgb(30, 30, 30);

        private int hoveredTab = -1;
        private int hoveredCloseTab = -1;

        // SelectedIndexChanged, TabPages, SelectedTab, SelectedIndex, ImageList,
        // Alignment, TabCount and SelectTab are inherited directly from TabControl.
        public event EventHandler<TabPageEventArgs> TabAdded;
        public event EventHandler<TabPageEventArgs> TabClosed;

        public TabControlWithCloseButtons()
        {
            // Do NOT call the old UserControl InitializeComponent method.
            // Its nested tabControl1 must not be created or added here.
            Appearance = TabAppearance.Normal;
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Normal;
            Multiline = false;
            Padding = new Point(24, 4);
        }

        // Compatibility with the old wrapper's read-only property. There is
        // no inner TabControl anymore: the actual TabControl is this instance.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TabControl DesignTimeTabControl { get { return this; } }

        public void AddTab(TabPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            TabPages.Add(page);
            Invalidate();
        }

        public void AddTab(string text)
        {
            AddTab(new TabPage(text));
        }

        public void RemoveTab(TabPage page)
        {
            CloseTab(page);
        }

        public void RemoveTabAt(int index)
        {
            if (index < 0 || index >= TabCount)
                return;

            CloseTab(TabPages[index]);
        }

        private void CloseTab(TabPage page)
        {
            if (page == null || !TabPages.Contains(page))
                return;

            TabPages.Remove(page);
            TabClosed?.Invoke(this, new TabPageEventArgs(page));
            page.Dispose();

            hoveredTab = -1;
            hoveredCloseTab = -1;
            Invalidate();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            TabPage page = e.Control as TabPage;
            if (page != null)
                TabAdded?.Invoke(this, new TabPageEventArgs(page));
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            hoveredTab = -1;
            hoveredCloseTab = -1;
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            hoveredCloseTab = -1;
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        private int GetImageIndex(TabPage page)
        {
            if (ImageList == null)
                return -1;

            if (!string.IsNullOrEmpty(page.ImageKey))
                return ImageList.Images.IndexOfKey(page.ImageKey);

            return page.ImageIndex;
        }

        private int MeasureTextWidth(string value)
        {
            return TextRenderer.MeasureText(
                value,
                Font,
                new Size(10000, 1000),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }

        private void GetLayout(int index, out Rectangle iconRect,
                               out Rectangle textRect, out Rectangle closeRect)
        {
            Rectangle tab = GetTabRect(index);
            TabPage page = TabPages[index];
            int x = tab.Left + LeftMargin;
            iconRect = Rectangle.Empty;

            int imageIndex = GetImageIndex(page);
            if (ImageList != null && imageIndex >= 0 &&
                imageIndex < ImageList.Images.Count)
            {
                Size size = ImageList.ImageSize;
                iconRect = new Rectangle(
                    x, tab.Top + (tab.Height - size.Height) / 2,
                    size.Width, size.Height);
                x += size.Width + IconTextGap;
            }

            int spaceWidth = Math.Max(1,
                MeasureTextWidth("A    B") - MeasureTextWidth("AB"));
            int maxTextWidth = Math.Max(0,
                tab.Right - RightMargin - CloseSize - spaceWidth - x);
            int textWidth = Math.Min(MeasureTextWidth(page.Text), maxTextWidth);

            textRect = new Rectangle(x, tab.Top, textWidth, tab.Height);
            int closeX = Math.Min(tab.Right - RightMargin - CloseSize,
                x + textWidth + spaceWidth);
            closeRect = new Rectangle(
                closeX, tab.Top + (tab.Height - CloseSize) / 2,
                CloseSize, CloseSize);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);

            if (e.Index < 0 || e.Index >= TabCount)
                return;

            Rectangle tabRect = GetTabRect(e.Index);
            TabPage page = TabPages[e.Index];
            bool selected = e.Index == SelectedIndex;
            TabItemState state = selected
                ? TabItemState.Selected
                : e.Index == hoveredTab ? TabItemState.Hot : TabItemState.Normal;

            if (TabRenderer.IsSupported)
            {
                TabRenderer.DrawTabItem(e.Graphics, tabRect, state);
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(
                    selected ? SystemColors.Window : SystemColors.Control))
                {
                    e.Graphics.FillRectangle(brush, tabRect);
                }

                ControlPaint.DrawBorder(e.Graphics, tabRect,
                    SystemColors.ControlDark, ButtonBorderStyle.Solid);
            }

            Rectangle iconRect, textRect, closeRect;
            GetLayout(e.Index, out iconRect, out textRect, out closeRect);

            int imageIndex = GetImageIndex(page);
            if (!iconRect.IsEmpty && imageIndex >= 0 && ImageList != null)
                ImageList.Draw(e.Graphics, iconRect.X, iconRect.Y, imageIndex);

            TextRenderer.DrawText(e.Graphics, page.Text, Font, textRect,
                HeaderTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            bool closeHovered = e.Index == hoveredCloseTab;
            SmoothingMode oldMode = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                if (closeHovered)
                {
                    Rectangle hoverRect = closeRect;
                    hoverRect.Inflate(3, 3);
                    using (SolidBrush brush =
                        new SolidBrush(Color.FromArgb(220, 53, 69)))
                    {
                        e.Graphics.FillEllipse(brush, hoverRect);
                    }
                }

                using (Pen pen = new Pen(
                    closeHovered ? Color.White : SystemColors.GrayText, 1.7f))
                {
                    e.Graphics.DrawLine(pen,
                        closeRect.Left + 2, closeRect.Top + 2,
                        closeRect.Right - 2, closeRect.Bottom - 2);
                    e.Graphics.DrawLine(pen,
                        closeRect.Right - 2, closeRect.Top + 2,
                        closeRect.Left + 2, closeRect.Bottom - 2);
                }
            }
            finally
            {
                e.Graphics.SmoothingMode = oldMode;
            }
        }

        private int HitTestCloseButton(Point point)
        {
            for (int i = 0; i < TabCount; i++)
            {
                Rectangle icon, text, close;
                GetLayout(i, out icon, out text, out close);
                close.Inflate(3, 3);
                if (close.Contains(point) && GetTabRect(i).Contains(point))
                    return i;
            }

            return -1;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            // Visual Studio must own design-time page removal and persistence.
            if (e.Button != MouseButtons.Left || DesignMode ||
                LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            int index = HitTestCloseButton(e.Location);
            if (index >= 0)
                CloseTab(TabPages[index]);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int newHoveredTab = -1;
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(e.Location))
                {
                    newHoveredTab = i;
                    break;
                }
            }

            int newHoveredClose = HitTestCloseButton(e.Location);
            Cursor = newHoveredClose >= 0 ? Cursors.Hand : Cursors.Default;

            if (newHoveredTab != hoveredTab ||
                newHoveredClose != hoveredCloseTab)
            {
                hoveredTab = newHoveredTab;
                hoveredCloseTab = newHoveredClose;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoveredTab = -1;
            hoveredCloseTab = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            foreach (TabPage page in TabPages)
                page.BackColor = BackColor;
            Invalidate();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            foreach (TabPage page in TabPages)
                page.ForeColor = ForeColor;
            Invalidate();
        }
    }

    public class TabPageEventArgs : EventArgs
    {
        public TabPage TabPage { get; private set; }

        public TabPageEventArgs(TabPage page)
        {
            TabPage = page;
        }
    }
}
