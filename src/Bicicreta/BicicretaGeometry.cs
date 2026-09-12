namespace Bicicreta
{
    /// <summary>
    /// The bicycle's dimensions, in metres.
    ///
    /// These are shared because the visual, the collider, and the rider's seat have to
    /// agree: a lox is 7.6 m long and seats its rider 3.4 m up, so a bicycle-sized model
    /// on an untouched lox would leave the rider floating in the air above it and the
    /// player bumping into a body that is not drawn anywhere.
    /// </summary>
    internal static class BicicretaGeometry
    {
        internal const float WheelRadius = 0.4f;

        /// <summary>Distance between the two wheel centres.</summary>
        internal const float Wheelbase = 1.1f;

        /// <summary>Height of the frame, and so roughly where the rider sits.</summary>
        internal const float FrameHeight = 0.6f;

        internal const float SeatHeight = 0.95f;

        /// <summary>How far behind the front wheel the rider sits.</summary>
        internal const float SeatOffset = -0.1f;

        internal const float BodyHeight = 1.3f;

        internal const float BodyRadius = 0.45f;

        /// <summary>
        /// How far from the middle a collision can hurt something: far enough to cover
        /// the whole bicycle, near enough that riding past is not riding into.
        /// </summary>
        internal const float HitRadius = Wheelbase / 2f + BodyRadius;
    }
}
