using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using FBoothApp.Entity;
using System.Drawing.Drawing2D;

namespace FBoothApp
{
    class Report
    {
        static public void Error(string message, bool lockdown)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Debug.WriteLine("Error happend");
        }
    }


    class Actual
    {
        static public string DateNow()
        {
            var date1 = DateTime.Now;
            string todayDate = string.Empty;
            todayDate = date1.ToString("dd") + "." + date1.ToString("MM") + "." + "20" + date1.ToString("yy");
            return todayDate;
        }

        static public string FilePath()
        {
            string p1 = Environment.CurrentDirectory;
            string p2 = Actual.DateNow();
            string pathString = System.IO.Path.Combine(p1, p2);
            return pathString;
        }
    }

    class Control
    {
        static public bool photoTemplate(int actualPhotoNumber, int photoInTemplate)
        {
            if (actualPhotoNumber == photoInTemplate) return true;
            else return false;
        }
    }

    class Create
    {
        private static string currentSessionPath;

        static public string CurrentSessionDirectory()
        {
            if (currentSessionPath == null)
            {
                string p1 = Environment.CurrentDirectory;
                string p2 = Actual.DateNow();
                currentSessionPath = System.IO.Path.Combine(p1, p2);

                if (!Directory.Exists(currentSessionPath))
                {
                    Directory.CreateDirectory(currentSessionPath);
                }
            }

            return currentSessionPath;
        }
        static public void TodayPhotoFolder()
        {
            string p1 = Environment.CurrentDirectory;
            string p2 = Actual.DateNow();
            string pathString = System.IO.Path.Combine(p1, p2);
            Directory.CreateDirectory(pathString);
            string p3 = "prints";
            pathString = System.IO.Path.Combine(p2, p3);
            Directory.CreateDirectory(pathString);
        }
    }

    class ReSize
    {
        //hàm lưu ảnh
        public static void ImageAndSave(string imagepath, int photoInTemplateNumb, Layout layout)
        {
            try
            {
                // Lấy thông tin kích thước và tọa độ từ layout
                int targetWidth = layout.PhotoBoxes[photoInTemplateNumb - 1].BoxWidth;
                int targetHeight = layout.PhotoBoxes[photoInTemplateNumb - 1].BoxHeight;

                // Tải dữ liệu hình ảnh từ đường dẫn
                byte[] imageBytes = LoadImageData(imagepath);

                // Tạo ImageSource từ dữ liệu hình ảnh
                ImageSource imageSource = CreateImage(imageBytes, targetWidth, targetHeight);

                // Mã hóa dữ liệu hình ảnh
                imageBytes = GetEncodedImageData(imageSource, ".jpg");

                // Lưu dữ liệu hình ảnh đã mã hóa vào đường dẫn
                SaveImageData(imageBytes, naming(photoInTemplateNumb));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ImageAndSave: {ex.Message}");
            }
        }

        public static byte[] LoadImageData(string filePath)

        {
            FileStream fs = new FileStream(filePath, FileMode.Open, System.IO.FileAccess.Read);

            BinaryReader br = new BinaryReader(fs);

            byte[] imageBytes = br.ReadBytes((int)fs.Length);

            br.Close();

            fs.Close();

            return imageBytes;
        }

        public static ImageSource CreateImage(byte[] imageData,
            int decodePixelWidth, int decodePixelHeight)

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
            result.CacheOption = BitmapCacheOption.Default;

            result.EndInit();

            return result;
        }


        private static void SaveImageData(byte[] imageData, string filePath)

        {
            //if filepath not exist create one
            FileInfo file = new System.IO.FileInfo(filePath);
            file.Directory.Create();
            FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(imageData);

            bw.Close();

            fs.Close();
        }

        static public byte[] GetEncodedImageData(ImageSource image,
            string preferredFormat)

        {
            byte[] result = null;

            BitmapEncoder encoder = null;

            switch (preferredFormat.ToLower())

            {
                case ".jpg":

                case ".jpeg":

                    encoder = new JpegBitmapEncoder();

                    break;

                case ".png":

                    encoder = new PngBitmapEncoder();

                    break;
            }


            if (image is BitmapSource)

            {
                MemoryStream stream = new MemoryStream();

                encoder.Frames.Add(
                    BitmapFrame.Create(image as BitmapSource));

                encoder.Save(stream);

                stream.Seek(0, SeekOrigin.Begin);

                result = new byte[stream.Length];

                BinaryReader br = new BinaryReader(stream);

                br.Read(result, 0, (int)stream.Length);

                br.Close();

                stream.Close();
            }
            return result;
        }

        public static string naming(int numb)
        {
            string p1 = Environment.CurrentDirectory;
            string p2 = "resize";
            string p3 = ("resize" + numb.ToString() + ".jpg");
            return System.IO.Path.Combine(p1, p2, p3);
        }
    }
}