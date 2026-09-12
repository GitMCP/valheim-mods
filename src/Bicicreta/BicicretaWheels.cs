using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Rolls the wheels at the speed the bicycle is actually travelling.
    ///
    /// This is only how the bicycle looks, and it is deliberately not networked.
    /// <see cref="Character.GetVelocity"/> reads the rigidbody on the peer that owns the
    /// bicycle and, on every other peer, the velocity that owner publishes to its ZDO, so
    /// each client can work out the rotation for itself from something the game already
    /// sends. Nothing here needs a ZDO of its own, an RPC, or a config value to agree on.
    /// </summary>
    internal class BicicretaWheels : MonoBehaviour
    {
        /// <summary>
        /// How far a wheel turns per metre rolled: one turn every circumference, which
        /// for this wheel is about 0.4 turns a metre.
        /// </summary>
        private const float DegreesPerMetre = Mathf.Rad2Deg / BicicretaGeometry.WheelRadius;

        /// <summary>
        /// How quickly the wheels take up a change in speed. A remote rider's velocity
        /// arrives in ZDO updates rather than continuously, and wheels that followed it
        /// exactly would step rather than spin.
        /// </summary>
        private const float SpeedEasing = 8f;

        /// <summary>Below this, in m/s, the bicycle counts as standing still.</summary>
        private const float Still = 0.01f;

        internal Transform FrontHub;
        internal Transform RearHub;

        private Character _character;
        private float _speed;
        private float _angle;

        private void Awake()
        {
            _character = GetComponent<Character>();
        }

        private void LateUpdate()
        {
            if (FrontHub == null || RearHub == null || _character == null)
            {
                return;
            }

            // Only travel along the bicycle's own axis rolls the wheels. Being shoved
            // sideways, or falling, does not turn them.
            var along = Vector3.Dot(_character.GetVelocity(), transform.forward);
            if (Mathf.Abs(along) < Still && Mathf.Abs(_speed) < Still)
            {
                _speed = 0f;
                return;
            }

            _speed = Mathf.Lerp(_speed, along, 1f - Mathf.Exp(-SpeedEasing * Time.deltaTime));

            // Kept inside one turn so that a long ride cannot spend the precision of a
            // float on revolutions nobody can see.
            _angle = Mathf.Repeat(_angle + _speed * DegreesPerMetre * Time.deltaTime, 360f);

            // A positive turn about the bicycle's own x axis carries the top of a wheel
            // forwards, which is a wheel rolling the way the bicycle is going.
            var spin = Quaternion.Euler(_angle, 0f, 0f);
            FrontHub.localRotation = spin;
            RearHub.localRotation = spin;
        }
    }
}
