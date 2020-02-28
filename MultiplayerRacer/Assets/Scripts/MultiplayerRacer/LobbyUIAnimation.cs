using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerRacer
{
    public class LobbyUIAnimation : MonoBehaviour
    {
        [SerializeField] private float scaleSpeed = 1f;
        [SerializeField] private float fadeSpeed = 1f;

        public void CountDown(GameObject textGo, int count, bool withFade = false, Action onEnd = null)
        {
            if (textGo != null)
            {
                StartCoroutine(DoCountDown(textGo, count, withFade, onEnd));
            }
            else Debug.LogError("text game object is null");
        }

        private IEnumerator DoCountDown(GameObject go, int count, bool withFade = false, Action onEnd = null)
        {
            Text goText = go.GetComponent<Text>();
            Color goTextColor = goText.color;

            for (int current = count; current > 0; current--)
            {
                goText.text = current.ToString();
                yield return StartCoroutine(PopupTextEnumerator(go, null, withFade));
                go.transform.localScale = Vector3.zero;
                go.GetComponent<Text>().color = goTextColor;
            }
            onEnd?.Invoke();
        }

        private IEnumerator PopupTextEnumerator(GameObject go, Action callback, bool withFade = false)
        {
            yield return StartCoroutine(ScaleText(go.transform));

            if (withFade)
            {
                Text textComp = go.GetComponent<Text>();
                yield return StartCoroutine(FadeText(textComp, textComp.color));
            }

            callback?.Invoke();
        }

        private IEnumerator ScaleText(Transform transform)
        {
            float currentLerpTime = 0;

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
    }
}