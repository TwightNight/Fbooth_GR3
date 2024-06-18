using System;
using System.Collections.Generic;
using System.Drawing;
//using System.Text.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FBoothApp.Classes.Entity.Infrastructure;

namespace FBoothApp.Util
{
    public class ReadAndParseJsonFileWithSystemTextJson
    {
        //public static List<FrameType> UseFileOpenReadTextWithSystemTextJson(string jsonFilePath)
        //{
        //    FileStream json = File.OpenRead(jsonFilePath);
        //    List<FrameType> types = JsonSerializer.Deserialize<List<FrameType>>(json);
        //    json.Dispose();
        //    return types;
        //}
    }
    public class UtilClass
    {
        public static List<(Image, string)> LoadImagesFromFolder(string folderPath)
        {
            List<(Image, string)> images = new List<(Image, string)>();

            try
            {
                // Kiểm tra xem thư mục tồn tại không
                if (Directory.Exists(folderPath))
                {
                    // Lấy tất cả các đường dẫn của hình trong thư mục
                    string[] imagePaths = Directory.GetFiles(folderPath, "*.jpg");
                    imagePaths = imagePaths.Concat(Directory.GetFiles(folderPath, "*.jpeg")).ToArray();
                    imagePaths = imagePaths.Concat(Directory.GetFiles(folderPath, "*.png")).ToArray();
                    imagePaths = imagePaths.Concat(Directory.GetFiles(folderPath, "*.gif")).ToArray();
                    imagePaths = imagePaths.Concat(Directory.GetFiles(folderPath, "*.bmp")).ToArray();

                    // Lặp qua từng đường dẫn và tải hình ảnh vào danh sách
                    foreach (string imagePath in imagePaths)
                    {
                        using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            Image img = Image.FromStream(fs);
                            string name = Path.GetFileName(imagePath);
                            images.Add((img, name));
                        }
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
            }

            return images;
        }
        //public static void ExtractZipContent(string FileZipPath, string password, string OutputFolder)
        //{
        //    ZipFile file = null;
        //    try
        //    {
        //        FileStream fs = File.OpenRead(FileZipPath);
        //        file = new ZipFile(fs);

        //        if (!String.IsNullOrEmpty(password))
        //        {
        //            file.Password = password;
        //        }

        //        foreach (ZipEntry zipEntry in file)
        //        {
        //            if (!zipEntry.IsFile)
        //            {
        //                continue;
        //            }

        //            String entryFileName = zipEntry.Name;
        //            byte[] buffer = new byte[4096];
        //            Stream zipStream = file.GetInputStream(zipEntry);
        //            String fullZipToPath = Path.Combine(OutputFolder, entryFileName);
        //            string directoryName = Path.GetDirectoryName(fullZipToPath);

        //            if (directoryName.Length > 0)
        //            {
        //                Directory.CreateDirectory(directoryName);
        //            }

        //            using (FileStream streamWriter = File.Create(fullZipToPath))
        //            {
        //                StreamUtils.Copy(zipStream, streamWriter, buffer);
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        if (file != null)
        //        {
        //            file.IsStreamOwner = true;
        //            file.Close();
        //        }
        //    }
        //}
    }
}
