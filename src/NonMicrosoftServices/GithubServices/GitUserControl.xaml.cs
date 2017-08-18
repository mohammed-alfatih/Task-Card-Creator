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

namespace GithubServices
{
    /// <summary>
    /// Interaction logic for JiraUserControl.xaml
    /// </summary>
    public partial class GitUserControl : UserControl, INotifyPropertyChanged
    {
        //private IPagedQueryResult<Issue> searchResult;
        ApiOptions options = new ApiOptions
        {
            PageSize = 10
        };
        private IEnumerable<IReport> supportedReports;
        private IEnumerable<IReport> allReports;


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
        public ObservableCollection<Issue> Issues { get; set; }

        public IEnumerable<Issue> SelectedIssues => this.listView.SelectedItems.Cast<Issue>();

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

        public string User
        {
            get { return Settings.Default.GitService_User; }
            set
            {
                Settings.Default.GitService_User = value;
                OnPropertyChanged("User");
            }
        }
        //needs to be removed because we don't use JIRA query language
        public string Jql
        {
            get { return Settings.Default.GitService_Jql; }
            set
            {
                Settings.Default.GitService_Jql = value;
                OnPropertyChanged("Jql");
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
        //not sure if this is used
        public ObservableCollection<int> AvailableItemsPerPage { get; private set; }

        public int ItemsPerPage
        {
            get
            {
                return Settings.Default.GitService_Paging_ItemsPerPage;
            }

            set
            {
                Settings.Default.GitService_Paging_ItemsPerPage = value;
                OnPropertyChanged("ItemsPerPage");
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
            Issues = new ObservableCollection<Issue>();
            AvailableItemsPerPage = new ObservableCollection<int> { 10, 20, 50, 100 };
            SelectedReport = Reports.First();

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //change to 
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            this.LoadIssues(1);
        }

        private void LoadIssues(int page)
        {
            // Load in a seperate thread
            Settings.Default.Save();
            //Issues is Ienumerable not list.
            this.Issues.ToList().Clear();
            this.PageInfo = "Loading...";
            this.IsLoading = true;
            this.IsNavigatingBackEnabled = false;
            this.IsNavigatingNextEnabled = false;


            Task.Factory.StartNew(() =>
            {
                //code snippet to get issues
                var server = new Uri(ProjectUrl);
                Credentials token = new Credentials(User, passwordBox.Password);
                var Client = new GitHubClient(new ProductHeaderValue("Issue-Validation"), server);
                Client.Credentials = token;
                options.StartPage = (page - 1) * options.PageSize.Value;
                RepositoryIssueRequest request = new RepositoryIssueRequest
                {
                    Filter = IssueFilter.All,
                    State = ItemStateFilter.All,
                    Since = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14))
                };
                //returns Ienumerable but we want observablecollection
                //figure out another way to do it
                Issues = new ObservableCollection<Issue>(Client.Issue.GetAllForCurrent(options).Result.Where(Issue => Issue.PullRequest is null));
            })
                .ContinueWith(ui =>
                {
                    if (ui.Status == TaskStatus.Faulted)
                    {
                        this.PageInfo = string.Format("Error: {0}", ui.Exception.Message);
                    }
                    else
                    {
                        int totalPages = (Issues.Count() / options.PageSize.Value) + 1;
                        int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;

                        this.PageInfo = $"{currentPage} of {totalPages}";
                        this.IsNavigatingBackEnabled = currentPage > 1;
                        this.IsNavigatingNextEnabled = currentPage < totalPages;
                        this.IsLoading = false;

                        foreach (var issue in Issues)
                        {
                            this.Issues.Add(issue);
                        }

                        this.listView.SelectAll();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonFirst_OnClick(object sender, RoutedEventArgs e)
        {
            //if (searchResult != null)
            //{
            this.LoadIssues(1);
            //}
        }

        private void ButtonPrev_OnClick(object sender, RoutedEventArgs e)
        {
            //if (searchResult != null)
            //{
            int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;

            this.LoadIssues(Math.Max(1, currentPage - 1));
            //}
        }

        private void ButtonNext_OnClick(object sender, RoutedEventArgs e)
        {
            //if (searchResult != null)
            //{
            int currentPage = (options.StartPage.Value / options.PageSize.Value) + 1;
            int totalPages = (Issues.Count() / options.PageSize.Value) + 1;

            this.LoadIssues(Math.Min(currentPage + 1, totalPages));
            //}
        }

        private void ButtonLast_OnClick(object sender, RoutedEventArgs e)
        {
            //if (searchResult != null)
            //{
            int totalPages = (Issues.Count() / options.PageSize.Value) + 1;

            this.LoadIssues(Math.Max(1, totalPages));
            //}
        }

        //private void ComboboxNumberOfRecords_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //if (searchResult != null)
        //    //{
        //        this.LoadIssues(1);
        //    //}
        //}
    }
}