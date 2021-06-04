//---------------------------------------------------------------------------
// 
// File: HtmlTokenType.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Description: Definition of token types supported by HtmlLexicalAnalyzer
//
//---------------------------------------------------------------------------

namespace Microsoft.HtmlConverter
{
    /// <summary>
	/// types of lexical tokens for html-to-xaml converter
	/// </summary>
    internal enum HtmlTokenType
    {
        /// <summary>
        /// Defines the OpeningTagStart.
        /// </summary>
        OpeningTagStart,

        /// <summary>
        /// Defines the ClosingTagStart.
        /// </summary>
        ClosingTagStart,

        /// <summary>
        /// Defines the TagEnd.
        /// </summary>
        TagEnd,

        /// <summary>
        /// Defines the EmptyTagEnd.
        /// </summary>
        EmptyTagEnd,

        /// <summary>
        /// Defines the EqualSign.
        /// </summary>
        EqualSign,

        /// <summary>
        /// Defines the Name.
        /// </summary>
        Name,

        /// <summary>
        /// Defines the Atom.
        /// </summary>
        Atom, // any attribute value not in quotes

        /// <summary>
        /// Defines the Text.
        /// </summary>
        Text, //text content when accepting text

        /// <summary>
        /// Defines the Comment.
        /// </summary>
        Comment,

        /// <summary>
        /// Defines the EOF.
        /// </summary>
        EOF,
    }
}
