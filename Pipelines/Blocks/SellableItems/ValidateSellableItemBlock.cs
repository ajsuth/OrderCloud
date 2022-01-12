// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateSellableItemBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;
using Sitecore.Commerce.Plugin.Catalog;
using System.Collections.Generic;
using Ajsuth.Sample.OrderCloud.Engine.Models;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ValidateSellableItem pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ValidateSellableItem)]
    public class ValidateSellableItemBlock : AsyncPipelineBlock<ExportEntitiesArgument, SellableItem, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ValidateSellableItemBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ValidateSellableItemBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="SellableItem"/>.</returns>
        public override async Task<SellableItem> RunAsync(ExportEntitiesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument cannot be null.");
            Condition.Requires(arg.EntityId).IsNotNull($"{Name}: The entity id cannot be null.");

            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var sellableItem = await Commander.Pipeline<FindEntityPipeline>().RunAsync(new FindEntityArgument(typeof(SellableItem), arg.EntityId), context).ConfigureAwait(false) as SellableItem;
            if (sellableItem == null)
            {
                exportResult.Products.ItemsErrored++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "SellableItemNotFound",
                        new object[] { sellableItem.Id },
                        $"Ok| Sellable Item {sellableItem.Id} was not found.").ConfigureAwait(false),
                    context);

                return null;
            }

            context.CommerceContext.AddUniqueObjectByType(arg);

            context.Logger.LogDebug($"{Name}: Validating sellable item '{sellableItem.Id}'");

            if (sellableItem.IsBundle)
            {
                exportResult.Products.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "BundleNotSupported",
                        new object[] { sellableItem.Id },
                        $"Ok| Sellable Item {sellableItem.Id} is a bundle and is not supported.").ConfigureAwait(false),
                    context);

                return null;
            }

            if (!sellableItem.Published)
            {
                exportResult.Products.ItemsSkipped++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Information,
                        "SellableItemNotPublished",
                        new object[] { sellableItem.Id },
                        $"Ok| Sellable Item {sellableItem.Id} has not been published and will not be migrated.").ConfigureAwait(false),
                    context);

                return null;
            }

            if (!(await ValidateVariants(sellableItem, context, exportResult)))
            {
                return null;
            }

            return sellableItem;
        }

        /// <summary>
        /// Verifies that the sellable item's data model can be migrated to OrderCloud.
        /// </summary>
        /// <param name="sellableItem">The <see cref="SellableItem"/> to validate.</param>
        /// <param name="context">The context.</param>
        /// <returns>True if the sellable item's variants can be migrated to OrderCloud.</returns>
        protected async Task<bool> ValidateVariants(SellableItem sellableItem, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            /* Rules:
             * 1. A sellableItem with a single variant will be converted to a standalone product if no variation properties have been configured on the variant.
             * 2. Populated variation properties must be consistent across variants in order to validate the sellable item.
             * 3. The values of populated variation properties must create a unique variation to validate the sellable item.
             */
            if (!sellableItem.HasComponent<ItemVariationsComponent>())
            {
                return true;
            }

            var variationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
            var variations = variationsComponent.GetChildComponents<ItemVariationComponent>();
            if (variations.Count == 1)
            {
                return true;
            }

            var variantPropertyCombinations = new List<string>();
            var shouldHaveColor = false;
            var shouldHaveSize = false;
            var firstVariant = true;
            foreach (var variation in variations)
            {
                // only 1 variationcomponent can have no matching variation properties, otherwise the product should be flagged as corrupted/customised as this will likely cause errors with the storefront's intended representation.
                // Need to confirm that only unique combinations of variation properties occur within the variants otherwise flagged as corrupted for intended migration.
                // 

                if (!variation.HasChildComponent<DisplayPropertiesComponent>())
                {
                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            "VariationCorrupted",
                            new object[] { sellableItem.Id, variation.Id },
                            $"Ok| Sellable Item {sellableItem.Id} variation {variation.Id} does not have DisplayPropertiesComponent.").ConfigureAwait(false),
                        context);

                    return false;
                }

                var displayProperties = variation.GetChildComponent<DisplayPropertiesComponent>();
                if (firstVariant)
                {
                    firstVariant = false;
                    shouldHaveColor = !string.IsNullOrWhiteSpace(displayProperties.Color);
                    shouldHaveSize = !string.IsNullOrWhiteSpace(displayProperties.Size);
                }
                else
                {
                    if (shouldHaveColor && string.IsNullOrWhiteSpace(displayProperties.Color))
                    {
                        exportResult.Products.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                "VariationPropertiesInconsistent",
                                new object[] { sellableItem.Id, variation.Id, nameof(displayProperties.Color) },
                                $"Ok| Inconsistent variation properties. Sellable Item {sellableItem.Id} variation {variation.Id} does not have variation property {nameof(displayProperties.Color)}.").ConfigureAwait(false),
                            context);

                        return false;
                    }

                    if (shouldHaveSize && string.IsNullOrWhiteSpace(displayProperties.Size))
                    {
                        exportResult.Products.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                "VariationPropertiesInconsistent",
                                new object[] { sellableItem.Id, variation.Id, nameof(displayProperties.Size) },
                                $"Ok| Inconsistent variation properties. Sellable Item {sellableItem.Id} variation {variation.Id} does not have variation property {nameof(displayProperties.Size)}.").ConfigureAwait(false),
                            context);

                        return false;
                    }
                }

                var variationPropertyCombination = shouldHaveColor ? $"{displayProperties.Color}|" : string.Empty + (shouldHaveSize ? $"{displayProperties.Size}|" : string.Empty);
                if (variantPropertyCombinations.Contains(variationPropertyCombination))
                {
                    exportResult.Products.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            "VariationNotUnique",
                            new object[] { sellableItem.Id, variation.Id },
                            $"Ok| Sellable Item {sellableItem.Id} variation {variation.Id} does not have unique variation properties.").ConfigureAwait(false),
                        context);

                    return false;
                }
                else
                {
                    variantPropertyCombinations.Add(variationPropertyCombination);
                }
            }

            return true;
        }
    }
}