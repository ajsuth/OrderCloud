// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportCategoryAssignmentsMinionPipeline.cs" company="Sitecore Corporation">
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
    /// <summary>Defines the ExportCategoryAssignmentsMinion pipeline.</summary>
    /// <seealso cref="CommercePipeline{TArg, TResult}" />
    /// <seealso cref="IExportCategoryAssignmentsPipeline" />
    public class ExportCategoryAssignmentsPipeline : CommercePipeline<ExportEntitiesArgument, Category>, IExportCategoryAssignmentsPipeline
    {
        /// <summary>Initializes a new instance of the <see cref="ExportCategoriesPipeline" /> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ExportCategoryAssignmentsPipeline(IPipelineConfiguration<IExportCategoryAssignmentsPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}

