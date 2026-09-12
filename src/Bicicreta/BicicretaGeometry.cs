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

        /// <summary>
        /// The frame is only taken in this far across, rather than in proportion with
        /// its length: a cart is made to carry cargo and a bicycle is not, so a frame
        /// scaled evenly is far too wide to sit astride. Narrower than the seat, which
        /// then overhangs it the way a saddle does.
        /// </summary>
        internal const float FrameWidth = 0.3f;

        /// <summary>Height of the middle of the frame.</summary>
        internal const float FrameHeight = 0.6f;

        /// <summary>
        /// The top of the frame: what the seat and the handlebar stand on. The cart body
        /// is 0.35 m deep once it has been taken in to this length.
        /// </summary>
        internal const float FrameTop = FrameHeight + 0.175f;

        /// <summary>
        /// Where the frame's body ends and its two pull handles carry on forward towards
        /// <see cref="FrameFront"/>. The cart is one mesh, and not one this build of the
        /// game will let anything read, so this was measured off a screenshot.
        /// </summary>
        internal const float FrameBodyFront = 0.08f;

        /// <summary>Where the rider's weight rests: the height of the seat's pan.</summary>
        internal const float SeatPanHeight = 1.03f;

        /// <summary>How far behind the middle of the bicycle the pan sits.</summary>
        internal const float SeatPanOffset = -0.56f;

        /// <summary>
        /// The riding animation poses the rider around their root, which is at their
        /// feet, so their weight ends up well behind it and, as it turns out, a shade
        /// above it rather than below. Measured off screenshots against the bicycle's own
        /// known dimensions; without correcting for it the seat is built somewhere the
        /// rider is not.
        /// </summary>
        internal const float RiderSeatBack = 0.46f;

        internal const float RiderSeatDrop = -0.015f;

        internal const float AttachHeight = SeatPanHeight + RiderSeatDrop;

        internal const float AttachOffset = SeatPanOffset + RiderSeatBack;

        /// <summary>
        /// Where the rider's hands come to rest in the riding pose, which is what the
        /// handlebar has to reach to look held rather than nearby.
        /// </summary>
        internal const float HandlebarHeight = 1.47f;

        internal const float HandlebarOffset = -0.06f;

        /// <summary>
        /// Wide enough to read as a handlebar. The rider's hands sit about 0.13 m apart,
        /// so they hold it well inboard of its ends.
        /// </summary>
        internal const float HandlebarWidth = 0.36f;

        internal const float BarThickness = 0.07f;

        /// <summary>
        /// The stem stands at the very front of the frame's body, which is a little
        /// ahead of the hands, so the handlebar reaches back to them over a short neck.
        /// </summary>
        internal const float StemOffset = FrameBodyFront - BarThickness / 2f;

        internal const float BodyHeight = 1.3f;

        internal const float BodyRadius = 0.45f;

        /// <summary>
        /// How far from the middle a collision can hurt something: far enough to cover
        /// the whole bicycle, near enough that riding past is not riding into.
        /// </summary>
        internal const float HitRadius = (FrontWheel - RearWheel) / 2f + WheelRadius;
    }
}
