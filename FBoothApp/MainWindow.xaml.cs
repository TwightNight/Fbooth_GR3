using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using EOSDigital.API;
using EOSDigital.SDK;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using FBoothApp.Classes;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;
using FBoothApp.Entity;
using FBoothApp.Services;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FBoothApp.Entity.Request;
using FBoothApp.Entity.Enum;
using FBoothApp.Entity.Reponse;
using QRCoder;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using FBoothApp.Helpers;


namespace FBoothApp
{
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
        BackGroundProcess backGroundProcess;


        XDocument actualSettings = new XDocument();
        List<System.Windows.Media.ImageSource> resizedImages = new List<System.Windows.Media.ImageSource>();

        public int photoNumber = 0; // Thứ tự của ảnh được chụp
        public int photoNumberInTemplate = 0;
        int photosInTemplate = 0; //kiem tra so luong anh co dung voi slot trong layout khong
        static int PrintTime = 0;
        static int EmailSeindingTime = 0;

        int timeLeft = 5;
        int timeLeftCopy = 5;
        int printNumber = 0;
        int maxCopies = 1;
        short actualNumberOfCopies = 1;
        int printTime = 10;


        public string SmtpServerName;
        public string SmtpPortNumber;
        public string EmailHostAddress;
        public string EmailHostPassword;

        string printPath = string.Empty;

        private Layout layout;
        private FetchApiServices _apiServices;

        //public string templateName = string.Empty;
        string printerName = string.Empty;
        private string currentDirectory = Environment.CurrentDirectory;
        //public bool turnOnTemplateMenu = false;
        public bool PhotoTaken = false;


        public MainWindow()
        {
            layout = new Layout();
            _apiServices = new FetchApiServices();
            backGroundProcess = new BackGroundProcess();
            InitializeComponent();
            FillSavedData();
            ActivateTimers();
            //CheckTemplate();

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
            ReadyButton.Visibility = Visibility.Hidden;

            betweenPhotos.Start();
            secondCounter.Start();
        }


        public async void MakePhoto(object sender, EventArgs e)
        {
            try
            {
                SetThumbnailButtonsEnabled(false);

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
                ReadyButton.Visibility = Visibility.Visible;

                timeLeftCopy = timeLeft;


                Debug.WriteLine("photo number in template: " + photoNumberInTemplate);
                Debug.WriteLine("photos in template: " + photosInTemplate);
                Debug.WriteLine("photo number: " + photoNumber);

                Thread.Sleep(2000);

                PhotoTextBox.Visibility = Visibility.Visible;
                ThumbnailDockPanel.Visibility = Visibility.Visible;
                ThumbnailDockPanelGrid.Visibility = Visibility.Visible;

                PhotoTextBox.FontSize = 24;
                PhotoTextBox.Text = "";

                // Waiting for photo saving 
                while (PhotoTaken == false)
                {
                    Thread.Sleep(1000);
                }

                if (isRetake)
                {
                    UpdatePhotoAfterRetake();
                }
                else
                {
                    ShowPhotoThumbnail();
                }


                var layouts = await _apiServices.GetLayoutsAsync();
                var currentLayout = layouts.FirstOrDefault(l => l.LayoutCode == layout.LayoutCode);

                if (Control.photoTemplate(photosInTemplate, layout.PhotoSlot))
                {

                    // Show loading screen
                    LoadingGrid.Visibility = Visibility.Visible;
                    PhotoTextBox.Visibility = Visibility.Hidden;
                    ReadyButton.Visibility = Visibility.Hidden;

                    if (currentLayout != null)
                    {
                        await Task.Run(() =>
                        {
                            var printdata = new SavePrints(printNumber, BookingID);
                            printPath = printdata.PrintDirectory;
                            LayoutProcess.ProcessLayout(currentLayout, printPath);
                            printNumber++;
                        });
                        RetakePhotoMenu();

                        if (isRetake)
                        {
                            UpdatePhotoAfterRetake();
                        }
                    }
                    // Hide loading screen
                    LoadingGrid.Visibility = Visibility.Hidden;
                    PhotoTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    Debug.WriteLine("Not enough photos in the layout yet");
                }

            }
            catch (Exception ex)
            {
                Report.Error(ex.Message + "loi chup anh", false);
            }
        }



        private async void LoadLayouts()
        {
            LayoutTabControl.Items.Clear();
            var layouts = await _apiServices.GetLayoutsAsync();
            var sticker = await _apiServices.GetStickerTypesAsync();
            var photoSlots = layouts.GroupBy(layout => layout.PhotoSlot).OrderBy(group => group.Key);

            foreach (var photoSlot in photoSlots)
            {
                var tabItem = new TabItem
                {
                    Header = $"{photoSlot.Key} - Spot Frame",
                    Style = (Style)FindResource("TabItemStyle") // Apply the TabItemStyle
                };

                var scrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
                };

                var wrapPanel = new WrapPanel
                {
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Left, // Align items to the left
                    VerticalAlignment = VerticalAlignment.Top, // Align items to the top
                    Width = double.NaN, // Auto size to fill parent
                    Height = double.NaN, // Auto size to fill parent
                    Orientation = Orientation.Horizontal
                };

                foreach (var layout in photoSlot)
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(layout.LayoutURL)),
                        Tag = layout, // Store the layout ID in the Tag property
                        Margin = new Thickness(5),
                        Width = 150, // Set desired width
                        Height = 150, // Set desired height
                        Stretch = System.Windows.Media.Stretch.Uniform // Maintain aspect ratio
                    };
                    image.MouseLeftButtonDown += Layout_Click;

                    var tickImage = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/backgrounds/check.png")),
                        Width = 30,
                        Height = 30,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Visibility = Visibility.Hidden // Initially hidden
                    };

                    var grid = new Grid();
                    grid.Children.Add(image);
                    grid.Children.Add(tickImage);

                    var border = new Border
                    {
                        Child = grid,
                        BorderBrush = System.Windows.Media.Brushes.Transparent,
                        BorderThickness = new Thickness(3)
                    };

                    wrapPanel.Children.Add(border);
                }

                scrollViewer.Content = wrapPanel;
                tabItem.Content = scrollViewer;
                LayoutTabControl.Items.Add(tabItem);
            }
        }


        private Border selectedBorder;
        private void Layout_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clickedImage = sender as Image;
            if (clickedImage != null)
            {
                var layoutChoosen = clickedImage.Tag as Layout;
                layout = layoutChoosen;

                var parentGrid = clickedImage.Parent as Grid;
                var tickImage = parentGrid?.Children.OfType<Image>().FirstOrDefault(img => img.Source.ToString().Contains("check.png"));

                if (tickImage != null)
                {
                    // If the layout is already selected, deselect it
                    if (tickImage.Visibility == Visibility.Visible)
                    {
                        tickImage.Visibility = Visibility.Hidden;
                        if (selectedBorder != null)
                        {
                            selectedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        }
                        selectedBorder = null;
                        NextButtonTakingPhoto.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        // Remove the previous selection border if any
                        if (selectedBorder != null)
                        {
                            var previousGrid = selectedBorder.Child as Grid;
                            var previousTickImage = previousGrid?.Children.OfType<Image>().FirstOrDefault(img => img.Source.ToString().Contains("check.png"));
                            if (previousTickImage != null)
                            {
                                previousTickImage.Visibility = Visibility.Hidden;
                            }
                            selectedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        }

                        // Select the new layout
                        tickImage.Visibility = Visibility.Visible;
                        selectedBorder = parentGrid.Parent as Border;
                        if (selectedBorder != null)
                        {
                            selectedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        }

                        // Show the NextButtonTakingPhoto button
                        NextButtonTakingPhoto.Visibility = Visibility.Visible;
                    }
                }
            }
        }


        private void NextButtonTakingPhoto_Click(object sender, RoutedEventArgs e)
        {
            // Bắt đầu hiệu ứng fade-out
            var fadeOutStoryboard = (Storyboard)this.FindResource("FadeOutStoryboard");
            fadeOutStoryboard.Begin();
            Overlay.Visibility = Visibility.Collapsed;
            Overlay.IsHitTestVisible = false;

            NextButtonTakingPhoto.Visibility = Visibility.Hidden;
            StartButton_Click(sender, e);
        }

        private void DrawGridLines()
        {
            double width = GridCanvasLiveViewImage.ActualWidth;
            double height = GridCanvasLiveViewImage.ActualHeight;

            // Clear any existing children
            GridCanvasLiveViewImage.Children.Clear();

            // Draw vertical lines
            for (int i = 1; i < 3; i++)
            {
                double x = width * i / 3;
                Line verticalLine = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = System.Windows.Media.Brushes.White,
                    StrokeThickness = 1
                };
                GridCanvasLiveViewImage.Children.Add(verticalLine);
            }

            // Draw horizontal lines
            for (int i = 1; i < 3; i++)
            {
                double y = height * i / 3;
                Line horizontalLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = System.Windows.Media.Brushes.White,
                    StrokeThickness = 1
                };
                GridCanvasLiveViewImage.Children.Add(horizontalLine);
            }
        }

        //private void StopButton_Click(object sender, RoutedEventArgs e)
        //{
        //    sliderTimer.Start();
        //    //TODO OR NOT WHEN NO CAMERA CONNECTED WILL CAUSE BUG WHILE CLICK STOP IN FOREGRUND MENU
        //    MainCamera.StopLiveView();
        //    photosInTemplate = 0;
        //    photoNumberInTemplate = 0;
        //    if (turnOnTemplateMenu) StartLayoutsWelcomeMenu();
        //    else StartWelcomeMenu();
        //    TurnOffForegroundMenu();
        //}



        public void ShowTimeLeft(object sender, EventArgs e)
        {
            if (timeLeftCopy > 0)
            {
                PhotoTextBox.Text = timeLeftCopy.ToString();
                PhotoTextBox.FontSize = 100;

                // Tạo hoạt ảnh mờ dần và rõ dần
                DoubleAnimation fadeInOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = new Duration(TimeSpan.FromSeconds(1)),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(1)
                };

                // Bắt đầu hoạt ảnh
                PhotoTextBox.BeginAnimation(UIElement.OpacityProperty, fadeInOutAnimation);

                timeLeftCopy--;
            }
            else
            {
                // Khi đếm ngược kết thúc, đặt lại Opacity của PhotoTextBox về 1 và hiển thị thông điệp chuẩn bị chụp ảnh
                PhotoTextBox.Opacity = 1;
                PhotoTextBox.FontSize = 24;
                secondCounter.Stop();
            }
        }

        #endregion

        #region slider
        public void slider(object sender, EventArgs e)
        {
            var sliderData = new Slider();

            ImageBrush slide = new ImageBrush();
            slide.ImageSource = new BitmapImage(new Uri(sliderData.imagePath));
            slide.Stretch = Stretch.UniformToFill;

            // Apply the ImageBrush to the nested Border's background
            ImageBorder.Background = slide;

            var storyboard = (Storyboard)FindResource("ImageTransitionStoryboard");
            storyboard.Begin(Slider);
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
                    //Application.Current.Dispatcher.BeginInvoke(SetImageAction, EvfImage);
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LiveViewImage.Source = EvfImage;
                    }));
                }
            }
            catch (Exception ex) { Report.Error(ex.Message, false); }
        }

        private async void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {

            try
            {
                photoNumber++;
                var savedata = new SavePhoto(photoNumber, BookingID);
                string dir = savedata.FolderDirectory;
                string sessionFolder = savedata.GetSessionFolder();

                Info.FileName = savedata.PhotoName;
                sender.DownloadFile(Info, dir);
                if (sessionFolder != null)
                {
                    var sessionStartTime = DateTime.Now;
                    int totalPhotosTaken = savedata.CountPhotosInSession();
                    await CreatePhotoSessionAsync(BookingID, layout.LayoutID, sessionFolder, sessionStartTime);
                }

                ReSize.ImageAndSave(savedata.PhotoDirectory, photoNumberInTemplate, layout);

            }
            catch (Exception ex) { Report.Error(ex.Message + "loi resize", false); }

            PhotoTaken = true;

        }

        private Guid _currentSessionId;
        public async Task CreatePhotoSessionAsync(Guid bookingID, Guid layoutID, string sessionName, DateTime startTime)
        {
            var photoSessionRequest = new CreatePhotoSessionRequest
            {
                SessionName = sessionName,
                StartTime = startTime,
                LayoutID = layoutID,
                BookingID = bookingID
            };

            try
            {
                var response = await _apiServices.CreatePhotoSessionAsync(photoSessionRequest);

                if (response != null)
                {
                    _currentSessionId = response.PhotoSessionID;
                    Debug.WriteLine($"Photo session created successfully with ID: {_currentSessionId}");
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu xảy ra lỗi
                Debug.WriteLine($"Failed to create photo session: {ex.Message}");
            }
        }


        private async void TakeNewPictureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lấy thông tin booking hiện tại từ API (nếu cần)
                var booking = await _apiServices.GetBookingByIdAsync(BookingID);

                // Kiểm tra hiệu lực của booking
                if (IsBookingValid(booking.StartTime, booking.EndTime))
                {
                    // Cập nhật phiên làm việc hiện tại trước khi chụp lại ảnh
                    var savedata = new SavePhoto(photoNumber, BookingID);
                    var updateRequest = new UpdatePhotoSessionRequest
                    {
                        TotalPhotoTaken = savedata.CountPhotosInSession(),
                        Status = Entity.Enum.PhotoSessionStatus.Ended
                    };

                    //await _apiServices.UpdatePhotoSessionAsync(_currentSessionId, updateRequest);

                    // Tiếp tục quy trình chụp ảnh
                    photoNumber = 0;
                    photosInTemplate = 0;
                    photoNumberInTemplate = 0;
                    printNumber = 0;
                    SavePhoto.CurrentSessionPath = null;

                    HomeText.Visibility = Visibility.Visible;
                    /*RoundedTextBox.Clear();
                    RoundedTextBox_LostFocus(sender, e);*/
                    BorderPanel.Visibility = Visibility.Visible;

                    TurnOnLayoutMenu();
                    BookingPhotoThumbnailGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Booking đã hết hạn, yêu cầu check-in lại
                    MessageBox.Show("Your booking has expired. Please check-in again.", "Booking Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Thực hiện hành động cần thiết để yêu cầu người dùng check-in lại
                    // Ví dụ: Mở lại màn hình check-in
                    TurnOffLayoutMenu();
                    Overlay.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update photo session: {ex.Message}");
            }
        }

        private bool IsBookingValid(DateTime startTime, DateTime endTime)
        {
            DateTime currentTime = DateTime.Now;
            return currentTime >= startTime && currentTime <= endTime;
        }

        private void ErrorHandler_NonSevereErrorHappened(object sender, ErrorCode ex)
        {
            string errorCode = ((int)ex).ToString("X");
            switch (errorCode)
            {

                case "8D01": // TAKE_PICTURE_AF_NG
                    Debug.WriteLine("Autofocus error, ignoring and continuing.");
                    PhotoTaken = true;
                    return;

                    //case "8D01": // TAKE_PICTURE_AF_NG
                    //    if (photoNumberInTemplate != 0)
                    //    {
                    //        photoNumberInTemplate--;
                    //    }
                    //    if (photosInTemplate != 0)
                    //    {
                    //        photosInTemplate--;
                    //    }
                    //    PhotoTaken = true;
                    //    Debug.WriteLine("Autofocus error");
                    //    return;
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
            TurnOffLayoutMenu();

            SliderBorder.Visibility = Visibility.Hidden;
            Slider.Visibility = Visibility.Hidden;
            //StartButton.Visibility = Visibility.Hidden;

            //StartButtonMenu.Visibility = Visibility.Hidden;
            //InputGrid.Visibility = Visibility.Hidden;
            HomeText.Visibility = Visibility.Hidden;
            BorderPanel.Visibility = Visibility.Hidden;

            ReadyButton.Visibility = Visibility.Visible;
            //StopButton.Visibility = Visibility.Visible;
            PhotoTextBox.Visibility = Visibility.Visible;


            PhotoTextBox.Text = "Click the lens to take photo";

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
                //Slider.Background = liveView;

                MainCamera.StartLiveView();
                LiveViewImage.Visibility = Visibility.Visible;

                DrawGridLines();
                GridCanvasLiveViewImage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Report.Error(ex.Message, false);
            }
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
                //MainCamera.SetSetting(PropertyID.ImageQuality, (int)ImageQuality.LargeFineJPEG);
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


        #region Layout_Menu

        private Guid BoothID;
        private Guid BookingID;

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendButton.IsEnabled = false;
            SendButton.Visibility = Visibility.Hidden;
            //LoadingProgressBarContainer.Visibility = Visibility.Visible;
            StartLoadingAnimation();

            try
            {
                var checkinCode = long.Parse("985034");
                Guid BoothID = Guid.Parse("28110B4A-BF04-4C04-A19B-1B91D976EE7C");

                var request = new CheckinRequest
                {
                    BoothID = BoothID,
                    Code = checkinCode
                };

                var response = await _apiServices.CheckinAsync(request);
                if (response != null)
                {
                    var bookingIDresponse = response.BookingID;
                    BookingID = bookingIDresponse;

                    // Handle the response (e.g., show a message, update the UI)
                    // Tính toán tổng thời gian thuê và thời gian kết thúc
                    var rentalDuration = response.EndTime - response.StartTime;
                    var checkinBookingStartTime = DateTime.Now;
                    var formattedDuration = $"{rentalDuration.Hours} hours and {rentalDuration.Minutes} minutes";
                    var endTimeFormatted = response.EndTime.ToString("HH:mm");

                    // Hiển thị thông báo thành công
                    var successMessageBox = new CustomMessageBox($"Check-in successful at: {checkinBookingStartTime}!\nTotal rental time: {formattedDuration}\nRental time ends at: {endTimeFormatted}");
                    successMessageBox.ShowDialog();
                    if (successMessageBox.DialogResult == true)
                    {
                        // Process the response (e.g., show booking details)
                        bookingEndTime = response.EndTime;

                        StartBookingTimeRemainingCheck();

                        LoadBookedServices(response.BookingServices);

                        var availableServices = await _apiServices.GetAvailableServicesAsync();
                        LoadAvailableServices(availableServices);

                        // Turn on layout menu after successful check-in
                        TurnOnLayoutMenu();
                    }
                }
            }
            catch (Exception ex)
            {
                // Show the error message returned by the API
                var errorMessageBox = new CustomMessageBox(ex.Message);
                errorMessageBox.ShowDialog();
            }
            finally
            {
                SendButton.IsEnabled = true;
                //LoadingProgressBarContainer.Visibility = Visibility.Collapsed;
                SendButton.Visibility = Visibility.Visible;
                StopLoadingAnimation();
            }
        }

        private void StartLoadingAnimation()
        {
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            //LoadingRotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void StopLoadingAnimation()
        {
            //LoadingRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
        }

        private DispatcherTimer checkEndTimeTimer;
        private DateTime bookingEndTime;

        private TimeSpan timeRemaining;

        private void StartBookingTimeRemainingCheck()
        {
            if (checkEndTimeTimer == null)
            {
                checkEndTimeTimer = new DispatcherTimer();
                checkEndTimeTimer.Interval = TimeSpan.FromSeconds(1);
                checkEndTimeTimer.Tick += CheckEndTimeTimer_Tick;
            }
            checkEndTimeTimer.Start();
        }

        private void CheckEndTimeTimer_Tick(object sender, EventArgs e)
        {
            // Tính toán thời gian còn lại
            timeRemaining = bookingEndTime - DateTime.Now;

            if (timeRemaining.TotalSeconds > 0)
            {
                CountdownTextBlock.Visibility = Visibility.Visible;
                CountdownTextBlock.Text = $"Time remaining: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }
            else
            {
                // Ngay khi hết giờ, dừng bộ đếm thời gian để tránh việc lặp lại
                checkEndTimeTimer.Stop();
                checkEndTimeTimer = null; // Xóa bộ đếm thời gian để đảm bảo nó không bị kích hoạt lại

                // Hiển thị thông báo và đóng booking
                var endMessageBox = new CustomMessageBox("Your session has ended. The application will now reset.");
                endMessageBox.ShowDialog();

                // Gọi CloseBookingAsync để đóng booking và xử lý các logic còn lại
                ResetBookingState();
            }
        }


        private void HideAllElementsRecursive(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i) as UIElement;
                if (child != null)
                {
                    child.Visibility = Visibility.Collapsed;
                    HideAllElementsRecursive(child);
                }
            }
        }


        public void TurnOnLayoutMenu()
        {
            sliderTimer.Stop();
            Slider.Visibility = Visibility.Hidden;
            //SliderBorder.Visibility = Visibility.Hidden;
            ////InputGrid.Visibility = Visibility.Hidden;
            //HomeText.Visibility = Visibility.Hidden;
            //BorderPanel.Visibility = Visibility.Hidden;

            Overlay.Visibility = Visibility.Visible;
            Overlay.IsHitTestVisible = true;

            // Bắt đầu hiệu ứng fade-in
            var fadeInStoryboard = (Storyboard)this.FindResource("FadeInStoryboard");
            fadeInStoryboard.Begin();

            LayoutTabControlGrid.Visibility = Visibility.Visible;
            LayoutTabControl.Visibility = Visibility.Visible;


            LoadLayouts();
        }
        public void TurnOffLayoutMenu()
        {
            //LayoutsWrapPanel.Visibility = Visibility.Hidden;
            //LayoutScrollViewer.Visibility = Visibility.Hidden;
            LayoutTabControlGrid.Visibility = Visibility.Hidden;
            LayoutTabControl.Visibility = Visibility.Hidden;
        }
        //public void StartLayoutsWelcomeMenu()
        //{
        //    PhotoTextBox.Visibility = Visibility.Visible;
        //    PhotoTextBox.Text = "Hello";
        //    SliderBorder.Visibility = Visibility.Visible;

        //    //StartButtonMenu.Visibility = Visibility.Visible;

        //    Slider.Visibility = Visibility.Visible;
        //    sliderTimer.Start();

        //    //StopButton.Visibility = Visibility.Hidden;
        //    ReadyButton.Visibility = Visibility.Hidden;
        //    PrintButton.Visibility = Visibility.Hidden;
        //    ShowPrint.Visibility = Visibility.Hidden;

        //    //NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
        //    //AddOneCopyButton.Visibility = Visibility.Hidden;
        //    //MinusOneCopyButton.Visibility = Visibility.Hidden;
        //    PrintMenuGrid.Visibility = Visibility.Collapsed;
        //    SendEmailButton.Visibility = Visibility.Hidden;

        //    ThumbnailDockPanel.Visibility = Visibility.Hidden;
        //    ThumbnailDockPanelGrid.Visibility = Visibility.Hidden;
        //    SliderBorder.Visibility = Visibility.Hidden;
        //    Slider.Visibility = Visibility.Hidden;
        //}
        //public void CheckTemplate()
        //{

        //    if (turnOnTemplateMenu)
        //    {
        //        //StartButtonMenu.Visibility = Visibility.Visible;
        //        SendButton.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        //StartButton.Visibility = Visibility.Visible;
        //    }
        //}


        #endregion



        #region BackgroundMenu
        private void NextButtonBackGround_Click(object sender, RoutedEventArgs e)
        {
            BackgroundMenu();
        }

        private void BackgroundMenu()
        {
            //PhotoTextBox.Text = "Enhance your photo with backgrounds and stickers!";

            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            NextButtonBackGround.Visibility = Visibility.Hidden;
            ShowPictureInlayout.Visibility = Visibility.Hidden;
            GridShowPictureInlayout.Visibility = Visibility.Collapsed;
            ThumbnailDockPanel.Visibility = Visibility.Hidden;
            ThumbnailDockPanelGrid.Visibility = Visibility.Hidden;
            NextButtonSticker.Visibility = Visibility.Hidden;

            NextButtonPrinting.Visibility = Visibility.Visible;
            showStickerAndBackGround.Visibility = Visibility.Visible;
            BackgoundScrollViewer.Visibility = Visibility.Visible;
            BackgroundsWrapPanel.Visibility = Visibility.Visible;

            StickerScrollViewer.Visibility = Visibility.Visible;
            StickerWrapPanel.Visibility = Visibility.Visible;

            NextButtonSticker.Visibility = Visibility.Visible;
            GirdShowPrintPicture.Visibility = Visibility.Visible;

            //NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPrint.Source = actualPrint;


            ShowPrint.Visibility = Visibility.Visible;
            NextButtonSticker.Visibility = Visibility.Visible;
            LoadBackgrounds();
        }

        private void BackgroundTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl == StickerAndBackGroundTabControl) // Kiểm tra đúng tabControl
            {
                if (tabControl.SelectedItem is TabItem selectedTab && selectedTab.Header != null)
                {
                    string header = selectedTab.Header.ToString();
                    if (header == "BackGrounds")
                    {
                        LoadBackgrounds();
                    }
                    else if (header == "Stickers")
                    {
                        LoadSticker();
                    }
                }
            }
        }

        private void LoadBackgrounds()
        {
            BackgroundsWrapPanel.Children.Clear(); // Xóa các phần tử cũ trước khi tải mới
            string backgroundsDirectory = Path.Combine(Directory.GetCurrentDirectory(), $"Backgrounds\\{layout.LayoutCode}");

            // Các định dạng ảnh phổ biến
            string[] imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };

            List<string> backgroundFiles = new List<string>();

            // Duyệt qua tất cả các định dạng và thêm file ảnh vào danh sách
            foreach (var extension in imageExtensions)
            {
                backgroundFiles.AddRange(Directory.GetFiles(backgroundsDirectory, extension));
            }

            // Nếu có background nào, đặt ảnh đầu tiên làm background mặc định
            if (backgroundFiles.Count > 0)
            {
                ShowBG.Source = new BitmapImage(new Uri(backgroundFiles[0]));
            }

            foreach (string file in backgroundFiles)
            {
                Image backgroundImage = new Image
                {
                    Source = new BitmapImage(new Uri(file)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(10),
                    Width = 100, // Đặt kích thước cố định
                    Height = 100 // Đặt kích thước cố định
                };
                backgroundImage.MouseLeftButtonDown += Background_MouseLeftButtonDown;
                BackgroundsWrapPanel.Children.Add(backgroundImage);
            }
        }
        private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image clickedBackground = sender as Image;
            string fileName = System.IO.Path.GetFileName(((BitmapImage)clickedBackground.Source).UriSource.LocalPath);
            Bitmap background = new Bitmap(Path.Combine(Directory.GetCurrentDirectory(), "Backgrounds", layout.LayoutCode, fileName));

            Bitmap result = backGroundProcess.OverlayBackground(actualPrint, background);
            ShowBG.Source = clickedBackground.Source;
            ShowPrint.Source = backGroundProcess.ConvertToBitmapImage(result);
        }


        #endregion


        #region StickerMenu

        private void NextButtonSticker_Click(object sender, RoutedEventArgs e)
        {
            StickerMenu();
        }

        private void StickerMenu()
        {
            //PhotoTextBox.Text = "Select a fun sticker to personalize your photo!";


            //BackgoundScrollViewer.Visibility = Visibility.Hidden;
            //BackgroundsWrapPanel.Visibility = Visibility.Hidden;
            //NextButtonSticker.Visibility = Visibility.Hidden;

            //// Hiển thị StickerWrapPanel
            //StickerScrollViewer.Visibility = Visibility.Visible;
            //StickerWrapPanel.Visibility = Visibility.Visible;
            //NextButtonPrinting.Visibility = Visibility.Visible;
            LoadSticker();
        }

        private void LoadSticker()
        {
            StickerWrapPanel.Children.Clear(); // Xóa các phần tử cũ trước khi tải mới
            string stickersDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Stickers");

            // Lấy tất cả các thư mục StickerType
            var stickerTypeDirectories = Directory.GetDirectories(stickersDirectory);

            // Thêm ảnh đại diện và tên của StickerType vào StickerWrapPanel
            foreach (var stickerTypeDir in stickerTypeDirectories)
            {
                string representImagePath = Path.Combine(stickerTypeDir, "RepresentImage.png");

                if (File.Exists(representImagePath))
                {
                    // Tạo StackPanel để chứa cả hình ảnh và tên StickerType
                    StackPanel stackPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(10)
                    };

                    // Tạo Image cho ảnh đại diện
                    Image representImage = new Image
                    {
                        Source = new BitmapImage(new Uri(representImagePath)),
                        Stretch = Stretch.Uniform,
                        Width = 150, // Đặt kích thước cố định
                        Height = 150 // Đặt kích thước cố định
                    };

                    // Lấy tên StickerType từ tên thư mục
                    string stickerTypeName = new DirectoryInfo(stickerTypeDir).Name;

                    // Tạo TextBlock cho tên StickerType
                    TextBlock stickerTypeNameTextBlock = new TextBlock
                    {
                        Text = stickerTypeName,
                        FontSize = 15,
                        Foreground = new SolidColorBrush(Colors.Black),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 0) // Thêm khoảng cách nhỏ phía trên text
                    };

                    // Gắn event handler để khi click vào ảnh sẽ hiển thị các sticker của StickerType này
                    representImage.MouseLeftButtonDown += (s, e) => ShowStickersForType(stickerTypeDir);

                    // Thêm ảnh và tên vào StackPanel
                    stackPanel.Children.Add(representImage);
                    stackPanel.Children.Add(stickerTypeNameTextBlock);

                    // Thêm StackPanel vào StickerWrapPanel
                    StickerWrapPanel.Children.Add(stackPanel);
                }
            }
        }


        // Hàm hiển thị các sticker của StickerType khi người dùng chọn ảnh đại diện
        private void ShowStickersForType(string stickerTypeDir)
        {
            StickerWrapPanel.Children.Clear(); // Xóa các phần tử cũ trước khi tải mới

            // Tạo Grid để chứa nút Back và các sticker
            Grid gridContainer = new Grid
            {
                Margin = new Thickness(10)
            };

            // Define Grid rows: 1 row for Back button, 1 row for stickers
            gridContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Tạo nút Back với hình ảnh và chữ "Back"
            Button backButton = new Button
            {
                Width = 120,
                Height = 50,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Create a StackPanel to hold the image and text
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add the back image
            Image backImage = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/backgrounds/back.png")),
                Width = 20,
                Height = 20,
                Margin = new Thickness(5, 0, 10, 0)
            };
            stackPanel.Children.Add(backImage);

            // Add the text "Back"
            TextBlock backText = new TextBlock
            {
                Text = "Back",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Black
            };
            stackPanel.Children.Add(backText);

            // Add the StackPanel to the Button
            backButton.Content = stackPanel;
            backButton.Click += (s, e) => LoadSticker();

            // Add the Back button to the grid
            Grid.SetRow(backButton, 0);
            gridContainer.Children.Add(backButton);

            // Tạo một WrapPanel để chứa các sticker
            WrapPanel stickersPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10)
            };

            // Các định dạng ảnh phổ biến
            string[] imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };

            List<string> stickerFiles = new List<string>();

            // Duyệt qua tất cả các file ảnh sticker trong thư mục StickerType
            foreach (var extension in imageExtensions)
            {
                stickerFiles.AddRange(Directory.GetFiles(stickerTypeDir, extension));
            }

            // Loại bỏ ảnh đại diện nếu có trong danh sách
            stickerFiles = stickerFiles.Where(file => !file.EndsWith("RepresentImage.png")).ToList();

            // Thêm các sticker vào WrapPanel
            foreach (string file in stickerFiles)
            {
                Image stickerImage = new Image
                {
                    Source = new BitmapImage(new Uri(file)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(5),
                    Width = 100, // Đặt kích thước cố định
                    Height = 100 // Đặt kích thước cố định
                };
                stickerImage.MouseLeftButtonDown += StickerImage_MouseLeftButtonDown; // Thêm event handler cho mỗi sticker
                stickersPanel.Children.Add(stickerImage);
            }

            // Add the stickers panel to the grid
            Grid.SetRow(stickersPanel, 1);
            gridContainer.Children.Add(stickersPanel);

            // Add the grid container to the main wrap panel
            StickerWrapPanel.Children.Add(gridContainer);
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

            StickerProcess sticker = new StickerProcess
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

            CanvasSticker.Children.Add(sticker);

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(sticker);
            if (adornerLayer != null)
            {
                var resizeAdorner = new ResizeAdorner(sticker);
                adornerLayer.Add(resizeAdorner);
            }
        }

        private void Sticker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is StickerProcess clickedSticker)
            {
                clickedSticker.CaptureMouse();
            }
        }

        private void Sticker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is StickerProcess clickedSticker)
            {
                clickedSticker.ReleaseMouseCapture();
            }
        }

        private void Sticker_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StickerProcess clickedSticker = sender as StickerProcess;
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
                VisualBrush stickerBrush = new VisualBrush(CanvasSticker);
                dc.DrawRectangle(stickerBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
            }

            // Render DrawingVisual lên RenderTargetBitmap
            renderTargetBitmap.Render(dv);

            // Cập nhật ShowPrint để hiển thị hình ảnh đã được render kèm sticker
            ShowPrint.Source = renderTargetBitmap;
        }

        #endregion

        #region RetakePhotoMenu

        //private void LoadAndPrint(string printPath)
        //{
        //    var bi = new BitmapImage();
        //    bi.BeginInit();
        //    bi.CacheOption = BitmapCacheOption.OnLoad;
        //    bi.UriSource = new Uri(printPath);
        //    bi.EndInit();

        //    var vis = new DrawingVisual();
        //    var dc = vis.RenderOpen();
        //    dc.DrawImage(bi, new Rect { Width = bi.Width, Height = bi.Height });
        //    dc.Close();


        //    var printerSettings = new PrinterSettings();
        //    var labelPaperSize = new PaperSize
        //    {
        //        RawKind = (int)PaperKind.Custom,
        //        Height = 150,
        //        Width = 100
        //    };
        //    printerSettings.DefaultPageSettings.PaperSize = labelPaperSize;
        //    printerSettings.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        //    //            printerSettings.Copies = actualNumberOfCopies;
        //    pdialog.PrintVisual(vis, "My Image");
        //}


        private void RetakePhotoMenu()
        {

            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            //StopButton.Visibility = Visibility.Hidden;
            LiveViewImage.Visibility = Visibility.Hidden;
            GridCanvasLiveViewImage.Visibility = Visibility.Hidden;

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPictureInlayout.Source = actualPrint;
            GridShowPictureInlayout.Visibility = Visibility.Visible;
            ShowPictureInlayout.Visibility = Visibility.Visible;

            NextButtonBackGround.Visibility = Visibility.Visible;

            SetThumbnailButtonsEnabled(true);
        }


        public void ShowPhotoThumbnail()
        {
            // Xóa các nút hiện tại trong DockPanel
            ThumbnailDockPanel.Children.Clear();
            var getImageThumbnail = new GetImageThumbnail();

            // Tạo các nút hình thu nhỏ dựa trên số lượng ảnh đã chụp
            for (int i = 1; i <= photosInTemplate; i++)
            {
                string thumbnailPath = getImageThumbnail.GetThumbnailPathForIndex(i, BookingID);

                Button thumbnailButton = new Button
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Visibility = Visibility.Visible,
                    Margin = new Thickness(5),
                    Tag = i, // Lưu số thứ tự ảnh trong thuộc tính Tag
                    IsEnabled = thumbnailsClickable
                };

                Image thumbnailImage = new Image
                {
                    Source = new BitmapImage(new Uri(thumbnailPath)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0), // Đảm bảo không có viền trắng
                    SnapsToDevicePixels = true,
                    Width = 200, // Đặt kích thước phù hợp
                    Height = 200
                };

                thumbnailButton.Content = thumbnailImage;

                // Thêm sự kiện click cho các nút hình thu nhỏ
                thumbnailButton.Click += ThumbnailButton_Click;

                ThumbnailDockPanel.Children.Add(thumbnailButton);
            }
        }

        private void ThumbnailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int photoIndex)
            {
                RetakePhoto(sender, e, photoIndex);
            }
        }

        private bool isRetake = false;
        private int retakeIndex;
        private bool thumbnailsClickable = false;

        private void SetThumbnailButtonsEnabled(bool isEnabled)
        {
            foreach (Button btn in ThumbnailDockPanel.Children)
            {
                btn.IsEnabled = isEnabled;
            }
        }

        //màn hình chụp lại
        public void RetakePhoto(object sender, RoutedEventArgs e, int photoNumberToRetake)
        {
            RepeatPhotoDialog repeatPhotoDialog = new RepeatPhotoDialog();
            if (repeatPhotoDialog.ShowDialog() == true)
            {
                SetThumbnailButtonsEnabled(false);
                photosInTemplate--;
                // Lưu lại số thứ tự của ảnh được chọn để chụp lại
                photoNumberInTemplate = photoNumberToRetake - 1;
                ReadyButton.Visibility = Visibility.Visible;

                NextButtonBackGround.Visibility = Visibility.Hidden;
                NextButtonSticker.Visibility = Visibility.Hidden;

                ShowPictureInlayout.Visibility = Visibility.Hidden;
                GridShowPictureInlayout.Visibility = Visibility.Collapsed;
                PrintButton.Visibility = Visibility.Hidden;
                ShowPrint.Visibility = Visibility.Hidden;

                //NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
                //AddOneCopyButton.Visibility = Visibility.Hidden;
                //MinusOneCopyButton.Visibility = Visibility.Hidden;
                //PrintMenuGrid.Visibility = Visibility.Hidden;

                SendEmailButton.Visibility = Visibility.Hidden;
                CreateQRCodeButton.Visibility = Visibility.Hidden;


                isRetake = true;
                retakeIndex = photoNumberToRetake;

                StartButton_Click(sender, e);

            }
        }
        public void UpdatePhotoAfterRetake()
        {
            var getImageThumbnail = new GetImageThumbnail();
            string newThumbnailPath = getImageThumbnail.GetLatestThumbnailPath(BookingID);

            // Cập nhật nút ảnh thu nhỏ tương ứng
            foreach (Button btn in ThumbnailDockPanel.Children)
            {
                if ((int)btn.Tag == retakeIndex)
                {
                    Image thumbnailImage = (Image)btn.Content;
                    thumbnailImage.Source = new BitmapImage(new Uri(newThumbnailPath));
                    break;
                }
            }
            isRetake = false;
            retakeIndex = 0;
        }


        #endregion



        #region PrintMenu
        private async void NextButtonPrinting_Click(object sender, RoutedEventArgs e)
        {
            var endMessageBox = new CustomMessageBox("When you press next, you cannot go back. Do you agree?");
            endMessageBox.ShowDialog();
            if (endMessageBox.DialogResult == true)
            {
                NextButtonSticker.Visibility = Visibility.Collapsed;
                showStickerAndBackGround.Visibility = Visibility.Collapsed;
                //PrintMenu();

                RenderStickersOnImage();
                SaveFinalImage();
                await UploadAndGenerateQRCodeAsync();
                PhotoBookingLibraryMenu();
                HideStickers();
            }
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
                VisualBrush stickerBrush = new VisualBrush(CanvasSticker);
                dc.DrawRectangle(stickerBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
            }

            // Render DrawingVisual lên RenderTargetBitmap
            renderTargetBitmap.Render(dv);

            // Lưu RenderTargetBitmap thành một file PNG
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            // Tạo đối tượng SavePrints để lấy đường dẫn lưu ảnh
            var savePrints = new SavePrints(printNumber, BookingID);
            string filePath = savePrints.PrintDirectory;

            // Tăng số thứ tự của ảnh in để lần lưu tiếp theo không bị trùng tên
            printNumber++;

            using (FileStream file = File.OpenWrite(filePath))
            {
                encoder.Save(file);
            }

            // Cập nhật đường dẫn của ảnh đã in cho PrintMenu
            printPath = filePath;
        }


        private void HideStickers()
        {
            foreach (var child in CanvasSticker.Children)
            {
                if (child is StickerProcess sticker && sticker.Visibility == Visibility.Visible)
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
            foreach (var child in CanvasSticker.Children)
            {
                if (child is StickerProcess sticker)
                {
                    sticker.HideCloseButton();
                }
            }
        }


        //cho nay hien anh de in
        private void PhotoBookingLibraryMenu()
        {
            StickerWrapPanel.Visibility = Visibility.Hidden;
            NextButtonPrinting.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;
            //SendEmailButton.Visibility = Visibility.Visible;
            //PrintMenuGrid.Visibility = Visibility.Visible;

            ShowBookingPhotoThumbnail(BookingID);
        }

        public void ShowBookingPhotoThumbnail(Guid bookingID)
        {
            // Clear existing thumbnails
            BookingPhotoThumbnailWrapPanel.Children.Clear();

            // Determine the path to the booking folder
            string bookingFolderPath = Path.Combine(Environment.CurrentDirectory, Actual.DateNow(), bookingID.ToString());

            // Check if the booking folder exists
            if (!Directory.Exists(bookingFolderPath))
            {
                MessageBox.Show("No photos found for this booking.");
                return;
            }

            // Get all session folders, ordered by creation time
            var sessionFolders = Directory.GetDirectories(bookingFolderPath, "Session_*")
                                          .OrderByDescending(d => Directory.GetCreationTime(d))
                                          .ToArray();

            // Collect the latest print files from each session
            List<string> photoFiles = new List<string>();
            foreach (var sessionFolder in sessionFolders)
            {
                string printsFolderPath = Path.Combine(sessionFolder, "prints");
                if (Directory.Exists(printsFolderPath))
                {
                    var files = Directory.GetFiles(printsFolderPath, "*.jpg");
                    if (files.Length > 0)
                    {
                        var maxFile = files.OrderByDescending(f => int.Parse(Path.GetFileNameWithoutExtension(f).Split('_')[1])).FirstOrDefault();
                        photoFiles.Add(maxFile);
                    }
                }
            }

            // If no photos found, display a message
            if (photoFiles.Count == 0)
            {
                MessageBox.Show("No photos found for this booking.");
                return;
            }

            // Create thumbnail buttons for each photo file
            foreach (var photoFile in photoFiles)
            {
                // Create an Image control for the photo
                Image thumbnailImage = new Image
                {
                    Source = new BitmapImage(new Uri(photoFile)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(5),
                    SnapsToDevicePixels = true,
                    Width = 200,
                    Height = 200
                };

                // Create a Grid for the quantity circle
                Grid quantityGrid = new Grid
                {
                    Width = 30,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Visibility = Visibility.Hidden
                };

                // Create an Ellipse for the quantity circle
                Ellipse quantityEllipse = new Ellipse
                {
                    Fill = System.Windows.Media.Brushes.Red
                };

                // Create a TextBlock for the quantity
                TextBlock quantityTextBlock = new TextBlock
                {
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Text = "1", // Mặc định là 1 cho dịch vụ in ảnh
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                quantityGrid.Children.Add(quantityEllipse);
                quantityGrid.Children.Add(quantityTextBlock);

                // Remove control panel (no increase/decrease buttons needed)
                StackPanel controlPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(5),
                    Visibility = Visibility.Hidden // Không cần hiển thị
                };

                // Create a Grid for the thumbnail and controls
                Grid grid = new Grid();
                grid.Children.Add(thumbnailImage);
                grid.Children.Add(quantityGrid);
                grid.Children.Add(controlPanel);

                // Create a Border for the Grid
                Border border = new Border
                {
                    Child = grid,
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(5),
                    Tag = photoFile // Store the photo file path in the Tag property
                };

                // Add click event for the thumbnail
                border.MouseLeftButtonDown += (s, e) =>
                {
                    ThumbnailBooking_Click(s, e);

                    // Show control panel based on visibility
                    if (currentServiceType == ServiceType.Printing)
                    {
                        controlPanel.Visibility = Visibility.Collapsed; // Không cần hiển thị control panel
                    }
                    else if (currentServiceType == ServiceType.EmailSending)
                    {
                        foreach (var child in controlPanel.Children)
                        {
                            if (child is Button)
                            {
                                (child as Button).Visibility = Visibility.Collapsed;
                            }
                        }
                        controlPanel.Visibility = Visibility.Visible;
                    }
                };

                // Add the Border to the WrapPanel
                BookingPhotoThumbnailWrapPanel.Children.Add(border);
            }

            // Make the thumbnail grid visible
            BookingPhotoThumbnailGrid.Visibility = Visibility.Visible;
        }

        private List<string> selectedPhotoPaths = new List<string>();
        private Dictionary<string, int> selectedPhotoPathsWithQuantities = new Dictionary<string, int>();
        // Cờ kiểm tra ảnh đã được chọn hay chưa

        // Hàm khi người dùng chọn ảnh
        private void ThumbnailBooking_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedBorder = sender as Border;
            if (clickedBorder != null)
            {
                var parentGrid = clickedBorder.Child as Grid;
                var quantityGrid = parentGrid?.Children.OfType<Grid>().FirstOrDefault(grid => grid.Children.OfType<Ellipse>().Any());
                var controlPanel = parentGrid?.Children.OfType<StackPanel>().FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal);

                if (quantityGrid != null && controlPanel != null)
                {
                    HandlePhotoSelection(clickedBorder, quantityGrid, controlPanel);
                }
            }
        }



        // Hàm khi người dùng chọn dịch vụ
        private async void BookedService_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPhotoPaths.Count == 0)
            {
                MessageBox.Show("Please Select at least 1 photo");
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                var border = VisualTreeHelper.GetParent(button) as Border;
                var serviceData = button.DataContext as dynamic;

                if (serviceData != null)
                {
                    var selectedService = serviceData.Service;
                    currentServiceType = selectedService.ServiceType;
                    isServiceSelected = true;

                    // Cập nhật giao diện, v.v.
                    UpdateServiceButtons();

                    if (border != null)
                    {
                        border.Background = new SolidColorBrush(Colors.Orange);
                        button.Foreground = new SolidColorBrush(Colors.White);
                    }

                    if (currentServiceType == ServiceType.EmailSending)
                    {
                        SendEmailButton.Visibility = Visibility.Collapsed;
                        // Gọi hàm gửi email ngay lập tức nếu cần
                        SendEmailButtonClick(sender, e);
                    }
                    else if (currentServiceType == ServiceType.Printing)
                    {
                        PrintButton.Visibility = Visibility.Collapsed;
                        // Gọi hành động in ấn ngay lập tức (nếu có)
                    }
                    else if (currentServiceType == ServiceType.CreatingQR)
                    {
                        CreateQRCodeButton.Visibility = Visibility.Collapsed;
                        // Gọi trực tiếp hành động tạo QR Code ngay
                        CreateQRCodeButton_Click(sender, e);
                    }
                    else
                    {
                        SendEmailButton.Visibility = Visibility.Collapsed;
                        PrintButton.Visibility = Visibility.Collapsed;
                        CreateQRCodeButton.Visibility = Visibility.Collapsed;
                    }

                    BookingPhotoThumbnailGrid.Visibility = Visibility.Visible;
                }
            }
        }



        private void HandlePhotoSelection(Border clickedBorder, Grid quantityGrid, StackPanel controlPanel)
        {
            if (quantityGrid.Visibility == Visibility.Visible)
            {
                // Nếu ảnh đã được chọn, bỏ chọn ảnh này
                selectedPhotoPaths.Remove(clickedBorder.Tag as string);
                selectedPhotoPathsWithQuantities.Remove(clickedBorder.Tag as string);

                quantityGrid.Visibility = Visibility.Collapsed;
                clickedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }
            else
            {
                // Thêm ảnh vào danh sách ảnh đã chọn mà không ràng buộc số lượng
                selectedPhotoPaths.Add(clickedBorder.Tag as string);
                selectedPhotoPathsWithQuantities[clickedBorder.Tag as string] = 1;

                quantityGrid.Visibility = Visibility.Visible;
                clickedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }
        }

        // Helper method to find a visual parent of a specific type
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        private void HideBookingPhotoThumbnailGrid()
        {
            BookingPhotoThumbnailGrid.Visibility = Visibility.Hidden;
            BookingPhotoThumbnailWrapPanel.Children.Clear();
        }

        private void PrintMenu()
        {
            StickerWrapPanel.Visibility = Visibility.Hidden;
            NextButtonPrinting.Visibility = Visibility.Hidden;

            //PhotoTextBox.Text = "Press button to continue";
            //NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();

            actualPrint = new BitmapImage();
            actualPrint.BeginInit();
            actualPrint.UriSource = new Uri(printPath);
            actualPrint.EndInit();

            ShowPrint.Source = actualPrint;

            //PrintMenuGrid.Visibility = Visibility.Visible;
            //PrintMenuStackPanel.Visibility = Visibility.Visible;
            //PrintButton.Visibility = Visibility.Visible;

            //NumberOfCopiesTextBox.Visibility = Visibility.Visible;
            //AddOneCopyButton.Visibility = Visibility.Visible;
            //MinusOneCopyButton.Visibility = Visibility.Visible;

            //PrintMenuGrid.Visibility = Visibility.Visible;
            //SendEmailButton.Visibility = Visibility.Visible;


            ShowPrint.Visibility = Visibility.Visible;
            //StopButton.Visibility = Visibility.Visible;
            //        CreateDynamicBorder(ShowPrint.ActualWidth, ShowPrint.ActualHeight);
        }


        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPhotoPathsWithQuantities.Count == 0)
            {
                MessageBox.Show("Please select at least one photo to print.");
                return;
            }

            int totalPrintQuantity = selectedPhotoPathsWithQuantities.Values.Sum();
            int remainingPrintCount = printingUsageCount + totalPrintQuantity;

            // Giả sử bạn có một biến để lưu trữ tổng số lượng dịch vụ Printing đã book
            if (remainingPrintCount > totalPrintServiceCount)
            {
                MessageBox.Show($"You can only print a total of {totalPrintServiceCount} photos.");
                return;
            }

            foreach (var photoPathWithQuantity in selectedPhotoPathsWithQuantities)
            {
                string photoPath = photoPathWithQuantity.Key;
                int quantity = photoPathWithQuantity.Value;

                PrintingServices.Print(photoPath, printerName, (short)quantity);
            }

            // Cập nhật số lần sử dụng dịch vụ Printing
            printingUsageCount += totalPrintQuantity;

            if (printingUsageCount >= totalPrintServiceCount)
            {
                PrintButton.IsEnabled = false;
                PrintButton.Visibility = Visibility.Collapsed;
            }
        }





        //private void MinusOneCopyButtonClick(object sender, RoutedEventArgs e)
        //{
        //    if (actualNumberOfCopies > 1)
        //    {
        //        --actualNumberOfCopies;
        //        NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();
        //        TriggerTextBoxAnimation();
        //    }

        //}

        //private void AddOneCopyButtonClick(object sender, RoutedEventArgs e)
        //{
        //    if (actualNumberOfCopies < maxCopies)
        //    {
        //        ++actualNumberOfCopies;
        //        NumberOfCopiesTextBox.Text = actualNumberOfCopies.ToString();
        //        TriggerTextBoxAnimation();
        //    }
        //}
        //private void TriggerTextBoxAnimation()
        //{
        //    Storyboard storyboard = (Storyboard)this.Resources["TextChangeAnimation"];
        //    Storyboard.SetTarget(storyboard, NumberOfCopiesTextBox);
        //    storyboard.Begin();
        //}

        #endregion

        #region MenuSetting


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
                //layout.LayoutCode = actualSettings.Root.Element("actualTemplate").Value;
                //if (actualSettings.Root.Element("actualTemplate").Value == "All")
                //{
                //    turnOnTemplateMenu = true;
                //}
                firstprinter = actualSettings.Root.Element("actualPrinter").Value;
                secondprinter = actualSettings.Root.Element("secondPrinter").Value;
                timeLeft = System.Convert.ToInt32(actualSettings.Root.Element("timeBetweenPhotos").Value);
                printerName = FBoothApp.PrintingServices.ActualPrinter(layout.LayoutCode, firstprinter, secondprinter);
                timeLeftCopy = timeLeft;

                SmtpServerName = actualSettings.Root.Element("SmtpServerName").Value;
                SmtpPortNumber = actualSettings.Root.Element("SmtpPortNumber").Value;
                EmailHostAddress = actualSettings.Root.Element("EmailHostAddress").Value;
                EmailHostPassword = actualSettings.Root.Element("EmailHostPassword").Value;

                BoothID = Guid.Parse(actualSettings.Root.Element("BoothID").Value);
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
                //f12 để mở setting
                Menu menu1 = new Menu();
                this.Content = menu1;
            }
            if (e.Key == Key.Escape)
            {
                //esc để thoát chương trình
                Application.Current.Shutdown();
            }
        }
        #endregion

        private async Task UploadAndGenerateQRCodeAsync()
        {
            
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                
                // Giả sử ShowPrint là ảnh đã chỉnh sửa xong
                var bitmap = new RenderTargetBitmap((int)ShowPrint.ActualWidth, (int)ShowPrint.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(ShowPrint);

                // Lưu ảnh từ RenderTargetBitmap vào một tệp tạm để tải lên Firebase
                string tempPhotoPath = Path.Combine(Path.GetTempPath(), "temp_photo.png");
                using (var fileStream = new FileStream(tempPhotoPath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }

                // Tải ảnh lên Firebase và lấy URL
                string photoUrl = await UploadImageToFirebase(tempPhotoPath);

                // Xóa tệp tạm sau khi tải lên
                File.Delete(tempPhotoPath);

                // Tạo và hiển thị mã QR từ URL
                GenerateAndShowQRCode(photoUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        

        // Hàm tải ảnh lên Firebase
        private async Task<string> UploadImageToFirebase(string photoPath)
        {
            FirebaseHelper firebaseHelper = new FirebaseHelper(
                apiKey: "AIzaSyCHo3ofJXAIXBwlpu9L19NQyeXZjrkGGJA",
                bucket: "face-image-cb4c4.appspot.com",
                authEmail: "cuongtpse171590@fpt.edu.vn",
                authPassword: "cuongtpse171590-222"
            );

            string photoUrl = await firebaseHelper.UploadImageToFirebaseAsync(photoPath, "guest_images");

            if (string.IsNullOrEmpty(photoUrl))
            {
                throw new Exception("Failed to upload photo. Please try again.");
            }

            return photoUrl;
        }

        // Hàm tạo và hiển thị mã QR từ URL
        private void GenerateAndShowQRCode(string qrContent)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    Bitmap qrCodeImage = qrCode.GetGraphic(20);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        qrCodeImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Position = 0;

                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        var qrWindow = new Window
                        {
                            Title = "QR Code",
                            Width = 300,
                            Height = 300,
                            Content = new Image { Source = bitmapImage },
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };

                        qrWindow.ShowDialog();
                    }
                }
            }
        }

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
            PhotoTextBox.Text = "Welcome! Get ready to have some fun!";

            sliderTimer.Start();
            SliderBorder.Visibility = Visibility.Visible;
            Slider.Visibility = Visibility.Visible;
            //StartButton.Visibility = Visibility.Visible;

            ReadyButton.Visibility = Visibility.Hidden;
            PrintButton.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;

            //NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
            //AddOneCopyButton.Visibility = Visibility.Hidden;
            //MinusOneCopyButton.Visibility = Visibility.Hidden;
            //PrintMenuGrid.Visibility = Visibility.Hidden;
            SendEmailButton.Visibility = Visibility.Hidden;


            ThumbnailDockPanel.Visibility = Visibility.Hidden;
            ThumbnailDockPanelGrid.Visibility = Visibility.Hidden;
        }
        #endregion

        private void SendEmailButtonClick(object sender, RoutedEventArgs e)
        {
            if (selectedPhotoPaths.Count == 0)
            {
                MessageBox.Show("Please select at least one photo to send.");
                return;
            }

            // Hiển thị hộp thoại nhập email
            EmailSendDialog inputEmailSendDialog = new EmailSendDialog("Please enter your email address:", "name@gmail.com");
            if (inputEmailSendDialog.ShowDialog() == true)
            {

                List<string> photoPathsToSend = new List<string>();

                foreach (var photoPath in selectedPhotoPaths)
                {
                    photoPathsToSend.Add(photoPath);
                }

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmail(inputEmailSendDialog.Answer, photoPathsToSend);

                emailUsageCount += selectedPhotoPaths.Count;

                if (emailUsageCount >= totalEmailServiceCount)
                {
                    SendEmailButton.IsEnabled = false;
                    SendEmailButton.Visibility = Visibility.Collapsed;
                }
            }
        }



        /*private void RoundedTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (RoundedTextBox.Text == "Enter the code from your email here!")
            {
                RoundedTextBox.Text = string.Empty;
                RoundedTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void RoundedTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoundedTextBox.Text))
            {
                RoundedTextBox.Text = "Enter the code from your email here!";
                RoundedTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void RoundedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SendButton == null) return; // Ensure SendButton is not null

            if (string.IsNullOrWhiteSpace(RoundedTextBox.Text) || RoundedTextBox.Text == "Enter the code from your email here!")
            {
                SendButton.IsEnabled = false;
                SendButton.Opacity = 0.5;
            }
            else
            {
                SendButton.IsEnabled = true;
                SendButton.Opacity = 1.0;
            }
        }*/


        public void LoadBookedServices(List<BookingServiceResponse> bookingServices)
        {
            // Xóa các dịch vụ cũ trong danh sách
            BookedServicesList.ItemsSource = null;

            if (bookingServices != null && bookingServices.Count > 0)
            {
                // Gán danh sách dịch vụ đã book vào ItemsSource của ItemsControl
                BookedServicesList.ItemsSource = bookingServices.Select(s => new
                {
                    ServiceName = $"{s.Service.ServiceName} (x{s.Quantity})",
                    Price = s.SubTotal,
                    Service = s.Service // Lưu toàn bộ đối tượng Service để sử dụng sau này
                }).ToList();

                foreach (var bookedService in bookingServices)
                {
                    InitializeRemainingQuantities(bookedService); // Cập nhật dựa trên BookingServiceResponse
                }

                UpdateServiceButtons(); // Kiểm tra và cập nhật trạng thái các nút sau khi tải lại dịch vụ
            }
            else
            {
                MessageBox.Show("No services booked for this session.");
            }
        }

        private void UpdateServiceButtons()
        {
            foreach (var item in BookedServicesList.Items)
            {
                var container = BookedServicesList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                var button = container?.FindName("ServiceButton") as Button;
                var border = container?.FindName("ServiceButtonBorder") as Border;

                if (button != null && border != null)
                {
                    // Set the default border style
                    border.CornerRadius = new CornerRadius(10);
                    border.BorderThickness = new Thickness(2);
                    border.Padding = new Thickness(5);
                    border.Margin = new Thickness(5);
                    border.Background = new SolidColorBrush(Colors.Transparent);
                    border.BorderBrush = new SolidColorBrush(Colors.Gray);

                    // Disable all buttons by default
                    button.IsEnabled = false;
                    button.Foreground = new SolidColorBrush(Colors.Gray);

                    var serviceData = item as dynamic; // Using dynamic for anonymous type
                    if (serviceData != null)
                    {
                        var serviceType = serviceData.Service.ServiceType;

                        // Enable and highlight only Print and Email buttons
                        if (serviceType == ServiceType.Printing)
                        {
                            button.IsEnabled = printingUsageCount < totalPrintServiceCount;
                            border.Background = new SolidColorBrush(Colors.Green);
                            button.Foreground = new SolidColorBrush(Colors.White);
                        }
                        else if (serviceType == ServiceType.EmailSending)
                        {
                            button.IsEnabled = emailUsageCount < totalEmailServiceCount;
                            border.Background = new SolidColorBrush(Colors.Blue);
                            button.Foreground = new SolidColorBrush(Colors.White);
                        }
                    }
                }
            }
        }

        private void LoadAvailableServices(List<ServiceResponse> availableServices)
        {
            // Xóa các dịch vụ cũ trong danh sách
            AvailableServicesList.ItemsSource = null;

            if (availableServices != null && availableServices.Count > 0)
            {
                // Gán danh sách dịch vụ có sẵn vào ItemsSource của ItemsControl
                var serviceItems = availableServices.Select(s => new AvailableServiceItem
                {
                    ServiceName = s.ServiceName,
                    ServicePrice = s.ServicePrice,
                    ServiceID = s.ServiceID,
                    Quantity = 1 // Đặt số lượng mặc định là 1
                }).ToList();

                AvailableServicesList.ItemsSource = serviceItems;
            }
            else
            {
                MessageBox.Show("No available services at the moment.");
            }
        }





        // Hàm xử lý khi người dùng click vào dịch vụ đã đặt trong tab "Booked Services" // Flag to check if a service is selected
        private ServiceType currentServiceType; 
        private bool isServiceSelected = false;

        
        private void UnselectAllPhotos()
        {
            // Loop through all selected photos and unselect them
            foreach (var photoPath in selectedPhotoPaths.ToList())
            {
                if (selectedPhotoPathsWithQuantities.ContainsKey(photoPath))
                {
                    selectedPhotoPathsWithQuantities.Remove(photoPath);
                }

                // Find the corresponding UI element and reset its visual state
                foreach (var child in BookingPhotoThumbnailWrapPanel.Children)
                {
                    if (child is Border border && border.Tag as string == photoPath)
                    {
                        var parentGrid = border.Child as Grid;
                        var quantityGrid = parentGrid?.Children.OfType<Grid>().FirstOrDefault(grid => grid.Children.OfType<Ellipse>().Any());

                        if (quantityGrid != null)
                        {
                            quantityGrid.Visibility = Visibility.Collapsed;
                        }

                        border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    }
                }
            }

            // Clear the selected photos list
            selectedPhotoPaths.Clear();
        }

        // Hàm xử lý khi người dùng click vào dịch vụ có sẵn trong tab "Available Services"
        private async void AvailableService_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var serviceData = button.Tag as AvailableServiceItem;
                if (serviceData != null)
                {
                    try
                    {
                        var addServiceRequest = new AddExtraServiceRequest
                        {
                            BoothID = BoothID,
                            BookingID = BookingID,
                            ServiceList = new Dictionary<Guid, short>
                            {
                                { serviceData.ServiceID, (short)serviceData.Quantity }
                            }
                        };

                        var bookingResponse = await _apiServices.AddExtraServiceAsync(addServiceRequest);
                        if (bookingResponse != null)
                        {
                            MessageBox.Show($"Service '{serviceData.ServiceName}' with quantity {serviceData.Quantity} has been added to your booking.");

                            // Cập nhật số lượng dịch vụ đã book
                            foreach (var service in bookingResponse.BookingServices)
                            {
                                if (service.Service.ServiceType == ServiceType.EmailSending)
                                {
                                    totalEmailServiceCount += service.Quantity;
                                }
                                else if (service.Service.ServiceType == ServiceType.Printing)
                                {
                                    totalPrintServiceCount += service.Quantity;
                                }
                            }

                            UpdateServiceButtons(); // Refresh the buttons after adding the service
                        }
                        else
                        {
                            MessageBox.Show("Failed to add service to the booking.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}");
                    }
                }
            }
        }

        private async void ResetBookingState()
        {
            var savedata = new SavePhoto(photoNumber, BookingID);
            var updateRequest = new UpdatePhotoSessionRequest
            {
                TotalPhotoTaken = savedata.CountPhotosInSession(),
                Status = Entity.Enum.PhotoSessionStatus.Ended
            };

            await _apiServices.UpdatePhotoSessionAsync(_currentSessionId, updateRequest);

            // Reset Booking related values
            BookingID = Guid.Empty;
            photoNumber = 0;
            photosInTemplate = 0;
            photoNumberInTemplate = 0;
            printNumber = 0;
            SavePhoto.CurrentSessionPath = null;

            // Reset Service usage counts
            emailUsageCount = 0;
            printingUsageCount = 0;
            totalEmailServiceCount = 0;
            totalPrintServiceCount = 0;

            HomeText.Visibility = Visibility.Visible;
            SliderBorder.Visibility = Visibility.Visible;
            Slider.Visibility = Visibility.Visible;
            ImageBorder.Visibility = Visibility.Visible;

            sliderTimer.Start();
            slider(null, null);

            /*RoundedTextBox.Clear();
            RoundedTextBox_LostFocus(null, null);*/
            BorderPanel.Visibility = Visibility.Visible;

            checkEndTimeTimer?.Stop();
            timeRemaining = TimeSpan.Zero;
            CountdownTextBlock.Visibility = Visibility.Collapsed;

            BookingPhotoThumbnailGrid.Visibility = Visibility.Collapsed;
        }


        private async Task CloseBookingAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hiển thị thông báo trong khi xử lý đóng booking
                MessageBox.Show("Closing booking...");

                // Gọi phương thức CloseBookingAsync để gửi yêu cầu đóng booking tới API
                bool isClosed = await _apiServices.CloseBookingAsync(Guid.Parse("28110B4A-BF04-4C04-A19B-1B91D976EE7C"), BookingID);

                if (isClosed)
                {
                    //var savedata = new SavePhoto(photoNumber, BookingID);
                    //var updateRequest = new UpdatePhotoSessionRequest
                    //{
                    //    TotalPhotoTaken = savedata.CountPhotosInSession(),
                    //    Status = Entity.Enum.PhotoSessionStatus.Ended
                    //};

                    //await _apiServices.UpdatePhotoSessionAsync(_currentSessionId, updateRequest);

                    MessageBox.Show("Booking closed successfully.");
                    // Thực hiện các xử lý khác khi đóng booking thành công, ví dụ như làm mới UI hoặc điều hướng
                    //BookingID = Guid.Empty;
                    //photoNumber = 0;
                    //photosInTemplate = 0;
                    //photoNumberInTemplate = 0;
                    //printNumber = 0;
                    //SavePhoto.CurrentSessionPath = null;

                    //HomeText.Visibility = Visibility.Visible;
                    //SliderBorder.Visibility = Visibility.Visible;
                    //Slider.Visibility = Visibility.Visible;
                    //ImageBorder.Visibility = Visibility.Visible;

                    //sliderTimer.Start();
                    //slider(null, null);

                    //RoundedTextBox.Clear();
                    //RoundedTextBox_LostFocus(sender, e);
                    //BorderPanel.Visibility = Visibility.Visible;

                    //checkEndTimeTimer.Stop();
                    //timeRemaining = TimeSpan.Zero;
                    //CountdownTextBlock.Visibility = Visibility.Collapsed;

                    //BookingPhotoThumbnailGrid.Visibility = Visibility.Collapsed;
                    ResetBookingState();
                }
                else
                {
                    MessageBox.Show("Failed to close booking. Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                // Xử lý ngoại lệ nếu có
            }
        }

        // Hàm xử lý khi người dùng click vào nút đóng booking
        private async void CloseBooking_Click(object sender, RoutedEventArgs e)
        {
            await CloseBookingAsync(sender, e);
        }



        //Hàm xử lý khi người dùng click nút create QR code
        private async void CreateQRCodeButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra nếu không có ảnh nào được chọn
            if (selectedPhotoPaths.Count == 0)
            {
                MessageBox.Show("Please select one photo to create a QR code.");
                return;
            } else if (selectedPhotoPaths.Count > 1)
            {
                MessageBox.Show("Please select only one photo to create a QR code.");
                return;
            }

            // Giả sử bạn muốn lấy đường dẫn của ảnh đầu tiên trong danh sách
            string photoPath = selectedPhotoPaths[0];

            // Khởi tạo FirebaseHelper với thông tin Firebase của bạn
            FirebaseHelper firebaseHelper = new FirebaseHelper(
                apiKey: "AIzaSyCHo3ofJXAIXBwlpu9L19NQyeXZjrkGGJA",
                bucket: "face-image-cb4c4.appspot.com",
                authEmail: "cuongtpse171590@fpt.edu.vn",
                authPassword: "cuongtpse171590-222"
            );

            try
            {
                // Gọi phương thức để tải ảnh lên Firebase và nhận URL ảnh
                string photoUrl = await firebaseHelper.UploadImageToFirebaseAsync(photoPath, "guest_images");

                // Kiểm tra xem ảnh đã được tải lên thành công
                if (string.IsNullOrEmpty(photoUrl))
                {
                    MessageBox.Show("Failed to upload photo. Please try again.");
                    return;
                }

                // Tạo mã QR từ URL của ảnh đã tạo
                string qrContent = photoUrl;

                // Sử dụng thư viện QRCoder để tạo QR code
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        Bitmap qrCodeImage = qrCode.GetGraphic(20);

                        // Lưu QR code vào MemoryStream
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            qrCodeImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            memoryStream.Position = 0; // Reset vị trí stream về đầu

                            // Hiển thị hình ảnh QR trong một MessageBox
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = memoryStream; // Thiết lập nguồn cho hình ảnh
                            bitmapImage.EndInit();
                            bitmapImage.Freeze(); // Để tránh lỗi khi sử dụng từ thread khác

                            // Tạo một cửa sổ để hiển thị hình ảnh QR
                            var qrWindow = new Window
                            {
                                Title = "QR Code",
                                Width = 300,
                                Height = 300,
                                Content = new Image { Source = bitmapImage },
                                WindowStartupLocation = WindowStartupLocation.CenterScreen
                            };

                            qrWindow.ShowDialog(); // Hiển thị cửa sổ chứa hình ảnh QR
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }




        private async void ServiceTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl == ServiceTabControl) // Kiểm tra đúng tabControl
            {
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    if (selectedTab.Header.ToString() == "Booked Services")
                    {
                        try
                        {
                            // Gọi hàm GetBookingByIdAsync để lấy thông tin booking
                            var bookingResponse = await _apiServices.GetBookingByIdAsync(BookingID);
                            if (bookingResponse != null)
                            {
                                // Gọi hàm LoadBookedServices để hiển thị các dịch vụ đã đặt
                                LoadBookedServices(bookingResponse.BookingServices);
                            }
                            else
                            {
                                MessageBox.Show("No booking information found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to load booked services: {ex.Message}");
                        }
                    }
                    else if (selectedTab.Header.ToString() == "Available Services")
                    {
                        try
                        {
                            // Gọi hàm GetAvailableServicesAsync để lấy danh sách dịch vụ có sẵn
                            var availableServices = await _apiServices.GetAvailableServicesAsync();
                            if (availableServices != null && availableServices.Count > 0)
                            {
                                // Gọi hàm LoadAvailableServices để hiển thị các dịch vụ có sẵn
                                LoadAvailableServices(availableServices);
                            }
                            else
                            {
                                MessageBox.Show("No available services found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to load available services: {ex.Message}");
                        }
                    }
                }
            }
        }


        private void IncreaseServiceQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var serviceID = (Guid)button.Tag;
                var item = AvailableServicesList.Items.Cast<AvailableServiceItem>().FirstOrDefault(s => s.ServiceID == serviceID);
                if (item != null)
                {
                    item.Quantity++;
                    RefreshAvailableServicesList();
                }
            }
        }

        private void DecreaseServiceQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var serviceID = (Guid)button.Tag;
                var item = AvailableServicesList.Items.Cast<AvailableServiceItem>().FirstOrDefault(s => s.ServiceID == serviceID);
                if (item != null && item.Quantity > 1) // Đảm bảo số lượng không giảm dưới 1
                {
                    item.Quantity--;
                    RefreshAvailableServicesList();
                }
            }
        }

        private void RefreshAvailableServicesList()
        {
            // Refresh the ItemsSource to reflect the changes in quantity
            AvailableServicesList.ItemsSource = AvailableServicesList.Items.Cast<dynamic>().ToList();
        }

        private int emailUsageCount = 0; // Số lần đã sử dụng dịch vụ EmailSending
        private int printingUsageCount = 0; // Số lần đã sử dụng dịch vụ Printing
        private int creatingQRUsageCount = 0; //Số lần đã sử dụng dịch vụ CreatingQR

        private int totalEmailServiceCount = 0;
        private int totalPrintServiceCount = 0;
        private int totalQRServiceCount = 0;

        private void InitializeRemainingQuantities(BookingServiceResponse bookedService)
        {
            var serviceType = bookedService.Service.ServiceType;

            if (serviceType == ServiceType.EmailSending)
            {
                totalEmailServiceCount = bookedService.Quantity;
            }
            else if (serviceType == ServiceType.Printing)
            {
                totalPrintServiceCount = bookedService.Quantity;
            }
            else if (serviceType == ServiceType.CreatingQR)
            {
                totalQRServiceCount = bookedService.Quantity;
            }
        }

    }
}


