using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyHorizons.Avalonia.Controls
{
    public class PlayersControl : UserControl
    {
        public PlayersControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
