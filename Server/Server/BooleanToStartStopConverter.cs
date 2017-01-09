using System;
using System.Windows.Data;

namespace Server
{
    public class BooleanToStartStopConverter : IValueConverter
    {
        private const string strTrue = "Stop";
        private const string strFalse = "Start";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
                if ((bool)value)
                    return strTrue;
            return strFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString() == strTrue;
        }
    }

}
