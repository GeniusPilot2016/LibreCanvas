using System;
using System.Drawing;
using System.IO;
using System.Management;
using System.Media;
using System.Windows.Forms;
using _222303026_proje3.Properties;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace _222303026_proje3
{
    public partial class TakePhotoFromWebcam : Form
    {
        public event EventHandler<ImageAcceptedEventArgs> PhotoAccepted;

        SoundPlayer sound = new SoundPlayer();
        VideoCapture videoSource;
        Image<Bgr, Byte> takenPicture;
        int previousIndex = -1;
        public TakePhotoFromWebcam()
        {
            InitializeComponent();
            LoadVideoDevices();
            SetTheme();
            if (comboBox1.Items.Count == 0)
            {
                this.Close();
                MessageBox.Show("No webcams found. Please connect a webcam and try again.", "No Webcam Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
            this.ForeColor = Form.DefaultForeColor;
            comboBox1.BackColor = ComboBox.DefaultBackColor;
            comboBox1.ForeColor = ComboBox.DefaultForeColor;
            button1.BackColor = Color.Transparent;
        }
        private void DarkTheme()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            comboBox1.BackColor = Color.Black;
            comboBox1.ForeColor = Color.White;
            button1.BackColor = Color.FromArgb(32, 32, 32);
        }
        private void LoadVideoDevices()
        {
            DsDevice[] systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            foreach (DsDevice camera in systemCameras)
            {
                comboBox1.Items.Add(camera.Name);
            }

            if (comboBox1.Items.Count > 0)
            {
                previousIndex = 0;
                comboBox1.SelectedIndex = 0;
                StartVideoCapture(comboBox1.SelectedIndex);
            }
        }

        private void StartVideoCapture(int deviceIndex)
        {
            DsDevice[] systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (deviceIndex >= 0 && deviceIndex < systemCameras.Length)
            {
                videoSource = new VideoCapture(deviceIndex, VideoCapture.API.DShow);

                // Çözünürlüğü ayarlayın (örneğin, 1280x720)
                videoSource.Set(CapProp.FrameWidth, 1280);
                videoSource.Set(CapProp.FrameHeight, 720);

                videoSource.ImageGrabbed += new EventHandler(ProcessFrame);
                videoSource.Start();
            }
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (videoSource != null && videoSource.IsOpened)
            {
                Mat frame = new Mat();
                videoSource.Retrieve(frame);
                pictureBox1.Image = frame.ToImage<Bgr, Byte>().ToBitmap();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Convert the byte[] to a temporary file path and use it as the SoundLocation
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllBytes(tempFilePath, Resources.webcam_take_photo);
            sound.SoundLocation = tempFilePath;
            sound.Play();
            if (pictureBox1.Image != null)
            {
                using (Bitmap bitmap = new Bitmap(pictureBox1.Image))
                {
                    takenPicture = bitmap.ToImage<Bgr, Byte>();
                }
            }
            StopVideoCapture();
            WebcamPhotoPreview webcamPhotoPreview = new WebcamPhotoPreview(takenPicture.ToBitmap());
            webcamPhotoPreview.PhotoAccepted += (s, args) =>
            {
                PhotoAccepted?.Invoke(this, new ImageAcceptedEventArgs(takenPicture.ToBitmap()));
                this.Close();
            };
            webcamPhotoPreview.PhotoDiscarded += (s, args) =>
            {
                StartVideoCapture(comboBox1.SelectedIndex);
            };
            webcamPhotoPreview.ShowDialog();
        }
        private void StopVideoCapture()
        {
            if (videoSource != null)
            {
                videoSource.ImageGrabbed -= ProcessFrame;
                videoSource.Stop();
                if (videoSource != null && videoSource.IsOpened)
                {
                    videoSource.Dispose();
                    videoSource = null;
                }
            }   
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            StopVideoCapture();
            StartVideoCapture(comboBox1.SelectedIndex);
        }

        private void TakePhotoFromWebcam_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopVideoCapture();
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != previousIndex)
            {
                previousIndex = comboBox1.SelectedIndex;
                StopVideoCapture();
                StartVideoCapture(comboBox1.SelectedIndex);
            }
        }
    }

    public class ImageAcceptedEventArgs : EventArgs
    {
        public Bitmap AcceptedImage { get; }

        public ImageAcceptedEventArgs(Bitmap acceptedImage)
        {
            AcceptedImage = acceptedImage;
        }
    }

}
