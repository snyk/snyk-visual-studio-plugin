namespace Snyk.VisualStudio.Extension.CLI
{
    public interface ICliProvider
    {
        public ICli Cli { get; }
    }
}