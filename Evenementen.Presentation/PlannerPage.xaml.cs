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
    /// Interaction logic for PlannerPage.xaml
    /// </summary>
    public partial class PlannerPage : Page
    {
        public event EventHandler? BtnGoBackClicked;
        public event EventHandler? BtnRemoveClicked;

        public PlannerPage()
        {
            InitializeComponent();
        }

        private void Btn_GoBack_Click(object sender, RoutedEventArgs e)
        {
            BtnGoBackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void Btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            BtnRemoveClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
