
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using MonoTorrent.BEncoding;

namespace RdtClient.Service.Services;

public interface IEnricher
{
    Task<String> EnrichMagnetLink(String magnetLink);
    Task<Byte[]> EnrichTorrentBytes(Byte[] torrentBytes);
}

/// <summary>
/// Enriches magnet links and torrents by adding trackers from the tracker list grabber.
/// </summary>
public sealed class Enricher(ILogger<Enricher> logger, ITrackerListGrabber trackerListGrabber) : IEnricher
{
    /// <summary>
    /// Add trackers from the tracker list grabber to the magnet link.
    /// </summary>
    /// <param name="magnetLink">Magnet link to add trackers to. Is not modified</param>
    /// <returns>Magnet link with additional trackers</returns>
    public async Task<String> EnrichMagnetLink(String magnetLink)
    {
        var newTrackers = await trackerListGrabber.GetTrackers().ConfigureAwait(false);

        if (newTrackers.Length == 0)
        {
            logger.LogWarning("No new trackers were retrieved.");

            return magnetLink;
        }

        var qmIdx = magnetLink.IndexOf('?');

        if (qmIdx == -1 || qmIdx == magnetLink.Length - 1)
        {
            // No query string or empty query string
            var sb = new StringBuilder(magnetLink);

            if (qmIdx == -1)
            {
                sb.Append('?');
            }

            // Only add '&' between trackers, not before the first tracker when magnet ends with '?'
            for (var i = 0; i < newTrackers.Length; i++)
            {
                // Only add '&' for >0 trackers, or if the magnet did NOT end with '?' already
                if (i > 0 || (qmIdx != magnetLink.Length - 1 && qmIdx != -1))
                {
                    sb.Append('&');
                }

                sb.Append("tr=").Append(Uri.EscapeDataString(newTrackers[i]));
            }

            logger.LogInformation("Added {NewTrackersCount} new trackers to a magnet link with no initial query string. Total trackers: {TotalTrackersCount}.",
                                  newTrackers.Length,
                                  newTrackers.Length);

            return sb.ToString();
        }

        var schemePart = magnetLink[..qmIdx];
        var queryPart = magnetLink[(qmIdx + 1)..];

        // Manually parse query string into dictionary (to preserve current encodings!)
        var queryKVs = HttpUtility.ParseQueryString(queryPart);

        // gather key-values
        var paramDict = new Dictionary<String, List<String>>(StringComparer.OrdinalIgnoreCase);

        foreach (String key in queryKVs)
        {
            if (key == null)
            {
                continue;
            }

            if (!paramDict.ContainsKey(key))
            {
                paramDict[key] = new List<String>();
            }

            paramDict[key].AddRange(queryKVs.GetValues(key) ?? []);
        }

        // Remove any "tr" values and copy existing ones to a set
        var existingTrackers = paramDict.TryGetValue("tr", out var value)
            ? new HashSet<String>(value, StringComparer.OrdinalIgnoreCase)
            : new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        paramDict.Remove("tr");

        // Add new trackers if they aren't duplicates
        var newUniqueTrackers = newTrackers.Where(t => !existingTrackers.Contains(t)).ToList();

        foreach (var tr in newUniqueTrackers)
        {
            existingTrackers.Add(tr);
        }

        // Reconstruct param string: keep xt and dn as unencoded (since that's standard)
        var outParams = new List<String>();

        foreach (var kv in paramDict)
        {
            // for official keys like xt and dn, keep as-is (value should not be encoded again)
            foreach (var v in kv.Value)
            {
                if (kv.Key == "xt" || kv.Key == "dn")
                {
                    outParams.Add($"{kv.Key}={v}");
                }
                else
                {
                    outParams.Add($"{kv.Key}={HttpUtility.UrlEncode(v)}");
                }
            }
        }

        // trackers: always tr=... and encode the tracker value
        foreach (var tr in existingTrackers)
        {
            outParams.Add($"tr={Uri.EscapeDataString(tr)}");
        }

        var finalMagnet = schemePart + "?" + String.Join("&", outParams);

        logger.LogInformation("Added {NewTrackersCount} new trackers to the magnet link. Total trackers: {TotalTrackersCount}.",
                              newUniqueTrackers.Count,
                              existingTrackers.Count);

        return finalMagnet;
    }

    /// <summary>
    /// Add trackers from the tracker list grabber to the .torrent file bytes.
    /// </summary>
    /// <param name="torrentBytes">Torrent file bytes to add trackers to. Is not modified</param>
    /// <returns>Torrent file bytes with additional trackers</returns>
    public async Task<Byte[]> EnrichTorrentBytes(Byte[] torrentBytes)
    {
        if (torrentBytes == null || torrentBytes.Length == 0)
        {
            throw new ArgumentException("Torrent bytes cannot be null or empty.", nameof(torrentBytes));
        }

        BEncodedDictionary torrentDict;

        try
        {
            torrentDict = BEncodedValue.Decode<BEncodedDictionary>(torrentBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decode torrent bytes.");

            throw new InvalidOperationException("Invalid torrent file format.", ex);
        }

        var newTrackers = await trackerListGrabber.GetTrackers().ConfigureAwait(false);

        // --- Fixed section:
        if (!newTrackers.Any())
        {
            logger.LogWarning("No new trackers were retrieved.");

            torrentDict.Remove("announce");

            torrentDict["announce-list"] = new BEncodedList();

            return torrentDict.Encode();
        }

        var seenTrackers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        var allTrackers = new List<String>();

        if (torrentDict.TryGetValue("announce-list", out var alc) && alc is BEncodedList alList)
        {
            foreach (var tier in alList.OfType<BEncodedList>())
            {
                foreach (var s in tier.OfType<BEncodedString>())
                {
                    if (seenTrackers.Add(s.Text))
                    {
                        allTrackers.Add(s.Text);
                    }
                }
            }
        }

        if (torrentDict.TryGetValue("announce", out var announceValue) && announceValue is BEncodedString announceStr)
        {
            if (seenTrackers.Add(announceStr.Text))
            {
                allTrackers.Add(announceStr.Text);
            }
        }

        var addedTrackersCount = 0;

        foreach (var tracker in newTrackers)
        {
            if (seenTrackers.Add(tracker)) // if true, it was a new tracker not seen before
            {
                allTrackers.Add(tracker);
                addedTrackersCount++; // Increment here
            }
        }

        var dedupedAnnounceList = new BEncodedList();

        foreach (var tracker in allTrackers)
        {
            dedupedAnnounceList.Add(new BEncodedList
            {
                new BEncodedString(tracker)
            });
        }

        torrentDict["announce-list"] = dedupedAnnounceList;

        if (allTrackers.Count > 0)
        {
            torrentDict["announce"] = new BEncodedString(allTrackers[0]);
        }
        else
        {
            torrentDict.Remove("announce"); // Explicitly remove if no trackers are available.
        }

        logger.LogInformation("Added {NewTrackersCount} new trackers to the torrent. Total trackers: {TotalTrackersCount}.",
                              addedTrackersCount,
                              allTrackers.Count);

        return torrentDict.Encode();
    }
}