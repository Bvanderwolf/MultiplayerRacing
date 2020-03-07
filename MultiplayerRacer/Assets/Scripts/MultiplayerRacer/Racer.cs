using UnityEngine;
using Photon.Pun;
using System;

namespace MultiplayerRacer
{
    using MultiplayerRacerScenes = InRoomManager.MultiplayerRacerScenes;

    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback, IPunObservable
    {
        [SerializeField] private GameObject carCamera;
        [SerializeField] private bool canRace;
        [SerializeField] private float minDistanceToTeleportAt;

        public GameObject Camera => carCamera;
        public bool CanRace => canRace;

        /// <summary>
        /// event that will be called on road bound enter and on
        /// same entry as exit
        /// </summary>
        public event Action<GameObject> OnRoadBoundInteraction;

        private Quaternion cameraRotation;
        private Rigidbody2D RB;
        private PhotonView PV;

        private Vector2 remotePosition;
        private float remoteRotation;
        private const float SMOOTH_DELAY = 10f;

        private float boundEnterAxisValue;
        private const float BOUND_AXIS_ERROR_MARGIN = 0.5f;

        private void Awake()
        {
            RB = GetComponent<Rigidbody2D>();
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
        }

        //Update during render frames
        private void Update()
        {
            //update other clients their car
            if (!PV.IsMine)
            {
                RB.position = Vector2.Lerp(RB.position, remotePosition, Time.deltaTime * SMOOTH_DELAY);
                RB.rotation = Mathf.Lerp(RB.rotation, remoteRotation, Time.deltaTime * SMOOTH_DELAY);
            }
        }

        private void LateUpdate()
        {
            if (!canRace)
                return;

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
            }
            else
            {
                //if this is not our script we set the position and rotation of the racer
                remotePosition = (Vector2)stream.ReceiveNext();
                remoteRotation = (float)stream.ReceiveNext();

                //if the distance to the remote position is to far, teleport to it
                if (Vector2.Distance(RB.position, remotePosition) > minDistanceToTeleportAt)
                {
                    RB.position = remotePosition;
                }
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

                //subcribe to game start event
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
        }

        /// <summary>
        /// should be called when the car is able to race
        /// </summary>
        private void OnRacerCanStart()
        {
            canRace = true;
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
                OnRoadBoundInteraction(collision.transform.parent.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!PV.IsMine)
                return;

            if (collision.tag == "RoadBound")
            {
                //get exit y value (can be x to depending on orientation of bound)
                float boundExitAxisValue = transform.position.y;
                float diff = boundEnterAxisValue - boundExitAxisValue;
                //define whether the exit direction is the same as the direction of entry
                bool sameExitAsEntry = diff >= -BOUND_AXIS_ERROR_MARGIN && diff <= BOUND_AXIS_ERROR_MARGIN;
                //if the exit is the same as the entry we need to shift the road back
                if (sameExitAsEntry)
                {
                    OnRoadBoundInteraction(collision.transform.parent.gameObject);
                }
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