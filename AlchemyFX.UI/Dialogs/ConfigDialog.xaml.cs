using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.FSharp.Core;
using Microsoft.Win32;
using LiteDB;
using LiteDB.FSharp;
using static LiteDB.FSharp.Extensions;
using AlchemyFX;
using AlchemyFX.Data;
using static AlchemyFX.WSL;
using System.Collections.ObjectModel;
using AlchemyFX.UI.Controls;

namespace AlchemyFX.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for ConfigDialog.xaml
    /// </summary>
    public partial class ConfigDialog : Window
    {
        bool IsChangedBuildPath
        {
            get
            {
                return appConfig.BuildPath != BuildDirectoryTextBox.Text;
            }
        }

        private readonly MainWindow mainWindow;
        private readonly AppConfig appConfig;
        private readonly WSL.Path wslPath;

        public class PackageManager
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        private ObservableCollection<ImageSelector.Choice> ImageSelectorChoices = new ObservableCollection<ImageSelector.Choice> {
            new ImageSelector.Choice() {
                Image = "pack://application:,,,/Images/resizable_window.svg",
                Name = "Full Resizable Window" 
            },
            new ImageSelector.Choice() {
                Image = "pack://application:,,,/Images/tool_window.svg",
                Name = "Tool window (Widget)"
            },
        };

        PackageManager[] packageManagers = new PackageManager[] {
            new PackageManager() { Label = "npm (Default)", Value = "npm" },
            new PackageManager() { Label = "pnpm (Recommended)", Value = "pnpm" },
            new PackageManager() { Label = "yarn", Value = "yarn" },
            new PackageManager() { Label = "bun", Value = "bun" }
        };

        public ConfigDialog(
            MainWindow mainWindow,
            AppConfig appConfig,
            WSL.Path wslPath
        )
        {
            this.appConfig = appConfig;
            this.wslPath = wslPath;
            this.mainWindow = mainWindow;

            InitializeComponent();

            Cursor = Cursors.AppStarting;

            BuildDirectoryTextBox.Text = appConfig.BuildPath;

            PackageManagerComboBox.ItemsSource = packageManagers;
            PackageManagerComboBox.SelectedValue = appConfig.PackageManager;

            var distros = WSL.getDistros().ToArray();
            DistrosComboBox.ItemsSource = distros;
            DistrosComboBox.SelectedItem = FindSelectedDistro(distros);

            Cursor = Cursors.Arrow;
            StayOnTopCheckBox.IsChecked = appConfig.StayOnTop;
            AppStyleSelector.ItemsSource = ImageSelectorChoices;
            AppStyleSelector.SelectedIndex = (int)appConfig.AppStyle;
        }

        private DistroInfo? FindSelectedDistro(WSL.DistroInfo[] distros)
        {
            var lastSavedDistro = distros.FirstOrDefault(distro => distro.Name == appConfig.WSLDistro);
            return lastSavedDistro == null ? distros.FirstOrDefault(distro => distro.IsDefault) : lastSavedDistro;
        }

        private string SelectedDistro
        {
            get
            {
                return (string)DistrosComboBox.SelectedValue;
            }
        }

        private void DisableTopmostForAction(Action action)
        {
            var currentTopmost = Topmost;
            var currentWindowTopmost = mainWindow.Topmost;
            mainWindow.Topmost = false;
            Topmost = false;
            action();
            mainWindow.Topmost = currentWindowTopmost;
            Topmost = currentTopmost;
        }

        private void SaveBuildPath()
        {
            var buildPath = BuildDirectoryTextBox.Text;
            if (!String.IsNullOrEmpty(buildPath) && wslPath.SetDistro(SelectedDistro).Exists(buildPath))
            {
                appConfig.BuildPath = buildPath;
                Cursor = Cursors.Arrow;
                DialogResult = true;
            }
            else
            {
                Cursor = Cursors.Arrow;
                SaveButton.IsChecked = false;
                SaveButton.IsEnabled = true;
                DisableTopmostForAction(() =>
                {
                    MessageBox.Show(
                        $"Directory \"{buildPath}\" does not exist",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            SaveButton.IsChecked = true;
            SaveButton.IsEnabled = false;
            
            appConfig.WSLDistro = SelectedDistro;
            appConfig.PackageManager = (string)PackageManagerComboBox.SelectedValue;
            mainWindow.AppStyle = (AppStyles)AppStyleSelector.SelectedIndex;
            mainWindow.StayOnTop = StayOnTopCheckBox.IsChecked.Value;
            if (IsChangedBuildPath)
            {
                SaveBuildPath();
            }
            else
            {
                Cursor = Cursors.Arrow;
                DialogResult = true;
            }
            SaveButton.IsChecked = false;
            SaveButton.IsEnabled = true;
        }

        private void AppStyleSelector_ChoiceChanged(object sender, ChoiceChangedEventArgs e)
        {
            StayOnTopCheckBox.IsEnabled = (AppStyles)AppStyleSelector.SelectedIndex == AppStyles.ToolWindow;
        }
    }
}
