using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using MultiplayerRacerEnums;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System.Collections.Generic;

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
        public int PlayersPlaying { get; private set; }
        public bool IsReady { get; private set; } = false;

        /// <summary>
        /// name of track set in lobby
        /// </summary>
        public string NameOfTrackChoosen { get; private set; } = "Default";

        public event Action<MultiplayerRacerScenes, bool> OnReadyStatusChange;

        public event Action OnGameStart;

        /// <summary>
        /// called before OnGameStart in case of a game reset
        /// </summary>
        public event Action OnGameRestart;

        public event Action<MultiplayerRacerScenes> OnSceneReset;

        public MultiplayerRacerScenes CurrentScene { get; private set; } = MultiplayerRacerScenes.LOBBY;
        public GamePhase CurrentGamePhase { get; private set; } = GamePhase.NONE;

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
        private const int RANDOM_SEED = 133;

        private PhotonView PV;
        private MultiplayerRacerUI UI = null;
        private InRoomManagerHelper helper = null;
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

            helper = new InRoomManagerHelper();

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

        //sets number in the room
        public void UpdateNumberInRoom()
        {
            int num = helper.GetNumberInRoomOfPlayer(PhotonNetwork.LocalPlayer);
            Debug.LogError("Our number in Room: " + num);
            NumberInRoom = num;
        }

        /// <summary>
        /// Returns car sprites used for car select
        /// </summary>
        /// <returns></returns>
        public List<Sprite> GetSelectableCarSprites()
        {
            return helper?.CarSpritesSelectable;
        }

        /// <summary>
        /// Returns a non read/write car sprite used for
        /// the game scene
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Sprite GetUsableCarSprite(int index)
        {
            return helper?.CarSpritesUsable[index];
        }

        /// <summary>
        /// returns number in room of player that corresponds
        /// with given actornumber
        /// </summary>
        /// <param name="actorNumber"></param>
        /// <returns></returns>
        public int GetRoomNumberOfActor(int actorNumber)
        {
            return helper.GetNumberInRoomOfPlayer(actorNumber);
        }

        /// <summary>
        /// returns number in room of given player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public int GetRoomNumberOfActor(Player player)
        {
            return helper.GetNumberInRoomOfPlayer(player);
        }

        public int GetActorNumberOfPlayerInRoom(int playerNumber)
        {
            return helper.GetActorNumberOfPlayerInRoom(playerNumber);
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
        /// Called by the master client to let all players restart and reset their
        /// scenes for another game play round
        /// </summary>
        public void SendGameRestart()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PV.RPC("RestartGameScene", RpcTarget.AllViaServer);
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
            switch (scene)
            {
                case MultiplayerRacerScenes.LOBBY:
                    //we can only start if the maxium amount of players in the lobby are ready
                    if (Master.PlayersReady != PhotonNetwork.CurrentRoom.MaxPlayers)
                        return;
                    break;

                case MultiplayerRacerScenes.GAME:
                    //we can only start racing if all players in the games scene are ready
                    if (Master.PlayersReady != Master.PlayersInGameScene)
                        return;
                    break;
            }

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
            //set randomness seed for each client to be the same.
            Random.InitState(RANDOM_SEED);

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
            Debug.LogError("loaded");
            //and set it up
            Room room = PhotonNetwork.CurrentRoom;
            UI.SetupRoomInfo(PhotonNetwork.NickName, room);
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
            if (!PhotonNetwork.IsMasterClient)
                return;

            switch (CurrentScene)
            {
                //in the lobby scene we just reset the players ready
                case MultiplayerRacerScenes.LOBBY:
                    Master.ResetPlayersReady();
                    break;
                //in the game scene, we update players in game scene
                case MultiplayerRacerScenes.GAME:
                    //if the master client is the only one left in the game scene he leaves the room
                    if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
                    {
                        ((GameUI)UI).ShowText("Last player in game scene :: Leaving Room");
                        StartCoroutine(LastManLeaveWithDelay());
                        return;
                    }
                    Master.UpdatePlayersInGameScene(false);
                    switch (CurrentGamePhase)
                    {
                        //in the setup gamephase send all players ready up info again and players ready is reset
                        case GamePhase.SETUP:
                            Master.ResetPlayersReady();
                            ((GameUI)UI).SendShowReadyUpInfo();
                            break;

                        case GamePhase.RACING:
                            //the race can be finished if the last player left to finish, left the room
                            if (Master.RaceIsFinished)
                            {
                                Player[] finishedPlayersOrdered = helper.GetFinishedPlayersOrdered();
                                PV.RPC("OnRaceEnded", RpcTarget.AllViaServer, finishedPlayersOrdered);
                            }
                            break;
                    }
                    break;
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
                //get a list of players ordered based on finish time
                Player[] finishedPlayersOrdered = helper.GetFinishedPlayersOrdered();
                //if the race is finished, tell all players this
                PV.RPC("OnRaceEnded", RpcTarget.AllViaServer, finishedPlayersOrdered);
            }
            else
            {
                //format string so unnecessary miliseconds is not shown
                time = time.Split('.')[0];
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
                switch (CurrentScene)
                {
                    case MultiplayerRacerScenes.GAME:
                        ((GameUI)UI).ShowText($"Host migrated :: {newMasterClient.NickName} is the new host");
                        break;
                }
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
            lobbyUI.UpdatePlayerInfo(room.PlayerCount);
            FullRoomCheck(); //entering player can be the one to fill the lobby
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            //Update our number in room since
            UpdateNumberInRoom();
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
                    //reset all player related info, then update it again
                    LobbyUI lobbyUI = (LobbyUI)UI;
                    lobbyUI.ResetPlayerInfo(); 
                    lobbyUI.OnPlayerLeftSelectedCar(otherPlayer);
                    lobbyUI.UpdatePlayerInfo(room.PlayerCount);
                    break;

                case MultiplayerRacerScenes.GAME:
                    ((GameUI)UI).ShowText($"{otherPlayer.NickName} left the game");
                    break;
            }
            /*if resetable, set ourselfs to unready status, exit button and room status is shown
            and on scene reset gets fired*/
            if (IsResetAble)
            {
                SetReady(false);
                UI.ShowExitButton();
                UI.SetButtonInfoActiveState(true);
                OnSceneReset?.Invoke(CurrentScene);
            }
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(Racer.FINISH_TIME_HASHTABLE_KEY) && PhotonNetwork.IsMasterClient)
            {
                string time = (string)changedProps[Racer.FINISH_TIME_HASHTABLE_KEY];
                SetRacerFinished(targetPlayer.NickName, time);
            }
            if (changedProps.ContainsKey(LobbyUI.SPRITE_INDEX_HASHTABLE_KEY) && CurrentScene == MultiplayerRacerScenes.LOBBY)
            {
                int numberInRoom = helper.GetNumberInRoomOfPlayer(targetPlayer);
                int carSpriteIndex = (int)changedProps[LobbyUI.SPRITE_INDEX_HASHTABLE_KEY];
                ((LobbyUI)UI).SetPlayerInfoCarSprite(numberInRoom, carSpriteIndex, helper.CarSpritesUsable[carSpriteIndex]);
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

            string text = $"{playerName} finished{(winnerPlayer ? " first " : " ")}after {time}";
            ((GameUI)UI).ShowText(text);
        }

        [PunRPC]
        private void OnRaceEnded(Player[] finishedPlayersOrdered)
        {
            CurrentGamePhase = GamePhase.FINISH;

            GameUI gameUI = (GameUI)UI;
            gameUI.ShowLeaderboard(finishedPlayersOrdered);
            if (PhotonNetwork.IsMasterClient)
            {
                //the masterclient will have options to choose from on end
                gameUI.ShowRaceEndedOptionsWithDelay();
            }
        }

        [PunRPC]
        private void UpdatePlayersInGameScene(bool join)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //let the room master update players in game scene
                Master.UpdatePlayersInGameScene(join);

                //if all players are in the game scene, the master client can start the game
                if (Master.PlayersInGameScene == PhotonNetwork.CurrentRoom.PlayerCount)
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
                switch (CurrentScene)
                {
                    case MultiplayerRacerScenes.LOBBY:
                        //check for max players ready in lobby each time
                        MaxPlayersReadyCheck(CurrentScene);
                        break;

                    case MultiplayerRacerScenes.GAME:
                        //only check for max players ready if all players are in game scene
                        bool allPlayersInGameScene = Master.PlayersInGameScene == PhotonNetwork.CurrentRoom.PlayerCount;
                        if (allPlayersInGameScene && CurrentGamePhase == GamePhase.SETUP)
                        {
                            MaxPlayersReadyCheck(CurrentScene);
                        }
                        break;
                }
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
        private void RestartGameScene()
        {
            //set current game phase back to setup
            CurrentGamePhase = GamePhase.SETUP;

            //set ready status to false
            IsReady = false;

            //call on restart and then on scene reset
            OnGameRestart();
            OnSceneReset(CurrentScene);

            //update game UI
            GameUI gameUI = (GameUI)UI;
            gameUI.HideLeaderBoard();

            //master client has its own additional updates
            if (PhotonNetwork.IsMasterClient)
            {
                gameUI.SendShowReadyUpInfo();
                Master.ResetPlayersFinished();
                Master.ResetPlayersReady();
            }
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