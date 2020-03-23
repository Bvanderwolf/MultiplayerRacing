using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    using IEnumerator = System.Collections.IEnumerator;

    public class LobbyUI : MultiplayerRacerUI
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private GameObject playersInfo;
        [SerializeField] private GameObject maxPlayerInputField;
        [SerializeField] private CarSelectNavigation carSelect;

        [SerializeField] private Color connectColor = new Color(0, 0.75f, 0);

        [SerializeField] private Color disconnectColor = new Color(0.75f, 0, 0);

        private float readyStatusAnchorX;
        private const int MAX_READYBUTTONS_IN_ROW = 4;

        private bool selectingCar = true;
        private PhotonView PV;

        public const string SPRITE_INDEX_HASHTABLE_KEY = "CarSpriteIndex";

        public List<int> SelectedCars { get; private set; } = new List<int>();

        protected override void Awake()
        {
            base.Awake();
            PV = GetComponent<PhotonView>();
            readyStatusAnchorX = playersInfo.GetComponent<RectTransform>().anchoredPosition.x;
            //attach select button on click listener
            carSelect.SelectButton.onClick.AddListener(OnCarSelectButtonPress);
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
        /// if not selecting a car
        /// </summary>
        /// <param name="count"></param>
        public void UpdatePlayerInfo(int count)
        {
            if (selectingCar)
                return;

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
            for (int ci = 0; ci < count; ci++)
            {
                //place button on canvas based
                GameObject child = statusTransform.GetChild(ci).gameObject;
                SetPlayerInfoItem(child, ci, count);
            }
        }

        public void SetPlayerInfoCarSprite(int playerNumber, int index, Sprite carSprite)
        {
            //car sprite index kan niet meer gebruikt worden nu hij gekozen is
            Transform playerInfo = playersInfo.transform.GetChild(playerNumber - 1);
            Image carImage = playerInfo.Find("CarImage").GetComponent<Image>();
            carImage.sprite = carSprite;
            carImage.gameObject.SetActive(true);

            //add index of choosen car to selected cars index
            SelectedCars.Add(index);
            if (selectingCar)
            {
                //if we are looking at a car (in focus) that is taken, disable select button
                bool takenCarInFocus = index == carSelect.TextureNumInFocus - 1;
                if (takenCarInFocus) carSelect.SetSelectButtonInteractableState(false);
            }
        }

        private void SetPlayerInfoItem(GameObject item, int ci, int count)
        {
            SetPlayerInfoHeader(item.transform.Find("PlayerName")?.gameObject, $"Player {ci + 1}");
            //set it to active if not already active
            if (!item.activeInHierarchy)
            {
                item.SetActive(true);
            }
            //if max players has been reached set buttons to be interactable
            if (count == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                item.GetComponentInChildren<Button>().interactable = true;
            }
        }

        /// <summary>
        /// sets ready button header text
        /// </summary>
        /// <param name="textGo"></param>
        private void SetPlayerInfoHeader(GameObject textGo, string name)
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
                    PV.RPC("UpdateReadyButton", RpcTarget.AllViaServer, playerNumber, ready);
                });
            }
            else Debug.LogError($"Player number {playerNumber} is out of bounds of child array");
        }

        /// <summary>
        /// resets ready buttons to default values, making them inactive, repositioning them,
        /// resseting the color and making them uninteractable
        /// </summary>
        public void ResetPlayerInfo()
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
            //laat playerinfo zien
            carSelect.gameObject.SetActive(value);
        }

        public void OnPlayerLeftSelectedCar(Player player)
        {
            if (!player.CustomProperties.ContainsKey(SPRITE_INDEX_HASHTABLE_KEY))
                return;

            int spriteIndex = (int)player.CustomProperties[SPRITE_INDEX_HASHTABLE_KEY];
            SelectedCars.Remove(spriteIndex);
            if (selectingCar)
            {
                bool freedCarInFocus = spriteIndex == carSelect.TextureNumInFocus - 1;
                if (freedCarInFocus) carSelect.SetSelectButtonInteractableState(true);
            }
            else
            {
                int playerNumber = InRoomManager.Instance.GetRoomNumberOfActor(player);
                Transform playerInfo = playersInfo.transform.GetChild(playerNumber - 1);
                Image carImage = playerInfo.Find("CarImage").GetComponent<Image>();
                carImage.sprite = null;
                carImage.gameObject.SetActive(false);
            }
        }

        private void OnCarSelectButtonPress()
        {
            //get car sprite index
            int carSpriteIndex = carSelect.TextureNumInFocus - 1;
            //set seleting car to false and update ready buttons
            selectingCar = false;
            UpdatePlayerInfo(PhotonNetwork.CurrentRoom.PlayerCount);

            //set car sprite index as custom property for this player
            Hashtable table = PhotonNetwork.LocalPlayer.CustomProperties;
            if (!table.ContainsKey(SPRITE_INDEX_HASHTABLE_KEY))
                table.Add(SPRITE_INDEX_HASHTABLE_KEY, carSpriteIndex); //add new
            else
                table[SPRITE_INDEX_HASHTABLE_KEY] = carSpriteIndex; //update current
            PhotonNetwork.LocalPlayer.SetCustomProperties(table);
            //set car select to inactive state
            SetCarSelectActiveState(false);
        }

        public void UpdateCarsSelectedWithPlayerProperties()
        {
            Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
            foreach (KeyValuePair<int, Player> player in players)
            {
                if (player.Value.CustomProperties.ContainsKey(SPRITE_INDEX_HASHTABLE_KEY))
                {
                    int index = (int)player.Value.CustomProperties[SPRITE_INDEX_HASHTABLE_KEY];
                    int numberInRoom = InRoomManager.Instance.GetRoomNumberOfActor(player.Key);
                    Sprite sprite = InRoomManager.Instance.GetUsableCarSprite(index);
                    SetPlayerInfoCarSprite(numberInRoom, index, sprite);
                }
            }
        }

        public void OnLobbyLeave()
        {
            ResetPlayerInfo();
            ResetCarSelectProps();
        }

        public void ResetCarSelectProps()
        {
            SelectedCars.Clear();
            PhotonNetwork.LocalPlayer.CustomProperties.Remove(SPRITE_INDEX_HASHTABLE_KEY);
            selectingCar = true;
            carSelect.gameObject.SetActive(false);
            StartCoroutine(ResetAllCarTextures(carSelect.OnLobbyLeave));
        }

        private IEnumerator ResetAllCarTextures(Action onEnd)
        {
            Dictionary<int, Color[]> carTextureColors = carSelect.CarTextureColors;
            if (carTextureColors.Count == 0)
                yield break;

            List<Sprite> sprites = InRoomManager.Instance.GetSelectableCarSprites();
            foreach (var texColorPair in carTextureColors)
            {
                int spriteIndex = texColorPair.Key - 1;
                Texture2D tex = sprites[spriteIndex].texture;
                for (int x = 0; x < tex.width; x++)
                {
                    for (int y = 0; y < tex.height; y++)
                    {
                        int i = x + tex.width * y;
                        Color memTexCol = texColorPair.Value[i];
                        tex.SetPixel(x, y, memTexCol);
                    }
                }
                tex.Apply();
                yield return new WaitForFixedUpdate();
            }
            onEnd?.Invoke();
        }

        public void StartCountDownForGameScene(Action endAction, Func<bool> check = null)
        {
            if (countdownText == null)
                if (!FindAndSetCountdownTextReference())
                    return;

            //reset ready buttons so players are not able to ready up/down
            ResetPlayerInfo();
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