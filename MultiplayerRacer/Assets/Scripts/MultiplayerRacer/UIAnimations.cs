using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class UIAnimations : MonoBehaviour
    {
        [SerializeField] private float scaleSpeed = 1f;
        [SerializeField] private float fadeSpeed = 1f;

        private const int MAX_COUNTDOWN_COUNT = 5;

        public bool CountingDown { get; private set; }

        /// <summary>
        /// Takes given Gameobject and creates a countdown effect with given count, fade and on end action
        /// </summary>
        /// <param name="textGo"></param>
        /// <param name="count"></param>
        /// <param name="withFade"></param>
        /// <param name="onEnd"></param>
        public void CountDown(GameObject textGo, int count, bool withFade = false, Action onEnd = null, Func<bool> check = null)
        {
            //start countdown coroutine if gameobject is not null and has text component
            if (textGo != null && textGo.GetComponent<Text>() != null)
            {
                StartCoroutine(DoCountDown(textGo, count, withFade, onEnd, check));
            }
            else Debug.LogError("text game object is null or has no text component");
        }

        /// <summary>
        /// Makes given button uninteractable for given ammount of time
        /// </summary>
        public void TimeOutButton(Button button, float time)
        {
            if (button != null && time > 0)
            {
                StartCoroutine(DoButtonTimeOut(button, time));
            }
            else Debug.LogError($"button to time out was null or time: {time} isnt greater than 0");
        }

        /// <summary>
        /// Does a popup op of given text. If given text is null or empty, uses text
        /// component its text;
        /// </summary>
        public void PopupText(GameObject textGo, string text, float fadeDelay = 0, bool withFade = true)
        {
            if (textGo != null && textGo.GetComponent<Text>() != null)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Text textComp = textGo.GetComponent<Text>();
                    textComp.text = text;
                }
                StartCoroutine(PopupTextEnumerator(textGo, null, withFade));
            }
            else Debug.LogError("text game object or Text component is null");
        }

        /// <summary>
        /// Does countdown with given values, can take and onEnd action to invoke when done
        /// Can also take an check Func to stop coroutine if this asserts to false after a count
        /// </summary>
        /// <returns></returns>
        private IEnumerator DoCountDown(GameObject go, int count, bool withFade = false, Action onEnd = null, Func<bool> check = null)
        {
            CountingDown = true;
            Text goText = go.GetComponent<Text>();
            //save original color for after fade
            Color goTextColor = goText.color;
            //set count to max countdown count if higher
            if (count > MAX_COUNTDOWN_COUNT)
            {
                count = MAX_COUNTDOWN_COUNT;
            }
            bool hasCeck = check != null;
            //for each count, do a text pop with given gameobject
            for (int current = count; current > 0; current--)
            {
                goText.text = current.ToString();
                yield return StartCoroutine(PopupTextEnumerator(go, null, withFade));
                go.transform.localScale = Vector3.zero;
                go.GetComponent<Text>().color = goTextColor;
                if (hasCeck && !check.Invoke())
                {
                    Debug.LogError("failed countdown :: check was triggered");
                    CountingDown = false;
                    yield break;
                }
            }
            CountingDown = false;
            onEnd?.Invoke();
        }

        private IEnumerator PopupTextEnumerator(GameObject go, Action callback, bool withFade = false, float fadeDelay = 0)
        {
            //scale text
            yield return StartCoroutine(ScaleText(go.transform));

            //fade text if needed
            if (withFade)
            {
                yield return new WaitForSeconds(fadeDelay);
                Text textComp = go.GetComponent<Text>();
                yield return StartCoroutine(FadeText(textComp, textComp.color));
            }

            callback?.Invoke();
        }

        private IEnumerator ScaleText(Transform transform)
        {
            float currentLerpTime = 0;
            //linearly interpolate scale of text between 0,0,0 and 1,1,1
            while (transform.localScale != Vector3.one)
            {
                currentLerpTime += Time.deltaTime * scaleSpeed;
                if (currentLerpTime > 1)
                {
                    currentLerpTime = 1;
                }

                float perc = currentLerpTime / 1;
                transform.localScale = Vector3.zero + (perc * (Vector3.one - Vector3.zero));
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator FadeText(Text text, Color startColor)
        {
            float currentLerpTime = 0;
            //linearly interpolate color between startcolor and 0,0,0,0
            while (text.color.a != 0)
            {
                currentLerpTime += Time.deltaTime * fadeSpeed;
                if (currentLerpTime > 1)
                {
                    currentLerpTime = 1;
                }

                float perc = currentLerpTime / 1;
                text.color = Color.Lerp(startColor, Color.clear, perc);
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator DoButtonTimeOut(Button button, float time)
        {
            //set button to uninteractable for given time
            button.interactable = false;
            yield return new WaitForSeconds(time);
            button.interactable = true;
        }
    }
}