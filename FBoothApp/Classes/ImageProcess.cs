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
using FBoothApp.Entity;

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



        public Bitmap OverlayBackground(Bitmap background, string sessionDirectory)
        {
            // Lấy tất cả các file ảnh trong thư mục session
            string[] photoFiles = Directory.GetFiles(sessionDirectory, "*.jpg");

            // Lấy danh sách các ô trong suốt trong background
            List<Rectangle> slots = GetTransparentSlots(background);

            // Tạo một bitmap mới với kích thước bằng background
            Bitmap result = new Bitmap(background.Width, background.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                // Vẽ background lên trước
                g.DrawImage(background, 0, 0, background.Width, background.Height);

                // Duyệt qua các file ảnh và vẽ chúng vào các ô trong suốt
                for (int i = 0; i < Math.Min(photoFiles.Length, slots.Count); i++)
                {
                    Bitmap photo = new Bitmap(photoFiles[i]);
                    Rectangle slot = slots[i];

                    // Resize ảnh để vừa với ô trong suốt
                    Bitmap resizedPhoto = new Bitmap(slot.Width, slot.Height);
                    using (Graphics graphics = Graphics.FromImage(resizedPhoto))
                    {
                        graphics.DrawImage(photo, 0, 0, slot.Width, slot.Height);
                    }

                    // Vẽ ảnh đã resize lên result bitmap
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

        public static Bitmap RenderIcons(Bitmap bitmap, List<IconInImage> icons)
        {
            // Kiểm tra nếu bitmap là null
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");
            }

            // Tạo một bitmap mới với kích thước của bitmap đầu vào
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

            // Sử dụng Graphics để vẽ lên bitmap mới
            using (Graphics g = Graphics.FromImage(result))
            {
                // Vẽ hình bitmap đầu vào lên bitmap mới
                g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);

                // Vẽ các icon lên bitmap mới tại vị trí được chỉ định
                foreach (IconInImage icon in icons)
                {
                    // Tạo một bitmap mới từ ảnh icon và loại bỏ màu nền đen
                    Bitmap transparentIcon = new Bitmap(icon.IconBitmap);
                    transparentIcon.MakeTransparent(Color.Black);

                    // Vẽ icon lên bitmap kết quả
                    g.DrawImage(transparentIcon, new Rectangle(icon.Position, icon.Size));

                    // Giải phóng bộ nhớ của bitmap tạm thời
                    transparentIcon.Dispose();
                }
            }

            return result;
        }

    }

}
