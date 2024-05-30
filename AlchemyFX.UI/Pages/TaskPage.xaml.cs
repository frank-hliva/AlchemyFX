using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
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

using AlchemyFX;
using AlchemyFX.Data;
using AlchemyFX.Data.Model;
using AlchemyFX.View;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using System.ComponentModel;
using Microsoft.FSharp.Core;
using System.Windows.Markup;
using AlchemyFX.UI.Editors;

namespace AlchemyFX.UI.Pages
{
    /// <summary>
    /// Interaction logic for TaskPage.xaml
    /// </summary>
    public partial class TaskPage : Page
    { 
        private readonly TaskViewModel taskViewModel;
        private readonly TaskMarkdownEditorViewModel taskMarkdownEditorViewModel;
        private readonly ProfileViewModel profileViewModel;

        private readonly AppConfig appConfig;
        private readonly WindowStorage windowStorage;

        private readonly StatusBarBuilder statusBarBuilder;
        private readonly GlobalCommandMediator globalCommands;

        public TaskPage(
            TaskViewModel taskViewModel,
            TaskMarkdownEditorViewModel taskMarkdownEditorViewModel,
            ProfileViewModel profileViewModel,
            AppConfig appConfig,
            WindowStorage windowStorage,
            ITheme theme,
            GlobalCommandMediator globalCommands
        )
        {
            this.taskViewModel = taskViewModel;
            this.appConfig = appConfig;
            this.globalCommands = globalCommands;
            this.taskMarkdownEditorViewModel = taskMarkdownEditorViewModel;
            this.profileViewModel = profileViewModel;

            this.DataContext = new
            {
                Task = taskViewModel,
                TaskMarkdownEditor = taskMarkdownEditorViewModel,
                Profile = profileViewModel
            };
            this.windowStorage = windowStorage;
            InitializeComponent();
            TaskTabs.SelectedIndex = Int32.Parse(windowStorage.GetValue("TaskTabs.SelectedIndex", "0"));
            TaskBodyTabs.SelectedIndex = Int32.Parse(windowStorage.GetValue("TaskBodyTabs.SelectedIndex", "0"));
            TaskBodyMarkdownEditor.Theme = FSharpOption<ITheme>.Some(theme);
            TaskBodyMarkdownEditor.RegisterTextProcessor(new TextEditorProcessor());
            var openSettingsButton = StatusBarBuilder.CreateOpenSettingsButton();
            openSettingsButton.Click += (sender, e) => globalCommands.TriggerCommand(GlobalCommands.ShowConfigDialog);

            statusBarBuilder = new StatusBarBuilder(statusBar).WithComponent(openSettingsButton);
            DisplayStatuses();
            (taskMarkdownEditorViewModel as INotifyPropertyChanged).PropertyChanged += TaskPage_PropertyChanged;
            (taskViewModel as INotifyPropertyChanged).PropertyChanged += TaskPage_PropertyChanged;
            globalCommands.SubscribeCommand(
                GlobalCommands.LoadTaskMarkdownEditorContent,
                () => {
                    this.taskMarkdownEditorViewModel.LoadDocumentFromString(
                        taskViewModel.TaskBody
                    );
                    NavigateTo(taskViewModel.TaskUrl);
                }
            );
            globalCommands.TriggerCommand(
                GlobalCommands.LoadTaskMarkdownEditorContent
            );
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            globalCommands.TriggerCommand(
                GlobalCommands.LoadTaskMarkdownEditorContent
            );
        }

        private void TaskPage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsModified":
                    UpdateStatus();
                    break;
            }
        }

        private void UpdateStatus()
        {
            DisplayStatuses();
            globalCommands.TriggerCommand(GlobalCommands.ChangeProfileUpdateStatus);
        }

        private void DisplayStatuses()
        {
            var profileViewModel = (taskViewModel as IProfileViewModelProvider).ProfileViewModel;
            statusBarBuilder["Profile"] = Status.ProfileViewModel.toProfileStatus(profileViewModel);
            statusBarBuilder["VM"] = Status.Location.toHostName(profileViewModel.MainUrl);
            statusBarBuilder["MD"] = taskMarkdownEditorViewModel.IsModified ? "Changed" : "Not changed";
            statusBarBuilder["Save"] = profileViewModel.IsModified ? "[CTRL]+[S]" : null;
        }

        private void TaskOpenButton_Click(object sender, RoutedEventArgs e)
        {
            Location.openInBrowser(
                RedmineTaskUrlTextBox.Text
            );
        }

        public bool TryToSaveChangesAsPrompted()
        {
            if (taskMarkdownEditorViewModel.IsModified)
            {
                switch (MessageBox.Show(
                    "Changes in the editor have not been saved.\nDo you want to save these changes?",
                    "Confirmation",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                ))
                {
                    case MessageBoxResult.Yes:
                        {
                            try
                            {
                                EnterProfileNameIfNeededAndSaveChanges();
                                return true;
                            }
                            catch (Exception ex)
                            {
                                return false;
                            }
                        }
                    case MessageBoxResult.No: return true;
                    default: return false;
                }
            }
            return true;
        }

        private void EnterProfileNameIfNeededAndSaveChanges()
        {
            if (string.IsNullOrEmpty(profileViewModel.ProfileName))
            {
                var inputDialog = new Dialogs.InputBox(new Dialogs.InputBox.Props() { 
                    Question = "The profile has not yet been entered\nPlease enter a name for the new profile:",
                    Title = "New profile name",
                    OkButtonText = "Create",
                });
                var profileName = (
                    inputDialog.ShowDialog() == true
                        ? inputDialog.Answer
                        : null
                );
                if (profileName != null)
                {
                    if (profileViewModel.IsValidProfileName(profileName))
                    {
                        profileViewModel.ProfileName = profileName;
                    }
                    else
                    {
                        MessageBox.Show(
                            "The entered profile name is not valid",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        throw new Exception("Profile name is not valid");
                    }
                }
            }
            taskViewModel.TaskBody = taskMarkdownEditorViewModel.SaveChanges();
            globalCommands.TriggerCommand(GlobalCommands.SaveTaskMarkdownEditorContent);
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            EnterProfileNameIfNeededAndSaveChanges();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                if (taskViewModel.IsModified)
                {
                    EnterProfileNameIfNeededAndSaveChanges();
                }
            }
        }

        private async Task NavigateTo(string url)
        {
            await RedmineTaskView.EnsureCoreWebView2Async();
            RedmineTaskView.CoreWebView2.Navigate(url);
        }

        private async void RedmineTaskUrlTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    {
                        await NavigateTo(taskViewModel.TaskUrl);
                        break;
                    }
            }
        }
        private void OpenTaskBodySection(TabItem section)
        {
            TaskBodyTabs.SelectedItem = section;
        }

        private void OpenTaskBodyEditor()
        {
            OpenTaskBodySection(BodyEditorSection);
        }

        private void FlowDocumentScrollViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenTaskBodyEditor();
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenTaskBodyEditor();
        }

        private void ViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenTaskBodySection(BodyViewerSection);
        }

        private void ViewXamlSourceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TaskBodyXamlEditor.TextSource = TaskBodyMarkdownEditor.Document.ExportToXaml();
            OpenTaskBodySection(TaskBodyXamlEditorSection);
        }

        private void TaskTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            windowStorage.SetValue("TaskTabs.SelectedIndex", TaskTabs.SelectedIndex.ToString());
        }

        private void TaskBodyTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            windowStorage.SetValue("TaskBodyTabs.SelectedIndex", TaskBodyTabs.SelectedIndex.ToString());
        }
    }
}
