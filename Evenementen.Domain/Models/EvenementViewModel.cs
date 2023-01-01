﻿using System.ComponentModel;

namespace Evenementen.Domain
{

    public class EvenementViewModel 
    {
        public string Identifier { get; set; } = "";
        public string Naam { get; set; } = "";
        public string? Beschrijving { get; set; }
        public string Prijs { get; set; } = "";
        public string StartDatum { get; set; } = "";
        public string EindDatum { get; set; } = "";
        public string? ParentEvenementNaam { get; set; }
        public string? ParentEvenementId { get; set; }

        public Dictionary<string, string> Subevenementen { get; set; } = new();

        public string ToExtendedInfoString()
        {
            string indent = new(' ', 16);
            return $"{StartDatum} - {EindDatum} - {Prijs} : {Naam} {string.Join("" , Subevenementen.Select(x => "\n" + indent + x.Value))}";
        }

        public override string? ToString()
        {
            return $"{(Identifier == null ? "..." : Identifier)}   {Naam}";
        }

    }
}