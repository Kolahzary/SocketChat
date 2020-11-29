using System;
using System.Globalization;
using System.Windows.Data;

namespace SocketChat
{
    public class BooleanToStartStopConverter : IMultiValueConverter
    {
        private const string srvTrue = "Stop";
        private const string srvFalse = "Start";
        private const string clntTrue = "Disconnect";
        private const string clntFalse = "Connect";
        private const string error = "?";

        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isServer = (bool)value[0];
            bool isActive = (bool)value[1];

            if (isServer && isActive)
            {
                return srvTrue;
            }

            if (isServer && !isActive)
            {
                return srvFalse;
            }

            if (!isServer && isActive)
            {
                return clntTrue;
            }

            if (!isServer && !isActive)
            {
                return clntFalse;
            }
            else
            {
                return error;
            }

            //if (value is bool)
            //{
            //    if ((bool)value)
            //    {
            //        return strTrue;
            //    }
            //}

            //return strFalse;
        }

        //public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //{
        //    //return value.ToString() == strTrue;
        //}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}