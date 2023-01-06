using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Evenementen.Domain;

namespace Evenementen.Presentation
{
    public class EvenementenApp
    {
        public const string SETTINGSFILE = "settings.txt";

        public string ConnectionString { get; set; } =
             @"Server=.\SQLEXPRESS;Database=EvenementenDb;Trusted_Connection=True;Encrypt=False";

        public string FilePath { get; set; } = "";
        public bool DbFound = false;

        public OverviewViewModel? CurrentEvenement { get; set; } = new();

        //public Dictionary<string, string?> _evenementen = new();
        public PlannerViewModel _plannerVM = new();

        private MainWindow _mainWindow;
        private OverviewPage _overviewPage;
        private SettingsPage _settingsPage;
        private PlannerPage _plannerPage;
        DomainController _controller;

        public EvenementenApp(DomainController controller)
        {
            ReadSettings();
            _controller = controller;

            _mainWindow = new();
            _overviewPage = new OverviewPage();
            _settingsPage = new SettingsPage();
            _plannerPage = new PlannerPage();
            _settingsPage.Tbx_connection.Text = ConnectionString;
            _mainWindow.MainFrame.Content = _overviewPage;

            _overviewPage.LsbEvenementen.ItemsSource = CurrentEvenement?.Subevenementen;
            _plannerPage.Lsb_Planner.ItemsSource = _plannerVM.PlannerEvenementen;
            _overviewPage.LsbEvenementen.DisplayMemberPath = "Value";
            SubscribeToEvents();
            CheckDbAndSetOverview();
            _mainWindow.Show();
        }

        #region Mapping csv File

        private void On_StartMapping_Clicked(object? sender, EventArgs e)
        {
            try
            {
                SetSettingsControlsAreEnebled(false);
                SaveSettings();
                _settingsPage.Lbl_Status.Content = "Mapping started...";

                MapFileIntoDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void MapFileIntoDatabase()
        {
            try
            {
                var res = _controller.MapCsvFileIntoDatabase(_settingsPage.Tbx_connection.Text, _settingsPage.Tbx_path.Text);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void MappingIsDone(object? sender, int e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _settingsPage.Lbl_Status.Content = "Mapping done!";
                SetSettingsControlsAreEnebled(true);
            });
        }

        private void ProgressMade(object? sender, double e)
        {
            Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
            {
                _settingsPage.PrbarMap.Value = e;
            }));

        }

        #endregion


        #region Window startup 

        private void SubscribeToEvents()
        {
            _overviewPage.SettingsBtnClicked += On_SettingsBtn_Clicked;
            _overviewPage.EvenementSelected += On_EvenementSelected;
            _overviewPage.UpBtnClicked += On_GoUp_CLicked;
            _settingsPage.GoBackClicked += On_GoFromSettingsToOverviewPage_Clicked;
            _settingsPage.StartMappingClicked += On_StartMapping_Clicked;
            _controller.RowAdded += ProgressMade;
            _controller.JobDone += MappingIsDone;
            _overviewPage.FindCicked += On_Find_Clicked;
            _overviewPage.AddBtnClicked += On_AddEvenementToDbPlannerBtn_Clicked;
            _overviewPage.PlannerBtnClicked += On_OpenPlannerPageBtn_Clicked;
            _plannerPage.BtnGoBackClicked += On_GoFromSettingsToOverviewPage_Clicked;
            _plannerPage.BtnRemoveClicked += RemoveBtn_Clicked;
            _overviewPage.AboutBtnClicked += On_AboutBtn_Clicked;
        }
        private async void CheckDbAndSetOverview()
        {
            if (DbFound) return;
            await Task.Run(() =>
            {
                try
                {
                    var res = _controller.CheckIfDbExists(ConnectionString);
                    if (res)
                    {
                        DbFound = true;
                        UpdateOverviewPage(null);
                        UnblockOverviewPageControls();
                    }
                    else
                    {
                        MessageBox.Show("Database not found, open settings to create one.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }

        #endregion


        #region Planner

        private void RemoveBtn_Clicked(object? sender, EventArgs e)
        {

            try
            {
                if (_plannerPage.Lsb_Planner.SelectedItem is KeyValuePair<string, string> evn)
                {
                    _controller.RemoveEvenementFromPlannerById(evn.Key);
                    RefreshPlannerPage();
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        private void On_OpenPlannerPageBtn_Clicked(object? sender, EventArgs e)
        {
            RefreshPlannerPage();
            _mainWindow.MainFrame.Content = _plannerPage;
        }

        private void RefreshPlannerPage()
        {
            try
            {
                _plannerVM = _controller.GetPlannerViewModel();

                _plannerPage.Lsb_Planner.ItemsSource = _plannerVM.PlannerEvenementen;
                _plannerPage.Lsb_Planner.Items.Refresh();
                _plannerPage.Lbl_PriceValue.Content = _plannerVM.TotalPrice;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        private void On_AddEvenementToDbPlannerBtn_Clicked(object? sender, string e)
        {
            if (e == CurrentEvenement?.Identifier)
            {
                try
                {
                    _controller.AddEvenementToPlanner(e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }


        #endregion


        #region Settings

        private void OpenSettingsPage()
        {
            _mainWindow.MainFrame.Content = _settingsPage;
        }

        private void SaveSettings()
        {
            File.WriteAllText(SETTINGSFILE, _settingsPage.Tbx_connection.Text);
        }

        private void ReadSettings()
        {
            try
            {
                if (File.Exists(SETTINGSFILE))
                {
                    using (StreamReader reader = new(SETTINGSFILE))
                    {
                        var res = reader.ReadLine();
                        if (res != null)
                        {
                            ConnectionString = res;
                        }
                    }
                }
                else
                {
                    SaveSettings();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }

        private void On_SettingsBtn_Clicked(object? sender, EventArgs e)
        {
            OpenSettingsPage();
        }

        private void SetSettingsControlsAreEnebled(bool status = false)
        {
            _settingsPage.Grd_Settings.IsEnabled = status;
        }

        private void On_GoFromSettingsToOverviewPage_Clicked(object? sender, EventArgs e)
        {
            SaveSettings();
            CheckDbAndSetOverview();
            OpenOverviewPage();
        }

        #endregion



        #region Overview Page

        private void On_EvenementSelected(object? sender, string e)
        {
            UpdateOverviewPage(e);
        }

        private void On_Find_Clicked(object? sender, string word)
        {
            var current = string.IsNullOrWhiteSpace(CurrentEvenement?.Identifier) ? null : CurrentEvenement.Identifier;
            var evn = _controller.GetEvenementenByParentEvenementId(current, word);

            if (evn != null && CurrentEvenement != null)
            {
                CurrentEvenement.Subevenementen = evn == null ? new() : evn;
                RefreshOverviewPageControls(CurrentEvenement);
            }
        }

        private void On_GoUp_CLicked(object? sender, EventArgs e)
        {
            if (CurrentEvenement?.ParentEvenementId is null)
            {
                CurrentEvenement = new();
                UpdateOverviewPage(null);
                RefreshOverviewPageControls(CurrentEvenement);
            }
            else
            {
                try
                {
                    var res = _controller.GetOverviewViewModelByEvenementId(CurrentEvenement.ParentEvenementId);
                    if (res != null)
                    {
                        CurrentEvenement = res;
                        UpdateOverviewPage(CurrentEvenement.Identifier);
                        RefreshOverviewPageControls(CurrentEvenement);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OpenOverviewPage()
        {
            ReadSettings();
            if (DbFound)
            {
                UnblockOverviewPageControls();
                UpdateOverviewPage(null);
            }

            _mainWindow.MainFrame.Content = _overviewPage;
        }

        private void RefreshOverviewPageControls(OverviewViewModel? evnVM)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overviewPage.Tbl_id.Text = evnVM == null ? "" : evnVM.Identifier;
                _overviewPage.Tbl_title.Text = evnVM == null ? "" : evnVM.Naam;
                _overviewPage.Tbl_beschr.Text = evnVM == null ? "" : evnVM.Beschrijving;
                _overviewPage.Tbl_start.Text = evnVM == null ? "" : evnVM.StartDatum;
                _overviewPage.Tbl_end.Text = evnVM == null ? "" : evnVM.EindDatum;
                _overviewPage.Tbl_price.Text = evnVM == null ? "" : evnVM.Prijs;
                _overviewPage.Tbl_hoofdevn.Text = evnVM == null ? "" : evnVM.ParentEvenementNaam;
                _overviewPage.Lbl_nav.Content = evnVM?.TreePath.ToString();
                _overviewPage.Btn_Up.IsEnabled = string.IsNullOrWhiteSpace(CurrentEvenement?.Identifier) ? false : true;
                _overviewPage.LsbEvenementen.ItemsSource = CurrentEvenement?.Subevenementen;
                _overviewPage.LsbEvenementen.Items.Refresh();
            });
        }

        private void UpdateOverviewPage(string? evnId)
        {
            try
            {
                var res = _controller.GetOverviewViewModelByEvenementId(evnId);
                if (res != null)
                {
                    CurrentEvenement = res;
                    RefreshOverviewPageControls(CurrentEvenement);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UnblockOverviewPageControls()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DbFound)
                {
                    _overviewPage.BtnPlanner.IsEnabled = true;
                    _overviewPage.Btn_Find.IsEnabled = true;
                    if (CurrentEvenement?.Subevenementen?.Count > 0) _overviewPage.Btn_Find.IsEnabled = true;
                    _overviewPage.BtnPlanner.IsEnabled = true;
                    _overviewPage.TxbSearch.IsEnabled = true;
                }

            });
        }

        #endregion


        private void On_AboutBtn_Clicked(object? sender, EventArgs e)
        {
            MessageBox.Show(_controller.GetAboutMessage());
        }

    }
}
