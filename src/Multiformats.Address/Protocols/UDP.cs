namespace Multiformats.Address.Protocols
{
    public class UDP : Number
    {
        public UDP()
            : base("udp", 17)
        {
        }

        public UDP(int port)
            : this()
        {
            Value = port;
        }

        public UDP(uint port)
            : this()
        {
            Value = (int)port;
        }

        public UDP(short port)
            : this()
        {
            Value = (int)port;
        }

        public UDP(ushort port)
            : this()
        {
            Value = (int)port;
        }
    }
}
