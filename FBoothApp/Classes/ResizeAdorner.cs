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

namespace FBoothApp.Classes
{
    public class ResizeAdorner : Adorner
    {
        private readonly Thumb _resizeThumb;
        private readonly VisualCollection _visuals;

        public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _visuals = new VisualCollection(this);
            _resizeThumb = new Thumb
            {
                Cursor = Cursors.SizeNWSE,
                Width = 10,
                Height = 10,
                Background = new SolidColorBrush(Colors.Coral)
            };

            _resizeThumb.DragDelta += ResizeThumb_DragDelta;

            _visuals.Add(_resizeThumb);
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (AdornedElement is FrameworkElement adornedElement)
            {
                double aspectRatio = adornedElement.ActualWidth / adornedElement.ActualHeight;
                double newWidth = adornedElement.Width + e.HorizontalChange;
                double newHeight = newWidth / aspectRatio;

                if (newWidth > 0 && newHeight > 0)
                {
                    adornedElement.Width = newWidth;
                    adornedElement.Height = newHeight;
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _resizeThumb.Arrange(new Rect(AdornedElement.DesiredSize.Width - 5, AdornedElement.DesiredSize.Height - 5, 10, 10));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        protected override int VisualChildrenCount => _visuals.Count;
    }
}
