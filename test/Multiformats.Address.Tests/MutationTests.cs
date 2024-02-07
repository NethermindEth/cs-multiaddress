using Multiformats.Address.Protocols;
using Xunit;

namespace Multiformats.Address.Tests
{
    public class MutationTests
    {
        [Fact]
        public void TestReplacement()
        {
            var m = Multiaddress.Decode("/ip4/127.0.0.1/udp/1234");
            Assert.Equal("/ip4/0.0.0.0/udp/1234", m.ReplaceOrAdd<IP4>("0.0.0.0"));
            Assert.Equal("/ip4/0.0.0.0/udp/5678", m.ReplaceOrAdd<UDP>(5678));
            Assert.Equal("/ip4/0.0.0.0/udp/5678/p2p/QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC", m.ReplaceOrAdd<P2P>("QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC"));
        }
    }
}
