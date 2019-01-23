using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Models
{
    public class EventParams
    {
        public string Genre { get; set; }

        // Of de gebruiker geeft een city op
        public string City { get; set; }

        public string GeoHash { get; set; }

        // het aantal km het evenement maximaal mag liggen van de opgegeven plaats
        public float Radius { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
