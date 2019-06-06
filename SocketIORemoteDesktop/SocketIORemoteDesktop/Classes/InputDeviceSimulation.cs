using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SocketIORemoteDesktop
{
    public class InputDeviceSimulation
    {
        private static InputDeviceSimulation instance;
        public static InputDeviceSimulation Instance
        {
            get
            {
                if (instance == null)
                    instance = new InputDeviceSimulation();
                return instance;
            }
        }

        private void Button_Click(object sender)
        {
            //Click();
            SendUnicode("Hello World !");
            SendKeyBoradKey((short)Keys.F1);
        }

        [DllImport("user32.dll")]
        private static extern UInt32 SendInput(UInt32 nInputs, ref INPUT pInputs, int cbSize);
        [DllImport("user32.dll", EntryPoint = "GetMessageExtraInfo", SetLastError = true)]
        private static extern IntPtr GetMessageExtraInfo();
        private enum InputType
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2,
        }
        [Flags()]
        private enum MOUSEEVENTF
        {
            MOVE = 0x0001,  //mouse move     
            LEFTDOWN = 0x0002,  //left button down     
            LEFTUP = 0x0004,  //left button up     
            RIGHTDOWN = 0x0008,  //right button down     
            RIGHTUP = 0x0010,  //right button up     
            MIDDLEDOWN = 0x0020, //middle button down     
            MIDDLEUP = 0x0040,  //middle button up     
            XDOWN = 0x0080,  //x button down     
            XUP = 0x0100,  //x button down     
            WHEEL = 0x0800,  //wheel button rolled     
            VIRTUALDESK = 0x4000,  //map to entire virtual desktop     
            ABSOLUTE = 0x8000,  //absolute move     
        }
        [Flags()]
        private enum KEYEVENTF
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            UNICODE = 0x0004,
            SCANCODE = 0x0008,
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public Int32 type;//0-MOUSEINPUT;1-KEYBDINPUT;2-HARDWAREINPUT     
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public Int32 mouseData;
            public Int32 dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public Int16 wVk;
            public Int16 wScan;
            public Int32 dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public Int32 uMsg;
            public Int16 wParamL;
            public Int16 wParamH;
        }
        ////Simulate mouse   
        //public static void Click()
        //{
        //    INPUT input_down = MouseDown();
        //    INPUT input_up = input_down;
        //    input_up.mi.dwFlags = (int)MOUSEEVENTF.LEFTUP;
        //    SendInput(1, ref input_up, Marshal.SizeOf(input_up));
        //}

        private static MOUSEEVENTF ConvertMouseButtonToMouseEventDown(MouseButton button)
        {
            MOUSEEVENTF dwflags = 0;
            switch (button)
            {
                case MouseButton.Left:
                    dwflags = MOUSEEVENTF.LEFTDOWN;
                    break;
                case MouseButton.Right:
                    dwflags = MOUSEEVENTF.RIGHTDOWN;
                    break;
                case MouseButton.Middle:
                    dwflags = MOUSEEVENTF.MIDDLEDOWN;
                    break;
                case MouseButton.XButton1:
                    dwflags = MOUSEEVENTF.XDOWN;
                    break;
                default:
                    return 0;
            }
            return dwflags;
        }
        private static MOUSEEVENTF ConvertMouseButtonToMouseEventUp(MouseButton button)
        {
            MOUSEEVENTF dwflags = 0;
            switch (button)
            {
                case MouseButton.Left:
                    dwflags = MOUSEEVENTF.LEFTUP;
                    break;
                case MouseButton.Right:
                    dwflags = MOUSEEVENTF.RIGHTUP;
                    break;
                case MouseButton.Middle:
                    dwflags = MOUSEEVENTF.MIDDLEUP;
                    break;
                case MouseButton.XButton1:
                    dwflags = MOUSEEVENTF.XUP;
                    break;
                default:
                    return 0;
            }
            return dwflags;
        }

        public static void MouseDown(MouseButton button,Point location)
        {
            MOUSEEVENTF dwflags = 0;
            dwflags = ConvertMouseButtonToMouseEventDown(button);
            if (dwflags == 0) return;

            INPUT input_mouse = new INPUT();
            input_mouse.type = (int)InputType.INPUT_MOUSE;
            input_mouse.mi.dx = location.X;
            input_mouse.mi.dy = location.Y;
            input_mouse.mi.mouseData = 0;
            input_mouse.mi.dwFlags = (int)dwflags;
            SendInput(1, ref input_mouse, Marshal.SizeOf(input_mouse));
        }

        public static void MouseUp(MouseButton button, Point location)
        {
            MOUSEEVENTF dwflags = 0;
            dwflags = ConvertMouseButtonToMouseEventUp(button);
            if (dwflags == 0) return;

            INPUT input_mouse = new INPUT();
            input_mouse.type = (int)InputType.INPUT_MOUSE;
            input_mouse.mi.dx = location.X;
            input_mouse.mi.dy = location.Y;
            input_mouse.mi.mouseData = 0;
            input_mouse.mi.dwFlags = (int)dwflags;
            SendInput(1, ref input_mouse, Marshal.SizeOf(input_mouse));
        }

        public static void MouseScroll(int value)
        {
            INPUT input_mouse = new INPUT();
            input_mouse.type = (int)InputType.INPUT_MOUSE;
            input_mouse.mi.dx = 0;
            input_mouse.mi.dy = 0;
            input_mouse.mi.mouseData = value;
            input_mouse.mi.dwFlags = (int)MOUSEEVENTF.WHEEL;
            SendInput(1, ref input_mouse, Marshal.SizeOf(input_mouse));
        }

        public static void MouseMove(Point location)
        {
            INPUT input_mouse = new INPUT();
            input_mouse.type = (int)InputType.INPUT_MOUSE;
            input_mouse.mi.dx = location.X;
            input_mouse.mi.dy = location.Y;
            input_mouse.mi.mouseData = 0;
            input_mouse.mi.dwFlags = (int)MOUSEEVENTF.MOVE;
            SendInput(1, ref input_mouse, Marshal.SizeOf(input_mouse));
        }

        //Simulate keystrokes  Send unicode characters to send any character
        public static void SendUnicode(string message)
        {
            for (int i = 0; i < message.Length; i++)
            {
                UnicodeKeyDown(message[i]);
                UnicodeKeyUp(message[i]);
            }
        }

        private static void UnicodeKeyUp(char character)
        {
            INPUT input_up = new INPUT();
            input_up.type = (int)InputType.INPUT_KEYBOARD;
            input_up.ki.wScan = (short)character;
            input_up.ki.wVk = 0;
            input_up.ki.dwFlags = (int)(KEYEVENTF.KEYUP | KEYEVENTF.UNICODE);
            SendInput(1, ref input_up, Marshal.SizeOf(input_up));//keyup      
        }

        private static void UnicodeKeyDown(char character)
        {
            INPUT input_down = new INPUT();
            input_down.type = (int)InputType.INPUT_KEYBOARD;
            input_down.ki.dwFlags = (int)KEYEVENTF.UNICODE;
            input_down.ki.wScan = (short)character;
            input_down.ki.wVk = 0;
            SendInput(1, ref input_down, Marshal.SizeOf(input_down));//keydown     
        }

        //Simulate keystrokes 
        public static void SendKeyBoradKey(short key)
        {
            INPUT input_down = new INPUT();
            input_down.type = (int)InputType.INPUT_KEYBOARD;
            input_down.ki.dwFlags = 0;
            input_down.ki.wVk = key;
            SendInput(1, ref input_down, Marshal.SizeOf(input_down));//keydown     

            INPUT input_up = new INPUT();
            input_up.type = (int)InputType.INPUT_KEYBOARD;
            input_up.ki.wVk = key;
            input_up.ki.dwFlags = (int)KEYEVENTF.KEYUP;
            SendInput(1, ref input_up, Marshal.SizeOf(input_up));//keyup      

        }
        //Send non-unicode characters, only send lowercase letters and numbers (发送非unicode字符，只能发送小写字母和数字)     
        public static void SendNoUnicode(string message)
        {
            string str = "abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < message.Length; i++)
            {
                short sendChar = 0;
                if (str.IndexOf(message[i].ToString().ToLower()) > -1)
                    sendChar = (short)GetKeysByChar(message[i]);
                else
                    sendChar = (short)message[i];
                INPUT input_down = new INPUT();
                input_down.type = (int)InputType.INPUT_KEYBOARD;
                input_down.ki.dwFlags = 0;
                input_down.ki.wVk = sendChar;
                SendInput(1, ref input_down, Marshal.SizeOf(input_down));//keydown     
                INPUT input_up = new INPUT();
                input_up.type = (int)InputType.INPUT_KEYBOARD;
                input_up.ki.wVk = sendChar;
                input_up.ki.dwFlags = (int)KEYEVENTF.KEYUP;
                SendInput(1, ref input_up, Marshal.SizeOf(input_up));//keyup      
            }
        }
        private static Keys GetKeysByChar(char c)
        {
            string str = "abcdefghijklmnopqrstuvwxyz";
            int index = str.IndexOf(c.ToString().ToLower());
            switch (index)
            {
                case 0:
                    return Keys.A;
                case 1:
                    return Keys.B;
                case 2:
                    return Keys.C;
                case 3:
                    return Keys.D;
                case 4:
                    return Keys.E;
                case 5:
                    return Keys.F;
                case 6:
                    return Keys.G;
                case 7:
                    return Keys.H;
                case 8:
                    return Keys.I;
                case 9:
                    return Keys.J;
                case 10:
                    return Keys.K;
                case 11:
                    return Keys.L;
                case 12:
                    return Keys.M;
                case 13:
                    return Keys.N;
                case 14:
                    return Keys.O;
                case 15:
                    return Keys.P;
                case 16:
                    return Keys.Q;
                case 17:
                    return Keys.R;
                case 18:
                    return Keys.S;
                case 19:
                    return Keys.T;
                case 20:
                    return Keys.U;
                case 21:
                    return Keys.V;
                case 22:
                    return Keys.W;
                case 23:
                    return Keys.X;
                case 24:
                    return Keys.Y;
                default:
                    return Keys.Z;
            }
        }
    }
}
