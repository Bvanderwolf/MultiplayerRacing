﻿using MultiplayerRacerEnums;
using Photon.Pun;
using UnityEngine;

namespace MultiplayerRacer
{
    public class Road : MonoBehaviour
    {
        [SerializeField] private GameObject main;
        [SerializeField] private GameObject carSpawns;
        [SerializeField] private GameObject sidewalkFlex;
        [SerializeField] private GameObject roadBound;
        [SerializeField] private GameObject finish;
        [SerializeField] private GameObject startLights;
        [SerializeField] private RoadProps props;
        [SerializeField] private RoadEnvironment[] environments;

        public GameObject Main => main;
        public Bounds MainBounds => main.GetComponent<SpriteRenderer>().bounds;

        public GameObject CarSpawns => carSpawns;
        public Bounds carSpawnBounds => carSpawns.transform.GetChild(0).GetComponent<SpriteRenderer>().bounds;

        public Bounds SideWalkFlexBounds => sidewalkFlex.GetComponent<SpriteRenderer>().bounds;
        private Vector3 SideWalkFlexUpPosition => new Vector3(0, (MainBounds.size.y * 0.5f) + (SideWalkFlexBounds.size.y * 0.5f));

        public void SetupRoad(RoadType[] types, int trackIndex)
        {
            SetRoadType(types, trackIndex);
            ConfigureRoadProps(types.Length, trackIndex);
            ConfigureEnvironment(trackIndex, types[trackIndex]);
        }

        /// <summary>
        /// sets up road type based on index and given types
        /// </summary>
        /// <param name="types"></param>
        /// <param name="trackIndex"></param>
        private void SetRoadType(RoadType[] types, int trackIndex)
        {
            string name = types[trackIndex].ToString();

            if (name.Contains("DEFAULT"))
                SetDefaultType();
            else if (name.Contains("START"))
                SetStartType();
            else if (name.Contains("END"))
                SetEndType();
            else
                Debug.LogError("Type " + name + "is not valid");
        }

        /// <summary>
        /// Sets up the default road type
        /// </summary>
        private void SetDefaultType()
        {
            sidewalkFlex.transform.localPosition = Vector3.zero;
            sidewalkFlex.SetActive(false);
            roadBound.SetActive(true);
            finish.SetActive(false);
            startLights.SetActive(false);
            SetCarSpawnsActive(false);
        }

        /// <summary>
        /// Sets up the start road type
        /// </summary>
        private void SetStartType()
        {
            sidewalkFlex.transform.localPosition = -SideWalkFlexUpPosition;
            sidewalkFlex.SetActive(true);
            roadBound.SetActive(true);
            finish.SetActive(false);
            startLights.SetActive(true);
            SetCarSpawnsActive(true);
        }

        /// <summary>
        /// Sets up the end road type
        /// </summary>
        private void SetEndType()
        {
            sidewalkFlex.transform.localPosition = SideWalkFlexUpPosition;
            sidewalkFlex.SetActive(true);
            roadBound.SetActive(false);
            finish.SetActive(true);
            startLights.SetActive(false);
            SetCarSpawnsActive(false);
        }

        /// <summary>
        /// configures road props based on track length and track index
        /// </summary>
        /// <param name="trackIndex"></param>
        private void ConfigureRoadProps(int trackLength, int trackIndex)
        {
            //define not configured
            bool notConfigured = trackIndex >= RoadManager.PropConfiguration.Count;
            if (notConfigured)
            {
                //define if index represents start or end road
                bool startOrEnd = trackIndex == 0 || trackIndex == trackLength - 1;
                //if not configured already, add the prop config
                RoadManager.PropConfiguration.Add(props.Setup(startOrEnd));
            }
            else
            {
                //if already configured, use stored configuration
                props.Setup(RoadManager.PropConfiguration[trackIndex]);
            }
        }

        /// <summary>
        /// configures road environment based on track length and track index
        /// </summary>
        /// <param name="trackLength"></param>
        /// <param name="trackIndex"></param>
        private void ConfigureEnvironment(int trackIndex, RoadType type)
        {
            //define not configured
            bool notConfigured = trackIndex >= RoadManager.EnvironmentConfiguration.Count;
            if (notConfigured)
            {
                string config = "";
                //update config with environments settings
                for (int i = 0; i < environments.Length; i++)
                {
                    config += environments[i].Setup(type);
                }
                //add settng to configuration list
                RoadManager.EnvironmentConfiguration.Add(config);
            }
            else
            {
                //get setting
                string config = RoadManager.EnvironmentConfiguration[trackIndex];
                //update environments
                for (int i = 0; i < environments.Length; i++)
                {
                    environments[i].Setup(ref config);
                }
            }
        }

        /// <summary>
        /// sets all car spawns to an inactive state
        /// </summary>
        private void SetCarSpawnsActive(bool value)
        {
            //set all car spawns to an inactive state
            Transform carSpawnTransform = carSpawns.transform;
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                carSpawnTransform.GetChild(ci).gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// Loops through all car spawns and try resetting them, if a car spawn
        /// is outside the player count bound, this car spawn will not be used
        /// so it will be made inactive
        /// </summary>
        public void ResetCarSpawns()
        {
            //loop through all car spawns and try resetting them
            Transform carSpawnTransform = carSpawns.transform;
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                GameObject carSpawn = carSpawnTransform.GetChild(ci).gameObject;
                carSpawn.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        /// <summary>
        /// Sets up car spawns based on playerCount and returns the gameobject
        /// of the one used by your car. Will return null when failed.
        /// </summary>
        /// <returns></returns>
        public GameObject SetupCarSpawns(int playerCount, int numberInRoom, Color unreadyColor)
        {
            Transform carSpawnTransform = carSpawns.transform;
            Transform road = main.transform;
            if (carSpawnTransform == null || road == null)
            {
                Debug.LogError("Wont get car spawn :: car spawn or road is null");
                return null;
            }
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                bool used = ci < playerCount;
                if (used)
                {
                    GameObject carSpawn = carSpawnTransform.GetChild(ci).gameObject;
                    carSpawn.GetComponent<SpriteRenderer>().color = unreadyColor;
                }
            }
            //return the position of the child based on our number in the room
            return carSpawnTransform.GetChild(numberInRoom - 1).gameObject;
        }

        /// <summary>
        /// Shifts the road based on given values (for now y increase/decrease only)
        /// </summary>
        public void Shift(float newY)
        {
            transform.localPosition += new Vector3(0, newY);
        }
    }
}