namespace Snyk.VisualStudio.Extension.Settings
{
    public interface ISnykOptions
    {
        string ApiToken
        {
            get;
            set;
        }

        string CustomEndpoint
        {
            get;
            set;
        }

        string Organization
        {
            get;
            set;
        }

        bool IgnoreUnknownCA
        {
            get;
            set;
        }

        string AdditionalOptions
        {
            get;
        }

        bool IsScanAllProjects
        {
            get;
        }
    }
}
