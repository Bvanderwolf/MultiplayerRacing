using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class LobbyUI : MultiplayerRacerUI
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private GameObject playersInfo;
        [SerializeField] private GameObject maxPlayerInputField;
        [SerializeField] private GameObject carSelect;

        [SerializeField] private Color connectColor = new Color(0, 0.75f, 0);

        [SerializeField] private Color disconnectColor = new Color(0.75f, 0, 0);

        private float readyStatusAnchorX;
        private const int MAX_READYBUTTONS_IN_ROW = 4;

        protected override void Awake()
        {
            base.Awake();
            readyStatusAnchorX = playersInfo.GetComponent<RectTransform>().anchoredPosition.x;
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
                SetButtonInfoActiveState(false);
                SetRoomStatusActiveState(false);
            }
            //set connect color to default: is red for not connected
            UpdateConnectColor(false);
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

        public void SetConnectButtonInteractability(bool value)
        {
            if (connectButton == null)
                if (!FindAndSetConnectButtonReference())
                    return;

            connectButton.interactable = value;
        }

        public void SetMaxPlayersInputFieldActiveState(bool value)
        {
            if (maxPlayerInputField == null)
                Debug.LogError("MaxPlayerInputField not found :: not setting it");

            if (value)
                maxPlayerInputField.GetComponent<InputField>().onEndEdit.AddListener(OnMaxPlayerInputFieldEdited);
            else
                maxPlayerInputField.GetComponent<InputField>().onEndEdit.RemoveListener(OnMaxPlayerInputFieldEdited);

            maxPlayerInputField.SetActive(value);
        }

        public void OnMaxPlayerInputFieldEdited(string edit)
        {
            if (InValidMaxPlayerInput(edit))
            {
                InputField input = maxPlayerInputField.GetComponent<InputField>();
                input.text = "";
                input.placeholder.color = Color.red;
                return;
            }
            //update the room's max players so players can join
            InRoomManager.Instance.SetMaxPlayersInRoom(int.Parse(edit));
            //since on joined room won't be called for the master client, set it manualy
            MatchMakingManager.Instance.OnJoinedRoom();
            //set active state of input field to false
            SetMaxPlayersInputFieldActiveState(false);
        }

        private bool InValidMaxPlayerInput(string input)
        {
            if (!char.IsNumber(input[0]))
                return false;

            int number = int.Parse(input);
            return number <= 1 || number > MatchMakingManager.MAX_PLAYERS;
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
        /// Tries setting up the room status pannel with given paramaters
        /// this function overrides MultiplayerRacerUI which does a lot of
        /// background work aswell.
        /// </summary>
        /// <param name="nickname">player name in room</param>
        /// <param name="room">room in</param>
        /// <param name="ismaster">ismaster client status</param>
        public override void SetupRoomInfo(string nickname, Room room)
        {
            base.SetupRoomInfo(nickname, room);

            //if room status is not enabled yet, enable it and disable connect button
            if (!roomStatus.activeInHierarchy)
            {
                SetButtonInfoActiveState(true);
                connectButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates ready count buttons on lobby canvas based on given count
        /// </summary>
        /// <param name="count"></param>
        public void UpdateReadyButtons(int count)
        {
            if (playersInfo == null)
                if (!FindAndSetReadyStatusReference())
                    return;

            RectTransform statusTransform = playersInfo.GetComponent<RectTransform>();
            if (count <= 0 || count > statusTransform.childCount)
            {
                Debug.LogError("count does not corespond with child count");
                return;
            }
            //get constrain count (rows)
            int constraintCount = statusTransform.GetComponent<GridLayoutGroup>().constraintCount;
            //get collum count based on player count devided by constrainCount
            int collums = Mathf.CeilToInt(count / (float)constraintCount);
            //define x position of ready button based on start value (anchorX), constraintCount and collums
            float x = readyStatusAnchorX * Mathf.Pow(constraintCount, collums - 1);
            //if the player count makes it so there are not 1 collum or maximum collums we add the base value
            if (count > constraintCount && count < statusTransform.childCount - 1)
                x += readyStatusAnchorX;

            statusTransform.anchoredPosition = new Vector2(x, statusTransform.anchoredPosition.y);
            //loop through children based on count and display them on given position
            for (int ci = 0; ci < count; ci++)
            {
                //place button on canvas based
                GameObject child = statusTransform.GetChild(ci).gameObject;
                SetReadyButton(child, ci, count);
            }
        }

        private void SetReadyButton(GameObject button, int ci, int count)
        {
            SetReadyButtonHeader(button.transform.Find("PlayerName")?.gameObject, $"Player {ci + 1}");
            //set it to active if not already active
            if (!button.activeInHierarchy)
            {
                button.SetActive(true);
            }
            //if max players has been reached set buttons to be interactable
            if (count == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                button.GetComponentInChildren<Button>().interactable = true;
            }
        }

        /// <summary>
        /// sets ready button header text
        /// </summary>
        /// <param name="textGo"></param>
        private void SetReadyButtonHeader(GameObject textGo, string name)
        {
            if (textGo != null)
            {
                Text header = textGo.GetComponent<Text>();
                header.text = name;
            }
            else Debug.LogError("ready button header not set :: text gameobject is null");
        }

        /// <summary>
        /// Starts listening to a ready button corresponding with given player number
        /// </summary>
        /// <param name="playerNumber"></param>
        public void ListenToReadyButton()
        {
            int playerNumber = InRoomManager.Instance.NumberInRoom;
            //player number has to be between 0 and max players value
            if (playerNumber < 0 || playerNumber > MatchMakingManager.MAX_PLAYERS)
                return;

            //add onclick listener to players ready button
            Transform statusTransform = playersInfo.transform;
            Button button = statusTransform.GetChild(playerNumber - 1)?.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    //set room manager ready settings
                    bool ready = !InRoomManager.Instance.IsReady;
                    InRoomManager.Instance.SetReady(ready);

                    //set time out for the clicked ready button
                    animations.TimeOutButton(button, InRoomManager.READY_SEND_TIMEOUT);

                    //send rpc through server so all players get it around the same time
                    PhotonView PV = GetComponent<PhotonView>();
                    PV.RPC("UpdateReadyButton", RpcTarget.AllViaServer, playerNumber, ready);
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
            if (playersInfo == null)
                if (!FindAndSetReadyStatusReference())
                    return;

            Transform statusTransform = playersInfo.transform;
            for (int ci = 0; ci < statusTransform.childCount; ci++)
            {
                GameObject child = statusTransform.GetChild(ci).gameObject;
                RectTransform rectTF = child.GetComponent<RectTransform>();
                Button button = child.GetComponentInChildren<Button>();
                rectTF.anchoredPosition = new Vector2(0, rectTF.anchoredPosition.y);
                button.image.color = Color.white;
                button.interactable = false;
                button.onClick.RemoveAllListeners();
                if (child.activeInHierarchy)
                {
                    child.SetActive(false);
                }
            }
        }

        public void SetCarSelectActiveState(bool value)
        {
            //buttons voor navigatie koppelen (onpress callback functies)

            //callback voor selecting car -> set active state false, laat playerinfo zien en zet hashtable propertie
        }

        public void StartCountDownForGameScene(Action endAction, Func<bool> check = null)
        {
            if (countdownText == null)
                if (!FindAndSetCountdownTextReference())
                    return;

            //reset ready buttons so players are not able to ready up/down
            ResetReadyButtons();
            //hide exit button
            HideExitButton();
            SetButtonInfoActiveState(false);
            SetRoomStatusActiveState(false);

            //start countdown animation and let masterclient load the game scene on end
            animations.CountDown(countdownText, InRoomManager.COUNTDOWN_LENGTH, endAction, check);
        }

        [PunRPC]
        private void UpdateReadyButton(int playerNumber, bool ready)
        {
            //get ready button on canvas based on player number and change its color based on ready value
            Button button = playersInfo.transform.GetChild(playerNumber - 1)?.GetComponentInChildren<Button>();
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
                    playersInfo = child;
                    break;
                }
            }

            if (playersInfo == null)
            {
                Debug.LogError($"Ready status GameObject not found");
                return false;
            }

            return true;
        }

        #endregion FallbackFunctions
    }
}