interface IConnectedStream : IDisposable
{
    public Stream DataStream { get; }
    public bool IsConnected { get; }
}
