// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportBuyersPipeline.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Pipelines;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines
{
    /// <summary>Defines the ExportBuyers pipeline.</summary>
    /// <seealso cref="CommercePipeline{TArg, TResult}" />
    /// <seealso cref="IExportBuyersPipeline" />
    public class ExportBuyersPipeline : CommercePipeline<ExportEntitiesArgument, Shop>, IExportBuyersPipeline
    {
        /// <summary>Initializes a new instance of the <see cref="ExportBuyersPipeline" /> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ExportBuyersPipeline(IPipelineConfiguration<IExportBuyersPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }
    }
}

