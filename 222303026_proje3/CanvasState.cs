public class CanvasState
{
    public Bitmap Bitmap { get; set; }
    public Size CanvasSize { get; set; }

    public CanvasState(Bitmap bitmap, Size canvasSize)
    {
        Bitmap = bitmap;
        CanvasSize = canvasSize;
    }
}
