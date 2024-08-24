using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Xml;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace FBoothApp
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
   
    public partial class Menu : Page
    {
        List<string> printerList = new List<string>();
        List<int> copiesCount = new List<int>();

        UserSettings userSettings = new UserSettings();
        XDocument settings = new XDocument();
        XDocument userSettingXDoc = new XDocument();
        private string currentDirectory = Environment.CurrentDirectory;
        List<string> variables = new List<string>();


        public Menu()
        {
            InitializeComponent();
            FillList();
            FillComboBox();
        }
        public void FillList()
        {

            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                printerList.Add(printer);
            }

            for(int i=1;i<6;i++)
            {
                copiesCount.Add(i);
            }
        }
        public void FillComboBox()
        {
            PrinterComboBox.ItemsSource = printerList;
            Printer2ComboBox.ItemsSource = printerList;
            variables.Add("BoothID");  // Thêm BoothID vào ComboBox
            LoadDefaultValues();
        }
        public void LoadDefaultValues()
        {
            try
            {
                settings = XDocument.Load(Path.Combine(currentDirectory, "menusettings.xml"));
                settings.Root.Elements("setting");
                PrinterComboBox.SelectedValue = settings.Root.Element("actualPrinter").Value;
                Printer2ComboBox.SelectedValue = settings.Root.Element("secondPrinter").Value;
                TimeBetweenPhotosTextBox.Text = settings.Root.Element("timeBetweenPhotos").Value; // Load giá trị vào TextBox
                ChangeEmailAddressTextBox.Text = settings.Root.Element("EmailHostAddress").Value;
                ChangeEmailPasswordTextBox.Text = settings.Root.Element("EmailHostPassword").Value;
                ChangeEmailServerTextBox.Text = settings.Root.Element("SmtpServerName").Value;
                ChangeEmailPortTextBox.Text = settings.Root.Element("SmtpPortNumber").Value;
                BoothIDTextBox.Text = settings.Root.Element("BoothID").Value;
            }
            catch (XmlException e)
            {
                Debug.WriteLine("LoadDefaultValues exception");
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine("missing settings in menusettings.xml");
            }
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save();  // Gọi phương thức Save để lưu các thay đổi
                MessageBox.Show("Settings saved successfully!");  // Thông báo khi lưu thành công
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving settings: " + ex.Message);  // Hiển thị thông báo lỗi nếu có
            }
        }

        private void Save()
        {
            try
            {
                var savedata = new MenuData();
                savedata.FillValues(
                    PrinterComboBox.SelectedValue.ToString(),
                    TimeBetweenPhotosTextBox.Text, // Lưu giá trị từ TextBox
                    Printer2ComboBox.SelectedValue.ToString(),
                    ChangeEmailAddressTextBox.Text,
                    ChangeEmailPasswordTextBox.Text,
                    ChangeEmailServerTextBox.Text,
                    ChangeEmailPortTextBox.Text,
                    BoothIDTextBox.Text
                );

                // Lưu toàn bộ cài đặt vào tệp XML
                settings.Root.Element("BoothID").Value = BoothIDTextBox.Text;
                settings.Root.Element("timeBetweenPhotos").Value = TimeBetweenPhotosTextBox.Text; // Lưu giá trị từ TextBox vào XML
                settings.Save(Path.Combine(currentDirectory, "menusettings.xml"));

                MessageBox.Show("Settings saved successfully!");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving settings: " + e.Message);
            }
        }




        private void WithoutSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        //private void FillDefaultValue()
        //{            
        //    userSettings.SetNewUserSettings("Hello", "Prepare for first photo", "Get ready for second one", "Third photo coming, prepare!", "the end",
        //       "backgrounds", "backgrounds", "red", "orange", "orange", "blue");
        //    userSettings.SaveOptions("ble");
        //}
    }
}
