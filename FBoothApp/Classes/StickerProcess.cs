using System;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

namespace FBoothApp.Entity
{
    public class StickerProcess : ContentControl
    {
        private readonly Thumb _moveThumb;
        private readonly Button _closeButton;
        private readonly Image _image;

        public StickerProcess()
        {
            _moveThumb = new Thumb { Width = 0, Height = 0 }; // Invisible thumb for moving
            _closeButton = new Button
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };

            _image = new Image();

            var closeImage = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/backgrounds/close.png")),
                Width = 16,
                Height = 16
            };

            _closeButton.Content = closeImage;
            _closeButton.Click += CloseButton_Click;

            var visualContainer = new Grid();
            visualContainer.Children.Add(_image);
            visualContainer.Children.Add(_moveThumb);
            visualContainer.Children.Add(_closeButton);

            _closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            _closeButton.VerticalAlignment = VerticalAlignment.Top;

            this.Content = visualContainer;

            _moveThumb.DragDelta += MoveThumb_DragDelta;

            this.MouseLeftButtonDown += Sticker_MouseLeftButtonDown;
            this.MouseLeftButtonUp += Sticker_MouseLeftButtonUp;
        }

        public BitmapImage ImageSource
        {
            get => (BitmapImage)_image.Source;
            set => _image.Source = value;
        }

        public void HideCloseButton()
        {
            _closeButton.Visibility = Visibility.Collapsed;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.Parent is Canvas canvas)
            {
                double newLeft = Canvas.GetLeft(this) + e.HorizontalChange;
                double newTop = Canvas.GetTop(this) + e.VerticalChange;

                // Ensure the sticker stays within the bounds of the canvas
                newLeft = Math.Max(0, newLeft);
                newTop = Math.Max(0, newTop);
                newLeft = Math.Min(canvas.ActualWidth - this.ActualWidth, newLeft);
                newTop = Math.Min(canvas.ActualHeight - this.ActualHeight, newTop);

                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Parent is Canvas canvas)
            {
                canvas.Children.Remove(this);
            }
        }

        private void Sticker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CaptureMouse();
        }

        private void Sticker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
        }
    }
}
