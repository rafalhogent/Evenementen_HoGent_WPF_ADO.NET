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

        public EvenementViewModel? CurrentEvenement { get; set; } = null;

        public Dictionary<string, string?> _evenementen = new();
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

            _overviewPage.LsbEvenementen.ItemsSource = _evenementen;
            _plannerPage.Lsb_Planner.ItemsSource = _plannerVM.PlannerEvenementen;
            _overviewPage.LsbEvenementen.DisplayMemberPath = "Value";
            SubscribeToEvents();
            CheckDbAndSetOverview();
            _mainWindow.Show();
        }

        private void SubscribeToEvents()
        {
            _overviewPage.SettingsBtnClicked += SettingsBtn_Clicked;
            _overviewPage.EvenementSelected += ShowEvenementDetails_Clicked;
            _overviewPage.UpBtnClicked += GoUpBtn_Clicked;
            _settingsPage.GoBackClicked += GoToOverviewPage_Clicked;
            _settingsPage.MappingStarted += Mapping_Started;
            _controller.RowAdded += ProgressMade;
            _controller.JobDone += MappingIsDone;
            _overviewPage.FindCicked += Find_Clicked;
            _overviewPage.AddBtnClicked += AddEvenementToDb;
            _overviewPage.PlannerBtnClicked += OpenPlannerPage;
            _plannerPage.BtnGoBackClicked += GoToOverviewPage_Clicked;
            _plannerPage.BtnRemoveClicked += RemoveBtn_Clicked;

        }

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

        private void OpenPlannerPage(object? sender, EventArgs e)
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
                        UnblockControls();
                        ResetOverview();
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

        private void UnblockControls()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DbFound)
                {
                    _overviewPage.BtnPlanner.IsEnabled = true;
                    _overviewPage.Btn_Find.IsEnabled = true;
                    if (CurrentEvenement != null) _overviewPage.Btn_Find.IsEnabled = true;
                    _overviewPage.BtnPlanner.IsEnabled = true;
                    _overviewPage.TxbSearch.IsEnabled = true;
                }

            });
        }

        private void ResetOverview()
        {
            UpdateEvenemetenListBox(null);
            RefreshOverviewPage(null);
        }

        private void UpdateEvenemetenListBox(string? parent)
        {
            try
            {
                var res = _controller.GetEvenementenByParentEvenementId(parent);
                _evenementen = res != null ? res : new();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }



        private void AddEvenementToDb(object? sender, string e)
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

        private void Find_Clicked(object? sender, string word)
        {
            _evenementen = _controller.GetEvenementenByParentEvenementId(CurrentEvenement?.Identifier, word);
            RefreshOverviewPage(CurrentEvenement);
        }

        private void GoUpBtn_Clicked(object? sender, EventArgs e)
        {
            if (CurrentEvenement?.ParentEvenementId is null)
            {
                CurrentEvenement = null;
                UpdateEvenemetenListBox(null);
                RefreshOverviewPage(CurrentEvenement);
            }
            else
            {
                try
                {
                    var res = _controller.GetEvenementDetailsById(CurrentEvenement.ParentEvenementId);
                    if (res != null)
                    {
                        CurrentEvenement = res;
                        UpdateEvenemetenListBox(CurrentEvenement.Identifier);
                        RefreshOverviewPage(CurrentEvenement);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }

            }

        }

        private void ShowEvenementDetails_Clicked(object? sender, string e)
        {
            ShowEvenementDetailsOnPage(e);
        }

        private void ShowEvenementDetailsOnPage(string? evnId)
        {
            try
            {
                if (evnId != null)
                {
                    var res = _controller.GetEvenementDetailsById(evnId);
                    if (res != null)
                    {
                        _evenementen = _controller.GetEvenementenByParentEvenementId(res.Identifier);
                        CurrentEvenement = res;
                        RefreshOverviewPage(res);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void RefreshOverviewPage(EvenementViewModel? evnVM)
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
                _overviewPage.Btn_Up.IsEnabled = CurrentEvenement == null ? false : true;
                _overviewPage.LsbEvenementen.ItemsSource = _evenementen;
                _overviewPage.LsbEvenementen.Items.Refresh();
            });
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

        private void SettingsBtn_Clicked(object? sender, EventArgs e)
        {
            OpenSettingsPage();
        }

        private void GoToOverviewPage_Clicked(object? sender, EventArgs e)
        {
            SaveSettings();
            CheckDbAndSetOverview();
            OpenOverviewPage();
        }

        private void OpenOverviewPage()
        {
            ReadSettings();
            if (DbFound)
            {
                UnblockControls();
                ResetOverview();
            }

            _mainWindow.MainFrame.Content = _overviewPage;
        }

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

        private void Mapping_Started(object? sender, EventArgs e)
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

        private void SetSettingsControlsAreEnebled(bool status = false)
        {
            _settingsPage.Grd_Settings.IsEnabled = status;
        }




    }
}
