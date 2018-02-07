using System;
using System.IO;
using System.Threading.Tasks;
using RingByteBuffer;
using TPPCore.Irc;
using Xunit;

namespace TPPCore.Utils.Tests
{
    public class IrcClientTest
    {
        [Fact]
        public async Task TestClient()
        {
            var inputStream = new RingBufferStream(100000, false);
            var outputStream = new RingBufferStream(100000, false);

            // The streams aren't readable until there is data,
            // so add something before constructing the stream readers.
            inputStream.WriteByte(0);
            outputStream.WriteByte(0);

            var inputWriter = new StreamWriter(inputStream);
            var outputReader = new StreamReader(outputStream);

            var client = new IrcClient(new StreamReader(inputStream),
                new StreamWriter(outputStream));

            // Undo the hack
            inputStream.ReadByte();
            outputStream.ReadByte();

            client.EnableCtcpPingVersion()
                .EnablePingHandler()
                .EnableChannelTracker();

            client.RateLimiter = new TokenBucketRateLimiter(20, 30);

            await client.Register("someone", "someone", "someone");

            Assert.Equal("NICK someone", outputReader.ReadLine());
            Assert.Equal("USER someone 8 * :someone", outputReader.ReadLine());

            inputWriter.WriteLine(":example.com 001 someone :Welcome");
            inputWriter.WriteLine(":example.com 002 someone :Host is fake");
            inputWriter.WriteLine(":example.com 003 someone :Created sometime");
            inputWriter.WriteLine(":example.com 004 someone :Nothing useful");
            inputWriter.Flush();

            await client.WaitReply(NumericalReplyCodes.RPL_WELCOME, 1000);

            Assert.Equal("someone", client.ClientId.Nickname);

            inputWriter.WriteLine(":example.com 375 someone :- Hello");
            inputWriter.WriteLine(":example.com 372 someone :- blah blah blah");
            inputWriter.WriteLine(":example.com 376 someone :End MOTD");
            inputWriter.Flush();

            await client.WaitReply(NumericalReplyCodes.RPL_ENDOFMOTD, 1000);

            await client.Join(new[] {"#test"});
            Assert.StartsWith("JOIN #test", outputReader.ReadLine());

            inputWriter.WriteLine(":someone!someone@example.com JOIN :#test");
            inputWriter.Flush();

            var message = await client.ReadMessage();
            Assert.Equal("#test", message.TargetLower);

            await client.Privmsg("#test", "hello world!");
            Assert.StartsWith("PRIVMSG #test :hello world!", outputReader.ReadLine());

            inputWriter.WriteLine("@display-name=Fred :fred!~fred@example.com PRIVMSG #test :hey");
            inputWriter.Flush();
            message = await client.ReadMessage();
            Assert.Equal("Fred", message.Tags["display-name"]);
            Assert.Equal("~fred", message.Prefix.ClientId.User);
            Assert.Equal("PRIVMSG", message.Command);
            Assert.Equal("hey", message.TrailingParameter);

            inputWriter.WriteLine(":example.com PING :abc");
            inputWriter.Flush();
            await client.ProcessOnce();
            Assert.Equal("PONG :abc", outputReader.ReadLine());

            inputWriter.WriteLine(":drone!botscan@example.com PRIVMSG someone :\u0001VERSION\u0001");
            inputWriter.Flush();
            await client.ProcessOnce();
            Assert.StartsWith("NOTICE drone :\u0001VERSION ", outputReader.ReadLine());

            inputWriter.WriteLine(":drone!botscan@example.com PRIVMSG someone :\u0001PING abc\u0001");
            inputWriter.Flush();
            await client.ProcessOnce();
            Assert.Equal("NOTICE drone :\u0001PING abc\u0001", outputReader.ReadLine());
        }
    }
}
