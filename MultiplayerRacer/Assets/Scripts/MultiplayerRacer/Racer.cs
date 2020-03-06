using UnityEngine;
using Photon.Pun;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback
    {
        [SerializeField] private GameObject carCamera;
        [SerializeField] private bool canRace;

        public GameObject Camera => carCamera;
        public bool CanRace => canRace;

        private Quaternion cameraRotation;

        private void Awake()
        {
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
        }

        private void LateUpdate()
        {
            //for now simply reset camera rotation after all updates
            carCamera.transform.rotation = cameraRotation;
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            PhotonView PV = info.photonView;
            if (PV.IsMine)
            {
                //to stuff with photon message info
                info.Sender.TagObject = this.gameObject;

                //subcribe to game start event
                InRoomManager.Instance.OnGameStart += OnRacerCanStart;

                //make others disable our camera
                PV.RPC("DisableCamera", RpcTarget.OthersBuffered, PV.ViewID);
            }
            //kan Photonview reference hieruit halen
            //kan behouden voor references naar vanalles en nog wat
        }

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