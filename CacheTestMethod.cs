[Fact]
public void TestMethod()
{
    var expectedKey = "expectedKey";
    var expectedValue = "expectedValue";
    var expectedMilliseconds = 100;
    var mockCache = new Mock<IMemoryCache>();
    var mockCacheEntry = new Mock<ICacheEntry>();

    string? keyPayload = null;
    mockCache
        .Setup(mc => mc.CreateEntry(It.IsAny<object>()))
        .Callback((object k) => keyPayload = (string)k)
        .Returns(mockCacheEntry.Object); // this should address your null reference exception

    object? valuePayload = null;
    mockCacheEntry
        .SetupSet(mce => mce.Value = It.IsAny<object>())
        .Callback<object>(v => valuePayload = v);

    TimeSpan? expirationPayload = null;
    mockCacheEntry
        .SetupSet(mce => mce.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan?>())
        .Callback<TimeSpan?>(dto => expirationPayload = dto);

    // Act
    var success = _target.SetCacheValue(expectedKey, expectedValue,
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMilliseconds(expectedMilliseconds)));

    // Assert
    Assert.True(success);
    Assert.Equal(expectedKey, keyPayload);
    Assert.Equal(expectedValue, valuePayload as string);
    Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), expirationPayload);
}
