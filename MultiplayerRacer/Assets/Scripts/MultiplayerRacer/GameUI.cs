using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class GameUI : MultiplayerRacerUI
    {
        [SerializeField] private GameObject readyUpInfo;
        [SerializeField] private KeyCode readyUpKey;
        [SerializeField] private KeyCode readyUpResetKey;

        private const float READYUP_INFO_SHOW_DELAY = 0.75f;
        private string readyUpResetText;

        protected override void Awake()
        {
            base.Awake();
            readyUpResetText = $"Press {readyUpResetKey} key to start Ready up";
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

        public void StartCountDownForRaceStart(Action endAction, Func<bool> check)
        {
            //hide unnecessary UI Elements
            HideExitButton();
            HideRoomStatus();

            animations.CountDown(countdownText, InRoomManager.COUNTDOWN_LENGTH, true, endAction, check);
        }

        private IEnumerator SetupReadyUpWithDelay()
        {
            //after delay show ready up info
            yield return new WaitForSeconds(READYUP_INFO_SHOW_DELAY);
            readyUpInfo.SetActive(true);

            //define count and room for check in playercount differnce during wait for player input
            Room room = PhotonNetwork.CurrentRoom;
            int count = room.PlayerCount;

            //get our car game object and if found wait for player input
            GameObject car = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
            car.GetComponent<RacerInput>().WaitForPlayerInput(
                readyUpKey,
                (succes) => SetReadyUpResult(succes),
                () => count == room.PlayerCount);
        }

        /// <summary>
        /// Handles setup after ready up result based on succes or not
        /// </summary>
        /// <param name="succes"></param>
        private void SetReadyUpResult(bool succes)
        {
            readyUpInfo.SetActive(false);
            if (succes)
            {
                InRoomManager.Instance.SetReady(true);
            }
            else
            {
                //either the player has not pressed the ready up key or a player has left
                OnReadyUpFailed();
            }
        }

        /// <summary>
        /// Handles setup after ready up reset based on succes or not
        /// </summary>
        /// <param name="succes"></param>
        private void SetReadyUpResetResult(bool succes)
        {
            if (succes)
            {
                //start ready up process again
                GetComponent<PhotonView>().RPC("ShowReadyUpInfo", RpcTarget.AllViaServer);
            }
            else
            {
                //Dead end, master client failed resetting. For now all players leave the room
                InRoomManager.Instance.SendAllLeaveRoom();
            }
        }

        private void OnReadyUpFailed()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //set readyUpInfo back to active
                readyUpInfo.SetActive(true);

                //Store our old ready up text and set ready up reset text
                Text readyUp = readyUpInfo.GetComponent<Text>();
                string readyUpText = readyUp.text;
                readyUp.text = readyUpResetText;

                //get our car game object and if found wait for player input
                GameObject car = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
                car.GetComponent<RacerInput>().WaitForPlayerInput(readyUpResetKey, (succes) =>
                {
                    readyUp.text = readyUpText; //reset ready up text
                    SetReadyUpResetResult(succes); //handle reset result
                });
            }
        }

        [PunRPC]
        private void ShowReadyUpInfo()
        {
            /*it can happen that a player is already ready when setting up ready up
             after a ready up reset. To handle this we just set his value to false*/
            if (InRoomManager.Instance.IsReady)
            {
                InRoomManager.Instance.SetReady(false);
            }

            //start the setup coroutine
            StartCoroutine(SetupReadyUpWithDelay());
        }
    }
}