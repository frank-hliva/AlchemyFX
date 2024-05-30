using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Win32;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Diagnostics.Tracing;
using static Microsoft.FSharp.Core.ByRefKinds;
using System.Text.RegularExpressions;
using Unity;
using System.Windows.Controls.Primitives;

using AlchemyFX;
using AlchemyFX.UI;
using AlchemyFX.Data;
using AlchemyFX.Utils;
using AlchemyFX.View;

using AlchemyFX.UI.Dialogs;
using AlchemyFX.UI.Pages;
using System.ComponentModel;
using static AlchemyFX.WSL;
using System.Windows.Automation.Text;
using System.Xml.Linq;

namespace AlchemyFX.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AlchemyFX.App.Context appContext;
        private readonly AppConfig appConfig;
        private readonly IBasicStorage windowStorage;
        private readonly LocationBuilder locationBuilder;
        private readonly WSL.Command wslCommand;
        private readonly WSL.Path wslPath;
        private readonly StatusBarBuilder statusBarBuilder;
        private readonly TaskProfile taskProfile;

        private readonly ApplicationViewModel applicationViewModel;
        private readonly ProfileViewModel profileViewModel;

        private readonly TaskPage taskPage;
        private readonly ProfilesPage profilesPage;

        private readonly GlobalCommandMediator globalCommands;

        private static readonly ResourceDictionary resourceDictionary = new ResourceDictionary {
            Source = new Uri("Resources/Resources.xaml", UriKind.Relative)
        };
        private readonly BitmapImage START_ICON = resourceDictionary["StartIcon"] as BitmapImage;
        private readonly BitmapImage STOP_ICON = resourceDictionary["StopIcon"] as BitmapImage;

        private readonly double DefaultWidth;
        private readonly double DefaultHeight;

        private readonly IEnumerable<NamedViewerChoice> viewerChoices;

        static BitmapImage resourceToBitmap(string uri)
        { 
            return new BitmapImage(new Uri(uri));
        }

        public MainWindow(
            AlchemyFX.App.Context appContext,
            AppConfig appConfig,

            WindowStorage windowStorage,
            LocationBuilder locationBuilder,

            WSL.Command wslCommand,
            WSL.Path wslPath,

            TaskProfile taskProfile,

            TaskPage taskPage,
            ProfilesPage profilesPage,
            ApplicationViewModel applicationViewModel,
            ProfileViewModel profileViewModel,
            GlobalCommandMediator globalCommands
        )
        {
            this.appContext = appContext;
            this.appConfig = appConfig;

            this.windowStorage = windowStorage;
            this.locationBuilder = locationBuilder;

            this.wslCommand = wslCommand;
            this.wslPath = wslPath;

            this.taskProfile = taskProfile;
            this.applicationViewModel = applicationViewModel;
            this.profileViewModel = profileViewModel;
            this.globalCommands = globalCommands;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            viewerChoices = Locations.ViewerChoices;
            InitializeComponent();
            PageTabs.Items.CurrentChanging += PageTabs_CurrentTabChanging;
            DefaultWidth = Width;
            DefaultHeight = Height;
            this.globalCommands.SubscribeCommand(
                GlobalCommands.ShowConfigDialog,
                ShowConfigDialog
            );
            this.globalCommands.SubscribeCommand(
                GlobalCommands.OpenTaskPage,
                () => PageTabs.SelectedItem = TaskTab
            );
            var profileNotifyPropertyChanged = profileViewModel as INotifyPropertyChanged;
            profileNotifyPropertyChanged.PropertyChanged += ProfileService_PropertyChanged;
            DataContext = new {
                App = applicationViewModel,
                Profile = profileViewModel,
                ViewerChoices = viewerChoices
            };
            TaskFrame.Content = this.taskPage = taskPage;
            ProfilesFrame.Content = this.profilesPage = profilesPage;

            var openSettingsButton = StatusBarBuilder.CreateOpenSettingsButton();
            openSettingsButton.Click += (sender, e) => this.globalCommands.TriggerCommand(GlobalCommands.ShowConfigDialog);
            statusBarBuilder = new StatusBarBuilder(statusBar).WithComponent(openSettingsButton);
            DisplayStatuses(); 

            StayOnTop = appConfig.StayOnTop;
            AppStyle = appConfig.AppStyle;
            var viewerComboBoxSelectedIndex = windowStorage.GetValue("ViewerComboBox.SelectedIndex");
            ViewerSelectorComboBox.SelectedIndex = (
                viewerComboBoxSelectedIndex == null
                    ? 0
                    : Int32.Parse(viewerComboBoxSelectedIndex)
            );
            UpdateWSLCommandStatus();
            try
            {
                Left = Double.Parse(windowStorage.GetValue("x"));
                Top = Double.Parse(windowStorage.GetValue("y"));
                if (appConfig.AppStyle == AppStyles.ResizableWindow)
                {
                    Width = Double.Parse(windowStorage.GetValue("Width"));
                    Height = Double.Parse(windowStorage.GetValue("Height"));
                    WindowState = (WindowState)Int32.Parse(windowStorage.GetValue("WindowState"));
                }
                var selectedTab = windowStorage.GetValue("SelectedTab");
                PageTabs.SelectedIndex = selectedTab == null ? 0 : Int32.Parse(selectedTab);
            }
            catch (Exception)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (appConfig.IsFirstStart)
            {
                ShowConfigDialog();
            }
        }

        void PageTabs_CurrentTabChanging(object sender, System.ComponentModel.CurrentChangingEventArgs e)
        {
            var toElement = PageTabs.SelectedItem as FrameworkElement;
            if (e.IsCancelable)
            {
                var currentTab = (sender as ICollectionView).CurrentItem as FrameworkElement;
                if (currentTab?.Name == "TaskTab")
                {
                    if (!taskPage.TryToSaveChangesAsPrompted())
                    {
                        PageTabs.SelectedItem = currentTab;
                        e.Cancel = true;
                        return;
                    }
                }
            }
            PageTabs.SelectedItem = toElement;
            e.Cancel = false;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            StopWSLCommand();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (localhostWSLRunningCommand != null)
            {
                StopWSLCommand();
            }
            try
            {
                if (AppStyle == AppStyles.ResizableWindow)
                {
                    windowStorage.SetValue("Width", Width.ToString());
                    windowStorage.SetValue("Height", Height.ToString());
                    windowStorage.SetValue("WindowState", ((int)WindowState).ToString());
                }
                windowStorage.SetValue("x", Left.ToString());
                windowStorage.SetValue("y", Top.ToString());
                windowStorage.SetValue("ViewerComboBox.SelectedIndex", ViewerSelectorComboBox.SelectedIndex.ToString());
            }
            catch (Exception ex)
            {
            }
        }

        private void ProfileService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentProfile":
                case "Profiles":
                    DisplayStatuses();
                    break;
            }
        }

        public bool StayOnTop
        {
            get {
                return appConfig.StayOnTop;
            }

            set {
                appConfig.StayOnTop = value;
                Topmost = appConfig.AppStyle == AppStyles.ResizableWindow ? false : value;
            }
        }

        public AppStyles AppStyle
        {
            get
            {
                return appConfig.AppStyle;
            }

            set
            {
                appConfig.AppStyle = value;
                switch (value)
                {
                    case AppStyles.ResizableWindow:
                        ResizeMode = ResizeMode.CanResize;
                        WindowStyle = WindowStyle.SingleBorderWindow;
                        break;
                    case AppStyles.ToolWindow:
                        ResizeMode = ResizeMode.NoResize;
                        WindowStyle = WindowStyle.SingleBorderWindow;
                        Width = DefaultWidth;
                        Height = DefaultHeight;
                        WindowState = WindowState.Normal;
                        break;
                }
            }
        }

        private void UpdateTaskLocations()
        {
            try
            {
                var generatedLocations = locationBuilder.GetAllLocationsFor(sourceUrl: UrlTextBox.Text);
                LocalhostTextBox.Text = generatedLocations.Localhost.ToString();
                SpaceTextBox.Text = generatedLocations.Space.ToString();
                LocalhostCommandTextBox.Text = generatedLocations.RunLocalhostCommand;
                if (ViewerSelectorComboBox.SelectedItem is NamedViewerChoice)
                {
                    var selectedViewer = ViewerSelectorComboBox.SelectedItem as NamedViewerChoice;
                    ViewerTextBox.Text = generatedLocations.ViewByChoice(selectedViewer.Id);
                }
                else
                {
                    ViewerTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                LocalhostTextBox.Text = "";
                SpaceTextBox.Text = "";
                LocalhostCommandTextBox.Text = "";
                ViewerTextBox.Text = "";
            }
        }

        private void DisplayStatuses()
        {
            statusBarBuilder["Profile"] = Status.ProfileViewModel.toProfileStatus(profileViewModel);
            statusBarBuilder["VM"] = Status.Location.toHostName(profileViewModel.MainUrl);
            statusBarBuilder["WSL"] = appConfig.WSLDistro;
            statusBarBuilder["PM"] = appConfig.PackageManager;
            statusBarBuilder["Local dev"] = CommandStatusToString(CommandStatus);
        }

        private bool IsStartedWSLCommand { get { return localhostWSLRunningCommand != null; } }

        private void Log(string output, Brush? color = null, string heading = "MIS", Brush? headingColor = null)
        {
            if (output != null && output != "")
            {
                new Inline[]
                {
                    new Run(DateTime.Now.ToString("HH:mm:ss")) { Foreground = Brushes.Gray },
                    new Run(" ["),
                    new Run(heading) { Foreground = headingColor ?? Brushes.LightGreen },
                    new Run("] "),
                    new Run(output) { Foreground = color ?? Brushes.LightGray },
                    new LineBreak()
                }.ForEach(ConsoleOutputTextBox.Inlines.Add);
            }
        }

        private void UpdateWSLCommandStatus(bool localhostScriptRunning = false)
        {
            var isStartedWSL = IsStartedWSLCommand;
            StartLocalhostCommandIcon.Source = isStartedWSL ? STOP_ICON : START_ICON;

            isStartedWSL = isStartedWSL && localhostScriptRunning;

            var isEmptyLocalhostCommandTextBox = String.IsNullOrWhiteSpace(LocalhostCommandTextBox.Text);

            UrlOpenButton.IsEnabled = !isEmptyLocalhostCommandTextBox;

            StartLocalhostCommandButton.IsChecked = isStartedWSL;
            StartLocalhostCommandButton.IsEnabled = !isEmptyLocalhostCommandTextBox;

            var isEnabledLocalhostOpenButton = !String.IsNullOrWhiteSpace(LocalhostTextBox.Text) && isStartedWSL;
            LocalhostOpenButton.IsEnabled = isEnabledLocalhostOpenButton;

            var isEnabledSpaceOpenButton = !String.IsNullOrWhiteSpace(SpaceTextBox.Text) && isStartedWSL;
            SpaceOpenButton.IsEnabled = isEnabledSpaceOpenButton;
            StartLocalhostCommandButton.ToolTip = isStartedWSL ? "Stop" : "Start";
        }

        private void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTaskLocations();
        }

        private void UrlOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (UrlTextBox.Text != "")
            {
                Location.openInBrowser(UrlTextBox.Text);
            }
        }

        private void LocalhostOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocalhostTextBox.Text != "")
            {
                Location.openInBrowser(LocalhostTextBox.Text);
            }
        }

        private void SpaceOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpaceTextBox.Text != "")
            {
                Location.openInBrowser(SpaceTextBox.Text);
            }
        }

        private void CopyLocalCommandButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(LocalhostCommandTextBox.Text);
        }

        const string START = "Start";
        const string STOP = "Stop";

        private AlchemyFX.WSL.RunningCommand? localhostWSLRunningCommand = null;

        private void StopWSLCommand()
        {
            if (IsStartedWSLCommand && localhostWSLRunningCommand != null)
            {
                localhostWSLRunningCommand.Kill();
                localhostWSLRunningCommand = null;
            }
        }

        enum WSLCommandStatus {
            Nothing,
            Starting,
            Started,
            Stopping,
            Stopped
        }

        string CommandStatusToString(WSLCommandStatus commandStatus)
        {
            switch (CommandStatus)
            {
                case WSLCommandStatus.Starting: return "STARTING...";
                case WSLCommandStatus.Started: return "STARTED";
                case WSLCommandStatus.Stopping: return "STOPPING...";
                case WSLCommandStatus.Stopped: return "STOPPED";
                default: return "NOT STARTED";
            }
        }

        WSLCommandStatus commandStatus = WSLCommandStatus.Nothing;

        WSLCommandStatus CommandStatus
        {
            get { return commandStatus; }
            set
            {
                commandStatus = value;
                DisplayStatuses();
            }
        }

        Microsoft.FSharp.Control.FSharpHandler<CommandDataReceivedEventArgs> outputDataReceivedHandler = null;

        private readonly Lazy<Brush?> ConsoleFontDefaultBrush = new Lazy<Brush?>(() => 
            System.Windows.Application.Current.TryFindResource("DarkFontBrush") as SolidColorBrush
        );

        private readonly Lazy<Brush?> ErrorFuchsiaBrush = new Lazy<Brush?>(() =>
            System.Windows.Application.Current.TryFindResource("ErrorFuchsiaBrush") as SolidColorBrush
        );

        private readonly Lazy<Brush?> ErrorLightRedBrush = new Lazy<Brush?>(() =>
            System.Windows.Application.Current.TryFindResource("ErrorLightRedBrush") as SolidColorBrush
        );

        private void StartLocalhostCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsStartedWSLCommand)
            {
                CommandStatus = WSLCommandStatus.Stopping;
                Log("Stopping WSL...");
                StopWSLCommand();
                Log("WSL localhost Stopped.", color: ConsoleFontDefaultBrush.Value);
                CommandStatus = WSLCommandStatus.Stopped;
            }
            else
            {
                CommandStatus = WSLCommandStatus.Starting;
                var endOfOutputRegex = new Regex("press h \\+ enter to show help");
                Log("Starting WSL...", color: ConsoleFontDefaultBrush.Value);

                if (outputDataReceivedHandler != null)
                {
                    wslCommand.OutputDataReceived.RemoveHandler(outputDataReceivedHandler);
                }
                outputDataReceivedHandler = (sender, eventArgs) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var receivedData = StandardOutput.clean(eventArgs.Data);
                        switch (eventArgs.OutputType)
                        {
                            case OutputType.ErrorData:
                                Log(receivedData, color: ErrorLightRedBrush.Value, heading: "ERROR", headingColor: ErrorFuchsiaBrush.Value);
                                break;
                            default:
                                Log(receivedData, heading: "WSL");
                                break;
                        }
                        CommandLineScrollViewer.ScrollToBottom();
                        if (receivedData != null && endOfOutputRegex.Match(receivedData).Success)
                        {
                            UpdateWSLCommandStatus(localhostScriptRunning: true);
                            Log("WSL localhost Started.", color: ConsoleFontDefaultBrush.Value);
                            CommandStatus = WSLCommandStatus.Started;
                        }
                    });
                };
                wslCommand.OutputDataReceived.AddHandler(outputDataReceivedHandler);
                localhostWSLRunningCommand = wslCommand.Start(LocalhostCommandTextBox.Text);
            }
            UpdateWSLCommandStatus();
        }

        void ShowConfigDialog()
        {
            var configDialog = new ConfigDialog(this, appConfig, wslPath);
            switch (configDialog.ShowDialog())
            {
                case true:
                    DisplayStatuses();
                    UpdateTaskLocations();
                    break;
                default:
                    break;
            }
        }

        private void LocalhostCommandTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateWSLCommandStatus(LocalhostTextBox.IsEnabled);
        }

        private void ClearCommandOutput_Click(object sender, RoutedEventArgs e)
        {
            ConsoleOutputTextBox.Inlines.Clear();
        }

        private void SaveCommandOutputToFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "Text files (*.log)|*.log|All files (*.*)|*.*",
                FilterIndex = 1,
                FileName = "Alchemy.log"
            };
            switch (saveFileDialog.ShowDialog(this))
            {
                case true:
                    File.WriteAllText(saveFileDialog.FileName, ConsoleOutputTextBox.Text);
                    break;
                default:
                    break;
            }
        }

        private void PageTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            windowStorage.SetValue("SelectedTab", PageTabs.SelectedIndex.ToString());
        }

        private void ViewerSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTaskLocations();
        }

        private void CopyViewerContentButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ViewerTextBox.Text);
        }
    }
}