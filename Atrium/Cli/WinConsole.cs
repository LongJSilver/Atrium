using System;
using System.IO;

namespace Atrium
{
    internal class WinConsole : IConsole
    {
        private TextWriter _out;
        private TextWriter _err;

        public WinConsole()
        {
            this._out = Console.Out;
            this._err = Console.Error;
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }


        #region IConsole
        public CliKeyEvent Read()
        {

            ConsoleKeyInfo k = Console.ReadKey(true);
            CliKey key = (CliKey)k.Key;

            CliKeyEvent keyEvent = new CliKeyEvent(k.KeyChar, key,
                k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                k.Modifiers.HasFlag(ConsoleModifiers.Alt),
                k.Modifiers.HasFlag(ConsoleModifiers.Control)
                );
            return keyEvent;
        }

        public bool Available => Console.KeyAvailable;
        public int CursorLeft => Console.CursorLeft;

        public int CursorTop => Console.CursorTop;

        public int BufferWidth => Console.BufferWidth;

        public int BufferHeight => Console.BufferHeight;

        public TextWriter Writer => _out;

        public void SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);

        public void SetCursorPosition(int left, int top)
        {
            Console.SetCursorPosition(left, top);
        }

        public void Write(char value)
        {
            this._out.Write(value);
        }
        public void Write(string value)
        {
            this._out.Write(value);
        }

        public void WriteLine(string value) => this._out.WriteLine(value);



        #endregion
    }

}
