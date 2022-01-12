// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IExportCatalogAssignmentsPipeline.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines
{
    /// <summary>Defines the ExportCatalogAssignments pipeline interface</summary>
    /// <seealso cref="IPipeline{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.ExportCatalogAssignments)]
    public interface IExportCatalogAssignmentsPipeline : IPipeline<ExportEntitiesArgument, Catalog, CommercePipelineExecutionContext>
    {
    }
}
