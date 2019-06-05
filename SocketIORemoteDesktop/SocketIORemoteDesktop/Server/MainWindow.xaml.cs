using Newtonsoft.Json;
using System;
using System.Threading;
using System.Windows;

namespace SocketIORemoteDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public RemoteEngine remoteEngine;

        public ManualResetEvent ManualResetEvent { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            remoteEngine = RemoteEngine.Instance;
            remoteEngine.ComEngine.OnInited += Instance_OnInited;
            remoteEngine.onEvent += RemoteEngine_onEvent;
            remoteEngine.ComEngine.InitRemote();
        }

        private void RemoteEngine_onEvent(object sender, EventHandlerRemoteEvent e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                Title = e.rawData;
            }));
        }

        private void Instance_OnInited(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtClientID.Text = remoteEngine.ComEngine.ID;
                txtPassword.Text = remoteEngine.ComEngine.Password;
            }));
        }

        private void cmdConnect_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("Starting TestSocketIOClient Example...");
            WndClientRemote wnd = new WndClientRemote(txtClientID.Text,txtPassword.Text);
            wnd.Show();
        }

        private object CreateUri()
        {
            throw new NotImplementedException();
        }

        private object CreateOptions()
        {
            throw new NotImplementedException();
        }

        private void cmdAddRequester_Click(object sender, RoutedEventArgs e)
        {
            
        }

        //private void SocketError(object sender, ErrorEventArgs e)
        //{
        //    Console.WriteLine(e.Message);
        //}

        //private void SocketConnectionClosed(object sender, EventArgs e)
        //{
        //    Console.WriteLine("Socket closed!");
        //}

        //private void SocketMessage(object sender, MessageEventArgs e)
        //{
        //    Console.WriteLine($"Message = {e.Message}");
        //}

        //private void SocketOpened(object sender, EventArgs e)
        //{
        //    Console.WriteLine("Socket open");
        //}
    }
    internal class Part
    {
        public Part()
        {
        }

        public string PartNumber { get; set; }
        public string Code { get; set; }
        public int Level { get; set; }
    }
    [Serializable()]
    public class ScreenFrame
    {
        [JsonProperty]
        public string ID { set; get; }
        [JsonProperty]
        public string Data { set; get; }
        [JsonProperty]

        public int Size { set; get; } = 4;

        [JsonProperty]
        public int ScreenWidth { set; get; }
        [JsonProperty]
        public int ScreenHeight { set; get; }

        public ScreenFrame(int size)
        {
            Data = "";// new byte[size];
        }

        //private static ImageCodecInfo GetEncoder(ImageFormat format)
        //{
        //    var codecs = ImageCodecInfo.GetImageDecoders();
        //    foreach (var codec in codecs)
        //    {
        //        if (codec.FormatID == format.Guid)
        //        {
        //            return codec;
        //        }
        //    }

        //    return null;
        //}

        //public static byte[] ImageToByte(System.Drawing.Image image)

        //{


        //    MemoryStream ms = new MemoryStream();

        //    var qualityEncoder = Encoder.Quality;
        //    var encoderParameters = new EncoderParameters(1);
        //    encoderParameters.Param[0] = new EncoderParameter(qualityEncoder, 25L);

        //    image.Save(ms, GetEncoder(ImageFormat.Jpeg), encoderParameters);

        //    byte[] array = ms.ToArray();

        //    return array;

        //}
        public static string ByteToString(byte[] image)

        {
            return Convert.ToBase64String(image);

        }

        public static byte[] StringToBytes(string imageString)

        {

            if (imageString == null)

                throw new ArgumentNullException("imageString");

            return Convert.FromBase64String(imageString);

            //System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(array));

            //return image;

        }
    }
}
