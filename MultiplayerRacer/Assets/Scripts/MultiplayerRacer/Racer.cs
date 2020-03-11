using UnityEngine;
using Photon.Pun;
using System;
using MultiplayerRacerEnums;
using ExitGames.Client.Photon;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback, IPunObservable
    {
        [SerializeField] private GameObject carCamera;
        [SerializeField] private Transform remoteCar;
        [SerializeField] private bool canRace;

        public GameObject Camera => carCamera;
        public bool CanRace => canRace;

        /// <summary>
        /// event that will be called on road bound enter and on
        /// same entry as exit
        /// </summary>
        public event Action<GameObject, bool, bool> OnRoadBoundInteraction;

        private Quaternion cameraRotation;
        private Rigidbody2D RB;
        private PhotonView PV;

        private RacerInput remoteRacerInput;
        private Vector2 remotePosition;
        private float remoteInputV;
        private float remoteInputH;

        private double lastSnapShot;
        private float delta;

        private float boundEnterAxisValue;
        private const float BOUND_AXIS_ERROR_MARGIN = 0.5f;
        private const float INTERPOLATION_PERIOD = 0.1f;

        private TimeSpan startTime;

        private void Awake()
        {
            RB = GetComponent<Rigidbody2D>();
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
        }

        //Update during render frames
        private void FixedUpdate()
        {
            //update other clients their car
            if (!PV.IsMine)
            {
                remoteRacerInput.SimulateRemote(remoteInputV, remoteInputH);
                //RB.position = Vector2.Lerp(RB.position, remotePosition, Time.deltaTime);
                remoteCar.position = transform.position + (Vector3)(remotePosition - RB.position);
            }
        }

        private void LateUpdate()
        {
            //for now simply reset camera rotation after all updates
            carCamera.transform.rotation = cameraRotation;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(RB.position);
                stream.SendNext(RacerInput.Gas);
                stream.SendNext(RacerInput.Steer);
            }
            else
            {
                float history = Time.time - INTERPOLATION_PERIOD;
                if (history > lastSnapShot && history < info.SentServerTime)
                {
                    delta = Mathf.Abs((float)(info.SentServerTime - lastSnapShot));
                }
                lastSnapShot = info.SentServerTime;

                remotePosition = (Vector2)stream.ReceiveNext();
                remoteInputV = (float)stream.ReceiveNext();
                remoteInputH = (float)stream.ReceiveNext();
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            PhotonView _PV = info.photonView;
            PV = _PV;
            if (_PV.IsMine)
            {
                //to stuff with photon message info
                info.Sender.TagObject = this.gameObject;

                //subcribe to in room events
                InRoomManager.Instance.OnGameStart += OnRacerCanStart;
                InRoomManager.Instance.OnSceneReset += OnRacerReset;

                //make others disable our camera
                _PV.RPC("DisableCamera", RpcTarget.OthersBuffered, _PV.ViewID);
            }
            else
            {
                remoteRacerInput = GetComponent<RacerInput>();
            }
        }

        /// <summary>
        /// should be called when the scene resets, resets rigidbody of car
        /// </summary>
        /// <param name="scene"></param>
        private void OnRacerReset(MultiplayerRacerScenes scene)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            RB.velocity = Vector2.zero;
            RB.angularVelocity = 0;
            RB.rotation = 0;
            canRace = false;
        }

        /// <summary>
        /// should be called when the car is able to race
        /// </summary>
        private void OnRacerCanStart()
        {
            canRace = true;
            startTime = DateTime.UtcNow.TimeOfDay;
        }

        private void AddFinishInfoToHashTable(string time)
        {
            Hashtable table = new Hashtable();
            table.Add("FinishTime", time);
            PhotonNetwork.LocalPlayer.SetCustomProperties(table);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!PV.IsMine)
                return;

            if (collision.tag == "RoadBound")
            {
                //store y value (can be x to depending on orientation of bound)
                boundEnterAxisValue = transform.position.y;
                //fire event returning the parent gameobject which is the RoadPiece
                OnRoadBoundInteraction(collision.transform.parent.gameObject, false, false);
            }
            else if (collision.tag == "Finish")
            {
                //get the difference between end and start time
                TimeSpan endTime = DateTime.UtcNow.TimeOfDay;
                string time = endTime.Subtract(startTime).ToString("c");

                //add finish time to hash table
                AddFinishInfoToHashTable(time);
                //set canRace to false so the player can't input but the car can ease out
                canRace = false;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!PV.IsMine)
                return;

            if (collision.tag == "RoadBound")
            {
                //it is possible that when resetting we exit a bound. we want to account for this
                bool boundExitAfterReset = !canRace;
                //get exit y value (can be x to depending on orientation of bound)
                float boundExitAxisValue = transform.position.y;
                float diff = boundEnterAxisValue - boundExitAxisValue;
                //define whether the exit direction is the same as the direction of entry
                bool sameExitAsEntry = diff >= -BOUND_AXIS_ERROR_MARGIN && diff <= BOUND_AXIS_ERROR_MARGIN;
                //if the exit is the same as the entry we need to shift the road back
                OnRoadBoundInteraction(collision.transform.parent.gameObject, true, sameExitAsEntry || boundExitAfterReset);
            }
        }

        [PunRPC]
        private void DisableCamera(int viewID)
        {
            //get the camera of the car in our scene based on viewID and set it to inactive
            PhotonView PV = PhotonView.Find(viewID);
            GameObject carCamera = PV.GetComponent<Racer>()?.Camera;
            if (carCamera != null)
            {
                carCamera.SetActive(false);
            }
            else Debug.LogError("Won't set car camera to inactive :: carCamera object is null");
        }
    }
}