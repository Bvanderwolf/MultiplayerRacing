﻿using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private Button exitButton;

        [SerializeField] private GameObject roomStatus;
        [SerializeField] private GameObject readyStatus;
        [SerializeField] private GameObject countdownText;

        [SerializeField] private Color connectColor = new Color(0, 0.75f, 0);

        [SerializeField] private Color disconnectColor = new Color(0.75f, 0, 0);
        [SerializeField] private float roomStatRoundness = 0.25f;

        private LobbyUIAnimation animations;

        private void Awake()
        {
            //set up ui animations reference
            animations = GetComponent<LobbyUIAnimation>();
            if (animations == null)
            {
                animations = gameObject.AddComponent<LobbyUIAnimation>();
            }
        }

        //sets up connect button and returns if succeeded
        public void SetupConnectButton(Action<Button> clickAction)
        {
            //if connect button is null try finding it and if not found return
            if (connectButton == null)
                if (!FindAndSetConnectButtonReference())
                    return;

            //handle edgecase where connect button is inactive and room status enabled
            if (!connectButton.gameObject.activeInHierarchy)
            {
                connectButton.gameObject.SetActive(true);
                roomStatus.SetActive(false);
            }
            connectButton.onClick.AddListener(() => clickAction.Invoke(connectButton));
        }

        /// <summary>
        /// Updates the connect button its color based on given connected value
        /// </summary>
        /// <param name="connected"></param>
        public void UpdateConnectColor(bool connected)
        {
            if (connectButton == null)
                if (!FindAndSetConnectButtonReference())
                    return;

            //show green or red button color for succesfull/unsuccesfull connection with master
            connectButton.image.color = connected ? connectColor : disconnectColor;
        }

        /// <summary>
        /// Updates the connected button text based on the given connected value
        /// </summary>
        /// <param name="connected"></param>
        public void UpdateConnectStatus(bool connected)
        {
            if (connectButton == null)
                if (!FindAndSetConnectButtonReference())
                    return;

            string destination = connected ? "Room" : "Master";
            //replace the button text with updated text based on new status
            Text textComponent = connectButton.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                string text = textComponent.text;
                textComponent.text = text.Remove(text.IndexOf(ConnectDestination())) + destination;
            }
        }

        /// <summary>
        /// should return the destination from the connect button text
        /// if failed, returns an empty string
        /// </summary>
        /// <returns></returns>
        public string ConnectDestination()
        {
            if (connectButton == null)
                if (!FindAndSetConnectButtonReference())
                    return "";

            string text = connectButton.GetComponentInChildren<Text>().text;
            return text.Substring(text.LastIndexOf(' ') + 1);
        }

        /// <summary>
        /// Sets up room status panel with nickname
        /// </summary>
        /// <param name="nickname"></param>
        private void UpdateNickname(string nickname)
        {
            Text nicknameComp = roomStatus.transform.Find("Nickname")?.GetComponent<Text>();
            if (nicknameComp != null)
            {
                nicknameComp.text = $"Nickname: {nickname}";
            }
            else Debug.LogError("Wont update nickname :: text component is null");
        }

        /// <summary>
        /// sets up room status panel with room information
        /// </summary>
        /// <param name="playercount"></param>
        public void UpdateRoomInfo(Room room)
        {
            Text playerCountComp = roomStatus.transform.Find("Playercount")?.GetComponent<Text>();
            if (playerCountComp != null)
            {
                playerCountComp.text = $"Players: {room.PlayerCount}/{MatchMakingManager.MAX_PLAYERS}";
            }
            else Debug.LogError("Wont update player count :: text component is null");

            Text roomNameComp = roomStatus.transform.Find("RoomName")?.GetComponent<Text>();
            if (roomNameComp != null)
            {
                roomNameComp.text = $"RoomName: {room.Name}";
            }
            else Debug.LogError("Wont update room name :: text component is null");
        }

        /// <summary>
        /// sets up room status panel with ismasterclient status
        /// </summary>
        /// <param name="ismaster"></param>
        private void UpdateIsMasterclient()
        {
            Text isMasterclientComp = roomStatus.transform.Find("Ismasterclient")?.GetComponent<Text>();
            if (isMasterclientComp != null)
            {
                string yesOrNo = PhotonNetwork.IsMasterClient ? "yes" : "no";
                isMasterclientComp.text = $"IsMasterclient: {yesOrNo}";
            }
            else Debug.LogError("Wont update ismasterclient :: text component is null");
        }

        /// <summary>
        /// Tries setting up the room status pannel with given paramaters
        /// </summary>
        /// <param name="nickname">player name in room</param>
        /// <param name="room">room in</param>
        /// <param name="ismaster">ismaster client status</param>
        public void SetupRoomStatus(string nickname, Room room)
        {
            if (roomStatus == null)
                if (!FindAndSetRoomStatusReference())
                    return;

            //if room status is not enabled yet, enable it and disable connect button
            if (!roomStatus.activeInHierarchy)
            {
                roomStatus.SetActive(true);
                connectButton.gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(nickname))
            {
                UpdateNickname(nickname);
            }
            else Debug.LogError($"string nickname: {nickname} is not valid");

            if (room != null)
            {
                UpdateRoomInfo(room);
            }
            else Debug.LogError("given room to show status of is null");

            UpdateIsMasterclient();
        }

        /// <summary>
        /// sets up exit button with given click Action
        /// </summary>
        /// <param name="clickAction"></param>
        public void SetupExitButton(UnityAction clickAction)
        {
            if (exitButton == null)
                if (!FindAndSetExitButtonReference())
                    return;

            exitButton.gameObject.SetActive(true);
            exitButton.onClick.AddListener(() =>
            {
                clickAction?.Invoke();
                exitButton.onClick.RemoveListener(clickAction);
                exitButton.gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// Updates ready count buttons on lobby canvas based on given count
        /// </summary>
        /// <param name="count"></param>
        public void UpdateReadyButtons(int count)
        {
            if (readyStatus == null)
                if (!FindAndSetReadyStatusReference())
                    return;

            Transform statusTransform = readyStatus.transform;
            if (count < 0 || count > statusTransform.childCount)
            {
                Debug.LogError("count does not corespond with child count");
                return;
            }

            //setup necessary variables for chaining buttons on x axis
            float width = GetComponent<Canvas>().pixelRect.width;
            float buttonWidthHalf = statusTransform.GetChild(0).GetComponent<RectTransform>().rect.width * 0.5f;
            float margin = (width - (buttonWidthHalf * count)) / (count + 1);
            float x = -(width * 0.5f) - (buttonWidthHalf * 0.5f);
            //loop through children based on count and display them on given position
            for (int ci = 0; ci < count; ci++)
            {
                //place button on canvas based
                GameObject child = statusTransform.GetChild(ci).gameObject;
                RectTransform rectTF = child.GetComponent<RectTransform>();
                x += margin + buttonWidthHalf;
                rectTF.anchoredPosition = new Vector2(x, rectTF.anchoredPosition.y);
                //set it to active if not already active
                if (!child.activeInHierarchy)
                {
                    child.SetActive(true);
                }
                //if max players has been reached set buttons to be interactable
                if (count == MatchMakingManager.MAX_PLAYERS)
                {
                    child.GetComponent<Button>().interactable = true;
                }
            }
        }

        /// <summary>
        /// Starts listening to a ready button corresponding with given player number
        /// </summary>
        /// <param name="playerNumber"></param>
        public void ListenToReadyButton(InRoomManager roomManager)
        {
            if (roomManager != InRoomManager.Instance)
                return;

            int playerNumber = roomManager.NumberInRoom;
            //player number has to be between 0 and max players value
            if (playerNumber < 0 || playerNumber > MatchMakingManager.MAX_PLAYERS)
                return;

            //add onclick listener to players ready button
            Transform statusTransform = readyStatus.transform;
            Button button = statusTransform.GetChild(playerNumber - 1)?.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    bool ready = !InRoomManager.Instance.IsReady;
                    roomManager.SetReady(ready);
                    //send buffered rpc through server so all players get it around the same time
                    PhotonView PV = GetComponent<PhotonView>();
                    PV.RPC("UpdateReadyButton", RpcTarget.AllBufferedViaServer, playerNumber, ready);
                });
            }
            else Debug.LogError($"Player number {playerNumber} is out of bounds of child array");
        }

        /// <summary>
        /// resets ready buttons to default values, making them inactive, repositioning them,
        /// resseting the color and making them uninteractable
        /// </summary>
        public void ResetReadyButtons()
        {
            if (readyStatus == null)
                if (!FindAndSetReadyStatusReference())
                    return;

            Transform statusTransform = readyStatus.transform;
            for (int ci = 0; ci < statusTransform.childCount; ci++)
            {
                GameObject child = statusTransform.GetChild(ci).gameObject;
                RectTransform rectTF = child.GetComponent<RectTransform>();
                Button button = child.GetComponent<Button>();
                rectTF.anchoredPosition = new Vector2(0, rectTF.anchoredPosition.y);
                button.image.color = Color.white;
                button.interactable = false;
                if (child.activeInHierarchy)
                {
                    child.SetActive(false);
                }
            }
        }

        public void DoCountDown()
        {
            if (countdownText == null)
                if (!FindAndSetCountdownTextReference())
                    return;
            //reset ready buttons so players are not able to ready up/down
            ResetReadyButtons();

            //start countdown animation and let masterclient load the game scene on ond
            animations.CountDown(countdownText, InRoomManager.COUNTDOWN_LENGTH, true, () =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    int levelIndexToLoad = InRoomManager.Instance.NextLevelIndex;
                    if (levelIndexToLoad != -1)
                    {
                        PhotonNetwork.LoadLevel(levelIndexToLoad);
                    }
                    else Debug.LogError("Level index to load is not valid. Check current level index");
                }
            });
        }

        [PunRPC]
        private void UpdateReadyButton(int playerNumber, bool ready)
        {
            //get ready button on canvas based on player number and change its color based on ready value
            Button button = readyStatus.transform.GetChild(playerNumber - 1)?.GetComponent<Button>();
            button.image.color = ready ? connectColor : Color.white;
        }

        #region FallbackFunctions

        /// <summary>
        /// tries find connect button in scene en reset its reference
        /// </summary>
        private bool FindAndSetConnectButtonReference()
        {
            foreach (Button b in GetComponentsInChildren<Button>())
            {
                if (b.gameObject.name == "ConnectButton")
                {
                    connectButton = b;
                    break;
                }
            }

            if (connectButton == null)
            {
                Debug.LogError($"Connect button not found");
                return false;
            }

            return true;
        }

        /// <summary>
        /// tries find exit button in scene en reset its reference
        /// </summary>
        private bool FindAndSetExitButtonReference()
        {
            foreach (Button b in GetComponentsInChildren<Button>())
            {
                if (b.gameObject.name == "ExitButton")
                {
                    exitButton = b;
                    break;
                }
            }

            if (exitButton == null)
            {
                Debug.LogError($"Exit button not found");
                return false;
            }

            return true;
        }

        /// <summary>
        /// tries find ready status in scene en reset its reference
        /// </summary>
        private bool FindAndSetReadyStatusReference()
        {
            Transform tf = transform;
            for (int ci = 0; ci < tf.childCount; ci++)
            {
                GameObject child = tf.GetChild(ci).gameObject;
                if (child.name == "ReadyStatus")
                {
                    readyStatus = child;
                    break;
                }
            }

            if (readyStatus == null)
            {
                Debug.LogError($"Ready status GameObject not found");
                return false;
            }

            return true;
        }

        /// <summary>
        /// tries find room status in scene en reset its reference
        /// </summary>
        private bool FindAndSetRoomStatusReference()
        {
            Transform tf = transform;
            for (int ci = 0; ci < tf.childCount; ci++)
            {
                GameObject child = tf.GetChild(ci).gameObject;
                if (child.name == "RoomStatus")
                {
                    roomStatus = child;
                    break;
                }
            }

            if (roomStatus == null)
            {
                Debug.LogError($"Room status GameObject not found");
                return false;
            }

            return true;
        }

        /// <summary>
        /// tries find countdown text in scene en reset its reference
        /// </summary>
        private bool FindAndSetCountdownTextReference()
        {
            Transform tf = transform;
            for (int ci = 0; ci < tf.childCount; ci++)
            {
                GameObject child = tf.GetChild(ci).gameObject;
                if (child.name == "CountdownText")
                {
                    countdownText = child;
                    break;
                }
            }

            if (roomStatus == null)
            {
                Debug.LogError($"countdown text GameObject not found");
                return false;
            }

            return true;
        }

        #endregion FallbackFunctions
    }
}