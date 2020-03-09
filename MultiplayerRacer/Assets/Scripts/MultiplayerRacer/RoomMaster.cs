using UnityEngine;

namespace MultiplayerRacer
{
    public class RoomMaster
    {
        public static bool Registered { get; private set; } = false;
        public int PlayersReady { get; private set; } = 0;
        public int PlayersInGameScene { get; private set; } = 0;
        public int[] PlayersFinished;

        public const float LAST_MAN_LEAVE_DELAY = 4f;

        public const int BYTESIZE = (2 * 4) + (4 + 16 - 2);

        public RoomMaster(int[] finishedPlayers, int playersready = 0, int playersingamescene = 0)
        {
            PlayersReady = playersready;
            PlayersInGameScene = playersingamescene;
            PlayersFinished = finishedPlayers;
        }

        public bool RaceIsFinished()
        {
            if (PlayersFinished == null)
                return false;

            int count = 0;
            for (int i = 0; i < PlayersFinished.Length; i++)
            {
                if (PlayersFinished[i] != 0)
                    count++;
            }
            return count == PlayersInGameScene;
        }

        public static void SetRegistered()
        {
            Registered = true;
        }

        public void UpdatePlayersFinished(int numberInRoom)
        {
            if (PlayersFinished == null)
                return;

            for (int i = 0; i < PlayersFinished.Length; i++)
            {
                if (PlayersFinished[i] == 0)
                {
                    PlayersFinished[i] = numberInRoom;
                    break;
                }
                if (i == PlayersFinished.Length)
                    Debug.LogError("tried adding number in room to full players finished array");
            }
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