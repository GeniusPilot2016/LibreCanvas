// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
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

using Carpathia;
using System.Runtime.InteropServices;

public static class UIHelper
{
    // The feauture that I added to change the title bar color of the form after graduated from my school.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool LockWindowUpdate(IntPtr hWndLock);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_9586 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    private static int cachedColorRef = -1;
    private static Color cachedColor;

    public static void ApplyCustomTitleBar(Form form, bool darkMode)
    {
        if (!(form.IsDisposed || form.Disposing))
        {
            // Suspend layout to prevent flickering
            form.SuspendLayout();
            Color color = darkMode ? Color.Black : Color.White;
            try
            {
                // Set title bar color
                if (cachedColorRef == -1 || cachedColor != color)
                {
                    cachedColor = color;
                    cachedColorRef = ColorTranslator.ToWin32(color);
                }

                DwmSetWindowAttribute(form.Handle, 35, ref cachedColorRef, 4); // DWMWA_CAPTION_COLOR

                // Enable dark mode (if desired)
                if (darkMode)
                {
                    if (Environment.OSVersion.Version < new Version(10, 0, 18362))
                    {
                        var useImmersiveDarkMode = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_9586;
                        DwmSetWindowAttribute(form.Handle, useImmersiveDarkMode, ref cachedColorRef, 4);
                    }
                    else
                    {
                        var useImmersiveDarkMode = DWMWA_USE_IMMERSIVE_DARK_MODE;
                        int value = 1;
                        DwmSetWindowAttribute(form.Handle, useImmersiveDarkMode, ref value, 4);
                    }
                }

                //Update the window to reflect changes
                SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
            finally
            {
                form.ResumeLayout(false);
                form.Refresh();
            }
        }
    }
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_IMMERSIVE_DARK_MODE = 20;
    public static void SetFormBackgroundFluent(Form form, bool darkMode)
    {
        if (!OperatingSystem.IsWindows() || Environment.OSVersion.Version.Major < 10 || Environment.OSVersion.Version.Build < 22523)
            return;
        if (form == null || form.IsDisposed || form.Disposing) return;
        if (!form.IsHandleCreated)
            form.CreateControl();

        // Set the backdrop type to mica 
        int dark = darkMode ? 1 : 0;
        int backdropType = 2;
        DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, 4);
        DwmSetWindowAttribute(form.Handle, DWMWA_IMMERSIVE_DARK_MODE, ref dark, 4);
        DwmSetWindowAttribute(form.Handle, 35, ref cachedColorRef, 4);

        // Set the form's BackColor to a darker shade to enhance the acrylic effect, especially in dark mode. The TransparencyKey is set to the same color to make it fully transparent.
        form.BackColor = darkMode ? Color.FromArgb(30, 30, 31) :
            Color.FromArgb(SystemColors.Control.R - 16, SystemColors.Control.G - 16, SystemColors.Control.B - 17);
        form.TransparencyKey = form.BackColor;
        form.FormClosing += (s, e) =>
        {
            RemoveFluentBackground(form, darkMode);
        };
    }

    private static void ChangeTrackbarColors(Form form)
    {
        foreach (Control ctrl in form.Controls)
        {
            if (ctrl is TrackBar trackBar)
            {
                trackBar.BackColor = form.BackColor;
            }
            if (ctrl.HasChildren)
            {
                ChangeTrackbarColorsRecursive(ctrl, form.BackColor);
            }
        }
    }

    private static void ChangeTrackbarColorsRecursive(Control ctrl, Color backColor)
    {
        foreach (Control child in ctrl.Controls)
        {
            if (child is TrackBar trackBar)
            {
                trackBar.BackColor = backColor;
            }
            if (child.HasChildren)
            {
                ChangeTrackbarColorsRecursive(child, backColor);
            }
        }
    }
    public static void RemoveFluentBackground(Form form, bool darkMode)
    {
        if (!OperatingSystem.IsWindows() || Environment.OSVersion.Version.Major < 10 || Environment.OSVersion.Version.Build < 22523)
            return;
        if (form == null || form.IsDisposed || form.Disposing) return;
        if (!form.IsHandleCreated)
            form.CreateControl();
        int backdropType = 0; // Default
        DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, 4);
        DwmSetWindowAttribute(form.Handle, DWMWA_IMMERSIVE_DARK_MODE, ref backdropType, 4);
        DwmSetWindowAttribute(form.Handle, 35, ref cachedColorRef, 4);
        form.BackColor = darkMode ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        form.TransparencyKey = Color.Empty;
    }
}