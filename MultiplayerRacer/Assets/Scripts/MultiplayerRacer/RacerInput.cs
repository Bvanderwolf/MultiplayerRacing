using System;
using System.Collections;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerInput : MonoBehaviour
    {
        private const float MAX_WAIT_FOR_PLAYER_INPUT = 20f;

        private Racer racer;

        private void Awake()
        {
            racer = GetComponent<Racer>();
        }

        //Game frame update
        private void Update()
        {
            //we can only check input if we can race
            if (!racer.CanRace)
                return;

            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.LogError("W input worked");
            }
        }

        /// <summary>
        /// Starts waiting for given input to do onInput action
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        public void WaitForPlayerInput(KeyCode key, Action<bool> result, Func<bool> check = null)
        {
            StartCoroutine(WaitForInput(key, result, check));
        }

        /// <summary>
        /// Waits for given key to be pressed and invokes action on press
        /// Uses MAX_WAIT_FOR_PLAYER_INPUT to check if player waits to long
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private IEnumerator WaitForInput(KeyCode key, Action<bool> result, Func<bool> check = null)
        {
            bool waitExceed = false;
            bool hasCheck = check != null;
            float wait = 0;
            while (!waitExceed)
            {
                wait += Time.deltaTime;
                waitExceed = wait >= MAX_WAIT_FOR_PLAYER_INPUT;
                if (Input.GetKeyDown(key))
                {
                    result(true);
                    yield break;
                }
                if (hasCheck)
                {
                    if (!check.Invoke())
                    {
                        Debug.LogError("failed wait for input :: check was triggered");
                        result(false);
                        yield break;
                    }
                }
                if (waitExceed)
                {
                    Debug.LogError("failed wait for input :: wait time exceeded Max wait time");
                    result(false);
                    yield break;
                }
                //wait for game frame so we can use getkeydown.
                yield return null;
            }
        }
    }
}