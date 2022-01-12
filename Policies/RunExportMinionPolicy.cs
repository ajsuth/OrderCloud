// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RunExportMinionPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    /// <summary>Defines the run export minion policy.</summary>
    /// <seealso cref="RunMinionPolicy" />
    public class RunExportMinionPolicy : RunMinionPolicy
    {
        /// <summary>
        /// Gets or sets the export assignments flag, which will override the export entities default behaviour.
        /// </summary>
        /// <value>
        /// The export assignments flag.
        /// </value>
        public bool ExportAssignments { get; set; } = false;
    }
}
