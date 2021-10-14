using System;
using System.Globalization;
using System.Windows.Controls;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Errors
{
    public class UrlValidationRule : ValidationRule
    {
        public UrlValidationRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return value is string apiAddress
                ? string.IsNullOrWhiteSpace(apiAddress)
                    ? new ValidationResult(false, "Invalid Api Address: cannot be empty.")
                    : Uri.IsWellFormedUriString(apiAddress, UriKind.Absolute)
                        ? ValidationResult.ValidResult
                        : new ValidationResult(false, "Invalid Api Address: URI format could not be determined.")
                : new ValidationResult(false, "Invalid Api Address: cannot parse input.");
        }
    }
}
