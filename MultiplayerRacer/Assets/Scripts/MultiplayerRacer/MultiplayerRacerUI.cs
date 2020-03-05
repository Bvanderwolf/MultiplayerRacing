using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    /// <summary>
    /// Base class for MultiPlayerRace user interface classes. It contains
    /// basic needs like room status, exit and countdown handling
    /// </summary>
    public class MultiplayerRacerUI : MonoBehaviour
    {
        [SerializeField] protected GameObject countdownText;
        [SerializeField] protected GameObject roomStatus;
        [SerializeField] protected Button exitButton;

        protected UIAnimations animations;

        protected virtual void Awake()
        {
            //set up ui animations reference
            animations = GetComponent<UIAnimations>();
            if (animations == null)
            {
                animations = gameObject.AddComponent<UIAnimations>();
            }
        }

        /// <summary>
        /// Sets up room status panel with nickname
        /// </summary>
        /// <param name="nickname"></param>
        public void UpdateNickname(string nickname)
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
        public void UpdateIsMasterclient()
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
        public virtual void SetupRoomStatus(string nickname, Room room)
        {
            if (roomStatus == null)
                if (!FindAndSetRoomStatusReference())
                    return;

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
        /// sets active state of ExitButton to false
        /// </summary>
        public void HideExitButton()
        {
            if (exitButton == null)
                if (!FindAndSetExitButtonReference())
                    return;

            exitButton.gameObject.SetActive(false);
        }

        // <summary>
        /// sets active state of room status to false
        /// </summary>
        public void HideRoomStatus()
        {
            if (roomStatus == null)
                if (!FindAndSetRoomStatusReference())
                    return;

            roomStatus.SetActive(false);
        }

        // <summary>
        /// sets active state of room status to false
        /// </summary>
        public void ShowRoomStatus()
        {
            if (roomStatus == null)
                if (!FindAndSetRoomStatusReference())
                    return;

            roomStatus.SetActive(true);
        }

        /// <summary>
        /// sets active state of ExitButton to true
        /// </summary>
        public void ShowExitButton()
        {
            if (exitButton == null)
                if (!FindAndSetExitButtonReference())
                    return;

            exitButton.gameObject.SetActive(true);
        }

        #region FallbackFunctions

        /// <summary>
        /// tries find room status in scene en reset its reference
        /// </summary>
        protected bool FindAndSetRoomStatusReference()
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
        /// tries find exit button in scene en reset its reference
        /// </summary>
        protected bool FindAndSetExitButtonReference()
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
        /// tries find countdown text in scene en reset its reference
        /// </summary>
        protected bool FindAndSetCountdownTextReference()
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