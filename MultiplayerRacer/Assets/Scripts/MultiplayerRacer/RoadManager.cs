using Photon.Pun;
using UnityEngine;

namespace MultiplayerRacer
{
    using MultiplayerRacerScenes = InRoomManager.MultiplayerRacerScenes;

    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] roads;
        [SerializeField] private Color readyColor;

        private GameObject roadOn; //changes when roads get shifted
        private GameObject myCarSpawn;
        private GameObject myCar;
        private Color unReadyColor;
        private float roadLength;

        private void Awake()
        {
            //the player starts on the middle road
            roadOn = roads[1];
            roadLength = roadOn.transform.Find("Road").GetComponent<SpriteRenderer>().bounds.size.y;

            if (PhotonNetwork.IsConnected)
            {
                //start listeneing to onreadystatuschange and scenereset events
                InRoomManager.Instance.OnReadyStatusChange += OnReadyStatusChanged;
                InRoomManager.Instance.OnSceneReset += OnSceneHasReset;
                InRoomManager.Instance.OnGameStart += OnRaceStarted;
                //place the car spawns on the road for players and get our own car spawn
                myCarSpawn = SetupCarSpawns();
                myCar = PhotonNetwork.Instantiate("Prefabs/Car", myCarSpawn.transform.position, Quaternion.identity);
                myCar.GetComponent<Racer>().OnRoadBoundHit += OnRoadBoundHit; //subscribe to road bound hit event
                unReadyColor = myCarSpawn.GetComponent<SpriteRenderer>().color; //save default color as unready color
            }
            else Debug.LogError("Wont do car setup :: not connected to photon network");
        }

        private void OnRaceStarted()
        {
            //set all car spawns to an inactive state
            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                carSpawnTransform.GetChild(ci).gameObject.SetActive(false);
            }
        }

        private void OnSceneHasReset(MultiplayerRacerScenes scene)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

            //loop through all car spawns and try resetting them
            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                GameObject carSpawn = carSpawnTransform.GetChild(ci).gameObject;
                carSpawn.GetComponent<SpriteRenderer>().color = unReadyColor;
                carSpawn.transform.localPosition = Vector3.zero;
                /*if this car spawn is outside the player count bound,
                this car spawn will not be used so it can be made inactive*/
                if ((ci + 1) > playerCount)
                {
                    carSpawn.SetActive(false);
                }
            }

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

            GetComponent<PhotonView>().RPC(
                "UpdateReadyStatus",
                RpcTarget.AllViaServer,
                InRoomManager.Instance.NumberInRoom,
                ready);
        }

        /// <summary>
        /// should be called when our car hits a road bound
        /// </summary>
        private void OnRoadBoundHit(GameObject road)
        {
            ShiftRoad(road);
        }

        /// <summary>
        /// shifts roads based on bound hit
        /// </summary>
        private void ShiftRoad(GameObject road)
        {
            //the bound hit can only be of the road on or the one below it
            bool roadOnHit = road == roadOn;
            int roadIndex = roadOnHit ? roads.Length - 1 : 0;
            GameObject shiftingRoad = roads[roadIndex];
            shiftingRoad.transform.localPosition += new Vector3(0, roads.Length * (roadOnHit ? roadLength : -roadLength));

            if (roadOnHit)
            {
                roadIndex = roads.Length - 1;
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
            roadOn = roads[1];
        }

        /// <summary>
        /// Sets up car spawns and returns the gameobject of the one
        /// used by your car. Will return null when failed.
        /// </summary>
        /// <returns></returns>
        private GameObject SetupCarSpawns()
        {
            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            Transform road = roadOn.transform.Find("Road");
            if (carSpawnTransform == null || road == null)
            {
                Debug.LogError("Wont setup car spawns :: car spawn or road is null");
                return null;
            }
            int count = PhotonNetwork.CurrentRoom.PlayerCount;
            float width = road.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            float carSpawnWidthHalf = carSpawnTransform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size.x * 0.5f;
            float margin = (width - (carSpawnWidthHalf * count)) / (count + 1);
            float x = -(width * 0.5f) - (carSpawnWidthHalf * 0.5f);
            //loop through children based on count and place them on given position
            for (int ci = 0; ci < count; ci++)
            {
                x += margin + carSpawnWidthHalf;
                Transform tf = carSpawnTransform.GetChild(ci);
                Vector3 position = tf.localPosition;
                tf.localPosition = new Vector2(position.x + x, position.y);
                tf.gameObject.SetActive(true);
            }
            //return the position of the child based on our number in the room
            return carSpawnTransform.GetChild(InRoomManager.Instance.NumberInRoom - 1).gameObject;
        }

        /// <summary>
        /// gets a car spawn in scene based on given player number
        /// </summary>
        /// <param name="playerNumber"></param>
        /// <returns></returns>
        private Transform GetCarSpawn(int playerNumber)
        {
            if (playerNumber < 0 || playerNumber > MatchMakingManager.MAX_PLAYERS)
                return null;

            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            return carSpawnTransform.GetChild(playerNumber - 1);
        }

        [PunRPC]
        private void UpdateReadyStatus(int playerNumber, bool ready)
        {
            GetCarSpawn(playerNumber).GetComponent<SpriteRenderer>().color = ready ? readyColor : unReadyColor;
        }
    }
}