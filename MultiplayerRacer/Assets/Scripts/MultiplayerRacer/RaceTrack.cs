using MultiplayerRacerEnums;
using UnityEngine;

namespace MultiplayerRacer
{
    [System.Serializable]
    public struct RaceTrack
    {
        [SerializeField] private string name;
        [SerializeField] private RoadType[] roadTypes;

        private const int MAX_SHIFTS = 20;

        public string Name => name;
        public int RoadCount => roadTypes.Length;
        public RoadType[] RoadTypes => roadTypes;

        /// <summary>
        /// Checks whether road types is correct length and
        /// corrects it if neccesary
        /// </summary>
        public void CheckMaxRoadTypes()
        {
            if (roadTypes == null || roadTypes.Length > MAX_SHIFTS)
            {
                Debug.LogError("track to long or null :: resetting length to max");
                roadTypes = new RoadType[MAX_SHIFTS];
            }
        }

        /// <summary>
        /// Checks for a start and end in the track and
        /// corrects it if necessary
        /// </summary>
        public void CheckForStartAndEnd()
        {
            bool hasStart = false;
            bool hasEnd = false;
            //check all but first indexes for start types to reset
            for (int i = 0; i < roadTypes.Length; i++)
            {
                if (i == 0 && IsStartRoadType(roadTypes[i]))
                    hasStart = true;

                if (i != 0 && IsStartRoadType(roadTypes[i]))
                    roadTypes[i] = RoadType.DEFAULT;
            }
            //check all but last indexes for end types to reset
            for (int i = 0; i < roadTypes.Length; i++)
            {
                if (i == RoadTypes.Length - 1 && IsEndRoadType(roadTypes[i]))
                    hasEnd = true;

                if (i != roadTypes.Length - 1 && IsEndRoadType(roadTypes[i]))
                    roadTypes[i] = RoadType.DEFAULT;
            }
            //set first index to start and last to end
            if (!hasStart) roadTypes[0] = RoadType.START;
            if (!hasEnd) roadTypes[roadTypes.Length - 1] = RoadType.END;
        }

        private bool IsStartRoadType(RoadType type)
        {
            return type == RoadType.START
                || type == RoadType.START_HALF_FULL
                || type == RoadType.START_FULL;
        }

        private bool IsEndRoadType(RoadType type)
        {
            return type == RoadType.END
                || type == RoadType.END_HALF_FULL
                || type == RoadType.END_FULL;
        }
    }
}