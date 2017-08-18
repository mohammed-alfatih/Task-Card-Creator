using System;
using System.Collections.Generic;
using ReportInterface;
using TaskServerServiceInterface;
using System.Windows.Controls;

namespace GithubServices
{
    public class GitProject : ITaskProject
    {
        internal GitUserControl uc;

        public IEnumerable<string> WorkItemTypeCollection => new List<string>
        {
            "Story",
            "Task",
            "Bug"
        };

        public List<ReportItem> WorkItems
        {
            get
            {
                var l = new List<ReportItem>();
                foreach (var issue in uc.SelectedIssues)
                {
                    var ri = new ReportItem
                    {
                        Id = issue.Id.ToString(),
                        Title = issue.Title,
                        Description = issue.Body
                    };
                    ri.Fields.Add("Assignee", issue.Assignee);
                    ri.Fields.Add("CreatedAt", issue.CreatedAt);
                    ri.Fields.Add("Labels",issue.Labels);
                    ri.Fields.Add("Milestone",issue.Milestone.Title);
                    ri.Fields.Add("UpdatedAt",issue.UpdatedAt);
                    ri.Fields.Add("State",issue.State);

                    l.Add(ri);
                }
                return l;
            }
        }
        public IReport SelectedReport => uc.SelectedReport;

        public UserControl CreateUserControl(IEnumerable<IReport> supportedReports, IEnumerable<IReport> allReports)
        {
            uc = new GitUserControl(supportedReports, allReports);
            return uc;
        }

    }
}
