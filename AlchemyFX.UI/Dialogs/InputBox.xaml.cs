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
using System.Windows.Shapes;

namespace AlchemyFX.UI.Dialogs
{
	public partial class InputBox : Window
    {
        public class Props
        {
            public string? Question { get; set; }
            public string? Title { get; set; }
            public string? DefaultAnswer { get; set; }
            public string? OkButtonText { get; set; }
            public string? CancelButtonText { get; set; }

            public Props()
            {
                Question = "";
                Title = "";
                DefaultAnswer = "";
                OkButtonText = "OK";
                CancelButtonText = "Cancel";
            }
        }
        public InputBox(Props props)
        {
            InitializeComponent();
            QuestionLabel.Content = props.Question;
            Title = props.Title;
            AnswerTextBox.Text = props.DefaultAnswer;
            OKButtonText.Text = props.OkButtonText;
            CancelButtonText.Text = props.CancelButtonText;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            AnswerTextBox.SelectAll();
            AnswerTextBox.Focus();
        }

        public string Answer
        {
            get { return AnswerTextBox.Text; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void AnswerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OKButton != null)
            {
                OKButton.IsEnabled = !String.IsNullOrWhiteSpace(AnswerTextBox.Text);
            }
        }
    }
}
