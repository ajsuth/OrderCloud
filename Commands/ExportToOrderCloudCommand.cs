// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportToOrderCloudCommand.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Commands
{
    /// <summary>Defines the ExportToOrderCloud command.</summary>
    public class ExportToOrderCloudCommand : CommerceCommand
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportToOrderCloudCommand" /> class.</summary>
        /// <param name="commander">The <see cref="CommerceCommander"/>.</param>
        /// <param name="serviceProvider">The service provider</param>
        public ExportToOrderCloudCommand(CommerceCommander commander, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.Commander = commander;
        }

        /// <summary>The process of the command</summary>
        /// <param name="commerceContext">The commerce context</param>
        /// <param name="parameter">The parameter for the command</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public virtual async Task<ExportResult> Process(CommerceContext commerceContext, ExportSettings exportSettings, List<SitePolicy> siteSettings, SellableItemExportPolicy productSettings)
        {
            ExportResult result = null;

            var context = commerceContext.CreatePartialClone();
            using (var activity = CommandActivity.Start(context, this))
            {
                var arg = new ExportToOrderCloudArgument(exportSettings, siteSettings, productSettings);
                result = await Commander.Pipeline<IExportToOrderCloudPipeline>().RunAsync(arg, context.PipelineContextOptions).ConfigureAwait(false);
                
                return context.GetObject<ExportResult>();
            }
        }
    }
}