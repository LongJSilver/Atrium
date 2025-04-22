using System;
using System.Collections.Generic;
using System.IO;

namespace Atrium
{
    public partial class Cli
    {
        #region Fundamentals

        private bool IsStartOfLine() => this._cursorPos == 0;
        private bool IsEndOfLine() => this._cursorPos == this._cursorLimit;
        private bool IsStartOfBuffer() => this._console.CursorLeft == 0;
        private bool IsEndOfBuffer() => this._console.CursorLeft == this._console.BufferWidth - 1;


        //-----------------------------------------------------------------------------------------
        #region Writing to _text


        private void WriteLine() => this.NewLine();
        private void WriteLine(string StringToWrite, params object[] args)
        {
            this.WriteString(StringToWrite, args);
            this.NewLine();
        }
        private void WriteString(string StringToWrite, params object[] args)
        {
            //foreach (char character in str)
            //    WriteChar(character);

            if (String.IsNullOrEmpty(StringToWrite))
            {
                return;
            }

            if (args != null)
            {
                StringToWrite = String.Format(StringToWrite, args);
            }

            if (this.IsEndOfLine())
            {
                this._text.Append(StringToWrite);
                this._console.Write(StringToWrite);
                this._cursorPos += StringToWrite.Length;
            }
            else
            {
                int left = this._console.CursorLeft;
                int top = this._console.CursorTop;
                string existingTextOnTheRight = this._text.ToString(this._cursorPos, this._text.Length - this._cursorPos);
                this._text.Insert(this._cursorPos, StringToWrite);
                this._console.Write(StringToWrite + existingTextOnTheRight);
                this._console.SetCursorPosition(left, top);
                this.MoveCursorRight(StringToWrite.Length);
            }

        }

        TextWriter ICliFunctions.Writer => _console.Writer;

        private void WriteChar(char c)
        {
            if (this.IsEndOfLine())
            {
                this._text.Append(c);
                this._console.Write(c);
                this._cursorPos++;
            }
            else
            {
                int left = this._console.CursorLeft;
                int top = this._console.CursorTop;
                string str = this._text.ToString().Substring(this._cursorPos);
                this._text.Insert(this._cursorPos, c);
                this._console.Write(c.ToString() + str);
                this._console.SetCursorPosition(left, top);
                this.MoveCursorRight();
            }

        }

        private void Backspace()
        {
            if (this.IsStartOfLine())
            {
                return;
            }

            this.MoveCursorLeft();
            int index = this._cursorPos;
            this._text.Remove(index, 1);
            string replacement = this._text.ToString(index, this._text.Length - index);
            int left = this._console.CursorLeft;
            int top = this._console.CursorTop;
            this._console.Write(string.Format("{0} ", replacement));
            this._console.SetCursorPosition(left, top);

        }
        private void Delete()
        {
            if (this.IsEndOfLine())
            {
                return;
            }

            int index = this._cursorPos;
            this._text.Remove(index, 1);
            string replacement = this._text.ToString(index, this._text.Length - index);
            int left = this._console.CursorLeft;
            int top = this._console.CursorTop;
            this._console.Write(string.Format("{0} ", replacement));
            this._console.SetCursorPosition(left, top);

        }
        #endregion
        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Indietreggia il cursore di 1, e decrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary> 
        private void MoveCursorLeft()
        {
            if (this.IsStartOfLine())
            {
                return;
            }

            if (this.IsStartOfBuffer())
            {
                this._console.SetCursorPosition(this._console.BufferWidth - 1, this._console.CursorTop - 1);
            }
            else
            {
                this._console.SetCursorPosition(this._console.CursorLeft - 1, this._console.CursorTop);
            }

            this._cursorPos--;
        }

        /// <summary>
        /// Avanza il cursore di 1, e incrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary> 
        private void MoveCursorRight()
        {
            if (this.IsEndOfLine())
            {
                return;
            }

            if (this.IsEndOfBuffer())
            {
                this._console.SetCursorPosition(0, this._console.CursorTop + 1);
            }
            else
            {
                this._console.SetCursorPosition(this._console.CursorLeft + 1, this._console.CursorTop);
            }

            this._cursorPos++;
        }

        /// <summary>
        /// Avanza il cursore del numero passato come parametro, e incrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary>
        /// <param name="thisMuch"></param>
        public void MoveCursorRight(int thisMuch = 1)
        {
            if (thisMuch < 0)
            {
                return;
            }

            if (thisMuch > this._cursorLimit - this._cursorPos)
            {
                thisMuch = this._cursorLimit - this._cursorPos;
            }
            if (thisMuch == 0)
            {
                return;
            }

            this._cursorPos += thisMuch;

            int currentConsolePos = this.GlobalConsolePosition();
            int targetPos = currentConsolePos + thisMuch;

            int targetLeft = targetPos % this._console.BufferWidth;
            int targetTop = targetPos / this._console.BufferWidth;

            this._console.SetCursorPosition(targetLeft, targetTop);
        }

        /// <summary>
        /// Indietreggia il cursore del numero passato come parametro, e decrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary>
        /// <param name="thisMuch"></param>
        public void MoveCursorLeft(int thisMuch = 1)
        {
            if (thisMuch < 0)
            {
                return;
            }

            if (thisMuch > this._cursorPos)
            {
                thisMuch = this._cursorPos;
            }
            if (thisMuch == 0)
            {
                return;
            }

            this._cursorPos -= thisMuch;

            int currentConsolePos = this.GlobalConsolePosition();
            int targetPos = Math.Max(currentConsolePos - thisMuch, 0);

            int targetLeft = targetPos % this._console.BufferWidth;
            int targetTop = targetPos / this._console.BufferWidth;

            this._console.SetCursorPosition(targetLeft, targetTop);
        }

        private int GlobalConsolePosition()
        {
            return this._console.CursorTop * this._console.BufferWidth + this._console.CursorLeft;
        }


        #endregion

        #region CompositeActions


        /// <summary>
        /// Riporta il cursore all'inizio della linea.<para/>
        /// Questa funzione assume che <see cref="_text"/> sia allineato alla console.
        /// </summary>
        private void MoveCursorHome()
        {
            //while (!IsStartOfLine())
            //    MoveCursorLeft();
            this.MoveCursorLeft(this._cursorPos);
        }

        /// <summary>
        /// Porta il cursore alla fine della linea.<para/>
        /// Questa funzione assume che <see cref="_text"/> sia allineato alla console.
        /// </summary>
        private void MoveCursorEnd()
        {
            //while (!IsEndOfLine())
            //    MoveCursorRight();
            this.MoveCursorRight(this._cursorLimit - this._cursorPos);
        }

        private void NewLineIfNeeded()
        {
            if (this._cursorLimit != 0)
            {
                this.NewLine();
            }
        }
        private void NewLine()
        {
            this.MoveCursorEnd();
            int left = this._console.CursorLeft;
            int top = this._console.CursorTop;

            this._console.SetCursorPosition(0, top + 1);
            this.ResetTextBuffer();
        }


        private void WriteNewString(string str)
        {
            this.ClearLine();
            this.WriteString(str);
        }

        //private void ClearLine()
        //{
        //    ClearLine(false);
        //}

        /// <summary>
        /// <paramref name="alsoClearPrompt"/> assumes that the prompt can only be single-line. Which is realistic, piddiri.
        /// </summary>
        /// <param name="alsoClearPrompt"></param>
        private void ClearLine()
        {
            int charsToWrite = this._cursorLimit;
            this.MoveCursorHome();
            int top = this._console.CursorTop;
            int left = this._console.CursorLeft;
            this._console.Write(new string(' ', charsToWrite));
            this._console.SetCursorPosition(left, top);
            this.ResetTextBuffer();
        }

        #endregion

        #region Writing Actions

        private void PromptUser()
        {
            this._mode = InputMode.UserCommand;
            this.WritePrompt();
            this._acceptingInput = true;
        }
        private (int startLeft, int startTop, int endLeft, int endTop, int ConsoleW) _promptInfo;

        private void ClearPrompt()
        {
            int oldPosition = this._promptInfo.endLeft + this._cursorPos;

            for (int i = this._promptInfo.startTop; i < this._promptInfo.endTop; i++)
            {
                this._console.SetCursorPosition(0, i);

                this._console.Write(new string(' ', this._promptInfo.ConsoleW));
            }
            this._console.SetCursorPosition(0, this._promptInfo.endTop);
            this._console.Write(new string(' ', this._promptInfo.endLeft));

            //this._console.Write(this._text.ToString()); //riscriviamo il testo esistente.
            //MoveCursorHome();
            this.MoveCursorRight(oldPosition); //riportiamolo dov'era prima che iniziassimo
            this._console.SetCursorPosition(this._promptInfo.endLeft + this._cursorPos, this._promptInfo.endTop);
        }

        private void WritePrompt()
        {
            this._promptInfo.startLeft = this._console.CursorLeft;
            this._promptInfo.startTop = this._console.CursorTop;
            this._promptInfo.ConsoleW = this._console.BufferWidth;

            if (this.CommandMode != CliCommandMode.TypeOnly)
            {
                this.PrintNumberedCommandList();
            }
            this._console.Write(this.PromptString);

            this._promptInfo.endLeft = this._console.CursorLeft;
            this._promptInfo.endTop = this._console.CursorTop;

        }

        private bool _PromptNeedsRefresh = false;
        private void RefreshPrompt()
        {
            this._PromptNeedsRefresh = true;
        }
        private void InternalRefreshPrompt()
        {
            if (this._mode != InputMode.UserCommand || this._console.BufferWidth != this._promptInfo.ConsoleW)
            {
                return;
            }

            this._PromptNeedsRefresh = false;
            string backup = this._text.ToString();
            int oldPos = this._cursorPos;
            this.ClearLine();
            this.ClearPrompt();
            this._console.SetCursorPosition(this._promptInfo.startLeft, this._promptInfo.startTop);
            this.WritePrompt();
            this.WriteString(backup);
            this.MoveCursorHome();
            this.MoveCursorLeft(oldPos);
        }
        #endregion

        #region History

        private void PrevHistory()
        {
            if (this._historyIndex > 0)
            {
                this._historyIndex--;
                this.WriteNewString(this._history[this._historyIndex]);
            }
        }

        private void NextHistory()
        {
            if (this._historyIndex < this._history.Count)
            {
                this._historyIndex++;
                if (this._historyIndex == this._history.Count)
                {
                    this.ClearLine();
                }
                else
                {
                    this.WriteNewString(this._history[this._historyIndex]);
                }
            }
        }


        private void AddToHistory(String command)
        {
            if (_history.Count > 0 && _history[_history.Count - 1].Equals(command))
                return;
            this._history.Add(command);
            this._historyIndex = this._history.Count;
            if (this.ShouldPersistHistory)
            {
                this._historyStorage!.StoreList(this._history);
            }
        }

        private void ReadHistoryFromSecureStorage()
        {
            if (_historyStorage == null) throw new InvalidOperationException("There is no History storage service defined");
            this._history.Clear();
            IEnumerable<string> h = _historyStorage.ReadList();
            foreach (string item in h)
            {
                this._history.Add(item);
            }
            this._historyIndex = this._history.Count;
        }

        #endregion

        #region Autocomplete

        private void StartAutoComplete()
        {
            while (this._cursorPos > this._completionStart)
            {
                this.Backspace();
            }

            this._completionsIndex = 0;

            this.WriteString(this._completions![this._completionsIndex]);
        }

        private void NextAutoComplete()
        {
            while (this._cursorPos > this._completionStart)
            {
                this.Backspace();
            }

            this._completionsIndex++;

            if (this._completionsIndex == this._completions!.Length)
            {
                this._completionsIndex = 0;
            }

            this.WriteString(this._completions[this._completionsIndex]);
        }

        private void PreviousAutoComplete()
        {
            while (this._cursorPos > this._completionStart)
            {
                this.Backspace();
            }

            this._completionsIndex--;

            if (this._completionsIndex == -1)
            {
                this._completionsIndex = this._completions!.Length - 1;
            }

            this.WriteString(this._completions![this._completionsIndex]);
        }


        private void ResetAutoComplete()
        {
            this._completions = null;
            this._completionsIndex = 0;
        }


        #endregion

    }
}
