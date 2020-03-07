using System;
using UnityEngine;

namespace MultiplayerRacer
{
    [Serializable]
    public struct RaceTrack
    {
        [SerializeField] private string name;
        [SerializeField] private int shifts;

        public string Name => name;
        public int Shifts => shifts;
    }
}