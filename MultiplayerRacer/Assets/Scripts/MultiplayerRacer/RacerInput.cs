using System;
using System.Collections;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerInput : MonoBehaviour
    {
        private const float MAX_WAIT_FOR_PLAYER_INPUT = 20f;

        /// <summary>
        /// Starts waiting for given input to do onInput action
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        public void WaitForPlayerInput(KeyCode key, Action<bool> result)
        {
            StartCoroutine(WaitForInput(key, result));
        }

        /// <summary>
        /// Waits for given key to be pressed and invokes action on press
        /// Uses MAX_WAIT_FOR_PLAYER_INPUT to check if player waits to long
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private IEnumerator WaitForInput(KeyCode key, Action<bool> result)
        {
            bool input = false;
            bool waitExceed = false;
            float wait = 0;
            while (!input && !waitExceed)
            {
                wait += Time.deltaTime;
                input = Input.GetKeyDown(key);
                waitExceed = wait >= MAX_WAIT_FOR_PLAYER_INPUT;
                if (input)
                {
                    result(true);
                    yield break;
                }
                else if (waitExceed)
                {
                    Debug.LogError("Player waited to long for input :: waiting for master client to reset");
                    result(false);
                    yield break;
                }
                //wait for game frame so we can use getkeydown.
                yield return null;
            }
        }
    }
}