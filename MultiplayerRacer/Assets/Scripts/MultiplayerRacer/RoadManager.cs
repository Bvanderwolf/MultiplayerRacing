﻿using Photon.Pun;
using UnityEngine;

namespace MultiplayerRacer
{
    using MultiplayerRacerScenes = InRoomManager.MultiplayerRacerScenes;

    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private Road[] roads;
        [SerializeField] private Color readyColor;

        private Road roadOn; //changes when roads get shifted
        private GameObject myCarSpawn;
        private GameObject myCar;
        private PhotonView PV;
        private Color unReadyColor;
        private float roadLength;

        private void Awake()
        {
            PV = GetComponent<PhotonView>();
            //the player starts on the middle road
            roadOn = roads[1];
            roadLength = roadOn.MainBounds.size.y;

            if (PhotonNetwork.IsConnected)
            {
                //start listeneing to onreadystatuschange and scenereset events
                InRoomManager.Instance.OnReadyStatusChange += OnReadyStatusChanged;
                InRoomManager.Instance.OnSceneReset += OnSceneHasReset;
                InRoomManager.Instance.OnGameStart += OnRaceStarted;
                //place the car spawns on the road for players and get our own car spawn
                myCarSpawn = SetupCarSpawns();
                myCar = PhotonNetwork.Instantiate("Prefabs/Car", myCarSpawn.transform.position, Quaternion.identity);
                unReadyColor = myCarSpawn.GetComponent<SpriteRenderer>().color; //save default color as unready color
                SetupRacerConnection();
            }
            else Debug.LogError("Wont do car setup :: not connected to photon network");
        }

        private void SetupRacerConnection()
        {
            //subscribe to road bound enter and exit events
            Racer racer = myCar.GetComponent<Racer>();
            racer.OnRoadBoundInteraction += OnRoadNeedsShift;
        }

        private void OnRaceStarted()
        {
            roadOn.SetCarSpawnsInactive();
        }

        private void OnSceneHasReset(MultiplayerRacerScenes scene)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

            //reset car spawns according to new playercount
            roadOn.ResetCarSpawns(playerCount, unReadyColor);

            //re-setup car spawns getting our new car spawn and placing the car on it
            myCarSpawn = SetupCarSpawns();
            myCar.transform.position = myCarSpawn.transform.position;
        }

        /// <summary>
        /// Subscribe this function to an onready changed event to get info on new status and scene
        /// to act accordingly
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ready"></param>
        private void OnReadyStatusChanged(MultiplayerRacerScenes scene, bool ready)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            PV.RPC("UpdateReadyStatus", RpcTarget.AllViaServer, InRoomManager.Instance.NumberInRoom, ready);
        }

        /// <summary>
        /// should be called when our car enters a road bound
        /// </summary>
        private void OnRoadNeedsShift(GameObject road)
        {
            ShiftRoad(road);
        }

        /// <summary>
        /// shifts roads based on bound hit
        /// </summary>
        private void ShiftRoad(GameObject road)
        {
            //based on if bound hit was on road on setup shift values
            bool roadOnHit = road == roadOn.gameObject;
            int roadIndex = roadOnHit ? roads.Length - 1 : 0;
            Road shiftingRoad = roads[roadIndex];
            shiftingRoad.Shift(roads.Length * (roadOnHit ? roadLength : -roadLength));

            //shift roads array based on roadOnHit value
            if (roadOnHit)
            {
                roads[roadIndex--] = roads[roadIndex];
                roads[roadIndex--] = roads[roadIndex];
                roads[roadIndex] = shiftingRoad;
            }
            else
            {
                roads[roadIndex++] = roads[roadIndex];
                roads[roadIndex++] = roads[roadIndex];
                roads[roadIndex] = shiftingRoad;
            }
            //road on is always the middle one of the 3
            roadOn = roads[1];
        }

        /// <summary>
        /// Sets up car spawns on road on and returns the gameobject of the one
        /// used by your car. Will return null when failed.
        /// </summary>
        /// <returns></returns>
        private GameObject SetupCarSpawns()
        {
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int numberInRoom = InRoomManager.Instance.NumberInRoom;
            return roadOn.SetupCarSpawns(playerCount, numberInRoom);
        }

        /// <summary>
        /// gets a car spawn on road on based on given player number
        /// </summary>
        /// <param name="playerNumber"></param>
        /// <returns></returns>
        private Transform GetCarSpawn(int playerNumber)
        {
            if (playerNumber < 0 || playerNumber > MatchMakingManager.MAX_PLAYERS)
                return null;

            return roadOn.CarSpawns.transform.GetChild(playerNumber - 1);
        }

        [PunRPC]
        private void UpdateReadyStatus(int playerNumber, bool ready)
        {
            GetCarSpawn(playerNumber).GetComponent<SpriteRenderer>().color = ready ? readyColor : unReadyColor;
        }
    }
}