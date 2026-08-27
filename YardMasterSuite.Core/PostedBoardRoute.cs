namespace YardMasterSuite.Core
{
    /// <summary>
    /// Path-ahead membership for Next (6.10). Unknown track stays on the
    /// corridor (6.9); only a known other-branch track is ignored.
    /// </summary>
    public static class PostedBoardRoute
    {
        /// <summary>
        /// Attach closer than this is a real track identity. Farther (siding
        /// grab) falls back to the facing corridor so a mainline 6 is not dropped.
        /// On-path boards stay trusted even when the board sits a few metres off the rail.
        /// </summary>
        public const float ConfidentAttachMeters = 4f;

        /// <summary>
        /// True when this board is on a resolved track that is not on our
        /// thrown route. False when the board track is unknown — caller uses
        /// facing corridor so a nearby 6 is not dropped.
        /// </summary>
        public static bool IsOffRoute(bool hasPath, bool boardTrackKnown, bool onPath) =>
            hasPath && boardTrackKnown && !onPath;

        /// <summary>
        /// Trust GetClosest only when the board is on our path, or the attach
        /// is tight enough to be that rail (not a parallel siding).
        /// </summary>
        public static bool TrackIdentityTrusted(bool onPath, float attachMeters) =>
            onPath || (attachMeters >= 0f && attachMeters <= ConfidentAttachMeters);
    }
}
