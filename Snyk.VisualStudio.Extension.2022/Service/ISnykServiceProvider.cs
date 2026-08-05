using System.Threading;
using Snyk.VisualStudio.Extension.Authentication;
using Snyk.VisualStudio.Extension.Language;
using Snyk.VisualStudio.Extension.Settings;

namespace Snyk.VisualStudio.Extension.Service
{
    /// <summary>
    /// ServiceProvider interface for Snyk extension. Provide all needed services for this extension.
    /// The Visual Studio specific members live in ISnykServiceProvider.Vs.cs.
    /// </summary>
    public partial interface ISnykServiceProvider
    {
        /// <summary>
        /// Gets the package disposal token, cancelled when the extension shuts down. Exposed here
        /// (rather than reaching through <c>SnykVSPackage.Instance</c>) so consumers stay
        /// constructor-injectable and testable.
        /// </summary>
        CancellationToken DisposalToken { get; }

        /// <summary>
        /// Gets Solution service instance.
        /// </summary>
        ISolutionService SolutionService { get; }

        IWorkspaceTrustService WorkspaceTrustService { get; }

        /// <summary>
        /// Gets Tasks service instance.
        /// </summary>
        ISnykTasksService TasksService { get; }

        /// <summary>
        /// Gets <see cref="ISnykOptions"/> (Settings) implementation instance.
        /// </summary>
        ISnykOptions Options { get; }
        ISnykOptionsManager SnykOptionsManager { get; }

        /// <summary>
        /// Orchestrates IDE-side auth flow (login/logout, modal auth dialog).
        /// </summary>
        IAuthenticationFlowService AuthenticationFlowService { get; }

        /// <summary>
        /// Get Feature Flag Service
        /// </summary>
        IFeatureFlagService FeatureFlagService { get; }
        
        /// <summary>
        /// Get Language Client Manager
        /// </summary>
        ILanguageClientManager LanguageClientManager { get; set; }
    }
}
