using UnityEngine;
using Photon.Pun;

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

        private Quaternion cameraRotation;
        private Rigidbody2D rb;
        private PhotonView PV;

        private Vector2 remotePosition;
        private float remoteRotation;
        private float distance;
        private float angle;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
            //PhotonNetwork.SendRate = 40;
            //PhotonNetwork.SerializationRate = 20;
        }

        //Update during render frames
        private void FixedUpdate()
        {
            //update other clients their car
            if (!PV.IsMine)
            {
                float maxDistanceDelta = distance * (1.0f / PhotonNetwork.SerializationRate);//drawline gebruiken voor visual?
                rb.position = Vector2.MoveTowards(rb.position, remotePosition, maxDistanceDelta);
                float maxRotationDelta = angle * (1.0f / PhotonNetwork.SerializationRate);
                rb.rotation = Mathf.MoveTowards(rb.rotation, remoteRotation, maxRotationDelta);
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
                stream.SendNext(rb.position);
                stream.SendNext(rb.rotation);
                stream.SendNext(rb.velocity);
                stream.SendNext(rb.angularVelocity);
            }
            else
            {
                //if this is not our script we set the position and rotation of the racer
                remotePosition = (Vector2)stream.ReceiveNext();
                remoteRotation = (float)stream.ReceiveNext();

                //if the distance to the remote position is to far, teleport to it
                if (Vector2.Distance(rb.position, remotePosition) > minDistanceToTeleportAt)
                {
                    rb.position = remotePosition;
                }

                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

                rb.velocity = (Vector2)stream.ReceiveNext();
                remotePosition += rb.velocity * lag;
                distance = Vector2.Distance(rb.position, remotePosition);

                rb.angularVelocity = (float)stream.ReceiveNext();
                remoteRotation += rb.angularVelocity * lag;
                angle = Mathf.Abs(rb.rotation - remoteRotation);
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
            //kan gebruikt worden voor timestamp
        }

        /// <summary>
        /// should be called when the scene resets, resets rigidbody of car
        /// </summary>
        /// <param name="scene"></param>
        private void OnRacerReset(MultiplayerRacerScenes scene)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.rotation = 0;
        }

        /// <summary>
        /// should be called when the car is able to race
        /// </summary>
        private void OnRacerCanStart()
        {
            canRace = true;
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