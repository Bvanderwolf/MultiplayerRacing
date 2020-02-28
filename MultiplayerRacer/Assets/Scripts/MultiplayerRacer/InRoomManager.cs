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
        private LobbyUI lobbyUI = null;

        public int NumberInRoom { get; private set; } = 0;
        public bool IsReady { get; private set; } = false;

        public const int COUNTDOWN_LENGTH = 3;

        /*
         master client attributes (have default value if not master client)
         */
        private int playersReady = 0;
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

        /// <summary>
        /// sets InroomManager its isready value and updates the master client with this value
        /// </summary>
        /// <param name="value"></param>
        public void SetReady(bool value)
        {
            IsReady = value;
            GetComponent<PhotonView>().RPC("UpdatePlayersReady", RpcTarget.MasterClient, IsReady);
        }

        public void SetNumberInRoom(MatchMakingManager manager, int number)
        {
            if (manager == MatchMakingManager.Instance)
            {
                NumberInRoom = number;
            }
        }

        /// <summary>
        /// checks whether the room is full or not acts accordingly if so
        /// </summary>
        /// <param name="room"></param>
        private void FullRoomCheck(Room room)
        {
            if (room.PlayerCount == MatchMakingManager.MAX_PLAYERS)
            {
                lobbyUI.ListenToReadyButton(this);
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
                FullRoomCheck(room); //entering player can be the one to fill the room
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
                lobbyUI.ResetReadyButtons(); //reset ready buttons when a player leaves
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

        [PunRPC]
        private void UpdatePlayersReady(bool isready)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                playersReady += isready ? 1 : -1;
                if (playersReady == MatchMakingManager.MAX_PLAYERS)
                {
                    //not buffered because no players can join after the countdown has started
                    GetComponent<PhotonView>().RPC("StartCountdown", RpcTarget.AllViaServer);
                }
            }
        }

        [PunRPC]
        private void StartCountdown()
        {
            if (lobbyUI != null)
            {
                lobbyUI.DoCountDown();
            }
        }
    }
}