using System;
using System.Globalization;
using System.Windows.Data;

namespace Accelera.Models
{
    /// <summary>
    /// This class is used as value converter class to bind the radio button group to the view model. See details here:
    /// https://stackoverflow.com/questions/9212873/binding-radiobuttons-group-to-a-property-in-wpf
    /// </summary>
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
