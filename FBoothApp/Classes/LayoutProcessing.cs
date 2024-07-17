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
    class LayoutProcessing
    {
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

    }
}