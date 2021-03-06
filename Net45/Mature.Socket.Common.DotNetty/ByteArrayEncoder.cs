using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mature.Socket.Common.DotNetty
{
    public class ByteArrayEncoder : MessageToByteEncoder<byte[]>
    {
        protected override void Encode(IChannelHandlerContext context, byte[] message, IByteBuffer output)
        {
            output.WriteBytes(message);
        }
    }
}
