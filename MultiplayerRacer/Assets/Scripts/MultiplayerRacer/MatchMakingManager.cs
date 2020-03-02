using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class MatchMakingManager : MonoBehaviourPunCallbacks
    {
        public static MatchMakingManager Instance { get; private set; }
        private LobbyUI lobbyUI = null;
        private Color connectColor = new Color(0, 0.75f, 0);
        private Color disconnectColor = new Color(0.75f, 0, 0);

        private const string ROOM_NAME = "RacingRoom";
        public const int MAX_PLAYERS = 2;

        private bool connectingToMaster = false;
        private bool connectingToRoom = false;

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

        // Start is called before the first frame update
        private void Start()
        {
            AttachUI();
        }

        private void AttachUI()
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

        /// <summary>
        /// if in a room, set and returns a nickname based on your number in room
        /// </summary>
        /// <returns></returns>
        public string MakeNickname()
        {
            if (!PhotonNetwork.InRoom)
                return "";

            string nickname = $"Player{InRoomManager.Instance.NumberInRoom}";
            PhotonNetwork.NickName = nickname;
            return nickname;
        }

        /// <summary>
        /// tries leaving room if ready
        /// </summary>
        private void LeaveRoom()
        {
            if (PhotonNetwork.IsConnectedAndReady)
            {
                LeavingMasterCheck();
                PhotonNetwork.LeaveRoom();
            }
        }

        /// <summary>
        /// Sets up matchmaking and roomManagement values for being connected to a room
        /// </summary>
        /// <param name="room"></param>
        private void SetConnectedToRoom(Room room)
        {
            connectingToRoom = false;
            InRoomManager.Instance.RegisterRoomMaster();
            InRoomManager.Instance.SetNumberInRoom(this, room.PlayerCount);
        }

        /// <summary>
        /// Sets up matchmaking values for connecting to a room and then tries connecting to one
        /// </summary>
        private void SetConnectingToRoom()
        {
            connectingToRoom = true;

            //setup room options and join or create room
            RoomOptions options = new RoomOptions();
            options.IsVisible = false;
            options.MaxPlayers = MAX_PLAYERS;
            PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, options, TypedLobby.Default);
        }

        /// <summary>
        /// Sets up matchmaking values for being connected to the master server
        /// </summary>
        private void SetConnectedToMaster()
        {
            connectingToMaster = false;
        }

        /// <summary>
        /// sets up matchmaking values for connecting to master and then connects to it
        /// </summary>
        private void SetConnectingToMaster()
        {
            connectingToMaster = true;

            //set photonnetwork settings and connect
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = "v1";
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// checks if given room is full and acts accordingly if so
        /// </summary>
        /// <param name="room"></param>
        private void FullRoomCheck(Room room)
        {
            if (room.PlayerCount == MAX_PLAYERS)
            {
                lobbyUI.ListenToReadyButton();
            }
        }

        /// <summary>
        /// checks for a possible transferring of data from old masterclient to new
        /// should be called before leaving the room, to make sure data is not lost
        /// when the master client leaves
        /// </summary>
        private bool LeavingMasterCheck()
        {
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                /*the new master client will be the player with the next lowest actor number
                 according to the Photon Pun Documentation*/
                int newMasterNumber = PhotonNetwork.LocalPlayer.GetNext().ActorNumber;
                //update for each needed class, the master client data
                InRoomManager.Instance.SwitchRoomMaster(newMasterNumber);
                //if a master client is leaving send all outgoing commands to make sure the data is send
                PhotonNetwork.SendAllOutgoingCommands();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Should be called on application quit to make sure a quiting master client
        /// its data is not lost but given to a new one
        /// </summary>
        private void OnQuitEvent()
        {
            LeavingMasterCheck();
        }

        /// <summary>
        /// should be called when connect to master button is clicked
        /// </summary>
        private void OnConnectToMaster()
        {
            //dont connect to master if we are already trying
            if (!connectingToMaster)
            {
                SetConnectingToMaster();
                lobbyUI.SetConnectButtonInteractability(false);
            }
        }

        /// <summary>
        /// should be called when connect to room button is clicked
        /// </summary>
        private void OnConnectToRoom()
        {
            //connect only if we can actually connect to the room
            if (lobbyUI.ConnectDestination() == "Room" && !connectingToRoom && PhotonNetwork.IsConnectedAndReady)
            {
                SetConnectingToRoom();
                lobbyUI.SetConnectButtonInteractability(false);
            }
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            //if this client created the room, it is the master client and is the room master
            InRoomManager.Instance.SetRoomMaster(this);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.LogError($"Creating room failed with message: {message}");
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            //if we where connecting to a room we setup values and ui accordingly
            if (connectingToRoom)
            {
                Room room = PhotonNetwork.CurrentRoom;
                SetConnectedToRoom(room);
                lobbyUI.SetupRoomStatus(MakeNickname(), room);
                lobbyUI.UpdateReadyButtons(room.PlayerCount);
                lobbyUI.SetupExitButton(LeaveRoom);
                lobbyUI.SetConnectButtonInteractability(true);
                FullRoomCheck(room); //client can be the one filling up the room.
                Application.quitting += OnQuitEvent; //setup quitting event with leaving master check
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            Debug.LogError($"Joining room failed with message: {message}");
        }

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            //if we where connecting to master we set up values and ui accordingly
            if (connectingToMaster)
            {
                SetConnectedToMaster();
                lobbyUI.UpdateConnectStatus(true);
                lobbyUI.UpdateConnectColor(true);
                lobbyUI.SetConnectButtonInteractability(true);
            }
        }

        //Note* will be called before OnConnectedToMaster gets called
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            //reset ui when having left a room
            AttachUI();
            lobbyUI.ResetReadyButtons();
            Application.quitting -= OnQuitEvent; //unsubsribe from quitting event
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            //update lobby ui when disconnected
            lobbyUI.UpdateConnectStatus(false);
            lobbyUI.UpdateConnectColor(false);

            //try reattaching the UI for when we where inside another scene
            AttachUI();
            lobbyUI.ResetReadyButtons();

            print(cause);
        }
    }
}