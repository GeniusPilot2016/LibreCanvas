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
    public partial class WebcamPhotoPreview : Form
    {
        public event EventHandler PhotoAccepted;
        public event EventHandler PhotoDiscarded;

        public WebcamPhotoPreview(Image image)
        {
            InitializeComponent();
            pictureBox1.Image = image;
        }

        private void buttonAccept_Click(object sender, EventArgs e)
        {
            PhotoAccepted?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void buttonDiscard_Click(object sender, EventArgs e)
        {
            PhotoDiscarded?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void WebcamPhotoPreview_FormClosed(object sender, FormClosedEventArgs e)
        {
            PhotoDiscarded?.Invoke(this, EventArgs.Empty);
        }
    }
}
