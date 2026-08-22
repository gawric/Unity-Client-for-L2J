using System.Threading;

public abstract class ServerPacketHandler
{
    protected AsynchronousClient _client;
    protected CancellationTokenSource _tokenSource;

    public void SetClient(AsynchronousClient client, ClientPacketHandler clientPacketHandler)
    {
        _client = client;
        _tokenSource = new CancellationTokenSource();
    }

    public void CancelTokens()
    {
        if (_tokenSource != null)
            _tokenSource.Cancel();
    }
}
