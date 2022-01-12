// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCatalogAssignmentsMinionPipeline.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines
{
    /// <summary>Defines the ExportCatalogAssignmentsMinion pipeline.</summary>
    /// <seealso cref="CommercePipeline{TArg, TResult}" />
    /// <seealso cref="IExportCatalogAssignmentsPipeline" />
    public class ExportCatalogAssignmentsPipeline : CommercePipeline<ExportEntitiesArgument, Catalog>, IExportCatalogAssignmentsPipeline
    {
        /// <summary>Initializes a new instance of the <see cref="ExportCategoriesPipeline" /> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ExportCatalogAssignmentsPipeline(IPipelineConfiguration<IExportCatalogAssignmentsPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}

