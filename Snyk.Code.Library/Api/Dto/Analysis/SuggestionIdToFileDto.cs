namespace Snyk.Code.Library.Api.Dto.Analysis
{
    using System.Collections.Generic;

    /// <summary>
    /// Map Suggestion id to file object information.
    /// </summary>
    public class SuggestionIdToFileDto : Dictionary<string, List<FileDto>>
    {
    }
}
