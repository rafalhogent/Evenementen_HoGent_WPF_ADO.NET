using Microsoft.Win32;
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

namespace Evenementen.Presentation
{
    /// <summary>
    /// Interaction logic for ImportPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public event EventHandler? MappingStarted;
        public event EventHandler? GoBackClicked;

        public string ConnectionString { get; set; } = "";

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Btn_OpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var res = openFileDialog.FileName;
                Tbx_path.Text = res;
            }
        }

        private void Btn_StartMapping_Click(object sender, RoutedEventArgs e)
        {
            MappingStarted?.Invoke(this, EventArgs.Empty);
        }

        private void Btn_GoBack_Click(object sender, RoutedEventArgs e)
        {
            GoBackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}

