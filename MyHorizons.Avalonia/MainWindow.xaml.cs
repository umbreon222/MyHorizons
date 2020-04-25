using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MyHorizons.Avalonia.Controls;
using MyHorizons.Avalonia.Utility;
using MyHorizons.Data;
using MyHorizons.Data.PlayerData;
using MyHorizons.Data.Save;
using MyHorizons.Data.TownData;
using MyHorizons.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyHorizons.Avalonia
{
    public class MainWindow : Window
    {
        private static MainWindow? _singleton;

        private MainSaveFile? SaveFile;
        private Player? SelectedPlayer;
        private Villager? SelectedVillager;

        private readonly Grid TitleBarGrid;

        private readonly Grid CloseGrid;
        private readonly Button CloseButton;

        private readonly Grid ResizeGrid;
        private readonly Button ResizeButton;

        private readonly Grid MinimizeGrid;
        private readonly Button MinimizeButton;

        private bool PlayerLoading;
        private bool SettingItem;
        private Dictionary<ushort, string>? ItemDatabase;
        private Dictionary<byte, string>[]? VillagerDatabase;

        private readonly ItemGrid PlayerPocketsGrid;
        private readonly ItemGrid PlayerStorageGrid;
        private readonly ItemGrid VillagerFurnitureGrid;
        private readonly ItemGrid VillagerWallpaperGrid;
        private readonly ItemGrid VillagerFlooringGrid;

        public static Item SelectedItem = Item.NO_ITEM.Clone();

        public static MainWindow Singleton() => _singleton ?? throw new Exception("MainWindow singleton not constructed!");

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            TitleBarGrid = this.FindControl<Grid>("TitleBarGrid");
            CloseGrid = this.FindControl<Grid>("CloseGrid");
            CloseButton = this.FindControl<Button>("CloseButton");
            ResizeGrid = this.FindControl<Grid>("ResizeGrid");
            ResizeButton = this.FindControl<Button>("ResizeButton");
            MinimizeGrid = this.FindControl<Grid>("MinimizeGrid");
            MinimizeButton = this.FindControl<Button>("MinimizeButton");

            SetupSide("Left", StandardCursorType.LeftSide, WindowEdge.West);
            SetupSide("Right", StandardCursorType.RightSide, WindowEdge.East);
            SetupSide("Top", StandardCursorType.TopSide, WindowEdge.North);
            SetupSide("Bottom", StandardCursorType.BottomSide, WindowEdge.South);
            SetupSide("TopLeft", StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
            SetupSide("TopRight", StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
            SetupSide("BottomLeft", StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
            SetupSide("BottomRight", StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);

            TitleBarGrid.PointerPressed += (i, e) => PlatformImpl?.BeginMoveDrag(e);

            CloseGrid.PointerEnter += CloseGrid_PointerEnter;
            CloseGrid.PointerLeave += CloseGrid_PointerLeave;

            ResizeGrid.PointerEnter += ResizeGrid_PointerEnter;
            ResizeGrid.PointerLeave += ResizeGrid_PointerLeave;

            MinimizeButton.PointerLeave += MinimizeButton_PointerLeave;
            MinimizeButton.PointerEnter += MinimizeButton_PointerEnter;

            CloseButton.Click += CloseButton_Click;
            ResizeButton.Click += ResizeButton_Click;
            MinimizeButton.Click += MinimizeButton_Click;

            PlatformImpl.WindowStateChanged = WindowStateChanged;

            var openBtn = this.FindControl<Button>("OpenSaveButton");
            openBtn.Click += OpenFileButton_Click;

            this.FindControl<Button>("SaveButton").Click += SaveButton_Click;

            PlayerPocketsGrid = new ItemGrid(40, 10, 4, 16);
            PlayerStorageGrid = new ItemGrid(5000, 50, 100, 16);
            VillagerFurnitureGrid = new ItemGrid(16, 8, 2, 16)
            {
                HorizontalAlignment = HorizontalAlignment.Left
            };
            VillagerWallpaperGrid = new ItemGrid(1, 1, 1, 16)
            {
                HorizontalAlignment = HorizontalAlignment.Left
            };
            VillagerFlooringGrid = new ItemGrid(1, 1, 1, 16)
            {
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var playersControl = this.FindControl<PlayersControl>("PlayersControl");
            var playersGrid = playersControl.FindControl<StackPanel>("PocketsPanel");
            playersGrid.Children.Add(PlayerPocketsGrid);

            playersControl.FindControl<ScrollViewer>("StorageScroller").Content = PlayerStorageGrid;

            var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
            villagersControl.FindControl<StackPanel>("VillagerFurniturePanel").Children.Add(VillagerFurnitureGrid);
            villagersControl.FindControl<StackPanel>("VillagerWallpaperPanel").Children.Add(VillagerWallpaperGrid);
            villagersControl.FindControl<StackPanel>("VillagerFlooringPanel").Children.Add(VillagerFlooringGrid);

            SetSelectedItemIndex();
            
            openBtn.IsVisible = true;
            this.FindControl<TabControl>("EditorTabControl").IsVisible = false;
            this.FindControl<Grid>("BottomBar").IsVisible = false;
            
            _singleton = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetSelectedItemIndex()
        {
            if (ItemDatabase != null)
            {
                for (var i = 0; i < ItemDatabase.Keys.Count; i++)
                {
                    if (ItemDatabase.Keys.ElementAt(i) != SelectedItem.ItemId)
                        continue;
                    SettingItem = true;
                    this.FindControl<ComboBox>("ItemSelectBox").SelectedIndex = i;
                    this.FindControl<NumericUpDown>("Flag0Box").Value = SelectedItem.Flags0;
                    this.FindControl<NumericUpDown>("Flag1Box").Value = SelectedItem.Flags1;
                    this.FindControl<NumericUpDown>("Flag2Box").Value = SelectedItem.Flags2;
                    this.FindControl<NumericUpDown>("Flag3Box").Value = SelectedItem.Flags3;
                    SettingItem = false;
                    return;
                }
            }
            this.FindControl<ComboBox>("ItemSelectBox").SelectedIndex = -1;
        }

        public void SetItem(Item item)
        {
            if (SelectedItem == item || ItemDatabase == null)
                return;
            SelectedItem = item.Clone();
            SetSelectedItemIndex();
        }

        private void SetupUniversalConnections()
        {
            var selectBox = this.FindControl<ComboBox>("ItemSelectBox");
            selectBox.SelectionChanged += (o, e) =>
            {
                if (!SettingItem && selectBox.SelectedIndex > -1 && ItemDatabase != null)
                    SelectedItem = new Item(ItemDatabase.Keys.ElementAt(selectBox.SelectedIndex),
                        SelectedItem.Flags0, SelectedItem.Flags1, SelectedItem.Flags2, SelectedItem.Flags3, SelectedItem.UseCount);
            };

            this.FindControl<NumericUpDown>("Flag0Box").ValueChanged += (o, e) =>
            {
                if (!SettingItem)
                    SelectedItem.Flags0 = (byte)e.NewValue;
            };
            this.FindControl<NumericUpDown>("Flag1Box").ValueChanged += (o, e) =>
            {
                if (!SettingItem)
                    SelectedItem.Flags1 = (byte)e.NewValue;
            };
            this.FindControl<NumericUpDown>("Flag2Box").ValueChanged += (o, e) =>
            {
                if (!SettingItem)
                    SelectedItem.Flags2 = (byte)e.NewValue;
            };
            this.FindControl<NumericUpDown>("Flag3Box").ValueChanged += (o, e) =>
            {
                if (!SettingItem)
                    SelectedItem.Flags3 = (byte)e.NewValue;
            };
        }

        private void SetupPlayerTabConnections()
        {
            var playersControl = this.FindControl<PlayersControl>("PlayersControl");
            playersControl.FindControl<TextBox>("PlayerNameBox").GetObservable(TextBox.TextProperty).Subscribe(text =>
            {
                if (!PlayerLoading)
                    SelectedPlayer?.SetName(text);
            });
            playersControl.FindControl<NumericUpDown>("WalletBox").ValueChanged += (o, e) =>
            {
                if (!PlayerLoading)
                    SelectedPlayer?.Wallet.Set((uint)e.NewValue);
            };
            playersControl.FindControl<NumericUpDown>("BankBox").ValueChanged += (o, e) =>
            {
                if (!PlayerLoading)
                    SelectedPlayer?.Bank.Set((uint)e.NewValue);
            };
            playersControl.FindControl<NumericUpDown>("NookMilesBox").ValueChanged += (o, e) =>
            {
                if (!PlayerLoading)
                    SelectedPlayer?.NookMiles.Set((uint)e.NewValue);
            };
        }

        private void SetupVillagerTabConnections()
        {
            var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
            var villagerBox = villagersControl.FindControl<ComboBox>("VillagerBox");
            villagerBox.SelectionChanged += (o, e) => SetVillagerFromIndex(villagerBox.SelectedIndex);
            var personalityBox = villagersControl.FindControl<ComboBox>("PersonalityBox");
            personalityBox.SelectionChanged += (o, e) =>
            {
                if (SelectedVillager != null)
                    SelectedVillager.Personality = (byte)personalityBox.SelectedIndex;
            };
            villagersControl.FindControl<TextBox>("CatchphraseBox").GetObservable(TextBox.TextProperty).Subscribe(text =>
            {
                if (SelectedVillager != null)
                    SelectedVillager.Catchphrase = text;
            });
            villagersControl.FindControl<CheckBox>("VillagerMovingOutBox").GetObservable(ToggleButton.IsCheckedProperty).Subscribe(isChecked => {
                if (SelectedVillager != null && isChecked != null)
                    SelectedVillager.SetIsMovingOut(isChecked.Value);
            });
        }

        private void SetupSide(string name, StandardCursorType cursor, WindowEdge edge)
        {
            var ctl = this.FindControl<Control>(name);
            ctl.Cursor = new Cursor(cursor);
            ctl.PointerPressed += (i, e) =>
            {
                if (WindowState == WindowState.Normal)
                    PlatformImpl?.BeginResizeDrag(edge, e);
            };
        }

        private void WindowStateChanged(WindowState state)
        {
            base.HandleWindowStateChanged(state);
            if (state == WindowState.Minimized)
                return;
            var imageLoader = new ImageLoader();
            var img = this.FindControl<Image>("ResizeImage");
            var stateString = WindowState == WindowState.Normal ? "Maximize" : "Restore";
            var uri = new Uri($"resm:MyHorizons.Avalonia.Resources.{stateString}.png");
            var bitmap = imageLoader.LoadCachedImage(uri);
            img.Source = bitmap;
        }

        private void AddPlayerImages()
        {
            if (SaveFile == null)
                return;
            var playersControl = this.FindControl<PlayersControl>("PlayersControl");
            var contentHolder = playersControl.FindControl<StackPanel>("PlayerSelectorPanel");
            foreach (var playerSave in SaveFile.GetPlayerSaves())
            {
                var player = playerSave.Player;
                var img = new Image
                {
                    Width = 120,
                    Height = 120,
                    Source = LoadPlayerPhoto(playerSave.Index),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Content = img
                };
                button.Click += (o, e) => LoadPlayer(player);
                ToolTip.SetTip(img, playerSave.Player.GetName());
                contentHolder.Children.Add(button);
            }
        }

        private void LoadPlayer(Player player)
        {
            if (player == null || player == SelectedPlayer)
                return;
            PlayerLoading = true;
            SelectedPlayer = player;
            var playersControl = this.FindControl<PlayersControl>("PlayersControl");
            playersControl.FindControl<TextBox>("PlayerNameBox").Text = player.GetName();
            playersControl.FindControl<NumericUpDown>("WalletBox").Value = player.Wallet.Decrypt();
            playersControl.FindControl<NumericUpDown>("BankBox").Value = player.Bank.Decrypt();
            playersControl.FindControl<NumericUpDown>("NookMilesBox").Value = player.NookMiles.Decrypt();
            PlayerPocketsGrid.Items = player.Pockets;
            PlayerStorageGrid.Items = player.Storage;
            PlayerLoading = false;
        }

        private void LoadPatterns()
        {
            if (SaveFile?.Town == null)
                return;
            var panel = this.FindControl<StackPanel>("DesignsPanel");
            var paletteSelector = new PaletteSelector(SaveFile.Town.Patterns[0]) { Margin = new Thickness(410, 0, 0, 0) };
            var editor = new PatternEditor(SaveFile.Town.Patterns[0], paletteSelector, 384, 384);
            for (var i = 0; i < 50; i++)
            {
                var visualizer = new PatternVisualizer(SaveFile.Town.Patterns[i]);
                visualizer.PointerPressed += (o, e) => editor.SetDesign(visualizer.Design);
                panel.Children.Add(visualizer);
            }
            this.FindControl<Grid>("DesignsContent").Children.Insert(0, editor);
            this.FindControl<Grid>("DesignsContent").Children.Insert(1, paletteSelector);
        }

        private void LoadVillager(Villager villager)
        {
            var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
            if (villager == null || villager == SelectedVillager)
                return;
            var villagerPanel = villagersControl.FindControl<StackPanel>("VillagerPanel");
            if (SelectedVillager != null && villagerPanel.Children[SelectedVillager.Index] is Button currentButton)
                currentButton.Background = Brushes.Transparent;

            SelectedVillager = null;
            if (VillagerDatabase != null)
            {
                var comboBox = villagersControl.FindControl<ComboBox>("VillagerBox");
                comboBox.SelectedIndex = GetIndexFromVillagerName(VillagerDatabase[villager.Species][villager.VariantIdx]);
            }
            villagersControl.FindControl<ComboBox>("PersonalityBox").SelectedIndex = villager.Personality;
            villagersControl.FindControl<TextBox>("CatchphraseBox").Text = villager.Catchphrase;
            VillagerFurnitureGrid.Items = villager.Furniture;
            VillagerWallpaperGrid.Items = villager.Wallpaper;
            VillagerFlooringGrid.Items = villager.Flooring;
            if (villagerPanel.Children[villager.Index] is Button btn)
                btn.Background = Brushes.LightGray;
            SelectedVillager = villager;
            villagersControl.FindControl<CheckBox>("VillagerMovingOutBox").IsChecked = villager.IsMovingOut();
        }

        private int GetIndexFromVillagerName(string name)
        {
            if (VillagerDatabase == null)
                return -1;
            var idx = 0;
            foreach (var species in VillagerDatabase)
            {
                foreach (var villager in species)
                {
                    if (villager.Value == name)
                        return idx;
                    idx++;
                }
            }
            return -1;
        }

        private void SetVillagerFromIndex(int index)
        {
            var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
            if (VillagerDatabase == null || SelectedVillager == null || index <= -1)
                return;
            var imageLoader = new ImageLoader();
            var count = 0;
            for (var i = 0; i < VillagerDatabase.Length; i++)
            {
                var speciesDict = VillagerDatabase[i];
                if (count + speciesDict.Count > index)
                {
                    var species = (byte)i;
                    var variant = speciesDict.Keys.ElementAt(index - count);
                    if (SelectedVillager.Species != species || SelectedVillager.VariantIdx != variant)
                    {
                        SelectedVillager.Species = species;
                        SelectedVillager.VariantIdx = variant;

                        // Update image
                        var panel = villagersControl.FindControl<StackPanel>("VillagerPanel");
                        if (!(panel.Children[SelectedVillager.Index] is Button btn) || !(btn.Content is Image img))
                            return;
                        img.Source = imageLoader.LoadImageForVillager(SelectedVillager);
                        ToolTip.SetTip(img, VillagerDatabase[species][variant]);
                        return;
                    }
                }
                count += speciesDict.Count;
            }
        }

        private void LoadVillagers()
        {
            if (SaveFile?.Town == null)
                return;
            var imageLoader = new ImageLoader();
            for (var i = 0; i < 10; i++)
            {
                var villager = SaveFile.Town.GetVillager(i);
                var img = new Image
                {
                    Width = 64,
                    Height = 64,
                    Source = imageLoader.LoadImageForVillager(villager),
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Name = $"Villager{i}",
                    Content = img
                };
                button.Click += (o, e) => LoadVillager(villager);
                if (VillagerDatabase != null)
                    ToolTip.SetTip(img, VillagerDatabase[villager.Species][villager.VariantIdx]);
                var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
                villagersControl.FindControl<StackPanel>("VillagerPanel").Children.Add(button);
            }
        }

        private void LoadVillagerComboBoxItems()
        {
            var villagersControl = this.FindControl<VillagersControl>("VillagersControl");
            if (VillagerDatabase != null)
            {
                var comboBox = villagersControl.FindControl<ComboBox>("VillagerBox");
                var villagerList = new List<string>();
                foreach (var speciesList in VillagerDatabase)
                    villagerList.AddRange(speciesList.Values);
                comboBox.Items = villagerList;
            }
            villagersControl.FindControl<ComboBox>("PersonalityBox").Items = Villager.Personalities;
        }

        private async void OpenFileButton_Click(object? o, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter
                        {
                            Name = "New Horizons Save File",
                            Extensions = new List<string>
                            {
                                "dat"
                            }
                        },
                        new FileDialogFilter
                        {
                            Name = "All Files",
                            Extensions = new List<string>
                            {
                                "*"
                            }
                        }
                    }
            };

            var files = await openFileDialog.ShowAsync(this);
            if (files.Length <= 0)
                return;
            // Determine whether they selected the header file or the main file
            var file = files[0];
            string? headerPath = null;
            string? filePath = null;
            string? directory = Path.GetDirectoryName(file);
            if (directory != null)
            {
                if (file.EndsWith("Header.dat"))
                {
                    headerPath = file;
                    filePath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(file).Replace("Header", "")}.dat");
                }
                else
                {
                    filePath = file;
                    headerPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(file)}Header.dat");
                }
            }
            else
            {
                return;
            }

            if (!File.Exists(headerPath) || !File.Exists(filePath))
                return;
            SaveFile = new MainSaveFile(headerPath, filePath);
            if (SaveFile.Loaded && SaveFile.Town != null)
            {
                VillagerDatabase = VillagerDatabaseLoader.LoadVillagerDatabase((uint)SaveFile.GetRevision());
                LoadVillagerComboBoxItems();

                if (o is Button btn)
                    btn.IsVisible = false;

                this.FindControl<TabControl>("EditorTabControl").IsVisible = true;
                this.FindControl<Grid>("BottomBar").IsVisible = true;
                this.FindControl<TextBlock>("SaveInfoText").Text = $"Save File for Version {SaveFile.GetRevisionString()} Loaded";
                AddPlayerImages();
                LoadPlayer(SaveFile.GetPlayerSaves()[0].Player);
                LoadVillagers();
                LoadVillager(SaveFile.Town.GetVillager(0));
                LoadPatterns();

                // Load Item List
                ItemDatabase = ItemDatabaseLoader.LoadItemDatabase((uint)SaveFile.GetRevision());
                var itemsBox = this.FindControl<ComboBox>("ItemSelectBox");
                if (ItemDatabase != null)
                    itemsBox.Items = ItemDatabase.Values;

                // Set up connections
                SetupUniversalConnections();
                SetupPlayerTabConnections();
                SetupVillagerTabConnections();
            }
            else
            {
                SaveFile = null;
            }
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            SaveFile?.Save(null);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

        private void ResizeButton_Click(object? sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void MinimizeButton_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void CloseGrid_PointerEnter(object? sender, PointerEventArgs e) => CloseGrid.Background = new SolidColorBrush(0xFF648589);

        private void CloseGrid_PointerLeave(object? sender, PointerEventArgs e) => CloseGrid.Background = Brushes.Transparent;

        private void ResizeGrid_PointerEnter(object? sender, PointerEventArgs e) => ResizeGrid.Background = new SolidColorBrush(0xFF648589);

        private void ResizeGrid_PointerLeave(object? sender, PointerEventArgs e) => ResizeGrid.Background = Brushes.Transparent;

        private void MinimizeButton_PointerEnter(object? sender, PointerEventArgs e) => MinimizeGrid.Background = new SolidColorBrush(0xFF648589);

        private void MinimizeButton_PointerLeave(object? sender, PointerEventArgs e) => MinimizeGrid.Background = Brushes.Transparent;

        private Bitmap? LoadPlayerPhoto(int index)
        {
            if (SaveFile == null)
                return null;
            using var memStream = new MemoryStream(SaveFile.GetPlayer(index).GetPhotoData());
            return new Bitmap(memStream);
        }
    }
}
