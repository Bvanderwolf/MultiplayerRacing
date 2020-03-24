using ExitGames.Client.Photon;
using MultiplayerRacerEnums;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManagerHelper
    {
        public static readonly byte[] memRoomMaster = new byte[RoomMaster.BYTESIZE];
        public List<Sprite> CarSpritesSelectable { get; private set; }
        public List<Sprite> CarSpritesUsable { get; private set; }

        public InRoomManagerHelper()
        {
            //load car sprites for storage upon instantiation
            LoadCarSprites();
        }

        private void LoadCarSprites()
        {
            string selectablePath = "Sprites/Cars/CarSelect/";
            CarSpritesSelectable = new List<Sprite>()
            {
                Resources.Load<Sprite>(selectablePath + "car_people1"),
                Resources.Load<Sprite>(selectablePath + "car_people2"),
                Resources.Load<Sprite>(selectablePath + "car_people3"),
                Resources.Load<Sprite>(selectablePath + "car_people4"),
                Resources.Load<Sprite>(selectablePath + "car_people5"),
                Resources.Load<Sprite>(selectablePath + "car_people6"),
                Resources.Load<Sprite>(selectablePath + "car_people7"),
                Resources.Load<Sprite>(selectablePath + "car_people8")
            };
            string usablePath = "Sprites/Cars/PlayerUsed/";
            CarSpritesUsable = new List<Sprite>()
            {
                Resources.Load<Sprite>(usablePath + "car_people1"),
                Resources.Load<Sprite>(usablePath + "car_people2"),
                Resources.Load<Sprite>(usablePath + "car_people3"),
                Resources.Load<Sprite>(usablePath + "car_people4"),
                Resources.Load<Sprite>(usablePath + "car_people5"),
                Resources.Load<Sprite>(usablePath + "car_people6"),
                Resources.Load<Sprite>(usablePath + "car_people7"),
                Resources.Load<Sprite>(usablePath + "car_people8")
            };
        }

        public int GetNumberInRoomOfPlayer(int actorNumber)
        {
            Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
            Player player = players[actorNumber];
            int num = 1;
            /*for each player that has a smaller actornumber that the player
            increase num to eventually get its number in the room*/
            foreach (KeyValuePair<int, Player> pair in players)
            {
                if (player.ActorNumber > pair.Key)
                    num++;
            }

            return num;
        }

        public int GetNumberInRoomOfPlayer(Player player)
        {
            int num = 1;
            /*for each player that has a smaller actornumber that the player
            increase num to eventually get its number in the room*/
            foreach (KeyValuePair<int, Player> pair in PhotonNetwork.CurrentRoom.Players)
            {
                if (player.ActorNumber > pair.Key)
                    num++;
            }

            return num;
        }

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

        /// <summary>
        /// Returns an ordered array based on what time
        /// players have finished where index 0 is first
        /// and length - 1 is last
        /// </summary>
        /// <returns></returns>
        public Player[] GetFinishedPlayersOrdered()
        {
            Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
            Player[] playersOrdered = new Player[players.Count];
            //fill times dictionary with unordered values
            foreach (KeyValuePair<int, Player> pair in players)
            {
                playersOrdered[pair.Key - 1] = pair.Value;
            }
            //return an order list based on the finish time
            return playersOrdered.OrderBy((p) =>
            {
                string timeString = (string)p.CustomProperties["FinishTime"];
                return TimeSpan.Parse(timeString);
            }).ToArray();
        }

        /// <summary>
        /// registers RoomMaster as custom type and returns the result
        /// </summary>
        /// <returns></returns>
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