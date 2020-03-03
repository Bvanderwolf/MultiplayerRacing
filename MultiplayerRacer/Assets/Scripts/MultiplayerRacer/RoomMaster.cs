namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; }
        public int PlayersReady { get; private set; } = 0;

        public RoomMaster(int playersready = 0)
        {
            PlayersReady = playersready;
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
    }
}