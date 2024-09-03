namespace Snyk.Code.Library.Domain.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Snyk.Code.Library.Api.Dto.Analysis;

    /// <summary>
    /// Contains Snyk Code suggestion for code improvement.
    /// </summary>
    public class Suggestion
    {
        public Suggestion(string fileName, SuggestionDto suggestionDto, FileDto fileDto)
        {
            this.Id = suggestionDto.Id;
            this.Rule = suggestionDto.Rule;
            this.Message = suggestionDto.Message;
            this.Severity = suggestionDto.Severity;
            this.Categories = suggestionDto.Categories;
            this.Tags = suggestionDto.Tags;
            this.Title = suggestionDto.Title;
            this.Cwe = suggestionDto.Cwe;
            this.Text = suggestionDto.Text;
            this.FileName = fileName;
            this.RepoDatasetSize = suggestionDto.RepoDatasetSize;
            this.ExampleCommitDescriptions = suggestionDto.ExampleCommitDescriptions;
            this.Rows = Tuple.Create(fileDto.Rows[0], fileDto.Rows[1]);
            this.Columns = Tuple.Create(fileDto.Cols[0], fileDto.Cols[1]);
            this.Markers = fileDto.Markers.Select(markerDto => new Marker
            {
                MessageIndexes = markerDto.MessageIndexes,
                Positions = markerDto.Positions.Select(positionDto => new Position
                {
                    Columns = positionDto.Cols,
                    Rows = positionDto.Rows,
                    FileName = positionDto.File,
                }).ToList(),
            }).ToList();
        }

        public Suggestion()
        {

        }
        /// <summary>
        /// Gets a value indicating analysis suggestion id. Id for this (local) result.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets tuple of rows.
        /// </summary>
        public Tuple<int, int> Rows { get; set; }

        /// <summary>
        /// Gets tuple of columns.
        /// </summary>
        public Tuple<int, int> Columns { get; set; }

        /// <summary>
        /// Gets a value indicating analysis rule.
        /// </summary>
        public string Rule { get; set; }

        /// <summary>
        /// Gets a value indicating suggestion message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets file name (relative path).
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets a value indicating analysis severity value (from 1 to 4).
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// Gets position markers.
        /// </summary>
        public IList<Marker> Markers { get; set; }

        /// <summary>
        /// Gets a value indicating analysis categories.
        /// </summary>
        public IList<string> Categories { get; set; }

        /// <summary>
        /// Gets a value indicating analysis tags.
        /// </summary>
        public IList<string> Tags { get; set; }

        /// <summary>
        /// Gets a value indicating suggestion title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets repo dataset size.
        /// </summary>
        public int RepoDatasetSize { get; set; }

        /// <summary>
        /// Gets a value indicating analysis CWE list.
        /// </summary>
        public IList<string> Cwe { get; set; }

        /// <summary>
        /// Gets a value indicating suggestion description.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets a value indicating suggestion examples.
        /// </summary>
        public IList<string> ExampleCommitDescriptions { get; set; }

        /// <summary>
        /// Gets a value indicating suggestion list of proposed fixes and examples.
        /// </summary>
        public IList<SuggestionFix> Fixes { get; set; } = new List<SuggestionFix>();
    }
}
