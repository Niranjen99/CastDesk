namespace CastDesk.Tv;

/// <summary>What the TV app is playing, as reported by page-bridge.js.</summary>
/// <param name="HasVideo">A video is loaded.</param>
/// <param name="Playing">It is playing (not paused or ended).</param>
/// <param name="Title">Video title from the page's media session, or empty.</param>
/// <param name="Artist">Channel name from the page's media session, or empty.</param>
/// <param name="Artwork">Thumbnail URL, or empty.</param>
/// <param name="HasNext">The page offers a "next" action.</param>
/// <param name="HasPrevious">The page offers a "previous" action.</param>
internal sealed record MediaState(bool HasVideo, bool Playing, string Title, string Artist, string Artwork, bool HasNext, bool HasPrevious)
{
    public static readonly MediaState Nothing = new(false, false, "", "", "", false, false);
}
