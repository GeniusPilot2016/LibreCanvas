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

        private void Control_Clicked(object sender, EventArgs e)
        {
            if (sender is ToolStripItem clickedItem)
            {
                foreach (ToolStripItem item in ((ToolStrip)clickedItem.Owner).Items)
                {
                    if (item is ToolStripButton button)
                    {
                        button.Checked = item == clickedItem;
                    }
                    else if (item is ToolStripMenuItem menuItem)
                    {
                        if (clickedItem is ToolStripButton)
                        {
                            menuItem.CheckState = CheckState.Unchecked;
                        }
                        else
                        {
                            menuItem.Checked = item == clickedItem;
                        }
                    }
                }
            }
        }
    }
}
