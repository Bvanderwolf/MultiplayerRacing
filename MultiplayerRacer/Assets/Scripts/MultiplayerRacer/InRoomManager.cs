using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MultiplayerRacer
{
    public class InRoomManager : MonoBehaviour, IInRoomCallbacks
    {
        public static InRoomManager Instance { get; private set; }
        public static readonly byte[] memRoomMaster = new byte[2 * 4];

        private LobbyUI lobbyUI = null;

        public int NumberInRoom { get; private set; } = 0;
        public bool IsReady { get; private set; } = false;

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

        /// <summary>
        /// sets InroomManager its isready value and updates the master client with this value
        /// </summary>
        /// <param name="value"></param>
        public void SetReady(bool value)
        {
            IsReady = value;
            GetComponent<PhotonView>().RPC("UpdatePlayersReady", RpcTarget.MasterClient, IsReady);
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

        public void SetNumberInRoom(MatchMakingManager manager, int number)
        {
            if (manager == MatchMakingManager.Instance)
            {
                NumberInRoom = number;
            }
        }

        public void SwitchRoomMaster(int newMasterNumber)
        {
            if (newMasterNumber < 0)
                return;

            //send RPC call with master client data to new master client
            GetComponent<PhotonView>().RPC("UpdateRoomMaster", RpcTarget.All, newMasterNumber);
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

        private static short SerializeRoomMaster(StreamBuffer outStream, object customobject)
        {
            RoomMaster rm = (RoomMaster)customobject;

            int index = 0;
            lock (memRoomMaster)
            {
                byte[] bytes = memRoomMaster;
                Protocol.Serialize(rm.PlayersReady, bytes, ref index);
                Protocol.Serialize(rm.CurrentLevelIndex, bytes, ref index);
                outStream.Write(bytes, 0, 2 * 4);
            }

            return 2 * 4;
        }

        private static object DeserializeRoomMaster(StreamBuffer inStream, short length)
        {
            RoomMaster rm = new RoomMaster();
            lock (memRoomMaster)
            {
                inStream.Read(memRoomMaster, 0, 2 * 4);
                int index = 0;
                Protocol.Deserialize(out rm.PlayersReady, memRoomMaster, ref index);
                Protocol.Deserialize(out rm.CurrentLevelIndex, memRoomMaster, ref index);
            }

            return rm;
        }

        [PunRPC]
        private void UpdatePlayersReady(bool isready)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                //let the room master update players ready
                Master.UpdatePlayersReady(isready);
                //start countdown if all players are ready
                if (Master.PlayersReady == MatchMakingManager.MAX_PLAYERS)
                {
                    //not buffered because no players can join after the countdown has started
                    GetComponent<PhotonView>().RPC("StartCountdown", RpcTarget.AllViaServer);
                }
            }
        }

        [PunRPC]
        private void UpdateRoomMaster(int newRoomMasterNumber)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newRoomMasterNumber)
            {
                Debug.LogError("i get the master client data!");
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