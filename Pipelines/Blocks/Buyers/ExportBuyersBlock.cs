// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportBuyersBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportCustomers pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportCustomers)]
    public class ExportBuyersBlock : AsyncPipelineBlock<ExportToOrderCloudArgument, ExportToOrderCloudArgument, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportBuyersBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportBuyersBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="ExportToOrderCloudArgument"/>.</returns>
        public override async Task<ExportToOrderCloudArgument> RunAsync(ExportToOrderCloudArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            if (!arg.ProcessSettings.ProcessSites)
            {
                context.Logger.LogInformation($"Skipping site export - not enabled.");
                return arg;
            }

            long itemsProcessed = 0;

            var exportSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>();
            var siteSettings = exportSettings.SiteSettings;

            context.Logger.LogInformation($"{Name}-Reviewing Site Settings|Count:{siteSettings.Count}|Environment:{context.CommerceContext.Environment.Name}");

            if (siteSettings.Count == 0)
            {
                return arg;
            }

            itemsProcessed += siteSettings.Count;

            var domains = siteSettings.Select(s => s.Domain).Distinct();

            foreach (var domain in domains)
            {
                var error = false;

                var newContext = new CommercePipelineExecutionContextOptions(new CommerceContext(context.CommerceContext.Logger, context.CommerceContext.TelemetryClient)
                {
                    Environment = context.CommerceContext.Environment,
                    Headers = context.CommerceContext.Headers,
                },
                onError: x => error = true,
                onAbort: x =>
                {
                    if (!x.Contains("Ok|", StringComparison.OrdinalIgnoreCase))
                    {
                        error = true;
                    }
                });

                newContext.CommerceContext.AddObject(context.CommerceContext.GetObject<OrderCloudClient>());
                newContext.CommerceContext.AddObject(context.CommerceContext.GetObject<ExportResult>());
                var buyers = context.CommerceContext.GetObjects<Buyer>();
                foreach (var buyer in buyers)
                {
                    newContext.CommerceContext.AddObject(buyer);
                }
                var securityProfiles = context.CommerceContext.GetObjects<SecurityProfile>();
                foreach (var securityProfile in securityProfiles)
                {
                    newContext.CommerceContext.AddObject(securityProfile);
                }

                context.Logger.LogDebug($"{Name}-Exporting buyer: '{domain}'. Environment: {context.CommerceContext.Environment.Name}");
                await Commander.Pipeline<ExportBuyersPipeline>()
                    .RunAsync(
                        new ExportEntitiesArgument(domain, arg),
                        newContext)
                    .ConfigureAwait(false);

                context.CommerceContext.AddUniqueObjectByType(newContext.CommerceContext.GetObject<ExportResult>());
                buyers = newContext.CommerceContext.GetObjects<Buyer>();
                foreach (var buyer in buyers)
                {
                    context.CommerceContext.AddUniqueObject(buyer);
                }
                securityProfiles = newContext.CommerceContext.GetObjects<SecurityProfile>();
                foreach (var securityProfile in securityProfiles)
                {
                    context.CommerceContext.AddUniqueObject(securityProfile);
                }

                if (error)
                {
                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.ExportBuyersFailed,
                            new object[] { Name },
                            $"{Name}: Export Buyers failed.").ConfigureAwait(false),
                        context);
                }
            }

            context.Logger.LogInformation($"{Name}-Exporting Buyers Completed: {(int)itemsProcessed}. Environment: {context.CommerceContext.Environment.Name}");
            return arg;
        }
    }
}