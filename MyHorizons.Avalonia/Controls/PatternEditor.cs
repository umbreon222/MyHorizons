using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using MyHorizons.Data.TownData;
using System;
using System.Collections.Generic;
using static MyHorizons.Avalonia.Utility.GridUtil;

namespace MyHorizons.Avalonia.Controls
{
    public sealed class PatternEditor : PatternVisualizer
    {
        private readonly PaletteSelector _paletteSelector;
        private IReadOnlyList<Line>? LineCache;
        private int CellX = -1;
        private int CellY = -1;

        private double StepX;
        private double StepY;

        private bool LeftDown;
        private bool RightDown;

        private static readonly Pen GridPen = new Pen(new SolidColorBrush(0xFF999999), 2, null, PenLineCap.Flat, PenLineJoin.Bevel);
        private static readonly Pen HighlightPen = new Pen(new SolidColorBrush(0xFFFFFF00), 2, null, PenLineCap.Flat, PenLineJoin.Bevel);

        public PatternEditor(DesignPattern pattern, PaletteSelector selector, double width = 32, double height = 32) : base(pattern, width, height)
        {
            _paletteSelector = selector;
            Resize(Width, Height);
            ToolTip.SetTip(this, null);

            this.GetObservable(WidthProperty).Subscribe(newWidth => Resize(newWidth, Height));
            this.GetObservable(HeightProperty).Subscribe(newHeight => Resize(Width, newHeight));
            PointerMoved += OnPointerMoved;
            PointerLeave += (o, e) =>
            {
                CellX = CellY = -1;
                InvalidateVisual();
            };
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
        }

        private void Resize(double width, double height)
        {
            StepX = width / PATTERN_WIDTH;
            StepY = height / PATTERN_HEIGHT;
            LineCache = GetGridCache(width + 1, height + 1, StepX, StepY);
            InvalidateVisual();
        }

        public void SetDesign(DesignPattern? design)
        {
            Design = design;
            _paletteSelector.SetDesign(design);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var point = e.GetPosition(sender as IVisual);
            if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
            {
                CellX = CellY = -1;
                InvalidateVisual();
            }
            else
            {
                var tX = (int)(point.X / (Width / PATTERN_WIDTH));
                var tY = (int)(point.Y / (Height / PATTERN_HEIGHT));
                if (tX < 0 || tX >= PATTERN_WIDTH || tY < 0 || tY >= PATTERN_HEIGHT)
                {
                    CellX = CellY = -1;
                    InvalidateVisual();
                }
                else if (tX != CellX || tY != CellY)
                {
                    CellX = tX;
                    CellY = tY;

                    if (LeftDown)
                    {
                        if (Design?.GetPixel(CellX, CellY) != _paletteSelector.SelectedIndex)
                        {
                            Design?.SetPixel(CellX, CellY, (byte)_paletteSelector.SelectedIndex);
                            UpdateBitmap();
                        }
                    }
                    else if (RightDown)
                    {
                        _paletteSelector.SelectedIndex = Design?.GetPixel(CellX, CellY) ?? -1;
                    }

                    InvalidateVisual();
                }
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                    {
                        if (Design?.GetPixel(CellX, CellY) != _paletteSelector.SelectedIndex)
                        {
                            Design?.SetPixel(CellX, CellY, (byte)_paletteSelector.SelectedIndex);
                            UpdateBitmap();
                        }
                        LeftDown = true;
                        break;
                    }
                case PointerUpdateKind.RightButtonPressed:
                    {
                        _paletteSelector.SelectedIndex = Design?.GetPixel(CellX, CellY) ?? -1;
                        RightDown = true;
                        break;
                    }
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonReleased:
                    {
                        LeftDown = false;
                        break;
                    }
                case PointerUpdateKind.RightButtonReleased:
                    {
                        RightDown = false;
                        break;
                    }
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Draw grid
            if (LineCache != null)
                foreach (var line in LineCache)
                    context.DrawLine(GridPen, line.Point0, line.Point1);

            // Draw highlight
            if (CellX > -1 && CellY > -1)
                context.DrawRectangle(HighlightPen, new Rect(CellX * StepX, CellY * StepY, StepX, StepY));
        }
    }
}
