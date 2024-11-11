using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace FBoothApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class EmailSendDialog : Window
    {

        public EmailSendDialog(string question, string defaultAnswer = "")
        {
            InitializeComponent();
            textBoxEmailAnswer.Text = defaultAnswer;
            // Bật bàn phím ảo cho textBoxEmailAnswer
            
        }

        // Khi TextBox nhận focus
        private void textBoxEmailAnswer_GotFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TextBox GotFocus");

            // Mở bàn phím ảo 
            
        }

        // Khi TextBox mất focus
        private void textBoxEmailAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TextBox LostFocus");

            // Đóng bàn phím ảo nếu có
            
        }


        public static bool IsValidEmail(string email)
        {
            Regex rx = new Regex(
            @"^[-!#$%&'*+/0-9=?A-Z^_a-z{|}~](\.?[-!#$%&'*+/0-9=?A-Z^_a-z{|}~])*@[a-zA-Z](-?[a-zA-Z0-9])*(\.[a-zA-Z](-?[a-zA-Z0-9])*)+$");
            return rx.IsMatch(email);
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidEmail(Answer))
            {
                this.DialogResult = true;
                Debug.WriteLine("textBoxEmailAnswer is: " + Answer);
            }
            else
            {
                Report.Error("Wrong e-mail format \nPlease enter your e-mail correctly\nexample@mail.com", true);
            }
            
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            textBoxEmailAnswer.SelectAll();
            textBoxEmailAnswer.Focus();
        }

        private void textBoxEmailAnswer_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (IsValidEmail(textBoxEmailAnswer.Text))
            {
                btnDialogOk.IsEnabled = true;
                btnDialogOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }
            else
            {
                btnDialogOk.IsEnabled = false;
                btnDialogOk.Background = new SolidColorBrush(Colors.Gray);
            }
        }

        public string Answer
        {
            get { return textBoxEmailAnswer.Text; }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
