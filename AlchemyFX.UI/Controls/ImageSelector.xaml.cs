using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using AlchemyFX;
using AlchemyFX.UI;
using AlchemyFX.UI.ImageSelector;
using Newtonsoft.Json.Linq;

namespace AlchemyFX.UI.Controls
{
    public class ChoiceItem : INotifyPropertyChanged
    {
        public Choice Data { get; private set; }
        public ImageSelector ImageSelector { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isSelected;

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public ChoiceItem(Choice data)
        {
            Data = data;
        }
    }

    public partial class ImageSelector : UserControl
    {
        public ImageSelector()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent ChoiceChangedEvent =
            EventManager.RegisterRoutedEvent(
                "ChoiceChanged",
                RoutingStrategy.Bubble,
                typeof(EventHandler<ChoiceChangedEventArgs>),
                typeof(ImageSelector)
            );

        public readonly object objectLock = new object();

        public event RoutedEventHandler ChoiceChanged
        {
            add { lock (objectLock) { this.AddHandler(ChoiceChangedEvent, value); } }
            remove { lock (objectLock) { this.RemoveHandler(ChoiceChangedEvent, value); } }
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IEnumerable<object>),
            typeof(ImageSelector),
            new FrameworkPropertyMetadata(
                new ObservableCollection<object> {},
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        public IEnumerable<object>? ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable<object>; }
            set {
                Items = new ObservableCollection<ChoiceItem>(
                    value?.Select(choice => new ChoiceItem(choice as Choice))
                );
                SetValue(ItemsSourceProperty, value);
            }
        }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items",
            typeof(IEnumerable<ChoiceItem>),
            typeof(ImageSelector),
            new FrameworkPropertyMetadata(
                new ObservableCollection<ChoiceItem> {},
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        public IEnumerable<ChoiceItem>? Items
        {
            get { return GetValue(ItemsProperty) as IEnumerable<ChoiceItem>; }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(Choice),
            typeof(ImageSelector),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        public Choice? SelectedItem
        {
            get { return (Choice)GetValue(SelectedItemProperty); }
            set
            {
                var oldItem = (Choice)GetValue(SelectedItemProperty);
                ChoiceItem? newItem = null;
                if (value == null)
                {
                    DeselectAll();
                }
                else
                {
                    newItem = SelectChoiceItemById(value.Id);
                }
                SetValue(SelectedItemProperty, value);
                this.RaiseEvent(
                    new ChoiceChangedEventArgs(
                        ChoiceChangedEvent,
                        this,
                        FindChoiceItemByChoice(oldItem),
                        newItem
                    )
                );
            }
        }

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex",
            typeof(int),
            typeof(ImageSelector),
            new FrameworkPropertyMetadata(
                -1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set
            {
                SetValue(SelectedIndexProperty, value);
                SelectedItem = FindChoiceItemByIndex(value)?.Data;
            }
        }

        private void DeselectAll()
        {
            Items.ForEach(choiceItem => choiceItem.IsSelected = false);
        }

        private ChoiceItem? SelectChoiceItemBy(Func<ChoiceItem, bool> predicate)
        {
            DeselectAll();
            var choiceItem = Items?.Where(predicate).FirstOrDefault();
            if (choiceItem == null)
            {
                return null;
            }
            else
            {
                choiceItem.IsSelected = true;
                return choiceItem;
            }
        }

        private ChoiceItem? FindChoiceItemByChoice(Choice item)
        {
            return Items.FirstOrDefault(choiceItem => choiceItem.Data.Id == item?.Id);
        }

        private ChoiceItem? SelectChoiceItemById(Guid id)
        {
            return SelectChoiceItemBy(choiceItem => choiceItem.Data.Id == id);
        }

        private ChoiceItem? FindChoiceItemByIndex(int index)
        {
            return Items.ElementAtOrDefault(index);
        }

        private int FindIndexByChoice(Choice? choice)
        {
            var index = 0;
            foreach (var choiceItem in Items)
            {
                if (choiceItem.Data.Id == choice?.Id)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }



        private void SelectItem(object sender, MouseButtonEventArgs e)
        {
            var currentChoiceItem = (sender as FrameworkElement).DataContext as ChoiceItem;
            if (currentChoiceItem == null)
            {
                DeselectAll();
                SelectedItem = null;
            }
            else
            {
                SelectedIndex = FindIndexByChoice(currentChoiceItem.Data);
            }
        }
    }

    public class ChoiceChangedEventArgs : RoutedEventArgs
    {
        public ChoiceItem? OldItem { get; }
        public ChoiceItem? NewItem { get; }
        public ChoiceChangedEventArgs(
            RoutedEvent routedEvent,
            object source,
            ChoiceItem? oldItem,
            ChoiceItem? newItem
        ) : base(routedEvent, source)
        {
            OldItem = oldItem;
            NewItem = newItem;
        }
    }
}
