﻿using Snyk.VisualStudio.Extension.Service;
using System;
using Microsoft.VisualStudio.Shell;
using Snyk.VisualStudio.Extension.Language;

namespace Snyk.VisualStudio.Extension.Commands
{
    /// <summary>
    /// Common class for <see cref="SnykScanCommand"/> and <see cref="SnykStopCurrentTaskCommand"/> task commands.
    /// </summary>
    public abstract class AbstractTaskCommand : AbstractSnykCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTaskCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        public AbstractTaskCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService)
        {
        }

        /// <summary>
        /// Check is scan enabled.
        /// </summary>
        /// <returns>True if no other tasks running and solution is open.</returns>
        protected bool IsButtonAvailable()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var isLsReady = LanguageClientHelper.IsLanguageServerReady();
            return SnykSolutionService.Instance.IsSolutionOpen() && isLsReady;
        }

        /// <inheritdoc/>
        protected override void OnBeforeQueryStatus(object sender, EventArgs e) => ThreadHelper.JoinableTaskFactory.Run(this.UpdateStateAsync);
    }
}
