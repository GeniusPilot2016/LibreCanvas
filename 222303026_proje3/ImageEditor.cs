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
        int brushSize = 1, eraserSize = 1, shapeSize = 1, shapeType = 0, textSize = 9;
        bool isResizing = false;
        private ResizeDirection resizeDirection;
        private Point lastMousePos;

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
            InitializeComponent();
            fontFamilies = installedFontCollection.Families;
            for (int i = 0; i < fontFamilies.Length; i++)
            {
                fontsComboBox.Items.Add(fontFamilies[i].Name);
            }
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

                // Panelin her zaman ortalanmasý için konumunu yeniden hesaplýyoruz
                int centerX = (this.ClientSize.Width - canvasPanel.Width) / 2;
                int centerY = (this.ClientSize.Height - canvasPanel.Height) / 2;

                // Paneli merkezi konumda yerleþtiriyoruz
                canvasPanel.Location = new Point(centerX, centerY);

                // Paneli hemen yenile (Refresh kullan)
                canvasPanel.Refresh();

                lastMousePos = e.Location;
                labelSize.Text = $"{canvasPanel.Width} X {canvasPanel.Height}px";
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
                currentTool = drawType[8];
            }
        }

        private void roundToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (roundToolStripMenuItem1.Checked)
            {
                currentTool = drawType[9];
            }
        }

        private void rectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (rectangleToolStripMenuItem.Checked)
            {
                currentTool = drawType[10];
            }
        }

        private void roundedRectangleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (roundedRectangleToolStripMenuItem.Checked)
            {
                currentTool = drawType[11];
            }
        }

        private void triangleToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (triangleToolStripMenuItem1.Checked)
            {
                currentTool = drawType[12];
            }
        }

        private void hexagonToolStripMenuItem1_CheckedChanged(object sender, EventArgs e)
        {
            if (hexagonToolStripMenuItem1.Checked)
            {
                currentTool = drawType[13];
            }
        }

        private void colorDropTool_CheckedChanged(object sender, EventArgs e)
        {
            if (colorDropTool.Checked)
            {
                currentTool = drawType[15];
            }
        }
    }
}
