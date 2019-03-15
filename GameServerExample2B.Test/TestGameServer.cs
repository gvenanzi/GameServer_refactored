using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private FakeTransport transport;
        private FakeClock clock;
        private GameServer server;

        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCount, Is.EqualTo(5));

            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            //qui prendo l'id di tester
            FakeData data = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));
            uint id = (uint)(BitConverter.ToUInt32(data.data, 5));

            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            //poi con l'id di tester devo fare l'update con foobar
            Packet move = new Packet(3, id, 10.0f, 20.0f, 30.0f);
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();
            
            Assert.That(server.GetObj(id).X, Is.Not.EqualTo(10.0f));
            Assert.That(server.GetObj(id).Y, Is.Not.EqualTo(20.0f));
            Assert.That(server.GetObj(id).Z, Is.Not.EqualTo(30.0f));
        }

        [Test]
        public void TestEvilUpdate2()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            //qui prendo l'id di tester
            FakeData data = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));
            uint id = (uint)(BitConverter.ToUInt32(data.data, 5));
            //predo la posizione prima che possa essere modificata
            float[] xyz = new float[3];
            xyz[0] = server.GetObj(id).X;
            xyz[1] = server.GetObj(id).Y;
            xyz[2] = server.GetObj(id).Z;

            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            //poi con l'id di tester devo fare l'update con foobar
            Packet move = new Packet(3, id, 10.0f, 20.0f, 30.0f);
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();

            Assert.That(server.GetObj(id).X, Is.EqualTo(xyz[0]));
            Assert.That(server.GetObj(id).Y, Is.EqualTo(xyz[1]));
            Assert.That(server.GetObj(id).Z, Is.EqualTo(xyz[2]));
        }

        [Test]
        public void TestEvilUpdate3()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            //qui prendo l'id di tester
            FakeData data = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));
            uint id = (uint)(BitConverter.ToUInt32(data.data, 5));
            //predo la posizione prima che possa essere modificata
            float[] xyz = new float[3];
            xyz[0] = server.GetObj(id).X;
            xyz[1] = server.GetObj(id).Y;
            xyz[2] = server.GetObj(id).Z;

            //poi con l'id di tester devo fare l'update con foobar
            Packet move = new Packet(3, id, 10.0f, 20.0f, 30.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            Assert.That(server.GetObj(id).X, Is.EqualTo(10.0f));
            Assert.That(server.GetObj(id).Y, Is.EqualTo(20.0f));
            Assert.That(server.GetObj(id).Z, Is.EqualTo(30.0f));
        }

        [Test]
        public void TestWelcomeAgain()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            //non ci saranno ancora pacchetti nella coda quindi stampa 0
            uint welcomeCount = transport.GetCountOfSelectedData(1, new FakeEndPoint("tester", 0));
            Console.WriteLine("il numero di pacchetti join nella coda è: " + welcomeCount);
            server.SingleStep();

            //prendo il conto di quanti pacchetti di welcome per tester ci sono
            //nella coda dovrebbe esserci un pacchetto
            welcomeCount = transport.GetCountOfSelectedData(1, new FakeEndPoint("tester", 0));
            Console.WriteLine("il numero di pacchetti join nella coda è: " + welcomeCount);
            //qui prendo il pacchetto di welcome
            FakeData welcome = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));

            //mando avanti il tempo invio un pacchetto inutile e prendo il prossimo welcome 
            clock.IncreaseTimeStamp(2);
            transport.ClientEnqueue(packet, "fabio", 5);
            server.SingleStep();
            //mando avanti un'altra volta il tempo, forse perchè prima il paccheto poteva essere messo in coda solo dopo un secondo 
            //quindi con il precedente step rifaccio mettere il pacchetto in coda e con il successivo glielo faccio rinviare
            clock.IncreaseTimeStamp(2);
            transport.ClientEnqueue(packet, "fabio", 5);
            server.SingleStep();

            //riprendo il conto dei pacchetti di welcome
            welcomeCount = transport.GetCountOfSelectedData(1, new FakeEndPoint("tester", 0));
            Console.WriteLine("il numero di pacchetti join nella coda è: " + welcomeCount);

            //riprendo il pacchetto di welcome (ce ne dovrebbe essere solo uno)
            FakeData welcome2 = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));


            //controllo se entrambi i pacchetti sono uguali
            Assert.That(welcome2.data, Is.EqualTo(welcome.data));
        }

        //SE TOLGO L'INCREASE TIMESTAMP IL PACCHETTO NON ME LO REINVIA MAI MENTRE ANCHE SE LO METTO E AUMENTO IL TEMPO DI 0 IL PACCHETTO ME LO RE INVIA
        //possibile che il problema siano i float?
        [Test]
        public void TestWelcomeAgain2()//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            //qui prendo il pacchetto di welcome
            FakeData welcome = transport.GetDequeueSendingClientFakeData(1, new FakeEndPoint("tester", 0));

            //faccio un altro step del server
            //clock.IncreaseTimeStamp(0.0f);
            transport.ClientEnqueue(packet, "fabio", 5);
            server.SingleStep();

            //clock.IncreaseTimeStamp(0.00f);
            //faccio un altro step del server anche questo senza mandare avanti il tempo
            transport.ClientEnqueue(packet, "fabio", 5);
            server.SingleStep();

            //controllo che non ci sia ancora nessun pacchetto perchè ancora non e passato un secondo 
            Assert.That(transport.GetCountOfSelectedData(1, new FakeEndPoint("tester", 0)), Is.EqualTo(0));
        }

       

        [Test]
        public void TestAckPacketClientCount()//invio l'ack al server
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            transport.ClientEnqueue(packet, "fabio", 5);
            server.SingleStep();

            //prendo il client dal dizionario
            GameClient client = server.GetClient(new FakeEndPoint("tester", 0));

            Assert.That(client.GetAckTableCount(), Is.EqualTo(2));
        }

        [Test]
        public void TestAckPacketClientCount2()//invio l'ack al server
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            //prendo il client dal dizionario
            GameClient client = server.GetClient(new FakeEndPoint("tester", 0));

            Assert.That(client.GetAckTableCount(), Is.EqualTo(1));
        }

        [Test]
        public void TestAckPacketClientCount3()//invio l'ack al server
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            //prendo il client dal dizionario
            GameClient client = server.GetClient(new FakeEndPoint("tester", 0));

            Assert.That(client.GetAckTableCount(), Is.EqualTo(1));
        }

        [Test]
        public void TestTimeIncrease()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            clock.IncreaseTimeStamp(4);
            //rijoino perchè se la coda è vuota salta tutto
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
           
            Assert.That(server.Now, Is.EqualTo(4));
        }

        [Test]
        public void TestDecreaseTime()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            Assert.That(() => clock.IncreaseTimeStamp(-4), Throws.InstanceOf<ExceptionDecreaseTime>());
        }

        [Test]
        public void TestTimeCanNotIncrease()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            clock.IncreaseTimeStamp(4);
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            clock.IncreaseTimeStamp(4);


            Assert.That(server.Now, Is.EqualTo(4));
        }

        [Test]
        public void TestReJoinMalus()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            //prendo il client dal dizionario
            GameClient client = server.GetClient(new FakeEndPoint("tester", 0));
            Assert.That(client.Malus, Is.GreaterThan(0));
        }

        [Test]
        public void TestJoinMalus()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            //prendo il client dal dizionario
            GameClient client = server.GetClient(new FakeEndPoint("tester", 0));
            Assert.That(client.Malus, Is.EqualTo(0));
        }
    }
}
