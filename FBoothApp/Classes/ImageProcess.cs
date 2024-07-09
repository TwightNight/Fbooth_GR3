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

    }

}
