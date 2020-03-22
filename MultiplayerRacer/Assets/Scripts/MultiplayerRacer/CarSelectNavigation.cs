using System.Collections;
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

        private float[,] carImageInFocusAlphas;
        private float[,] carImageOutOfFocusAlphas;

        private GameObject carInFocus;
        private GameObject carOutOfFocus;

        private void Awake()
        {
            navigationLeft.onClick.AddListener(OnNavigateLeft);
            navigationRight.onClick.AddListener(OnNavigateRight);
            InitImages();
        }

        private void InitImages()
        {
            //create image in focus alphas based on image one texture size
            Texture texOne = carImageOne.GetComponent<Image>().mainTexture;
            carImageInFocusAlphas = new float[texOne.width, texOne.height];
            //create image in focus alphas based on image two texture size
            Texture texTwo = carImageTwo.GetComponent<Image>().mainTexture;
            carImageOutOfFocusAlphas = new float[texTwo.width, texTwo.height];
            //set car in focus and out of focus based on starting scene
            carInFocus = carImageOne.gameObject;
            carOutOfFocus = carImageTwo.gameObject;
            //update invisability of car out of focus
            UpdateImageInVisability(carOutOfFocus, carImageTwo.rect, new Bounds(back.position, back.sizeDelta * 2f));
        }

        public void ListenToSelectButton(UnityAction action)
        {
            selectButton.onClick.AddListener(action);
        }

        private void OnNavigateLeft()
        {
            Rect rect = carImageOne.rect;
            Vector3 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            //set position of car out of focus to the right of back
            carOutOfFocus.transform.position = back.position + offset;
            //move car, that is in focus, out of focus
            StartCoroutine(MoveCarImageOutOfFocus(carInFocus));
            //move car, that is out of focus, into focus
            StartCoroutine(MoveCarImageIntoFocus(carOutOfFocus));
        }

        private void OnNavigateRight()
        {
        }

        private IEnumerator MoveCarImageOutOfFocus(GameObject car)
        {
            Rect rect = carImageOne.rect;
            Bounds backBound = new Bounds(back.position, back.sizeDelta * 2f);
            Vector2 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 endPosition = (Vector2)back.position - offset;
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

        private IEnumerator MoveCarImageIntoFocus(GameObject car)
        {
            Rect rect = carImageOne.rect;
            Bounds backBound = new Bounds(back.position, back.sizeDelta * 2f);
            Vector2 offset = new Vector2((back.sizeDelta.x * 0.5f) + rect.width, 0);
            Vector2 endPosition = back.position;
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
                car.transform.position = Vector2.Lerp((Vector2)back.position + offset, endPosition, perc);
                //check if out of focus already
                UpdateImageVisability(car, rect, backBound);
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
            //define whether car is in focus
            bool inFocus = car == carInFocus;
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
                        if (col != Color.clear && col.a != 0f)
                        {
                            //save alpha colors of all pixels to array based on focus or not
                            if (inFocus)
                                carImageInFocusAlphas[x, y] = col.a;
                            else
                                carImageOutOfFocusAlphas[x, y] = col.a;
                        }
                    }
                }
            }
            tex.Apply();
        }

        private void UpdateImageVisability(GameObject car, Rect carRect, Bounds backBound)
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
                        //get inside of bounds pixel color
                        Color col = tex.GetPixel(x, y);
                        float a = inFocus ? carImageInFocusAlphas[x, y] : carImageOutOfFocusAlphas[x, y];
                        //set the color of pixel to the inside stored carImageAlphas array
                        Color newCol = new Color(col.r, col.g, col.b, a);
                        tex.SetPixel(x, y, newCol);
                    }
                }
            }
            tex.Apply();
        }
    }
}