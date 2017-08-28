using ReportInterface;

namespace MSFforAgile
{
    class QuestionCardRow
    {
        public ReportItem WorkItem { get; private set; }

        public QuestionCardRow(ReportItem workItem)
        {
            WorkItem = workItem;
        }
    }
}
