﻿using UnityEngine;

namespace MultiplayerRacer
{
    using RoadType = RoadManager.RoadType;

    public class Road : MonoBehaviour
    {
        [SerializeField] private GameObject main;
        [SerializeField] private GameObject carSpawns;
        [SerializeField] private GameObject sidewalkFlex;
        [SerializeField] private GameObject roadBound;
        [SerializeField] private GameObject finish;

        public GameObject Main => main;
        public Bounds MainBounds => main.GetComponent<SpriteRenderer>().bounds;

        public GameObject CarSpawns => carSpawns;
        public Bounds carSpawnBounds => carSpawns.transform.GetChild(0).GetComponent<SpriteRenderer>().bounds;

        public Bounds SideWalkFlexBounds => sidewalkFlex.GetComponent<SpriteRenderer>().bounds;
        private Vector3 SideWalkFlexUpPosition => new Vector3(0, (MainBounds.size.y * 0.5f) - (SideWalkFlexBounds.size.y * 0.5f));

        private RoadType type;

        public RoadType Type
        {
            get => type;
            set
            {
                switch (value)
                {
                    case RoadType.DEFAULT: SetDefaultType(); break;
                    case RoadType.START: SetStartType(); break;
                    case RoadType.END: SetEndType(); break;
                }
            }
        }

        private void SetDefaultType()
        {
            sidewalkFlex.transform.localPosition = Vector3.zero;
            sidewalkFlex.SetActive(false);
            roadBound.SetActive(true);
            finish.SetActive(false);
        }

        private void SetStartType()
        {
            sidewalkFlex.transform.localPosition = -SideWalkFlexUpPosition;
            sidewalkFlex.SetActive(true);
            roadBound.SetActive(true);
            finish.SetActive(false);
        }

        private void SetEndType()
        {
            sidewalkFlex.transform.localPosition = SideWalkFlexUpPosition;
            sidewalkFlex.SetActive(true);
            roadBound.SetActive(false);
            finish.SetActive(true);
        }

        /// <summary>
        /// sets all car spawns to an inactive state
        /// </summary>
        public void SetCarSpawnsInactive()
        {
            //set all car spawns to an inactive state
            Transform carSpawnTransform = carSpawns.transform;
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                carSpawnTransform.GetChild(ci).gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Loops through all car spawns and try resetting them, if a car spawn
        /// is outside the player count bound, this car spawn will not be used
        /// so it will be made inactive
        /// </summary>
        public void ResetCarSpawns(int playerCount, Color unReadyColor)
        {
            //loop through all car spawns and try resetting them
            Transform carSpawnTransform = carSpawns.transform;
            for (int ci = 0; ci < carSpawnTransform.childCount; ci++)
            {
                GameObject carSpawn = carSpawnTransform.GetChild(ci).gameObject;
                carSpawn.GetComponent<SpriteRenderer>().color = unReadyColor;
                carSpawn.transform.localPosition = Vector3.zero;
                /*if this car spawn is outside the player count bound,
                this car spawn will not be used so it can be made inactive*/
                if ((ci + 1) > playerCount)
                {
                    carSpawn.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Sets up car spawns based on playerCount and returns the gameobject
        /// of the one used by your car. Will return null when failed.
        /// </summary>
        /// <returns></returns>
        public GameObject SetupCarSpawns(int playerCount, int numberInRoom)
        {
            Transform carSpawnTransform = carSpawns.transform;
            Transform road = main.transform;
            if (carSpawnTransform == null || road == null)
            {
                Debug.LogError("Wont setup car spawns :: car spawn or road is null");
                return null;
            }
            float width = MainBounds.size.x;
            float carSpawnWidthHalf = carSpawnBounds.size.x * 0.5f;
            float margin = (width - (carSpawnWidthHalf * playerCount)) / (playerCount + 1);
            float x = -(width * 0.5f) - (carSpawnWidthHalf * 0.5f);
            //loop through children based on count and place them on given position
            for (int ci = 0; ci < playerCount; ci++)
            {
                x += margin + carSpawnWidthHalf;
                Transform tf = carSpawnTransform.GetChild(ci);
                Vector3 position = tf.localPosition;
                tf.localPosition = new Vector2(position.x + x, position.y);
                tf.gameObject.SetActive(true);
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