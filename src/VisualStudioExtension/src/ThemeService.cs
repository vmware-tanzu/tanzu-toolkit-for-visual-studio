using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Tanzu.Toolkit.WpfViews;
using Tanzu.Toolkit.WpfViews.ThemeService;

namespace Tanzu.Toolkit.VisualStudio
{
    public class ThemeService : IThemeService
    {
        public void SetTheme(FrameworkElement element)
        {
            if (element is Control control)
            {
                control.SetResourceReference(Control.BackgroundProperty, ThemedDialogColors.WindowPanelBrushKey);
                control.SetResourceReference(Control.ForegroundProperty, ThemedDialogColors.WindowPanelTextBrushKey);
                control.SetResourceReference(Control.BorderBrushProperty, ThemedDialogColors.WindowBorderBrushKey);
                control.SetResourceReference(TasExplorerView.ListItemMouseOverBrushProperty, ThemedDialogColors.ListItemMouseOverBrushKey);
                control.SetResourceReference(TasExplorerView.WizardFooterBrushProperty, ThemedDialogColors.WizardFooterBrushKey);
                control.SetResourceReference(DeploymentDialogView.HyperlinkBrushProperty, ThemedDialogColors.HyperlinkBrushKey);
            }

            ThemedDialogStyleLoader.SetUseDefaultThemedDialogStyles(element, true);
            ImageThemingUtilities.SetThemeScrollBars(element, true);
            if (!element.IsInitialized)
            {
                element.Initialized += OnElementInitialized;
            }
            else
            {
                MergeStyles(element);
            }
        }

        private static void OnElementInitialized(object sender, EventArgs args)
        {
            var element = (FrameworkElement)sender;
            MergeStyles(element);
            element.Initialized -= OnElementInitialized;
        }

        private static void MergeStyles(FrameworkElement element)
        {
            Collection<ResourceDictionary> dictionaries = element.Resources.MergedDictionaries;
            if (!dictionaries.Contains(ThemeResources))
            {
                dictionaries.Add(ThemeResources);
            }
        }
        private static ResourceDictionary ThemeResources { get; } = BuildThemeResources();

        public static object InputPaddingKey { get; } = "Toolkit" + nameof(InputPaddingKey);

        private static ResourceDictionary BuildThemeResources()
        {
            var resources = new ResourceDictionary();

            try
            {
                var inputPadding = new Thickness(6, 8, 6, 8); // This is the same padding used by WatermarkedTextBox.

                resources[InputPaddingKey] = inputPadding;

                resources[typeof(TextBox)] = new Style
                {
                    TargetType = typeof(TextBox),
                    BasedOn = (Style)Application.Current.FindResource(VsResourceKeys.TextBoxStyleKey),
                    Setters =
                    {
                        new Setter(Control.PaddingProperty, new DynamicResourceExtension(InputPaddingKey))
                    }
                };

                resources[typeof(ComboBox)] = new Style
                {
                    TargetType = typeof(ComboBox),
                    BasedOn = (Style)Application.Current.FindResource(VsResourceKeys.ComboBoxStyleKey),
                    Setters =
                    {
                        new Setter(Control.PaddingProperty, new DynamicResourceExtension(InputPaddingKey))
                    }
                };
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                //ex.Log();
            }

            return resources;
        }
    }
}
