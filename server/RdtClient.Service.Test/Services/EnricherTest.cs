using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Service.Services;
using MonoTorrent.BEncoding;

namespace RdtClient.Service.Test.Services;

public class EnricherTest : IDisposable
{
    private readonly MockRepository _mockRepository;
    private readonly Mock<ILogger<Enricher>> _loggerMock;
    private readonly Mock<ITrackerListGrabber> _trackerListGrabberMock;

    public EnricherTest()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _loggerMock = _mockRepository.Create<ILogger<Enricher>>(MockBehavior.Loose);
        _trackerListGrabberMock = _mockRepository.Create<ITrackerListGrabber>();
    }

    public void Dispose()
    {
        _mockRepository.VerifyAll();
    }

    private const String TestMagnetLink =
        "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=TestFile&tr=http%3A%2F%2Ftracker1.com%2Fannounce&tr=http%3A%2F%2Ftracker2.com%2Fannounce";

    [Fact]
    public async Task EnrichMagnetLink_AddsNoTrackers_WhenNoTrackersFromTrackerGrabber()
    {
        // Arrange
        SetupTrackerListGrabber([]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink, enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_AddsTrackers_WhenTrackersFromTrackerGrabber()
    {
        // Arrange
        SetupTrackerListGrabber(["http://new-tracker.com/announce"]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink + $"&tr={Uri.EscapeDataString("http://new-tracker.com/announce")}", enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_DoesNotAddDuplicateTrackers_WhenTrackersFromTrackerGrabberAlreadyPresent()
    {
        // Arrange
        SetupTrackerListGrabber(["http://new-tracker.com/announce", "http://tracker1.com/announce"]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink + $"&tr={Uri.EscapeDataString("http://new-tracker.com/announce")}", enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_Throws_WhenTrackerGrabberThrows()
    {
        // Arrange
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ThrowsAsync(new InvalidOperationException("Unable to fetch tracker list for enrichment."));

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichMagnetLink(TestMagnetLink));
    }

    [Fact]
    public async Task EnrichTorrentBytes_AddsTrackers_WhenTrackersFromTrackerGrabber()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(originalTracker),
            ["announce-list"] = new BEncodedList
            {
                new BEncodedList
                {
                    new BEncodedString(originalTracker)
                }
            }
        };

        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce"));
        Assert.True(enrichedDict.ContainsKey("announce-list"));

        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Contains(originalTracker, flattened);
        Assert.Contains(newTracker, flattened);

        var announce = ((BEncodedString)enrichedDict["announce"]).Text;
        Assert.Equal(flattened.First(), announce);
    }

    private void SetupTrackerListGrabber(String[] trackerList)
    {
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ReturnsAsync(trackerList)
            .Verifiable();
    }

    [Fact]
    public async Task EnrichTorrentBytes_DoesNotAddTrackers_WhenNoTrackersFromTrackerGrabber()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(originalTracker),
            ["announce-list"] = new BEncodedList
            {
                new BEncodedList
                {
                    new BEncodedString(originalTracker)
                }
            }
        };

        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce"));
        Assert.True(enrichedDict.ContainsKey("announce-list"));

        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Single(flattened);
        Assert.Contains(originalTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_DoesNotAddDuplicateTrackers()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var duplicateTracker = "http://tracker1.com/announce";

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(originalTracker),
            ["announce-list"] = new BEncodedList
            {
                new BEncodedList
                {
                    new BEncodedString(originalTracker)
                }
            }
        };

        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([duplicateTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Single(flattened);
        Assert.Contains(originalTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_AddsTrackers_WhenNoAnnounceListPresent()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(originalTracker)

            // No "announce-list"
        };

        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce-list"));
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Contains(originalTracker, flattened);
        Assert.Contains(newTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_Throws_WhenTrackerGrabberThrows()
    {
        // Arrange
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ThrowsAsync(new InvalidOperationException("Unable to fetch tracker list for enrichment."));

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString("http://tracker1.com/announce"),
            ["announce-list"] = new BEncodedList
            {
                new BEncodedList
                {
                    new BEncodedString("http://tracker1.com/announce")
                }
            }
        };

        var originalTorrentBytes = torrentDict.Encode();

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichTorrentBytes(originalTorrentBytes));
    }

    [Fact]
    public async Task EnrichMagnetLink_ReturnsOriginal_WhenMagnetIsMalformed()
    {
        SetupTrackerListGrabber(new[]
        {
            "http://some-tracker.com/announce"
        });

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // No '?' in magnet link (malformed)
        var malformed = "magnet:xt=bad";
        var result = await enricher.EnrichMagnetLink(malformed);

        Assert.Equal(malformed, result);

        // Magnet ends with '?'
        var endsWithQ = "magnet:?";
        var result2 = await enricher.EnrichMagnetLink(endsWithQ);

        Assert.Equal(endsWithQ, result2);
    }

    [Fact]
    public async Task EnrichTorrentBytes_ThrowsArgumentException_OnNullOrEmptyInput()
    {
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => enricher.EnrichTorrentBytes(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => enricher.EnrichTorrentBytes(Array.Empty<Byte>()));
    }

    [Fact]
    public async Task EnrichTorrentBytes_ThrowsInvalidOperationException_OnNonTorrentBytes()
    {
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        var notTorrent = new Byte[]
        {
            1, 2, 3, 4, 5
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichTorrentBytes(notTorrent));
    }
}
