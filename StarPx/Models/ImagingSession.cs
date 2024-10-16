using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPx.Models
{
    public class ImagingSessionRequest
    {
        public string? local_time { get; set; }
        public Geolocation? geolocation { get; set; }
        public string? targetname { get; set; }
        public SkyCoordinates? skycoordinates { get; set; }
    }

    public class Geolocation
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class SkyCoordinates
    {
        public double ra { get; set; }
        public double dec { get; set; }
    }
    public class ImagingSessionResponse
    {
        public string? imaging_session_id { get; set; }
    }
}
