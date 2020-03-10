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

        private Vector2 remotePosition;
        private float remoteRotation;
        private float moveTime;
        private float moveSpeed = 5f;
        private float lastMoveSpeed;

        private float boundEnterAxisValue;
        private const float BOUND_AXIS_ERROR_MARGIN = 0.5f;
        private const float MAX_DISTANCE_TO_REMOTE = 1.0f;
        private const float MAX_ANGLE_DIFFERENCE = 20f;

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
                //increase movetime so that the remote car keeps moving towards predicted location
                moveTime += Time.deltaTime * moveSpeed;
                //linearly interpolate between position and predicted remote position
                RB.position = Vector2.Lerp(RB.position, remotePosition, moveTime);
                RB.rotation = Mathf.Lerp(RB.rotation, remoteRotation, moveTime);
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
                //if this is our script we send the position and rotation
                stream.SendNext(RB.position);
                stream.SendNext(RB.rotation);
                stream.SendNext(RB.velocity);
                stream.SendNext(RB.angularVelocity);
            }
            else
            {
                //get difference in photon's current server time and its time when sending as lag
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                //store the remote position and rotation of other car
                remotePosition = (Vector2)stream.ReceiveNext();
                remoteRotation = (float)stream.ReceiveNext();
                //add to remote position the received velocity times the lag
                remotePosition += ((Vector2)stream.ReceiveNext() * lag);
                remoteRotation += ((float)stream.ReceiveNext() * lag);
                //if the distance to the remote position is to far, teleport to it
                if (Vector2.Distance(RB.position, remotePosition) > MAX_DISTANCE_TO_REMOTE)
                {
                    RB.position = remotePosition;
                }
                //if the difference between remote rotation and ours is to big, snap to it
                if ((Mathf.Abs(remoteRotation - RB.rotation) > MAX_ANGLE_DIFFERENCE))
                {
                    RB.rotation = remoteRotation;
                }

                moveTime = 0;
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