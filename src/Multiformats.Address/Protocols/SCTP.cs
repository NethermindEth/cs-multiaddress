namespace Multiformats.Address.Protocols;

public class SCTP : PortNumber
{
    public SCTP()
        : base("sctp", 132)
    {
    }

    public SCTP(int port)
        : this()
    {
        Value = port;
    }
}
