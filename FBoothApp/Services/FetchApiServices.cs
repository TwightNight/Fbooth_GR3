using FBoothApp.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FBoothApp.Services
{
    public class LayoutServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _layoutsFolderPath;
        private readonly string _backgroundsFolderPath;
        private Task<string> _initialLoadTask;

        private readonly string _localJsonPath;


        public LayoutServices()
        {
            _httpClient = new HttpClient();
            _initialLoadTask = _httpClient.GetStringAsync("https://localhost:7156/api/layout");
            _layoutsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Layouts");
            _backgroundsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Backgrounds");
            _localJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "layouts.json");

            if (!Directory.Exists(_layoutsFolderPath))
            {
                Directory.CreateDirectory(_layoutsFolderPath);
            }

            if (!Directory.Exists(_backgroundsFolderPath))
            {
                Directory.CreateDirectory(_backgroundsFolderPath);
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

            //var layoutIds = new HashSet<string>(layouts.ConvertAll(layout => layout.LayoutID.ToString()));

            // Download and save new or updated layouts
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

        //private List<Layout> LoadLocalLayouts()
        //{
        //    var layouts = new List<Layout>();
        //    foreach (var file in Directory.GetFiles(_layoutsFolderPath, "*.png"))
        //    {
        //        var layoutId = Path.GetFileNameWithoutExtension(file);
        //        layouts.Add(new Layout { LayoutID = layoutId, LayoutURL = file });
        //    }
        //    return layouts;
        //}

        private async Task DownloadAndSaveImageAsync(string url, string filePath)
        {
            using (var response = await _httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                WriteAllBytes(filePath, imageBytes);
            }
        }

        private void WriteAllBytes(string path, byte[] bytes)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        private bool FileNeedsUpdate(string filePath, DateTime? lastModified)
        {
            var fileInfo = new FileInfo(filePath);
            return lastModified.HasValue && fileInfo.LastWriteTime < lastModified.Value;
        }
    }
}
