using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace MultiplayerRacer
{
    public class GameUI : MultiplayerRacerUI
    {
        [SerializeField] private GameObject readyUpInfo;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Sends rpc to all clients to show ready up info
        /// Can only be excecuted by the master client
        /// </summary>
        public void SendShowReadyUpInfo()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GetComponent<PhotonView>().RPC("ShowReadyUpInfo", RpcTarget.AllViaServer);
            }
        }

        private IEnumerator ShowReadyUpInfoWithDelay()
        {
            yield return new WaitForSeconds(2f);
            readyUpInfo.SetActive(true);
        }

        [PunRPC]
        private void ShowReadyUpInfo()
        {
            StartCoroutine(ShowReadyUpInfoWithDelay());
        }
    }
}