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
    /// Interaction logic for OverviewPage.xaml
    /// </summary>
    public partial class OverviewPage : Page
    {
        public event EventHandler? SettingsBtnClicked;
        public event EventHandler? PlannerBtnClicked;
        public event EventHandler? UpBtnClicked;
        public event EventHandler<string>? EvenementSelected;
        public event EventHandler<string>? FindCicked;
        public event EventHandler<string>? AddBtnClicked;

        public OverviewPage()
        {
            InitializeComponent();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsBtnClicked?.Invoke(this, EventArgs.Empty);
        }

        private void LsbEvenementen_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is KeyValuePair<string,string> evn)
            {
                var si = listBox.SelectedItem;
                EvenementSelected?.Invoke(this, evn.Key);
            }
        }

        private void Btn_Up_Click(object sender, RoutedEventArgs e)
        {
            UpBtnClicked?.Invoke(this, EventArgs.Empty);
        }

        private void Btn_Find_Click(object sender, RoutedEventArgs e)
        {
            FindCicked?.Invoke(this, TxbSearch.Text);
        }

        private void Btn_AddToPlanner_Click(object sender, RoutedEventArgs e)
        {
            AddBtnClicked?.Invoke(this, Tbl_id.Text);
        }

        private void BtnPlanner_Click(object sender, RoutedEventArgs e)
        {
            PlannerBtnClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
