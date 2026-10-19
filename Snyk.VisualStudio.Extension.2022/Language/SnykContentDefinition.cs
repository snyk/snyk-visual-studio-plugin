using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykContentDefinitions
    {
#pragma warning disable 0649
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
#pragma warning restore 0649
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SnykContentTypeAttribute : Attribute
    {
        public SnykContentTypeAttribute(string name)
        {
            ContentTypes = name;
        }

        public string ContentTypes { get; }
    }

    [SnykContentType("RazorCoreCSharp")]
    [SnykContentType("code++")]
    [SnykContentType("CSharp")]
    [SnykContentType("mustache")]
    [SnykContentType("VB_LSP")]
    [SnykContentType("RazorCSharp")]
    [SnykContentType("ClangFormat")]
    [SnykContentType("JavaScript")]
    [SnykContentType("Python")]
    [SnykContentType("Pkgdef")]
    [SnykContentType("Register")]
    [SnykContentType("Roslyn Languages")]
    [SnykContentType("RoslynPreviewContentType")]
    [SnykContentType("CMakeSettings")]
    [SnykContentType("F#")]
    [SnykContentType("ResJSON")]
    [SnykContentType("RazorVisualBasic")]
    [SnykContentType("HTMLXProjection")]
    [SnykContentType("css")]
    [SnykContentType("htc")]
    [SnykContentType("wsh")]
    [SnykContentType("srf")]
    [SnykContentType("XML")]
    [SnykContentType("Basic")]
    [SnykContentType("C/C++")]
    [SnykContentType("underscore")]
    [SnykContentType("htmlx")]
    [SnykContentType("Razor")]
    [SnykContentType("DjangoTemplateTag")]
    [SnykContentType("vbscript")]
    [SnykContentType("Django Templates")]
    [SnykContentType("handlebars")]
    [SnykContentType("TypeScript")]
    [SnykContentType("DockerFileContentType")]
    [SnykContentType("Dockerfile")]
    [SnykContentType("LESS")]
    [SnykContentType("C#_LSP")]
    [SnykContentType("jade")]
    [SnykContentType("JSON")]
    [SnykContentType("HTML")]
    [SnykContentType("VSCT")]
    [SnykContentType("SCSS")]
    [SnykContentType("XAML")]
    [SnykContentType("CoffeeScript")]
    [SnykContentType("TerraformContentType")]
    [SnykContentType("YAML")]
    [SnykContentType("Apex")]
    [SnykContentType("C++")]
    [SnykContentType("Go")]
    [SnykContentType("Java")]
    [SnykContentType("Kotlin")]
    [SnykContentType("VB")]
    [SnykContentType("PHP")]
    [SnykContentType("Ruby")]
    [SnykContentType("Scala")]
    [SnykContentType("Swift")]
    public partial class SnykLanguageClient
    {
    }
}
