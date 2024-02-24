namespace PlexWatch.Utilities;

/// <summary>
/// Credit to @RaidMax for below Extension https://github.com/RaidMax/IW4M-Admin
/// </summary>
public static class EventExtensions
{
    public static Task InvokeAsync<TEventType>(this Func<TEventType, CancellationToken, Task>? function, TEventType eventArgType,
        CancellationToken token)
    {
        if (function is null) return Task.CompletedTask;

        return Task.WhenAll(function.GetInvocationList().Cast<Func<TEventType, CancellationToken, Task>>()
            .Select(x => RunHandler(x, eventArgType, token)));
    }

    private static async Task RunHandler<TEventType>(Func<TEventType, CancellationToken, Task> handler, TEventType eventArgType,
        CancellationToken token)
    {
        // special case to allow tasks like request after delay to run longer
        if (token == CancellationToken.None) await handler(eventArgType, token);

        using var timeoutToken = new CancellationTokenSource();
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken.Token);

        try
        {
            await handler(eventArgType, tokenSource.Token);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
