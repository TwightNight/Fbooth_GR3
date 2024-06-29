using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Linq.Expressions;
using EOSDigital.API;
using EOSDigital.SDK;
using System.Threading;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Linq;
using FBoothApp.Classes;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;
using Point = System.Drawing.Point;
using System.Drawing.Imaging;
using FBoothApp.Entity;
using System.Drawing.Drawing2D;


namespace FBoothApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public System.Windows.Threading.DispatcherTimer sliderTimer;
        public System.Windows.Threading.DispatcherTimer betweenPhotos;
        public System.Windows.Threading.DispatcherTimer secondCounter;
        private BitmapImage actualPrint;


        CanonAPI APIHandler;
        Camera MainCamera;
        ImageBrush liveView = new ImageBrush();
        Action<BitmapImage> SetImageAction;
        List<Camera> CamList;
        PrintDialog pdialog = new PrintDialog();
        ImageProcess imageProcess;

        XDocument actualSettings = new XDocument();
        List<System.Windows.Media.ImageSource> resizedImages = new List<System.Windows.Media.ImageSource>();

        public int photoNumber = 0; // Thứ tự của ảnh được chụp
        public int photoNumberInTemplate = 0;
        int timeLeft = 5;
        int timeLeftCopy = 5;
        int photosInTemplate = 0; //kiem tra so luong anh co dung voi slot trong layout khong
        int printNumber = 0;
        int maxCopies = 1;
        short actualNumberOfCopies = 1;
        int printtime = 10;

        public string SmtpServerName;
        public string SmtpPortNumber;
        public string EmailHostAddress;
        public string EmailHostPassword;

        string printPath = string.Empty;
        private Layout layout;
        //public string templateName = string.Empty;
        string printerName = string.Empty;
        private string currentDirectory = Environment.CurrentDirectory;
        public bool turnOnTemplateMenu = false;
        public bool PhotoTaken = false;


        public MainWindow()
        {
            layout = new Layout();
            imageProcess = new ImageProcess();
            InitializeComponent();
            FillSavedData();
            ActivateTimers();
            CheckTemplate();

            //Canon:
            try
            {
                Create.CurrentSessionDirectory();
                APIHandler = new CanonAPI();
                APIHandler.CameraAdded += APIHandler_CameraAdded;

                ErrorHandler.SevereErrorHappened += ErrorHandler_SevereErrorHappened;
                ErrorHandler.NonSevereErrorHappened += ErrorHandler_NonSevereErrorHappened;

                SetImageAction = (BitmapImage img) => { liveView.ImageSource = img; };

                RefreshCamera();
                OpenSession();
                MainCamera.SetCapacity(4096, 0x1FFFFFFF);

            }
            // TODO: Close main windows when null reference occures
            catch (NullReferenceException) { Report.Error("Check if camera is turned on and restart the program", true); }
            catch (DllNotFoundException) { Report.Error("Canon DLLs not found!", true); }
            catch (Exception ex) { Report.Error(ex.Message, true); }


        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                MainCamera?.Dispose();
                APIHandler?.Dispose();
            }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        #region TakePhoto
        private void ReadyButton_Click(object sender, EventArgs e)
        {
            betweenPhotos.Start();
            secondCounter.Start();
        }

        public void MakePhoto(object sender, EventArgs e)
        {
            try
            {
                photoNumberInTemplate++;
                photosInTemplate++;
                // MainCamera.TakePhotoAsync();
                Debug.WriteLine("taking a shot");
                MainCamera.SendCommand(CameraCommand.PressShutterButton, (int)ShutterButton.Halfway);
                Debug.WriteLine("halfway");
                MainCamera.SendCommand(CameraCommand.PressShutterButton, (int)ShutterButton.Completely_NonAF);
                Debug.WriteLine("completely_nonaf");
                MainCamera.SendCommand(CameraCommand.PressShutterButton, (int)ShutterButton.OFF);
                Debug.WriteLine("Finished taking a shot");

                betweenPhotos.Stop();
                secondCounter.Stop();

                PhotoTextBox.Visibility = Visibility.Hidden;

                timeLeftCopy = timeLeft;


                Debug.WriteLine("photo number in template: " + photoNumberInTemplate);
                Debug.WriteLine("photos in template: " + photosInTemplate);
                Debug.WriteLine("photo number: " + photoNumber);

            }

            catch (Exception ex) { Report.Error(ex.Message, false); }
            // TODO: zamiast sleppa jakas metoda ktora sprawdza czy zdjecie juz sie zrobilio i potem kolejna linia kodu-


            Thread.Sleep(2000);
            //jak mam sleep 4000 to mi nie dziala


            PhotoTextBox.Visibility = Visibility.Visible;
            PhotoTextBox.Text = "Prepare for next Photo!";

            // Waiting for photo saving 
            while (PhotoTaken == false)
            {
                Thread.Sleep(1000);
            }
            ShowPhotoThumbnail();
            PhotoTaken = false;  
            // One if than switch
            switch (layout.LayoutCode)
            {
                case "foreground_1":
                    if (Control.photoTemplate(photosInTemplate, 1))
                    {
                        var printdata = new SavePrints(printNumber);
                        printPath = printdata.PrintDirectory;
                        TemplateProcessing.foreground1(printPath);
                        printNumber++;
                        RetakePhotoMenu();
                        //BackgroundMenu();
                       //PrintMenu();
                        }
                    break;

                case "foreground_3":
                    if (Control.photoTemplate(photosInTemplate, 3))
                    {
                        var printdata = new SavePrints(printNumber);
                        printPath = printdata.PrintDirectory;
                        TemplateProcessing.foreground3(printPath);
                        printNumber++;
                        RetakePhotoMenu();
                        //BackgroundMenu();
                        //PrintMenu();
                    }
                    break;
                case "foreground_4":
                    if (Control.photoTemplate(photosInTemplate, 4))
                    {
                        var printdata = new SavePrints(printNumber);
                        printPath = printdata.PrintDirectory;
                        TemplateProcessing.foreground4(printPath);
                        printNumber++;
                        RetakePhotoMenu();
                        //BackgroundMenu();
                        //PrintMenu();
                    }
                    break;

                case "foreground_4_paski":
                    if (Control.photoTemplate(photosInTemplate, 4))
                    {
                        var printdata = new SavePrints(printNumber);
                        printPath = printdata.PrintDirectory;
                        TemplateProcessing.foreground4stripes(printPath);
                        printNumber++;
                        RetakePhotoMenu();
                        //BackgroundMenu();
                        //PrintMenu();
                    }
                    break;
                default:
                    Debug.WriteLine("bug at switch which template");
                    break;
            }

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            sliderTimer.Start();
            //TODO OR NOT WHEN NO CAMERA CONNECTED WILL CAUSE BUG WHILE CLICK STOP IN FOREGRUND MENU
            MainCamera.StopLiveView();
            photosInTemplate = 0;
            photoNumberInTemplate = 0;
            if (turnOnTemplateMenu) StartAllForegroundsWelcomeMenu();
            else StartWelcomeMenu();
            TurnOffForegroundMenu();
        }

       

        public void ShowTimeLeft(object sender, EventArgs e)
        {

            PhotoTextBox.Text = timeLeftCopy.ToString();
            timeLeftCopy--;
        }
        #endregion

        #region slider
        public void slider(object sender, EventArgs e)
        {
            var sliderData = new Slider();

            ImageBrush slide = new ImageBrush();
            //  slide.Stretch = Stretch.Uniform;

            slide.ImageSource = new BitmapImage(new Uri(sliderData.imagePath));
            Slider.Background = slide;

            //  var ratio = Math.Min(Slider.RenderSize.Width / slide.ImageSource.Width, Slider.RenderSize.Height / slide.ImageSource.Height);
            //    CreateDynamicBorder(slide.ImageSource.Width, slide.ImageSource.Height);

        }


        #endregion

        #region API Events

        private void APIHandler_CameraAdded(CanonAPI sender)
        {
            try { Dispatcher.Invoke((Action)delegate { RefreshCamera(); }); }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        private void MainCamera_StateChanged(Camera sender, StateEventID eventID, int parameter)
        {
            try { if (eventID == StateEventID.Shutdown) { Dispatcher.Invoke((Action)delegate { CloseSession(); }); } }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        private void MainCamera_LiveViewUpdated(Camera sender, Stream img)
        {
            try
            {
                using (WrapStream s = new WrapStream(img))
                {
                    img.Position = 0;
                    BitmapImage EvfImage = new BitmapImage();
                    EvfImage.BeginInit();
                    EvfImage.StreamSource = s;
                    EvfImage.CacheOption = BitmapCacheOption.OnLoad;
                    EvfImage.EndInit();
                    EvfImage.Freeze();
                    Application.Current.Dispatcher.BeginInvoke(SetImageAction, EvfImage);
                }
            }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        private void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {

            try
            {
                photoNumber++;
                var savedata = new SavePhoto(photoNumber);
                string dir = savedata.FolderDirectory;

                Info.FileName = savedata.PhotoName;
                sender.DownloadFile(Info, dir);


                //               ReSize.ImageAndSave(savedata.PhotoDirectory,photosInTemplate,templateName);
                ReSize.ImageAndSave(savedata.PhotoDirectory, photoNumberInTemplate, layout.LayoutCode);
                //TemplateProcessing.ImageAndSave(savedata.PhotoDirectory, photosInTemplate, templateName);

            }
            catch (Exception ex) { Report.Error(ex.Message, false); }

            PhotoTaken = true;

        }

        private void ErrorHandler_NonSevereErrorHappened(object sender, ErrorCode ex)
        {
            string errorCode = ((int)ex).ToString("X");
            switch (errorCode)
            {
                case "8D01": // TAKE_PICTURE_AF_NG
                    if (photoNumberInTemplate != 0)
                    {
                        photoNumberInTemplate--;
                    }
                    if (photosInTemplate != 0)
                    {
                        photosInTemplate--;
                    }
                    PhotoTaken = true;
                    Debug.WriteLine("Autofocus error");
                    return;
            }
            Report.Error($"SDK Error code: {ex} ({((int)ex).ToString("X")})", false);
        }

        private void ErrorHandler_SevereErrorHappened(object sender, Exception ex)
        {
            Report.Error(ex.Message, true);
        }

        #endregion

        #region Live view

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            sliderTimer.Stop();
            TurnOffForegroundMenu();
            SliderBorder.Visibility = Visibility.Visible;
            Slider.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Hidden;
            StartButtonMenu.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
            PhotoTextBox.Visibility = Visibility.Visible;
            PhotoTextBox.Text = "Are you ready for first picture?";

            try
            {
                MainCamera.SendCommand(CameraCommand.PressShutterButton, 1);

            }
            finally
            {
                MainCamera.SendCommand(CameraCommand.PressShutterButton, 0);
            }
            try
            {

                Slider.Background = liveView;
                MainCamera.StartLiveView();

            }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        #endregion

        #region Subroutines

        private void CloseSession()
        {
            try
            {
                MainCamera.CloseSession();
            }
            catch (ObjectDisposedException) { Report.Error("Camera has been turned off! \nPlease turned it on and restart the application", true); }
            //SettingsGroupBox.IsEnabled = false;
            //LiveViewGroupBox.IsEnabled = false;
            //SessionButton.Content = "Open Session";
            //SessionLabel.Content = "No open session";
            //StarLVButton.Content = "Start LV";
        }

        private void RefreshCamera()
        {
            CameraListBox.Items.Clear();
            CamList = APIHandler.GetCameraList();
            foreach (Camera cam in CamList) CameraListBox.Items.Add(cam.DeviceName);
            if (MainCamera?.SessionOpen == true) CameraListBox.SelectedIndex = CamList.FindIndex(t => t.ID == MainCamera.ID);
            else if (CamList.Count > 0) CameraListBox.SelectedIndex = 0;
        }

        private void OpenSession()
        {
            if (CameraListBox.SelectedIndex >= 0)
            {
                MainCamera = CamList[CameraListBox.SelectedIndex];
                MainCamera.OpenSession();
                MainCamera.LiveViewUpdated += MainCamera_LiveViewUpdated;
                MainCamera.StateChanged += MainCamera_StateChanged;
                MainCamera.DownloadReady += MainCamera_DownloadReady;
                MainCamera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host);
                MainCamera.SetCapacity(4096, 0x1FFFFFFF);

            }
        }

        private void EnableUI(bool enable)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.Invoke((Action)delegate { EnableUI(enable); });
            else
            {
                //    SettingsGroupBox.IsEnabled = enable;
                //   InitGroupBox.IsEnabled = enable;
                //     LiveViewGroupBox.IsEnabled = enable;
            }
        }



        #endregion

        #region Printing

        private void LoadAndPrint(string printPath)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(printPath);
            bi.EndInit();

            var vis = new DrawingVisual();
            var dc = vis.RenderOpen();
            dc.DrawImage(bi, new Rect { Width = bi.Width, Height = bi.Height });
            dc.Close();


            var printerSettings = new PrinterSettings();
            var labelPaperSize = new PaperSize
            {
                RawKind = (int)PaperKind.Custom,
                Height = 150,
                Width = 100
            };
            printerSettings.DefaultPageSettings.PaperSize = labelPaperSize;
            printerSettings.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            //            printerSettings.Copies = actualNumberOfCopies;
            pdialog.PrintVisual(vis, "My Image");
        }


        private void Print_Click(object sender, RoutedEventArgs e)
        {
            photosInTemplate = 0;
            photoNumberInTemplate = 0;
            Printing.Print(printPath, printerName, actualNumberOfCopies);
            actualNumberOfCopies = 1;
            if (turnOnTemplateMenu) StartAllForegroundsWelcomeMenu();
            else StartWelcomeMenu();
        }

        private void LoadSticker()
        {
            string stickerDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Sticker");
            string[] stickerFiles = Directory.GetFiles(stickerDirectory, "*.png");

            foreach (string stickerFile in stickerFiles)
            {
                Image stickerImage = new Image
                {
                    Source = new BitmapImage(new Uri(stickerFile)),
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(5)
                };
                stickerImage.MouseLeftButtonDown += StickerImage_MouseLeftButtonDown;
                StickerWrapPanel.Children.Add(stickerImage);
            }
        }

        private void StickerImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image clickedImage = sender as Image;
            if (clickedImage != null)
            {
                AddStickerToCanvas(clickedImage.Source as BitmapImage);
            }
        }

        private void AddStickerToCanvas(BitmapImage stickerImageSource)
        {

            Sticker sticker = new Sticker
            {
                Width = 100,
                Height = 100,
                ImageSource = stickerImageSource
            };

            Canvas.SetLeft(sticker, 0);
            Canvas.SetTop(sticker, 0);

            sticker.MouseLeftButtonDown += Sticker_MouseLeftButtonDown;
            sticker.MouseLeftButtonUp += Sticker_MouseLeftButtonUp;
            sticker.MouseMove += Sticker_MouseMove;

            canvasSticker.Children.Add(sticker);

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(sticker);
            if (adornerLayer != null)
            {
                var resizeAdorner = new ResizeAdorner(sticker);
                adornerLayer.Add(resizeAdorner);
            }
        }

        private void Sticker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Sticker clickedSticker)
            {
                clickedSticker.CaptureMouse();
            }
        }

        private void Sticker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Sticker clickedSticker)
            {
                clickedSticker.ReleaseMouseCapture();
            }
        }

        private void Sticker_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Sticker clickedSticker = sender as Sticker;
                if (clickedSticker != null)
                {
                    var position = e.GetPosition(ShowPrint);

                    // Calculate new position considering the sticker size
                    double newX = position.X - clickedSticker.ActualWidth / 2;
                    double newY = position.Y - clickedSticker.ActualHeight / 2;

                    // Constrain within the ShowPrint bounds
                    if (newX < 0) newX = 0;
                    if (newY < 0) newY = 0;
                    if (newX + clickedSticker.ActualWidth > ShowPrint.ActualWidth) newX = ShowPrint.ActualWidth - clickedSticker.ActualWidth;
                    if (newY + clickedSticker.ActualHeight > ShowPrint.ActualHeight) newY = ShowPrint.ActualHeight - clickedSticker.ActualHeight;

                    Canvas.SetLeft(clickedSticker, newX);
                    Canvas.SetTop(clickedSticker, newY);
                }
            }
        }

        private void RenderStickersOnImage()
        {
            // Tạo một RenderTargetBitmap với kích thước của ShowPrint
            int width = (int)ShowPrint.ActualWidth;
            int height = (int)ShowPrint.ActualHeight;
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            // Tạo một DrawingVisual và mở DrawingContext của nó
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                // Tạo một VisualBrush từ ShowPrint và vẽ nó lên DrawingContext
                VisualBrush vb = new VisualBrush(ShowPrint);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));

                // Tạo một VisualBrush từ canvasSticker và vẽ nó lên DrawingContext
                VisualBrush stickerBrush = new VisualBrush(canvasSticker);
                dc.DrawRectangle(stickerBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
            }

            // Render DrawingVisual lên RenderTargetBitmap
            renderTargetBitmap.Render(dv);

            // Cập nhật ShowPrint để hiển thị hình ảnh đã được render kèm sticker
            ShowPrint.Source = renderTargetBitmap;
        }

        private void SaveFinalImage()
        {
            HideCloseButtons();
            // Tạo một RenderTargetBitmap với kích thước của ShowPrint
            int width = (int)ShowPrint.ActualWidth;
            int height = (int)ShowPrint.ActualHeight;
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            // Tạo một DrawingVisual và mở DrawingContext của nó
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                // Tạo một VisualBrush từ ShowPrint và vẽ nó lên DrawingContext
                VisualBrush vb = new VisualBrush(ShowPrint);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));

                // Tạo một VisualBrush từ canvasSticker và vẽ nó lên DrawingContext
                VisualBrush stickerBrush = new VisualBrush(canvasSticker);
                dc.DrawRectangle(stickerBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
            }

            // Render DrawingVisual lên RenderTargetBitmap
            renderTargetBitmap.Render(dv);

            // Lưu RenderTargetBitmap thành một file PNG
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            // Đường dẫn và tên file cho ảnh cuối cùng
            string filePath = Path.Combine(currentDirectory, "Final pic.png");
            using (FileStream file = File.OpenWrite(filePath))
            {
                encoder.Save(file);
            }

            // Cập nhật đường dẫn của ảnh đã in cho PrintMenu
            printPath = filePath;
        }


        private void HideStickers()
        {
            foreach (var child in canvasSticker.Children)
            {
                if (child is Sticker sticker && sticker.Visibility == Visibility.Visible)
                {
                    // Ẩn sticker
                    sticker.Visibility = Visibility.Hidden;

                    // Loại bỏ Adorner
                    var adornerLayer = AdornerLayer.GetAdornerLayer(sticker);
                    if (adornerLayer != null)
                    {
                        var adorners = adornerLayer.GetAdorners(sticker);
                        if (adorners != null)
                        {
                            foreach (var adorner in adorners)
                            {
                                adornerLayer.Remove(adorner);
                            }
                        }
                    }
                }
            }
        }

        private void HideCloseButtons()
        {
            foreach (var child in canvasSticker.Children)
            {
                if (child is Sticker sticker)
                {
                    sticker.HideCloseButton();
                }
            }
        }

        private void NextButtonBackGround_Click(object sender, RoutedEventArgs e)
        {
            BackgroundMenu();
        }

        private void NextButtonSticker_Click(object sender, RoutedEventArgs e)
        {
            StickerMenu();
        }
        private void NextButtonPrinting_Click(object sender, RoutedEventArgs e)
        {
            
            RenderStickersOnImage();
            SaveFinalImage();
            PrintMenu();

            HideStickers();

        }

        private void RetakePhotoMenu()
        {
            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Hidden;

            PhotoTextBox.Text = "Choose a thumbnail below to retake the picture";

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPictureInlayout.Source = actualPrint;
            ShowPictureInlayout.Visibility = Visibility.Visible;

            NextButtonBackGround.Visibility = Visibility.Visible;
        }

        private void BackgroundMenu()
        {

            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Hidden;
            NextButtonBackGround.Visibility = Visibility.Hidden;
            ShowPictureInlayout.Visibility = Visibility.Hidden;

            FirstThumbnail.Visibility = Visibility.Hidden;
            SecondThumbnail.Visibility = Visibility.Hidden;
            ThirdThumbnail.Visibility = Visibility.Hidden;
            FourthThumbnail.Visibility = Visibility.Hidden;
            LeftThumbnail.Visibility = Visibility.Hidden;
            CenterThumbnail.Visibility = Visibility.Hidden;
            RightThumbnail.Visibility = Visibility.Hidden;

            BackgroundsWrapPanel.Visibility = Visibility.Visible;
            NextButtonSticker.Visibility = Visibility.Visible;

            PhotoTextBox.Text = "Choose background";
            NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPrint.Source = actualPrint;


            ShowPrint.Visibility = Visibility.Visible;
            BackgroundsWrapPanel.Visibility = Visibility.Visible;
            NextButtonSticker.Visibility = Visibility.Visible;
            LoadBackgrounds();
        }

        private void StickerMenu()
        {
            BackgroundsWrapPanel.Visibility = Visibility.Hidden;
            NextButtonSticker.Visibility = Visibility.Hidden;

            // Hiển thị StickerWrapPanel
            StickerWrapPanel.Visibility = Visibility.Visible;
            NextButtonPrinting.Visibility = Visibility.Visible;
            LoadSticker();
        }

        //cho nay hien anh de in
        private void PrintMenu()
        {
            StickerWrapPanel.Visibility = Visibility.Hidden;
            NextButtonPrinting.Visibility = Visibility.Hidden;

            PhotoTextBox.Text = "Press button to continue";
            NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPrint.Source = actualPrint;


            Print.Visibility = Visibility.Visible;
            NumberOfCopiesTextBox.Visibility = Visibility.Visible;
            AddOneCopyButton.Visibility = Visibility.Visible;
            MinusOneCopyButton.Visibility = Visibility.Visible;
            SendEmailButton.Visibility = Visibility.Visible;


            ShowPrint.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
            //        CreateDynamicBorder(ShowPrint.ActualWidth, ShowPrint.ActualHeight);
        }
        
        private void LoadBackgrounds()
        {
            BackgroundsWrapPanel.Children.Clear(); // Xóa các phần tử cũ trước khi tải mới
            string backgroundsDirectory = Path.Combine(Directory.GetCurrentDirectory(), $"Bground\\{layout.LayoutCode}");
            string[] backgroundFiles = Directory.GetFiles(backgroundsDirectory, "*.png");

            foreach (string file in backgroundFiles)
            {
                Image frame = new Image
                {
                    Source = new BitmapImage(new Uri(file)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(10),
                    Width = 200, // Đặt kích thước cố định
                    Height = 200 // Đặt kích thước cố định
                };
                frame.MouseLeftButtonDown += Background_MouseLeftButtonDown;

                BackgroundsWrapPanel.Children.Add(frame);
            }
        }
        private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image clickedBackground = sender as Image;
            string fileName = System.IO.Path.GetFileName(((BitmapImage)clickedBackground.Source).UriSource.LocalPath);
            Bitmap background = new Bitmap(Path.Combine(Directory.GetCurrentDirectory(), "Bground", layout.LayoutCode, fileName));

            // Lấy đường dẫn thư mục session hiện tại
            var savePhoto = new SavePhoto(photoNumber);
            string sessionDirectory = savePhoto.CurrentSessionDirectory();

            Bitmap result = imageProcess.OverlayBackgroundBINHTHUONG(actualPrint, background);
            //Bitmap result = imageProcess.OverlayBackground(background, sessionDirectory);
            ShowPrint.Source = imageProcess.ConvertToBitmapImageBINHTHUONG(result);

            //Image clickedBackground = sender as Image;
            //string fileName = System.IO.Path.GetFileName(((BitmapImage)clickedBackground.Source).UriSource.LocalPath);
            //Bitmap background = new Bitmap(Path.Combine(Directory.GetCurrentDirectory(), "Bground", layout.LayoutCode, fileName));

            //var savePhoto = new SavePhoto(photoNumber);
            //string sessionDirectory = savePhoto.CurrentSessionDirectory();

            //Bitmap result = imageProcess.OverlayBackground(background, sessionDirectory);

            //ShowPrint.Source = imageProcess.ConvertToBitmapImageBINHTHUONG(result);
            //ShowPrint.Visibility = Visibility.Visible;

            //// Position the ShowPrint image in the center of the canvasSticker
            //double left = (canvasSticker.ActualWidth - ShowPrint.ActualWidth) / 2;
            //double top = (canvasSticker.ActualHeight - ShowPrint.ActualHeight) / 2;

            //Canvas.SetLeft(ShowPrint, left);
            //Canvas.SetTop(ShowPrint, top);
        }

        

        private void MinusOneCopyButtonClick(object sender, RoutedEventArgs e)
        {
            if (actualNumberOfCopies > 1)
            {
                --actualNumberOfCopies;
                NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();
            }

        }

        private void AddOneCopyButtonClick(object sender, RoutedEventArgs e)
        {
            if (actualNumberOfCopies < maxCopies)
            {
                ++actualNumberOfCopies;
                NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();
            }
        }

        #endregion

        #region menu


        //doc file cau hinh
        private void FillSavedData()
        {
            string firstprinter;
            string secondprinter;

            //lay ten may in
            if (!File.Exists(Path.Combine(currentDirectory, "menusettings.xml")))
            {
                Debug.WriteLine("XMLsettings doesnt exist");
                Report.Error("XML settings cannot be found\nPlease Press F12 and update settings", true);
                return;
            }
            try
            {
                actualSettings = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(currentDirectory, "menusettings.xml"));
                actualSettings.Root.Elements("setting");
                layout.LayoutCode = actualSettings.Root.Element("actualTemplate").Value;
                if (actualSettings.Root.Element("actualTemplate").Value == "All")
                {
                    turnOnTemplateMenu = true;
                }
                firstprinter = actualSettings.Root.Element("actualPrinter").Value;
                secondprinter = actualSettings.Root.Element("secondPrinter").Value;
                maxCopies = System.Convert.ToInt32(actualSettings.Root.Element("maxNumberOfCopies").Value);
                timeLeft = System.Convert.ToInt32(actualSettings.Root.Element("timeBetweenPhotos").Value);
                printtime = System.Convert.ToInt32(actualSettings.Root.Element("printingTime").Value);
                printerName = FBoothApp.Printing.ActualPrinter(layout.LayoutCode, firstprinter, secondprinter);
                timeLeftCopy = timeLeft;

                SmtpServerName = actualSettings.Root.Element("SmtpServerName").Value;
                SmtpPortNumber = actualSettings.Root.Element("SmtpPortNumber").Value;
                EmailHostAddress = actualSettings.Root.Element("EmailHostAddress").Value;
                EmailHostPassword = actualSettings.Root.Element("EmailHostPassword").Value;


            }
            catch (XmlException e)
            {
                Debug.WriteLine("XMLsettings cannot be load properly");
                Report.Error("XML settings cannot be load properly\nPlease Press F12 and update settings", true);
            }
        }



        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                Menu menu1 = new Menu();
                this.Content = menu1;
            }
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }
        #endregion

        public void ActivateTimers()
        {
            sliderTimer = new System.Windows.Threading.DispatcherTimer();
            sliderTimer.Tick += new EventHandler(slider);
            sliderTimer.Interval = new TimeSpan(0, 0, 0, 2);
            sliderTimer.Start();

            betweenPhotos = new System.Windows.Threading.DispatcherTimer();
            betweenPhotos.Tick += new EventHandler(MakePhoto);
            betweenPhotos.Interval = new TimeSpan(0, 0, 0, timeLeft);

            secondCounter = new System.Windows.Threading.DispatcherTimer();
            secondCounter.Tick += new EventHandler(ShowTimeLeft);
            secondCounter.Interval = new TimeSpan(0, 0, 0, 0, 900);
        }

        #region Foreground_Menu


        //Chỗ này chọn templateName
        private void Foreground_3_button_Click(object sender, RoutedEventArgs e)
        {
            layout.LayoutCode = "foreground_3";
            StartButton_Click(sender, e);
        }
        private void Foreground_4_button_Click(object sender, RoutedEventArgs e)
        {
            layout.LayoutCode = "foreground_4";
            StartButton_Click(sender, e);
        }
        private void Foreground_1_button_Click(object sender, RoutedEventArgs e)
        {
            layout.LayoutCode = "foreground_1";
            StartButton_Click(sender, e);
        }
        private void Foreground_4_paski_button_Click(object sender, RoutedEventArgs e)
        {
            layout.LayoutCode = "foreground_4_paski";
            StartButton_Click(sender, e);
        }


        private void StartButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            TurnOnForegroundMenu();
        }
        public void TurnOnForegroundMenu()
        {
            sliderTimer.Stop();
            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;

            Foreground_1_button.Visibility = Visibility.Visible;
            Foreground_3_button.Visibility = Visibility.Visible;
            Foreground_4_button.Visibility = Visibility.Visible;
            Foreground_4_paski_button.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;

        }
        public void TurnOffForegroundMenu()
        {
            Foreground_1_button.Visibility = Visibility.Hidden;
            Foreground_3_button.Visibility = Visibility.Hidden;
            Foreground_4_button.Visibility = Visibility.Hidden;
            Foreground_4_paski_button.Visibility = Visibility.Hidden;
        }
        public void StartAllForegroundsWelcomeMenu()
        {
            PhotoTextBox.Visibility = Visibility.Visible;
            PhotoTextBox.Text = "Hello";
            SliderBorder.Visibility = Visibility.Visible;
            StartButtonMenu.Visibility = Visibility.Visible;
            Slider.Visibility = Visibility.Visible;
            sliderTimer.Start();

            StopButton.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            Print.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;
            NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
            AddOneCopyButton.Visibility = Visibility.Hidden;
            MinusOneCopyButton.Visibility = Visibility.Hidden;
            SendEmailButton.Visibility = Visibility.Hidden;
            FirstThumbnail.Visibility = Visibility.Hidden;
            SecondThumbnail.Visibility = Visibility.Hidden;
            ThirdThumbnail.Visibility = Visibility.Hidden;
            FourthThumbnail.Visibility = Visibility.Hidden;
            LeftThumbnail.Visibility = Visibility.Hidden;
            CenterThumbnail.Visibility = Visibility.Hidden;
            RightThumbnail.Visibility = Visibility.Hidden;

            //SliderBorderTakingphoto.Visibility = Visibility.Hidden;
            //SliderTakingPhoto.Visibility = Visibility.Hidden;
        }
        public void CheckTemplate()
        {

            if (turnOnTemplateMenu)
            {
                StartButtonMenu.Visibility = Visibility.Visible;
            }
            else
            {
                StartButton.Visibility = Visibility.Visible;
            }
        }
        #endregion
        #region frontend
        private void CreateDynamicBorder(double width, double height)
        {
            var printBorder = new Border();
            // border.Background = new SolidColorBrush(Colors.LightGray);
            printBorder.BorderThickness = new Thickness(10);
            printBorder.BorderBrush = new SolidColorBrush(Colors.Coral);
            //   border.CornerRadius = new CornerRadius(15);
            printBorder.Width = width;
            printBorder.Height = height;


        }
        private void StartWelcomeMenu()
        {
            PhotoTextBox.Visibility = Visibility.Visible;
            PhotoTextBox.Text = "Hello";

            sliderTimer.Start();
            SliderBorder.Visibility = Visibility.Visible;
            Slider.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Visible;

            StopButton.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            Print.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;
            NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
            AddOneCopyButton.Visibility = Visibility.Hidden;
            MinusOneCopyButton.Visibility = Visibility.Hidden;
            SendEmailButton.Visibility = Visibility.Hidden;
            FirstThumbnail.Visibility = Visibility.Hidden;
            SecondThumbnail.Visibility = Visibility.Hidden;
            ThirdThumbnail.Visibility = Visibility.Hidden;
            FourthThumbnail.Visibility = Visibility.Hidden;
            LeftThumbnail.Visibility = Visibility.Hidden;
            CenterThumbnail.Visibility = Visibility.Hidden;
            RightThumbnail.Visibility = Visibility.Hidden;
        }
        #endregion

        private void SendEmailButtonClick(object sender, RoutedEventArgs e)
        {
            // Mở bàn phím ảo
            //Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + "osk.exe");

            EmailSendDialog inputEmailSendDialog = new EmailSendDialog("Please enter your email address:", "name@example.com");
            if (inputEmailSendDialog.ShowDialog() == true)
            {
                Debug.WriteLine("inputemailsend is ok, answer is :" + inputEmailSendDialog.Answer);
                EmailSender emailSender = new EmailSender();
                switch (layout.LayoutCode)
                {
                    case "foreground_1":
                        emailSender.SendEmail(photoNumber, 1, inputEmailSendDialog.Answer, printPath);
                        break;

                    case "foreground_3":
                        emailSender.SendEmail(photoNumber, 3, inputEmailSendDialog.Answer, printPath);
                        break;
                    case "foreground_4":
                        emailSender.SendEmail(photoNumber, 4, inputEmailSendDialog.Answer, printPath);
                        break;

                    case "foreground_4_paski":
                        emailSender.SendEmail(photoNumber, 4, inputEmailSendDialog.Answer, printPath);
                        break;
                    default:
                        Debug.WriteLine("bug at switch which template in email send button");
                        break;
                }
            }
        }

        //show hinh anh thu nho
        public void ShowPhotoThumbnail()
        {
            var getImageThumbnail = new GetImageThumbnail();

            ImageBrush thumbnailImageBrush = new ImageBrush();
            getImageThumbnail.GetThumbnailPath();

            switch (layout.LayoutCode)
            {
                case "foreground_1":
                    if (photoNumberInTemplate == 1)
                    {
                        CenterThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                        CenterThumbnail.Visibility = Visibility.Visible;
                    }
                    break;


                case "foreground_3":
                    switch (photoNumberInTemplate)
                    {
                        case 1:
                            LeftThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            LeftThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 2:
                            CenterThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            CenterThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 3:
                            RightThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            RightThumbnail.Visibility = Visibility.Visible;
                            break;
                        default:
                            Debug.WriteLine("bug at switch which template in ShowPhotoThumbnail - foreground3");
                            Debug.WriteLine("bug because photoNumberInTemplate = " + photoNumberInTemplate);

                            break;
                    }
                    break;
                case "foreground_4":
                    switch (photoNumberInTemplate)
                    {
                        case 1:
                            FirstThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            FirstThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 2:
                            SecondThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            SecondThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 3:
                            ThirdThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            ThirdThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 4:
                            FourthThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            FourthThumbnail.Visibility = Visibility.Visible;
                            break;

                        default:
                            Debug.WriteLine("bug at switch which template in ShowPhotoThumbnail - foreground 4");
                            Debug.WriteLine("bug because photoNumberInTemplate = " + photoNumberInTemplate);

                            break;
                    }
                    break;

                case "foreground_4_paski":
                    switch (photoNumberInTemplate)
                    {
                        case 1:
                            FirstThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            FirstThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 2:
                            SecondThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            SecondThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 3:
                            ThirdThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            ThirdThumbnail.Visibility = Visibility.Visible;
                            break;

                        case 4:
                            FourthThumbnailImage.Source = new BitmapImage(new Uri(getImageThumbnail.thumbnailPath));
                            FourthThumbnail.Visibility = Visibility.Visible;
                            break;

                        default:
                            Debug.WriteLine("bug at switch which template in ShowPhotoThumbnail = foreground 4 paski");
                            Debug.WriteLine("bug because photoNumberInTemplate = " + photoNumberInTemplate);
                            break;
                    }
                    break;
                default:
                    Debug.WriteLine("bug at switch which template in showphotothumbnail");
                    break;
            }
        }

        private void LeftThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 1);
        }

        private void CenterThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            switch (layout.LayoutCode)
            {
                case "foreground_1":
                    RepeatJustTakenPhoto(sender, e, 1);
                    break;
                case "foreground_3":
                    RepeatJustTakenPhoto(sender, e, 2);
                    break;
                default:
                    Debug.WriteLine("Bug at centerThumbnail_OnClick");
                    break;
            }
        }

        private void RightThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 3);
        }

        private void FirstThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 1);
        }

        private void SecondThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 2);
        }

        private void ThirdThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 3);
        }

        private void FourthThumbnail_OnClick(object sender, RoutedEventArgs e)
        {
            RepeatJustTakenPhoto(sender, e, 4);
        }

        public void RepeatJustTakenPhoto(object sender, RoutedEventArgs e, int photoNumberToRepeat)
        {
            RepeatPhotoDialog repeatPhotoDialog = new RepeatPhotoDialog();
            if (repeatPhotoDialog.ShowDialog() == true)
            {
                photosInTemplate--;
                photoNumberInTemplate = photoNumberToRepeat - 1;
                ShowPictureInlayout.Visibility = Visibility.Hidden;
                Print.Visibility = Visibility.Hidden;
                ShowPrint.Visibility = Visibility.Hidden;
                NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
                AddOneCopyButton.Visibility = Visibility.Hidden;
                MinusOneCopyButton.Visibility = Visibility.Hidden;
                SendEmailButton.Visibility = Visibility.Hidden;

                StartButton_Click(sender, e);
                //TODO: Repeat selected photo, not only the last one like now
            }
        }
    }
}


