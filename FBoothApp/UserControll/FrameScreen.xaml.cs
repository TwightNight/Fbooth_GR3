using FBoothApp.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace FBoothApp.UserControll
{
    /// <summary>
    /// Interaction logic for FrameScreen.xaml
    /// </summary>
    public partial class FrameScreen : UserControl
    {

        public System.Windows.Controls.Image image;
        public event EventHandler ButtonContinueClick;
        public List<System.Windows.Controls.Image> selectedImages;
        public int index;
        List<System.Drawing.Image> images;
        public Bitmap imgTemp;
        public string codeFrameType;
        public Frames frameList;
        public int numberOfCut;

        public FrameScreen(BitmapImage selectedImages, Frames frameList, int numberOfCut)
        {
            InitializeComponent();
            image = new System.Windows.Controls.Image();
            index = 0;
            codeFrameType = $"{numberOfCut}a";
            //images = ConvertToDrawingImages(selectedImages);
            SetUpRightGrid();
            LoadFrames();
        }

        private void SetUpRightGrid()
        {
            double columnWidth = 250;
            double rowHeight = 250;
            string currentDirectory = Directory.GetCurrentDirectory();
            string projectDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            string framesDirectory = Path.Combine(projectDirectory, $"Frames\\{numberOfCut}cut");

            List<string> defaultFramesList = GetDefaultFramesList(framesDirectory);
            for (int i = 0; i < 2; i++)
            {
                ColumnDefinition columnDefinition = new ColumnDefinition();
                columnDefinition.Width = new GridLength(columnWidth, GridUnitType.Pixel);
                frames.ColumnDefinitions.Add(columnDefinition);
            }

            for (int i = 0; i < defaultFramesList.Count / 2 + 1; i++)
            {
                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(rowHeight, GridUnitType.Pixel);
                frames.RowDefinitions.Add(rowDefinition);
            }
            frames.HorizontalAlignment = HorizontalAlignment.Center;
            frames.VerticalAlignment = VerticalAlignment.Center;
        }
        private void LoadFrames()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string projectDirectory = Directory.GetParent(currentDirectory).Parent.FullName;
            string framesDirectory = Path.Combine(projectDirectory, $"Frames\\{numberOfCut}cut");
            List<string> defaultFramesList = GetDefaultFramesList(framesDirectory);
            int columnIndex = 0;
            int rowIndex = 0;
            foreach (string frameFile in defaultFramesList)
            {
                System.Windows.Controls.Image frame = new System.Windows.Controls.Image();
                frame.Source = new BitmapImage(new Uri(frameFile));
                frame.Stretch = Stretch.Uniform;
                frame.Margin = new Thickness(10);

                frames.Children.Add(frame);
                Grid.SetColumn(frame, columnIndex);
                Grid.SetRow(frame, rowIndex);
                frame.MouseLeftButtonDown += Frame_MouseLeftButtonDown;
                columnIndex++;
                if (columnIndex == 2)
                {
                    columnIndex = 0;
                    rowIndex++;
                }
            }
            imgTemp = RenderManager.GhepHinh(frameList.GetType($"{numberOfCut}a"), images);
            Photo.Source = ConvertToBitmapSource(RenderManager.GhepHinh(frameList.GetType($"{numberOfCut}a"), images));
        }


        private List<string> GetDefaultFramesList(string directory)
        {
            List<string> defaultframeLists = new List<string>();

            foreach (string file in Directory.GetFiles(directory, "default.png", SearchOption.AllDirectories))
            {
                defaultframeLists.Add(file);
            }
            return defaultframeLists;
        }
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonContinueClick?.Invoke(this, EventArgs.Empty);
        }

        private void Frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image clickedFrame = sender as System.Windows.Controls.Image;

            index = frames.Children.IndexOf(clickedFrame);
            if (index == 0)
            {
                codeFrameType = $"{numberOfCut}a";
            }
            else if (index == 1)
            {
                codeFrameType = $"{numberOfCut}b";
            }
            else if (index == 2)
            {
                codeFrameType = $"{numberOfCut}c";
            }
            else if (index == 3)
            {
                codeFrameType = $"{numberOfCut}d";
            }
            imgTemp = RenderManager.GhepHinh(frameList.GetType(codeFrameType), images);
            Photo.Source = ConvertToBitmapSource(RenderManager.GhepHinh(frameList.GetType(codeFrameType), images));
        }
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
        //public List<System.Drawing.Image> ConvertToDrawingImages(List<System.Windows.Controls.Image> wpfImages)
        //{
        //    List<System.Drawing.Image> drawingImages = new List<System.Drawing.Image>();

        //    foreach (System.Windows.Controls.Image wpfImage in wpfImages)
        //    {
        //        BitmapSource bitmapSource = (BitmapSource)wpfImage.Source;
        //        Bitmap bitmap = BitmapFromSource(bitmapSource);
        //        System.Drawing.Image drawingImage = System.Drawing.Image.FromHbitmap(bitmap.GetHbitmap());

        //        drawingImages.Add(drawingImage);
        //    }

        //    return drawingImages;
        //}

        private Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
