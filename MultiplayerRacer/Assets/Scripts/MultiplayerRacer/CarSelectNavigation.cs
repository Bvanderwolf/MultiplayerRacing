using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class CarSelectNavigation : MonoBehaviour
    {
        [SerializeField] private RectTransform carImage;
        [SerializeField] private RectTransform back;
        [SerializeField] private Text carName;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button navigationLeft;
        [SerializeField] private Button navigationRight;

        private float[,] carImageOneAlphas;

        private const float SLIDE_SPEED = 0.55f;

        private void Awake()
        {
            navigationLeft.onClick.AddListener(OnNavigateLeft);
            navigationRight.onClick.AddListener(OnNavigateRight);
            InitImageAlphas();
            StartCoroutine(MoveCarImageOutOfFocus());
        }

        private void InitImageAlphas()
        {
            Texture tex = carImage.GetComponent<Image>().mainTexture;
            carImageOneAlphas = new float[tex.width, tex.height];
        }

        public void ListenToSelectButton(UnityAction action)
        {
            selectButton.onClick.AddListener(action);
        }

        private void OnNavigateLeft()
        {
        }

        private void OnNavigateRight()
        {
        }

        private IEnumerator MoveCarImageOutOfFocus()
        {
            Rect rect = carImage.rect;
            Bounds backBound = new Bounds(back.position, back.sizeDelta * 2f);
            Vector2 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 endPosition = (Vector2)back.position - offset;
            float currentTime = 0;
            //linearly interpolate car image toward end position
            while (currentTime != 1f)
            {
                currentTime += Time.deltaTime * SLIDE_SPEED;
                float perc = currentTime / 1f;
                //ease car in
                perc = 1 - Mathf.Cos(perc * Mathf.PI * 0.5f);
                //linearly interpolate
                carImage.position = Vector2.Lerp(back.position, endPosition, perc);
                //check if out of focus already
                UpdateImageVisability(rect, backBound);
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// goes through all pixels of the car image and checks based on car size
        /// and bounds of background if pixels need to be transparant or not
        /// </summary>
        /// <param name="carRect"></param>
        /// <param name="backBound"></param>
        private void UpdateImageVisability(Rect carRect, Bounds backBound)
        {
            //get position from where texture is being drawn (left top corner)
            Vector2 drawStartPos = (Vector2)carImage.position + new Vector2(-carRect.width * 0.5f, carRect.height * 0.5f);
            //get main texture of car to update
            Texture2D tex = (Texture2D)carImage.GetComponent<Image>().mainTexture;
            //loop through texture its pixels
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //if a pixel is out of background bounds it can be made transparent
                    Vector2 pixelPos = drawStartPos + (new Vector2(x, -y) * back.lossyScale);
                    if (!backBound.Contains(pixelPos))
                    {
                        //get out of bounds pixel color
                        Color col = tex.GetPixel(x, y);
                        if (col.a != 0f)
                        {
                            //if the color is not already transparent, set its apha to zero
                            Color newCol = new Color(col.r, col.g, col.b, 0f);
                            tex.SetPixel(x, y, newCol);
                        }
                        //save alpha colors of all pixels to be used when sliding in focus again
                        carImageOneAlphas[x, y] = col.a;
                    }
                }
            }
            tex.Apply();
        }
    }
}