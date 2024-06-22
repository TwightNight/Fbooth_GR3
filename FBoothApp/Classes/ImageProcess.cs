using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace FBoothApp.Classes
{
    public class ImageProcess
    {
        public Bitmap OverlayBackgroundBINHTHUONG(BitmapImage photo, Bitmap background)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap photoBitmap;
                BitmapDecoder decoder = BitmapDecoder.Create(photo.UriSource, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapFrame frame = decoder.Frames[0];
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(frame);
                encoder.Save(ms);
                photoBitmap = new Bitmap(ms);

                // Resize photoBitmap to match background size
                Bitmap resizedPhoto = new Bitmap(background.Width, background.Height);
                using (Graphics g = Graphics.FromImage(resizedPhoto))
                {
                    g.DrawImage(photoBitmap, 0, 0, background.Width, background.Height);
                }

                // Create a new result bitmap with the same size as background
                Bitmap result = new Bitmap(background.Width, background.Height);
                using (Graphics g = Graphics.FromImage(result))
                {
                    // Draw the resized photo first
                    g.DrawImage(resizedPhoto, 0, 0, resizedPhoto.Width, resizedPhoto.Height);
                    // Then draw the background on top
                    g.DrawImage(background, 0, 0, background.Width, background.Height);
                }

                return result;
            }
        }

        public BitmapImage ConvertToBitmapImageBINHTHUONG(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }



        public Bitmap OverlayBackground(Bitmap background, string photosDirectory)
        {
            string[] photoFiles = Directory.GetFiles(photosDirectory, "*.jpg");

            // Get the slots (transparent areas) in the background
            List<Rectangle> slots = GetTransparentSlots(background);

            // Create a new result bitmap with the same size as background
            Bitmap result = new Bitmap(background.Width, background.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                // Draw the background first
                g.DrawImage(background, 0, 0, background.Width, background.Height);

                // Iterate over the photo files and draw them into the slots
                for (int i = 0; i < Math.Min(photoFiles.Length, slots.Count); i++)
                {
                    Bitmap photo = new Bitmap(photoFiles[i]);
                    Rectangle slot = slots[i];

                    // Resize photo to fit into the slot
                    Bitmap resizedPhoto = new Bitmap(slot.Width, slot.Height);
                    using (Graphics graphics = Graphics.FromImage(resizedPhoto))
                    {
                        graphics.DrawImage(photo, 0, 0, slot.Width, slot.Height);
                    }

                    // Draw the resized photo into the result bitmap
                    g.DrawImage(resizedPhoto, slot.X, slot.Y, slot.Width, slot.Height);
                }
            }

            return result;
        }

        public BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private List<Rectangle> GetTransparentSlots(Bitmap bitmap)
        {
            List<Rectangle> slots = new List<Rectangle>();
            bool[,] visited = new bool[bitmap.Width, bitmap.Height];

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (!visited[x, y] && IsTransparent(bitmap.GetPixel(x, y)))
                    {
                        Rectangle slot = GetSlot(bitmap, x, y, visited);
                        if (slot.Width > 0 && slot.Height > 0)
                        {
                            slots.Add(slot);
                        }
                    }
                }
            }

            return slots;
        }

        private bool IsTransparent(Color color)
        {
            return color.A == 0;
        }

        private Rectangle GetSlot(Bitmap bitmap, int startX, int startY, bool[,] visited)
        {
            int minX = startX;
            int minY = startY;
            int maxX = startX;
            int maxY = startY;

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                Point point = queue.Dequeue();
                int x = point.X;
                int y = point.Y;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);

                foreach (Point neighbor in GetNeighbors(x, y, bitmap.Width, bitmap.Height))
                {
                    if (!visited[neighbor.X, neighbor.Y] && IsTransparent(bitmap.GetPixel(neighbor.X, neighbor.Y)))
                    {
                        visited[neighbor.X, neighbor.Y] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private IEnumerable<Point> GetNeighbors(int x, int y, int width, int height)
        {
            if (x > 0) yield return new Point(x - 1, y);
            if (x < width - 1) yield return new Point(x + 1, y);
            if (y > 0) yield return new Point(x, y - 1);
            if (y < height - 1) yield return new Point(x, y + 1);
        }

    }
}
