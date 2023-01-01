﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evenementen.Domain
{
    public class PlannerViewModel
    {
        public string TotalPrice { get; set; } = "";
        public Dictionary<string, string> PlannerEvenementen { get; set; } = new();
    }
}
