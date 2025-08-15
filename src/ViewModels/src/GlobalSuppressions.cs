// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private",
        Justification = "'internal' allows this field to be easily accessed by the test class for this view model.", Scope = "member",
        Target = "~F:Tanzu.Toolkit.ViewModels.DeploymentDialogViewModel._outputViewModel")]