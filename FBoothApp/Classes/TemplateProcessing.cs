using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace FBoothApp
{
    class TemplateProcessing
    {
        static public void ImageAndSave(string imagePath, int photoInTemplateNumb, string templateName)
        {
            // Load layout template
            string layoutTemplatePath = Path.Combine(imagePath, templateName + ".png");
            BitmapImage layoutTemplate = new BitmapImage(new Uri(layoutTemplatePath));

            // Phân tích layout template để tìm các vùng trống
            List<Rectangle> emptySlots = FindEmptySlots(layoutTemplate);

            if (emptySlots.Count < photoInTemplateNumb)
            {
                throw new ArgumentException("Số lượng ô trống không đủ cho số lượng ảnh cần chèn");
            }

            List<BitmapSource> images = new List<BitmapSource>();

            for (int i = 0; i < photoInTemplateNumb; i++)
            {
                // Create each image with appropriate size from empty slots
                byte[] imageBytes = LoadImageData(Path.Combine(imagePath, $"image{i + 1}.jpg"));
                Rectangle slot = emptySlots[i];
                BitmapSource imageSource = CreateImage(imageBytes, slot.Width, slot.Height);
                images.Add(imageSource);
            }

            // Combine images into the layout template
            BitmapSource combinedImage = CombineImagesWithTemplate(layoutTemplate, images, emptySlots);

            // Save the final image
            byte[] combinedImageBytes = GetEncodedImageData(combinedImage, ".jpg");
            SaveImageData(combinedImageBytes, naming(photoInTemplateNumb));
        }

        public static byte[] LoadImageData(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public static BitmapSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight)
        {
            if (imageData == null) return null;

            BitmapImage result = new BitmapImage();
            result.BeginInit();

            if (decodePixelWidth > 0)
            {
                result.DecodePixelWidth = decodePixelWidth;
            }

            if (decodePixelHeight > 0)
            {
                result.DecodePixelHeight = decodePixelHeight;
            }

            result.StreamSource = new MemoryStream(imageData);
            result.CreateOptions = BitmapCreateOptions.None;
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.EndInit();

            return result;
        }

        private static void SaveImageData(byte[] imageData, string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            file.Directory.Create();

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(imageData, 0, imageData.Length);
            }
        }

        static public byte[] GetEncodedImageData(BitmapSource image, string preferredFormat)
        {
            BitmapEncoder encoder;

            switch (preferredFormat.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

                default:
                    throw new NotSupportedException("Format not supported");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public static string naming(int numb)
        {
            string directory = Path.Combine(Environment.CurrentDirectory, "resize");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"resize{numb}.jpg");
        }

        private static List<Rectangle> FindEmptySlots(BitmapImage layoutTemplate)
        {
            List<Rectangle> emptySlots = new List<Rectangle>();
            int width = layoutTemplate.PixelWidth;
            int height = layoutTemplate.PixelHeight;

            // Convert BitmapImage to Bitmap for easier manipulation
            WriteableBitmap writeableBitmap = new WriteableBitmap(layoutTemplate);
            Bitmap bitmap = new Bitmap(writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            writeableBitmap.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bitmap.UnlockBits(data);

            // Find empty slots (transparent regions)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);
                    if (pixelColor.A == 0)
                    {
                        // Found a transparent pixel, start finding the empty slot
                        Rectangle emptySlot = FindSlot(bitmap, x, y);
                        if (!emptySlots.Contains(emptySlot))
                        {
                            emptySlots.Add(emptySlot);
                        }
                    }
                }
            }

            return emptySlots;
        }

        private static Rectangle FindSlot(Bitmap bitmap, int startX, int startY)
        {
            int maxX = startX;
            int maxY = startY;
            for (int x = startX; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, startY).A != 0)
                {
                    maxX = x - 1;
                    break;
                }
            }

            for (int y = startY; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(startX, y).A != 0)
                {
                    maxY = y - 1;
                    break;
                }
            }

            return new Rectangle(startX, startY, maxX - startX + 1, maxY - startY + 1);
        }

        private static BitmapSource CombineImagesWithTemplate(BitmapSource template, List<BitmapSource> images, List<Rectangle> slots)
        {
            int width = template.PixelWidth;
            int height = template.PixelHeight;

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(template, new System.Windows.Rect(0, 0, width, height));

                for (int i = 0; i < images.Count && i < slots.Count; i++)
                {
                    Rectangle slot = slots[i];
                    drawingContext.DrawImage(images[i], new System.Windows.Rect(slot.X, slot.Y, slot.Width, slot.Height));
                }
            }

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            return renderBitmap;
        }

        

        static public void foreground1(string printPath)
        {
            try
            {
                var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));
                firstImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_1.png");

                var foreground = System.Drawing.Image.FromFile(tempPath);

                tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
                var empty = System.Drawing.Image.FromFile(tempPath);


                using (Graphics grfx = Graphics.FromImage(empty))
                {

                    grfx.DrawImage(firstImage, 0, 0);

                    grfx.DrawImage(foreground, 0, 0);

                    empty.Save(printPath);
                    empty.Dispose();
                }
            }
            catch (FileNotFoundException)
            {
            }
        }
        static public void foreground3(string printPath)
        {
            try
            {
                var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));

                var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));

                var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));

                string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_3.png");

                var foreground = System.Drawing.Image.FromFile(tempPath);

                tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
                var empty = System.Drawing.Image.FromFile(tempPath);

                //   Bitmap changedImage = new Bitmap(Convert.ToInt32(1024), Convert.ToInt32(1024), System.Drawing.Imaging.PixelFormat.Format32bppArgb);


                using (Graphics grfx = Graphics.FromImage(empty))
                {
                    grfx.DrawImage(firstImage, 50, 80);
                    grfx.DrawImage(secondImage, 50, 492);
                    grfx.DrawImage(thirdImage, 50, 906);
                    grfx.DrawImage(firstImage, 645, 80);
                    grfx.DrawImage(secondImage, 645, 492);
                    grfx.DrawImage(thirdImage, 645, 906);
                    grfx.DrawImage(foreground, 0, 0);


                    empty.Save(printPath);
                    empty.Dispose();
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        static public void foreground4(string printPath)
        {
            try
            {
                var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));
                firstImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));
                secondImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));
                thirdImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                var fourthImage = System.Drawing.Image.FromFile(ReSize.naming(4));
                fourthImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

                string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_4.png");

                var foreground = System.Drawing.Image.FromFile(tempPath);

                tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
                var empty = System.Drawing.Image.FromFile(tempPath);


                using (Graphics grfx = Graphics.FromImage(empty))
                {
                    grfx.DrawImage(firstImage, 82, 866);
                    grfx.DrawImage(secondImage, 82, 78);
                    grfx.DrawImage(thirdImage, 660, 866);
                    grfx.DrawImage(fourthImage, 660, 78);
                    grfx.DrawImage(foreground, 0, 0);
                    empty.Save(printPath);
                    empty.Dispose();
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        static public void foreground4stripes(string printPath)
        {
            try
            {
                var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));

                var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));

                var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));

                var fourthImage = System.Drawing.Image.FromFile(ReSize.naming(4));

                string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_4_paski.png");

                var foreground = System.Drawing.Image.FromFile(tempPath);

                tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
                var empty = System.Drawing.Image.FromFile(tempPath);


                using (Graphics grfx = Graphics.FromImage(empty))
                {
                    grfx.DrawImage(firstImage, 50, 80);
                    grfx.DrawImage(secondImage, 50, 472);
                    grfx.DrawImage(thirdImage, 50, 866);
                    grfx.DrawImage(fourthImage, 50, 1260);
                    grfx.DrawImage(firstImage, 645, 80);
                    grfx.DrawImage(secondImage, 645, 472);
                    grfx.DrawImage(thirdImage, 645, 866);
                    grfx.DrawImage(fourthImage, 645, 1260);
                    grfx.DrawImage(foreground, 0, 0);

                    empty.Save(printPath);
                    empty.Dispose();
                }
            }
            catch (FileNotFoundException)
            {
            }
        }


        static public string ActualTemplateDirectory()
        {
            string p1 = Environment.CurrentDirectory;
            string p2 = "templates";
            return System.IO.Path.Combine(p1, p2);
        }

        static public string PrintsDirectory()
        {
            string p1 = Actual.FilePath();
            string p2 = "prints";
            return System.IO.Path.Combine(p1, p2);
        }
    }
}