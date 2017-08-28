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
            "User Story",
            "Task",
            "Bug",
            "A-question"
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
                        Id = issue.Number.ToString(),
                        Title = issue.Task
                    };
                    ri.Fields.Add("Assignee", (issue.Assignee == "Not Assigned") ? "" : issue.Assignee);
                    ri.Fields.Add("Label",issue.Label);
                    ri.Fields.Add("Milestone",issue.Milestone);
                    ri.Fields.Add("State",issue.Estimate);
                    ri.Fields.Add("Number", issue.Number);
                    //ri.Type = issue.Label == "A-bug" ? "Bug" : "Task";
                    ri.Fields.Add("Stack Rank", "-");
                    ri.Fields.Add("IterationPath", issue.Milestone);
                    ri.Fields.Add("Original Estimate", null);
                    ri.Fields.Add("Repro Steps", null);
                    ri.Fields.Add("Story Points", null);
                    ri.Fields.Add("Backlog Priority", null);
                    ri.Fields.Add("Remaining Work", null);
                    ri.Fields.Add("Effort", issue.Estimate);
                    ri.Fields.Add("Final Effort", issue.Estimate);
                    ri.Fields.Add("AreaPath", null);
                    ri.Fields.Add("Feature Type", issue.Label);
                    switch (issue.Label)
                    {
                        case "A-bug":
                            ri.Type = "Bug";
                            break;
                        case "A-feature":
                            ri.Type = "Task";
                            break;
                        case "A-question":
                            ri.Type = "Question";
                            break;
                        default:
                            break;
                    }

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
