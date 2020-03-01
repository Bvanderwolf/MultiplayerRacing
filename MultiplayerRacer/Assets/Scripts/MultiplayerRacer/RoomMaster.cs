using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; }
        public int PlayersReady = 0;
        public int CurrentLevelIndex = 0;

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

        public static void SetRegistered()
        {
            Registered = true;
        }

        public void UpdatePlayersReady(bool ready)
        {
            PlayersReady += ready ? 1 : -1;
        }
    }
}