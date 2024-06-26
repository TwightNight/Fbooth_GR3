using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;

namespace FBoothApp.Entity
{
    public class Sticker : ContentControl
    {
        private readonly Thumb _moveThumb;
        private readonly Button _closeButton;
        private readonly Image _image;

        public Sticker()
        {
            _moveThumb = new Thumb { Width = 0, Height = 0 }; // Invisible thumb for moving
            _closeButton = new Button { Content = "X", Width = 20, Height = 20 };
            _image = new Image();

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
                double newX = Canvas.GetLeft(this) + e.HorizontalChange;
                double newY = Canvas.GetTop(this) + e.VerticalChange;

                if (newX >= 0 && newX + this.ActualWidth <= canvas.ActualWidth)
                    Canvas.SetLeft(this, newX);

                if (newY >= 0 && newY + this.ActualHeight <= canvas.ActualHeight)
                    Canvas.SetTop(this, newY);
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
