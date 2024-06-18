using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Classes.Entity
{
    public class Infrastructure
    {
        public class FrameType
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Column { get; set; }
            public int Row { get; set; }
            public string Code { get; set; }
            public List<(Image, string)> BackgroundImages { get; set; }
            public ImageInFrame ImageInFrame { get; set; }
        }
        public class IconInImage
        {
            public Bitmap IconBitmap { get; set; }
            public Point Position { get; set; }
            public Size Size { get; set; }

        }
        public class ImageInFrame : IComparable<ImageInFrame>
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int MarginTop { get; set; }
            public int MarginLeft { get; set; }
            public int MarginRight { get; set; }
            public int MarginBottom { get; set; }
            public PointF TopLeft { get; set; }

            public int CompareTo(ImageInFrame other)
            {
                if (this.TopLeft.Y == other.TopLeft.Y)
                {
                    return this.TopLeft.X.CompareTo(other.TopLeft.X);
                }
                if (this.TopLeft.X == other.TopLeft.X)
                {
                    return this.TopLeft.Y.CompareTo(other.TopLeft.Y);
                }
                return -1;
            }
        }
    }
}
