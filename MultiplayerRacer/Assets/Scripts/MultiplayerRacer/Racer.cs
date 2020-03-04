using UnityEngine;
using Photon.Pun;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback
    {
        [SerializeField] private GameObject carCamera;

        public GameObject Camera => carCamera;

        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            info.Sender.TagObject = this.gameObject;
            PhotonView PV = info.photonView;
            PV.RPC("DisableCamera", RpcTarget.OthersBuffered, PV.ViewID);
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
            }
        }
    }
}