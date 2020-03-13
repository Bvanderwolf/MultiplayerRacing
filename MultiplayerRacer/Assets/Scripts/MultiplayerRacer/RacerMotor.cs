using System;
using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerMotor : MonoBehaviour
    {
        [SerializeField] private float acceleration;
        [SerializeField] private float steering;
        [SerializeField] private float maxVelocity;
        [SerializeField] private ParticleSystem[] sparks;

        private Rigidbody2D rb;
        private float steerFriction;
        private float weelFriction;
        private float maxVelocityBoosted;
        private const float INPUT_THRESHOLD = 0.01f;
        private const float DRIFT_BOOST_THRESHOLD = 60f;
        private const float DRIFT_BOOST_FACTOR = 10f;
        private const float DRIFT_DAMP = 4f;

        private bool driftStart = false;
        private bool driftEnd = false;

        [SerializeField] private float driftRotation;
        private float lastRotation;
        private bool lastDriftWasLeft = false;

        private bool boosting = false;
        private float currentBoostTime = 0;
        private const float BOOST_TIME = 1.5f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            steerFriction = rb.angularDrag;
            weelFriction = rb.drag;
            maxVelocityBoosted = maxVelocity * 3f;
        }

        public void AddSpeed(float inputV)
        {
            if (boosting)
            {
                float boostDelta = currentBoostTime / BOOST_TIME;
                currentBoostTime += Time.deltaTime;
                if (currentBoostTime >= BOOST_TIME)
                {
                    currentBoostTime = 0;
                    boosting = false;
                }

                Vector2 boostForce = transform.up * DRIFT_BOOST_FACTOR * (1f - boostDelta);
                rb.AddForce(boostForce);
            }

            if (NoGas(inputV))
                return;

            Vector2 speed = transform.up * (inputV * acceleration);
            rb.AddForce(speed);
        }

        public void Steer(float inputH, bool drift)
        {
            //only rotatate if player has input
            if (NoSteer(inputH))
                return;

            //based on going forward or backward do rotation
            float direction = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.up));
            if (direction >= 0.0f)
            {
                rb.rotation += inputH * steering * (rb.velocity.magnitude / maxVelocity);
            }
            else
            {
                rb.rotation -= inputH * steering * (rb.velocity.magnitude / maxVelocity);
            }
            //if no drifting input is given the car will be forced to not drift
            if (!drift)
            {
                if (!driftEnd)
                {
                    //if the drift end flag is false, handle the drift ending
                    OnDriftEnd();
                }
                float force = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.left));
                Vector2 relativeForce = Vector2.right * force;
                rb.AddForce(rb.GetRelativeVector(relativeForce * DRIFT_DAMP));
            }
            else
            {
                if (!driftStart)
                {
                    //if the drift was not started, handle the drift starting
                    OnDriftStart();
                }
                OnDrifting();
            }
            lastRotation = rb.rotation;
        }

        private void OnDrifting()
        {
            //get the difference between last rotation and current
            float difference = rb.rotation - lastRotation;
            //define whether we are drifting left
            bool leftDrift = difference > 0;
            //define if we are still drifting in the same direction
            bool sameDriftAsLast = lastDriftWasLeft == leftDrift;

            if (leftDrift)
            {
                /*If we start drifting left and it is not the same
                 drift direction as last, we reset the drift rotation*/
                if (!sameDriftAsLast)
                    driftRotation = 0;

                driftRotation += difference;
            }
            else
            {
                /*If we start drifting right and it is not the same
                 drift direction as last, we reset the drift rotation*/
                if (!sameDriftAsLast)
                    driftRotation = 0;

                driftRotation -= difference;
            }
            lastDriftWasLeft = leftDrift;
        }

        public void ResetDrift()
        {
            driftRotation = 0;
        }

        private void OnDriftEnd()
        {
            foreach (ParticleSystem spark in sparks)
            {
                if (spark.isPlaying)
                {
                    spark.Stop();
                }
                CheckForBoost();
                driftRotation = 0;
            }
            driftEnd = true;
            driftStart = false;
        }

        private void CheckForBoost()
        {
            //define boost
            bool boost = driftRotation >= DRIFT_BOOST_THRESHOLD;
            if (boost)
            {
                //set boosting flag
                boosting = true;
                //if already boosting, reset the boost time
                currentBoostTime = 0;
            }
        }

        private void OnDriftStart()
        {
            foreach (ParticleSystem spark in sparks)
            {
                if (!spark.isPlaying)
                {
                    spark.Play();
                }
            }
            driftRotation = 0;
            driftStart = true;
            driftEnd = false;
        }

        /// <summary>
        /// checks for gas input, if none, increases drag
        /// </summary>
        /// <param name="inputV"></param>
        /// <returns></returns>
        private bool NoGas(float inputV)
        {
            if (inputV > -INPUT_THRESHOLD && inputV < INPUT_THRESHOLD)
            {
                rb.drag = weelFriction * 2f;
                return true;
            }
            rb.drag = weelFriction;
            return false;
        }

        /// <summary>
        /// checks for steering input, if none increase angular drag
        /// </summary>
        /// <param name="inputH"></param>
        /// <returns></returns>
        private bool NoSteer(float inputH)
        {
            if (inputH > -INPUT_THRESHOLD && inputH < INPUT_THRESHOLD)
            {
                rb.angularDrag = steerFriction * 3f;
                return true;
            }
            rb.angularDrag = steerFriction;
            return false;
        }

        public void ClampVelocity()
        {
            //clamp velocity keeping boosted max velocity into account
            if (rb.velocity.magnitude > (boosting ? maxVelocityBoosted : maxVelocity))
            {
                rb.velocity = rb.velocity.normalized * maxVelocity;
            }
        }
    }
}