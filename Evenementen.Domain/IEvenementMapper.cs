using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evenementen.Domain
{
    public interface IEvenementMapper
    {
        public event EventHandler<double> RowAdded;
        public event EventHandler<int> FileRead;
        public event EventHandler<int> AllRowsMapped;

        void AddEvenementToPlanner(string evenementId);
        bool CheckDbExists(string cs);
        Evenement? GetEvenementById(string evnId);
        IEnumerable<Evenement> GetEvenementenByParentEvenementId(string? parentEvenementId, string? word = null);
        IEnumerable<Evenement> GetEvenementenFromPlanner();
        bool IsEvenementByIdAlreadyAddedToPlanner(string identifier);
        int MapCsvIntoDatabase(string connectionString, string csvPath);
        void RemoveEvenementFromPlannerById(string key);
    }
}
