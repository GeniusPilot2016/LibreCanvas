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

public class BitmapResizer
{
    public static Bitmap ResizeBitmap(Bitmap originalBitmap, int newWidth, int newHeight)
    {
        Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);
        using (Graphics graphics = Graphics.FromImage(resizedBitmap))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
        }
        Logger.Log($"Resized bitmap from {originalBitmap.Width}x{originalBitmap.Height} to {newWidth}x{newHeight}", Logger.LogTypes.Info);
        return resizedBitmap;
    }
}
