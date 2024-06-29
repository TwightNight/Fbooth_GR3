using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace FBoothApp.Classes
{
    public class ResizeAdorner : Adorner
    {
        private readonly Thumb _resizeThumb;
        private readonly VisualCollection _visualChildren;
        private readonly FrameworkElement _stickerElement;
        private readonly double _aspectRatio;
        public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _stickerElement = adornedElement as FrameworkElement;
            _visualChildren = new VisualCollection(this);

            // Tính toán tỷ lệ (aspect ratio) của sticker
            _aspectRatio = _stickerElement.Width / _stickerElement.Height;

            _resizeThumb = new Thumb
            {
                Cursor = Cursors.SizeNWSE,
                Width = 10,
                Height = 10,
                Background = Brushes.Blue
            };
            _resizeThumb.DragDelta += ResizeThumb_DragDelta;

            _visualChildren.Add(_resizeThumb);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double adornerWidth = _stickerElement.ActualWidth;
            double adornerHeight = _stickerElement.ActualHeight;
            _resizeThumb.Arrange(new Rect(adornerWidth - _resizeThumb.Width, adornerHeight - _resizeThumb.Height, _resizeThumb.Width, _resizeThumb.Height));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index) => _visualChildren[index];
        protected override int VisualChildrenCount => _visualChildren.Count;
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_stickerElement == null) return;

            // Tính toán kích thước mới với tỷ lệ
            double newWidth = _stickerElement.Width + e.HorizontalChange;
            double newHeight = newWidth / _aspectRatio;

            var parentCanvas = VisualTreeHelper.GetParent(_stickerElement) as Canvas;
            var showPrint = Application.Current.MainWindow.FindName("ShowPrint") as Image;

            if (showPrint != null && parentCanvas != null)
            {
                // Giới hạn kích thước sticker trong phạm vi ShowPrint
                double maxWidth = showPrint.ActualWidth - Canvas.GetLeft(_stickerElement);
                double maxHeight = showPrint.ActualHeight - Canvas.GetTop(_stickerElement);

                bool canResize = true;

                if (newWidth > maxWidth)
                {
                    newWidth = maxWidth;
                    newHeight = newWidth / _aspectRatio;
                    canResize = false;
                }

                if (newHeight > maxHeight)
                {
                    newHeight = maxHeight;
                    newWidth = newHeight * _aspectRatio;
                    canResize = false;
                }

                if (newWidth > 0 && newHeight > 0 && canResize)
                {
                    _stickerElement.Width = newWidth;
                    _stickerElement.Height = newHeight;
                }
            }

            // Cập nhật lại vị trí của resize thumb
            InvalidateArrange();
        }   
    }
}
