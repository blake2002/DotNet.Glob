﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet.Globbing
{
    public class GlobTokeniser
    {

        private StringBuilder _currentBufferText;

        public GlobTokeniser()
        {
            _currentBufferText = new StringBuilder();
        }

        public IList<IGlobToken> Tokenise(string globText)
        {
            var tokens = new List<IGlobToken>();

            using (var reader = new GlobStringReader(globText))
            {
                while (reader.ReadChar())
                {
                    if (reader.IsBeginningOfRangeOrList)
                    {
                        tokens.Add(ReadRangeOrListToken(reader));
                    }
                    else if (reader.IsSingleCharacterMatch)
                    {
                        tokens.Add(ReadSingleCharacterMatchToken());
                    }
                    else if (reader.IsWildcardCharacterMatch)
                    {
                        tokens.Add(ReadWildcardToken());
                    }
                    else if (reader.IsPathSeperator())
                    {
                        tokens.Add(ReadPathSeperatorToken());
                    }
                    else if (reader.IsValidLiteralCharacter())
                    {
                        // literal
                        tokens.Add(ReadLiteralToken(reader));
                    }
                }
            }

            _currentBufferText.Clear();

            return tokens;

        }

        private IGlobToken ReadLiteralToken(GlobStringReader reader)
        {
            AcceptCurrentChar(reader);
            while (!reader.HasReachedEnd)
            {
                if (GlobStringReader.IsValidLiteralCharacter(reader.PeekChar()))
                {
                    if (reader.ReadChar())
                    {
                        AcceptCurrentChar(reader);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            return new LiteralToken(GetBufferAndReset());
        }

        /// <summary>
        /// Parses a token for a range or list globbing expression.
        /// </summary>
        private IGlobToken ReadRangeOrListToken(GlobStringReader reader)
        {
            bool isNegated = false;
            bool isNumberRange = false;
            bool isLetterRange = false;
            bool isCharList = false;

            if (reader.PeekChar() == GlobStringReader.ExclamationMarkChar)
            {
                isNegated = true;
                reader.Read();
            }

            var nextChar = reader.PeekChar();
            if (Char.IsLetterOrDigit(nextChar))
            {
                reader.Read();
                nextChar = reader.PeekChar();
                if (nextChar == GlobStringReader.DashChar)
                {
                    if (Char.IsLetter(reader.CurrentChar))
                    {
                        isLetterRange = true;
                    }
                    else
                    {
                        isNumberRange = true;
                    }
                    //  throw new ArgumentOutOfRangeException("Range expressions must either be a letter range, i.e [a-z] or a number range i.e [0-9]");
                }
                else
                {
                    isCharList = true;
                }

                AcceptCurrentChar(reader);
            }
            else
            {
                isCharList = true;
                reader.Read();
                AcceptCurrentChar(reader);
            }

            if (isLetterRange || isNumberRange)
            {
                // skip over the dash char
                reader.ReadChar();
            }

            while (reader.ReadChar())
            {
                //  ReadCharacter(CharacterType.BracketedText, CurrentChar);
                if (reader.IsEndOfRangeOrList)
                {
                    var peekChar = reader.PeekChar();
                    // Close brackets within brackets are escaped with another
                    // Close bracket. e.g. [a]] matches a[
                    if (peekChar == GlobStringReader.CloseBracketChar)
                    {
                        AcceptCurrentChar(reader);
                        // Read();
                        //ReadCharacter(CharacterType.BracketedText, CurrentChar);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    AcceptCurrentChar(reader);
                }
            }

            // construct token
            IGlobToken result = null;
            var value = GetBufferAndReset();
            if (isCharList)
            {
                result = new CharacterListToken(value.ToCharArray());
            }
            else if (isLetterRange)
            {
                var start = value[0];
                var end = value[1];
                result = new LetterRangeToken() { IsNegated = isNegated, Start = start, End = end };
            }
            else if (isNumberRange)
            {
                var start = value[0]; // int.Parse(value[0].ToString());
                var end = value[1]; // int.Parse(value[1].ToString());
                result = new NumberRangeToken() { IsNegated = isNegated, Start = start, End = end };
            }

            return result;


        }

        private IGlobToken ReadPathSeperatorToken()
        {
            return new PathSeperatorToken();
        }

        private IGlobToken ReadWildcardToken()
        {
            return new WildcardToken();
        }

        private IGlobToken ReadSingleCharacterMatchToken()
        {
            // this.Read();
            return new SingleCharacterToken();
        }

        private void AcceptChar(char character)
        {
            _currentBufferText.Append(character);
        }

        private void AcceptCurrentChar(GlobStringReader reader)
        {
            _currentBufferText.Append(reader.CurrentChar);
        }

        private string GetBufferAndReset()
        {
            var text = _currentBufferText.ToString();
            _currentBufferText.Clear();
            return text;
        }

    }
}