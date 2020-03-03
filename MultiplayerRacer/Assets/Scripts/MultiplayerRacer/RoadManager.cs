using Photon.Pun;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] roads;

        private GameObject roadOn;
        private GameObject myCar;

        private void Awake()
        {
            //the player starts on the middle road
            roadOn = roads[1];

            if (PhotonNetwork.IsConnected)
            {
                //place the car spawns on the road for players and get our own car spawn
                Vector3 myCarSpawnPosition = SetupCarSpawns();
                myCar = PhotonNetwork.Instantiate("prefabs/car", myCarSpawnPosition, Quaternion.identity);
            }
            else Debug.LogError("Wont do car setup :: not connected to photon network");
        }

        /// <summary>
        /// Sets up car spawns and returns the world position of the one
        /// used by your car. Will return zero when failed.
        /// </summary>
        /// <returns></returns>
        private Vector3 SetupCarSpawns()
        {
            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            Transform road = roadOn.transform.Find("Road");
            if (carSpawnTransform == null || road == null)
            {
                Debug.LogError("Wont setup car spawns :: car spawn or road is null");
                return Vector3.zero;
            }
            int count = carSpawnTransform.childCount;
            float width = road.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            float carSpawnWidthHalf = carSpawnTransform.GetChild(0).GetComponent<SpriteRenderer>().sprite.bounds.size.x * 0.5f;
            float margin = (width - (carSpawnWidthHalf * count)) / (count + 1);
            float x = -(width * 0.5f) - (carSpawnWidthHalf * 0.5f);
            //loop through children based on count and place them on given position
            for (int ci = 0; ci < count; ci++)
            {
                x += margin + carSpawnWidthHalf;
                Transform tf = carSpawnTransform.GetChild(ci);
                Vector3 position = tf.localPosition;
                tf.localPosition = new Vector2(position.x + x, position.y);
                tf.gameObject.SetActive(true);
            }
            return carSpawnTransform.GetChild(InRoomManager.Instance.NumberInRoom).position;
        }
    }
}