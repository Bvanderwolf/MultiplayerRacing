using ExitGames.Client.Photon;
using MultiplayerRacerEnums;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManagerHelper
    {
        public static readonly byte[] memRoomMaster = new byte[RoomMaster.BYTESIZE];

        /// <summary>
        /// Returns a UI script based on current scene
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public MultiplayerRacerUI GetCanvasReference(MultiplayerRacerScenes scene)
        {
            switch (scene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    return GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();

                case MultiplayerRacerScenes.GAME:
                    return GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<GameUI>();
            }
            return null;
        }

        public bool RegisterRoomMaster()
        {
            return PhotonPeer.RegisterType(typeof(RoomMaster), (byte)RoomMaster.CODE, SerializeRoomMaster, DeserializeRoomMaster);
        }

        private static short SerializeRoomMaster(StreamBuffer outStream, object customobject)
        {
            RoomMaster rm = (RoomMaster)customobject;

            lock (memRoomMaster)
            {
                byte[] bytes = memRoomMaster;
                int index = 0;
                Protocol.Serialize(rm.PlayersReady, bytes, ref index);
                Protocol.Serialize(rm.PlayersInGameScene, bytes, ref index);
                Protocol.Serialize(rm.PlayersFinished, bytes, ref index);
                outStream.Write(bytes, 0, RoomMaster.BYTESIZE);
            }
            return RoomMaster.BYTESIZE;
        }

        private static object DeserializeRoomMaster(StreamBuffer inStream, short length)
        {
            int playersready;
            int playersingamescene;
            int playersfinished;
            lock (memRoomMaster)
            {
                inStream.Read(memRoomMaster, 0, RoomMaster.BYTESIZE);
                int index = 0;
                Protocol.Deserialize(out playersready, memRoomMaster, ref index);
                Protocol.Deserialize(out playersingamescene, memRoomMaster, ref index);
                Protocol.Deserialize(out playersfinished, memRoomMaster, ref index);
            }
            RoomMaster rm = new RoomMaster(playersfinished, playersready, playersingamescene);

            return rm;
        }
    }
}