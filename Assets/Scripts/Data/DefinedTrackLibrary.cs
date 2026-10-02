using UnityEngine;

namespace ARTrackBuilder.Data
{
    public enum RaceFormat
    {
        City,
        Medium,
        GrandPrix
    }

    public enum WaypointRule
    {
        Path,
        Finish,
        ExactShot,
        Pit,
        LongShot,
        TensionTrap,
        Lava,
        Ice,
        Rain
    }

    /// <summary>
    /// Un punto del circuito y la regla del reglamento que se proyecta ahí.
    /// </summary>
    public struct TrackWaypoint
    {
        public Vector3 Position;
        public WaypointRule Rule;
        public int RuleValue;

        public static TrackWaypoint At(Vector3 position, WaypointRule rule, int ruleValue = 0)
        {
            return new TrackWaypoint
            {
                Position = position,
                Rule = rule,
                RuleValue = ruleValue
            };
        }
    }

    /// <summary>
    /// Circuito cerrado, en metros, armado para un formato de carrera.
    /// </summary>
    public sealed class DefinedTrack
    {
        public string TrackID;
        public string DisplayName;
        public string ModuleName;
        public RaceFormat Format;
        public int ModuleIndex;
        public int RaceIndex;
        public int Laps;
        public TrackWaypoint[] Waypoints;
    }

    /// <summary>
    /// Abre cada dificultad en la carrera 1 del módulo Óvalo.
    /// El paquete completo vive en RaceSeasonCatalog.
    /// </summary>
    public static class DefinedTrackLibrary
    {
        public static int Count => 3;

        public static DefinedTrack Get(int index)
        {
            int wrapped = ((index % Count) + Count) % Count;
            return RaceSeasonCatalog.Get((RaceFormat)wrapped, 0, 0);
        }

        public static DefinedTrack Get(RaceFormat format)
        {
            return RaceSeasonCatalog.Get(format, 0, 0);
        }

        public static int IndexOf(RaceFormat format)
        {
            return (int)format;
        }
    }
}
