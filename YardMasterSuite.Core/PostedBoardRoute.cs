namespace YardMasterSuite.Core
{
    /// <summary>
    /// Path-ahead membership for Next (6.10). Unknown track stays on the
    /// corridor (6.9); only a known other-branch track is ignored.
    /// </summary>
    public static class PostedBoardRoute
    {
        /// <summary>
        /// True when this board is on a resolved track that is not on our
        /// thrown route. False when the board track is unknown — caller uses
        /// facing corridor so a nearby 6 is not dropped.
        /// </summary>
        public static bool IsOffRoute(bool hasPath, bool boardTrackKnown, bool onPath) =>
            hasPath && boardTrackKnown && !onPath;
    }
}
