using UnityEngine;
using Photon.Pun;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback
    {
        [SerializeField] private GameObject carCamera;

        public GameObject Camera => carCamera;

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            PhotonView PV = info.photonView;
            if (PV.IsMine)
            {
                info.Sender.TagObject = this.gameObject;
                PV.RPC("DisableCamera", RpcTarget.OthersBuffered, PV.ViewID);
            }
            //kan Photonview reference hieruit halen
            //kan behouden voor references naar vanalles en nog wat
        }

        [PunRPC]
        private void DisableCamera(int viewID)
        {
            //get the camera of the car based on viewID and set it to inactive
            PhotonView PV = PhotonView.Find(viewID);
            GameObject carCamera = PV.GetComponent<Racer>()?.Camera;
            if (carCamera != null)
            {
                carCamera.SetActive(false);
                Debug.LogError("setting camera of other player to inactive");
            }
            else Debug.LogError("Won't set car camera to inactive :: carCamera object is null");
        }
    }
}