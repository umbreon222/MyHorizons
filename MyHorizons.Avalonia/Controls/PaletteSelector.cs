using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using MyHorizons.Data.TownData;
using System;

namespace MyHorizons.Avalonia.Controls
{
    public sealed class PaletteSelector : Canvas
    {
        private static readonly Pen GridPen = new Pen(new SolidColorBrush(0xFF777777), 2, null, PenLineCap.Flat, PenLineJoin.Bevel);
        private static readonly Pen SelectedPen = new Pen(new SolidColorBrush(0xFFAFAF00), 2, null, PenLineCap.Flat, PenLineJoin.Bevel);
        private static readonly Bitmap BackgroundImage = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:MyHorizons.Avalonia.Resources.ItemGridBackground.png")));

        private DesignPattern? _design;

        private int _selectedIndex;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                InvalidateVisual();
            }
        }

        public new double Height
        {
            get => base.Height;
            private set => base.Height = value;
        }

        public new double Width
        {
            get => base.Width;
            set => Resize(value);
        }

        public PaletteSelector(DesignPattern pattern, double width = 16.0d)
        {
            _design = pattern;
            SelectedIndex = 0;
            Background = new ImageBrush(BackgroundImage)
            {
                Stretch = Stretch.Uniform,
                TileMode = TileMode.Tile,
                SourceRect = new RelativeRect(0, 0, BackgroundImage.Size.Width, BackgroundImage.Size.Height, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, width, width, RelativeUnit.Absolute),
            };
            Resize(width);
            PointerPressed += OnPointerPressed;
        }

        public void SetDesign(DesignPattern? pattern)
        {
            _design = pattern;
            InvalidateVisual();
        }

        private void Resize(double w)
        {
            base.Width = w;
            base.Height = 16 * w;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(sender as IVisual);
            var idx = (int)(point.Y / Width);
            if (idx == SelectedIndex || idx <= -1 || idx >= 16)
                return;
            SelectedIndex = idx;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_design == null)
                return;
            for (var i = 0; i < 16; i++)
            {
                var rect = new Rect(0, i * Width, Width, Width);
                if (i < 15)
                    context.FillRectangle(new SolidColorBrush(_design.Palette[i].ToArgb()), rect);
                context.DrawRectangle(i == SelectedIndex ? SelectedPen : GridPen, rect);
            }
        }
    }
}
