// <copyright file="ExportToOrderCloudArgument.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using System.Collections.Generic;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments
{
    /// <summary>Defines the ExportCustomersMinion pipeline argument.</summary>
    /// <seealso cref="PipelineArgument" />
    public class ExportToOrderCloudArgument : PipelineArgument
    {
        public ExportToOrderCloudArgument(ExportSettings processSettings, List<CatalogExportPolicy> catalogSettings, SellableItemExportPolicy productSettings)
        {
            Condition.Requires(processSettings, nameof(processSettings)).IsNotNull();

            ProcessSettings = processSettings;
            CatalogSettings = catalogSettings ?? new List<CatalogExportPolicy>();
            ProductSettings = productSettings;
        }

        /// <summary>
        /// The process settings
        /// </summary>
        public ExportSettings ProcessSettings { get; set; }

        /// <summary>
        /// The product settings
        /// </summary>
        public List<CatalogExportPolicy> CatalogSettings { get; set; }

        /// <summary>
        /// The product settings
        /// </summary>
        public SellableItemExportPolicy ProductSettings { get; set; }
    }
}
