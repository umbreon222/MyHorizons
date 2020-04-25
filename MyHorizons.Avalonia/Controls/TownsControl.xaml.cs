using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyHorizons.Avalonia.Controls
{
    public class TownsControl : UserControl
    {
        public TownsControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
