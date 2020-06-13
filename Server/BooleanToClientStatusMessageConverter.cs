﻿using System;
using System.Windows.Data;

namespace SocketChat
{
    public class BooleanToClientStatusMessageConverter : IValueConverter
    {
        private const string strTrue = "Server is connected";
        private const string strFalse = "Server is not connected";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value)
                {
                    return strTrue;
                }
            }

            return strFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString() == strTrue;
        }
    }
}