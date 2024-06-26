﻿using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Tanzu.Toolkit.VisualStudio.Views.Converters
{
    public class ListToStringConverter : IValueConverter
    {
        public string EmptyListMessage { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is ObservableCollection<string> valueList))
            {
                return string.Empty;
            }

            return valueList.Count == 0 ? EmptyListMessage : string.Join(", ", valueList);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}