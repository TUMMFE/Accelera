using System.Windows;

namespace Accelera.Models
{
    /// <summary>
    /// Infos on MVVM binding for DialogResult can be found here:
    /// https://stackoverflow.com/questions/501886/how-should-the-viewmodel-close-the-form/3329467#3329467
    /// </summary>
    public class DialogCloser
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(DialogCloser),
                new PropertyMetadata(DialogResultChanged));

        private static void DialogResultChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window != null)
                window.DialogResult = e.NewValue as bool?;
        }
        public static void SetDialogResult(Window target, bool? value)
        {
            target.SetValue(DialogResultProperty, value);
        }
    }
}
