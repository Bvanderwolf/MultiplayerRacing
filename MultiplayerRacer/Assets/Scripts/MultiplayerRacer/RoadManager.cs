﻿using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRacer
{
    using MultiplayerRacerScenes = InRoomManager.MultiplayerRacerScenes;

    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private Road[] roads;
        [SerializeField] private RaceTrack[] tracks;
        [SerializeField] private Color readyColor;

        public enum RoadType { DEFAULT, START, END }

        private Road roadOn;
        private GameObject myCarSpawn;
        private GameObject myCar;
        private PhotonView PV;
        private Color unReadyColor;
        private float roadLength;

        private Dictionary<string, RaceTrack> raceTrackDict;
        private RaceTrack trackPlaying;
        private int trackIndexOn;

        private void Awake()
        {
            PV = GetComponent<PhotonView>();
            SetupRoadValues();
            SetupSceneRelations();
            //Testing();
        }

        private void Testing()
        {
            GameObject.Find("Car").GetComponent<Racer>().OnRoadBoundInteraction += DoRoadShift;
            trackPlaying = raceTrackDict["Default"];
        }

        private void OnValidate()
        {
            //check for all tracks if their values are meeting requirements
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].CheckMaxRoadTypes();
                tracks[i].CheckForStartAndEnd();
            }
        }

        /// <summary>
        /// Sets up values related to the track and road
        /// </summary>
        private void SetupRoadValues()
        {
            raceTrackDict = new Dictionary<string, RaceTrack>();
            foreach (RaceTrack track in tracks) raceTrackDict.Add(track.Name, track);

            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].CheckMaxRoadTypes();
                tracks[i].CheckForStartAndEnd();
            }

            trackIndexOn = 0;
            roadOn = roads[2];
            roadLength = roadOn.MainBounds.size.y;
        }

        /// <summary>
        /// Sets up road manager values related to scene interactions
        /// </summary>
        private void SetupSceneRelations()
        {
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

        /// <summary>
        /// sets up values related to the connection with our car
        /// </summary>
        private void SetupRacerConnection()
        {
            //subscribe to road bound enter and exit events
            Racer racer = myCar.GetComponent<Racer>();
            racer.OnRoadBoundInteraction += DoRoadShift;
        }

        private void OnRaceStarted()
        {
            trackPlaying = raceTrackDict["Default"]; //for now use default track. In future get from parameter
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
        private void DoRoadShift(GameObject roadCollidedOn, bool onExit, bool sameExitAsEntry)
        {
            TryShiftRoad(roadCollidedOn, onExit, sameExitAsEntry);
        }

        /// <summary>
        /// Very complex function, shifts a road based on direction of car if the car enters
        /// (onExit = false) the road bound or if the car exits (onExit = true) the road bound
        /// but unsuccesfully. Exceptions are when the car on enter (onExit=false) is entering
        /// or leaving the start road or the end road.
        /// </summary>
        private void TryShiftRoad(GameObject roadCollidedOn, bool onExit, bool sameExitAsEntry)
        {
            //define wheter we are going forward by checking if we hit bound on our road
            bool roadOnHit = roadCollidedOn == roadOn.gameObject;

            //Dont shift the road if the car is on entering bound(onExit = false) we are entering or leaving a start or end
            if (EnteringStartOrEnd(onExit, roadOnHit) || LeavingStartOrEnd(onExit, roadOnHit))
                return;

            //define succesfull exit by whether the car was exiting the bound not on the same side as the entry
            bool succesFullExit = onExit && !sameExitAsEntry;

            //define whether we are on start or end
            bool onStart = trackIndexOn == 0;
            bool onEnd = trackIndexOn == trackPlaying.RoadCount - 1;

            /*if the car had a succesfull exit of the bound, we can increase
            or decrease our trackIndexOn and check for start or end reached
            but don't shift the road*/
            if (succesFullExit)
            {
                UpdateRoadOn(roadOnHit);
                return;
            }

            bool unSuccesfullExitTowardsStart = !roadOnHit && (onExit && !succesFullExit);
            bool unSuccesfullExitTowardsEnd = roadOnHit && (onExit && !succesFullExit);

            /*if we are unsuccesfully exiting toward start when on
            start or unsuccesfully exiting towards end when on end we dont have to shift roads*/
            if ((unSuccesfullExitTowardsEnd && onStart) || (unSuccesfullExitTowardsStart && onEnd))
                return;

            /*forward shift is defined by whether the car is moving forward (roadOnHit=true) or
             the car had an unsuccesfull exit towards start and the car did not have an unsuccesfull
             exit towards the end*/
            bool forwardShift = (roadOnHit || unSuccesfullExitTowardsStart) && !unSuccesfullExitTowardsEnd;
            DoRoadShift(forwardShift);
        }

        /// <summary>
        /// Shifts the road by moving the bottom road to the front or the front road to
        /// the bottom based on given forwardShift value
        /// </summary>
        /// <param name="forwardShift"></param>
        private void DoRoadShift(bool forwardShift)
        {
            //shift top or bottom road based on forward shift value
            int roadIndex = forwardShift ? roads.Length - 1 : 0;
            Road shiftingRoad = roads[roadIndex];
            shiftingRoad.Shift(roads.Length * (forwardShift ? roadLength : -roadLength));

            //shift roads array based on whether we are going forward or not
            if (forwardShift)
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
        }

        /// <summary>
        /// Updateds roadOn value and trackIndexOn based on given forward value
        /// </summary>
        /// <param name="forward"></param>
        private void UpdateRoadOn(bool forward)
        {
            trackIndexOn += forward ? 1 : -1;
            //define whether we are on start or end
            bool onStart2 = trackIndexOn == 0;
            bool onEnd2 = trackIndexOn == trackPlaying.RoadCount - 1;
            //if we are on the starting road and are moving backward, we set road on to bottom one
            if (onStart2 && !forward)
            {
                roadOn = roads[2];
            } //if we are on the end of the road and are moving forward we set road on to top one
            else if (onEnd2 && forward)
            {
                roadOn = roads[0];
            }
            else
            {
                //on default, road on is always the middle one of the 3
                roadOn = roads[1];
            }
            Debug.LogError(trackIndexOn);
        }

        /// <summary>
        /// returns, based on direction, if we are entering start or end
        /// </summary>
        /// <returns></returns>
        private bool EnteringStartOrEnd(bool onExit, bool forward)
        {
            bool enteringStart_Enter = !onExit && (trackIndexOn - 1 == 0) && !forward;
            bool enteringEnd_Enter = !onExit && (trackIndexOn + 2 == trackPlaying.RoadCount) && forward;
            return enteringStart_Enter || enteringEnd_Enter;
        }

        /// <summary>
        /// returns, based on direction whether we are leaving start or end
        /// </summary>
        /// <returns></returns>
        private bool LeavingStartOrEnd(bool onExit, bool forward)
        {
            bool leavingStart_Enter = !onExit && trackIndexOn == 0 && forward;
            bool leavingStart_Exit = onExit && trackIndexOn == 1 && !forward;
            bool leavingEnd_Enter = !onExit && trackIndexOn == trackPlaying.RoadCount - 1 && !forward;
            bool leavingEnd_Exit = onExit && trackIndexOn + 1 == trackPlaying.RoadCount - 1 && forward;
            return leavingStart_Enter || leavingEnd_Enter || leavingStart_Exit || leavingEnd_Exit;
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