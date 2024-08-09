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
        public bool turnOnTemplateMenu = false;
        public bool PhotoTaken = false;


        public MainWindow()
        {
            layout = new Layout();
            _apiServices = new FetchApiServices();
            backGroundProcess = new BackGroundProcess();
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
            var layouts = await _apiServices.GetLayoutsAsync();
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
            // Add your logic here for what happens when the next button is clicked
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

        private void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {

            try
            {
                photoNumber++;
                var savedata = new SavePhoto(photoNumber, BookingID);
                string dir = savedata.FolderDirectory;

                Info.FileName = savedata.PhotoName;
                sender.DownloadFile(Info, dir);


                ReSize.ImageAndSave(savedata.PhotoDirectory, photoNumberInTemplate, layout);

            }
            catch (Exception ex) { Report.Error(ex.Message + "loi resize", false); }

            PhotoTaken = true;

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
            StartButton.Visibility = Visibility.Hidden;

            //StartButtonMenu.Visibility = Visibility.Hidden;
            //InputGrid.Visibility = Visibility.Hidden;
            HomeText.Visibility = Visibility.Hidden;
            BorderPanel.Visibility = Visibility.Hidden;

            ReadyButton.Visibility = Visibility.Visible;
            //StopButton.Visibility = Visibility.Visible;
            PhotoTextBox.Visibility = Visibility.Visible;


            PhotoTextBox.Text = "Click the lens to start capturing photos!";

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
                DrawGridLines();
                MainCamera.StartLiveView();
                LiveViewImage.Visibility = Visibility.Visible;
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
            LoadingProgressBarContainer.Visibility = Visibility.Visible;
            StartLoadingAnimation();

            try
            {
                var checkinCode = long.Parse(RoundedTextBox.Text);

                var request = new CheckinRequest
                {
                    BoothID = BoothID,
                    Code = checkinCode
                };

                var response = await _apiServices.CheckinAsync(request);

                var bookingIDresponse = response.BookingID;
                BookingID = bookingIDresponse;

                // Handle the response (e.g., show a message, update the UI)
                // Tính toán tổng thời gian thuê và thời gian kết thúc
                var rentalDuration = response.EndTime - response.StartTime;
                var checkinTime = DateTime.Now;
                var formattedDuration = $"{rentalDuration.Hours} hours and {rentalDuration.Minutes} minutes";
                var endTimeFormatted = response.EndTime.ToString("HH:mm");

                // Hiển thị thông báo thành công
                var successMessageBox = new CustomMessageBox($"Check-in successful at: {checkinTime}!\nTotal rental time: {formattedDuration}\nRental time ends at: {endTimeFormatted}");
                successMessageBox.ShowDialog();
                if (successMessageBox.DialogResult == true)
                {
                    // Process the response (e.g., show booking details)
                    sessionEndTime = response.EndTime;
                    StartEndTimeCheck();

                    // Turn on layout menu after successful check-in
                    TurnOnLayoutMenu();
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
                LoadingProgressBarContainer.Visibility = Visibility.Collapsed;
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

            LoadingRotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void StopLoadingAnimation()
        {
            LoadingRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
        }

        private DispatcherTimer checkEndTimeTimer;
        private DateTime sessionEndTime;

        private void StartEndTimeCheck()
        {
            if (checkEndTimeTimer == null)
            {
                checkEndTimeTimer = new DispatcherTimer();
                checkEndTimeTimer.Interval = TimeSpan.FromSeconds(10); // Check every 30 seconds
                checkEndTimeTimer.Tick += CheckEndTimeTimer_Tick;
            }
            checkEndTimeTimer.Start();
        }

        private void CheckEndTimeTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now >= sessionEndTime)
            {
                checkEndTimeTimer.Stop();
                var endMessageBox = new CustomMessageBox("Your session has ended. The application will now close.");
                endMessageBox.ShowDialog();
                Application.Current.Shutdown();
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
            PhotoTextBox.Text = "Choose a Layout to Begin Your Experience!";
            sliderTimer.Stop();
            Slider.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            //InputGrid.Visibility = Visibility.Hidden;
            HomeText.Visibility = Visibility.Hidden;
            BorderPanel.Visibility = Visibility.Hidden;

            //LayoutsWrapPanel.Visibility = Visibility.Visible;
            //LayoutScrollViewer.Visibility = Visibility.Visible;
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
        public void StartLayoutsWelcomeMenu()
        {
            PhotoTextBox.Visibility = Visibility.Visible;
            PhotoTextBox.Text = "Hello";
            SliderBorder.Visibility = Visibility.Visible;

            //StartButtonMenu.Visibility = Visibility.Visible;

            Slider.Visibility = Visibility.Visible;
            sliderTimer.Start();

            //StopButton.Visibility = Visibility.Hidden;
            ReadyButton.Visibility = Visibility.Hidden;
            PrintButton.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;

            //NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
            //AddOneCopyButton.Visibility = Visibility.Hidden;
            //MinusOneCopyButton.Visibility = Visibility.Hidden;
            PrintMenuGrid.Visibility = Visibility.Collapsed;
            SendEmailButton.Visibility = Visibility.Hidden;

            ThumbnailDockPanel.Visibility = Visibility.Hidden;
            ThumbnailDockPanelGrid.Visibility = Visibility.Hidden;
            SliderBorder.Visibility = Visibility.Hidden;
            Slider.Visibility = Visibility.Hidden;
        }
        public void CheckTemplate()
        {

            if (turnOnTemplateMenu)
            {
                //StartButtonMenu.Visibility = Visibility.Visible;
                SendButton.Visibility = Visibility.Visible;
            }
            else
            {
                StartButton.Visibility = Visibility.Visible;
            }
        }


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
            NextButtonSticker.Visibility = Visibility.Visible;
            GirdShowPrintPicture.Visibility= Visibility.Visible;

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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
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
            string[] backgroundFiles = Directory.GetFiles(backgroundsDirectory, "*.png");

            foreach (string file in backgroundFiles)
            {
                Image backgroundImage = new Image
                {
                    Source = new BitmapImage(new Uri(file)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(10),
                    Width = 200, // Đặt kích thước cố định
                    Height = 200 // Đặt kích thước cố định
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

        private async void LoadSticker()
        {
            StickerWrapPanel.Children.Clear();
            var stickerFiles = await _apiServices.GetStickersAsync();

            foreach (var stickerFile in stickerFiles)
            {
                Image stickerImage = new Image
                {
                    Source = new BitmapImage(new Uri(stickerFile.StickerURL)),
                    Width = 100,
                    Height = 100,
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
                PrintMenuGrid.Visibility = Visibility.Hidden;

                SendEmailButton.Visibility = Visibility.Hidden;


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
        private void NextButtonPrinting_Click(object sender, RoutedEventArgs e)
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
            SendEmailButton.Visibility = Visibility.Visible;
            PrintMenuGrid.Visibility = Visibility.Visible;

            ShowBookingPhotoThumbnail(BookingID);
        }

        public void ShowBookingPhotoThumbnail(Guid bookingID)
        {
            // Xóa các nút hiện tại trong WrapPanel
            BookingPhotoThumbnailWrapPanel.Children.Clear();

            // Đường dẫn tới thư mục gốc của booking cụ thể
            string bookingFolderPath = Path.Combine(Environment.CurrentDirectory, Actual.DateNow(), bookingID.ToString());

            // Kiểm tra xem thư mục có tồn tại không
            if (!Directory.Exists(bookingFolderPath))
            {
                MessageBox.Show("No photos found for this booking.");
                return;
            }

            // Lấy tất cả các thư mục session
            string[] sessionFolders = Directory.GetDirectories(bookingFolderPath, "Session_*");

            // Lấy ảnh có số thứ tự lớn nhất từ thư mục "prints" của các session
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

            if (photoFiles.Count == 0)
            {
                MessageBox.Show("No photos found for this booking.");
                return;
            }

            // Duyệt qua từng file ảnh và tạo nút hình thu nhỏ
            foreach (var photoFile in photoFiles)
            {
                // Tạo Image cho ảnh
                Image thumbnailImage = new Image
                {
                    Source = new BitmapImage(new Uri(photoFile)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(5), // Đảm bảo không có viền trắng
                    SnapsToDevicePixels = true,
                    Width = 200, // Đặt kích thước phù hợp
                    Height = 200
                };

                // Tạo Grid để chứa vòng tròn và số
                Grid quantityGrid = new Grid
                {
                    Width = 30,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 5, 0),
                    Visibility = Visibility.Hidden
                };

                // Tạo Ellipse để tạo vòng tròn đỏ
                Ellipse quantityEllipse = new Ellipse
                {
                    Fill = System.Windows.Media.Brushes.Red
                };

                // Tạo TextBlock để hiển thị số lượng
                TextBlock quantityTextBlock = new TextBlock
                {
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Text = "0",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                quantityGrid.Children.Add(quantityEllipse);
                quantityGrid.Children.Add(quantityTextBlock);

                // Tạo các nút cộng trừ và TextBlock cho số lượng
                StackPanel controlPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(5),
                    Visibility = Visibility.Hidden
                };

                Button decreaseButton = new Button
                {
                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/backgrounds/minus.png")),
                        Width = 20,
                        Height = 20
                    },
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Tag = quantityTextBlock,
                    Style = (Style)FindResource("NoHighlightButtonStyle")
                };
                decreaseButton.Click += DecreaseCount;

                Button increaseButton = new Button
                {
                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/backgrounds/plus.png")),
                        Width = 20,
                        Height = 20
                    },
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Tag = quantityTextBlock,
                    Style = (Style)FindResource("NoHighlightButtonStyle")
                };
                increaseButton.Click += IncreaseCount;

                controlPanel.Children.Add(decreaseButton);
                controlPanel.Children.Add(increaseButton);

                // Tạo Grid để chứa cả thumbnailImage và quantityGrid
                Grid grid = new Grid();
                grid.Children.Add(thumbnailImage);
                grid.Children.Add(quantityGrid);
                grid.Children.Add(controlPanel);

                // Tạo Border cho Grid
                Border border = new Border
                {
                    Child = grid,
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(5),
                    Tag = photoFile // Lưu đường dẫn file ảnh trong thuộc tính Tag
                };

                // Thêm sự kiện click cho Border
                border.MouseLeftButtonDown += (s, e) =>
                {
                    ThumbnailBooking_Click(s, e);
                    controlPanel.Visibility = controlPanel.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                };

                BookingPhotoThumbnailWrapPanel.Children.Add(border);
            }

            // Hiển thị BookingPhotoThumbnailGrid
            BookingPhotoThumbnailGrid.Visibility = Visibility.Visible;
        }

        private List<string> selectedPhotoPaths = new List<string>();
        private Dictionary<string, int> selectedPhotoPathsWithQuantities = new Dictionary<string, int>();

        private void ThumbnailBooking_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedBorder = sender as Border;
            if (clickedBorder != null)
            {
                var parentGrid = clickedBorder.Child as Grid;
                var quantityGrid = parentGrid?.Children.OfType<Grid>().FirstOrDefault(grid => grid.Children.OfType<Ellipse>().Any());

                if (quantityGrid != null)
                {
                    // Nếu ảnh đã được chọn, bỏ chọn
                    if (quantityGrid.Visibility == Visibility.Visible)
                    {
                        quantityGrid.Visibility = Visibility.Hidden;
                        clickedBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        selectedPhotoPaths.Remove(clickedBorder.Tag as string);
                        selectedPhotoPathsWithQuantities.Remove(clickedBorder.Tag as string);
                    }
                    else
                    {
                        // Nếu ảnh chưa được chọn, chọn ảnh
                        quantityGrid.Visibility = Visibility.Visible;
                        clickedBorder.BorderBrush = System.Windows.Media.Brushes.Green;
                        var quantityTextBlock = quantityGrid.Children.OfType<TextBlock>().FirstOrDefault();
                        selectedPhotoPaths.Add(clickedBorder.Tag as string);
                        selectedPhotoPathsWithQuantities[clickedBorder.Tag as string] = 1;
                        if (quantityTextBlock != null)
                        {
                            quantityTextBlock.Text = "1"; // Đặt số lượng ban đầu là 1
                        }
                    }
                }
            }
        }

        private void IncreaseCount(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                var quantityTextBlock = button.Tag as TextBlock;
                if (quantityTextBlock != null)
                {
                    int count;
                    if (int.TryParse(quantityTextBlock.Text, out count))
                    {
                        count++;
                        quantityTextBlock.Text = count.ToString();
                        var parentGrid = VisualTreeHelper.GetParent(button) as Grid;
                        if (parentGrid != null)
                        {
                            var border = VisualTreeHelper.GetParent(parentGrid) as Border;
                            if (border != null && border.Tag is string photoPath)
                            {
                                selectedPhotoPathsWithQuantities[photoPath] = count;
                            }
                        }
                    }
                }
            }
        }

        private void DecreaseCount(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                var quantityTextBlock = button.Tag as TextBlock;
                if (quantityTextBlock != null)
                {
                    int count;
                    if (int.TryParse(quantityTextBlock.Text, out count))
                    {
                        count = Math.Max(1, count - 1);
                        quantityTextBlock.Text = count.ToString();
                        var parentGrid = VisualTreeHelper.GetParent(button) as Grid;
                        if (parentGrid != null)
                        {
                            var border = VisualTreeHelper.GetParent(parentGrid) as Border;
                            if (border != null && border.Tag is string photoPath)
                            {
                                selectedPhotoPathsWithQuantities[photoPath] = count;
                            }
                        }
                    }
                }
            }
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

            PrintMenuGrid.Visibility = Visibility.Visible;
            PrintMenuStackPanel.Visibility = Visibility.Visible;
            PrintButton.Visibility = Visibility.Visible;

            //NumberOfCopiesTextBox.Visibility = Visibility.Visible;
            //AddOneCopyButton.Visibility = Visibility.Visible;
            //MinusOneCopyButton.Visibility = Visibility.Visible;

            PrintMenuGrid.Visibility = Visibility.Visible;
            SendEmailButton.Visibility = Visibility.Visible;


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

            foreach (var photoPathWithQuantity in selectedPhotoPathsWithQuantities)
            {
                string photoPath = photoPathWithQuantity.Key;
                int quantity = photoPathWithQuantity.Value;

                Printing.Print(photoPath, printerName, (short)quantity); 
            }

            //photosInTemplate = 0;
            //photoNumberInTemplate = 0;

            //Printing.Print(printPath, printerName, actualNumberOfCopies);
            //actualNumberOfCopies = 1;

            //if (turnOnTemplateMenu) StartLayoutsWelcomeMenu();
            //else StartWelcomeMenu();
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
                layout.LayoutCode = actualSettings.Root.Element("actualTemplate").Value;
                if (actualSettings.Root.Element("actualTemplate").Value == "All")
                {
                    turnOnTemplateMenu = true;
                }
                firstprinter = actualSettings.Root.Element("actualPrinter").Value;
                secondprinter = actualSettings.Root.Element("secondPrinter").Value;
                maxCopies = System.Convert.ToInt32(actualSettings.Root.Element("maxNumberOfCopies").Value);
                timeLeft = System.Convert.ToInt32(actualSettings.Root.Element("timeBetweenPhotos").Value);
                printTime = System.Convert.ToInt32(actualSettings.Root.Element("printingTime").Value);
                printerName = FBoothApp.Printing.ActualPrinter(layout.LayoutCode, firstprinter, secondprinter);
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
            StartButton.Visibility = Visibility.Visible;

            ReadyButton.Visibility = Visibility.Hidden;
            PrintButton.Visibility = Visibility.Hidden;
            ShowPrint.Visibility = Visibility.Hidden;

            //NumberOfCopiesTextBox.Visibility = Visibility.Hidden;
            //AddOneCopyButton.Visibility = Visibility.Hidden;
            //MinusOneCopyButton.Visibility = Visibility.Hidden;
            PrintMenuGrid.Visibility = Visibility.Hidden;
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

            // Mở bàn phím ảo
            //Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar + "osk.exe");

            EmailSendDialog inputEmailSendDialog = new EmailSendDialog("Please enter your email address:", "name@gmail.com");
            if (inputEmailSendDialog.ShowDialog() == true)
            {
                Debug.WriteLine("inputemailsend is ok, answer is :" + inputEmailSendDialog.Answer);
                EmailSender emailSender = new EmailSender();
                emailSender.SendEmail(inputEmailSendDialog.Answer, selectedPhotoPaths);
            }
        }



        private void RoundedTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (RoundedTextBox.Text == "Enter your code here!")
            {
                RoundedTextBox.Text = string.Empty;
                RoundedTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void RoundedTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoundedTextBox.Text))
            {
                RoundedTextBox.Text = "Enter your code here!";
                RoundedTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void RoundedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SendButton == null) return; // Ensure SendButton is not null

            if (string.IsNullOrWhiteSpace(RoundedTextBox.Text) || RoundedTextBox.Text == "Enter your code here!")
            {
                SendButton.IsEnabled = false;
                SendButton.Opacity = 0.5;
            }
            else
            {
                SendButton.IsEnabled = true;
                SendButton.Opacity = 1.0;
            }
        }
    }
}


