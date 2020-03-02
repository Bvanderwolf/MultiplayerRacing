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

        public static void SetRegistered()
        {
            Registered = true;
        }

        /// <summary>
        /// needs to be called after a new master has been assigned
        /// to make sure that if the old master left, it can readjust
        /// its values based on the missing player
        /// </summary>
        public void LeavingMasterCheck(bool wasReady)
        {
            if (CurrentLevelIndex == 0)
            {
                if (wasReady) UpdatePlayersReady(false);
            }
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