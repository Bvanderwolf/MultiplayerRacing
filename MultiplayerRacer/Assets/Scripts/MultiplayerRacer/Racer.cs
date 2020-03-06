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

        private const float SMOOTH_DELAY = 10f;

        private Quaternion cameraRotation;
        private Rigidbody2D rb;
        private PhotonView PV;

        private Vector2 remotePosition;
        private float remoteRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
        }

        //Update during render frames
        private void Update()
        {
            //update other clients their car
            if (!PV.IsMine)
            {
                rb.position = Vector2.Lerp(rb.position, remotePosition, Time.deltaTime * SMOOTH_DELAY);
                rb.rotation = Mathf.Lerp(rb.rotation, remoteRotation, Time.deltaTime * SMOOTH_DELAY);
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
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            Debug.LogError(lag);
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