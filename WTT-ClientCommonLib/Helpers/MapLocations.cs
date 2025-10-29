using System.Collections.Generic;
using System.Numerics;

namespace WTTClientCommonLib.Helpers;

public class MapLocations
{
    public class Location
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
    }

    public class MapsLocations
    {
        public List<Location> Interchange { get; set; }
        public List<Location> FactoryDay { get; set; }
        public List<Location> FactoryNight { get; set; }
        public List<Location> Customs { get; set; }
        public List<Location> Woods { get; set; }
        public List<Location> Lighthouse { get; set; }
        public List<Location> Shoreline { get; set; }
        public List<Location> Reserve { get; set; }
        public List<Location> Laboratory { get; set; }
        public List<Location> StreetsOfTarkov { get; set; }
        public List<Location> GroundZero { get; set; }
        public List<Location> GroundZero21 { get; set; }
        public List<Location> Labyrinth { get; set; }
    }
}