using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerMotor : MonoBehaviour
    {
        [SerializeField] private float acceleration;
        [SerializeField] private float steering;
        [SerializeField] private float maxVelocity;
        [SerializeField] private ParticleSystem spark;
        [SerializeField] private TrailRenderer[] trails;

        private Rigidbody2D rb;
        private float steerFriction;
        private float weelFriction;
        private float maxVelocityBoosted;
        private const float INPUT_THRESHOLD = 0.01f;
        private const float MIN_DRIFT_TIME = 1f;
        private const float DRIFT_BOOST_FACTOR = 2.5f;
        private const float DRIFT_DAMP = 4f;

        private bool driftStart = false;
        private bool driftEnd = false;

        private float driftTime;
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

        private void FixedUpdate()
        {
            if (boosting)
            {
                //define boost percentage
                float boostPerc = currentBoostTime / BOOST_TIME;
                currentBoostTime += Time.deltaTime;
                if (currentBoostTime >= BOOST_TIME)
                {
                    //if current boost time exceeds set boost time, stop
                    currentBoostTime = 0;
                    boosting = false;
                    spark.Stop();
                }
                //add boost force based on percentage in boosts to make boost fade
                Vector2 boostForce = transform.up * (DRIFT_BOOST_FACTOR * (1f - boostPerc));
                rb.AddForce(boostForce);
            }
        }

        public void AddSpeed(float inputV)
        {
            //if no input is given, return
            if (NoGas(inputV))
                return;

            //add speed based on input and acceleration to rigidbody
            Vector2 speed = transform.up * (inputV * acceleration);
            rb.AddForce(speed);
        }

        public void Steer(float inputH)
        {
            //only rotatate if player has input
            if (NoSteer(inputH))
                return;

            //based on going forward or backward do rotation
            float direction = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.up));
            if (direction >= 0.0f)
            {
                rb.rotation += inputH * steering * (rb.velocity.magnitude / (maxVelocity * 0.5f));
            }
            else
            {
                rb.rotation -= inputH * steering * (rb.velocity.magnitude / (maxVelocity * 0.5f));
            }
        }

        public void Drift(bool drift)
        {
            float force = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.left));
            Vector2 relativeForce = Vector2.right * force;

            //if no drifting input is given the car will be forced to not drift
            if (!drift)
            {
                //add damped relative force
                rb.AddForce(rb.GetRelativeVector(relativeForce * DRIFT_DAMP));
                if (!driftEnd)
                {
                    //if the drift end flag is false, handle the drift ending
                    OnDriftEnd();
                }
            }
            else
            {
                //add normal relative force
                rb.AddForce(rb.GetRelativeVector(relativeForce));
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
                    driftTime = 0;

                driftTime += Time.deltaTime;
            }
            else
            {
                /*If we start drifting right and it is not the same
                 drift direction as last, we reset the drift rotation*/
                if (!sameDriftAsLast)
                    driftTime = 0;

                driftTime += Time.deltaTime;
            }
            lastDriftWasLeft = leftDrift;
        }

        public void ResetDrift()
        {
            driftTime = 0;
        }

        private void OnDriftEnd()
        {
            foreach (TrailRenderer trail in trails)
            {
                trail.emitting = false;
            }
            CheckForBoost();
            driftTime = 0;
            driftEnd = true;
            driftStart = false;
        }

        private void CheckForBoost()
        {
            //define boost
            bool boost = driftTime >= MIN_DRIFT_TIME;
            if (boost)
            {
                SetBoost();
            }
        }

        public void SetBoost()
        {
            //set boosting flag
            boosting = true;
            //set spark particle system to play
            spark.Play();
            //if already boosting, reset the boost time
            currentBoostTime = 0;
        }

        private void OnDriftStart()
        {
            foreach (TrailRenderer trail in trails)
            {
                trail.emitting = true;
            }
            driftTime = 0;
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