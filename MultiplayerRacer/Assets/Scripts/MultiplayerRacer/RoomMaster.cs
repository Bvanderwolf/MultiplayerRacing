using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public int PlayersReady { get; private set; } = 0;
        private int currentLevelIndex = 0;

        public int NextLevelIndex
        {
            get
            {
                //return next value only if not out of scene count bounds, else -1
                int next = currentLevelIndex + 1;
                if (next <= SceneManager.sceneCountInBuildSettings)
                {
                    return next;
                }
                else return -1;
            }
        }

        public void UpdatePlayersReady(bool ready)
        {
            PlayersReady += ready ? 1 : -1;
        }
    }
}