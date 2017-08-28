using System;
using System.Globalization;
using System.Windows.Data;
using ReportInterface;

namespace ScrumDetailed.Converters
{
    class AssigneeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var workItem = value as ReportItem;
                if (workItem != null)
                {
                    const string fieldName = "Assignee";
                    if (workItem.Fields[fieldName] != null)
                    {
                        string assignedTo = workItem.Fields[fieldName].ToString();
                        return assignedTo;
                    }
                }
                return "-";
            }
            catch (Exception exception)
            {
                return string.Format("Error: {0}", exception.Message);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
