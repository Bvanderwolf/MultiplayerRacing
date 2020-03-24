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

        public Dictionary<int, Color[]> CarTextureColors { get; private set; }
        private GameObject carInFocus;
        private GameObject carOutOfFocus;

        private Vector2 carImageScale;
        private Bounds backBounds;

        private string[] carNames = {
            "Grey Car", "Black Car",
            "Brown Car", "Green Car",
            "White Car", "Blue Car",
            "Yellow Car", "Ambulance"};

        public Button SelectButton => selectButton;
        public int TextureNumInFocus { get; private set; }

        private void Awake()
        {
            navigationLeft.onClick.AddListener(OnNavigateLeft);
            navigationRight.onClick.AddListener(OnNavigateRight);

            Vector2 backSize = (back.sizeDelta * 2f) - new Vector2(BACK_SIDE_WIDTH, 0);
            backBounds = new Bounds(back.position, backSize);

            InitImages();
        }

        /// <summary>
        /// should be called when when leaving a room/lobby to reset
        /// car select to default settings
        /// </summary>
        public void OnLobbyLeave()
        {
            SetDefaultFocusSettings();
            SetDefaultSceneSettings(false);
            UpdateImageInVisability(carOutOfFocus, carImageTwo.rect);
        }

        private void InitImages()
        {
            CarTextureColors = new Dictionary<int, Color[]>();

            SetDefaultFocusSettings();
            SetDefaultSceneSettings(true);
            //set car image scale to be used when pixel positions need to be calculated
            Texture2D tex = (Texture2D)carImageOne.GetComponent<Image>().mainTexture;
            carImageScale = new Vector2(carImageOne.rect.width / tex.width, carImageOne.rect.height / tex.height);

            //update invisability of car out of focus
            UpdateImageInVisability(carOutOfFocus, carImageTwo.rect);
        }

        /// <summary>
        /// Sets default settings related to focus
        /// </summary>
        private void SetDefaultFocusSettings()
        {
            //set car in focus and out of focus based on starting scene
            carInFocus = carImageOne.gameObject;
            carOutOfFocus = carImageTwo.gameObject;

            Vector3 offset = new Vector3((back.sizeDelta.x * 0.5f) + carImageOne.rect.width, 0);
            carInFocus.transform.position = back.position;
            carOutOfFocus.transform.position = back.position + offset;

            TextureNumInFocus = 1;
        }

        /// <summary>
        /// sets default settings related to scene setup. Set init bool
        /// to true when calling this the first time
        /// </summary>
        /// <param name="init"></param>
        private void SetDefaultSceneSettings(bool init)
        {
            List<Sprite> carSprites = InRoomManager.Instance.GetSelectableCarSprites();

            //create image in focus alphas based on image one texture size
            Image imageOne = carInFocus.GetComponent<Image>();
            imageOne.sprite = carSprites[0];
            Texture2D texOne = (Texture2D)imageOne.mainTexture;
            if (init) AddTextureToDictionary(texOne);

            //create image in focus alphas based on image two texture size
            Image imageTwo = carOutOfFocus.GetComponent<Image>();
            imageTwo.sprite = carSprites[1];
            Texture2D texTwo = (Texture2D)imageTwo.mainTexture;
            if (init) AddTextureToDictionary(texTwo);

            //set text to a car name based on in focus texture
            carName.text = carNames[TextureNumInFocus - 1];
        }

        /// <summary>
        /// Adds given texture to the dictionary, using its last
        /// character as digit as key
        /// </summary>
        /// <param name="tex"></param>
        private void AddTextureToDictionary(Texture2D tex)
        {
            int num = int.Parse(tex.name.Substring(tex.name.Length - 1));
            CarTextureColors.Add(num, tex.GetPixels());
        }

        /// <summary>
        /// Subscribe action to selectbutton
        /// </summary>
        /// <param name="action"></param>
        public void ListenToSelectButton(UnityAction action)
        {
            selectButton.onClick.AddListener(action);
        }

        public void SetSelectButtonInteractableState(bool value)
        {
            selectButton.interactable = value;
        }

        private void SetNavigationInteractableState(bool value)
        {
            navigationLeft.interactable = value;
            navigationRight.interactable = value;
        }

        private void OnNavigateLeft()
        {
            Rect rect = carImageOne.rect;
            //update focus with increase = true
            UpdateFocus(false);
            //move car, that is in focus, out of focus
            StartCoroutine(MoveCarImageOutOfFocus(carInFocus, rect, true));
            //move car, that is out of focus, into focus
            StartCoroutine(MoveCarImageIntoFocus(carOutOfFocus, rect, true));
        }

        private void OnNavigateRight()
        {
            Rect rect = carImageOne.rect;
            UpdateFocus(true);
            //move car, that is in focus, out of focus
            StartCoroutine(MoveCarImageOutOfFocus(carInFocus, rect, false));
            //move car, that is out of focus, into focus
            StartCoroutine(MoveCarImageIntoFocus(carOutOfFocus, rect, false));
        }

        /// <summary>
        /// Moves car out of focus position towards an end position based on given
        /// left value. Will make given car's texture transparant when leaving.
        /// </summary>
        /// <param name="car"></param>
        /// <param name="rect"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        private IEnumerator MoveCarImageOutOfFocus(GameObject car, Rect rect, bool left)
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
                UpdateImageInVisability(car, rect);
                yield return new WaitForFixedUpdate();
            }
            //car is now out of focus
            carOutOfFocus = car;
        }

        /// <summary>
        /// Moves car into focus from outside of the background bounds towards
        /// the middle focus position. Will make car's texture apparent when
        /// moving inside background bounds
        /// </summary>
        /// <param name="car"></param>
        /// <param name="rect"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        private IEnumerator MoveCarImageIntoFocus(GameObject car, Rect rect, bool left)
        {
            //the user can't select a car if it is switching it
            selectButton.interactable = false;
            SetNavigationInteractableState(false);

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
                UpdateImageVisability(car, CarTextureColors[num], rect);
                yield return new WaitForFixedUpdate();
            }
            carInFocus = car;
            selectButton.interactable = IsFocusedOnSelectableCar();
            SetNavigationInteractableState(true);
        }

        /// <summary>
        /// goes through all pixels of the car image and checks based on car size
        /// and bounds of background if pixels need to be transparant or not
        /// </summary>
        /// <param name="carRect"></param>
        /// <param name="backBound"></param>
        private void UpdateImageInVisability(GameObject car, Rect carRect)
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
                    if (!backBounds.Contains(pixelPos))
                    {
                        //get out of bounds pixel color and set it to transparent
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
        }

        /// <summary>
        /// Goes through all pixels of the car, updating pixels that are inside
        /// background bounds to be apparent.
        /// </summary>
        /// <param name="car"></param>
        /// <param name="pixelColors"></param>
        /// <param name="carRect"></param>
        private void UpdateImageVisability(GameObject car, Color[] pixelColors, Rect carRect)
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
                    if (backBounds.Contains(pixelPos))
                    {
                        int i = x + tex.width * y;
                        Color memTexCol = pixelColors[i];
                        tex.SetPixel(x, y, memTexCol);
                    }
                }
            }
            tex.Apply();
        }

        /// <summary>
        /// Updates focus values bassed on whether we are increasing
        /// texture num in focus or decreasing it
        /// </summary>
        /// <param name="increase"></param>
        private void UpdateFocus(bool increase)
        {
            Vector3 offset = new Vector3((back.sizeDelta.x * 0.5f) + carImageOne.rect.width, 0);
            List<Sprite> carSprites = InRoomManager.Instance.GetSelectableCarSprites();
            if (increase)
            {
                //update texture number in focus (increasing it)
                bool outOfbounds = TextureNumInFocus == carSprites.Count;
                TextureNumInFocus = outOfbounds ? 1 : TextureNumInFocus + 1;
                //set position of car out of focus to the right of back
                carOutOfFocus.transform.position = back.position - offset;
            }
            else
            {
                //update texture number in focus (decreasing it)
                bool outOfbounds = TextureNumInFocus == 1;
                TextureNumInFocus = outOfbounds ? carSprites.Count : TextureNumInFocus - 1;
                //set position of car out of focus to the right of back
                carOutOfFocus.transform.position = back.position + offset;
            }
            //set new car sprite based on texture num in focus
            Image image = carOutOfFocus.GetComponent<Image>();
            image.sprite = carSprites[TextureNumInFocus - 1];
            carName.text = carNames[TextureNumInFocus - 1];
            if (!CarTextureColors.ContainsKey(TextureNumInFocus))
            {
                //if the texture colors dictionary doesn't contain the colors, add it
                CarTextureColors.Add(TextureNumInFocus, ((Texture2D)image.mainTexture).GetPixels());
            }
            UpdateImageInVisability(carOutOfFocus, carOutOfFocus.GetComponent<RectTransform>().rect);
        }

        /// <summary>
        /// Returns whether the car is focus is selectable based on SelectedCars list
        /// in LobbyUI
        /// </summary>
        /// <returns></returns>
        private bool IsFocusedOnSelectableCar()
        {
            return !transform.parent.GetComponent<LobbyUI>().SelectedCars.Contains(TextureNumInFocus - 1);
        }
    }
}