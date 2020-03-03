using UnityEngine;
using Photon.Pun;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback
    {
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
            //kan Photonview reference hieruit halen
            //kan behouden voor references naar vanalles en nog wat
        }
    }
}