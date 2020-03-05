using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace MultiplayerRacer
{
    public class GameUI : MultiplayerRacerUI
    {
        [SerializeField] private GameObject readyUpInfo;
        [SerializeField] private KeyCode readyUpKey;

        private const float READYUP_INFO_SHOW_DELAY = 0.75f;

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

        private IEnumerator SetupReadyUpWithDelay()
        {
            //after delay show ready up info
            yield return new WaitForSeconds(READYUP_INFO_SHOW_DELAY);
            readyUpInfo.SetActive(true);

            //get our car game object and if found wait for player input
            GameObject car = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
            car.GetComponent<RacerInput>().WaitForPlayerInput(readyUpKey, (succes) => SetReadyUpResult(succes));
        }

        /// <summary>
        /// Handles setup after ready up result based on succes or not
        /// </summary>
        /// <param name="succes"></param>
        private void SetReadyUpResult(bool succes)
        {
            if (succes)
            {
                Debug.LogError("this player has succesfully pressed " + readyUpKey);
                InRoomManager.Instance.SetReady(true);
            }
            else
            {
                Debug.LogError("this player has not pressed " + readyUpKey);
                //event waarbij master client return moet drukken om nieuwe ready up te starten
            }
            readyUpInfo.SetActive(false);
        }

        [PunRPC]
        private void ShowReadyUpInfo()
        {
            StartCoroutine(SetupReadyUpWithDelay());
        }
    }
}