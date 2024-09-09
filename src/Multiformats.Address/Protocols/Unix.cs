using System;
using System.Linq;
using System.Text;
using BinaryEncoding;

namespace Multiformats.Address.Protocols
{
    public class Unix : MultiaddressProtocol
    {
        public string Path => Value != null ? (string)Value : string.Empty;

        public Unix()
            : base("unix", 400, -1)
        {
        }

        public Unix(string address)
            : this()
        {
            Value = address;
        }

        public override void Decode(string value)
        {
            Value = value;
        }

        public override void Decode(byte[] bytes)
        {
            Value = Encoding.UTF8.GetString(bytes);
        }

        public override byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes((string)Value);
        }
    }
}
