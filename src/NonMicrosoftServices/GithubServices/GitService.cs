using System.ComponentModel.Composition;
using System;
using System.Windows;
using TaskServerServiceInterface;

namespace GithubServices
{
    [Export(typeof(ITaskServerService))]
    public class GitService : ITaskServerService
    {
        public string Name => "Devtopia";

        public string Description => "Enterprise Github";

        public string ShortDescription => "Enterprise Github";

        public bool IsInstalled => true;

        public ITaskProject ConnectToProject(Window window)
        {
            return new GitProject();
        }
    }
}
