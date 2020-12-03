using EnvDTE;
using System;

namespace Snyk.VisualStudio.Extension.Services
{
    public class SnykSolutionService
    {
        private static SnykSolutionService instance;

        private IServiceProvider serviceProvider;

        private SnykSolutionService() { }

        private SnykSolutionService(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public static SnykSolutionService NewInstance(IServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                instance = new SnykSolutionService(serviceProvider);
            }
            
            return instance;
        }

        public static SnykSolutionService Instance
        {
            get
            {
                return instance;
            }
        }
        
        public Projects GetProjects()
        {
            DTE dte = (DTE) this.ServiceProvider.GetService(typeof(DTE));

            return dte.Solution.Projects;
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }

            set
            {
                serviceProvider = value;
            }
        }
    }
}
