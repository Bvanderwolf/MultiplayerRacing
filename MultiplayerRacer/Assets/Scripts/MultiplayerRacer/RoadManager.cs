using UnityEngine;

namespace MultiplayerRacer
{
    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] roads;

        private GameObject roadOn;

        private void Awake()
        {
            //the player starts on the middle road
            roadOn = roads[1];
            //place the car spawns on the road for players
            SetupCarSpawns();
        }

        private void SetupCarSpawns()
        {
            Transform carSpawnTransform = roadOn.transform.Find("CarSpawns");
            Transform road = roadOn.transform.Find("Road");
            if (carSpawnTransform == null || road == null)
            {
                Debug.LogError("Wont setup car spawns :: car spawn or road is null");
                return;
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
        }
    }
}