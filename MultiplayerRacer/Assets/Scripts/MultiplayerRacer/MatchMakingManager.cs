using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    using MultiplayerRacerScenes = InRoomManager.MultiplayerRacerScenes;

    public class MatchMakingManager : MonoBehaviourPunCallbacks
    {
        public static MatchMakingManager Instance { get; private set; }
        private MultiplayerRacerUI UI = null;
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
            AttachUI(MultiplayerRacerScenes.LOBBY);
        }

        private void AttachUI(MultiplayerRacerScenes scene)
        {
            switch (scene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    UI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
                    ((LobbyUI)UI).SetupConnectButton(OnConnectButtonClick);
                    break;

                case MultiplayerRacerScenes.GAME:
                    UI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<GameUI>();
                    break;

                default:
                    break;
            }
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

            string destination = ((LobbyUI)UI).ConnectDestination();

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
                DoLeavingChecks();
                switch (InRoomManager.Instance.CurrentScene)
                {
                    case MultiplayerRacerScenes.LOBBY:
                        break;

                    case MultiplayerRacerScenes.GAME:
                        //we load the lobby scene bofore leaving the room
                        SceneManager.LoadScene((int)MultiplayerRacerScenes.LOBBY);
                        break;

                    default:
                        break;
                }

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
            PhotonNetwork.GameVersion = "v2";
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
                ((LobbyUI)UI).ListenToReadyButton();
            }
        }

        /// <summary>
        /// checks for a possible transferring of data from old masterclient to new
        /// should be called before leaving the room, to make sure data is not lost
        /// when the master client leaves
        /// </summary>
        private void DoLeavingChecks()
        {
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                /*the new master client will be the player with the next lowest actor number
                 according to the Photon Pun Documentation*/
                int newMasterNumber = PhotonNetwork.LocalPlayer.GetNext().ActorNumber;

                //send the room master data to new master with newMasterNumber with leaving set to true
                InRoomManager.Instance.SendMasterDataToNewMaster(newMasterNumber, true);

                //We are no long the room master
                InRoomManager.Instance.ResetIsRoomMaster();
                //if a client is leaving send all outgoing commands to make sure the data is send
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }

        /// <summary>
        /// Should be called on application quit to make sure a quiting master client
        /// its data is not lost but given to a new one
        /// </summary>
        private void OnQuitEvent()
        {
            DoLeavingChecks();
        }

        private void OnMasterLoadedGameScene(Scene scene, LoadSceneMode mode)
        {
            AttachUI(MultiplayerRacerScenes.GAME); //reattach ui but based on game scene
            UI.SetupExitButton(LeaveRoom); //setup exit button with leave room function
            SceneManager.sceneLoaded -= OnMasterLoadedGameScene; //unsubscribe this function from scene loaded event
        }

        public void OnGameSceneLoaded()
        {
            AttachUI(MultiplayerRacerScenes.GAME); //reattach ui but based on game scene
            UI.SetupExitButton(LeaveRoom); //setup exit button with leave room function
        }

        public void AttachOnGameSceneLoaded(InRoomManager instance)
        {
            if (instance != InRoomManager.Instance || !PhotonNetwork.IsMasterClient)
                return;

            SceneManager.sceneLoaded += OnMasterLoadedGameScene;
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
                ((LobbyUI)UI).SetConnectButtonInteractability(false);
            }
        }

        /// <summary>
        /// should be called when connect to room button is clicked
        /// </summary>
        private void OnConnectToRoom()
        {
            LobbyUI lobbyUI = (LobbyUI)UI;
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
                LobbyUI lobbyUI = (LobbyUI)UI;
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
                LobbyUI lobbyUI = (LobbyUI)UI;
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
            AttachUI(MultiplayerRacerScenes.LOBBY);
            ((LobbyUI)UI).ResetReadyButtons();
            InRoomManager.Instance.SetToLobby();
            Application.quitting -= OnQuitEvent; //unsubsribe from quitting event
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            //try reattaching the UI for when we where inside another scene
            AttachUI(MultiplayerRacerScenes.LOBBY);
            //update lobby ui after having tried reattaching it
            LobbyUI lobbyUI = (LobbyUI)UI;
            lobbyUI.UpdateConnectStatus(false);
            lobbyUI.UpdateConnectColor(false);
            lobbyUI.ResetReadyButtons();
            InRoomManager.Instance.SetToLobby();

            print(cause);
        }
    }
}