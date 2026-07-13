namespace Anhei4Map.Core.Services;

public static class RetryPolicy
{
    private static readonly int[] Delays = [0, 1000, 2000, 4000, 8000, 30000];
    private const int MaxRetries = 10;
    private const int MaxDelayMs = 30000;

    public static int? NextDelay(int attempt)
    {
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be non-negative.");
        }

        if (attempt >= MaxRetries)
        {
            return null;
        }

        if (attempt < Delays.Length)
        {
            return Delays[attempt];
        }

        return MaxDelayMs;
    }
}
