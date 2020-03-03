using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        public static InRoomManager Instance { get; private set; }
        public static readonly byte[] memRoomMaster = new byte[4];

        private LobbyUI lobbyUI = null;

        public int NumberInRoom { get; private set; } = 0;
        public bool IsReady { get; private set; } = false;

        private int currentLevelIndex = 0;

        public int NextLevelIndex
        {
            get
            {
                //return next value only if not out of scene count bounds, else -1
                int next = currentLevelIndex + 1;
                if (next <= SceneManager.sceneCountInBuildSettings)
                {
                    return next;
                }
                else return -1;
            }
        }

        public bool InLobby => currentLevelIndex == 0;
        public bool InGame => currentLevelIndex == 1;

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
            lobbyUI = GameObject.FindGameObjectWithTag("Canvas")?.GetComponent<LobbyUI>();
        }

        public void SetToLobby()
        {
            currentLevelIndex = 0;
            IsReady = false;
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
            IsReady = value;
            GetComponent<PhotonView>().RPC("UpdatePlayersReady", RpcTarget.MasterClient, IsReady);
        }

        /// <summary>
        ///resets RoomMaster instance value
        /// </summary>
        public void ResetIsRoomMaster()
        {
            Master = null;
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
        /// checks whether the room is full or not acts accordingly if so
        /// </summary>
        /// <param name="room"></param>
        private void FullRoomCheck(Room room)
        {
            if (room.PlayerCount == MatchMakingManager.MAX_PLAYERS)
            {
                lobbyUI.ListenToReadyButton();
            }
        }

        /// <summary>
        /// Used by the master client to load the game scene
        /// </summary>
        private void LoadGameScene()
        {
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

        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //reset lobbyUI value since we are now in the game scene
            lobbyUI = null;
            //unsubscribe from scene loaded event
            SceneManager.sceneLoaded += OnGameSceneLoaded;
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            /*Note: photon assigns new masterclient when current one leaves.
            New one is the one with the lowest actor number, so first one to join
            should correlate to NumberInRoom*/
            if (lobbyUI != null)
            {
                lobbyUI.UpdateIsMasterclient();
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            //Update Room status
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
                lobbyUI.UpdateReadyButtons(room.PlayerCount);
                FullRoomCheck(room); //entering player can be the one to fill the room
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");
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

            //Update Room status
            if (lobbyUI != null)
            {
                Room room = PhotonNetwork.CurrentRoom;
                lobbyUI.UpdateRoomInfo(room);
                lobbyUI.UpdateNickname(MatchMakingManager.Instance.MakeNickname());
                //if we where in the lobby reset ready status
                if (InLobby)
                {
                    lobbyUI.ResetReadyButtons(); //reset ready buttons when a player leaves
                    lobbyUI.UpdateReadyButtons(room.PlayerCount);
                    lobbyUI.ShowExitButton(); //handle edge cases where exit button is in hidden state
                    SetReady(false);
                }
            }
            else Debug.LogError("Wont update room :: lobbyUI is null");

            //the masterclient resets the players ready count when someone leaves
            if (PhotonNetwork.IsMasterClient)
            {
                Master.ResetPlayersReady();
            }
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
                int off = 0;
                Protocol.Serialize(rm.PlayersReady, bytes, ref off);
                outStream.Write(bytes, 0, 4);
                return 4;
            }
        }

        private static object DeserializeRoomMaster(StreamBuffer inStream, short length)
        {
            int playersready;
            lock (memRoomMaster)
            {
                inStream.Read(memRoomMaster, 0, 4);
                int off = 0;
                Protocol.Deserialize(out playersready, memRoomMaster, ref off);
            }
            RoomMaster rm = new RoomMaster(playersready);

            return rm;
        }

        [PunRPC]
        private void UpdatePlayersReady(bool isready)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //let the room master update players ready
                Master.UpdatePlayersReady(isready);
                Debug.LogError($"isready: {isready}, players ready: {Master.PlayersReady}");
                //start countdown if all players are ready
                if (Master.PlayersReady == MatchMakingManager.MAX_PLAYERS)
                {
                    //not buffered because no players can join after the countdown has started
                    GetComponent<PhotonView>().RPC("StartCountdown", RpcTarget.AllViaServer);
                }
            }
        }

        [PunRPC]
        private void UpdateRoomMaster(int newRoomMasterNumber, RoomMaster newMaster, bool wasLeaving)
        {
            //if your actornumber matches the new room master number you get the data
            if (PhotonNetwork.LocalPlayer.ActorNumber == newRoomMasterNumber)
            {
                Master = newMaster;
                Debug.LogError("New master players ready gained: " + newMaster.PlayersReady);
                //if the master client was leaving we make sure to handle that
                if (wasLeaving && InLobby)
                {
                    Master.ResetPlayersReady();
                }
            }
        }

        [PunRPC]
        private void StartCountdown()
        {
            if (lobbyUI != null)
            {
                //subscribe client to scene loaded event before starting the room countdown
                SceneManager.sceneLoaded += OnGameSceneLoaded;
                lobbyUI.StartGameCountDown(LoadGameScene, () =>
                {
                    Debug.LogError(IsReady);
                    return IsReady;
                });
            }
        }
    }
}