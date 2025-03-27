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
        public ImageEditor()
        {
            InitializeComponent();
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
    }
}
