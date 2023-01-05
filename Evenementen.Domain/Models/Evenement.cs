namespace Evenementen.Domain
{

    public class Evenement
    {
        public string Identifier { get; set; } = null!;
        public string Naam { get; set; } = null!;
        public string? Beschrijving { get; set; }
        public decimal? Prijs { get; set; }
        public DateTime? StartDatum { get; set; }
        public DateTime? EindDatum { get; set; }
        public string? ParentEvenementId { get; set; }


        public List<Evenement> Subevenementen { get; set; } = new();


        private string DateInfo
        {
            get { return StartDatum != null && EindDatum != null ? $" {FormatDate(StartDatum)} - {FormatDate(EindDatum)} " : ""; ; }
        }

        private string PriceInfo
        {
            get { return Prijs == null ? "" : 
                    $" -   {(Prijs != 0 ? (Subevenementen.Count == 0 ? Prijs : "Total " + Prijs ) + " €" : "gratis" )}"; }
        }

        private string ChildrenInfo
        {
            get
            {
                return Subevenementen?.Count() > 0 ? "\n      - " + string.Join("\n      - ", Subevenementen.Select(x => x.ToString())) : "";
            }
        }

        public override string ToString()
        {
            return $"{DateInfo}   {Naam}   {PriceInfo} ";
        }

        public string ToStringExtended()
        {
            return $" @{DateInfo}   {Naam}   {PriceInfo}  {ChildrenInfo}";
        }

        private string FormatDate(DateTime? date)
        {
            if (date == null) return "";
            return $"{date.Value.ToShortDateString()} {date.Value.ToShortTimeString()}";
        }

    }
}