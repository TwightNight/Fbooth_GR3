using FBoothApp.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace FBoothApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadConfiguration();


            await FetchConfigurationAsync();



            //// Khởi chạy cửa sổ chính của ứng dụng sau khi cấu hình được cập nhật
            //MainWindow mainWindow = new MainWindow();
            //mainWindow.Show();
        }

        private void LoadConfiguration()
        {
            Settings = new AppSettings
            {
                BoothName = ConfigurationManager.AppSettings["ApplicationName"],
                //BranchId = ConfigurationManager.AppSettings["BranchId"],
                BoothId = ConfigurationManager.AppSettings["RoomId"]
            };
        }



        private async Task FetchConfigurationAsync()
        {
            var httpClient = new HttpClient();

            // Lấy giá trị ApiUrl từ cấu hình
            string apiUrl = ConfigurationManager.AppSettings["ApiUrl"];
            //string branchId = ConfigurationManager.AppSettings["BranchId"];
            string boothId = ConfigurationManager.AppSettings["BoothId"];

            // Tạo URL đầy đủ cho API yêu cầu cấu hình
            var requestUrl = $"{apiUrl}/booth/{boothId}";

            try
            {
                // Gửi yêu cầu GET đến API và nhận phản hồi dưới dạng chuỗi
                var response = await httpClient.GetStringAsync(requestUrl);

                // Chuyển đổi chuỗi JSON thành đối tượng AppSettings
                var config = JsonConvert.DeserializeObject<AppSettings>(response);

                // Nếu config không null, cập nhật cấu hình ứng dụng và lưu vào cấu hình cục bộ
                if (config != null)
                {
                    App.Settings.BoothName = config.BoothName;
                    //App.Settings.BranchId = config.BranchId;
                    App.Settings.BoothId = config.BoothId;
                        UpdateLocalConfiguration(App.Settings);
                }
            }
            catch (HttpRequestException ex)
            {
                // Xử lý lỗi khi yêu cầu HTTP thất bại
                Debug.WriteLine($"Error fetching configuration: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Xử lý lỗi khi chuyển đổi JSON thất bại
                Debug.WriteLine($"Error parsing configuration: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        private void UpdateLocalConfiguration(AppSettings settings)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var appSettings = new Dictionary<string, string>
            {
                { "ApplicationName", settings.BoothName },
                { "BoothId", settings.BoothId.ToString() }
            };

            foreach (var keyValuePair in appSettings)
            {
                if (config.AppSettings.Settings[keyValuePair.Key] == null)
                {
                    config.AppSettings.Settings.Add(keyValuePair.Key, keyValuePair.Value);
                }
                else
                {
                    // Thay thế giá trị ngay cả khi giá trị hiện tại là chuỗi rỗng
                    config.AppSettings.Settings[keyValuePair.Key].Value = keyValuePair.Value;
                }
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
