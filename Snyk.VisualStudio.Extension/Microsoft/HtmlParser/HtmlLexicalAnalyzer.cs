//---------------------------------------------------------------------------
// 
// File: HtmlLexicalAnalyzer.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Description: Lexical analyzer for Html-to-Xaml converter
//
//---------------------------------------------------------------------------
namespace Microsoft.HtmlConverter
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// lexical analyzer class
    /// recognizes tokens as groups of characters separated by arbitrary amounts of whitespace
    /// also classifies tokens according to type.
    /// </summary>
    internal class HtmlLexicalAnalyzer
    {
        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        /// <summary>
        /// Defines the _inputStringReader. String reader which will move over input text.
        /// </summary>
        private StringReader inputStringReader;

        // next character code read from input that is not yet part of any token
        // and the character it represents
        /// <summary>
        /// Defines the _nextCharacterCode.
        /// </summary>
        private int nextCharacterCode;

        /// <summary>
        /// Defines the _nextCharacter.
        /// </summary>
        private char nextCharacter;

        /// <summary>
        /// Defines the _lookAheadCharacterCode.
        /// </summary>
        private int lookAheadCharacterCode;

        /// <summary>
        /// Defines the _lookAheadCharacter.
        /// </summary>
        private char lookAheadCharacter;

        /// <summary>
        /// Defines the _previousCharacter.
        /// </summary>
        private char previousCharacter;

        /// <summary>
        /// Defines the _ignoreNextWhitespace.
        /// </summary>
        private bool ignoreNextWhitespace;

        /// <summary>
        /// Defines the _isNextCharacterEntity.
        /// </summary>
        private bool isNextCharacterEntity;

        /// <summary>
        /// Defines the _nextToken. Store token and type in local variables before copying them to output parameters.
        /// </summary>
        internal StringBuilder nextToken;

        /// <summary>
        /// Defines the _nextTokenType.
        /// </summary>
        internal HtmlTokenType nextTokenType;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlLexicalAnalyzer"/> class.
        /// </summary>
        /// <param name="inputTextString">The inputTextString<see cref="string"/>.</param>
        internal HtmlLexicalAnalyzer(string inputTextString)
        {
            this.inputStringReader = new StringReader(inputTextString);
            this.nextCharacterCode = 0;
            this.nextCharacter = ' ';
            this.lookAheadCharacterCode = this.inputStringReader.Read();
            this.lookAheadCharacter = (char)this.lookAheadCharacterCode;
            this.previousCharacter = ' ';
            this.ignoreNextWhitespace = true;
            this.nextToken = new StringBuilder(100);
            this.nextTokenType = HtmlTokenType.Text;

            // read the first character so we have some value for the NextCharacter property
            this.GetNextCharacter();
        }

        // ---------------------------------------------------------------------
        //
        // Internal Properties
        //
        // ---------------------------------------------------------------------

        /// <summary>
        /// Gets the NextTokenType.
        /// </summary>
        internal HtmlTokenType NextTokenType
        {
            get
            {
                return this.nextTokenType;
            }
        }

        /// <summary>
        /// Gets the NextToken.
        /// </summary>
        internal string NextToken
        {
            get
            {
                return this.nextToken.ToString();
            }
        }

        /// <summary>
        /// Gets the NextCharacter.
        /// </summary>
        private char NextCharacter
        {
            get
            {
                return this.nextCharacter;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsAtEndOfStream.
        /// </summary>
        private bool IsAtEndOfStream
        {
            get
            {
                return this.nextCharacterCode == -1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsAtTagStart.
        /// </summary>
        private bool IsAtTagStart
        {
            get
            {
                return this.nextCharacter == '<' && (this.lookAheadCharacter == '/' || this.IsGoodForNameStart(this.lookAheadCharacter)) && !this.isNextCharacterEntity;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsAtTagEnd.
        /// </summary>
        private bool IsAtTagEnd
        {
            // check if at end of empty tag or regular tag
            get
            {
                return (this.nextCharacter == '>' || (this.nextCharacter == '/' && this.lookAheadCharacter == '>')) && !this.isNextCharacterEntity;
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsAtDirectiveStart.
        /// </summary>
        private bool IsAtDirectiveStart
        {
            get
            {
                return (this.nextCharacter == '<' && this.lookAheadCharacter == '!' && !this.IsNextCharacterEntity);
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsNextCharacterEntity.
        /// </summary>
        private bool IsNextCharacterEntity
        {
            // check if next character is an entity
            get
            {
                return this.isNextCharacterEntity;
            }
        }

        /// <summary>
        /// Retrieves next recognizable token from input string and identifies its type.
        /// if no valid token is found, the output parameters are set to null
        /// if end of stream is reached without matching any token, token type
        /// paramter is set to EOF.
        /// </summary>
        internal void GetNextContentToken()
        {
            Debug.Assert(this.nextTokenType != HtmlTokenType.EOF);

            this.nextToken.Length = 0;

            if (this.IsAtEndOfStream)
            {
                this.nextTokenType = HtmlTokenType.EOF;
                return;
            }

            if (this.IsAtTagStart)
            {
                this.GetNextCharacter();

                if (this.NextCharacter == '/')
                {
                    this.nextToken.Append("</");
                    this.nextTokenType = HtmlTokenType.ClosingTagStart;

                    // advance
                    this.GetNextCharacter();

                    this.ignoreNextWhitespace = false; // Whitespaces after closing tags are significant
                }
                else
                {
                    this.nextTokenType = HtmlTokenType.OpeningTagStart;
                    this.nextToken.Append("<");
                    this.ignoreNextWhitespace = true; // Whitespaces after opening tags are insignificant
                }
            }
            else if (this.IsAtDirectiveStart)
            {
                // either a comment or CDATA
                this.GetNextCharacter();
                if (this.lookAheadCharacter == '[')
                {
                    // cdata
                    this.ReadDynamicContent();
                }
                else if (this.lookAheadCharacter == '-')
                {
                    this.ReadComment();
                }
                else
                {
                    // neither a comment nor cdata, should be something like DOCTYPE
                    // skip till the next tag ender
                    this.ReadUnknownDirective();
                }
            }
            else
            {
                // read text content, unless you encounter a tag
                this.nextTokenType = HtmlTokenType.Text;
                while (!this.IsAtTagStart && !this.IsAtEndOfStream && !this.IsAtDirectiveStart)
                {
                    if (this.NextCharacter == '<' && !this.IsNextCharacterEntity && this.lookAheadCharacter == '?')
                    {
                        // ignore processing directive
                        this.SkipProcessingDirective();
                    }
                    else
                    {
                        if (this.NextCharacter <= ' ')
                        {
                            // Respect xml:preserve or its equivalents for whitespace processing
                            if (this.ignoreNextWhitespace)
                            {
                                // Ignore repeated whitespaces
                            }
                            else
                            {
                                // Treat any control character sequence as one whitespace
                                this.nextToken.Append(' ');
                            }

                            this.ignoreNextWhitespace = true; // and keep ignoring the following whitespaces
                        }
                        else
                        {
                            this.nextToken.Append(this.NextCharacter);
                            this.ignoreNextWhitespace = false;
                        }

                        this.GetNextCharacter();
                    }
                }
            }
        }

        /// <summary>
        /// Unconditionally returns a token which is one of: TagEnd, EmptyTagEnd, Name, Atom or EndOfStream
        /// Does not guarantee token reader advancing.
        /// </summary>
        internal void GetNextTagToken()
        {
            this.nextToken.Length = 0;

            if (this.IsAtEndOfStream)
            {
                this.nextTokenType = HtmlTokenType.EOF;
                return;
            }

            this.SkipWhiteSpace();

            if (this.NextCharacter == '>' && !this.IsNextCharacterEntity)
            {
                // &gt; should not end a tag, so make sure it's not an entity
                this.nextTokenType = HtmlTokenType.TagEnd;
                this.nextToken.Append('>');
                this.GetNextCharacter();

                // Note: _ignoreNextWhitespace must be set appropriately on tag start processing
            }
            else if (this.NextCharacter == '/' && this.lookAheadCharacter == '>')
            {
                // could be start of closing of empty tag
                this.nextTokenType = HtmlTokenType.EmptyTagEnd;
                this.nextToken.Append("/>");
                this.GetNextCharacter();
                this.GetNextCharacter();
                this.ignoreNextWhitespace = false; // Whitespace after no-scope tags are sifnificant
            }
            else if (this.IsGoodForNameStart(this.NextCharacter))
            {
                this.nextTokenType = HtmlTokenType.Name;

                // starts a name
                // we allow character entities here
                // we do not throw exceptions here if end of stream is encountered
                // just stop and return whatever is in the token
                // if the parser is not expecting end of file after this it will call
                // the get next token function and throw an exception
                while (this.IsGoodForName(this.NextCharacter) && !this.IsAtEndOfStream)
                {
                    this.nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }
            }
            else
            {
                // Unexpected type of token for a tag. Reprot one character as Atom, expecting that HtmlParser will ignore it.
                this.nextTokenType = HtmlTokenType.Atom;
                this.nextToken.Append(this.NextCharacter);
                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// Unconditionally returns equal sign token. Even if there is no
        /// real equal sign in the stream, it behaves as if it were there.
        /// Does not guarantee token reader advancing.
        /// </summary>
        internal void GetNextEqualSignToken()
        {
            Debug.Assert(this.nextTokenType != HtmlTokenType.EOF);

            this.nextToken.Length = 0;

            this.nextToken.Append('=');
            this.nextTokenType = HtmlTokenType.EqualSign;

            this.SkipWhiteSpace();

            if (this.NextCharacter == '=')
            {
                // '=' is not in the list of entities, so no need to check for entities here
                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// Unconditionally returns an atomic value for an attribute
        /// Even if there is no appropriate token it returns Atom value
        /// Does not guarantee token reader advancing.
        /// </summary>
        internal void GetNextAtomToken()
        {
            Debug.Assert(nextTokenType != HtmlTokenType.EOF);

            this.nextToken.Length = 0;

            this.SkipWhiteSpace();

            this.nextTokenType = HtmlTokenType.Atom;

            if ((this.NextCharacter == '\'' || this.NextCharacter == '"') && !this.IsNextCharacterEntity)
            {
                char startingQuote = this.NextCharacter;
                this.GetNextCharacter();

                // Consume all characters between quotes
                while (!(this.NextCharacter == startingQuote && !this.IsNextCharacterEntity) && !this.IsAtEndOfStream)
                {
                    this.nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }

                if (this.NextCharacter == startingQuote)
                {
                    this.GetNextCharacter();
                }

                // complete the quoted value
                // NOTE: our recovery here is different from IE's
                // IE keeps reading until it finds a closing quote or end of file
                // if end of file, it treats current value as text
                // if it finds a closing quote at any point within the text, it eats everything between the quotes
                // TODO: Suggestion:
                // however, we could stop when we encounter end of file or an angle bracket of any kind
                // and assume there was a quote there
                // so the attribute value may be meaningless but it is never treated as text
            }
            else
            {
                while (!this.IsAtEndOfStream && !char.IsWhiteSpace(this.NextCharacter) && this.NextCharacter != '>')
                {
                    this.nextToken.Append(this.NextCharacter);

                    this.GetNextCharacter();
                }
            }
        }

        /// <summary>
        /// Advances a reading position by one character code
        /// and reads the next availbale character from a stream.
        /// This character becomes available as NextCharacter property.
        /// </summary>
        private void GetNextCharacter()
        {
            if (this.nextCharacterCode == -1)
            {
                throw new InvalidOperationException("GetNextCharacter method called at the end of a stream");
            }

            this.previousCharacter = this.nextCharacter;

            this.nextCharacter = this.lookAheadCharacter;
            this.nextCharacterCode = this.lookAheadCharacterCode;

            // next character not an entity as of now
            this.isNextCharacterEntity = false;

            this.ReadLookAheadCharacter();

            if (this.nextCharacter == '&')
            {
                if (this.lookAheadCharacter == '#')
                {
                    // numeric entity - parse digits - &#DDDDD;
                    int entityCode;
                    entityCode = 0;

                    this.ReadLookAheadCharacter();

                    // largest numeric entity is 7 characters
                    for (int i = 0; i < 7 && char.IsDigit(this.lookAheadCharacter); i++)
                    {
                        entityCode = 10 * entityCode + (this.lookAheadCharacterCode - (int)'0');

                        this.ReadLookAheadCharacter();
                    }

                    if (this.lookAheadCharacter == ';')
                    {
                        // correct format - advance
                        this.ReadLookAheadCharacter();

                        this.nextCharacterCode = entityCode;

                        // if this is out of range it will set the character to '?'
                        this.nextCharacter = (char)this.nextCharacterCode;

                        // as far as we are concerned, this is an entity
                        this.isNextCharacterEntity = true;
                    }
                    else
                    {
                        // not an entity, set next character to the current lookahread character
                        // we would have eaten up some digits
                        this.nextCharacter = this.lookAheadCharacter;
                        this.nextCharacterCode = this.lookAheadCharacterCode;
                        this.ReadLookAheadCharacter();

                        this.isNextCharacterEntity = false;
                    }
                }
                else if (char.IsLetter(this.lookAheadCharacter))
                {
                    // entity is written as a string
                    string entity = "";

                    // maximum length of string entities is 10 characters
                    for (int i = 0; i < 10 && (char.IsLetter(this.lookAheadCharacter) || char.IsDigit(this.lookAheadCharacter)); i++)
                    {
                        entity += this.lookAheadCharacter;

                        this.ReadLookAheadCharacter();
                    }

                    if (this.lookAheadCharacter == ';')
                    {
                        // advance
                        this.ReadLookAheadCharacter();

                        if (HtmlSchema.IsEntity(entity))
                        {
                            this.nextCharacter = HtmlSchema.EntityCharacterValue(entity);
                            this.nextCharacterCode = (int)this.nextCharacter;
                            this.isNextCharacterEntity = true;
                        }
                        else
                        {
                            // just skip the whole thing - invalid entity
                            // move on to the next character
                            this.nextCharacter = this.lookAheadCharacter;
                            this.nextCharacterCode = this.lookAheadCharacterCode;

                            this.ReadLookAheadCharacter();

                            // not an entity
                            this.isNextCharacterEntity = false;
                        }
                    }
                    else
                    {
                        // skip whatever we read after the ampersand
                        // set next character and move on
                        this.nextCharacter = this.lookAheadCharacter;

                        this.ReadLookAheadCharacter();

                        this.isNextCharacterEntity = false;
                    }
                }
            }
        }

        /// <summary>
        /// The ReadLookAheadCharacter.
        /// </summary>
        private void ReadLookAheadCharacter()
        {
            if (this.lookAheadCharacterCode != -1)
            {
                this.lookAheadCharacterCode = this.inputStringReader.Read();
                this.lookAheadCharacter = (char)this.lookAheadCharacterCode;
            }
        }

        /// <summary>
        /// skips whitespace in the input string
        /// leaves the first non-whitespace character available in the NextCharacter property
        /// this may be the end-of-file character, it performs no checking.
        /// </summary>
        private void SkipWhiteSpace()
        {
            // TODO: handle character entities while processing comments, cdata, and directives
            // TODO: SUGGESTION: we could check if lookahead and previous characters are entities also
            while (true)
            {
                if (this.nextCharacter == '<' && (this.lookAheadCharacter == '?' || this.lookAheadCharacter == '!'))
                {
                    this.GetNextCharacter();

                    if (this.lookAheadCharacter == '[')
                    {
                        // Skip CDATA block and DTDs(?)
                        while (!this.IsAtEndOfStream && !(this.previousCharacter == ']' && this.nextCharacter == ']' && this.lookAheadCharacter == '>'))
                        {
                            this.GetNextCharacter();
                        }

                        if (this.nextCharacter == '>')
                        {
                            this.GetNextCharacter();
                        }
                    }
                    else
                    {
                        // Skip processing instruction, comments
                        while (!this.IsAtEndOfStream && this.nextCharacter != '>')
                        {
                            this.GetNextCharacter();
                        }

                        if (this.nextCharacter == '>')
                        {
                            this.GetNextCharacter();
                        }
                    }
                }


                if (!char.IsWhiteSpace(this.NextCharacter))
                {
                    break;
                }

                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// checks if a character can be used to start a name
        /// if this check is true then the rest of the name can be read.
        /// </summary>
        /// <param name="character">The character<see cref="char"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool IsGoodForNameStart(char character)
        {
            return character == '_' || char.IsLetter(character);
        }

        /// <summary>
        /// checks if a character can be used as a non-starting character in a name
        /// uses the IsExtender and IsCombiningCharacter predicates to see
        /// if a character is an extender or a combining character.
        /// </summary>
        /// <param name="character">The character<see cref="char"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool IsGoodForName(char character)
        {
            // we are not concerned with escaped characters in names
            // we assume that character entities are allowed as part of a name
            return
                    this.IsGoodForNameStart(character) ||
                    character == '.' ||
                    character == '-' ||
                    character == ':' ||
                    char.IsDigit(character) ||
                    this.IsCombiningCharacter(character) ||
                    this.IsExtender(character);
        }

        /// <summary>
        /// identifies a character as being a combining character, permitted in a name
        /// TODO: only a placeholder for now but later to be replaced with comparisons against
        /// the list of combining characters in the XML documentation.
        /// </summary>
        /// <param name="character">The character<see cref="char"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool IsCombiningCharacter(char character)
        {
            // TODO: put actual code with checks against all combining characters here
            return false;
        }

        /// <summary>
        /// identifies a character as being an extender, permitted in a name
        /// TODO: only a placeholder for now but later to be replaced with comparisons against
        /// the list of extenders in the XML documentation.
        /// </summary>
        /// <param name="character">The character<see cref="char"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool IsExtender(char character)
        {
            // TODO: put actual code with checks against all extenders here
            return false;
        }

        /// <summary>
        /// skips dynamic content starting with '<![' and ending with ']>'.
        /// </summary>
        private void ReadDynamicContent()
        {
            // verify that we are at dynamic content, which may include CDATA
            Debug.Assert(this.previousCharacter == '<' && this.nextCharacter == '!' && this.lookAheadCharacter == '[');

            // Let's treat this as empty text
            this.nextTokenType = HtmlTokenType.Text;
            this.nextToken.Length = 0;

            // advance twice, once to get the lookahead character and then to reach the start of the cdata
            this.GetNextCharacter();
            this.GetNextCharacter();

            // NOTE: 10/12/2004: modified this function to check when called if's reading CDATA or something else
            // some directives may start with a <![ and then have some data and they will just end with a ]>
            // this function is modified to stop at the sequence ]> and not ]]>
            // this means that CDATA and anything else expressed in their own set of [] within the <! [...]>
            // directive cannot contain a ]> sequence. However it is doubtful that cdata could contain such
            // sequence anyway, it probably stops at the first ]
            while (!(this.nextCharacter == ']' && this.lookAheadCharacter == '>') && !this.IsAtEndOfStream)
            {
                // advance
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                // advance, first to the last >
                this.GetNextCharacter();

                // then advance past it to the next character after processing directive
                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// skips comments starting with '<!-' and ending with '-->'.
        /// NOTE: 10/06/2004: processing changed, will now skip anything starting with
        /// the "<!-"  sequence and ending in "!>" or "->", because in practice many html pages do not
        /// use the full comment specifying conventions.
        /// </summary>
        private void ReadComment()
        {
            // verify that we are at a comment
            Debug.Assert(this.previousCharacter == '<' && this.nextCharacter == '!' && this.lookAheadCharacter == '-');

            // Initialize a token
            this.nextTokenType = HtmlTokenType.Comment;
            this.nextToken.Length = 0;

            // advance to the next character, so that to be at the start of comment value
            this.GetNextCharacter(); // get first '-'
            this.GetNextCharacter(); // get second '-'
            this.GetNextCharacter(); // get first character of comment content

            while (true)
            {
                // Read text until end of comment
                // Note that in many actual html pages comments end with "!>" (while xml standard is "-->")
                while (!this.IsAtEndOfStream && !(this.nextCharacter == '-' && this.lookAheadCharacter == '-' || this.nextCharacter == '!' && this.lookAheadCharacter == '>'))
                {
                    this.nextToken.Append(this.NextCharacter);
                    this.GetNextCharacter();
                }

                // Finish comment reading
                this.GetNextCharacter();

                if (this.previousCharacter == '-' && this.nextCharacter == '-' && this.lookAheadCharacter == '>')
                {
                    // Standard comment end. Eat it and exit the loop
                    this.GetNextCharacter(); // get '>'
                    break;
                }
                else if (this.previousCharacter == '!' && this.nextCharacter == '>')
                {
                    // Nonstandard but possible comment end - '!>'. Exit the loop
                    break;
                }
                else
                {
                    // Not an end. Save character and continue continue reading
                    this.nextToken.Append(this.previousCharacter);
                    continue;
                }
            }

            // Read end of comment combination
            if (this.nextCharacter == '>')
            {
                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// skips past unknown directives that start with "<!" but are not comments or Cdata
        /// ignores content of such directives until the next ">" character
        /// applies to directives such as DOCTYPE, etc that we do not presently support.
        /// </summary>
        private void ReadUnknownDirective()
        {
            // verify that we are at an unknown directive
            Debug.Assert(this.previousCharacter == '<' && this.nextCharacter == '!' && !(this.lookAheadCharacter == '-' || this.lookAheadCharacter == '['));

            // Let's treat this as empty text
            this.nextTokenType = HtmlTokenType.Text;
            this.nextToken.Length = 0;

            // advance to the next character
            this.GetNextCharacter();

            // skip to the first tag end we find
            while (!(this.nextCharacter == '>' && !this.IsNextCharacterEntity) && !this.IsAtEndOfStream)
            {
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                // advance past the tag end
                this.GetNextCharacter();
            }
        }

        /// <summary>
        /// skips processing directives starting with the characters '<?' and ending with '?>' 
        /// NOTE: 10/14/2004: IE also ends processing directives with a />, so this function is
        /// being modified to recognize that condition as well.
        /// </summary>
        private void SkipProcessingDirective()
        {
            // verify that we are at a processing directive
            Debug.Assert(this.nextCharacter == '<' && this.lookAheadCharacter == '?');

            // advance twice, once to get the lookahead character and then to reach the start of the drective
            this.GetNextCharacter();
            this.GetNextCharacter();

            while (!((this.nextCharacter == '?' || this.nextCharacter == '/') && this.lookAheadCharacter == '>') && !this.IsAtEndOfStream)
            {
                // advance
                // we don't need to check for entities here because '?' is not an entity
                // and even though > is an entity there is no entity processing when reading lookahead character
                this.GetNextCharacter();
            }

            if (!this.IsAtEndOfStream)
            {
                // advance, first to the last >
                this.GetNextCharacter();

                // then advance past it to the next character after processing directive
                this.GetNextCharacter();
            }
        }
    }
}
