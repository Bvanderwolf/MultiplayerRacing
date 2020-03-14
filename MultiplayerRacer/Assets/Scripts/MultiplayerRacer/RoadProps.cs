using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerRacer
{
    [System.Serializable]
    public class RoadProps
    {
        [SerializeField] private Transform propParent;

        private const int CONFIG_CHAR_COUNT = 2;

        public string SetProps(bool startOrEnd)
        {
            List<Sprite[]> propSprites = RoadManager.RoadProps;

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
                    config += SetPropSprite(renderer, propSprites);
                    //enable collider
                    collider.enabled = true;
                    //set collider to fit around the sprite
                    Vector3 size = renderer.sprite.bounds.size;
                    collider.size = new Vector2(size.x * child.lossyScale.x, size.y * child.lossyScale.y);
                }
                else
                {
                    //if this prop is not shown set its sprite to null and collider disabled
                    renderer.sprite = null;
                    collider.enabled = false;
                }
            }

            return $"{count}{config}";
        }

        private string SetPropSprite(SpriteRenderer renderer, List<Sprite[]> sprites)
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

        public void SetProps(string savedConfiguration)
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
                    SetPropSprite(renderer, propSprites, config);
                    //enable collider
                    collider.enabled = true;
                    //set collider to fit around the sprite
                    Vector3 size = renderer.sprite.bounds.size;
                    collider.size = new Vector2(size.x * child.lossyScale.x, size.y * child.lossyScale.y);
                }
                else
                {
                    //if this prop is not shown set its sprite to null and collider disabled
                    renderer.sprite = null;
                    collider.enabled = false;
                }
            }
        }

        private void SetPropSprite(SpriteRenderer renderer, List<Sprite[]> sprites, string config)
        {
            int index_spriteType = int.Parse(config.Substring(0, 1));
            int index_sprite = int.Parse(config.Substring(1, 1));
            renderer.sprite = sprites[index_spriteType][index_sprite];
        }
    }
}