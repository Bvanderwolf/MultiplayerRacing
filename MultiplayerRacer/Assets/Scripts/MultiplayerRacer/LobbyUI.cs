using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private GameObject roomStatus;
        [SerializeField] private Color connectColor = new Color(0, 0.75f, 0);
        [SerializeField] private Color disconnectColor = new Color(0.75f, 0, 0);
        [SerializeField] private float roomStatRoundness = 0.25f;

        //sets up connect button and returns if succeeded
        public void SetupConnectButton(Action<Button> clickAction)
        {
            //if connect button is null try finding it and if not found return
            if (connectButton == null)
                if (FindAndSetConnectButtonReference())
                    return;

            //handle edgecase where connect button is inactive and room status enabled
            if (!connectButton.gameObject.activeInHierarchy)
            {
                connectButton.gameObject.SetActive(true);
                roomStatus.SetActive(false);
            }
            connectButton.onClick.AddListener(() => clickAction.Invoke(connectButton));
        }

        public void UpdateConnectColor(bool connected)
        {
            if (connectButton == null)
                if (FindAndSetConnectButtonReference())
                    return;

            //show green or red button color for succesfull/unsuccesfull connection with master
            Image image = connectButton.image;
            if (image != null)
            {
                image.color = connected ? connectColor : disconnectColor;
            }
        }

        public void UpdateConnectStatus(bool connected)
        {
            if (connectButton == null)
                if (FindAndSetConnectButtonReference())
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
                if (FindAndSetConnectButtonReference())
                    return "";

            string text = connectButton.GetComponentInChildren<Text>().text;
            return text.Substring(text.LastIndexOf(' ') + 1);
        }

        /// <summary>
        /// Sets up room status with nickname
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
        /// sets up room status with room information
        /// </summary>
        /// <param name="playercount"></param>
        public void UpdateRoomInfo(Room room)
        {
            Text playerCountComp = roomStatus.transform.Find("Playercount")?.GetComponent<Text>();
            if (playerCountComp != null)
            {
                playerCountComp.text = $"Playercount: {room.PlayerCount}";
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
        /// sets up room status with ismasterclient status
        /// </summary>
        /// <param name="ismaster"></param>
        private void UpdateIsMasterclient(bool ismaster)
        {
            Text isMasterclientComp = roomStatus.transform.Find("Ismasterclient")?.GetComponent<Text>();
            if (isMasterclientComp != null)
            {
                string yesOrNo = ismaster ? "yes" : "no";
                isMasterclientComp.text = $"IsMasterclient: {yesOrNo}";
            }
            else Debug.LogError("Wont update ismasterclient :: text component is null");
        }

        public void SetupRoomStatus(string nickname, Room room, bool ismaster)
        {
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

            UpdateIsMasterclient(ismaster);
        }

        /// <summary>
        /// tries find connect button in scene en reset its reference
        /// </summary>
        private bool FindAndSetConnectButtonReference()
        {
            if (connectButton == null)
            {
                connectButton = GetComponentInChildren<Button>();
                if (connectButton == null || connectButton.name != "ConnectButton")
                {
                    Debug.LogError("connect button not found or invalid name");
                    return false;
                }
            }
            return true;
        }
    }
}