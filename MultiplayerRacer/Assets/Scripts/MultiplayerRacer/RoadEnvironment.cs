using MultiplayerRacerEnums;
using UnityEngine;

namespace MultiplayerRacer
{
    [System.Serializable]
    public class RoadEnvironment
    {
        [SerializeField] private Transform pavilionPositions;
        [SerializeField] private Transform TreePositions;
        [SerializeField] private Transform housePositions;

        /// <summary>
        /// Sets up environment based on given type, modifying
        /// the config, appending the new setup to the config string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="config"></param>
        public void Setup(RoadType type, out string config)
        {
            string _config = "";
            string typeName = type.ToString();

            _config += SetupFolliage(typeName);
            _config += SetupHouses(typeName);
            _config += SetupPavilion(typeName);

            config = _config;
        }

        private string SetupFolliage(string type)
        {
            bool none = !type.Contains("FULL");
            if (none)
            {
                for (int ci = 0; ci < TreePositions.childCount; ci++)
                    TreePositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                return "0";
            }
            int childCount = TreePositions.childCount;
            int showCount = type.Contains("HALF") ? Mathf.RoundToInt(childCount * 0.5f) : childCount;
            string config = showCount.ToString();
            Sprite[] folliage = RoadManager.EnvironmentObjects["Folliage"];
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = TreePositions.GetChild(ci).GetComponent<SpriteRenderer>();
                if (canShow)
                {
                    int index = Random.Range(0, folliage.Length);
                    renderer.sprite = folliage[index];
                    config += index;
                }
                else
                {
                    renderer.sprite = null;
                }
            }
            return config;
        }

        private string SetupHouses(string type)
        {
            bool none = !type.Contains("FULL");
            if (none)
            {
                for (int ci = 0; ci < housePositions.childCount; ci++)
                    housePositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                return "0";
            }
            int childCount = housePositions.childCount;
            int showCount = type.Contains("HALF") ? Mathf.RoundToInt(childCount * 0.5f) : childCount;
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = housePositions.GetChild(ci).GetComponent<SpriteRenderer>();
                renderer.enabled = canShow;
            }
            return showCount.ToString();
        }

        private string SetupPavilion(string type)
        {
            bool none = !type.Contains("FULL");
            if (none)
            {
                for (int ci = 0; ci < pavilionPositions.childCount; ci++)
                    pavilionPositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                return "0";
            }
            int childCount = pavilionPositions.childCount;
            int showCount = type.Contains("HALF") ? Mathf.RoundToInt(childCount * 0.5f) : childCount;
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = pavilionPositions.GetChild(ci).GetComponent<SpriteRenderer>();
                renderer.enabled = canShow;
            }
            return showCount.ToString();
        }

        /// <summary>
        /// Sets up environment based on config modifying
        /// the config, removing the setup for this environment
        /// </summary>
        /// <param name="savedConfig"></param>
        public void Setup(ref string savedConfig)
        {
        }
    }
}