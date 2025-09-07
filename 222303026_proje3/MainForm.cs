using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ThemeManager.RegisterForm(this); 
            SetTheme();
            SetFonts();
            if (Settings1.Default.RecentFiles == null)
            {
                Settings1.Default.RecentFiles = new System.Collections.Specialized.StringCollection();
            }
            if (Settings1.Default.RecentFiles.Count > 0)
            {
                listBox1.Items.Clear();
                foreach (var file in Settings1.Default.RecentFiles)
                {
                    listBox1.Items.Add(file);
                }
                listBox1.Enabled = true;
            }
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
            foreach (Control control in Controls)
            {
                control.Font = uiFonts.SetUIFont(control.Font.Size, control.Font.Style);
                foreach(Control logoParts in panel1.Controls)
                {
                    logoParts.Font = uiFonts.SetUIFont(logoParts.Font.Size, logoParts.Font.Style);
                }
            }
            contextMenuStrip1.Font = uiFonts.SetUIFont(contextMenuStrip1.Font.Size, contextMenuStrip1.Font.Style);
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
            foreach(Button buttons in this.Controls.OfType<Button>())
            {
                buttons.BackColor = Color.Transparent;
                buttons.ForeColor = Button.DefaultForeColor;
            }
            label1.ForeColor = Form.DefaultForeColor;
            listBox1.BackColor = SystemColors.Window;
            listBox1.ForeColor = ListBox.DefaultForeColor;
            TitleBarHelper.ApplyCustomTitleBar(this, false);
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            foreach (Button buttons in this.Controls.OfType<Button>())
            {
                buttons.BackColor = Color.FromArgb(32, 32, 32);
                buttons.ForeColor = Color.White;
            }
            label1.ForeColor = Color.White;
            listBox1.BackColor = Color.Black;
            listBox1.ForeColor = Color.White;
            TitleBarHelper.ApplyCustomTitleBar(this, true);
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "PNG Files|*.png|JPEG Files|*.jpg|Bitmap Files|*.bmp";
                DialogResult dialogResult = openFileDialog1.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    string file = openFileDialog1.FileName;
                    ImageEditor imageEditor = new ImageEditor(file);
                    if (Settings1.Default.RecentFiles.Contains(file) == false)
                    {
                        Settings1.Default.RecentFiles.Add(file);
                        Settings1.Default.Save();
                    }
                    this.Hide();
                    imageEditor.Show();
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("The selected file was not found. Please check the file path.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the file: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Düğmenin ortasını hesapla
            Point buttonCenter = new Point(button1.Width / 2, button1.Height / 2);
            // Düğmenin ekran üzerindeki konumunu al
            Point screenPoint = button1.PointToScreen(buttonCenter);
            // ContextMenuStrip'i düğmenin ortasında göster
            contextMenuStrip1.Show(screenPoint);
        }

        private void createFileFromScratchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageEditor imageEditor = new ImageEditor();
            this.Hide();
            imageEditor.Show();
        }

        private void createWithAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateWithAIForm createWithAIForm = new CreateWithAIForm();
            createWithAIForm.ShowDialog();
            var generatedImage = createWithAIForm.GetGeneratedImage();
            if (generatedImage != null)
            {
                ImageEditor imageEditor = new ImageEditor(generatedImage, true);
                this.Hide(); 
                imageEditor.Show();
            }
        }

        private void createWithWebcamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TakePhotoFromWebcam takePhotoFromWebcam = new TakePhotoFromWebcam();
            takePhotoFromWebcam.PhotoAccepted += TakePhotoFromWebcam_PhotoAccepted;
            takePhotoFromWebcam.ShowDialog();
        }

        private void TakePhotoFromWebcam_PhotoAccepted(object sender, ImageAcceptedEventArgs e)
        {
            // Create an instance of ImageEditor with the accepted image
            ImageEditor imageEditor = new ImageEditor(e.AcceptedImage, false);

            // Hide the MainForm
            this.Hide();

            // Show the ImageEditor
            imageEditor.Show();
        }

        private void listBox1_Click(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.SelectedItem != null)
                {
                    string selectedFile = listBox1.SelectedItem.ToString();
                    ImageEditor imageEditor = new ImageEditor(selectedFile);
                    this.Hide();
                    imageEditor.Show();
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("The selected file was not found. Please check the file path.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Settings1.Default.RecentFiles.Remove(listBox1.SelectedItem.ToString());
                if (Settings1.Default.RecentFiles.Count == 0)
                {
                    listBox1.Items.Clear();
                    listBox1.Enabled = false;
                    listBox1.Items.Add("There's no recently opened file");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the file: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
    }
}
