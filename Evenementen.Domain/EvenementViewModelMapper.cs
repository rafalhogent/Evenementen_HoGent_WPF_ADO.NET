using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evenementen.Domain
{
    public class EvenementViewModelMapper
    {
        public static EvenementViewModel Map(Evenement evn, string? parentNaam, IEnumerable<Evenement>? subevn = null )
        {
            Dictionary<string, string> children = new();
            if (subevn != null)
            {
                foreach (var item in subevn)
                {
                    children.Add(item.Identifier, $"{item.StartDatum} - {item.EindDatum} - {item.Prijs} € : {item.Naam} " );
                }
            }
            return new EvenementViewModel()
            {
                Identifier = evn.Identifier,
                Naam = evn.Naam,
                Prijs = evn.Prijs == null ? "" : $"{evn.Prijs} €",
                Beschrijving = evn.Beschrijving,
                StartDatum = evn.StartDatum == null ? "" : evn.StartDatum.Value.ToString(),
                EindDatum = evn.EindDatum == null ? "" : evn.EindDatum.Value.ToString(),
                ParentEvenementNaam = parentNaam,
                ParentEvenementId = evn.ParentEvenementId,
                Subevenementen = children,
            };
        }
    }
}
