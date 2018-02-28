using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TPPCore.Service.Chat.Tests
{
    public class MockTwitchIrcServer
    {
        public int Port { get { return ((IPEndPoint) listener.LocalEndpoint).Port; }}
        private TcpListener listener;
        private bool running = false;

        public MockTwitchIrcServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
        }

        public async Task Run()
        {
            listener.Start();
            running = true;

            var task = listener.AcceptTcpClientAsync();

            while (running) {
                var cancelSource = new CancellationTokenSource();
                var delayTask = Task.Delay(1000, cancelSource.Token);
                var result = await Task.WhenAny(task, delayTask);

                if (result == task)
                {
                    cancelSource.Cancel();
                    var client = await task;
                    task = listener.AcceptTcpClientAsync();
                    await handleClient(client);
                }
            }
        }

        public void Stop()
        {
            running = false;
        }

        private async Task handleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream);
            var reader = new StreamReader(stream);

            await writer.WriteLineAsync(":tmi.twitch.tv 001 exampleUser :Welcome, GLHF!");
            await writer.WriteLineAsync(":tmi.twitch.tv 002 exampleUser :Your host is tmi.twitch.tv");
            await writer.WriteLineAsync(":tmi.twitch.tv 003 exampleUser :This server is rather new");
            await writer.WriteLineAsync(":tmi.twitch.tv 004 exampleUser :-");
            await writer.WriteLineAsync(":tmi.twitch.tv 375 exampleUser :-");
            await writer.WriteLineAsync(":tmi.twitch.tv 372 exampleUser :You are in a maze of twisty passages.");
            await writer.WriteLineAsync(":tmi.twitch.tv 376 exampleUser :>");

            await writer.WriteLineAsync(":exampleUser!exampleUser@exampleUser.tmi.twitch.tv JOIN #channelTest");

            await writer.WriteLineAsync(":exampleUser.tmi.twitch.tv 353 exampleUser = #<channelTest> :exampleUser");
            await writer.WriteLineAsync(":exampleUser.tmi.twitch.tv 366 exampleUser #<channelTest> :End of /NAMES list");

            await writer.WriteLineAsync(@"@ban-reason=Follow\sthe\srules :tmi.twitch.tv CLEARCHAT #dallas :ronni");
            await writer.WriteLineAsync(@"@color=#0D4200;display-name=dallas;emote-sets=0,33,50,237,793,2126,3517,4578,5569,9400,10337,12239;turbo=0;user-id=1337;user-type=admin :tmi.twitch.tv GLOBALUSERSTATE");
            await writer.WriteLineAsync(@"@badges=global_mod/1,turbo/1;color=#0D4200;display-name=dallas;emotes=25:0-4,12-16/1902:6-10;id=b34ccfc7-4977-403a-8a94-33c6bac34fb8;mod=0;room-id=1337;subscriber=0;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=global_mod :ronni!ronni@ronni.tmi.twitch.tv PRIVMSG #dallas :Kappa Keepo Kappa");
            await writer.WriteLineAsync(@"@badges=staff/1,bits/1000;bits=100;color=;display-name=dallas;emotes=;id=b34ccfc7-4977-403a-8a94-33c6bac34fb8;mod=0;room-id=1337;subscriber=0;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=staff :ronni!ronni@ronni.tmi.twitch.tv PRIVMSG #dallas :cheer100");
            await writer.WriteLineAsync(@"@broadcaster-lang=en;r9k=0;slow=0;subs-only=0 :tmi.twitch.tv ROOMSTATE #dallas");
            await writer.WriteLineAsync(@"@slow=10 :tmi.twitch.tv ROOMSTATE #dallas");
            await writer.WriteLineAsync(@"@msg-id=slow_off :tmi.twitch.tv NOTICE #dallas :This room is no longer in slow mode.");

            await writer.WriteLineAsync(@"@badges=staff/1,broadcaster/1,turbo/1;color=#008000;display-name=ronni;emotes=;id=db25007f-7a18-43eb-9379-80131e44d633;login=ronni;mod=0;msg-id=resub;msg-param-months=6;msg-param-sub-plan=Prime;msg-param-sub-plan-name=Prime;room-id=1337;subscriber=1;system-msg=ronni\shas\ssubscribed\sfor\s6\smonths!;tmi-sent-ts=1507246572675;turbo=1;user-id=1337;user-type=staff :tmi.twitch.tv USERNOTICE #dallas :Great stream -- keep it up!");
            await writer.WriteLineAsync(@"@badges=turbo/1;color=#9ACD32;display-name=TestChannel;emotes=;id=3d830f12-795c-447d-af3c-ea05e40fbddb;login=testchannel;mod=0;msg-id=raid;msg-param-displayName=TestChannel;msg-param-login=testchannel;msg-param-viewerCount=15;room-id=56379257;subscriber=0;system-msg=15\sraiders\sfrom\sTestChannel\shave\sjoined\n!;tmi-sent-ts=1507246572675;tmi-sent-ts=1507246572675;turbo=1;user-id=123456;user-type= :tmi.twitch.tv USERNOTICE #othertestchannel");
            await writer.WriteLineAsync(@"@badges=;color=;display-name=SevenTest1;emotes=30259:0-6;id=37feed0f-b9c7-4c3a-b475-21c6c6d21c3d;login=seventest1;mod=0;msg-id=ritual;msg-param-ritual-name=new_chatter;room-id=6316121;subscriber=0;system-msg=Seventoes\sis\snew\shere!;tmi-sent-ts=1508363903826;turbo=0;user-id=131260580;user-type= :tmi.twitch.tv USERNOTICE #seventoes :HeyGuys");
            await writer.WriteLineAsync(@"@color=#0D4200;display-name=ronni;emote-sets=0,33,50,237,793,2126,3517,4578,5569,9400,10337,12239;mod=1;subscriber=1;turbo=1;user-type=staff :tmi.twitch.tv USERSTATE #dallas");

            while (stream.CanRead && running)
            {
                var line = await reader.ReadLineAsync();

                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase))
                {
                    await writer.WriteLineAsync("ERROR :Goodbye");
                    break;
                }
            }

            writer.Close();
        }
    }
}
