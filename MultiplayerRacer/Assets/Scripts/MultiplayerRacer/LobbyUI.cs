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

        //sets up connect button and returns if succeeded
        public bool SetupConnectButton(Action<Button> clickAction)
        {
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(() => clickAction.Invoke(connectButton));
            }
            else
            {
                if (connectButton == null)
                {
                    Debug.LogError("No connect button found on canvas");
                    return false;
                }
                else
                {
                    connectButton.onClick.AddListener(() => clickAction.Invoke(connectButton));
                }
            }
            return true;
        }

        public void UpdateConnectColor(bool connected)
        {
            //show green or red button color for succesfull/unsuccesfull connection with master
            Image image = connectButton.image;
            if (image != null)
            {
                image.color = connected ? connectColor : disconnectColor;
            }
        }

        public void UpdateConnectStatus(bool connected)
        {
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
                return "";

            string text = connectButton.GetComponentInChildren<Text>().text;
            return text.Substring(text.LastIndexOf(' ') + 1);
        }

        public void SetupRoomStatus()
        {
        }
    }
}