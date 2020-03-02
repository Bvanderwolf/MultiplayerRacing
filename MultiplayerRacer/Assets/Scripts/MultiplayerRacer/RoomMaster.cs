using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; }
        public int PlayersReady { get; private set; } = 0;
        public int CurrentLevelIndex { get; private set; } = 0;

        public int NextLevelIndex
        {
            get
            {
                //return next value only if not out of scene count bounds, else -1
                int next = CurrentLevelIndex + 1;
                if (next <= SceneManager.sceneCountInBuildSettings)
                {
                    return next;
                }
                else return -1;
            }
        }

        public bool InLobby => CurrentLevelIndex == 0;

        public static void SetRegistered()
        {
            Registered = true;
        }

        public void ResetPlayersReady()
        {
            PlayersReady = 0;
        }

        public void UpdatePlayersReady(bool ready)
        {
            PlayersReady += ready ? 1 : -1;
        }

        public RoomMaster SetAttributes(int playersready, int currentlevelindex)
        {
            PlayersReady = playersready;
            CurrentLevelIndex = currentlevelindex;
            return this;
        }
    }
}