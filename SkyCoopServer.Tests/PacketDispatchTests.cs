using System;
using System.Collections.Generic;
using Xunit;

namespace SkyCoopServer.Tests
{
    public class PacketDispatchTests
    {
        [Fact]
        public void Invoke_ReturnsTrue_WhenHandlerSucceeds()
        {
            bool called = false;

            bool result = PacketDispatch.Invoke(
                (int)Packet.Type.Welcome,
                () => called = true,
                _ => { });

            Assert.True(result);
            Assert.True(called);
        }

        [Fact]
        public void Invoke_ReturnsFalse_AndLogsOnce_WhenHandlerThrows()
        {
            var logs = new List<string>();

            bool result = PacketDispatch.Invoke(
                (int)Packet.Type.Welcome,
                () => throw new InvalidOperationException("boom"),
                logs.Add);

            Assert.False(result);
            Assert.Single(logs);
            Assert.Contains("Welcome", logs[0]);
            Assert.Contains("InvalidOperationException", logs[0]);
            Assert.Equal(
                "[Dispatch] Welcome failed: InvalidOperationException: boom",
                logs[0]);
        }

        [Fact]
        public void Describe_UnknownId_ReturnsUnknownWithNumber()
        {
            Assert.Equal("Unknown(999999)", PacketDispatch.Describe(999999));
        }

        [Fact]
        public void Invoke_DoesNotThrow_WhenLoggerThrows()
        {
            Exception? thrown = Record.Exception(() => PacketDispatch.Invoke(
                (int)Packet.Type.Welcome,
                () => throw new InvalidOperationException("boom"),
                _ => throw new InvalidOperationException("logger boom")));

            Assert.Null(thrown);
        }

        [Fact]
        public void Guard_DoesNotThrow_WhenLoggerThrows()
        {
            Exception? thrown = Record.Exception(() => PacketDispatch.Guard(
                "PeerConnectedEvent",
                () => throw new InvalidOperationException("listener boom"),
                _ => throw new InvalidOperationException("logger boom")));

            Assert.Null(thrown);
        }

        [Fact]
        public void Guard_DoesNotRethrow()
        {
            var logs = new List<string>();

            bool result = PacketDispatch.Guard(
                "PeerConnectedEvent",
                () => throw new InvalidOperationException("listener boom"),
                logs.Add);

            Assert.False(result);
            Assert.Single(logs);
            Assert.Contains("PeerConnectedEvent", logs[0]);
            Assert.Contains("InvalidOperationException", logs[0]);
        }
    }
}
