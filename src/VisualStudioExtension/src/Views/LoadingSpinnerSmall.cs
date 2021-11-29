using System.Windows;
using System.Windows.Controls;

namespace Tanzu.Toolkit.VisualStudio.Views
{
    public class LoadingSpinnerSmall : Control
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(LoadingSpinnerSmall),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        static LoadingSpinnerSmall()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingSpinnerSmall), new FrameworkPropertyMetadata(typeof(LoadingSpinnerSmall)));
        }
    }
}
