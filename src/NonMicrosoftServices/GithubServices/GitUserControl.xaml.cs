// This source is subject to Microsoft Public License (Ms-PL).
// Please see https://github.com/frederiksen/Task-Card-Creator for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ReportInterface;
using GithubServices.Properties;
using Octokit;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GithubServices
{
    /// <summary>
    /// Interaction logic for GitUserControl.xaml
    /// </summary>
    /// 

    public class IssueTask
    {
        public string Assignee { get; set; }
        public string Task { get; set; }
        public int Number { get; set; }
        public string Estimate { get; set; }
        public string Milestone { get; set; }
        public string Label { get; set; }
       
        public IssueTask(string assignee, string task, int number, string milestone, string estimate,string label)
        {
            Assignee = assignee;
            Task = task;
            Number = number;
            Milestone = milestone;
            Estimate = estimate;
            Label = label;
        }
    }

    public partial class GitUserControl : UserControl, INotifyPropertyChanged
    {
        private IReadOnlyList<Issue> phIssues;
 
        //not used
        ApiOptions options = new ApiOptions
        {
            PageSize = 200
        };
        private IEnumerable<IReport> supportedReports;
        private IEnumerable<IReport> allReports;

        public ObservableCollection<IssueTask> OrganizedTasks { get; set; }

        public ObservableCollection<IssueTask> DisplayedTasks { get; set; }

        public HashSet<string> Milestones { get; set; }

        public ObservableCollection<IReport> Reports { get; set; }

        private IReport selectedReport;

        public IReport SelectedReport
        {
            get { return selectedReport; }
            set
            {
                selectedReport = value;
                OnPropertyChanged("SelectedReport");
            }
        }

        public ObservableCollection<Issue> GitIssues { get; set; }

        public List<Repository> Repos { get; set; }

        public IEnumerable<IssueTask> SelectedIssues => this.listView.SelectedItems.Cast<IssueTask>();

        private string _CurrentMilestone;

        public string CurrentMilestone
        {
            get { return _CurrentMilestone; }
            set
            {
                _CurrentMilestone = value;
                LoadCurrentMilestoneIssues();
                OnPropertyChanged("CurrentMilestone");
            }
        }

        public ObservableCollection<int> Projects { get; set; }

        public string ProjectUrl
        {
            get { return Settings.Default.GitService_Url; }
            set
            {
                Settings.Default.GitService_Url = value;
                OnPropertyChanged("ProjectUrl");
            }
        }

        public string IntendedRepo
        {
            get { return Settings.Default.GitService_Repo; }
            set
            {
                Settings.Default.GitService_Repo = value;
                OnPropertyChanged("IntendedRepo");
            }
        }

        public string User
        {
            get { return Settings.Default.GitService_User; }
            set
            {
                Settings.Default.GitService_User = value;
                OnPropertyChanged("User");
            }
        }

        public string GitToken
        {
            get
            {
                return Settings.Default.GitService_Token;
            }
            set
            {
                Settings.Default.GitService_Token = value;
                OnPropertyChanged("GitToken");
            }
        }

        private string pageInfo = "-";
        public string PageInfo
        {
            get { return pageInfo; }
            set
            {
                pageInfo = value;
                OnPropertyChanged("PageInfo");
            }
        }


        private bool isNavigatingBackEnabled;
        public bool IsNavigatingBackEnabled
        {
            get
            {
                return this.isNavigatingBackEnabled;
            }

            set
            {
                this.isNavigatingBackEnabled = value;
                OnPropertyChanged("IsNavigatingBackEnabled");
            }
        }

        private bool isNavigatingNextEnabled;
        public bool IsNavigatingNextEnabled
        {
            get
            {
                return this.isNavigatingNextEnabled;
            }

            set
            {
                this.isNavigatingNextEnabled = value;
                OnPropertyChanged("IsNavigatingNextEnabled");
            }
        }

        private bool isLoading;
        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }

            set
            {
                this.isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }

        private bool showAll = false;
        public bool ShowAll
        {
            get { return showAll; }
            set
            {
                showAll = value;

                Reports.Clear();
                if (showAll)
                {
                    foreach (var report in allReports)
                    {
                        Reports.Add(report);
                    }
                }
                else
                {
                    foreach (var report in supportedReports)
                    {
                        Reports.Add(report);
                    }
                }
                SelectedReport = Reports.FirstOrDefault();

                OnPropertyChanged("ShowAll");
            }
        }

        public GitUserControl(IEnumerable<IReport> supportedReports, IEnumerable<IReport> allReports)
        {
            DataContext = this;

            this.supportedReports = supportedReports;
            this.allReports = allReports;

            Reports = new ObservableCollection<IReport>(supportedReports);
            phIssues = new List<Issue>();//new ObservableCollection<Issue>();
            GitIssues = new ObservableCollection<Issue>();
            Repos = new List<Repository>();
            OrganizedTasks = new ObservableCollection<IssueTask>();
            Milestones = new HashSet<string>();

            SelectedReport = Reports.First();

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //PropertyChangedEventHandler handler = PropertyChanged;
            //if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            this.LoadIssues(1);
        }

        private void LoadIssues(int page)
        {
            Settings.Default.Save();
            GitIssues.Clear();
            this.PageInfo = "Loading...";
            this.IsLoading = true;
            this.IsNavigatingBackEnabled = false;
            this.IsNavigatingNextEnabled = false;
            DateTimeOffset currDate = DateTimeOffset.Now;
            Milestone currmilestone = new Milestone();
            Task.Factory.StartNew(() =>
            {
                //code snippet to get issues
                var server = new Uri(ProjectUrl);
                Credentials token = new Credentials(GitToken);
                var Client = new GitHubClient(new ProductHeaderValue("Issue-Validation"), server)
                {
                    Credentials = token
                };

                Connection connect = new Connection(new ProductHeaderValue("Get-Milestones"), server)
                {
                    Credentials = token
                };

                ApiConnection aConnection = new ApiConnection(connect);
                MilestonesClient mClient = new MilestonesClient(aConnection);

                options.StartPage = (page - 1) * options.PageSize.Value;
                
                try
                {
                    phIssues = Client.Issue.GetAllForRepository("ArcGISPro",IntendedRepo).GetAwaiter().GetResult();
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                
            })
                .ContinueWith(ui =>
                {
                    TimeSpan minSpan = TimeSpan.MaxValue;
                    
                    foreach (var issue in phIssues)
                    {
                        Milestones.Add(issue.Milestone.Title);
                        if (issue.Milestone.DueOn.HasValue)
                        {
                            if (DateTimeOffset.Compare((issue.Milestone.DueOn.Value), currDate) > 0 && minSpan > issue.Milestone.DueOn.Value - currDate)
                                currmilestone = issue.Milestone;
                        }
                    }
                    
                    if (ui.Status == TaskStatus.Faulted)
                    {
                        this.PageInfo = string.Format("Error: {0}", ui.Exception.Message);
                    }
                    else
                    {
                        foreach (var issue in phIssues)
                        {
                            GitIssues.Add(issue);
                        }

                        CurrentMilestone = currmilestone.Title;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void LoadCurrentMilestoneIssues()
        {

            OrganizedTasks.Clear();
            bool NotATask = false;
            foreach (var issue in phIssues)
            {
                if (!issue.Milestone.Title.Equals(CurrentMilestone))
                    continue;
                NotATask = false;
                foreach (var lable in issue.Labels)
                {
                    if (lable.Name.Equals("A-bug"))
                    {
                        OrganizedTasks.Add(new IssueTask(issue.Assignee.Login, issue.Title, issue.Number, issue.Milestone.Title, "Estimate?", "A-bug"));
                        NotATask = true;
                        break;
                    }
                    else if(lable.Name.Equals("A-question"))
                    {
                        OrganizedTasks.Add(new IssueTask(issue.Assignee.Login, issue.Title, issue.Number, issue.Milestone.Title, "Estimate?", "A-question"));
                        NotATask = true;
                        break;
                    }
                }
                if (!NotATask)
                    Parse(issue);
            }
            int totalPages = (OrganizedTasks.Count() / options.PageSize.Value) + 1;
            int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;

            PageInfo = $"{currentPage} of {totalPages}";
            IsNavigatingBackEnabled = currentPage > 1;
            IsNavigatingNextEnabled = currentPage < totalPages;
            IsLoading = false;
            listView.SelectAll();
        }

        private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonFirst_OnClick(object sender, RoutedEventArgs e)
        {
            if (OrganizedTasks != null)
            {
                this.LoadIssues(1);
            }
        }

        private void ButtonPrev_OnClick(object sender, RoutedEventArgs e)
        {
            if (OrganizedTasks != null)
            {
                int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;

                this.LoadIssues(Math.Max(1, currentPage - 1));
            }
        }

        private void ButtonNext_OnClick(object sender, RoutedEventArgs e)
        {
            if (OrganizedTasks != null)
            {
                int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;
                int totalPages = (phIssues.Count() / options.PageSize.Value) + 1;

                this.LoadIssues(Math.Min(currentPage + 1, totalPages));
            }
        }

        private void ButtonLast_OnClick(object sender, RoutedEventArgs e)
        {
            if (OrganizedTasks != null)
            {
                int totalPages = (phIssues.Count() / options.PageSize.Value) + 1;

                this.LoadIssues(Math.Max(1, totalPages));
            }
        }

        private void Parse(Issue issue)
        {
            List<IssueTask> issuetasks = new List<IssueTask>();
            Regex AssignedReg = new Regex("(?<=\\[ \\])(.*)(@)(.*)(?=\r\n)");
            Regex NonAssignedReg = new Regex("(?<=\\[ \\])(.[^@]*?)(?=\r\n)");
            Regex LableReg = new Regex("(C-)([0-9].*)(?=\n)");
            Regex typeLableReg = new Regex("(A-)([a-zA-Z]+)");

            string issueLable = "Estimate?";
            string typelable = "";
            
            foreach(var label in issue.Labels)
            {
                if (LableReg.IsMatch(label.Name))
                    issueLable = label.Name;
                if (typeLableReg.IsMatch(label.Name))
                    typelable = label.Name;
            }

            MatchCollection AssignedMatches = AssignedReg.Matches(issue.Body);

            MatchCollection NonAssignedMatches = NonAssignedReg.Matches(issue.Body);

            if (AssignedMatches.Count == 0 && NonAssignedMatches.Count == 0)
                return;
            foreach(var match in AssignedMatches)
            {
                IssueTask phIssueTask = new IssueTask(match.ToString().Substring(match.ToString().IndexOf("@")), match.ToString().Substring(0, match.ToString().Length - (match.ToString().Length- match.ToString().IndexOf("@"))), issue.Number, issue.Milestone.Title, issueLable,typelable);
                OrganizedTasks.Add(phIssueTask);
            }
            foreach(var match in NonAssignedMatches)
            {
                IssueTask phIssueTask = new IssueTask("Not Assigned", match.ToString(),issue.Number,issue.Milestone.Title,issueLable,typelable);
                OrganizedTasks.Add(phIssueTask);
            }
            
            
        }
    }
}