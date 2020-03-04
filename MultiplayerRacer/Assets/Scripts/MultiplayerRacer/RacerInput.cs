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
        /// <param name="onInput"></param>
        public void WaitForPlayerInput(KeyCode key, Action<bool> onInput)
        {
            StartCoroutine(WaitForInput(key, onInput));
        }

        /// <summary>
        /// Waits for given key to be pressed and invokes action on press
        /// Uses MAX_WAIT_FOR_PLAYER_INPUT to check if player waits to long
        /// </summary>
        /// <param name="key"></param>
        /// <param name="onInput"></param>
        /// <returns></returns>
        private IEnumerator WaitForInput(KeyCode key, Action<bool> onInput)
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
                    onInput(true);
                    yield break;
                }
                else if (waitExceed)
                {
                    Debug.LogError("Player waited to long for input :: waiting for master client to reset");
                    onInput(false);
                    yield break;
                }
                //wait for game frame so we can use getkeydown.
                yield return null;
            }
        }
    }
}