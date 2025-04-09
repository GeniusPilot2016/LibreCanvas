public abstract class Shape
{
    public Color Color { get; set; }
    public int Thickness { get; set; }

    public abstract void Draw(Graphics g);
}
