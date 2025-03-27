using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class ImageEditor : Form
    {
        Bitmap canvas = new Bitmap(800, 600);
        int x = -1, y = -1;
        bool isdrawing = false;
        Pen brushes;
        Color color1 = Color.Black, color2 = Color.White;
        int brushSize = 1, eraserSize = 1, shapeSize = 1, shapeType = 0, textSize = 9;
        bool isResizing = false;
        Point lastMousePos;
        Panel[] resizeHandles;
        public ImageEditor()
        {
            InitializeComponent();
            InitializeResizeHandles();
            pictureBoxCanvas.Image = canvas;
        }
        private void InitializeResizeHandles()
        {
            resizeHandles = new Panel[8];
            for (int i = 0; i < resizeHandles.Length; i++)
            {
                resizeHandles[i] = new Panel
                {
                    Size = new Size(10, 10),
                    BackColor = Color.Black
                };
                resizeHandles[i].MouseDown += ResizeHandle_MouseDown;
                resizeHandles[i].MouseMove += ResizeHandle_MouseMove;
                resizeHandles[i].MouseUp += ResizeHandle_MouseUp;
                Controls.Add(resizeHandles[i]);
            }

            // Set appropriate cursors for each handle
            resizeHandles[0].Cursor = Cursors.SizeNWSE; // Top-left
            resizeHandles[1].Cursor = Cursors.SizeNESW; // Top-right
            resizeHandles[2].Cursor = Cursors.SizeNESW; // Bottom-left
            resizeHandles[3].Cursor = Cursors.SizeNWSE; // Bottom-right
            resizeHandles[4].Cursor = Cursors.SizeWE;   // Middle-left
            resizeHandles[5].Cursor = Cursors.SizeWE;   // Middle-right
            resizeHandles[6].Cursor = Cursors.SizeNS;   // Top-middle
            resizeHandles[7].Cursor = Cursors.SizeNS;   // Bottom-middle

            PositionResizeHandles();
        }
        private void PositionResizeHandles()
        {
            int offset = 5;
            resizeHandles[0].Location = new Point(pictureBoxCanvas.Left - offset, pictureBoxCanvas.Top - offset); // Top-left
            resizeHandles[1].Location = new Point(pictureBoxCanvas.Right - offset, pictureBoxCanvas.Top - offset); // Top-right
            resizeHandles[2].Location = new Point(pictureBoxCanvas.Left - offset, pictureBoxCanvas.Bottom - offset); // Bottom-left
            resizeHandles[3].Location = new Point(pictureBoxCanvas.Right - offset, pictureBoxCanvas.Bottom - offset); // Bottom-right
            resizeHandles[4].Location = new Point(pictureBoxCanvas.Left - offset, pictureBoxCanvas.Top + pictureBoxCanvas.Height / 2 - offset); // Middle-left
            resizeHandles[5].Location = new Point(pictureBoxCanvas.Right - offset, pictureBoxCanvas.Top + pictureBoxCanvas.Height / 2 - offset); // Middle-right
            resizeHandles[6].Location = new Point(pictureBoxCanvas.Left + pictureBoxCanvas.Width / 2 - offset, pictureBoxCanvas.Top - offset); // Top-middle
            resizeHandles[7].Location = new Point(pictureBoxCanvas.Left + pictureBoxCanvas.Width / 2 - offset, pictureBoxCanvas.Bottom - offset); // Bottom-middle
        }

        private void ResizeHandle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isResizing = true;
                lastMousePos = e.Location;
                labelSize.Text = $"{pictureBoxCanvas.Width} X {pictureBoxCanvas.Height} px";
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                Panel handle = sender as Panel;
                int dx = e.X - lastMousePos.X;
                int dy = e.Y - lastMousePos.Y;

                if (handle == resizeHandles[0]) // Top-left
                {
                    pictureBoxCanvas.Left += dx;
                    pictureBoxCanvas.Top += dy;
                    pictureBoxCanvas.Width -= dx;
                    pictureBoxCanvas.Height -= dy;
                }
                else if (handle == resizeHandles[1]) // Top-right
                {
                    pictureBoxCanvas.Top += dy;
                    pictureBoxCanvas.Width += dx;
                    pictureBoxCanvas.Height -= dy;
                }
                else if (handle == resizeHandles[2]) // Bottom-left
                {
                    pictureBoxCanvas.Left += dx;
                    pictureBoxCanvas.Width -= dx;
                    pictureBoxCanvas.Height += dy;
                }
                else if (handle == resizeHandles[3]) // Bottom-right
                {
                    pictureBoxCanvas.Width += dx;
                    pictureBoxCanvas.Height += dy;
                }
                else if (handle == resizeHandles[4]) // Middle-left
                {
                    pictureBoxCanvas.Left += dx;
                    pictureBoxCanvas.Width -= dx;
                }
                else if (handle == resizeHandles[5]) // Middle-right
                {
                    pictureBoxCanvas.Width += dx;
                }
                else if (handle == resizeHandles[6]) // Top-middle
                {
                    pictureBoxCanvas.Top += dy;
                    pictureBoxCanvas.Height -= dy;
                }
                else if (handle == resizeHandles[7]) // Bottom-middle
                {
                    pictureBoxCanvas.Height += dy;
                }

                PositionResizeHandles();
                lastMousePos = e.Location;
            }
        }

        private void ResizeHandle_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isResizing = false;
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
        private void pictureBoxCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isResizing = true;
                lastMousePos = e.Location;
            }
        }

        private void pictureBoxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                int newWidth = pictureBoxCanvas.Width + (e.X - lastMousePos.X);
                int newHeight = pictureBoxCanvas.Height + (e.Y - lastMousePos.Y);
                pictureBoxCanvas.Size = new Size(newWidth, newHeight);
                PositionResizeHandles();
                lastMousePos = e.Location;
            }
        }

        private void pictureBoxCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isResizing = false;
            }
        }
    }
}
