using System;
using System.Globalization;
using System.Windows.Data;

namespace SocketChat
{
    public class BooleanToConnectionStatusConverter : IMultiValueConverter
    {
        private const string srvTrue = "Server is active";
        private const string srvFalse = "Server is stopped";
        private const string clntTrue = "Connected to server";
        private const string clntFalse = "Disconnected from server";
        private const string error = "Cannot detect connection status";

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

        public object ConvertBack(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //return value.ToString() == strTrue;

            bool isServer = (bool)value[0];
            bool isActive = (bool)value[1];

            return value[0].ToString() == error;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}