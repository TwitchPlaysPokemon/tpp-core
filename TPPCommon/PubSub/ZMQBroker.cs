using NetMQ;
using NetMQ.Sockets;
using System;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// This broker acts as an intermediary for publishers and subscribers.
    /// This allows seemlessly adding new publishers and subscribers to the pub-sub network.
    /// 
    /// See https://netmq.readthedocs.io/en/latest/xpub-xsub/ for more information.
    /// </summary>
    public class ZMQBroker : IBroker
    {
        public void Run()
        {
            using (var xPubSocket = new XPublisherSocket(Addresses.BuildFullAddress(Addresses.TCPLocalHost, Addresses.PublisherPort)))
            using (var xSubSocket = new XSubscriberSocket(Addresses.BuildFullAddress(Addresses.TCPLocalHost, Addresses.SubscriberPort)))
            {
                Console.WriteLine("Intermediary broker started, and waiting for messages");

                // proxy messages between frontend / backend
                var proxy = new Proxy(xSubSocket, xPubSocket);

                // blocks indefinitely
                proxy.Start();
            }
        }
    }
}
