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
using FBoothApp.Services;
using FBoothApp.Entity;
using System.Net;

namespace FBoothApp
{
    class TemplateProcessing
    {
        //static public void foreground1(string printPath)
        //{
        //    try
        //    {
        //        var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));
        //        firstImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //        string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_1.png");

        //        var foreground = System.Drawing.Image.FromFile(tempPath);

        //        tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
        //        var empty = System.Drawing.Image.FromFile(tempPath);


        //        using (Graphics grfx = Graphics.FromImage(empty))
        //        {

        //            grfx.DrawImage(firstImage, 0, 0);

        //            grfx.DrawImage(foreground, 0, 0);

        //            empty.Save(printPath);
        //            empty.Dispose();
        //        }
        //    }
        //    catch (FileNotFoundException)
        //    {
        //    }
        //}
        //static public void foreground3(string printPath)
        //{
        //    try
        //    {
        //        var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));

        //        var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));

        //        var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));

        //        string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_3.png");

        //        var foreground = System.Drawing.Image.FromFile(tempPath);

        //        tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
        //        var empty = System.Drawing.Image.FromFile(tempPath);

        //        //   Bitmap changedImage = new Bitmap(Convert.ToInt32(1024), Convert.ToInt32(1024), System.Drawing.Imaging.PixelFormat.Format32bppArgb);


        //        using (Graphics grfx = Graphics.FromImage(empty))
        //        {
        //            grfx.DrawImage(firstImage, 50, 80);
        //            grfx.DrawImage(secondImage, 50, 492);
        //            grfx.DrawImage(thirdImage, 50, 906);
        //            grfx.DrawImage(firstImage, 645, 80);
        //            grfx.DrawImage(secondImage, 645, 492);
        //            grfx.DrawImage(thirdImage, 645, 906);
        //            grfx.DrawImage(foreground, 0, 0);


        //            empty.Save(printPath);
        //            empty.Dispose();
        //        }
        //    }
        //    catch (FileNotFoundException)
        //    {
        //    }
        //}

        //static public void foreground4(string printPath)
        //{
        //    try
        //    {
        //        var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));
        //        firstImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //        var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));
        //        secondImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //        var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));
        //        thirdImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //        var fourthImage = System.Drawing.Image.FromFile(ReSize.naming(4));
        //        fourthImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

        //        string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_4.png");

        //        var foreground = System.Drawing.Image.FromFile(tempPath);

        //        tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
        //        var empty = System.Drawing.Image.FromFile(tempPath);


        //        using (Graphics grfx = Graphics.FromImage(empty))
        //        {
        //            grfx.DrawImage(firstImage, 82, 866);
        //            grfx.DrawImage(secondImage, 82, 78);
        //            grfx.DrawImage(thirdImage, 660, 866);
        //            grfx.DrawImage(fourthImage, 660, 78);
        //            grfx.DrawImage(foreground, 0, 0);
        //            empty.Save(printPath);
        //            empty.Dispose();
        //        }
        //    }
        //    catch (FileNotFoundException)
        //    {
        //    }
        //}

        //static public void foreground4stripes(string printPath)
        //{
        //    try
        //    {
        //        var firstImage = System.Drawing.Image.FromFile(ReSize.naming(1));

        //        var secondImage = System.Drawing.Image.FromFile(ReSize.naming(2));

        //        var thirdImage = System.Drawing.Image.FromFile(ReSize.naming(3));

        //        var fourthImage = System.Drawing.Image.FromFile(ReSize.naming(4));

        //        string tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "foreground_4_paski.png");

        //        var foreground = System.Drawing.Image.FromFile(tempPath);

        //        tempPath = System.IO.Path.Combine(ActualTemplateDirectory(), "empty.png");
        //        var empty = System.Drawing.Image.FromFile(tempPath);


        //        using (Graphics grfx = Graphics.FromImage(empty))
        //        {
        //            grfx.DrawImage(firstImage, 50, 80);
        //            grfx.DrawImage(secondImage, 50, 472);
        //            grfx.DrawImage(thirdImage, 50, 866);
        //            grfx.DrawImage(fourthImage, 50, 1260);
        //            grfx.DrawImage(firstImage, 645, 80);
        //            grfx.DrawImage(secondImage, 645, 472);
        //            grfx.DrawImage(thirdImage, 645, 866);
        //            grfx.DrawImage(fourthImage, 645, 1260);
        //            grfx.DrawImage(foreground, 0, 0);

        //            empty.Save(printPath);
        //            empty.Dispose();
        //        }
        //    }
        //    catch (FileNotFoundException)
        //    {
        //    }
        //}

        public static void ProcessLayout(Layout layout, string printPath)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            try
            {
                // Tạo một bitmap mới với kích thước của layout
                Bitmap finalImage = new Bitmap(layout.Width, layout.Height);

                using (Graphics grfx = Graphics.FromImage(finalImage))
                {
                    // Vẽ mỗi ảnh vào vị trí tương ứng trên bitmap mới
                    for (int i = 0; i < layout.PhotoSlot; i++)
                    {
                        var photoPath = ReSize.naming(i + 1);
                        if (File.Exists(photoPath))
                        {
                            var photo = System.Drawing.Image.FromFile(photoPath);
                            var coordinates = layout.PhotoBoxes[i];

                            // Kiểm tra xem ảnh là ngang hay dọc
                            if (coordinates.BoxWidth > coordinates.BoxHeight)
                            {
                                // Ảnh ngang
                                grfx.DrawImage(photo, new Rectangle(coordinates.CoordinatesX + 1, coordinates.CoordinatesY + 1, coordinates.BoxWidth + 2, coordinates.BoxHeight + 2));
                            }
                            else
                            {
                                // Ảnh dọc
                                photo.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                grfx.DrawImage(photo, coordinates.CoordinatesX, coordinates.CoordinatesY);
                            }

                            photo.Dispose();
                        }
                    }

                    // Tải hình ảnh layout từ URL và vẽ lên bitmap mới
                    var layoutImage = DownloadImage(layout.LayoutURL);
                    grfx.DrawImage(layoutImage, 0, 0, layout.Width, layout.Height);

                    // Lưu ảnh cuối cùng
                    finalImage.Save(printPath);
                }

                // Giải phóng tài nguyên
                finalImage.Dispose();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static Image DownloadImage(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData(url);
                using (MemoryStream mem = new MemoryStream(data))
                {
                    return Image.FromStream(mem);
                }
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