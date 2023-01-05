using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evenementen.Domain
{
    public class EvenementViewModelMapper
    {
        public static OverviewViewModel Map(Evenement? evn, string? parentNaam, IEnumerable<Evenement>? subevn = null)
        {
            Dictionary<string, string> children = new();
            OverviewViewModel ovvVM = new OverviewViewModel();
            if (subevn != null)
            {
                foreach (var item in subevn.OrderBy(x=>x.StartDatum))
                {
                    children.Add(item.Identifier, item.ToString());
                }
            }

            ovvVM.Subevenementen = children;

            if (evn != null)
            {
                ovvVM.Identifier = evn.Identifier;
                ovvVM.Naam = evn.Naam;
                ovvVM.Prijs = evn.Prijs == null ? "" : $"{(evn.Prijs == 0 ? "gratis" : evn.Prijs + " €")} ";
                ovvVM.Beschrijving = evn.Beschrijving;
                ovvVM.StartDatum = evn.StartDatum == null ? "" : evn.StartDatum.Value.ToString();
                ovvVM.EindDatum = evn.EindDatum == null ? "" : evn.EindDatum.Value.ToString();
                ovvVM.ParentEvenementNaam = parentNaam;
                ovvVM.ParentEvenementId = evn.ParentEvenementId;
            }

            return ovvVM;
        }
    }
}
