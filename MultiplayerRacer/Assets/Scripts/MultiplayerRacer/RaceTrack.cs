using System;
using MultiplayerRacerEnums;
using UnityEngine;

namespace MultiplayerRacer
{
    [Serializable]
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
            //check all but first indexes for start types to reset
            for (int i = 1; i < roadTypes.Length; i++)
            {
                if (roadTypes[i] == RoadType.START)
                    roadTypes[i] = RoadType.DEFAULT;
            }
            //check all but last indexes for end types to reset
            for (int i = 0; i < roadTypes.Length - 1; i++)
            {
                if (roadTypes[i] == RoadType.END)
                    roadTypes[i] = RoadType.DEFAULT;
            }
            //set first index to start and last to end
            roadTypes[0] = RoadType.START;
            roadTypes[roadTypes.Length - 1] = RoadType.END;
        }
    }
}