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

        #endregion - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -



        #region  - - - - - - - - - private  Methodes  - - - - - - - - - - - - - - - - - - - -

        private void AddSubEvenementen(List<Evenement> subevn, ref List<Evenement> children)
        {
            foreach (var item in subevn)
            {
                children.Add(item);
                var res = _evenementMapper.GetEvenementenByParentEvenementId(item.Identifier);
                if (res != null && res.Count() > 0)
                {
                    AddSubEvenementen(res.ToList(), ref children);
                }
            }
        }

        private List<Evenement> GetAllChildrenbyEvenementId(string? id)
        {
            List<Evenement> allGrandChildren = new();
            AddSubEvenementen(_evenementMapper.GetEvenementenByParentEvenementId(id).ToList(), ref allGrandChildren);
            return allGrandChildren;
        }

        private Evenement GetEvenementFullInfo(Evenement evenement)
        {

            Dictionary<string, string> children = new();

            var res = _evenementMapper.GetEvenementenByParentEvenementId(evenement?.Identifier);
            if (res != null && res.Count() > 0)
            {
                children = res.Select(x => new { id = x.Identifier, name = x.Naam })
                    .ToDictionary(x => x.id, x => x.name);
            }

            if (evenement.Prijs == null || evenement.StartDatum is null || evenement.EindDatum is null)
            {
                List<Evenement> grandChildren = GetAllChildrenbyEvenementId(evenement?.Identifier);

                if (evenement.StartDatum == null)
                {
                    var sd = grandChildren.Select(e => e.StartDatum).Min();
                    evenement.StartDatum = sd;
                }

                if (evenement.EindDatum == null)
                {
                    var ed = grandChildren.Select(e => e.EindDatum).Max();
                    evenement.EindDatum = ed;
                }

                if (evenement.Prijs == null)
                {
                    var pr = grandChildren.Select(e => e.Prijs).Sum();
                    evenement.Prijs = pr;
                }
            }

            return evenement;
        }

        private bool CheckIfEvenementPeriodeCollides(Evenement evenement)
        {
            List<Evenement> plannerEven = new();
            _evenementMapper.GetEvenementenFromPlanner().ToList()
                .ForEach(x =>
                {
                    plannerEven.Add(x);
                    plannerEven.AddRange(GetAllChildrenbyEvenementId(x.Identifier));
                });
            evenement = GetEvenementFullInfo(evenement);

            foreach (var planEvn in plannerEven.Where(x => x.StartDatum != null || x.EindDatum != null).ToList())
            {
                if (evenement.StartDatum <= planEvn.EindDatum && evenement.EindDatum >= planEvn.StartDatum)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -




        #region  - - - - - - - - - Databse Methodes  - - - - - - - - - - - - - - - - - - - - - - - - - - -

        public Dictionary<string, string?> GetEvenementenByParentEvenementId(string? parentEvenementId, string? word = null)
        {
            return _evenementMapper.GetEvenementenByParentEvenementId(parentEvenementId, word)
                .Select(x => new { id = x.Identifier, name = x.ToString() })
                .OrderBy(x => x.name).ToDictionary(x => x.id, x => x.name);
        }

        public EvenementViewModel GetEvenementDetailsById(string evnId)
        {
            Evenement? parent = null;
            EvenementViewModel evnVM = new();

            var evnRes = _evenementMapper.GetEvenementById(evnId);
            if (evnRes != null)
            {

                evnVM = EvenementViewModelMapper.Map(GetEvenementFullInfo(evnRes), parent?.Naam, null);
            }

            return evnVM;
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

        public PlannerViewModel GetPlannerViewModel()
        {
            PlannerViewModel pVM = new();
            List<Evenement> plannerEvn = _evenementMapper.GetEvenementenFromPlanner()
                                        .Select(x => GetEvenementFullInfo(x)).ToList();

            pVM.TotalPrice = $"{plannerEvn.Select(x => x.Prijs).Sum()} €";

            pVM.PlannerEvenementen = plannerEvn.OrderBy(x => x.StartDatum)
                             .Select(x => new
                             {
                                 Key = x.Identifier,
                                 Value = EvenementViewModelMapper
                                 .Map(GetEvenementFullInfo(x), null, GetAllChildrenbyEvenementId(x.Identifier)
                                 .Where(x => x.StartDatum != null).OrderBy(x => x.StartDatum)).ToExtendedInfoString()
                             }).ToDictionary(x => x.Key, x => x.Value);
            return pVM;
        }

        public void RemoveEvenementFromPlannerById(string key)
        {
            _evenementMapper.RemoveEvenementFromPlannerById(key);
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    }
}