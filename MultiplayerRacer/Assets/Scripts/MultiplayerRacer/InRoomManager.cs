using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using MultiplayerRacerEnums;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    using IEnumerator = System.Collections.IEnumerator;

    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        public static InRoomManager Instance { get; private set; }

        public static readonly byte[] memRoomMaster = new byte[RoomMaster.BYTESIZE];

        /// <summary>
        /// returns, based on PhotonNetwork.CurrentRoom, whether the room in is full or not
        /// </summary>
        public static bool IsFull
        {
            get
            {
                if (!PhotonNetwork.InRoom) return false;
                Room r = PhotonNetwork.CurrentRoom;
                return r.PlayerCount == r.MaxPlayers;
            }
        }

        public int NumberInRoom { get; private set; } = 0;
        public bool IsReady { get; private set; } = false;

        public event Action<MultiplayerRacerScenes, bool> OnReadyStatusChange;

        public event Action OnGameStart;

        public event Action<MultiplayerRacerScenes> OnSceneReset;

        public MultiplayerRacerScenes CurrentScene { get; private set; } = MultiplayerRacerScenes.LOBBY;
        private GamePhase CurrentGamePhase = GamePhase.NONE;

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

        private PhotonView PV;
        private MultiplayerRacerUI UI = null;
        private InRoomManagerHelper helper = new InRoomManagerHelper();
        private bool IsResetAble => CurrentScene == MultiplayerRacerScenes.LOBBY || CurrentGamePhase == GamePhase.SETUP;

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
            PV = GetComponent<PhotonView>();
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
            UI = helper.GetCanvasReference(CurrentScene);
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
            OnReadyStatusChange?.Invoke(CurrentScene, value); //let others know if there are scripts subscribed
            PV.RPC("UpdatePlayersReady", RpcTarget.MasterClient, IsReady); //let master client know
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
                if (helper.RegisterRoomMaster())
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
        public void SendMasterDataToNewMaster(int newMasterNumber)
        {
            if (newMasterNumber < 0 || !RoomMaster.Registered)
                return;

            //send RPC call with master client data to new master client
            PV.RPC("UpdateRoomMaster", RpcTarget.All, newMasterNumber, Master);
        }

        /// <summary>
        /// Called by the master client to let all players leave the room. Should
        /// only be used when a big problem/error has occured
        /// </summary>
        public void SendAllLeaveRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PV.RPC("LeaveRoomForcibly", RpcTarget.Others);
            }
        }

        /// <summary>
        /// checks whether the room is full or not acts accordingly if so
        /// </summary>
        /// <param name="room"></param>
        private void FullRoomCheck()
        {
            /*since players can only join the room in the lobby scene
            the fullroom check is only done here*/
            if (CurrentScene != MultiplayerRacerScenes.LOBBY)
                return;

            if (IsFull)
            {
                ((LobbyUI)UI).ListenToReadyButton();
            }
        }

        private void MaxPlayersReadyCheck(MultiplayerRacerScenes scene)
        {
            if (Master.PlayersReady != MatchMakingManager.MAX_PLAYERS)
                return;

            //countdown can start
            PV.RPC("StartCountdown", RpcTarget.AllViaServer);
        }

        private void OnRaceStart()
        {
            CurrentGamePhase = GamePhase.RACING;
            //make car controls available to players
            OnGameStart?.Invoke();
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
            CurrentGamePhase = GamePhase.SETUP;
            SetReady(false); //reset ready value for usage in game scene

            //now that currentScene is updated, we set our canvas reference
            UI = helper.GetCanvasReference(CurrentScene);

            //and set it up
            Room room = PhotonNetwork.CurrentRoom;
            UI.SetupRoomStatus(PhotonNetwork.NickName, room);
            room.IsOpen = false; //New players cannot join the game if the game scene has been loaded

            //update players in game values and tell others buffered to setup game scene
            PV.RPC("UpdatePlayersInGameScene", RpcTarget.MasterClient, true);

            //unsubscribe from scene loaded event
            SceneManager.sceneLoaded -= OnLoadedGameScene;
        }

        /// <summary>
        /// function used by master client to deal with a player leafing the
        /// room and so also him
        /// </summary>
        private void OnPlayerLeftMaster()
        {
            //the masterclient resets the players ready count when someone leaves
            if (PhotonNetwork.IsMasterClient)
            {
                //if the master client is the only one left he leaves the room
                if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
                {
                    Debug.LogError("Last player in game scene :: Leaving Room");
                    StartCoroutine(LastManLeaveWithDelay());
                    return;
                }
                switch (CurrentScene)
                {
                    //in the lobby scene we just reset the players ready
                    case MultiplayerRacerScenes.LOBBY:
                        Master.ResetPlayersReady();
                        break;
                    //in the game scene, we update players in game scene
                    case MultiplayerRacerScenes.GAME:
                        Master.UpdatePlayersInGameScene(false);
                        //if we where in game setup we show ready up info again.
                        if (CurrentGamePhase == GamePhase.SETUP)
                        {
                            ((GameUI)UI).SendShowReadyUpInfo();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Use this for the masterclient, when he leaves after the room is empty
        /// </summary>
        /// <returns></returns>
        private IEnumerator LastManLeaveWithDelay()
        {
            yield return new WaitForSeconds(RoomMaster.LAST_MAN_LEAVE_DELAY);
            MatchMakingManager.Instance.LeaveRoomForced();
        }

        /// <summary>
        /// Call this when a racer has finished to make the room manager
        /// handle this with his resources at hand
        /// </summary>
        private void SetRacerFinished(string playerName, string time)
        {
            if (CurrentScene != MultiplayerRacerScenes.GAME || !PhotonNetwork.IsMasterClient)
                return;
            //define whether the racer is the winning racer by checking player finished count
            bool winningRacer = Master.PlayersFinished == 0;
            //update players finished
            Master.UpdatePlayersFinished();
            if (Master.RaceIsFinished)
            {
                //if the race is finished, tell all players this
                PV.RPC("OnRaceEnded", RpcTarget.AllViaServer);
            }
            else
            {
                //if race is not finished, tell all clients to show information on player finishing
                PV.RPC("ShowPlayerFinishedInfo", RpcTarget.AllViaServer, playerName, time, winningRacer);
            }
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
            /*since players can only join the room when in the lobby scene
            we dont do anything if a player joins when in an other scene*/
            if (CurrentScene != MultiplayerRacerScenes.LOBBY)
                return;

            if (UI == null)
            {
                Debug.LogError("Wont update room :: UI is null");
                return;
            }

            Room room = PhotonNetwork.CurrentRoom;
            LobbyUI lobbyUI = (LobbyUI)UI;
            lobbyUI.UpdateRoomInfo(room);
            lobbyUI.UpdateReadyButtons(room.PlayerCount);
            FullRoomCheck(); //entering player can be the one to fill the lobby
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
            //master needs to deal with player leaving the room/him
            OnPlayerLeftMaster();
            //invoke shared functions
            Room room = PhotonNetwork.CurrentRoom;
            UI.UpdateRoomInfo(room);
            UI.UpdateNickname(MatchMakingManager.Instance.MakeNickname());
            //Update Room status based on current scene
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    lobbyUI.ResetReadyButtons(); //reset ready buttons when a player leaves
                    lobbyUI.UpdateReadyButtons(room.PlayerCount);
                    break;
            }
            SetReady(false);
            //if resetable, exit button and room status is shown and on scene reset gets fired
            if (IsResetAble)
            {
                UI.ShowExitButton();
                UI.ShowRoomStatus();
                OnSceneReset?.Invoke(CurrentScene);
            }
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey("FinishTime") && PhotonNetwork.IsMasterClient)
            {
                SetRacerFinished(targetPlayer.NickName, (string)changedProps["FinishTime"]);
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
        }

        [PunRPC]
        private void ShowPlayerFinishedInfo(string playerName, string time, bool winnerPlayer)
        {
            //if we where the racer finishing, we dont have the see the information
            if (PhotonNetwork.NickName == playerName)
                return;

            string text = $"{playerName} finished{(winnerPlayer ? " first " : " ")}at {time}";
            ((GameUI)UI).ShowText(text);
        }

        [PunRPC]
        private void OnRaceEnded()
        {
            CurrentGamePhase = GamePhase.FINISH;
            ((GameUI)UI).ShowLeaderboard();
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
        private void UpdateRoomMaster(int newRoomMasterNumber, RoomMaster newMaster)
        {
            //if your actornumber matches the new room master number you get the data
            if (PhotonNetwork.LocalPlayer.ActorNumber == newRoomMasterNumber)
            {
                Master = newMaster;
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
            //define count and room for check in playercount differnce during countdown
            Room room = PhotonNetwork.CurrentRoom;
            int count = room.PlayerCount;
            //Do countdown based on current Scene
            switch (CurrentScene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    ((LobbyUI)UI).StartCountDownForGameScene(LoadGameScene, () => count == room.PlayerCount);
                    break;

                case MultiplayerRacerScenes.GAME:
                    ((GameUI)UI).StartCountDownForRaceStart(OnRaceStart, () => count == room.PlayerCount);
                    break;
            }
        }
    }
}