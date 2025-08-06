using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Tanzu.Toolkit.VisualStudio.Options
{
    public class FolderEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider?.GetService(typeof(IWindowsFormsEditorService)) is IWindowsFormsEditorService)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select a directory";
                    dialog.SelectedPath = value as string ?? string.Empty;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                }
            }

            return value;
        }
    }
}