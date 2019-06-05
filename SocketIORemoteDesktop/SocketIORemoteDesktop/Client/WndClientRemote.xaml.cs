using Newtonsoft.Json;
using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SocketIORemoteDesktop
{
    /// <summary>
    /// Interaction logic for WndClientRemote.xaml
    /// </summary>
    public partial class WndClientRemote : Window
    {
        private string ClientID;
        private string ClientPassword;
        //private BitmapImage Image;
        private MemoryStream memoryBmp = new MemoryStream();
        private MemoryStream memory = new MemoryStream();
        private DecompressScreenCapture Decomprsser;
        private object LockObject = new object();
        private CommunicationEngine ComEngine;

        public WndClientRemote(string ClientIDIn,string PasswordIn)
        {
            InitializeComponent();
            ClientID = ClientIDIn;
            ClientPassword = PasswordIn;
            ComEngine = new CommunicationEngine();
            //Image = new BitmapImage();
            //Image.StreamSource = memory;
            //ImgScreen.Source = Image;
        }
        private bool Refreshed = false; 
        public void ShowScreen(ScreenFrame screen)
        {
            lock (Decomprsser)
            {
                Decomprsser.Iterate(screen);
                if (!Refreshed)
                {
                    Refreshed = true;
                    ImgScreen.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        lock (LockObject)
                        {
                            lock (Decomprsser)
                                BitmapToImageSource(Decomprsser.Screen);
                        }
                        Refreshed = false;
                    }));
                }
            }
        }
        void BitmapToImageSource(Bitmap bitmap)
        {
            //using (MemoryStream memory = new MemoryStream())
            {
                //byte[] data = Convert.FromBase64String(bitmap);
                //MemoryStream memoryBmp = new MemoryStream();
                //memoryBmp.SetLength(0);
                //memoryBmp.Write(bitmap, 0, bitmap.Length);
                //Bitmap bmp = new Bitmap(memoryBmp);
                //MemoryStream memory = new MemoryStream();
                //memory.SetLength(0);
                memory.Seek(0, SeekOrigin.Begin);
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.StreamSource.Seek(0, SeekOrigin.Begin);
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                ImgScreen.Source = bitmapimage;
            }
        }
        private int frameCount = -1;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComEngine.GetSocket.Emit("AddRequester",
                new AckImpl((response) =>
                {
                    //dynamic pak = JsonConvert.DeserializeObject(response.ToString());
                    //System.Drawing.Rectangle ScreenBounds =
                    //new System.Drawing.Rectangle(
                    //    pak.ScreenBounds.Left,
                    //    pak.ScreenBounds.Right,
                    //    pak.ScreenBounds.Width,
                    //    pak.ScreenBounds.Height
                    //    );
                    Decomprsser = new DecompressScreenCapture();
                    //SendRemoteScreen = pak.HasRequester == "true";
                })
                                ,
                JsonConvert.SerializeObject(
                new {
                    client_id = ClientID,
                    password = ClientPassword
                }));
            ComEngine.GetSocket.On("ScreenFrameIn", (input) =>
            {
                if (Decomprsser == null)
                    return;
                ScreenFrame screen = JsonConvert.DeserializeObject<ScreenFrame>(input.ToString());
                int id = Convert.ToInt32(screen.ID);
                //Dispatcher.BeginInvoke(new Action(() =>
                //{
                //    Title = screen.ID;
                //}));
                if (frameCount < id)
                {
                    frameCount = id; 
                    ShowScreen(screen);
                }
            });
        }


        //private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        //{
      
        //}

        //private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        //{

        //}

        private bool BoolMouseDown = false; 

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BoolMouseDown = true;
            SendMouseEvent(e);
        }

        private void SendMouseEvent(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            ComEngine.GetSocket.Emit("RE",
                                               JsonConvert.SerializeObject(
                                                   new
                                                   {
                                                       E = "MB",
                                                       B = e.ChangedButton,
                                                       BS = (int)e.ButtonState,
                                                       position.X,
                                                       position.Y,
                                                       WSC = 0
                                                   }
                                               ));
        }
        private void SendMouseEvent(MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            ComEngine.GetSocket.Emit("RE",
                                               JsonConvert.SerializeObject(
                                                   new
                                                   {
                                                       E = "MM",
                                                       position.X,
                                                       position.Y,
                                                   }
                                               ));
        }
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BoolMouseDown = false;
            SendMouseEvent(e);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SendMouseEvent(e);
        }

        private void SendMouseEvent(MouseWheelEventArgs e)
        {
            var position = e.GetPosition(this);
            ComEngine.GetSocket.Emit("RE",
                                               JsonConvert.SerializeObject(
                                                   new
                                                   {
                                                       E = "MS",
                                                       WSC = e.Delta
                                                   }
                                               ));
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (BoolMouseDown)
                SendMouseEvent(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SendKeyboardEvent(e);
        }

        private void SendKeyboardEvent(KeyEventArgs e)
        {
            ComEngine.GetSocket.Emit("RE",
                                                JsonConvert.SerializeObject(
                                                    new
                                                    {
                                                        E = "K",
                                                        K = e.Key,
                                                        KS=e.KeyStates
                                                    }
                                                ));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ComEngine.Dispose();
        }
    }
    }
