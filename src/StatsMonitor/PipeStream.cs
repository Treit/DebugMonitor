
using System.IO.Pipes;

sealed class PipeStream : IConnectedStream, IDisposable
{
    bool _disposed;
    readonly NamedPipeClientStream _clientStream;

    public PipeStream(string host, string pipeName)
    {
        var npcs = new NamedPipeClientStream(host, pipeName, PipeDirection.In);
        _clientStream = npcs;
        _clientStream.Connect();
    }

    public Stream DataStream
    {
        get
        {
            ThrowIfDisposed();
            return _clientStream;
        }
    }

    public bool IsConnected => _clientStream.IsConnected;

    public void Dispose()
    {
        _clientStream.Dispose();
        _disposed = true;
    }

    void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PipeStream));
        }
    }
}