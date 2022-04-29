namespace Snyk.VisualStudio.Extension.Shared.Lsp
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Utilities;

    public class SnykContentDefinition
    {
        public const string Identifier = "snyk";

        public const string TfExtension = ".tf";
        public const string CsExtension = ".cs";
        public const string JsExtension = ".js";

        [Export]
        [Name(Identifier)]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition SnykContentTypeDefinition { get; set; }

        [Export]
        [FileExtension(TfExtension)]
        [ContentType(Identifier)]
        internal static FileExtensionToContentTypeDefinition SnykTfFileExtensionDefinition { get; set; }

        [Export]
        [FileExtension(CsExtension)]
        [ContentType(Identifier)]
        internal static FileExtensionToContentTypeDefinition SnykCsFileExtensionDefinition { get; set; }

        [Export]
        [FileExtension(JsExtension)]
        [ContentType(Identifier)]
        internal static FileExtensionToContentTypeDefinition SnykJsFileExtensionDefinition { get; set; }
    }
}
