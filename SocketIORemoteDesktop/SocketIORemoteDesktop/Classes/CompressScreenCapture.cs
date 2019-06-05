using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIORemoteDesktop
{
    public class CompressScreenCapture
    {
        public PixelFormat Quality
        {
            get => _quality; set
            {
                switch (value)
                {
                    case PixelFormat.Format8bppIndexed: PixelDepth = 1; break;
                    case PixelFormat.Format16bppRgb555:
                        PixelDepth = 2; break;
                    case PixelFormat.Format32bppArgb:
                        PixelDepth = 4; break;
                }
                _quality = value;
            }
        }
        private int PixelDepth = 1;
        public CompressScreenCapture(Rectangle screenBoundsIn)
        {
            Quality = PixelFormat.Format16bppRgb555;
            // Initialize with black screen; get bounds from screen.
            this.screenBounds = screenBoundsIn;

            // Initialize 2 buffers - 1 for the current and 1 for the previous image
            prev = new Bitmap(screenBounds.Width, screenBounds.Height, Quality);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, Quality);

            // Clear the 'prev' buffer - this is the initial state
            //using (Graphics g = Graphics.FromImage(prev))
            //{
            //    g.Clear(Color.Black);
            //}

            // Compression buffer -- we don't really need this but I'm lazy today.
            compressionBuffer = new byte[screenBounds.Width * screenBounds.Height * PixelDepth];

            // Compressed buffer -- where the data goes that we'll send.
            int backbufSize = K4os.Compression.LZ4.LZ4Codec.MaximumOutputSize(this.compressionBuffer.Length);
            backbuf = new byte[backbufSize];
        }

        private Rectangle screenBounds;
        private Bitmap prev;
        private Bitmap cur;
        private byte[] compressionBuffer;

        private int backbufSize;
        private byte[] backbuf { get; set; }

        private int n = 0;
        private PixelFormat _quality = PixelFormat.Format8bppIndexed;

        private void Capture()
        {
            // Fill 'cur' with a screenshot

            cur = WindowsAgent.Instance.CaptureScreen();

            Bitmap clone = cur.Clone(new Rectangle (0,0,cur.Width,cur.Height),Quality);

            //using (Graphics gr = Graphics.FromImage(clone))
            //{
            //    gr.DrawImage(cur, new Rectangle(0, 0, clone.Width, clone.Height));
            //}

            cur = clone;
            //using (var gfxScreenshot = Graphics.FromImage(cur))
            //{
            //    gfxScreenshot.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size, CopyPixelOperation.SourceCopy);
            //}
        }

        private unsafe bool ApplyXor(BitmapData previous, BitmapData current)
        {
            bool hasdata = false;
            byte* prev0 = (byte*)previous.Scan0.ToPointer();
            byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = previous.Height;
            int width = previous.Width;
            int halfwidth = width / 2;

            fixed (byte* target = this.compressionBuffer)
                switch (Quality)
                {
                    case PixelFormat.Format32bppArgb:
                        {
                            ulong* dst = (ulong*)target;

                            for (int y = 0; y < height; ++y)
                            {
                                ulong* prevRow = (ulong*)(prev0 + previous.Stride * y);
                                ulong* curRow = (ulong*)(cur0 + current.Stride * y);

                                for (int x = 0; x < halfwidth; ++x)
                                {
                                    ulong tmp = curRow[x] ^ prevRow[x];
                                    *(dst++) = tmp;

                                    hasdata |= (tmp != 0);
                                }
                            }
                        }
                        break;
                    case PixelFormat.Format8bppIndexed:
                        {
                            UInt16* dst = (UInt16*)target;

                            for (int y = 0; y < height; ++y)
                            {
                                UInt16* prevRow = (UInt16*)(prev0 + previous.Stride * y);
                                UInt16* curRow = (UInt16*)(cur0 + current.Stride * y);

                                for (int x = 0; x < halfwidth; ++x)
                                {
                                    UInt16 tmp = (UInt16)(curRow[x] ^ prevRow[x]);
                                    *(dst++) = tmp;

                                    hasdata |= (tmp != 0);
                                }
                            }
                        }
                        break;
                    case PixelFormat.Format16bppRgb555:
                        {
                            UInt32* dst = (UInt32*)target;

                            for (int y = 0; y < height; ++y)
                            {
                                UInt32* prevRow = (UInt32*)(prev0 + previous.Stride * y);
                                UInt32* curRow = (UInt32*)(cur0 + current.Stride * y);

                                for (int x = 0; x < halfwidth; ++x)
                                {
                                    UInt32 tmp = (UInt32)(curRow[x] ^ prevRow[x]);
                                    *(dst++) = tmp;

                                    hasdata |= (tmp != 0);
                                }
                            }
                        }
                        break;
                }
            return hasdata;
        }

        private int Compress()
        {
            // Grab the backbuf in an attempt to update it with new data
            var backbuf = this.backbuf;

            backbufSize = K4os.Compression.LZ4.LZ4Codec.Encode(
                this.compressionBuffer, 0, this.compressionBuffer.Length,
                backbuf, 0, backbuf.Length);

            //Buffer.BlockCopy(BitConverter.GetBytes(backbuf.Size), 0, backbuf.Data, 0, 4);

            return backbufSize;
        }

        public bool Iterate()
        {
            bool hasdata = false;
            Stopwatch sw = Stopwatch.StartNew();

            // Capture a screen:
            Capture();

            TimeSpan timeToCapture = sw.Elapsed;

            // Lock both images:
            var locked1 = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height),
                                       ImageLockMode.ReadWrite, Quality);
            var locked2 = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height),
                                        ImageLockMode.ReadWrite, Quality);
            try
            {
                // Xor screen:
                hasdata = ApplyXor(locked2, locked1);
                if (!hasdata)
                    return false;
                TimeSpan timeToXor = sw.Elapsed;

                // Compress screen:
                int length = Compress();

                TimeSpan timeToCompress = sw.Elapsed;

                if ((++n) % 50 == 0)
                {
                    Debug.WriteLine("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s " +
                                  "{3} Kb => {4:0.0} FPS     \r",
                        timeToCapture.TotalSeconds, timeToXor.TotalSeconds,
                        timeToCompress.TotalSeconds, length / 1024,
                        1.0 / sw.Elapsed.TotalSeconds);
                }

                // Swap buffers:
                //var tmp = cur;
                //cur = prev;

                //prev = cur;
            }
            finally
            {
                cur.UnlockBits(locked1);
                prev.UnlockBits(locked2);
            }
            if (!hasdata)
                return false;
            prev = (Bitmap)cur.Clone();
            return true;
        }

        public ScreenFrame Frame { get
            {
                var buffer = new byte[backbufSize];
                Buffer.BlockCopy(backbuf, 0, buffer, 0, backbufSize);
                var result = new ScreenFrame(backbufSize)
                {
                    Data = ScreenFrame.ByteToString(buffer),
                    ScreenHeight = cur.Height,
                    ScreenWidth = cur.Width
                };
                return result;
            }
        }
    }
    public class DecompressScreenCapture
    {
        public PixelFormat Quality
        {
            get => _quality; set
            {
                switch (value)
                {
                    case PixelFormat.Format8bppIndexed: PixelDepth = 1; break;
                    case PixelFormat.Format16bppRgb555:
                        PixelDepth = 2; break;
                    case PixelFormat.Format32bppArgb:
                        PixelDepth = 4; break;
                }
                _quality = value;
            }
        }
        private int PixelDepth = 1;
        public DecompressScreenCapture()
        {
            Quality  = PixelFormat.Format16bppRgb555;
        }

        private void init(ScreenFrame backbuf)
        {
            // Initialize with black screen; get bounds from screen.
            this.screenBounds = new Rectangle(0, 0, backbuf.ScreenWidth, backbuf.ScreenHeight); ;

            // Initialize 2 buffers - 1 for the current and 1 for the previous image
            //prev = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            cur = new Bitmap(screenBounds.Width, screenBounds.Height, Quality);

            // Clear the 'prev' buffer - this is the initial state
            //using (Graphics g = Graphics.FromImage(cur))
            //{
            //    g.Clear(Color.Black);
            //}

            // Compression buffer -- we don't really need this but I'm lazy today.
            compressionBuffer = new byte[screenBounds.Width * screenBounds.Height * PixelDepth];
        }

        private Rectangle screenBounds;
        private Bitmap cur;
        private byte[] compressionBuffer;

        private int backbufSize;
        //public ScreenFrame backbuf { get; private set; }

        private int n = 0;
        private PixelFormat _quality = PixelFormat.Format8bppIndexed;

        private unsafe void ApplyXor(BitmapData current, byte[] masketBuffer)
        {
            //byte* prev0 = masketBuffer;
            byte* cur0 = (byte*)current.Scan0.ToPointer();

            int height = current.Height;
            int width = current.Width;
            int halfwidth = width / 2;

            fixed (byte* masked = masketBuffer)
                switch (Quality)
                {
                    case PixelFormat.Format32bppArgb:
                        {
                            ulong* maskedLong = (ulong*)masked;

                            for (int y = 0; y < height; ++y)
                            {
                                //ulong* masketRow = (ulong*)(prev0 + previous.Stride * y);
                                ulong* curRow = (ulong*)(cur0 + current.Stride * y);

                                for (int x = 0; x < halfwidth; ++x)
                                {
                                    curRow[x] = curRow[x] ^ *(maskedLong++);
                                    //curRow[x] = *(maskedLong++);
                                }
                            }
                        }
                        break;
                    case PixelFormat.Format8bppIndexed:
                        UInt16* maskedByte = (UInt16*)masked;

                        for (int y = 0; y < height; ++y)
                        {
                            //ulong* masketRow = (ulong*)(prev0 + previous.Stride * y);
                            UInt16* curRow = (UInt16*)(cur0 + current.Stride * y);

                            for (int x = 0; x < halfwidth; ++x)
                            {
                                curRow[x] = (UInt16)(curRow[x] ^ *(maskedByte++));
                                //curRow[x] = *(maskedLong++);
                            }
                        }
                        break;
                    case PixelFormat.Format16bppRgb555:
                        UInt32* maskedint = (UInt32*)masked;

                        for (int y = 0; y < height; ++y)
                        {
                            //ulong* masketRow = (ulong*)(prev0 + previous.Stride * y);
                            UInt32* curRow = (UInt32*)(cur0 + current.Stride * y);

                            for (int x = 0; x < halfwidth; ++x)
                            {
                                curRow[x] = (UInt32)(curRow[x] ^ *(maskedint++));
                                //curRow[x] = *(maskedLong++);
                            }
                        }
                        break;
                }
        }

        private int Decompress(ScreenFrame backbuf)
        {
            byte[] buffer = ScreenFrame.StringToBytes(backbuf.Data);
            backbuf.Size = K4os.Compression.LZ4.LZ4Codec.Decode(
                buffer, 0, buffer.Length,
                 this.compressionBuffer, 0, this.compressionBuffer.Length);

            //Buffer.BlockCopy(BitConverter.GetBytes(backbuf.Size), 0, backbuf.Data, 0, 4);

            return backbuf.Size;
        }

        public void Iterate(ScreenFrame backbuf)
        {

            if (screenBounds == Rectangle.Empty)
            {
                //todo: add change screen size capability to code
                init(backbuf);
            }
            lock (compressionBuffer)
            {
                int frameCount = 0;

                Stopwatch sw = Stopwatch.StartNew();

                // Capture a screen:
                //Capture();

                TimeSpan timeToCapture = sw.Elapsed;

                // Lock both images:
                var locked1 = cur.LockBits(new Rectangle(0, 0, cur.Width, cur.Height),
                                           ImageLockMode.ReadWrite, Quality);
                //var locked2 = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height),
                //                            ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {

                    // Compress screen:
                    int length = Decompress(backbuf);


                    // Xor screen:
                    ApplyXor(locked1, compressionBuffer);

                    TimeSpan timeToXor = sw.Elapsed;

                    TimeSpan timeToCompress = sw.Elapsed;

                    if ((++n) % 50 == 0)
                    {
                        Console.Write("Iteration: {0:0.00}s, {1:0.00}s, {2:0.00}s " +
                                      "{3} Kb => {4:0.0} FPS     \r",
                            timeToCapture.TotalSeconds, timeToXor.TotalSeconds,
                            timeToCompress.TotalSeconds, length / 1024,
                            1.0 / sw.Elapsed.TotalSeconds);
                    }

                    // Swap buffers:
                    //var tmp = cur;
                    //cur = prev;
                    //prev = tmp;
                }
                finally
                {
                    cur.UnlockBits(locked1);
                    //prev.UnlockBits(locked2);
                }
            }
        }

        public Bitmap Screen { get { return cur; } }
    }
}
