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
        public string Setup(RoadType type)
        {
            string _config = "";
            string typeName = type.ToString();

            _config += SetupFolliage(typeName);
            _config += SetupHouses(typeName);
            _config += SetupPavilion(typeName);

            return _config;
        }

        /// <summary>
        /// Sets up folliage based on given type
        /// returning the config for folliage
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// sets up houses based on given type
        /// returning the config for houses
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// sets up pavilion based on type
        /// returning pavilion config
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
            SetupFolliage(ref savedConfig);
            SetupHouses(ref savedConfig);
            SetupPavilion(ref savedConfig);
        }

        /// <summary>
        /// ses up folliage based on given config
        /// updating it for further usage
        /// </summary>
        /// <param name="config"></param>
        private void SetupFolliage(ref string config)
        {
            bool none = config[0] == '0';
            if (none)
            {
                for (int ci = 0; ci < TreePositions.childCount; ci++)
                    TreePositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                config = config.Substring(1);
                return;
            }

            int childCount = TreePositions.childCount;
            int showCount = int.Parse(config.Substring(0, 1));
            config = config.Substring(1);
            Sprite[] folliage = RoadManager.EnvironmentObjects["Folliage"];
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = TreePositions.GetChild(ci).GetComponent<SpriteRenderer>();
                if (canShow)
                {
                    int index = int.Parse(config.Substring(0, 1));
                    config = config.Substring(1);
                    renderer.sprite = folliage[index];
                }
                else
                {
                    renderer.sprite = null;
                }
            }
        }

        /// <summary>
        /// ses up houses based on given config
        /// updating it for further usage
        /// </summary>
        /// <param name="config"></param>
        private void SetupHouses(ref string config)
        {
            bool none = config[0] == '0';
            if (none)
            {
                for (int ci = 0; ci < housePositions.childCount; ci++)
                    housePositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                config = config.Substring(1);
                return;
            }

            int childCount = housePositions.childCount;
            int showCount = int.Parse(config.Substring(0, 1));
            config = config.Substring(1);
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = housePositions.GetChild(ci).GetComponent<SpriteRenderer>();
                renderer.enabled = canShow;
            }
        }

        /// <summary>
        /// ses up pavilion based on given config
        /// updating it for further usage
        /// </summary>
        /// <param name="config"></param>
        private void SetupPavilion(ref string config)
        {
            bool none = config[0] == '0';
            if (none)
            {
                for (int ci = 0; ci < pavilionPositions.childCount; ci++)
                    pavilionPositions.GetChild(ci).GetComponent<SpriteRenderer>().sprite = null;

                config = config.Substring(1);
                return;
            }

            int childCount = pavilionPositions.childCount;
            int showCount = int.Parse(config.Substring(0, 1));
            config = config.Substring(1);
            for (int ci = 0; ci < childCount; ci++)
            {
                bool canShow = ci < showCount;
                SpriteRenderer renderer = pavilionPositions.GetChild(ci).GetComponent<SpriteRenderer>();
                renderer.enabled = canShow;
            }
        }
    }
}