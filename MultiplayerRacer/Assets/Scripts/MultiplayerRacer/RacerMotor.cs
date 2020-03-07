using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerMotor : MonoBehaviour
    {
        [SerializeField] private float acceleration;
        [SerializeField] private float steering;
        [SerializeField] private float maxVelocity;

        private Rigidbody2D rb;
        private float steerFriction;
        private float weelFriction;
        private const float INPUT_THRESHOLD = 0.01f;
        private const float DRIFT_DAMP = 4f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            steerFriction = rb.angularDrag;
            weelFriction = rb.drag;
        }

        public void AddSpeed(float inputV)
        {
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
                float force = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.left));
                Vector2 relativeForce = Vector2.right * force;
                rb.AddForce(rb.GetRelativeVector(relativeForce * DRIFT_DAMP));
            }
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
                rb.angularDrag = steerFriction * 2f;
                return true;
            }
            return false;
        }

        public void ClampVelocity()
        {
            if (rb.velocity.magnitude > maxVelocity)
            {
                rb.velocity = rb.velocity.normalized * maxVelocity;
            }
        }
    }
}