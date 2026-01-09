// ABOUTME: Language Server command extensions for configuration and settings
// ABOUTME: Provides methods to fetch HTML configuration from the Language Server

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Snyk.VisualStudio.Extension.Language
{
    public static class LanguageServerCommands
    {
        /// <summary>
        /// Retrieves HTML configuration UI from the Language Server.
        /// Returns null if LS is not available or command fails.
        /// </summary>
        /// <param name="rpc">The JSON RPC wrapper to communicate with the Language Server</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTML string for configuration UI, or null if unavailable</returns>
        public static async Task<string> GetConfigHtmlAsync(IJsonRpc rpc, CancellationToken cancellationToken = default)
        {
            if (rpc == null) return null;

            try
            {
                var parameters = new
                {
                    command = LsConstants.SnykWorkspaceConfiguration,
                    arguments = new object[] { }
                };

                var result = await rpc.InvokeWithParameterObjectAsync<JToken>(
                    LsConstants.WorkspaceExecuteCommand,
                    parameters,
                    cancellationToken
                );

                return result?.Value<string>();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - fallback will handle this
                System.Diagnostics.Debug.WriteLine($"Error fetching config HTML: {ex.Message}");
                return null;
            }
        }
    }
}
