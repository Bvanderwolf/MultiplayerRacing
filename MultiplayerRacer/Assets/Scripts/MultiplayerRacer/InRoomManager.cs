using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        public static InRoomManager Instance { get; private set; }
        public static readonly byte[] memRoomMaster = new byte[2 * 4];

        public enum MultiplayerRacerScenes { LOBBY, GAME }

        private MultiplayerRacerUI UI = null;

        public int NumberInRoom { get; private set; } = 0;
        public bool IsReady { get; private set; } = false;

        public event Action<MultiplayerRacerScenes, bool> OnReadyStatusChange;

        public event Action<MultiplayerRacerScenes> OnSceneReset;

        public MultiplayerRacerScenes CurrentScene { get; private set; } = MultiplayerRacerScenes.LOBBY;

        public int NextLevelIndex
        {
            get
            {
                //return next value only if not out of scene count bounds, else -1
                int next = (int)CurrentScene + 1;
                if (next <= SceneManager.sceneCountInBuildSettings)
                {
                    return next;
                }
                else return -1;
            }
        }

        public const int COUNTDOWN_LENGTH = 3;
        public const float READY_SEND_TIMEOUT = 0.75f;

        /*
         master client attributes are stored inside the room master instance
         this defaults to null and is set for the client creating the room
         */
        public RoomMaster Master { get; private set; } = null;

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
            SetCanvasReference();
        }

        private void SetCanvasReference()
        {
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    UI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
                    break;

                case MultiplayerRacerScenes.GAME:
                    UI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<GameUI>();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// sets InroomManager its isready value and updates the master client with this value
        /// if updateMaster is true, set it to false if you are the master client
        /// </summary>
        /// <param name="value"></param>
        public void SetReady(bool value)
        {
            if (IsReady == value)
            {
                Debug.Log("Trying to update ready value with same value :: not updating Room Master instance");
                return;
            }
            IsReady = value; //set ready value
            if (CurrentScene == MultiplayerRacerScenes.GAME) Debug.LogError(value);
            OnReadyStatusChange?.Invoke(CurrentScene, value); //let others know if there are scripts subscribed
            GetComponent<PhotonView>().RPC("UpdatePlayersReady", RpcTarget.MasterClient, IsReady); //let master client know
        }

        /// <summary>
        /// sets the room master. Will need a MatchMakingManager instance for security
        /// </summary>
        /// <param name="matchMakingManager"></param>
        public void SetRoomMaster(MatchMakingManager matchMakingManager)
        {
            if (matchMakingManager == MatchMakingManager.Instance)
            {
                Master = new RoomMaster();
            }
            else Debug.LogError("matchmaking manager reference is null or not the singleton instance");
        }

        /// <summary>
        /// Registers the Room Master as a Custom Serializable Type for this room
        /// </summary>
        public void RegisterRoomMaster()
        {
            if (!RoomMaster.Registered)
            {
                bool succes = PhotonPeer.RegisterType(typeof(RoomMaster), (byte)'R', SerializeRoomMaster, DeserializeRoomMaster);
                if (succes)
                {
                    RoomMaster.SetRegistered();
                }
                else Debug.LogError("Failed Registering Room Master! Can't play game");
            }
            else Debug.LogError("Room Master is already registered :: wont do it again");
        }

        //sets number in the room
        public void SetNumberInRoom(MatchMakingManager manager, int number)
        {
            if (manager == MatchMakingManager.Instance)
            {
                NumberInRoom = number;
            }
        }

        /// <summary>
        /// should be called before a master client switch happens to send
        /// the roomMaster data to the newly assigned master client
        /// </summary>
        /// <param name="newMasterNumber"></param>
        public void SendMasterDataToNewMaster(int newMasterNumber, bool wasLeaving)
        {
            if (newMasterNumber < 0 || !RoomMaster.Registered)
                return;

            //send RPC call with master client data to new master client
            GetComponent<PhotonView>().RPC("UpdateRoomMaster", RpcTarget.All, newMasterNumber, Master, wasLeaving);
        }

        /// <summary>
        /// Called by the master client to let all players leave the room. Should
        /// only be used when a big problem/error has occured
        /// </summary>
        public void SendAllLeaveRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GetComponent<PhotonView>().RPC("LeaveRoomForcibly", RpcTarget.AllViaServer);
            }
        }

        /// <summary>
        /// checks whether the room is full or not acts accordingly if so
        /// </summary>
        /// <param name="room"></param>
        private void FullRoomCheck(Room room)
        {
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    if (room.PlayerCount == MatchMakingManager.MAX_PLAYERS)
                    {
                        lobbyUI.ListenToReadyButton();
                    }
                    break;

                case MultiplayerRacerScenes.GAME:
                    break;

                default:
                    break;
            }
        }

        private void MaxPlayersReadyCheck(MultiplayerRacerScenes scene)
        {
            if (Master.PlayersReady != MatchMakingManager.MAX_PLAYERS)
                return;

            switch (scene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    //Start countdown for game scene via server so a players start countdown at the same time
                    GetComponent<PhotonView>().RPC("StartCountdown", RpcTarget.AllViaServer);
                    break;

                case MultiplayerRacerScenes.GAME:
                    //countdown can start for race
                    Debug.LogError("can start countdown for race");
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Sets up values for client to load game scene, then lets masterclient load it.
        /// Only the masterclient loads the scene since PhotonNetwork.AutomaticalySyncScene is set to true
        /// </summary>
        private void LoadGameScene()
        {
            //subscribe OnGameSceneLoaded to scene loaded event before sloading the level
            SceneManager.sceneLoaded += OnLoadedGameScene;
            MatchMakingManager.Instance.AttachOnGameSceneLoaded(this);

            if (PhotonNetwork.IsMasterClient)
            {
                int levelIndexToLoad = NextLevelIndex;
                if (levelIndexToLoad != -1)
                {
                    PhotonNetwork.LoadLevel(levelIndexToLoad);
                }
                else Debug.LogError("Level index to load is not valid. Check current level index");
            }
        }

        private void OnLoadedGameScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != 1)
            {
                Debug.LogError("Loaded Scene that wasn't Game scene width buildIndex " + scene.buildIndex);
                return;
            }

            CurrentScene = (MultiplayerRacerScenes)scene.buildIndex; //set current scene to game scene build index(should be 1)
            SetReady(false); //reset ready value for usage in game scene

            //now that currentScene is updated, we set our canvas reference
            SetCanvasReference();

            //and set it up
            Room room = PhotonNetwork.CurrentRoom;
            UI.SetupRoomStatus(PhotonNetwork.NickName, room);
            room.IsOpen = false; //New players cannot join the game if the game scene has been loaded

            //update players in game values and tell others buffered to setup game scene
            PhotonView PV = GetComponent<PhotonView>();
            PV.RPC("UpdatePlayersInGameScene", RpcTarget.MasterClient, true);
            Debug.LogError("client setup game scene");
            //unsubscribe from scene loaded event
            SceneManager.sceneLoaded -= OnLoadedGameScene;
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            /*Note: photon assigns new masterclient when current one leaves.
            New one is the one with the lowest actor number, so first one to join
            should correlate to NumberInRoom*/
            if (UI != null)
            {
                UI.UpdateIsMasterclient();
            }
            else Debug.LogError("Wont update room :: UI is null");
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (UI == null)
            {
                Debug.LogError("Wont update room :: UI is null");
                return;
            }

            //invoke shared functions
            Room room = PhotonNetwork.CurrentRoom;
            UI.UpdateRoomInfo(room);

            //Update Room status based on current Scene
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    lobbyUI.UpdateReadyButtons(room.PlayerCount);
                    break;

                case MultiplayerRacerScenes.GAME:
                    break;

                default:
                    break;
            }
            FullRoomCheck(room); //entering player can be the one to fill the lobby
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            /*if the leaving player its actor number was greater than ours,
            it means this player joined before us so our number will go down
            by one to get the correct value for our number in the room*/
            Player me = PhotonNetwork.LocalPlayer;
            if (me.ActorNumber > otherPlayer.ActorNumber)
            {
                NumberInRoom--;
            }

            //the masterclient resets the players ready count when someone leaves
            if (PhotonNetwork.IsMasterClient)
            {
                /*reset players ready provided, of course, that the RoomMaster data has been transefered already.
                 if this is not the case consistently, we need some kind of fallback for this*/
                if (Master != null)
                {
                    Master.ResetPlayersReady();
                }
                else Debug.LogError("Failed resetting players ready on player leave :: RoomMaster instance is null");
            }

            if (UI == null)
            {
                Debug.LogError("Wont update room :: UI is null");
                return;
            }

            //invoke shared functions
            Room room = PhotonNetwork.CurrentRoom;
            UI.UpdateRoomInfo(room);
            UI.UpdateNickname(MatchMakingManager.Instance.MakeNickname());
            UI.ShowExitButton(); //handle edge cases where exit button is in hidden state

            //Update Room status based on current scene
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    //lobbyUI related updates
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    lobbyUI.ResetReadyButtons(); //reset ready buttons when a player leaves
                    lobbyUI.UpdateReadyButtons(room.PlayerCount);
                    break;

                case MultiplayerRacerScenes.GAME:
                    //gameUI related updates
                    break;

                default:
                    break;
            }
            SetReady(false);
            OnSceneReset?.Invoke(CurrentScene); //on scene reset gets fired after all changes have been dome
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }

        private static short SerializeRoomMaster(StreamBuffer outStream, object customobject)
        {
            RoomMaster rm = (RoomMaster)customobject;

            lock (memRoomMaster)
            {
                byte[] bytes = memRoomMaster;
                int index = 0;
                Protocol.Serialize(rm.PlayersReady, bytes, ref index);
                Protocol.Serialize(rm.PlayersInGameScene, bytes, ref index);
                outStream.Write(bytes, 0, 2 * 4);
            }
            return 2 * 4;
        }

        private static object DeserializeRoomMaster(StreamBuffer inStream, short length)
        {
            int playersready;
            int playersingamescene;
            lock (memRoomMaster)
            {
                inStream.Read(memRoomMaster, 0, 2 * 4);
                int index = 0;
                Protocol.Deserialize(out playersready, memRoomMaster, ref index);
                Protocol.Deserialize(out playersingamescene, memRoomMaster, ref index);
            }
            RoomMaster rm = new RoomMaster(playersready, playersingamescene);

            return rm;
        }

        [PunRPC]
        private void UpdatePlayersInGameScene(bool join)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //let the room master update players in game scene
                Master.UpdatePlayersInGameScene(join);

                //if all players are in the game scene, the master client can start the game
                if (Master.PlayersInGameScene == MatchMakingManager.MAX_PLAYERS)
                {
                    ((GameUI)UI).SendShowReadyUpInfo(); //make the game ui send ready up info
                }
            }
        }

        [PunRPC]
        private void UpdatePlayersReady(bool isready)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //let the room master update players ready
                Master.UpdatePlayersReady(isready);
                //check if all players are ready
                MaxPlayersReadyCheck(CurrentScene);
            }
        }

        [PunRPC]
        private void UpdateRoomMaster(int newRoomMasterNumber, RoomMaster newMaster, bool wasLeaving)
        {
            //if your actornumber matches the new room master number you get the data
            if (PhotonNetwork.LocalPlayer.ActorNumber == newRoomMasterNumber)
            {
                Master = newMaster;
                //if the master client was leaving we make sure to handle that
                if (wasLeaving)
                {
                    Master.ResetPlayersReady();
                }
            }
        }

        [PunRPC]
        private void LeaveRoomForcibly()
        {
            MatchMakingManager.Instance.LeaveRoomForced();
        }

        [PunRPC]
        private void StartCountdown()
        {
            if (UI == null)
            {
                Debug.LogError("Won't start StartCountdown :: UI is null");
                return;
            }

            //Do countdown based on current Scene
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    //start countdown, loading game scene on end and checking playercount state each count
                    lobbyUI.StartGameCountDown(LoadGameScene, () => PhotonNetwork.CurrentRoom.PlayerCount == MatchMakingManager.MAX_PLAYERS);
                    break;

                case MultiplayerRacerScenes.GAME:
                    break;

                default:
                    break;
            }
        }
    }
}