using UnityEngine;

namespace MultiplayerRacer
{
    public class RacerMotor : MonoBehaviour
    {
        [SerializeField] private float acceleration;
        [SerializeField] private float steering;
        [SerializeField] private float maxVelocity;

        private Rigidbody2D rb;
        private const float HORIZONTAL_INPUT_THRESHOLD = 0.01f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void AddSpeed(float inputV)
        {
            Vector2 speed = transform.up * (inputV * acceleration);
            rb.AddForce(speed);
        }

        public void Steer(float inputH)
        {
            //only rotatate if player has input
            if (inputH > -HORIZONTAL_INPUT_THRESHOLD && inputH < HORIZONTAL_INPUT_THRESHOLD)
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

            float driftForce = Vector2.Dot(rb.velocity, rb.GetRelativeVector(Vector2.left));
            Vector2 relativeForce = Vector2.right * driftForce;

            rb.AddForce(rb.GetRelativeVector(relativeForce));
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