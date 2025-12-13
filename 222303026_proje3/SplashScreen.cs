// ArtFusion - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace _222303026_proje3
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            SetFonts();
            labelVersion.Text = $"Version {GetInformations.GetVersionAndStatus().version} {GetInformations.GetVersionAndStatus().status}\r\n";
            AddShadowToBackOfForm();
            RoundCornersOfForm();
        }
        private void SetFonts()
        {
            UIFonts uiFonts = UIFonts.Instance;
            foreach (Control control in this.Controls)
            {
                control.Font = uiFonts.SetUIFont(control.Font.Size, control.Font.Style);
            }
        }

        private void SplashScreen_Shown(object sender, EventArgs e)
        {
            progressTimer.Start();
        }

        private async void progressTimer_Tick(object sender, EventArgs e)
        {
            if (progressTimer.Enabled)
            {
                progressBar1.Increment(2);
                if (progressBar1.Value >= progressBar1.Maximum)
                {
                    progressTimer.Stop();
                    labelStatus.Text = "ArtFusion is started.";
                    await Task.Delay(1000); // Small delay to show the final status update
                    this.Hide();
                    RemoveShadowFromBackOfForm(); // Remove shadow before closing to prevent visual artifacts
                    switch (Settings1.Default.ShowRecentFiles) // Check user setting
                    {
                        case true:
                            MainForm mainForm = new MainForm(); // Open MainForm with recent files
                            mainForm.Show();
                            break;
                        case false:
                            ImageEditor editor = new ImageEditor(); // Open ImageEditor directly
                            editor.Show();
                            break;
                    }
                    
                }
            }
        }
        Point mouseDownScreenPoint = Point.Empty;
        Point formStartLocation = Point.Empty;
        bool isDragging = false;

        private void splash_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Start dragging
                isDragging = true;
                mouseDownScreenPoint = MousePosition;
                formStartLocation = this.Location;
            }
        }

        private void splash_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // Use Cursor.Position for better reliability
                Point currentMousePosition = Cursor.Position;

                // Calculate the difference
                Point diff = new Point(
                    currentMousePosition.X - mouseDownScreenPoint.X,
                    currentMousePosition.Y - mouseDownScreenPoint.Y
                );

                // Update the form's location
                this.Location = new Point(
                    formStartLocation.X + diff.X,
                    formStartLocation.Y + diff.Y
                );
            }
        }

        private void splash_MouseUp(object sender, MouseEventArgs e)
        {
            // Stop dragging
            if (e.Button == MouseButtons.Left)
            {
                StopDragging();
            }
        }

        private void splash_Deactivate(object sender, EventArgs e)
        {
            StopDragging();
        }
        private void StopDragging()
        {
            isDragging = false;
        }
        public const int GCL_STYLE = -26;
        public const int CS_DROPSHADOW = 0x00020000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetClassLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetClassLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void AddShadowToBackOfForm()
        {
            int classStyle = GetClassLong(this.Handle, GCL_STYLE);
            classStyle |= CS_DROPSHADOW;
            SetClassLong(this.Handle, GCL_STYLE, classStyle);
        }
        private void RemoveShadowFromBackOfForm()
        {
            int classStyle = GetClassLong(this.Handle, GCL_STYLE);
            classStyle &= ~CS_DROPSHADOW;
            SetClassLong(this.Handle, GCL_STYLE, classStyle);
        }
        private void RoundCornersOfForm()
        {
            int dpi = (int)(96 * this.DeviceDpi / 96f); // Get the current DPI
            int radius = 15 * dpi / 96; // Adjust radius based on DPI
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, radius, radius), 180, 90); // Top-left corner
            path.AddArc(new Rectangle(this.Width - radius, 0, radius, radius), 270, 90); // Top-right corner
            path.AddArc(new Rectangle(this.Width - radius, this.Height - radius, radius, radius), 0, 90); // Bottom-right corner
            path.AddArc(new Rectangle(0, this.Height - radius, radius, radius), 90, 90); // Bottom-left corner
            path.CloseFigure();
            this.Region = new Region(path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close(); // Close the form
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; // Minimize the form
        }
    }
}
