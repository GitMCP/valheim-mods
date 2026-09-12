namespace Bicicreta
{
    /// <summary>
    /// The bicycle's dimensions, in metres, along its own axis: +z is the way it faces.
    ///
    /// These are shared because the visual, the collider, and the rider's seat have to
    /// agree: a lox is 7.6 m long and seats its rider 3.4 m up, so a bicycle-sized model
    /// on an untouched lox would leave the rider floating in the air above it and the
    /// player bumping into a body that is not drawn anywhere.
    /// </summary>
    internal static class BicicretaGeometry
    {
        internal const float WheelRadius = 0.4f;

        internal const float FrontWheel = 0.55f;

        /// <summary>
        /// Far enough back that the wheel is clear of both the frame and the seat. It
        /// cannot be tucked in closer: the frame has to reach under the seat, the wheel
        /// stands taller than the underside of the frame, and so the two only avoid each
        /// other when the wheel is entirely behind it.
        /// </summary>
        internal const float RearWheel = -1.35f;

        internal const float FrameFront = 0.55f;

        internal const float FrameBack = -0.95f;

        internal const float FrameLength = FrameFront - FrameBack;

        internal const float FrameMiddle = (FrameFront + FrameBack) / 2f;

        /// <summary>Height of the middle of the frame.</summary>
        internal const float FrameHeight = 0.6f;

        /// <summary>Where the rider's weight rests: the height of the seat's pan.</summary>
        internal const float SeatPanHeight = 1.03f;

        /// <summary>How far behind the middle of the bicycle the pan sits.</summary>
        internal const float SeatPanOffset = -0.56f;

        /// <summary>
        /// The riding animation poses the rider around their root, which is at their
        /// feet, so their weight ends up well behind and a little below it. Measured off
        /// a screenshot against the bicycle's own known dimensions; without correcting
        /// for it the seat is built somewhere the rider is not.
        /// </summary>
        internal const float RiderSeatBack = 0.46f;

        internal const float RiderSeatDrop = 0.12f;

        internal const float AttachHeight = SeatPanHeight + RiderSeatDrop;

        internal const float AttachOffset = SeatPanOffset + RiderSeatBack;

        internal const float BodyHeight = 1.3f;

        internal const float BodyRadius = 0.45f;

        /// <summary>
        /// How far from the middle a collision can hurt something: far enough to cover
        /// the whole bicycle, near enough that riding past is not riding into.
        /// </summary>
        internal const float HitRadius = (FrontWheel - RearWheel) / 2f + WheelRadius;
    }
}
