using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using MyHorizons.Data;
using MyHorizons.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static MyHorizons.Avalonia.Utility.GridUtil;

namespace MyHorizons.Avalonia.Controls
{
    internal class ItemGrid : Canvas
    {
        private static readonly Border ItemToolTip = new Border
        {
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            BorderBrush = Brushes.White,
            Child = new TextBlock
            {
                Foreground = Brushes.Black,
                Background = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                ZIndex = int.MaxValue
            }
        };

        private int _itemsPerRow;
        private int _itemsPerCol;
        private int _itemSize;
        private ItemCollection _items;
        private IReadOnlyList<Line> LineCache;
        private readonly IList<RectangleGeometry> ItemCache;
        private int X = -1;
        private int Y = -1;
        private int CurrentIdx = -1;

        private bool MouseLeftDown;
        private bool MouseRightDown;
        private bool MouseMiddleDown;

        private const uint GridColor = 0xFF999999;
        private const uint HighlightColor = 0x7FFFFF00;
        private static readonly Pen GridPen = new Pen(new SolidColorBrush(GridColor), 2, null, PenLineCap.Flat, PenLineJoin.Bevel);
        private static readonly SolidColorBrush HighlightBrush = new SolidColorBrush(HighlightColor);
        private static readonly SolidColorBrush ItemBrush = new SolidColorBrush(0xBB00FF00);
        private static readonly Bitmap BackgroundImage = new Bitmap(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("resm:MyHorizons.Avalonia.Resources.ItemGridBackground.png")));

        public int ItemsPerRow
        {
            get => _itemsPerRow;
            set
            {
                if (_itemsPerRow == value) return;
                _itemsPerRow = value;
                Resize(_itemsPerRow * _itemSize, _itemsPerCol * _itemSize);
            }
        }

        public int ItemsPerCol
        {
            get => _itemsPerCol;
            set
            {
                if (_itemsPerCol == value) return;
                _itemsPerCol = value;
                Resize(_itemsPerRow * _itemSize, _itemsPerCol * _itemSize);
            }
        }

        public int ItemSize
        {
            get => _itemSize;
            set
            {
                if (_itemSize == value) return;
                _itemSize = value;
                Resize(_itemsPerRow * _itemSize, _itemsPerCol * _itemSize);
                if (Background is ImageBrush brush)
                    brush.DestinationRect = new RelativeRect(0, 0, _itemSize, _itemSize, RelativeUnit.Absolute);
            }
        }

        public ItemCollection Items
        {
            get => _items;
            set
            {
                if (value == null) return;

                _items = value;
                _items.PropertyChanged += Items_PropertyChanged;
                InvalidateVisual(); // Invalidate the visual state so we re-render the image.
            }
        }

        private void Items_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Items")
                InvalidateVisual();
        }

        public ItemGrid(int numItems, int itemsPerRow, int itemsPerCol, int itemSize = 32)
        {
            _itemsPerRow = itemsPerRow;
            _itemsPerCol = itemsPerCol;
            _itemSize = itemSize;

            var items1 = new Item[numItems];
            for (var i = 0; i < numItems; i++)
                items1[i] = Item.NO_ITEM;
            _items = new ItemCollection(items1);
            LineCache = new List<Line>();
            ItemCache = new List<RectangleGeometry>();
            Resize(itemsPerRow * itemSize, itemsPerCol * itemSize);
            Background = new ImageBrush(BackgroundImage)
            {
                Stretch = Stretch.Uniform,
                TileMode = TileMode.Tile,
                SourceRect = new RelativeRect(0, 0, BackgroundImage.Size.Width, BackgroundImage.Size.Height, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(0, 0, itemSize, itemSize, RelativeUnit.Absolute)
            };

            PointerMoved += ItemGrid_PointerMoved;
            PointerLeave += ItemGrid_PointerLeave;
            PointerPressed += ItemGrid_PointerPressed;
            PointerReleased += ItemGrid_PointerReleased;
        }

        private void SetItem()
        {
            var currentItem = _items[CurrentIdx];
            if (currentItem == MainWindow.SelectedItem) // Poor hack
                return;
            InvalidateVisual();
            _items[CurrentIdx] = MainWindow.SelectedItem.Clone();
        }

        private void ShowTip(PointerEventArgs e, bool updateText = false)
        {
            var grid = MainWindow.Singleton().FindControl<Grid>("MainContentGrid");
            var point = e.GetPosition(grid);

            if (updateText && ItemToolTip.Child is TextBlock block)
                block.Text = ItemDatabaseLoader.GetNameForItem(_items[CurrentIdx]);

            ItemToolTip.Margin = new Thickness(point.X + 15, point.Y + 10, 0, 0);
            if (ItemToolTip.Parent == null)
                grid.Children.Add(ItemToolTip);
        }

        private static void HideTip()
        {
            if (ItemToolTip.Parent != null && ItemToolTip.Parent is Grid grid)
                grid.Children.Remove(ItemToolTip);
        }

        private void ItemGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonReleased:
                    MouseLeftDown = false;
                    break;
                case PointerUpdateKind.RightButtonReleased:
                    MouseRightDown = false;
                    break;
                case PointerUpdateKind.MiddleButtonReleased:
                    MouseMiddleDown = false;
                    break;
            }
        }

        private void ItemGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                    if (CurrentIdx > -1 && CurrentIdx < _items.Count)
                    {
                        SetItem();
                        ShowTip(e, true);
                    }
                    MouseLeftDown = true;
                    break;
                case PointerUpdateKind.RightButtonPressed:
                    if (CurrentIdx > -1 && CurrentIdx < _items.Count)
                        MainWindow.Singleton().SetItem(_items[CurrentIdx]);
                    MouseRightDown = true;
                    break;
                case PointerUpdateKind.MiddleButtonPressed:
                    MouseMiddleDown = true;
                    break;
            }
        }

        private void ItemGrid_PointerLeave(object? sender, PointerEventArgs e)
        {
            if (X == -1 || Y == -1) return;
            X = Y = CurrentIdx = -1;
            HideTip();
            InvalidateVisual();
        }

        private void ItemGrid_PointerMoved(object? sender, PointerEventArgs e)
        {
            var point = e.GetPosition(sender as IVisual);
            if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
            {
                X = Y = CurrentIdx = -1;
                MouseLeftDown = false;
                MouseRightDown = false;
                MouseMiddleDown = false;
                HideTip();
                InvalidateVisual();
                return;
            }

            var tX = (int)point.X - (int)point.X % _itemSize;
            var tY = (int)point.Y - (int)point.Y % _itemSize;
            var updateText = false;

            if (tX != X || tY != Y)
            {
                X = tX;
                Y = tY;

                var idx = (tY / _itemSize) * _itemsPerRow + tX / _itemSize;
                if (idx != CurrentIdx)
                {
                    CurrentIdx = idx;
                    updateText = true;
                }

                InvalidateVisual();
            }

            if (CurrentIdx > -1 && CurrentIdx < _items.Count)
            {
                if (MouseLeftDown)
                {
                    SetItem();
                    ShowTip(e, true);
                    return;
                }

                if (MouseRightDown)
                    MainWindow.Singleton().SetItem(_items[CurrentIdx]);
                ShowTip(e, updateText);
            }
        }

        private void Resize(int width, int height)
        {
            Width = width + 1;
            Height = height + 1;
            CreateAndCacheGridLines();
            CreateAndCacheItemRects();
        }

        private void CreateAndCacheGridLines() => LineCache = GetGridCache(Width, Height, _itemSize, _itemSize);

        private void CreateAndCacheItemRects()
        {
            ItemCache.Clear();
            for (var y = 0; y < ItemsPerCol; y++)
                for (var x = 0; x < ItemsPerRow; x++)
                    ItemCache.Add(new RectangleGeometry(new Rect(x * _itemSize, y * _itemSize, _itemSize, _itemSize)));
        }

        private static int Clamp(int value, int min, int max) => value < min ? value : value > max ? max : value;

        private int PositionToIdx(int x, int y) => Math.Max(0, y) / _itemSize * _itemsPerRow + (Math.Max(0, x) / _itemSize) % _itemsPerRow;

        private Item PositionToItem(int x, int y) => Items[PositionToIdx(x, y)];

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Draw items.
            // TODO?: Caching the brushes in ItemColorManager may increase performance.
            if (Items != null)
                for (var i = 0; i < Items.Count; i++)
                    if (_items[i].ItemId != 0xFFFE)
                        context.DrawGeometry(ItemBrush, null, ItemCache[i]);

            // Draw highlight.
            if (X > -1 && Y > -1 && CurrentIdx > -1 && CurrentIdx < _items.Count)
                context.FillRectangle(HighlightBrush, new Rect(X, Y, _itemSize, _itemSize));

            // Draw grid above items from gridline cache.
            foreach (var line in LineCache)
                context.DrawLine(GridPen, line.Point0, line.Point1);
        }
    }
}
