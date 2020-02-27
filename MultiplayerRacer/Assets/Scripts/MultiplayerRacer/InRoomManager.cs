using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        private LobbyUI lobbyUI = null;

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
                lobbyUI.UpdateRoomInfo(PhotonNetwork.CurrentRoom);
            }
            else Debug.LogError("Wont update room info :: lobbyUI is null");
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }
    }
}