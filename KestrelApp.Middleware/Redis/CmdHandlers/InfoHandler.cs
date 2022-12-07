﻿using System;
using System.Threading.Tasks;

namespace KestrelApp.Middleware.Redis.CmdHandlers
{
    /// <summary>
    /// Info处理者
    /// </summary>
    sealed class InfoHandler : CmdHandler
    {
        /// <summary>
        /// 是否能处理
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool CanHandle(RedisContext context)
        {
            return context.Cmd.Name == RedisCmdName.Info;
        }

        /// <summary>
        /// 处理命令
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected override async Task HandleAsync(RedisClient client, RedisCmd cmd)
        {
            var response = new InfoResponse("redis_version: 9.9.9");
            await client.ResponseAsync(response);
        }

        private class InfoResponse : RedisResponse
        {
            private readonly BufferBuilder builder = new();

            public InfoResponse(string info)
            {
                //$935
                //redis_version: 2.4.6

                builder
                    .Write('$').Write(info.Length.ToString()).WriteLine()
                    .Write(info).WriteLine();
            }

            public override ReadOnlyMemory<byte> ToMemory()
            {
                return this.builder.WrittenMemory;
            }
        }
    }
}
