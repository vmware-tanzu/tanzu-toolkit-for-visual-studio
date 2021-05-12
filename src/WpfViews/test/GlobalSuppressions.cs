// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "'protected' allows test classes inheriting from ViewTestSupport to access this field.", Scope = "member", Target = "~F:Tanzu.Toolkit.WpfViews.Tests.ViewTestSupport.mockCloudFoundryService")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "'protected' allows test classes inheriting from ViewTestSupport to access this field.", Scope = "member", Target = "~F:Tanzu.Toolkit.WpfViews.Tests.ViewTestSupport.mockDialogService")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "'protected' allows test classes inheriting from ViewTestSupport to access this field.", Scope = "member", Target = "~F:Tanzu.Toolkit.WpfViews.Tests.ViewTestSupport.mockLoggingService")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "'protected' allows test classes inheriting from ViewTestSupport to access this field.", Scope = "member", Target = "~F:Tanzu.Toolkit.WpfViews.Tests.ViewTestSupport.mockViewLocatorService")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "'protected' allows test classes inheriting from ViewTestSupport to access this field.", Scope = "member", Target = "~F:Tanzu.Toolkit.WpfViews.Tests.ViewTestSupport.services")]
