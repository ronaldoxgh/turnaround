using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public class TextPos
    {
        public int CharIndex;
        public int LineNo;
        public int ColNo;
        internal int _lineStart;

        public override string ToString()
        {
            return LineNo + ":" + ColNo;
        }
    }

    public class StringParser
    {
        public const string AlphaCharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";
        public const string DigitCharSet = "0123456789";
        public const string AlphaCharSetEx = AlphaCharSet + DigitCharSet;
        public const string SpaceCharSet = "\x0d\x0a\x20\x09\x1a";
        public const string HexCharSet = "0123456789abcdefABCDEF";

        private char[] _chars;
        private TextPos _pos = new TextPos();

        public StringParser(string sourceCode)
        {
            _chars = sourceCode.ToCharArray();
            _pos = new TextPos { CharIndex = 0, LineNo = 1, ColNo = 1 };
        }

        public string CurrentLine { get { return new string(_chars, _pos._lineStart, _pos.CharIndex - _pos._lineStart); } }

        public TextPos Pos
        {
            get
            {
                return new TextPos { CharIndex = _pos.CharIndex, ColNo = _pos.ColNo, LineNo = _pos.LineNo };
            }

            set
            {
                _pos.CharIndex = value.CharIndex;
                _pos.ColNo = value.ColNo;
                _pos.LineNo = value.LineNo;
            }
        }

        public bool Eof()
        {
            return !(_pos.CharIndex < _chars.Length);
        }

        public bool AnyChar(ref char charRead)
        {
            if (_pos.CharIndex < _chars.Length)
            {
                charRead = _chars[_pos.CharIndex++];
                switch ((int) charRead)
                {
                    case 13:
                        _pos._lineStart = _pos.CharIndex;
                        ++_pos.LineNo;
                        _pos.ColNo = 0;
                        break;
                    case 10:
                        _pos._lineStart = _pos.CharIndex;
                        break;
                    default:
                        ++_pos.ColNo;
                        break;
                }
                return true;
            }
            return false;
        }

        public bool ThisChar(string wantedCharSet, ref char charRead)
        {
            var priorPos = Pos;
            if (AnyChar(ref charRead))
            {
                if (wantedCharSet.IndexOf(charRead.ToString(), StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
                Pos = priorPos;
            }
            return false;
        }

        public bool ThisChar(string wantedCharSet)
        {
            char foo = '\x00';
            return ThisChar(wantedCharSet, ref foo);
        }

        public bool ThisChar(char wantedChar)
        {
            return ThisChar(wantedChar.ToString());
        }

        public bool Spaces()
        {
            int skipped = 0;
            while (ThisChar(SpaceCharSet))
                ++skipped;
            return skipped > 0;
        }

        public bool EndOfLine()
        {
            var ocurrs = 0;
            if (ThisChar('\x0d'))
                ++ocurrs;
            if (ThisChar('\x0a'))
                ++ocurrs;
            if (ThisChar('\x1a'))
                ++ocurrs;
            return ocurrs > 0 || Eof();
        }

        public virtual bool Comments()
        {
            return false;
        }

        public bool Skip()
        {
            int loops = 0;
            while (Spaces() || Comments())
                ++loops;
            return loops > 0;
        }

        public bool ThisStr(string wantedCharSet, ref string strRead)
        {
            var tempStr = new StringBuilder();
            char charRead = '\x00';
            while (ThisChar(wantedCharSet, ref charRead))
                tempStr.Append(charRead);
            if (tempStr.Length > 0)
            {
                strRead = tempStr.ToString();
                return true;
            }
            return false;
        }

        public string LastWordRead = "";

        public bool AnyWord(ref string wordRead)
        {
            Skip();
            char charRead = '\x00';
            if (ThisChar(AlphaCharSet, ref charRead))
            {
                var tempStr = new StringBuilder();
                tempStr.Append(charRead);
                while (ThisChar(AlphaCharSetEx, ref charRead))
                    tempStr.Append(charRead);
                wordRead = tempStr.ToString();
                LastWordRead = wordRead;
                return true;
            }
            return false;
        }

        public bool ThisWord(string wantedWord, ref string wordRead)
        {
            var priorPos = Pos;
            if (AnyWord(ref wordRead))
            {
                if (wordRead.Equals(wantedWord, StringComparison.OrdinalIgnoreCase))
                    return true;
                Pos = priorPos;
            }
            return false;
        }

        public bool ThisWord(string wantedWord)
        {
            string foo = null;
            return ThisWord(wantedWord, ref foo);
        }

        public bool ThisWord(string[] wantedWordSet, ref int readIndex)
        {
            var priorPos = Pos;
            string wordRead = null;
            if (AnyWord(ref wordRead))
            {
                for (int i = 0; i < wantedWordSet.Length; i++)
                    if (wantedWordSet[i].Equals(wordRead, StringComparison.OrdinalIgnoreCase))
                    {
                        readIndex = i;
                        return true;
                    }
                Pos = priorPos;
            }
            return false;
        }

        public bool ThisWord(string[] wantedWordSet)
        {
            int foo = -1;
            return ThisWord(wantedWordSet, ref foo);
        }

        public bool ThisText(string wantedText)
        {
            Skip();
            var priorPos = Pos;
            var wantedChars = wantedText.ToCharArray();
            for (int i = 0; i < wantedChars.Length; i++)
                if (!ThisChar(wantedChars[i]))
                {
                    Pos = priorPos;
                    return false;
                }
            return true;
        }

        public bool ThisText(string[] wantedTextSet, ref string textRead)
        {
            foreach (var text in wantedTextSet)
                if (ThisText(text))
                {
                    textRead = text;
                    return true;
                }
            return false;
        }

        public bool ThisText(string[] wantedTextSet, ref int textIndex)
        {
            for (int i = 0; i < wantedTextSet.Length; i++)
                if (ThisText(wantedTextSet[i]))
                {
                    textIndex = i;
                    return true;
                }
            return false;
        }
    }
}
