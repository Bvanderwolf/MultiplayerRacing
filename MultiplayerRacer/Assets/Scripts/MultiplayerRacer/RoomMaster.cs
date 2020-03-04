namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; } = false;
        public int PlayersReady { get; private set; } = 0;
        public int PlayersInGameScene { get; private set; } = 0;

        public RoomMaster(int playersready = 0, int playersingamescene = 0)
        {
            PlayersReady = playersready;
            PlayersInGameScene = playersingamescene;
        }

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

        public void UpdatePlayersInGameScene(bool join)
        {
            PlayersInGameScene += join ? 1 : -1;
        }
    }
}