using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRacer
{
    [System.Serializable]
    public class RoadProps
    {
        [SerializeField] private Transform propParent;
        [SerializeField] private GameObject[] positions;

        private const int CONFIG_CHAR_COUNT = 3;

        public string SetupProps(bool startOrEnd, Bounds bounds)
        {
            List<Sprite[]> propSprites = RoadManager.RoadProps;

            //set all positions to inactive before setting positions of props
            for (int i = 0; i < positions.Length; i++) positions[i].SetActive(false);

            //decide on a random ammount of props to show or none if start or end
            int count = startOrEnd ? 0 : Random.Range(0, propParent.childCount + 1);
            string config = "";

            for (int ci = 0; ci < propParent.childCount; ci++)
            {
                Transform child = propParent.GetChild(ci);
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
                bool canShow = ci < count;
                if (canShow)
                {
                    //if this prop can be shown, set its sprite and update config with it
                    config += SetupPropSprite(renderer, propSprites);
                    //enable collider and set its size
                    collider.enabled = true;
                    Vector3 size = renderer.sprite.bounds.size;
                    collider.size = new Vector2(size.x * child.lossyScale.x, size.y * child.lossyScale.y);
                    //setup position of prop and update the config with it
                    config += SetupPropPosition(child);
                }
                else
                {
                    //if this prop is not shown set its sprite to null and collider disabled
                    renderer.sprite = null;
                    collider.enabled = false;
                    child.localPosition = Vector3.zero;
                }
            }

            return $"{count}{config}";
        }

        private string SetupPropPosition(Transform prop)
        {
            //get a random position which is inactive to place the prop on
            int positionIndex = Random.Range(0, positions.Length);
            while (positions[positionIndex].activeInHierarchy)
            {
                //get new random position index while choosen position is already active
                positionIndex = Random.Range(0, positions.Length);
            }
            //set new position to active so other props can't be placed on it
            positions[positionIndex].SetActive(true);
            prop.localPosition = positions[positionIndex].transform.localPosition;
            //return position index
            return positionIndex.ToString();
        }

        private string SetupPropSprite(SpriteRenderer renderer, List<Sprite[]> sprites)
        {
            //get random index of sprite type
            int index_spriteType = Random.Range(0, sprites.Count);
            int index_sprite = 0;
            if (sprites[index_spriteType].Length > 1)
            {
                //if this is an array with more than one sprite, get a random one
                index_sprite = Random.Range(0, sprites[index_spriteType].Length);
            }
            renderer.sprite = sprites[index_spriteType][index_sprite];

            return $"{index_spriteType}{index_sprite}";
        }

        public void SetupProps(string savedConfiguration)
        {
            List<Sprite[]> propSprites = RoadManager.RoadProps;
            int count = int.Parse(savedConfiguration.Substring(0, 1));
            string subConfig = savedConfiguration.Substring(1);

            for (int ci = 0; ci < propParent.childCount; ci++)
            {
                Transform child = propParent.GetChild(ci);
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
                bool canShow = ci < count;
                if (canShow)
                {
                    string config = subConfig.Substring(ci * CONFIG_CHAR_COUNT, CONFIG_CHAR_COUNT);
                    //if this prop can be shown, set its sprite
                    SetupPropSprite(renderer, propSprites, config);
                    //enable collider
                    collider.enabled = true;
                    //set collider to fit around the sprite
                    Vector3 size = renderer.sprite.bounds.size;
                    collider.size = new Vector2(size.x * child.lossyScale.x, size.y * child.lossyScale.y);
                    //set position of prop based on last character in config
                    SetupPropPosition(child, int.Parse(config.Substring(CONFIG_CHAR_COUNT - 1)));
                }
                else
                {
                    //if this prop is not shown set its sprite to null and collider disabled
                    renderer.sprite = null;
                    collider.enabled = false;
                }
            }
        }

        private void SetupPropPosition(Transform prop, int positionIndex)
        {
            positions[positionIndex].SetActive(true);
            prop.localPosition = positions[positionIndex].transform.localPosition;
        }

        private void SetupPropSprite(SpriteRenderer renderer, List<Sprite[]> sprites, string config)
        {
            int index_spriteType = int.Parse(config.Substring(0, 1));
            int index_sprite = int.Parse(config.Substring(1, 1));
            renderer.sprite = sprites[index_spriteType][index_sprite];
        }
    }
}