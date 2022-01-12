// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportToOrderCloudPipeline.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines
{
    /// <summary>Defines the ExportToOrderCloud pipeline.</summary>
    /// <seealso cref="CommercePipeline{TArg, TResult}" />
    /// <seealso cref="IExportToOrderCloudPipeline" />
    public class ExportToOrderCloudPipeline : CommercePipeline<ExportToOrderCloudArgument, ExportResult>, IExportToOrderCloudPipeline
    {
        /// <summary>Initializes a new instance of the <see cref="ExportToOrderCloudPipeline" /> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ExportToOrderCloudPipeline(IPipelineConfiguration<IExportToOrderCloudPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}

