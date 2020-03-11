using System;
using System.Collections;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerInput : MonoBehaviour
    {
        public static float Gas => Input.GetAxis("Vertical");
        public static float Steer => -Input.GetAxis("Horizontal");

        public float lastInputH { get; private set; }
        public float lastInputV { get; private set; }

        private const float MAX_WAIT_FOR_PLAYER_INPUT = 20f;

        private Racer racer;
        private RacerMotor motor;

        private void Awake()
        {
            racer = GetComponent<Racer>();
            motor = GetComponent<RacerMotor>();
        }

        //Render frames
        private void FixedUpdate()
        {
            //we can only check input if we can race
            if (!racer.CanRace)
                return;

            //left (1.0) to right (-1.0)
            float h = Steer;
            //up (1.0) to down (-1.0)
            float v = Gas;
            bool drift = Input.GetKey(KeyCode.LeftShift);

            //update motor values
            motor.AddSpeed(v);
            motor.Steer(h, drift);
            motor.ClampVelocity();
        }

        public void UpdateRemote(float v, float h)
        {
            motor.AddSpeed(v);
            motor.Steer(h, false);
            motor.ClampVelocity();
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