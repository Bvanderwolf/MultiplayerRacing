using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        public static InRoomManager Instance { get; private set; }
        private LobbyUI lobbyUI = null;

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }

            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
            lobbyUI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
        }

        private void FullRoomCheck(Room room)
        {
            if (room.PlayerCount == MatchMakingManager.MAX_PLAYERS)
            {
                lobbyUI.ListenToReadyButton(MatchMakingManager.Instance.NumberInRoom);
            }
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            //Update Room status
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
                lobbyUI.UpdateReadyButtons(room.PlayerCount);
                FullRoomCheck(room);
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            //Update Room status
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
                lobbyUI.ResetReadyButtons();
                lobbyUI.UpdateReadyButtons(room.PlayerCount);
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }
    }
}