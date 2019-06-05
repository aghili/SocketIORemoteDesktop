using Newtonsoft.Json;
using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;

namespace SocketIORemoteDesktop
{
    public class RemoteEngine
    {
        private static RemoteEngine instance;
        public static RemoteEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new RemoteEngine();
                return instance;
            }
        }

        public event EventHandler<EventHandlerRemoteEvent> onEvent;

        public bool SendRemoteScreen { get; private set; } = false;

        public CommunicationEngine ComEngine;
        private RemoteEngine()
        {
            ComEngine = new CommunicationEngine();
            Init();
            StartThreads();
            StartInputDeviceSimulation();
        }

        private void StartInputDeviceSimulation()
        {
            ComEngine.GetSocket.On("REIn", (input) =>
            {
                var eventArg = new EventHandlerRemoteEvent(input.ToString());
                switch (eventArg.Type)
                {
                    case EventType.KeyBoard:
                        InputDeviceSimulation.SendKeyBoradKey((short)eventArg.Key);
                        break;
                    case EventType.MouseButton:
                        switch (eventArg.ButtonState)
                        {
                          
                            case MouseButtonState.Pressed:
                                InputDeviceSimulation.MouseDown(eventArg.ChangedButton,eventArg.Location);
                                break;
                            case MouseButtonState.Released:
                                InputDeviceSimulation.MouseUp(eventArg.ChangedButton, eventArg.Location);
                                break;
                        }
                        break;
                    case EventType.MouseMove:
                        //InputDeviceSimulation.MouseMove(eventArg.Location);
                        break;
                    case EventType.MouseScroll:
                        InputDeviceSimulation.MouseScroll(eventArg.Delta);
                        break;
                    default:
                        return;
                }
                onEvent?.BeginInvoke(this,eventArg, (e) => { }, null);
            });
        }

        private void StartImputDeviceWatchr()
        {
            Subscribe();
        }

        #region Mouse and Keyboard Hook

        //private IKeyboardMouseEvents m_GlobalHook;

        public void Subscribe()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            //m_GlobalHook = Hook.GlobalEvents();

            //m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
            //m_GlobalHook.KeyPress += GlobalHookKeyPress;
        }

        public void Unsubscribe()
        {
            //m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            //m_GlobalHook.KeyPress -= GlobalHookKeyPress;

            //It is recommened to dispose it
           // m_GlobalHook.Dispose();
        }
        #endregion


        private void StartThreads()
        {
            new Thread(new ThreadStart(() =>
            {
                CompressScreenCapture Csc = new CompressScreenCapture(WindowsAgent.Instance.GetScreenBound());
                int frameCount = 0;
                while (true)
                {
                    if (SendRemoteScreen)
                        try
                        {
                            if (Csc.Iterate())
                            {
                                var Screen = Csc.Frame;
                                Screen.ID = (frameCount++).ToString();
                                ComEngine.GetSocket.Emit("ScreenFrame",
                                    new AckImpl((response) =>
                                    {
                                        dynamic pak = JsonConvert.DeserializeObject(response.ToString());
                                    SendRemoteScreen = pak.HasRequester == "True";
                                })
                                    ,
                                    JsonConvert.SerializeObject(
                                        Screen
                                    //new
                                    //{
                                    //    ClientID = ComEngine.ID,
                                    //    Image = ScreenFrame.ImageToByte(WindowsAgent.Instance.CaptureScreen())
                                    //}
                                    ));
                            }
                            Thread.Sleep(500);
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(5000);
                        }
                    else
                        Thread.Sleep(5000);
                }
            }))
            { IsBackground = true }.Start();
        }

        private void Init()
        {
            //todo: add RequestScreen event on server side
            ComEngine.GetSocket.On("RequestScreen", () => {
                SendRemoteScreen = true;
            });

        }
    }

    public class EventHandlerRemoteEvent:EventArgs
    {
        public string rawData { set; get; }
        public EventType Type { set; get; } = EventType.None;
        public MouseButtons ChangedButton { get; private set; }
        public MouseButtonState ButtonState { get; private set; }
        public Point Location { get; private set; }
        public int Delta { get; private set; }
        public Key Key { get; private set; }
        public KeyStates KeyState { get; private set; }

        public EventHandlerRemoteEvent(string eventIn){
            rawData = eventIn;
            dynamic Event = JsonConvert.DeserializeObject(eventIn);
            string eType = Event.E;
            switch (eType)
            {
                case "MB":
                    Type = EventType.MouseButton;
                    ChangedButton = (MouseButtons)Convert.ToInt32(Event.B);
                    ButtonState = (MouseButtonState)Convert.ToInt32(Event.BS);
                    Location = new Point (Convert.ToInt32(Event.X), Convert.ToInt32(Event.Y));
                    break;
                case "MM":
                    Type = EventType.MouseMove;
                    Location = new Point(Convert.ToInt32(Event.X), Convert.ToInt32(Event.Y));
                    break;
                case "MS":
                    Type = EventType.MouseScroll;
                    Delta = Convert.ToInt32(Event.WSC);
                    break;
                case "K":
                    Type = EventType.KeyBoard;
                    Key = Event.K;
                    KeyState = (KeyStates)Convert.ToInt32(Event.KS);
                    break;
            }
        }
    }

    public enum EventType
    {
        None,
        KeyBoard,
        MouseButton,
        MouseMove,
        MouseScroll
    }
}
