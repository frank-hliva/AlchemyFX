using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using LiteDB;

using AlchemyFX;
using AlchemyFX.Data;
using AlchemyFX.Data.Model;

using AlchemyFX.View;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using AlchemyFX.UI;

namespace AlchemyFX.UI.Pages
{
    /// <summary>
    /// Interaction logic for ProfilesPage.xaml
    /// </summary>
    public partial class ProfilesPage : Page
    {
        private readonly AppConfig appConfig;
        private readonly ProfileViewModel profileViewModel;
        private readonly StatusBarBuilder statusBarBuilder;
        private readonly GlobalCommandMediator globalCommands;

        public ProfilesPage(
            AppConfig appConfig,
            ProfileViewModel profileViewModel,
            GlobalCommandMediator globalCommands
        )
        {
            this.profileViewModel = profileViewModel;
            (this.profileViewModel as INotifyPropertyChanged).PropertyChanged += ProfileService_PropertyChanged;
            this.appConfig = appConfig;
            this.globalCommands = globalCommands;
            InitializeComponent();
            var openSettingsButton = StatusBarBuilder.CreateOpenSettingsButton();

            openSettingsButton.Click += (sender, e) =>
                globalCommands.TriggerCommand(
                    GlobalCommands.ShowConfigDialog
                );

            globalCommands.SubscribeCommand(
                GlobalCommands.ChangeProfileUpdateStatus,
                ChangeProfileUpdateStatus
            );

            statusBarBuilder = new StatusBarBuilder(statusBar)
                .WithComponent(openSettingsButton);

            DisplayStatuses();
            globalCommands.TriggerCommand(GlobalCommands.ChangeProfileUpdateStatus);
        }

        void InitializeProfiles()
        {
            DataContext = profileViewModel;
            ProfilesListBox.SelectedItem = profileViewModel.CurrentProfile;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeProfiles();
        }

        private void ChangeProfileUpdateStatus()
        {
            var isModified = profileViewModel.IsModified;
            SaveChangesButton.IsEnabled = isModified;
            ProfileNameTextBox.Background = GetProfileNameTextBoxBackground(
                IsProfileNameTextBoxChanged,
                isModified
            );
            statusBarBuilder["Content"] = (
                IsProfileNameTextBoxChanged && (isModified || !profileViewModel.IsEditable)
                    ? "Changed"
                    : "Not changed"
            );

        }

        private bool IsProfileNameTextBoxChanged
        {
            get
            {
                return profileViewModel.ProfileName != profileViewModel.CurrentProfile.Name;
            }
        }

        private Brush GetProfileNameTextBoxBackground(bool isProfileNameTextBoxChanged, bool isModified)
        {
            return profileViewModel.IsEditable && isProfileNameTextBoxChanged && !isModified
                ? HexColor.toBrush("#FFF4F2")
                : Brushes.LightYellow;
        }

        private void DisplayStatuses()
        {
            statusBarBuilder["Profile"] = Status.ProfileViewModel.toProfileStatus(profileViewModel);
            statusBarBuilder["VM"] = Status.Location.toHostName(profileViewModel.MainUrl);
            statusBarBuilder["Mode"] = ProccessingModeModule.toDisplayableString(this.profileViewModel.Mode);
            statusBarBuilder["Save"] = SaveChangesButton.IsEnabled ? "[CTRL]+[S]" : "-";
        }

        private void ProfileService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentProfile":
                    if (this.profileViewModel.Mode != ProccessingMode.Edit)
                    {
                        ProfilesListBox.UnselectAll();
                    }
                    DisplayStatuses();
                    break;
                case "Profiles":
                    DisplayStatuses();
                    break;
            }
        }

        ObjectId? GetCurrentProfileId
        {
            get
            {
                var profiles = this.profileViewModel?.Profiles;
                try
                {
                    return profiles.First().Id;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = ProfilesListBox.SelectedItem as Profile;
            if (selectedProfile != null)
            {
                profileViewModel.Edit(selectedProfile);
                ProfileNameTextBox.Focus();
            } else
            {
                ProfilesListBox.UnselectAll();
            }
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            profileViewModel.CreateNew();
            ProfileNameTextBox.Focus();
            globalCommands.TriggerCommand(GlobalCommands.ChangeProfileUpdateStatus);
        }

        private void SaveChanges()
        {
            try
            {
                profileViewModel.SaveChanges();
                ProfilesListBox.SelectedItem = profileViewModel.CurrentProfile;
            }
            catch (KeyNotFoundException ex)
            {
                ProfilesListBox.SelectedItem = null;
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(
                    "Do you really want to delete this profile?",
                    "Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                ) == MessageBoxResult.Yes
            )
            {
                profileViewModel.Delete();
            }
        }

        private void ProfileNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.CaretIndex = textBox.Text.Length;
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            profileViewModel.Close();
        }

        private void ProfileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChangeProfileUpdateStatus();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                if (SaveChangesButton.IsEnabled)
                {
                    SaveChanges();
                    e.Handled = true;
                }
            }
        }

        private void ProfilesListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            globalCommands.TriggerCommand(GlobalCommands.OpenTaskPage);
        }
    }
}
