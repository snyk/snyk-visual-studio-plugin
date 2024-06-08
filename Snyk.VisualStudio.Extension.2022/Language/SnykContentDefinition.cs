using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public class SnykContentDefinitions
    {
        [Export]
        [FileExtension(".yaml")]
        [ContentType("yaml")]
        internal static FileExtensionToContentTypeDefinition SnykyamlFileExtensionDefinition;

        [Export]
        [FileExtension(".yml")]
        [ContentType("yaml")]
        internal static FileExtensionToContentTypeDefinition SnykymlFileExtensionDefinition;

        [Export]
        [FileExtension(".tf")]
        [ContentType("TerraformContentType")]
        internal static FileExtensionToContentTypeDefinition SnykTerraformFileExtensionDefinition;

        [Export]
        [Name("TerraformContentType")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition TerraformContentTypeDefinition;

        [Export]
        [Name("RazorCoreCSharp")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition RazorCoreCSharpContentTypeDefinition;

        [Export]
        [Name("code++")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition codePlusPlusContentTypeDefinition;

        [Export]
        [Name("CSharp")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition CSharpContentTypeDefinition;

        [Export]
        [Name("mustache")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition mustacheContentTypeDefinition;

        [Export]
        [Name("VB_LSP")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition VB_LSPContentTypeDefinition;

        [Export]
        [Name("RazorCSharp")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition RazorCSharpContentTypeDefinition;

        [Export]
        [Name("ClangFormat")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition ClangFormatContentTypeDefinition;

        [Export]
        [Name("JavaScript")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition JavaScriptContentTypeDefinition;

        [Export]
        [Name("Python")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition PythonContentTypeDefinition;

        [Export]
        [Name("Pkgdef")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition PkgdefContentTypeDefinition;

        [Export]
        [Name("CMakeSettings")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition CMakeSettingsContentTypeDefinition;

        [Export]
        [Name("F#")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition FSharpContentTypeDefinition;

        [Export]
        [Name("ResJSON")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition ResJSONContentTypeDefinition;

        [Export]
        [Name("RazorVisualBasic")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition RazorVisualBasicContentTypeDefinition;

        [Export]
        [Name("HTMLXProjection")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition HTMLXProjectionContentTypeDefinition;

        [Export]
        [Name("css")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition cssContentTypeDefinition;

        [Export]
        [Name("XML")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition XMLContentTypeDefinition;

        [Export]
        [Name("Basic")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition BasicContentTypeDefinition;

        [Export]
        [Name("C/C++")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition C_CPlusPlusContentTypeDefinition;

        [Export]
        [Name("htmlx")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition htmlxContentTypeDefinition;

        [Export]
        [Name("Razor")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition RazorContentTypeDefinition;

        [Export]
        [Name("DjangoTemplateTag")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition DjangoTemplateTagContentTypeDefinition;

        [Export]
        [Name("vbscript")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition vbscriptContentTypeDefinition;

        [Export]
        [Name("Django Templates")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition Django_TemplatesContentTypeDefinition;

        [Export]
        [Name("handlebars")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition handlebarsContentTypeDefinition;

        [Export]
        [Name("TFSourceControlOutput")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition TFSourceControlOutputContentTypeDefinition;

        [Export]
        [Name("TypeScript")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition TypeScriptContentTypeDefinition;

        [Export]
        [Name("DockerFileContentType")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition DockerFileContentTypeContentTypeDefinition;

        [Export]
        [Name("Dockerfile")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition DockerfileContentTypeDefinition;

        [Export]
        [Name("LESS")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition LESSContentTypeDefinition;

        [Export]
        [Name("C#_LSP")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition CSharp_LSPContentTypeDefinition;

        [Export]
        [Name("jade")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition jadeContentTypeDefinition;

        [Export]
        [Name("JSON")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition JSONContentTypeDefinition;

        [Export]
        [Name("HTML")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition HTMLContentTypeDefinition;

        [Export]
        [Name("VSCT")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition VSCTContentTypeDefinition;

        [Export]
        [Name("SCSS")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition SCSSContentTypeDefinition;

        [Export]
        [Name("XAML")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition XAMLContentTypeDefinition;

        [Export]
        [Name("CoffeeScript")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteBaseTypeName)]
        internal static ContentTypeDefinition CoffeeScriptContentTypeDefinition;
    }

    [ContentType("RazorCoreCSharp")]
    [ContentType("code++")]
    [ContentType("CSharp")]
    [ContentType("mustache")]
    [ContentType("VB_LSP")]
    [ContentType("RazorCSharp")]
    [ContentType("ClangFormat")]
    [ContentType("JavaScript")]
    [ContentType("Python")]
    [ContentType("Pkgdef")]
    [ContentType("Register")]
    [ContentType("Roslyn Languages")]
    [ContentType("RoslynPreviewContentType")]
    [ContentType("CMakeSettings")]
    [ContentType("F#")]
    [ContentType("ResJSON")]
    [ContentType("RazorVisualBasic")]
    [ContentType("HTMLXProjection")]
    [ContentType("css")]
    [ContentType("htc")]
    [ContentType("wsh")]
    [ContentType("srf")]
    [ContentType("XML")]
    [ContentType("Basic")]
    [ContentType("C/C++")]
    [ContentType("underscore")]
    [ContentType("htmlx")]
    [ContentType("Razor")]
    [ContentType("DjangoTemplateTag")]
    [ContentType("vbscript")]
    [ContentType("Django Templates")]
    [ContentType("handlebars")]
    [ContentType("TypeScript")]
    [ContentType("DockerFileContentType")]
    [ContentType("Dockerfile")]
    [ContentType("LESS")]
    [ContentType("C#_LSP")]
    [ContentType("jade")]
    [ContentType("JSON")]
    [ContentType("HTML")]
    [ContentType("VSCT")]
    [ContentType("SCSS")]
    [ContentType("XAML")]
    [ContentType("CoffeeScript")]
    [ContentType("TerraformContentType")]
    [ContentType("YAML")]
    public partial class SnykLanguageClient
    {
    }
}
