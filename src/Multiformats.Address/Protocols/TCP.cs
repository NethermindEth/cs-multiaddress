namespace Multiformats.Address.Protocols;

public class TCP : PortNumber
{
    public TCP()
        : base("tcp", 6)
    {
    }

    public TCP(int port)
        : this()
    {
        Value = port;
    }

    public TCP(uint port)
        : this()
    {
        Value = (int)port;
    }

    public TCP(short port)
        : this()
    {
        Value = (int)port;
    }

    public TCP(ushort port)
        : this()
    {
        Value = (int)port;
    }
}
