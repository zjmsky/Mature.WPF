﻿using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mature.Socket.Server.DotNetty
{
    public class ChannelManagerHandler : ChannelHandlerAdapter
    {
        public override bool IsSharable => true;
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            Console.WriteLine($"新连接建立：{context.Channel.Id.AsShortText()}");
        }
    }
}
