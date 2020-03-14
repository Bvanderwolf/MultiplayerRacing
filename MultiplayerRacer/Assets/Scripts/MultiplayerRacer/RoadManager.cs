using Photon.Pun;
using System.Collections.Generic;
using MultiplayerRacerEnums;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private Road[] roads;
        [SerializeField] private RaceTrack[] tracks;
        [SerializeField] private Color readyColor;

        private Road roadOn;
        private GameObject myCarSpawn;
        private GameObject myCar;
        private PhotonView PV;
        private Color unReadyColor;
        private float roadLength;

        private Dictionary<string, RaceTrack> raceTrackDict;
        private RaceTrack trackPlaying;
        private int trackIndexOn = 0;

        public static readonly List<Sprite[]> RoadProps = new List<Sprite[]>();
        public static readonly List<string> PropConfiguration = new List<string>();

        private void Awake()
        {
            PV = GetComponent<PhotonView>();

            LoadRoadProps();
            //SetupRoadValues();
            SetupSceneRelations();
            Testing();
        }

        private void Testing()
        {
            raceTrackDict = new Dictionary<string, RaceTrack>();
            foreach (RaceTrack track in tracks) raceTrackDict.Add(track.Name, track);

            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].CheckMaxRoadTypes();
                tracks[i].CheckForStartAndEnd();
            }

            GameObject.Find("Car").GetComponent<Racer>().OnRoadBoundInteraction += DoRoadShift;
            trackPlaying = raceTrackDict["Default"];

            //set other roads their type based on next road types to come
            roadOn = roads[2];
            roadOn.SetupRoad(trackPlaying.RoadTypes, trackIndexOn);
            roads[1].SetupRoad(trackPlaying.RoadTypes, 1);
            roads[0].SetupRoad(trackPlaying.RoadTypes, 2);
            roadLength = roadOn.MainBounds.size.y;
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

        //load in all necessary road props for the roads to manage
        private void LoadRoadProps()
        {
            string path = "Sprites/Roads/Props/";

            RoadProps.Add(new Sprite[4] {
                Resources.Load<Sprite>(path + "Container_A"),
                Resources.Load<Sprite>(path + "Container_B"),
                Resources.Load<Sprite>(path + "Container_C"),
                Resources.Load<Sprite>(path + "Container_A")
            });
            RoadProps.Add(new Sprite[1] {
                Resources.Load<Sprite>(path + "Crate")
            });
            RoadProps.Add(new Sprite[2] {
                Resources.Load<Sprite>(path + "Czech_Hdgehog_A"),
                Resources.Load<Sprite>(path + "Czech_Hdgehog_B")
            });
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

            trackPlaying = raceTrackDict[InRoomManager.Instance.NameOfTrackChoosen];
            roadOn = roads[2];
            roadOn.SetupRoad(trackPlaying.RoadTypes, trackIndexOn);
            roads[1].SetupRoad(trackPlaying.RoadTypes, trackIndexOn + 1);
            roads[0].SetupRoad(trackPlaying.RoadTypes, trackIndexOn + 2);
            roadLength = roadOn.MainBounds.size.y;
        }

        /// <summary>
        /// Sets up road manager values related to scene interactions
        /// </summary>
        private void SetupSceneRelations()
        {
            if (PhotonNetwork.IsConnected)
            {
                //start listeneing to room manager events
                InRoomManager.Instance.OnReadyStatusChange += OnReadyStatusChanged;
                InRoomManager.Instance.OnSceneReset += OnSceneHasReset;
                InRoomManager.Instance.OnGameStart += OnRaceStarted;
                InRoomManager.Instance.OnGameRestart += OnGameHasRestart;
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
            roadOn.SetCarSpawnsInactive();
        }

        private void OnGameHasRestart()
        {
            //reset track index on to start of track
            trackIndexOn = 0;
            //reset positions of roads
            roads[0].transform.localPosition = new Vector3(0, roadLength);
            roads[1].transform.localPosition = new Vector3(0, 0);
            roads[2].transform.localPosition = new Vector3(0, -roadLength);

            //set roads their type based on next road types to come
            roads[2].SetupRoad(trackPlaying.RoadTypes, trackIndexOn);
            roads[1].SetupRoad(trackPlaying.RoadTypes, trackIndexOn + 1);
            roads[0].SetupRoad(trackPlaying.RoadTypes, trackIndexOn + 2);

            roadOn = roads[2];
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

            //define succesfull exit by whether the car was exiting the bound not on the same side as the entry
            bool succesFullExit = onExit && !sameExitAsEntry;

            /*if the car had a succesfull exit of the bound, we can increase
            or decrease our trackIndexOn and check for start or end reached
            but don't shift the road*/
            if (succesFullExit)
            {
                UpdateRoadOn(roadOnHit);
                return;
            }

            //define whether we are on start or end
            bool onStart = trackIndexOn == 0;
            bool onEnd = trackIndexOn == trackPlaying.RoadCount - 1;

            //Dont shift the road if the car is on entering bound(onExit = false) we are entering or leaving a start or end
            if (EnteringStartOrEnd(onExit, roadOnHit) || LeavingStartOrEnd(onExit, roadOnHit))
                return;

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
            UpdateRoads(forwardShift, onExit);
        }

        /// <summary>
        /// Shifts the road by moving the bottom road to the front or the front road to
        /// the bottom based on given forwardShift value and updates roads array
        /// </summary>
        private void UpdateRoads(bool forwardShift, bool onExit)
        {
            //shift top or bottom road based on forward shift value
            int roadIndex = forwardShift ? roads.Length - 1 : 0;
            Road shiftingRoad = roads[roadIndex];

            //new y position for shifting road is based on it being a forward shift or not
            float y = roads.Length * (forwardShift ? roadLength : -roadLength);
            shiftingRoad.Shift(y);

            //update the type of this shifting road
            UpdateShiftedRoadType(shiftingRoad, forwardShift, onExit);

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
        /// Tries updating the road type of given road based on forward Shift,
        /// keeping in account the road count of the track playing
        /// </summary>
        /// <param name="shiftedRoad"></param>
        private void UpdateShiftedRoadType(Road shiftingRoad, bool forwardShift, bool onExit)
        {
            //if the road was shifted on exit, the offset is smaller than if it was on entering
            int offset = onExit ? 1 : 2;
            //index of the type for shifting road is either 2 indexes ahead or behind, based on forwardshift or not
            int newShiftingRoadTypeIndex = forwardShift ? trackIndexOn + offset : trackIndexOn - offset;
            shiftingRoad.SetupRoad(trackPlaying.RoadTypes, newShiftingRoadTypeIndex);
        }

        /// <summary>
        /// Updateds roadOn value and trackIndexOn based on given forward value
        /// </summary>
        /// <param name="forward"></param>
        private void UpdateRoadOn(bool forward)
        {
            trackIndexOn += forward ? 1 : -1;
            //define whether we are on start or end
            bool onStart = trackIndexOn == 0;
            bool onEnd = trackIndexOn == trackPlaying.RoadCount - 1;
            //if we are on the starting road and are moving backward, we set road on to bottom one
            if (onStart && !forward)
            {
                roadOn = roads[2];
            } //if we are on the end of the road and are moving forward we set road on to top one
            else if (onEnd && forward)
            {
                roadOn = roads[0];
            }
            else
            {
                //on default, road on is always the middle one of the 3
                roadOn = roads[1];
            }
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