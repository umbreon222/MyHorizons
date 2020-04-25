using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;

namespace MyHorizons.Avalonia.Controls
{
    public class VillagersControl : UserControl
    {
        public VillagersControl()
        {
            this.InitializeComponent();
            var villagerFilter = this.FindControl<TextBox>("VillagerFilter");
            villagerFilter.KeyUp += VillagerFilter_KeyUp;
            var villagerFilterClear = this.FindControl<Button>("VillagerFilterClear");
            villagerFilterClear.Click += VillagerFilterClear_Click;
        }

        private void VillagerFilterClear_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            var villagerFilter = this.FindControl<TextBox>("VillagerFilter");
            villagerFilter.Text = string.Empty;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void VillagerFilter_KeyUp(object? sender, KeyEventArgs e)
        {
            if (sender == null)
                return;
            var villagerFilter = (TextBox)sender;
            var searchText = villagerFilter.Text;
            var villagerBox = this.FindControl<ComboBox>("VillagerBox");
            if (villagerBox.ItemCount == 0)
                return;
            villagerBox.SelectedItem = villagerBox.Items.OfType<string>()
                .Select(item => new Tuple<string, int>(item, LevenshteinDistance(searchText, item)))
                .OrderBy(x => x.Item2).First().Item1;
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            {
                return 0;
            }
            if (string.IsNullOrEmpty(a))
            {
                return b.Length;
            }
            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }
            var lengthA = a.Length;
            var lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (var i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (var j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (var i = 1; i <= lengthA; i++)
            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min
                (
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost
                );
            }
            return distances[lengthA, lengthB];
        }
    }
}
