namespace CastDesk.Tv;

/// <summary>What the TV app is playing, as reported by page-bridge.js.</summary>
/// <param name="HasVideo">A video is loaded.</param>
/// <param name="Playing">It is playing (not paused or ended).</param>
/// <param name="Title">Video title from the page's media session (or the player), or empty.</param>
/// <param name="Artist">Channel name from the page's media session (or the player), or empty.</param>
/// <param name="Artwork">Thumbnail URL, or empty.</param>
/// <param name="HasNext">The page offers a "next" action.</param>
/// <param name="HasPrevious">The page offers a "previous" action.</param>
/// <param name="Watching">The TV app is on its video page (not the home screen, search and so on).</param>
/// <param name="Music">YouTube marks the open video as music (a song, music video or podcast episode).</param>
/// <param name="VideoId">The open video's YouTube ID, or empty.</param>
/// <param name="StartedAt">When the current playback started, in Unix milliseconds (0 when paused or unknown).</param>
/// <param name="EndsAt">When it will end, in Unix milliseconds (0 when paused, live or unknown).</param>
internal sealed record MediaState(bool HasVideo, bool Playing, string Title, string Artist, string Artwork, bool HasNext, bool HasPrevious,
    bool Watching = false, bool Music = false, string VideoId = "", long StartedAt = 0, long EndsAt = 0)
{
    public static readonly MediaState Nothing = new(false, false, "", "", "", false, false);
}
