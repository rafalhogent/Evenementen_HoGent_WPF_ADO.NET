using System;
using System.Collections;
using System.Linq;
using System.Numerics;

namespace Evenementen.Domain
{
    public class DomainController
    {
        private IEvenementMapper _evenementMapper;
        public event EventHandler<double>? RowAdded;
        public event EventHandler<int>? FileRead;
        public event EventHandler<int>? JobDone;

        public DomainController(IEvenementMapper evenementMapper)
        {
            _evenementMapper = evenementMapper;
            _evenementMapper.RowAdded += OnRowAdded;
            _evenementMapper.FileRead += OnFileRead;
            _evenementMapper.AllRowsMapped += OnJobDone;
        }

        #region  - - - - - - - - -  Events Subscribers - - - - - - - - - - - - - - -
        private void OnJobDone(object? sender, int e)
        {
            JobDone?.Invoke(sender, e);
        }

        private void OnRowAdded(object? sender, double e)
        {
            RowAdded?.Invoke(sender, e);
        }

        private void OnFileRead(object? sender, int e)
        {
            FileRead?.Invoke(sender, e);
        }
        #endregion



        #region  - - - - - - - - - Settings  - - - - - - - - - - - - - - - - - - - - - - 

        public int MapCsvFileIntoDatabase(string connectionString, string csvPath)
        {
            var res = _evenementMapper.MapCsvIntoDatabase(connectionString, csvPath);
            return res;
        }

        public bool CheckIfDbExists(string connectionString)
        {
            return _evenementMapper.CheckDbExists(connectionString);
        }
        public string GetAboutMessage()
        {
            return "2022 Gentse Evenementen v 1.10";
        }

        #endregion - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -



        #region  - - - - - - - - - private  Methodes  - - - - - - - - - - - - - - - - - - - -

        private bool CheckIfEvenementPeriodeCollides(Evenement evenement)
        {
            List<Evenement> plannerEven = _evenementMapper.GetEvenementenFromPlanner()
                                                          .SelectNestedChildren(x => x.Subevenementen)
                                                          .Where(x => x.StartDatum != null || x.EindDatum != null).ToList();

            foreach (var planEvn in plannerEven)
            {
                if (evenement.StartDatum <= planEvn.EindDatum && evenement.EindDatum >= planEvn.StartDatum)
                {
                    return true;
                }
            }
            return false;
        }

        private void CalculateEvenementDynamicData(ref Evenement evn)
        {
            var flattendSubevenementen = evn.Subevenementen.SelectNestedChildren(s => s.Subevenementen).ToList();
            if (evn.Prijs == null)
            {
                evn.Prijs = flattendSubevenementen.Select(x => x.Prijs).Sum();
            }
            if (evn.StartDatum == null)
            {
                evn.StartDatum = flattendSubevenementen.Select(x => x.StartDatum).Min();
            }
            if (evn.EindDatum == null)
            {
                evn.EindDatum = flattendSubevenementen.Select(x => x.EindDatum).Max();
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -




        #region  - - - - - - - - - Databse Methodes  - - - - - - - - - - - - - - - - - - - - - - - - - - -

        public Dictionary<string, string> GetEvenementenByParentEvenementId(string? parentEvenementId, string? word = null)
        {
            var eveDict = _evenementMapper.GetEvenementenByParentEvenementId(parentEvenementId, word)
                .Select(x => new { id = x.Identifier, name = x })
                .OrderBy(x => x.name.ToString()).ToDictionary(x => x.id, x => x.name.ToString());

            return eveDict == null ? new Dictionary<string, string>() : eveDict;
        }

        public OverviewViewModel GetOverviewViewModelByEvenementId(string? evnId)
        {
            Evenement? parent = null;
            OverviewViewModel overviewVM = new();

            var evnRes = _evenementMapper.GetEvenementById(evnId);
            var subevn = _evenementMapper.GetEvenementenByParentEvenementId(evnId);
            if (evnRes != null)
            {
                evnRes.Subevenementen = subevn.OrderBy(x => x.StartDatum).ToList();
                CalculateEvenementDynamicData(ref evnRes);
            }
            overviewVM = EvenementViewModelMapper.Map(evnRes, parent?.Naam, subevn);
            return overviewVM;
        }


        public PlannerViewModel GetPlannerViewModel()
        {
            PlannerViewModel pVM = new();
            List<Evenement> plannerEvn = _evenementMapper.GetEvenementenFromPlanner().ToList();

            for (int i = 0; i < plannerEvn.Count; i++)
            {
                var evn = plannerEvn[i];

                var flattendSubevenementen = evn.Subevenementen
                                            .SelectNestedChildren(s => s.Subevenementen)
                                            .Where(x => x.StartDatum != null && x.EindDatum != null).OrderBy(x => x.StartDatum).ToList();
                evn.Subevenementen = flattendSubevenementen;

                CalculateEvenementDynamicData(ref evn);
            }


            pVM.TotalPrice = $"{plannerEvn.Select(x => x.Prijs).Sum()} €";

            pVM.PlannerEvenementen = plannerEvn
                                               .OrderBy(x => x.StartDatum)
                                               .Select(x => new
                                               {
                                                   Key = x.Identifier,
                                                   Value = x.ToStringExtended()
                                               }).ToDictionary(x => x.Key, x => x.Value);
            return pVM;
        }

        public void AddEvenementToPlanner(string evenementId)
        {
            var evm = _evenementMapper.GetEvenementById(evenementId);
            if (evm != null)
            {
                bool evnAdded = _evenementMapper.IsEvenementByIdAlreadyAddedToPlanner(evm.Identifier);
                if (!evnAdded)
                {
                    // check periodes

                    if (!CheckIfEvenementPeriodeCollides(evm))
                    {
                        _evenementMapper.AddEvenementToPlanner(evenementId);
                    }
                    else
                    {
                        throw new Exception("Selected Evenement collides with Evenements in planner");
                    }
                }
                else
                {
                    throw new Exception("Evenement already added to planner");
                }
            }
            else
            {
                throw new Exception("Evenement does not exist");
            }
        }

        public void RemoveEvenementFromPlannerById(string key)
        {
            _evenementMapper.RemoveEvenementFromPlannerById(key);
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    }
}