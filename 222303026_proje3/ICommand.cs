using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public interface ICommand
    {
        void Unexecute();
        void Execute();
    }
    public class UpdateBitmapCommand : ICommand
    {
        private Bitmap _previousBitmap;
        private Bitmap _currentBitmap;
        private PictureBox _pictureBoxCanvas;

        public UpdateBitmapCommand(PictureBox pictureBoxCanvas, Bitmap previousBitmap, Bitmap currentBitmap)
        {
            _pictureBoxCanvas = pictureBoxCanvas;
            _previousBitmap = previousBitmap;
            _currentBitmap = currentBitmap;
        }

        public void Execute()
        {
            _pictureBoxCanvas.Image = _currentBitmap;
            _pictureBoxCanvas.Invalidate();
        }

        public void Unexecute()
        {
            _pictureBoxCanvas.Image = _previousBitmap;
            _pictureBoxCanvas.Invalidate();
        }
    }
}
