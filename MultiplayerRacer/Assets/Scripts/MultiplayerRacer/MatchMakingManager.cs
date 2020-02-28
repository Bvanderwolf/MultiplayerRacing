using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class MatchMakingManager : MonoBehaviourPunCallbacks
    {
        private LobbyUI lobbyUI = null;
        private Color connectColor = new Color(0, 0.75f, 0);
        private Color disconnectColor = new Color(0.75f, 0, 0);

        private const string ROOM_NAME = "RacingRoom";
        public const int MAX_PLAYERS = 2;

        private bool connectingToMaster = false;
        private bool connectingToRoom = false;

        // Start is called before the first frame update
        private void Start()
        {
            AttachUIListeners();
        }

        private void AttachUIListeners()
        {
            //setup lobby ui if not already done
            if (lobbyUI == null)
            {
                lobbyUI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
            }
            lobbyUI.SetupConnectButton(OnConnectButtonClick);
        }

        /// <summary>
        /// Should be fired when the connect button is clicked.
        /// </summary>
        /// <param name="connectButton">connect button reference</param>
        private void OnConnectButtonClick(Button connectButton)
        {
            //check if the connectButton is valid
            if (connectButton == null)
                return;

            string destination = lobbyUI.ConnectDestination();

            //check if it is null or an empty string
            if (destination == "")
                return;

            //decide on what to connect to
            switch (destination)
            {
                case "Master":
                    OnConnectToMaster();
                    break;

                case "Room":
                    OnConnectToRoom();
                    break;

                default:
                    Debug.LogError("connect button text does not suffice as destination");
                    break;
            }
        }

        private string MakeNickname()
        {
            if (!PhotonNetwork.InRoom)
                return "";

            return $"Player{PhotonNetwork.CurrentRoom.PlayerCount}";
        }

        private void OnConnectToMaster()
        {
            //dont connect to master if we are already trying
            if (connectingToMaster)
                return;

            connectingToMaster = true;

            //set photonnetwork settings and connect
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = "v1";
            PhotonNetwork.ConnectUsingSettings();
        }

        private void OnConnectToRoom()
        {
            //check if we can actually connect to the room
            if (lobbyUI.ConnectDestination() != "Room" || connectingToRoom || !PhotonNetwork.IsConnectedAndReady)
                return;

            connectingToRoom = true;

            //setup room options and join or create room
            RoomOptions options = new RoomOptions();
            options.IsVisible = false;
            options.MaxPlayers = MAX_PLAYERS;
            PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, options, TypedLobby.Default);
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.LogError($"Creating room failed with message: {message}");
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            connectingToRoom = false;

            lobbyUI.SetupRoomStatus(
                MakeNickname(),
                PhotonNetwork.CurrentRoom,
                PhotonNetwork.IsMasterClient);

            lobbyUI.UpdateReadyButtons(PhotonNetwork.CurrentRoom.PlayerCount);

            lobbyUI.SetupExitButton(() =>
            {
                if (PhotonNetwork.IsConnectedAndReady)
                {
                    PhotonNetwork.LeaveRoom();
                }
            });
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            Debug.LogError($"Joining room failed with message: {message}");
        }

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();

            connectingToMaster = false;

            //update lobby ui when connected to master
            lobbyUI.UpdateConnectStatus(true);
            lobbyUI.UpdateConnectColor(true);
        }

        //Note* will be called before OnConnectedToMaster gets called
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            AttachUIListeners();
            lobbyUI.ResetReadyButtons();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            //update lobby ui when disconnected
            lobbyUI.UpdateConnectStatus(false);
            lobbyUI.UpdateConnectColor(false);

            //try reattaching the UI for when we where inside another scene
            AttachUIListeners();

            print(cause);
        }
    }
}