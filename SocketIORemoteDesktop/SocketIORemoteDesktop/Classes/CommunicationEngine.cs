using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quobject.SocketIoClientDotNet.Client;

namespace SocketIORemoteDesktop
{
    public class CommunicationEngine:IDisposable
    {
        private Socket socket;
        //todo: ADD EVENTS FOR CREATE NEW SOCKET AND DESSTROID SOCKET
        //private static CommunicationEngine instance = new CommunicationEngine ();

        public string ID { get; private set; } = null;
        public string Password { get; private set; } = null;

        public event EventHandler OnInited;

        public CommunicationEngine()
        {
            //todo: Add timer for watchdog of socket
            Connect();
        }

        //public static CommunicationEngine Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //            instance = new CommunicationEngine();
        //        return instance;
        //    }
        //}

        public Socket GetSocket
        {
            get
            {
                //todo: Add socket Check;
                return socket;
            }
        }

        public bool Connect()
        {
            try
            {
                if (socket != null)
                {
                    socket.Close();
                }

                IO.Options opt = new IO.Options()
                {

                };
                socket = IO.Socket("http://static.85-10-248-60.clients.your-server.de:5000/",opt);
                socket.Connect();
                return true;
                //return InitRemote();
            } catch(Exception ex)
            {
                //todo :add log for exception
                return false;
            }

        }

        public bool InitRemote()
        {
            try
            {
                socket.Emit("Init", new AckImpl((response) =>
                {
                    dynamic pak = JsonConvert.DeserializeObject(response.ToString());
                    ID = pak.client_id;
                    Password = pak.password;

                    OnInited.Invoke(this, new EventArgs());
                })
                ,
                                $"{{\"client_id\":\"{ID}\",\"password\":\"{Password}\"}} ");

                return true;
            }
            catch (Exception ex)
            {
                //todo :add log for exception
                return false;
            }
        }

        public void Dispose()
        {
            socket.Disconnect();
        }
    }
}
