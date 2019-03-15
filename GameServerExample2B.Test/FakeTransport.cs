using System;
using System.Net;
using System.Collections.Generic;

namespace GameServerExample2B.Test
{
    public struct FakeData
    {
        public FakeEndPoint endPoint;
        public byte[] data;
        public bool needAck;
    }

    public class FakeQueueEmpty : Exception
    {
    }

    public class FakeTransport : IGameTransport
    {
        private FakeEndPoint boundAddress;

        private Queue<FakeData> recvQueue;
        private Queue<FakeData> sendQueue;

        public FakeTransport()
        {
            recvQueue = new Queue<FakeData>();
            sendQueue = new Queue<FakeData>();
        }

        public void ClientEnqueue(FakeData data)
        {
            recvQueue.Enqueue(data);
        }

        public void ClientEnqueue(Packet packet, string address, int port)
        {
            recvQueue.Enqueue(new FakeData() { data = packet.GetData(), endPoint = new FakeEndPoint(address, port), needAck = packet.NeedAck});
        }

        public FakeData ClientDequeue()
        {
            if (sendQueue.Count <= 0)
                throw new FakeQueueEmpty();
            return sendQueue.Dequeue();
        }

        /// <summary>
        /// prendo il primo pacchetto del comando che voglio con la destinazione che voglio; lo rimuovo dalla coda
        /// </summary>
        /// <param name="command"></param>
        /// <param name="destination"></param>
        /// <returns> ritorna il primo pacchetto del comando e destinazione dati in input</returns>
        public FakeData GetDequeueSendingClientFakeData(byte command, FakeEndPoint destination)
        {
            //List<FakeData> packetsIWant = new List<FakeData>();
            FakeData packetsIWant = new FakeData();

            for (int i = 0; i < sendQueue.Count; i++)
            {
                //devo prendere i pacchetti con il comando che do in input e destinazione che dico io
                FakeData pack = sendQueue.Dequeue();
                //se il pacchetto non ha la destinazione che dico io lo rimetto dentro
                if (!pack.endPoint.Equals(destination))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }

                //se il pacchetto 
                byte comm = pack.data[0];
                //byte id = BitConverter.to(pack.data, 0);
                if (!(comm == command))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }
                //packetsIWant.Add(pack);
                packetsIWant = pack;
                break;
            }
            return packetsIWant;
        }

        public List<FakeData> GetDequeueSendingClientFakeDatas(byte command, FakeEndPoint destination)
        {
            List<FakeData> packetsIWant = new List<FakeData>();

            for (int i = 0; i < sendQueue.Count; i++)
            {
                //devo prendere i pacchetti con il comando che do in input e destinazione che dico io
                FakeData pack = sendQueue.Dequeue();
                //se il pacchetto non ha la destinazione che dico io lo rimetto dentro
                if (!pack.endPoint.Equals(destination))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }

                //se il pacchetto 
                byte comm = pack.data[0];
                //byte id = BitConverter.to(pack.data, 0);
                if (!(comm == command))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }
                packetsIWant.Add(pack);
            }
            return packetsIWant;
        }

        /// <summary>
        /// prendo il conto dei pacchetti che devo inviare all'indirizzo dato in input e che sono del comando dato sempre in input;
        /// non li tolgo dalla coda
        /// </summary>
        /// <param name="command">comando del pacchetto che devo cercare</param>
        /// <param name="destination">indirizzo di destinazione a cui il pacchetto deve arrivare</param>
        /// <returns></returns>
        public uint GetCountOfSelectedData(byte command, FakeEndPoint destination)
        {
            uint packetsIWant = 0;

            for (int i = 0; i < sendQueue.Count; i++)
            {
                //devo prendere i pacchetti con il comando che do in input e destinazione che dico io
                FakeData pack = sendQueue.Dequeue();
                //se il pacchetto non ha la destinazione che dico io lo rimetto dentro
                if (!pack.endPoint.Equals(destination))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }

                //se il pacchetto 
                byte comm = pack.data[0];
                //byte id = BitConverter.to(pack.data, 0);
                if (!(comm == command))
                {
                    sendQueue.Enqueue(pack);
                    continue;
                }
                packetsIWant++;
                sendQueue.Enqueue(pack);
            }
            return packetsIWant;
        }

        public void Bind(string address, int port)
        {
            boundAddress = new FakeEndPoint(address, port);
        }

        public EndPoint CreateEndPoint()
        {
            return new FakeEndPoint();
        }

        public byte[] Recv(int bufferSize, ref EndPoint sender)
        {
            FakeData fakeData = recvQueue.Dequeue();
            if (fakeData.data.Length > bufferSize)
                return null;
            sender = fakeData.endPoint;
            return fakeData.data;
        }

        public bool Send(byte[] data, EndPoint endPoint)
        {
            FakeData fakeData = new FakeData();
            fakeData.data = data;
            fakeData.endPoint = endPoint as FakeEndPoint;
            sendQueue.Enqueue(fakeData);
            return true;
        }

        public uint ClientQueueCount
        {
            get
            {
                return (uint)sendQueue.Count;
            }
        }
    }
}
