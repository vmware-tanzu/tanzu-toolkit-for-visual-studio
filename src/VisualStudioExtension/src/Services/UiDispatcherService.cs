﻿using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class UiDispatcherService : IUiDispatcherService
    {
        public async Task RunOnUiThreadAsync(Action method)
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            method.Invoke();
        }
    }
}
