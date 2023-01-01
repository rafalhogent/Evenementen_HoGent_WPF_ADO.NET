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


        public override string? ToString()
        {
            string date = StartDatum != null ? $"{FormatDate(StartDatum)} - {FormatDate(EindDatum)}" : "";
            //return $"{Naam}";
            return $"{Naam} {date}";
        }

        private string FormatDate(DateTime? date)
        {
            if (date == null) return "";
            return $"{date.Value.ToShortDateString()} {date.Value.ToShortTimeString()}";
        }

    }
}