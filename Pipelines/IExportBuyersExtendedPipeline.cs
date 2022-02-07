// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IExportBuyersExtendedPipeline.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Pipelines;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines
{
    /// <summary>Defines the ExportBuyersExtended pipeline interface</summary>
    /// <seealso cref="IPipeline{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.ExportBuyersExtended)]
    public interface IExportBuyersExtendedPipeline : IPipeline<ExportEntitiesArgument, Shop, CommercePipelineExecutionContext>
    {
    }
}
