namespace Common;

/// <summary>
/// Common test utilities and helpers.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Generates a random string of specified length.
    /// </summary>
    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Generates a random URL for testing.
    /// </summary>
    public static string RandomUrl(string scheme = "https")
    {
        return $"{scheme}://{RandomString(8).ToLower()}.example.com";
    }

    /// <summary>
    /// Creates a cancellation token that cancels after the specified timeout.
    /// </summary>
    public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        return cts.Token;
    }

    /// <summary>
    /// Creates a cancellation token that cancels after the specified milliseconds.
    /// </summary>
    public static CancellationToken CreateTimeoutToken(int milliseconds)
    {
        return CreateTimeoutToken(TimeSpan.FromMilliseconds(milliseconds));
    }
}
