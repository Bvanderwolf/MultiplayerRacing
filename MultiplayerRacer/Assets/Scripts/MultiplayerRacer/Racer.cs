﻿using UnityEngine;
using Photon.Pun;
using System;
using MultiplayerRacerEnums;
using ExitGames.Client.Photon;

namespace MultiplayerRacer
{
    public class Racer : MonoBehaviour, IPunInstantiateMagicCallback, IPunObservable
    {
        [SerializeField] private GameObject carCamera;
        [SerializeField] private SpriteRenderer spriteRend;
        [SerializeField] private Transform remoteCar;
        [SerializeField] private bool canRace;
        [SerializeField] private bool showRemote;

        public GameObject CarCamera => carCamera;
        public bool CanRace => canRace;

        /// <summary>
        /// event that will be called on road bound enter and on
        /// same entry as exit
        /// </summary>
        public event Action<GameObject, bool, bool> OnRoadBoundInteraction;

        private Quaternion cameraRotation;
        private Rigidbody2D RB;
        private PhotonView PV;

        private RacerInput remoteRacerInput;
        private RacerMotor motor;

        private Vector2 remotePosition;
        private float remoteRotation;
        private float distanceToRemote;
        private float angleToRemote;

        private float remoteInputV;
        private float remoteInputH;
        private bool remoteDrift;
        private bool remoteCollision;
        private bool colliding;

        private float lag;
        private float positionCatchupFactor;
        private float angleCatchupFactor;

        private float boundEnterAxisValue;
        private const float BOUND_AXIS_ERROR_MARGIN = 0.5f;
        private const float SMOOTHING_POSITION_OFFSET = 2f;

        [SerializeField] private float MAX_REMOTE_DISTANCE = 0.5f;
        [SerializeField] private float MAX_REMOTE_ANGLE = 15f;

        private TimeSpan startTime;

        private void Awake()
        {
            RB = GetComponent<Rigidbody2D>();
            motor = GetComponent<RacerMotor>();
            //store camera rotation for reset in late update
            cameraRotation = carCamera.transform.rotation;
            if (showRemote)
            {
                remoteCar.gameObject.SetActive(true);
            }
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = PhotonNetwork.SendRate;
        }

        //Update during render frames
        private void FixedUpdate()
        {
            //update other clients their car
            if (!PV.IsMine)
            {
                SimulateCar();
                CorrectCarSimulation();
                UpdateRemoteCarGhost();

                if (NotVisible())
                {
                    //if the car is remote car is not visible, set the position and rotation directly
                    RB.position = remotePosition;
                    RB.rotation = remoteRotation;
                }
            }
        }

        private void LateUpdate()
        {
            //for now simply reset camera rotation after all updates
            carCamera.transform.rotation = cameraRotation;
        }

        //uses remote inputs to simulate the car
        private void SimulateCar()
        {
            bool racing = InRoomManager.Instance.CurrentGamePhase == GamePhase.RACING;
            if (racing)
            {
                //only simulate remote car based on remote inputs if we are in the racing phase
                remoteRacerInput.SimulateRemote(remoteInputV, remoteInputH, remoteDrift);
            }
        }

        //linearly interpolates between position and remote position
        private void CorrectCarSimulation()
        {
            float tickTime = 1f / PhotonNetwork.SerializationRate;

            bool distanceCatchup = distanceToRemote > MAX_REMOTE_DISTANCE;
            bool angleCatchUp = angleToRemote > MAX_REMOTE_ANGLE;

            float movedelta = (distanceToRemote * tickTime);
            float rotateDelta = (angleToRemote * tickTime);

            float movedelta_lag = distanceToRemote * (tickTime * lag);
            float rotateDelta_lag = angleToRemote * (tickTime * lag);

            float maxMoveDelta = (distanceCatchup ? movedelta * positionCatchupFactor : movedelta);
            float maxRotateDelta = (angleCatchUp ? rotateDelta * angleCatchupFactor : rotateDelta);

            //correct errors by delayed remote input by linear interpolation towards remote position and rotation
            RB.position = Vector2.MoveTowards(RB.position, remotePosition, maxMoveDelta);
            RB.rotation = Mathf.MoveTowards(RB.rotation, remoteRotation, maxRotateDelta);
        }

        //updates remote car as ghost with remote position and remote rotation
        private void UpdateRemoteCarGhost()
        {
            remoteCar.position = remotePosition;
            remoteCar.eulerAngles = new Vector3(0, 0, remoteRotation);
        }

        /// <summary>
        /// returns whether the remote car is visible or not
        /// </summary>
        /// <returns></returns>
        private bool NotVisible()
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            return !GeometryUtility.TestPlanesAABB(planes, remoteCar.GetComponent<SpriteRenderer>().bounds);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            bool racing = InRoomManager.Instance.CurrentGamePhase == GamePhase.RACING;
            if (stream.IsWriting)
            {
                //send position and rotation of simulated car
                stream.SendNext(RB.position);
                stream.SendNext(RB.rotation);
                stream.SendNext(RB.velocity);
                stream.SendNext(RB.angularVelocity);
                stream.SendNext(colliding);

                if (racing)
                {   //if the player is racing, send inputs
                    stream.SendNext(RacerInput.GasInput);
                    stream.SendNext(RacerInput.SteerInput);
                    stream.SendNext(RacerInput.DriftInput);
                }
            }
            else
            {
                lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

                remotePosition = (Vector2)stream.ReceiveNext();
                remoteRotation = (float)stream.ReceiveNext();
                remotePosition += (Vector2)stream.ReceiveNext() * lag;
                remoteRotation += (float)stream.ReceiveNext() * lag;
                remoteCollision = (bool)stream.ReceiveNext();

                bool distanceCatchup = distanceToRemote > MAX_REMOTE_DISTANCE;
                bool angleCatchUp = angleToRemote > MAX_REMOTE_ANGLE;

                if (distanceCatchup && remoteCollision)
                    RB.position = remotePosition;

                if (angleCatchUp && remoteCollision)
                    RB.rotation = remoteRotation;

                distanceToRemote = (remotePosition - RB.position).magnitude;
                positionCatchupFactor = 1 + (distanceToRemote - MAX_REMOTE_DISTANCE);

                angleToRemote = Mathf.Abs(remoteRotation - RB.rotation);
                angleCatchupFactor = 1 + (angleToRemote - MAX_REMOTE_ANGLE);

                GameUI.SetDistanceToRemote(distanceToRemote);
                GameUI.SetPositionCatchupFactor(positionCatchupFactor);
                GameUI.SetAngleToRemote(angleToRemote);
                GameUI.SetAngleCatchupFactor(angleCatchupFactor);
                GameUI.SetLag(lag);

                if (racing)
                {
                    //if the player is racing, receive inputs
                    remoteInputV = (float)stream.ReceiveNext();
                    remoteInputH = (float)stream.ReceiveNext();
                    remoteDrift = (bool)stream.ReceiveNext();
                }
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            PhotonView _PV = info.photonView;
            PV = _PV;
            if (_PV.IsMine)
            {
                //to stuff with photon message info
                info.Sender.TagObject = this.gameObject;

                //subcribe to in room events
                InRoomManager.Instance.OnGameStart += OnRacerCanStart;
                InRoomManager.Instance.OnSceneReset += OnRacerReset;

                //make others disable our camera
                _PV.RPC("DisableCamera", RpcTarget.OthersBuffered, _PV.ViewID);
            }
            else
            {
                remoteRacerInput = GetComponent<RacerInput>();
            }
        }

        /// <summary>
        /// should be called when the scene resets, resets rigidbody of car
        /// </summary>
        /// <param name="scene"></param>
        private void OnRacerReset(MultiplayerRacerScenes scene)
        {
            if (scene != MultiplayerRacerScenes.GAME)
                return;

            RB.velocity = Vector2.zero;
            RB.angularVelocity = 0;
            RB.rotation = 0;
            canRace = false;
        }

        /// <summary>
        /// should be called when the car is able to race
        /// </summary>
        private void OnRacerCanStart()
        {
            canRace = true;
            startTime = DateTime.UtcNow.TimeOfDay;
        }

        private void AddFinishInfoToHashTable(string time)
        {
            Hashtable table = new Hashtable();
            table.Add("FinishTime", time);
            PhotonNetwork.LocalPlayer.SetCustomProperties(table);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!PV.IsMine)
                return;

            if (collision.tag == "RoadBound")
            {
                //store y value (can be x to depending on orientation of bound)
                boundEnterAxisValue = transform.position.y;
                //fire event returning the parent gameobject which is the RoadPiece
                OnRoadBoundInteraction(collision.transform.parent.gameObject, false, false);
            }
            else if (collision.tag == "Finish")
            {
                //get the difference between end and start time
                TimeSpan endTime = DateTime.UtcNow.TimeOfDay;
                string time = endTime.Subtract(startTime).ToString("c");

                //add finish time to hash table
                AddFinishInfoToHashTable(time);
                //set canRace to false so the player can't input but the car can ease out
                canRace = false;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!PV.IsMine)
                return;

            if (collision.tag == "RoadBound")
            {
                //it is possible that when resetting we exit a bound. we want to account for this
                bool boundExitAfterReset = !canRace;
                //get exit y value (can be x to depending on orientation of bound)
                float boundExitAxisValue = transform.position.y;
                float diff = boundEnterAxisValue - boundExitAxisValue;
                //define whether the exit direction is the same as the direction of entry
                bool sameExitAsEntry = diff >= -BOUND_AXIS_ERROR_MARGIN && diff <= BOUND_AXIS_ERROR_MARGIN;
                //if the exit is the same as the entry we need to shift the road back
                OnRoadBoundInteraction(collision.transform.parent.gameObject, true, sameExitAsEntry || boundExitAfterReset);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //reset drift when colliding with something
            motor.ResetDrift();
            colliding = true;
        }

        private void OnCollisionExit(Collision collision)
        {
            colliding = false;
        }

        [PunRPC]
        private void DisableCamera(int viewID)
        {
            //get the camera of the car in our scene based on viewID and set it to inactive
            PhotonView PV = PhotonView.Find(viewID);
            GameObject carCamera = PV.GetComponent<Racer>()?.CarCamera;
            if (carCamera != null)
            {
                carCamera.SetActive(false);
            }
            else Debug.LogError("Won't set car camera to inactive :: carCamera object is null");
        }
    }
}