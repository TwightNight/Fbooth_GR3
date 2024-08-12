using FBoothApp.Entity;
using FBoothApp.Entity.Request;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FBoothApp.Services
{
    public class FetchApiServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _layoutsFolderPath;
        private readonly string _backgroundsFolderPath;
        private readonly string _stickersFolderPath;

        private Task<string> _initialLoadTask;
        private Task<string> _initialStickerLoadTask;
        private readonly string _localJsonPath;

        public FetchApiServices()
        {
            _httpClient = new HttpClient();
            _apiBaseUrl = "https://localhost:7156/api";
            //_apiBaseUrl = "https://fboothapi.azurewebsites.net/api";
            _initialLoadTask = _httpClient.GetStringAsync($"{_apiBaseUrl}/layout");
            _initialStickerLoadTask = _httpClient.GetStringAsync($"{_apiBaseUrl}/sticker");


            _layoutsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Layouts");
            _backgroundsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Backgrounds");
            _stickersFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Stickers");

            _localJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "layouts.json");

            if (!Directory.Exists(_layoutsFolderPath))
            {
                Directory.CreateDirectory(_layoutsFolderPath);
            }

            if (!Directory.Exists(_backgroundsFolderPath))
            {
                Directory.CreateDirectory(_backgroundsFolderPath);
            }

            if (!Directory.Exists(_stickersFolderPath))
            {
                Directory.CreateDirectory(_stickersFolderPath);
            }
        }

        public async Task<List<Layout>> GetLayoutsAsync()
        {
            List<Layout> layouts = new List<Layout>();
            try
            {
                var response = await _initialLoadTask;
                layouts = JsonConvert.DeserializeObject<List<Layout>>(response);
                await SyncLocalFiles(layouts);
                SaveLayoutsToLocalJson(layouts);
            }
            catch (HttpRequestException)
            {
                layouts = LoadLocalLayoutsFromJson();
            }

            return layouts;
        }

        public async Task<List<Sticker>> GetStickersAsync()
        {
            List<Sticker> stickers = new List<Sticker>();
            try
            {
                var response = await _initialStickerLoadTask;
                stickers = JsonConvert.DeserializeObject<List<Sticker>>(response);
                await SyncStickersLocalFiles(stickers);
            }
            catch (HttpRequestException)
            {
                stickers = LoadLocalStickers();
            }

            return stickers;
        }

        private List<Sticker> LoadLocalStickers()
        {
            var stickers = new List<Sticker>();
            var stickerFiles = Directory.GetFiles(_stickersFolderPath, "*.png");

            foreach (var filePath in stickerFiles)
            {
                var stickerId = Path.GetFileNameWithoutExtension(filePath);
                stickers.Add(new Sticker
                {
                    StickerID = stickerId,
                    StickerURL = filePath,
                    LastModified = File.GetLastWriteTime(filePath)
                });
            }

            return stickers;
        }

        private async Task SyncStickersLocalFiles(List<Sticker> stickers)
        {
            var stickerFiles = new HashSet<string>(Directory.GetFiles(_stickersFolderPath, "*", SearchOption.AllDirectories));

            foreach (var sticker in stickers)
            {
                string stickerFileName = $"{sticker.StickerID}.png";
                string stickerFilePath = Path.Combine(_stickersFolderPath, stickerFileName);

                if (!File.Exists(stickerFilePath) || FileNeedsUpdate(stickerFilePath, sticker.LastModified))
                {
                    await DownloadAndSaveImageAsync(sticker.StickerURL, stickerFilePath);
                }
                stickerFiles.Remove(stickerFilePath); // Xóa các file sticker đã tồn tại từ danh sách
            }

            // Xóa các sticker không còn tồn tại trên server
            foreach (var file in stickerFiles)
            {
                File.Delete(file);
            }
        }


        private void SaveLayoutsToLocalJson(List<Layout> layouts)
        {
            var json = JsonConvert.SerializeObject(layouts, Formatting.Indented);
            File.WriteAllText(_localJsonPath, json);
        }

        private List<Layout> LoadLocalLayoutsFromJson()
        {
            if (!File.Exists(_localJsonPath))
            {
                return new List<Layout>();
            }

            var json = File.ReadAllText(_localJsonPath);
            return JsonConvert.DeserializeObject<List<Layout>>(json);
        }

        private async Task SyncLocalFiles(List<Layout> layouts)
        {
            var layoutFiles = new HashSet<string>(Directory.GetFiles(_layoutsFolderPath));
            var backgroundFiles = new HashSet<string>(Directory.GetFiles(_backgroundsFolderPath, "*", SearchOption.AllDirectories));

            foreach (var layout in layouts)
            {
                string layoutFileName = $"{layout.LayoutID}.png";
                string layoutFilePath = Path.Combine(_layoutsFolderPath, layoutFileName);
                string layoutBackgroundFolderPath = Path.Combine(_backgroundsFolderPath, layout.LayoutCode);

                if (!Directory.Exists(layoutBackgroundFolderPath))
                {
                    Directory.CreateDirectory(layoutBackgroundFolderPath);
                }

                // Save layout file
                if (!File.Exists(layoutFilePath) || FileNeedsUpdate(layoutFilePath, layout.LastModified))
                {
                    await DownloadAndSaveImageAsync(layout.LayoutURL, layoutFilePath);
                }
                layoutFiles.Remove(layoutFilePath); // Remove existing layout from the set

                // Save background files
                if (layout.Backgrounds != null)
                {
                    foreach (var background in layout.Backgrounds)
                    {
                        if (background != null)
                        {
                            string backgroundFileName = $"{background.BackgroundID}.png";
                            string backgroundFilePath = Path.Combine(layoutBackgroundFolderPath, backgroundFileName);

                            if (!File.Exists(backgroundFilePath) || FileNeedsUpdate(backgroundFilePath, layout.LastModified))
                            {
                                await DownloadAndSaveImageAsync(background.BackgroundURL, backgroundFilePath);
                            }
                            backgroundFiles.Remove(backgroundFilePath); // Remove existing background from the set
                        }
                    }
                }
            }

            // Delete layouts that are no longer present in the response
            foreach (var file in layoutFiles)
            {
                File.Delete(file);
            }

            // Delete backgrounds that are no longer present in the response
            foreach (var file in backgroundFiles)
            {
                File.Delete(file);
            }
        }

        private async Task DownloadAndSaveImageAsync(string url, string filePath)
        {
            using (var response = await _httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                SaveImageWithHighestQuality(filePath, imageBytes);
            }
        }

        private void SaveImageWithHighestQuality(string path, byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            using (var image = System.Drawing.Image.FromStream(ms))
            {
                var encoder = GetEncoder(ImageFormat.Png);
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    image.Save(fs, encoder, encoderParameters);
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        private bool FileNeedsUpdate(string filePath, DateTime? lastModified)
        {
            var fileInfo = new FileInfo(filePath);
            return lastModified.HasValue && fileInfo.LastWriteTime < lastModified.Value;
        }

        public async Task<BookingResponse> CheckinAsync(CheckinRequest request)
        {
            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/booking/checkin-booking", content);

            if (!response.IsSuccessStatusCode)
            {
                // Đọc thông báo lỗi từ phản hồi của API
                var errorResponse = await response.Content.ReadAsStringAsync();

                // Giải mã chuỗi JSON để chỉ lấy phần thông báo lỗi
                var errorObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorResponse);
                if (errorObject != null && errorObject.TryGetValue("message", out var errorMessage))
                {
                    throw new Exception(errorMessage);
                }

                // Nếu không có trường "message", ném lỗi chung chung
                throw new Exception("An unknown error occurred.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BookingResponse>(jsonResponse);
        }

        public async Task<BookingResponse> GetBookingByIdAsync(Guid bookingId)
        {
            try
            {
                var url = $"{_apiBaseUrl}/booking/{bookingId}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Đọc nội dung phản hồi và giải mã JSON thành BookingResponse
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var booking = JsonConvert.DeserializeObject<BookingResponse>(jsonResponse);

                    return booking;
                }
                else
                {
                    // Đọc thông báo lỗi từ phản hồi của API nếu có lỗi
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var errorObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorResponse);
                    if (errorObject != null && errorObject.TryGetValue("message", out var errorMessage))
                    {
                        throw new Exception(errorMessage);
                    }

                    throw new Exception("An unknown error occurred while retrieving the booking.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to retrieve booking: {ex.Message}");
                throw; // Ném lại ngoại lệ để gọi hàm xử lý
            }
        }


        public async Task<PhotoSessionResponse> CreatePhotoSessionAsync(CreatePhotoSessionRequest request)
        {
            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/photo-session", content);

            if (!response.IsSuccessStatusCode)
            {
                // Đọc thông báo lỗi từ phản hồi của API
                var errorResponse = await response.Content.ReadAsStringAsync();

                // Giải mã chuỗi JSON để chỉ lấy phần thông báo lỗi
                var errorObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorResponse);
                if (errorObject != null && errorObject.TryGetValue("message", out var errorMessage))
                {
                    throw new Exception(errorMessage);
                }

                throw new Exception("An unknown error occurred.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PhotoSessionResponse>(jsonResponse);
        }

        public async Task UpdatePhotoSessionAsync(Guid photoSessionID, UpdatePhotoSessionRequest updateRequest)
        {
            var jsonRequest = JsonConvert.SerializeObject(updateRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/photo-session/{photoSessionID}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                var errorObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorResponse);
                if (errorObject != null && errorObject.TryGetValue("message", out var errorMessage))
                {
                    throw new Exception(errorMessage);
                }

                throw new Exception("An unknown error occurred.");
            }
        }

    }
}
