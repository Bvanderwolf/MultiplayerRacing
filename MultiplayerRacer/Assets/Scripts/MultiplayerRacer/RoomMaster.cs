namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; } = false;
        public int PlayersReady { get; private set; } = 0;
        public int PlayersInGameScene { get; private set; } = 0;
        public int PlayersFinished { get; private set; } = 0;

        public const float LAST_MAN_LEAVE_DELAY = 4f;

        public const int BYTESIZE = 3 * 4;
        public const char CODE = 'R';

        public RoomMaster(int playersfinished = 0, int playersready = 0, int playersingamescene = 0)
        {
            PlayersReady = playersready;
            PlayersInGameScene = playersingamescene;
            PlayersFinished = playersfinished;
        }

        public bool RaceIsFinished => PlayersFinished == PlayersInGameScene;

        public static void SetRegistered()
        {
            Registered = true;
        }

        public void ResetPlayersReady()
        {
            PlayersReady = 0;
        }

        public void UpdatePlayersFinished()
        {
            PlayersFinished++;
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