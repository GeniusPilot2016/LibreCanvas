using System;
using System.Drawing;
using System.IO;
using System.Management;
using System.Media;
using System.Windows.Forms;
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

        public TakePhotoFromWebcam()
        {
            InitializeComponent();
            LoadVideoDevices();
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
            sound.SoundLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "webcam_take_photo.wav");
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
                videoSource.Dispose();
                videoSource = null;
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
