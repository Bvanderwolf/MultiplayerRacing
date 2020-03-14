using MultiplayerRacerEnums;
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
        [SerializeField] private RoadProps props;

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
        }

        /// <summary>
        /// sets up road type based on index and given types
        /// </summary>
        /// <param name="types"></param>
        /// <param name="trackIndex"></param>
        private void SetRoadType(RoadType[] types, int trackIndex)
        {
            switch (types[trackIndex])
            {
                case RoadType.DEFAULT: SetDefaultType(); break;
                case RoadType.START: SetStartType(); break;
                case RoadType.END: SetEndType(); break;
            }
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
                RoadManager.PropConfiguration.Add(props.SetupProps(startOrEnd, MainBounds));
            }
            else
            {
                //if already configured, use stored configuration
                props.SetupProps(RoadManager.PropConfiguration[trackIndex]);
            }
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