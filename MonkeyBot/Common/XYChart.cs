using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace MonkeyBot.Common
{
    public class XYChart
    {
        private readonly int chartWidth;
        private readonly int chartHeight;
        private readonly int axisBorder;

        private const int axisMargin = 25;

        public RectangleF AxisRect => new Rectangle(
            axisBorder + axisMargin,
            axisBorder,
            chartWidth - 2 * axisBorder - axisMargin,
            chartHeight - 2 * axisBorder - axisMargin);

        public ChartAxis AxisX { get; set; }
        public ChartAxis AxisY { get; set; }

        public XYChart() : this(1000, 500, 50)
        {
        }

        public XYChart(int width, int height, int borderMargin)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (borderMargin <= 0)
                throw new ArgumentOutOfRangeException(nameof(borderMargin));

            chartWidth = width;
            chartHeight = height;
            axisBorder = borderMargin;
        }

        public void ExportChart(string filePath, IEnumerable<PointF> xyValues)
        {
            if (filePath.IsEmptyOrWhiteSpace())
                throw new ArgumentNullException(nameof(filePath));
            if (xyValues == null || !xyValues.Any())
                throw new ArgumentException("Please provide some values");

            using Image image = new Bitmap(chartWidth, chartHeight);
            using Graphics graphics = Graphics.FromImage(image);
            using AdjustableArrowCap arrowCap = new AdjustableArrowCap(5, 5);
            using Pen axisPen = new Pen(Brushes.Black, 5.0f) { CustomEndCap = arrowCap, StartCap = LineCap.Square };
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            graphics.DrawLine(axisPen, AxisRect.Left, AxisRect.Bottom, AxisRect.Left, AxisRect.Top);
            graphics.DrawLine(axisPen, AxisRect.Left, AxisRect.Bottom, AxisRect.Right, AxisRect.Bottom);

            DrawAxes(graphics);

            DrawPoints(graphics, xyValues);

            image.Save(filePath, ImageFormat.Png);
        }

        private void DrawPoints(Graphics graphics, IEnumerable<PointF> xyValues)
        {
            using Pen linePen = new Pen(Brushes.Blue, 3.0f);
            float factorX = (AxisRect.Width - AxisRect.Width / AxisX.NumTicks) / (AxisX.Max - AxisX.Min);
            float factorY = (AxisRect.Height - AxisRect.Height / AxisY.NumTicks) / (AxisY.Max - AxisY.Min);

            var translatedPoints = xyValues
                .Select(p => new PointF(
                    p.X * factorX + AxisRect.Left,
                    AxisRect.Bottom - p.Y * factorY))
                .ToArray();

            if (translatedPoints.Length > 1)
            {
                graphics.DrawLines(linePen, translatedPoints);
            }
        }

        private void DrawAxes(Graphics graphics)
        {
            using Pen tickPen = new Pen(Brushes.Black, 3.0f);
            using Pen linesPen = new Pen(Brushes.Gray, 1.0f) { DashStyle = DashStyle.Dot };
            using StringFormat yFormat = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };
            using StringFormat xFormat = new StringFormat { Alignment = StringAlignment.Center };
            using Font font = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold);
            float deltaXChart = AxisRect.Width / AxisX.NumTicks;
            float deltaXValues = (AxisX.Max - AxisX.Min) / (AxisX.NumTicks - 1);
            for (int i = 0; i < AxisX.NumTicks; i++)
            {
                float x = AxisRect.Left + i * deltaXChart;
                graphics.DrawLine(tickPen, x, AxisRect.Bottom, x, AxisRect.Bottom + 10);
                string label = AxisX.LabelFunc?.Invoke(i) ?? $"{i * deltaXValues}";
                graphics.DrawString(label, font, Brushes.Black, x, AxisRect.Bottom + 30 - font.Height / 2, xFormat);
            }

            float deltaYChart = AxisRect.Height / AxisY.NumTicks;
            float deltaYValues = (AxisY.Max - AxisY.Min) / (AxisY.NumTicks - 1);
            for (int i = 0; i < AxisY.NumTicks; i++)
            {
                float y = AxisRect.Bottom - i * deltaYChart;
                graphics.DrawLine(tickPen, AxisRect.Left - 10, y, AxisRect.Left, y);
                graphics.DrawLine(linesPen, AxisRect.Left, y, AxisRect.Right, y);
                string label = AxisY.LabelFunc?.Invoke(i) ?? $"{i * deltaYValues}";
                graphics.DrawString(label, font, Brushes.Black, AxisRect.Left - 10, y, yFormat);
            }
        }
    }

    public class ChartAxis
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public int NumTicks { get; set; }
        public Func<int, string> LabelFunc { get; set; }
    }
}