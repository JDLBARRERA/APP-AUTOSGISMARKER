using UnityEngine;

namespace ARTrackBuilder.Data
{
    /// <summary>
    /// 150 pistas: 3 dificultades, 5 módulos y 10 carreras.
    /// Cada formato coloca solo las zonas de su reglamento, más un tramo de lluvia.
    /// Quedan separadas para recorrerse en orden alrededor de la vuelta.
    /// </summary>
    public static class RaceSeasonCatalog
    {
        public const int ModuleCount = 5;
        public const int RacesPerModule = 10;

        private const float HalfMat = 0.42f;
        private const float Height = 0.002f;

        private static readonly string[] ModuleNames =
        {
            "Óvalo",
            "Ocho",
            "Recta",
            "Manzanas",
            "Campeonato"
        };

        private struct RulePlan
        {
            public float Exact;
            public float Tension;
            public float Pit;
            public float Long;
            public float Ice;
            public float Lava;
            public float Rain;
            public float Exact2;
            public float Pit2;
            public float Long2;
            public float Tension2;
            public float Tension3;
        }

        public static string ModuleName(int module)
        {
            return ModuleNames[Wrap(module, ModuleCount)];
        }

        public static DefinedTrack Get(RaceFormat format, int module, int race)
        {
            int moduleIndex = Wrap(module, ModuleCount);
            int raceIndex = Wrap(race, RacesPerModule);
            int pointCount = format == RaceFormat.City ? 10 : format == RaceFormat.Medium ? 12 : 16;
            float formatScale = format == RaceFormat.City ? 0.78f : format == RaceFormat.Medium ? 0.9f : 1f;
            float raceScale = 0.94f + (raceIndex % 5) * 0.012f;
            bool mirror = raceIndex >= 5;

            Vector3[] raw = Shape(moduleIndex, pointCount);
            if (moduleIndex == 4)
            {
                DentTopSide(raw);
            }

            ApplyLayout(raw, formatScale * raceScale, (raceIndex % 5) * 3f, mirror);

            TrackWaypoint[] points = new TrackWaypoint[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                points[i] = TrackWaypoint.At(raw[i], WaypointRule.Path);
            }

            PlaceRules(points, format, PlanFor(moduleIndex));

            string formatName = format == RaceFormat.Medium ? "GRAN CIRCUITO" : format == RaceFormat.GrandPrix ? "GRAND PRIX" : "CIRCUITO";
            string moduleName = ModuleNames[moduleIndex];
            return new DefinedTrack
            {
                TrackID = "pkg-" + (int)format + "-" + moduleIndex + "-" + raceIndex,
                DisplayName = formatName + " · " + moduleName + " · Carrera " + (raceIndex + 1),
                ModuleName = moduleName,
                Format = format,
                ModuleIndex = moduleIndex,
                RaceIndex = raceIndex,
                Laps = format == RaceFormat.City ? 1 : format == RaceFormat.Medium ? 2 : 3,
                Waypoints = points
            };
        }

        private static RulePlan PlanFor(int module)
        {
            switch (module)
            {
                case 1:
                    return new RulePlan
                    {
                        Exact = 0.10f, Tension = 0.18f, Pit = 0.40f, Long = 0.50f, Ice = 0.62f, Lava = 0.88f, Rain = 0.54f,
                        Exact2 = 0.15f, Pit2 = 0.42f, Long2 = 0.58f, Tension2 = 0.36f, Tension3 = 0.68f
                    };
                case 2:
                    return new RulePlan
                    {
                        Exact = 0.06f, Tension = 0.76f, Pit = 0.88f, Long = 0.22f, Ice = 0.63f, Lava = 0.38f, Rain = 0.48f,
                        Exact2 = 0.12f, Pit2 = 0.48f, Long2 = 0.18f, Tension2 = 0.32f, Tension3 = 0.70f
                    };
                case 3:
                    return new RulePlan
                    {
                        Exact = 0.22f, Tension = 0.74f, Pit = 0.86f, Long = 0.10f, Ice = 0.59f, Lava = 0.34f, Rain = 0.46f,
                        Exact2 = 0.16f, Pit2 = 0.42f, Long2 = 0.68f, Tension2 = 0.28f, Tension3 = 0.52f
                    };
                case 4:
                    return new RulePlan
                    {
                        Exact = 0.05f, Tension = 0.56f, Pit = 0.63f, Long = 0.20f, Ice = 0.82f, Lava = 0.38f, Rain = 0.48f,
                        Exact2 = 0.12f, Pit2 = 0.88f, Long2 = 0.16f, Tension2 = 0.30f, Tension3 = 0.74f
                    };
                default:
                    return new RulePlan
                    {
                        Exact = 0.16f, Tension = 0.34f, Pit = 0.50f, Long = 0.66f, Ice = 0.80f, Lava = 0.92f, Rain = 0.08f,
                        Exact2 = 0.28f, Pit2 = 0.42f, Long2 = 0.74f, Tension2 = 0.22f, Tension3 = 0.58f
                    };
            }
        }

        private static void PlaceRules(TrackWaypoint[] points, RaceFormat format, RulePlan plan)
        {
            bool[] used = new bool[points.Length];
            used[0] = true;
            points[0] = TrackWaypoint.At(points[0].Position, WaypointRule.Finish);

            Place(points, used, plan.Long, WaypointRule.LongShot, 0, true);
            Place(points, used, plan.Exact, WaypointRule.ExactShot, 0, false);
            Place(points, used, plan.Pit, WaypointRule.Pit, 0, true);
            Place(points, used, plan.Rain, WaypointRule.Rain, 0, true);
            if (format == RaceFormat.GrandPrix)
            {
                Place(points, used, plan.Tension, WaypointRule.TensionTrap, 1, true);
                Place(points, used, plan.Tension2, WaypointRule.TensionTrap, 2, true);
                Place(points, used, plan.Tension3, WaypointRule.TensionTrap, 3, true);
                Place(points, used, plan.Exact2, WaypointRule.ExactShot, 0, false);
                Place(points, used, plan.Pit2, WaypointRule.Pit, 0, true);
                Place(points, used, plan.Long2, WaypointRule.LongShot, 0, true);
                Place(points, used, plan.Ice, WaypointRule.Ice, 0, true);
                Place(points, used, plan.Lava, WaypointRule.Lava, 0, true);
            }
            else if (format == RaceFormat.Medium)
            {
                Place(points, used, plan.Tension, WaypointRule.TensionTrap, 2, true);
                Place(points, used, plan.Exact2, WaypointRule.ExactShot, 0, false);
                Place(points, used, plan.Lava, WaypointRule.Lava, 0, true);
            }
            else
            {
                Place(points, used, plan.Tension, WaypointRule.TensionTrap, 1, true);
                Place(points, used, plan.Ice, WaypointRule.Ice, 0, true);
            }
        }

        private static void Place(TrackWaypoint[] points, bool[] used, float fraction, WaypointRule rule, int value, bool avoidCenter)
        {
            int count = points.Length;
            int ideal = Mathf.RoundToInt(fraction * count) % count;
            if (ideal < 0)
            {
                ideal += count;
            }

            int best = -1;
            float bestScore = float.MinValue;
            for (int delta = 0; delta < count; delta++)
            {
                int index = (ideal + delta) % count;
                if (index == 0 || used[index])
                {
                    continue;
                }

                Vector3 position = points[index].Position;
                bool crowdedCenter = avoidCenter && position.x * position.x + position.z * position.z < 0.006f;
                float score = crowdedCenter ? -1f : Separation(points, used, index) - (delta * 0.012f);
                if (NeighborIsRule(points, used, index))
                {
                    score -= 0.05f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            if (best < 0)
            {
                return;
            }

            used[best] = true;
            points[best] = TrackWaypoint.At(points[best].Position, rule, value);
        }

        private static float Separation(TrackWaypoint[] points, bool[] used, int index)
        {
            float nearest = 2f;
            Vector3 position = points[index].Position;
            for (int i = 0; i < points.Length; i++)
            {
                if (!used[i])
                {
                    continue;
                }

                float distance = Vector3.Distance(position, points[i].Position);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }

        private static bool NeighborIsRule(TrackWaypoint[] points, bool[] used, int index)
        {
            int previous = (index - 1 + points.Length) % points.Length;
            int next = (index + 1) % points.Length;
            return (used[previous] && points[previous].Rule != WaypointRule.Path)
                || (used[next] && points[next].Rule != WaypointRule.Path);
        }

        private static Vector3[] Shape(int module, int count)
        {
            switch (module)
            {
                case 1:
                    return FigureEight(count, 0.34f, 0.17f);
                case 2:
                    return Stadium(count, 0.26f, 0.15f);
                case 3:
                    return Stadium(count, 0.16f, 0.18f);
                case 4:
                    return Stadium(count, 0.28f, 0.15f);
                default:
                    return Oval(count, 0.36f, 0.22f);
            }
        }

        private static Vector3[] Oval(int count, float radiusX, float radiusZ)
        {
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                points[i] = new Vector3(radiusX * Mathf.Cos(t), Height, radiusZ * Mathf.Sin(t));
            }

            return points;
        }

        private static Vector3[] FigureEight(int count, float radiusX, float radiusZ)
        {
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.PI * 0.5f + (i / (float)count) * Mathf.PI * 2f;
                points[i] = new Vector3(radiusX * Mathf.Sin(t), Height, radiusZ * Mathf.Sin(t * 2f));
            }

            return points;
        }

        private static Vector3[] Stadium(int count, float halfLength, float radius)
        {
            float straight = halfLength * 2f;
            float arc = Mathf.PI * radius;
            float perimeter = (straight + arc) * 2f;
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = PointOnStadium((i / (float)count) * perimeter, halfLength, radius, straight, arc);
            }

            return points;
        }

        private static Vector3 PointOnStadium(float distance, float halfLength, float radius, float straight, float arc)
        {
            float d = distance;
            if (d < straight)
            {
                return new Vector3(Mathf.Lerp(-halfLength, halfLength, d / straight), Height, -radius);
            }

            d -= straight;
            if (d < arc)
            {
                float angle = -Mathf.PI * 0.5f + (d / arc) * Mathf.PI;
                return new Vector3(halfLength + Mathf.Cos(angle) * radius, Height, Mathf.Sin(angle) * radius);
            }

            d -= arc;
            if (d < straight)
            {
                return new Vector3(Mathf.Lerp(halfLength, -halfLength, d / straight), Height, radius);
            }

            d -= straight;
            float back = Mathf.PI * 0.5f + (d / Mathf.Max(arc, 0.0001f)) * Mathf.PI;
            return new Vector3(-halfLength + Mathf.Cos(back) * radius, Height, Mathf.Sin(back) * radius);
        }

        private static void DentTopSide(Vector3[] points)
        {
            int tip = 0;
            float best = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].z <= 0f)
                {
                    continue;
                }

                float score = Mathf.Abs(points[i].x) - points[i].z;
                if (score < best)
                {
                    best = score;
                    tip = i;
                }
            }

            points[tip].z *= 0.42f;
            int previous = (tip - 1 + points.Length) % points.Length;
            int next = (tip + 1) % points.Length;
            points[previous].z = Mathf.Lerp(points[previous].z, points[tip].z, 0.35f);
            points[next].z = Mathf.Lerp(points[next].z, points[tip].z, 0.35f);
        }

        private static void ApplyLayout(Vector3[] points, float scale, float degrees, bool mirror)
        {
            Quaternion rotation = Quaternion.Euler(0f, degrees, 0f);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 point = points[i];
                if (mirror)
                {
                    point.x = -point.x;
                }

                point = rotation * (point * scale);
                point.x = Mathf.Clamp(point.x, -HalfMat, HalfMat);
                point.z = Mathf.Clamp(point.z, -HalfMat, HalfMat);
                point.y = Height;
                points[i] = point;
            }
        }

        private static int Wrap(int value, int length)
        {
            return ((value % length) + length) % length;
        }
    }
}
