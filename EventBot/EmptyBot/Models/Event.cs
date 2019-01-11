using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Models
{
    public class Event
    {
        public string Genre { get; set; }

        // Of de gebruiker geeft een city op
        public string City { get; set; }

        // Of de gebruiker gebruikt zijn locatie (wat resulteerd in lat en long)
        public float Lat { get; set; }
        public float Long { get; set; }

        // het aantal km het evenement maximaal mag liggen van de opgegeven plaats
        public float Radius { get; set; }

        public string Date { get; set; }
    }
}
