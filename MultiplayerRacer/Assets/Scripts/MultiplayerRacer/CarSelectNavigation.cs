using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class CarSelectNavigation : MonoBehaviour
    {
        [SerializeField] private RectTransform carImageOne;
        [SerializeField] private RectTransform carImageTwo;
        [SerializeField] private RectTransform back;
        [SerializeField] private Text carName;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button navigationLeft;
        [SerializeField] private Button navigationRight;

        private const float SLIDE_SPEED = 0.55f;
        private const float BACK_SIDE_WIDTH = 60f;

        private Dictionary<int, Color[]> carTextureColors;
        private int currentTextureNumber;

        private GameObject carInFocus;
        private GameObject carOutOfFocus;

        private Vector2 carImageScale;

        private List<Sprite> CarSprites;

        private void Awake()
        {
            navigationLeft.onClick.AddListener(OnNavigateLeft);
            navigationRight.onClick.AddListener(OnNavigateRight);
            InitImages();
        }

        private void InitImages()
        {
            string path = "Sprites/Cars/";
            CarSprites = new List<Sprite>()
            {
                Resources.Load<Sprite>(path + "car_people1"),
                Resources.Load<Sprite>(path + "car_people2"),
                Resources.Load<Sprite>(path + "car_people3"),
                Resources.Load<Sprite>(path + "car_people4"),
                Resources.Load<Sprite>(path + "car_people5"),
                Resources.Load<Sprite>(path + "car_people6"),
                Resources.Load<Sprite>(path + "car_people7"),
                Resources.Load<Sprite>(path + "car_people8")
            };
            carTextureColors = new Dictionary<int, Color[]>();

            //create image in focus alphas based on image one texture size
            Image imageOne = carImageOne.GetComponent<Image>();
            imageOne.sprite = CarSprites[0];
            Texture2D texOne = (Texture2D)imageOne.mainTexture;
            AddTextureToDictionary(texOne);

            //create image in focus alphas based on image two texture size
            Image imageTwo = carImageTwo.GetComponent<Image>();
            imageTwo.sprite = CarSprites[1];
            Texture2D texTwo = (Texture2D)imageTwo.mainTexture;
            AddTextureToDictionary(texTwo);

            //set car in focus and out of focus based on starting scene
            carInFocus = carImageOne.gameObject;
            carOutOfFocus = carImageTwo.gameObject;

            //set car image scale to be used when pixel positions need to be calculated
            carImageScale = new Vector2(carImageOne.rect.width / texOne.width, carImageOne.rect.height / texOne.height);
            currentTextureNumber = 1;

            //update invisability of car out of focus
            UpdateImageInVisability(carOutOfFocus, carImageTwo.rect, new Bounds(back.position, back.sizeDelta * 2f));
        }

        private void AddTextureToDictionary(Texture2D tex)
        {
            int num = int.Parse(tex.name.Substring(tex.name.Length - 1));
            carTextureColors.Add(num, tex.GetPixels());
        }

        public void ListenToSelectButton(UnityAction action)
        {
            selectButton.onClick.AddListener(action);
        }

        private void OnNavigateLeft()
        {
            Rect rect = carImageOne.rect;
            Vector3 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 backSize = (back.sizeDelta * 2f) - new Vector2(BACK_SIDE_WIDTH, 0);
            Bounds backBound = new Bounds(back.position, backSize);
            //set position of car out of focus to the right of back
            carOutOfFocus.transform.position = back.position + offset;
            //move car, that is in focus, out of focus
            StartCoroutine(MoveCarImageOutOfFocus(carInFocus, rect, backBound, true));
            //move car, that is out of focus, into focus
            StartCoroutine(MoveCarImageIntoFocus(carOutOfFocus, rect, backBound, true));
        }

        private void OnNavigateRight()
        {
            Rect rect = carImageOne.rect;
            Vector3 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 backSize = (back.sizeDelta * 2f) - new Vector2(BACK_SIDE_WIDTH, 0);
            Bounds backBound = new Bounds(back.position, backSize);
            //set position of car out of focus to the right of back
            carOutOfFocus.transform.position = back.position - offset;
            //move car, that is in focus, out of focus
            StartCoroutine(MoveCarImageOutOfFocus(carInFocus, rect, backBound, false));
            //move car, that is out of focus, into focus
            StartCoroutine(MoveCarImageIntoFocus(carOutOfFocus, rect, backBound, false));
        }

        private IEnumerator MoveCarImageOutOfFocus(GameObject car, Rect rect, Bounds backBound, bool left)
        {
            Vector2 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 endPosition = (Vector2)back.position - (left ? offset : -offset);
            float currentTime = 0;
            //linearly interpolate car image toward end position
            while (currentTime != 1f)
            {
                currentTime += Time.deltaTime * SLIDE_SPEED;
                if (currentTime > 1f)
                    currentTime = 1f;

                float perc = currentTime / 1f;
                //ease car in
                perc = 1 - Mathf.Cos(perc * Mathf.PI * 0.5f);
                //linearly interpolate
                car.transform.position = Vector2.Lerp(back.position, endPosition, perc);
                //check if out of focus already
                UpdateImageInVisability(car, rect, backBound);
                yield return new WaitForFixedUpdate();
            }
            carOutOfFocus = car;
        }

        private IEnumerator MoveCarImageIntoFocus(GameObject car, Rect rect, Bounds backBound, bool left)
        {
            Vector2 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 startPosition = (Vector2)back.position + (left ? offset : -offset);
            Texture2D tex = (Texture2D)car.GetComponent<Image>().mainTexture;
            int num = int.Parse(tex.name.Substring(tex.name.Length - 1));
            float currentTime = 0;
            //linearly interpolate car image toward end position
            while (currentTime != 1f)
            {
                currentTime += Time.deltaTime * SLIDE_SPEED;
                if (currentTime > 1f)
                    currentTime = 1f;

                float perc = currentTime / 1f;
                //ease car in
                perc = 1 - Mathf.Cos(perc * Mathf.PI * 0.5f);
                //linearly interpolate
                car.transform.position = Vector2.Lerp(startPosition, back.position, perc);
                //update image visability
                UpdateImageVisability(car, carTextureColors[num], rect, backBound);
                yield return new WaitForFixedUpdate();
            }
            carInFocus = car;
        }

        /// <summary>
        /// goes through all pixels of the car image and checks based on car size
        /// and bounds of background if pixels need to be transparant or not
        /// </summary>
        /// <param name="carRect"></param>
        /// <param name="backBound"></param>
        private void UpdateImageInVisability(GameObject car, Rect carRect, Bounds backBound)
        {
            //get position from where texture is being drawn (left top corner)
            Vector2 drawStartPos = (Vector2)car.transform.position + new Vector2(-carRect.width * 0.5f, carRect.height * 0.5f);
            //get main texture of car to update
            Texture2D tex = (Texture2D)car.GetComponent<Image>().mainTexture;
            //loop through texture its pixels
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //if a pixel is out of background bounds it can be made transparent
                    Vector2 pixelPos = drawStartPos + ((new Vector2(x, -y) * carImageScale));
                    if (!backBound.Contains(pixelPos))
                    {
                        //get out of bounds pixel color and set it to transparent
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
        }

        private void UpdateImageVisability(GameObject car, Color[] pixelColors, Rect carRect, Bounds backBound)
        {
            //get position from where texture is being drawn (left top corner)
            Vector2 drawStartPos = (Vector2)car.transform.position + new Vector2(-carRect.width * 0.5f, carRect.height * 0.5f);
            //get main texture of car to update
            Texture2D tex = (Texture2D)car.GetComponent<Image>().mainTexture;
            //define whether car is in focus
            bool inFocus = car == carInFocus;
            //loop through texture its pixels
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    //if a pixel is inside of background bounds it can be made visible
                    Vector2 pixelPos = drawStartPos + (new Vector2(x, -y) * back.lossyScale);
                    if (backBound.Contains(pixelPos))
                    {
                        int i = x + tex.width * y;
                        Color memTexCol = pixelColors[i];
                        tex.SetPixel(x, y, memTexCol);
                    }
                }
            }
            tex.Apply();
        }
    }
}