using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class GameUI : MultiplayerRacerUI
    {
        [SerializeField] private GameObject readyUpInfo;
        [SerializeField] private GameObject eventText;
        [SerializeField] private GameObject leaderBoard;
        [SerializeField] private Transform debug;
        [SerializeField] private KeyCode readyUpKey;
        [SerializeField] private KeyCode readyUpResetKey;

        private const float READYUP_INFO_SHOW_DELAY = 0.75f;
        private const float RACE_END_OPTIONS_SHOW_DELAY = 4f;
        private const float EVENT_TEXT_FADE_DELAY = 2f;
        private string readyUpResetText;

        private PhotonView PV;

        private Queue<string> eventTextBuffer = new Queue<string>();
        private bool showingEventText = false;

        private static Text distanceToRemoteText;
        private static Text positionCatchupFactorText;
        private static Text angleToRemoteText;
        private static Text angleCatchupFactorText;
        private static Text lagText;

        protected override void Awake()
        {
            base.Awake();
            PV = GetComponent<PhotonView>();
            readyUpResetText = $"Press {readyUpResetKey} key to start Ready up";

            distanceToRemoteText = debug.Find("DistToRemote").GetComponent<Text>();
            positionCatchupFactorText = debug.Find("PositionCatchup").GetComponent<Text>();
            angleToRemoteText = debug.Find("AngleToRemote").GetComponent<Text>();
            angleCatchupFactorText = debug.Find("AngleCatchup").GetComponent<Text>();
            lagText = debug.Find("Lag").GetComponent<Text>();
        }

        /// <summary>
        /// Shows text on screen
        /// </summary>
        /// <param name="text"></param>
        public void ShowText(string text)
        {
            if (!showingEventText)
            {
                showingEventText = true;
                animations.PopupText(eventText, text, TryDequeueEvenTextBuffer, EVENT_TEXT_FADE_DELAY);
            }
            else
            {
                eventTextBuffer.Enqueue(text);
            }
        }

        /// <summary>
        /// Tries starting a popup text if there is text in the
        /// eventTextBuffer
        /// </summary>
        private void TryDequeueEvenTextBuffer()
        {
            if (eventTextBuffer.Count != 0)
            {
                animations.PopupText(
                    eventText,
                    eventTextBuffer.Dequeue(),
                    TryDequeueEvenTextBuffer,
                    EVENT_TEXT_FADE_DELAY);
            }
            else
            {
                showingEventText = false;
            }
        }

        /// <summary>
        /// tries showing the leaderboard
        /// </summary>
        public void ShowLeaderboard(Player[] finishedPlayersOrdered)
        {
            if (leaderBoard != null)
            {
                Transform itemsTF = leaderBoard.transform.Find("Items");
                //loop through "finishedPlayersOrdered length" ammount of children and set them up
                for (int ci = 0; ci < finishedPlayersOrdered.Length; ci++)
                {
                    Transform child = itemsTF.GetChild(ci);
                    //set nickname
                    Text nickname = child.Find("Name").GetComponent<Text>();
                    nickname.text = finishedPlayersOrdered[ci].NickName;
                    //split miliseconds from finish time
                    string finishTime = (string)finishedPlayersOrdered[ci].CustomProperties["FinishTime"];
                    finishTime = finishTime.Split('.')[0];
                    //set time
                    Text time = child.Find("Time").GetComponent<Text>();
                    time.text = finishTime;
                }
                leaderBoard.SetActive(true);
            }
            else Debug.LogError("wont show leaderboard :: is null");
        }

        /// <summary>
        /// tries hiding the leaderboard
        /// </summary>
        public void HideLeaderBoard()
        {
            if (leaderBoard != null)
            {
                leaderBoard.SetActive(false);
            }
            else Debug.LogError("wont hide leaderboard :: is null");
        }

        /// <summary>
        /// Sends rpc to all clients to show ready up info
        /// Can only be excecuted by the master client
        /// </summary>
        public void SendShowReadyUpInfo()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PV.RPC("ShowReadyUpInfo", RpcTarget.AllViaServer);
            }
        }

        /// <summary>
        /// should be called by the master client when the race has ended
        /// and the game can be reset or cleared
        /// </summary>
        public void ShowRaceEndedOptionsWithDelay()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(ShowRaceEndedOptionsDelayed());
            }
        }

        public void StartCountDownForRaceStart(Action endAction, Func<bool> check)
        {
            //hide unnecessary UI Elements
            HideExitButton();
            HideRoomStatus();

            animations.CountDown(countdownText, InRoomManager.COUNTDOWN_LENGTH, endAction, check);
        }

        private IEnumerator ShowRaceEndedOptionsDelayed()
        {
            //after delay show event text
            yield return new WaitForSeconds(RACE_END_OPTIONS_SHOW_DELAY);
            eventText.transform.localScale = Vector3.one;

            //define our restart key and our end game key
            KeyCode restartKey = readyUpKey;
            KeyCode endGameKey = readyUpResetKey;

            //set text to show the player the options
            string text = $"Press {restartKey} to restart the game or {endGameKey} to end the game";
            eventText.GetComponent<Text>().text = text;

            /*define our check as the player not pressing the end game key, so that
            if pressed our result is turned in with succes=false which means game end*/
            Func<bool> check = () => !Input.GetKeyDown(endGameKey);

            //get our car game object and wait for player input
            GameObject car = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
            car.GetComponent<RacerInput>().WaitForPlayerInput(
                restartKey,
                (succes) => SetRaceEndOptionChoosenResult(succes),
                check);
        }

        private IEnumerator SetupReadyUpWithDelay()
        {
            //after delay show ready up info
            yield return new WaitForSeconds(READYUP_INFO_SHOW_DELAY);
            readyUpInfo.SetActive(true);

            //define count and room for check in playercount differnce during wait for player input
            Room room = PhotonNetwork.CurrentRoom;
            int count = room.PlayerCount;

            //get our car game object and wait for player input
            GameObject car = (GameObject)PhotonNetwork.LocalPlayer.TagObject;
            car.GetComponent<RacerInput>().WaitForPlayerInput(
                readyUpKey,
                (succes) => SetReadyUpResult(succes),
                () => count == room.PlayerCount);
        }

        /// <summary>
        /// Handles setup after master client choose from his race end options
        /// </summary>
        /// <param name="succes"></param>
        private void SetRaceEndOptionChoosenResult(bool succes)
        {
            eventText.transform.localScale = Vector3.zero;
            if (succes)
            {
                //if succes is true, the player pressed the restart key and we can restart the game
                InRoomManager.Instance.SendGameRestart();
            }
            else
            {
                /*if succes is fals, either the master client pressed the end game
                key or waited to long which results in us clearing the room*/
                InRoomManager.Instance.SendAllLeaveRoom();
            }
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
                PV.RPC("ShowReadyUpInfo", RpcTarget.AllViaServer);
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
            /*it can happen that a player is already ready when setting up ready up,
             after a reset. To handle this we just set this value to false*/
            if (InRoomManager.Instance.IsReady)
            {
                InRoomManager.Instance.SetReady(false);
            }

            //start the setup coroutine
            StartCoroutine(SetupReadyUpWithDelay());
        }

        public static void SetDistanceToRemote(float value)
        {
            distanceToRemoteText.text = "DistToRemote: " + value.ToString("0.##");
        }

        public static void SetPositionCatchupFactor(float value)
        {
            positionCatchupFactorText.text = "PositionCatchup: " + (value < 0 ? 0 : value).ToString("0.##");
        }

        public static void SetAngleToRemote(float value)
        {
            angleToRemoteText.text = "AngleToRemote: " + value.ToString("0.##");
        }

        public static void SetAngleCatchupFactor(float value)
        {
            angleCatchupFactorText.text = "AngleCatchup: " + (value < 0 ? 0 : value).ToString("0.##");
        }

        public static void SetLag(float value)
        {
            lagText.text = "Lag:" + value;
        }
    }
}