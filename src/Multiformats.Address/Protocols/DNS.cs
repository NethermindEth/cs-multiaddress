using System.Text;

namespace Multiformats.Address.Protocols;

public class DNS : MultiaddressProtocol
{
    public DNS()
        : base("dns", 53, -1)
    {
    }

    public DNS(string address)
        : this()
    {
        Value = address;
    }

    public override void Decode(string value) => Value = value;
    public override void Decode(byte[] bytes) => Value = Encoding.UTF8.GetString(bytes);
    public override byte[] ToBytes() => Encoding.UTF8.GetBytes((string)Value);
    public override string ToString() => (string)Value ?? string.Empty;
}
