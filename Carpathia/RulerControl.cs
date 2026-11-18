
using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace Carpathia
{
    [ToolboxItem(true)]
    public class RulerControl : Control
    {
        private double scale = 1.0;
        private double origin;
        private bool vertical;
        private float? mouseMarker;

        // ================================
        // RULER APPEARANCE
        // ================================

        [Category("Ruler Appearance")]
        [Description("Sets the ruler background color.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                if (base.BackColor == value)
                    return;

                base.BackColor = value;
                Invalidate();
            }
        }

        [Category("Ruler Appearance")]
        [Description("Sets the ruler tick marks and text color.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Visible)]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                if (base.ForeColor == value)
                    return;

                base.ForeColor = value;
                Invalidate();
            }
        }

        // ================================
        // RULER ORIENTATION
        // ================================

        [Category("Ruler")]
        [Description(
            "Determines whether the ruler is vertical or horizontal.")]
        [DefaultValue(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Visible)]
        public bool Vertical
        {
            get => vertical;
            set
            {
                if (vertical == value)
                    return;

                vertical = value;
                Invalidate();
            }
        }

        // ================================
        // RULER SCALE
        // ================================

        // Display pixels per image pixel.
        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public double Scale
        {
            get => scale;
            set
            {
                double newScale = Math.Max(0.001, value);

                if (scale == newScale)
                    return;

                scale = newScale;
                Invalidate();
            }
        }

        // ================================
        // RULER ORIGIN
        // ================================

        // Where image coordinate zero appears on the ruler.
        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public double Origin
        {
            get => origin;
            set
            {
                if (origin == value)
                    return;

                origin = value;
                Invalidate();
            }
        }

        // ================================
        // MOUSE POSITION MARKER
        // ================================

        [Browsable(false)]
        [DesignerSerializationVisibility(
            DesignerSerializationVisibility.Hidden)]
        public float? MouseMarker
        {
            get => mouseMarker;
            set
            {
                if (mouseMarker == value)
                    return;

                mouseMarker = value;
                Invalidate();
            }
        }

        // ================================
        // CONSTRUCTOR
        // ================================

        public RulerControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            TabStop = false;

            BackColor = Color.FromArgb(240, 240, 240);
            ForeColor = Color.FromArgb(60, 60, 60);

            Font = new Font("Segoe UI", 8);

            Size = new Size(300, 26);
        }

        // ================================
        // PAINT RULER
        // ================================

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // Background uses the selected BackColor.
            g.Clear(BackColor);

            int length = Vertical ? Height : Width;

            // Choose 1, 2, 5 or 10 x a power of ten.
            // Major marks remain at least
            // approximately 70 screen pixels apart.

            double desired = 70.0 / Scale;

            double power = Math.Pow(
                10,
                Math.Floor(Math.Log10(desired))
            );

            double majorStep = power;

            foreach (double multiplier in
                     new[] { 1.0, 2.0, 5.0, 10.0 })
            {
                majorStep = multiplier * power;

                if (majorStep * Scale >= 70)
                    break;
            }

            double minorStep = majorStep / 5.0;

            int first = (int)Math.Ceiling(
                -Origin / (minorStep * Scale)
            );

            int last = (int)Math.Floor(
                (length - Origin) / (minorStep * Scale)
            );

            // Tick marks and labels use ForeColor.
            using var pen = new Pen(ForeColor);
            using var brush = new SolidBrush(ForeColor);

            for (int i = first; i <= last; i++)
            {
                bool major = i % 5 == 0;

                float pos = (float)(
                    Origin + i * minorStep * Scale
                );

                int tickLength = major ? 12 : 5;

                // ================================
                // HORIZONTAL RULER
                // ================================

                if (!Vertical)
                {
                    g.DrawLine(
                        pen,
                        pos,
                        Height - tickLength,
                        pos,
                        Height - 1
                    );

                    if (major)
                    {
                        string label = Math.Round(
                            i * minorStep
                        ).ToString("0");

                        g.DrawString(
                            label,
                            Font,
                            brush,
                            pos + 2,
                            1
                        );
                    }
                }

                // ================================
                // VERTICAL RULER
                // ================================

                else
                {
                    g.DrawLine(
                        pen,
                        Width - tickLength,
                        pos,
                        Width - 1,
                        pos
                    );

                    if (major)
                    {
                        string label = Math.Round(
                            i * minorStep
                        ).ToString("0");

                        var state = g.Save();

                        try
                        {
                            g.TranslateTransform(
                                1,
                                pos - 2
                            );

                            g.RotateTransform(-90);

                            g.DrawString(
                                label,
                                Font,
                                brush,
                                0,
                                0
                            );
                        }
                        finally
                        {
                            g.Restore(state);
                        }
                    }
                }
            }

            // ================================
            // MOUSE POSITION INDICATOR
            // ================================

            if (MouseMarker is float marker)
            {
                using var markerPen = new Pen(
                    Color.OrangeRed,
                    2
                );

                if (Vertical)
                {
                    g.DrawLine(
                        markerPen,
                        0,
                        marker,
                        Width,
                        marker
                    );
                }
                else
                {
                    g.DrawLine(
                        markerPen,
                        marker,
                        0,
                        marker,
                        Height
                    );
                }
            }
        }
    }
}
