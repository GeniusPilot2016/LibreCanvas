public class CanvasState
{
    public Bitmap Bitmap { get; set; }
    public Size MainBitmapSize { get; set; }

    public CanvasState(Bitmap bitmap, Size mainBitmapSize)
    {
        Bitmap = bitmap;
        MainBitmapSize = mainBitmapSize;
    }
}
