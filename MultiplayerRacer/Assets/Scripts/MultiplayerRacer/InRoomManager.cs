using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        private LobbyUI lobbyUI = null;

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
            lobbyUI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            //Update Room Info
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
                lobbyUI.UpdateReadyButtons(room.PlayerCount);
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            //Update Room Info
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
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