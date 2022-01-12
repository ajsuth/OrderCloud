// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportSellableItemBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using OrderCloud.SDK;
using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Net;
using System.Threading.Tasks;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using System.Linq;
using System.Collections.Generic;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Plugin.Shops;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;

namespace Ajsuth.Sample.OrderCloud.Engine.Pipelines.Blocks
{
    /// <summary>Defines the asynchronous executing ExportSellableItem pipeline block</summary>
    /// <seealso cref="AsyncPipelineBlock{TInput, TOutput, TContext}" />
    [PipelineDisplayName(OrderCloudConstants.Pipelines.Blocks.ExportSellableItem)]
    public class ExportSellableItemBlock : AsyncPipelineBlock<SellableItem, SellableItem, CommercePipelineExecutionContext>
    {
        /// <summary>Gets or sets the commander.</summary>
        /// <value>The commander.</value>
        protected CommerceCommander Commander { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ExportSellableItemBlock" /> class.</summary>
        /// <param name="commander">The commerce commander.</param>
        public ExportSellableItemBlock(CommerceCommander commander)
        {
            this.Commander = commander;
        }

        /// <summary>Executes the pipeline block's code logic.</summary>
        /// <param name="arg">The pipeline argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="SellableItem"/>.</returns>
        public override async Task<SellableItem> RunAsync(SellableItem sellableItem, CommercePipelineExecutionContext context)
        {
            Condition.Requires(sellableItem).IsNotNull($"{Name}: The sellable item can not be null");

            var client = context.CommerceContext.GetObject<OrderCloudClient>();
            var exportResult = context.CommerceContext.GetObject<ExportResult>();

            var variationPropertyPolicy = context.GetPolicy<VariationPropertyPolicy>();
            // Use reflection to identify property names? It's probably more practical just to code in the variation comparisons
            // variationPropertyPolicy.PropertyNames

            //for variations DisplayPropertiesComponent
            var requiresVariants = sellableItem.RequiresVariantsForOrderCloud();
            var productSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>().ProductSettings;
            var product = await GetOrCreateProduct(client, sellableItem, requiresVariants, productSettings, context, exportResult);
            if (product == null)
            {
                return null;
            }

            if (!requiresVariants)
            {
                return sellableItem;
            }

            var variants = await GetOrCreateVariants(client, sellableItem, product, context, exportResult);
            if (variants == null)
            {
                return null;
            }

            return sellableItem;
        }

        protected async Task<Product> GetOrCreateProduct(OrderCloudClient client, SellableItem sellableItem, bool requiresVariants, SellableItemExportPolicy productSettings, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var productId = sellableItem.FriendlyId.ToValidOrderCloudId();
            try
            {
                var product = await client.Products.GetAsync(productId);
                exportResult.Products.ItemsNotChanged++;

                return product;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var product = new Product
                        {
                            ID = productId,
                            Active = true,
                            Name = sellableItem.DisplayName,
                            Description = sellableItem.Description
                        };

                        product.xp.Brand = sellableItem.Brand;
                        product.xp.Manufacturer = sellableItem.Manufacturer;
                        product.xp.TypeOfGood = sellableItem.TypeOfGood;
                        product.xp.Tags = sellableItem.Tags;

                        var priceSchedules = new List<PriceSchedule>();
                        if (sellableItem.HasPolicy<ListPricingPolicy>())
                        {
                            priceSchedules = await CreatePriceSchedules(client, productId, sellableItem.GetPolicy<ListPricingPolicy>(), context, exportResult);
                            
                            var defaultPriceSchedule = priceSchedules?.FirstOrDefault(p => p.ID.EndsWith($"_{productSettings.DefaultCurrency}"));
                            product.DefaultPriceScheduleID = defaultPriceSchedule?.ID;

                            if (defaultPriceSchedule != null)
                            {
                                priceSchedules.Remove(defaultPriceSchedule);
                            }
                        }

                        if (!productSettings.MultiInventory)
                        {
                            // For single-source inventory we prepare the inventory up front to avoid an additional call to patch the product with inventory information
                            if (requiresVariants)
                            {
                                // Only migrate inventory from variants (sellable items may have inventory if it was assigned prior to creating variants. We do not want to bring these inventory records across to OrderCloud).
                                product.Inventory = new Inventory
                                {
                                    VariantLevelTracking = true
                                };
                                // If requires variants, this will be a post-product creation process, so we won't do anything here.
                            }
                            else
                            {
                                // sellable.ItemVariations will either contain the sole variant where we want to retrieve its inventory information or null which will return the inventory of the sellable item directly.
                                var inventory = await GetInventoryInformation(sellableItem, sellableItem.ItemVariations, productSettings.InventorySetId, context);

                                if (inventory != null)
                                {
                                    product.Inventory = new Inventory
                                    {
                                        Enabled = true,
                                        QuantityAvailable = inventory.Quantity
                                    };
                                }
                            }
                        }

                        context.Logger.LogInformation($"Saving product; Product ID: {product.ID}");
                        product = await client.Products.SaveAsync(productId, product);
                        exportResult.Products.ItemsUpdated++;

                        if (priceSchedules.Any())
                        {
                            // TODO: Create remaining multi-currency price schedules here.
                            // Maybe create a buyer group per currency and have them all default to shop's default currency. When the user wants to change currency, the middleware would need to move the user between groups.

                            // Ensure price schedules are only saved to valid buyer groups. Catalog information required here.

                            //var shop = context.CommerceContext.GetEntity<Shop>();
                            //if (shop.Currencies.FirstOrDefault(c => c.Code == price.CurrencyCode) != null)
                            //{
                            //    context.Logger.LogWarning($"Currency code '{price.CurrencyCode}' not assigned to shop. Price for Product '{id}' will not be migrated.");
                            //    continue;
                            //}
                        }

                        return product;
                    }
                    catch (Exception e)
                    {
                        exportResult.Catalogs.ItemsErrored++;

                        context.Abort(
                            await context.CommerceContext.AddMessage(
                                context.GetPolicy<KnownResultCodes>().Error,
                                OrderCloudConstants.Errors.CreateProductFailed,
                                new object[]
                                {
                                    Name,
                                    productId,
                                    e.Message,
                                    e
                                },
                                $"{Name}: Ok| Create product '{productId}' failed.\n{e.Message}\n{e}").ConfigureAwait(false),
                            context);

                        return null;
                    }
                }
                else
                {
                    exportResult.Catalogs.ItemsErrored++;

                    context.Abort(
                        await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            OrderCloudConstants.Errors.GetProductFailed,
                            new object[]
                            {
                                Name,
                                productId,
                                ex.Message,
                                ex
                            },
                            $"{Name}: Ok| Get product '{productId}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                        context);

                    return null;
                }
            }
        }

        protected async Task<List<Variant>> GetOrCreateVariants(OrderCloudClient client, SellableItem sellableItem, Product product, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                // 1. Create unique specs for product
                var specs = new List<Spec>();
                var variationsSummary = sellableItem.GetVariationsSummary();

                //var variationProperties = sellableItem.GetVariationProperties();
                //var distinctVariationProperties = variationProperties.GroupBy(v => new { v.Name }).Select(g => g.First()).ToList();
                var distinctVariationProperties = variationsSummary.UniqueProperties.Distinct();
                foreach (var variationProperty in distinctVariationProperties)
                {
                    var spec = new Spec
                    {
                        ID = $"{product.ID}_{variationProperty}",
                        Name = variationProperty,
                        Required = true,
                        DefinesVariant = true
                    };

                    exportResult.Specs.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving spec; Spec ID: {spec.ID}");
                    spec = await client.Specs.SaveAsync(spec.ID, spec);
                    exportResult.Specs.ItemsUpdated++;

                    specs.Add(spec);
                }

                // 2. Create spec options
                foreach (var spec in specs)
                {
                    //var specVariationProperties = variationsSummary.Where(v => v.Name == spec.Name).Select(g => g.Value).Distinct();
                    var specVariationProperties = variationsSummary.GetDistinctValues(spec.Name);

                    foreach (var propertyValue in specVariationProperties)
                    {
                        var option = new SpecOption
                        {
                            ID = propertyValue.ToValidOrderCloudId(),
                            Value = propertyValue
                        };

                        exportResult.SpecOptions.ItemsProcessed++;

                        context.Logger.LogInformation($"Saving spec option; Spec ID: {spec.ID}, Option ID: {option.ID}");
                        await client.Specs.SaveOptionAsync(spec.ID, option.ID, option);
                        exportResult.SpecOptions.ItemsUpdated++;
                    }
                }

                // 3. Create spec product assignments
                foreach (var spec in specs)
                {
                    var specProductAssignment = new SpecProductAssignment
                    {
                        SpecID = spec.ID,
                        ProductID = product.ID
                    };

                    exportResult.SpecProductAssignments.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving spec product assignment; Spec ID: {specProductAssignment.SpecID}, Product ID: {specProductAssignment.ProductID}");
                    await client.Specs.SaveProductAssignmentAsync(specProductAssignment);
                    exportResult.SpecProductAssignments.ItemsUpdated++;
                }

                // 4. Generate variants
                await client.Products.GenerateVariantsAsync(product.ID, true);
                var variantList = new List<Variant>();

                // 5. Disable unrequired variants
                var page = 1;
                ListPage<Variant> pagedVariants;
                do
                {
                    pagedVariants = await client.Products.ListVariantsAsync(product.ID, page: page++);
                    var exportPolicy = context.GetPolicy<SellableItemExportPolicy>();
                    foreach (var variant in pagedVariants.Items)
                    {
                        exportResult.Variants.ItemsProcessed++;

                        var matchingVariant = GetVariationSummary(variationsSummary, variant);
                        if (matchingVariant != null)
                        {
                            variantList.Add(variant);

                            var xcVariant = sellableItem.GetVariation(matchingVariant.Id);
                            var displayProperties = xcVariant.GetChildComponent<DisplayPropertiesComponent>();
                            var updatedVariant = new PartialVariant
                            {
                                ID = matchingVariant.Id,
                                Active = !xcVariant.Disabled,
                                Description = displayProperties.DisambiguatingDescription
                            };

                            updatedVariant.xp.Tags = xcVariant.Tags;

                            // 5a. Update variant inventory
                            var inventory = await GetInventoryInformation(sellableItem, matchingVariant.Id, exportPolicy.InventorySetId, context);
                            if (inventory != null)
                            {
                                updatedVariant.Inventory = new VariantInventory
                                {
                                    QuantityAvailable = inventory.Quantity
                                };
                            }

                            // 5b. Update variant pricing
                            if (xcVariant.HasPolicy<ListPricingPolicy>())
                            {
                                var priceSchedules = await CreatePriceSchedules(client, matchingVariant.Id, xcVariant.GetPolicy<ListPricingPolicy>(), context, exportResult);

                                // How would we apply variant specific pricing for multi-currency?
                                // It appears price markups can only support single currency as different currencies may require different results.
                                // Use xp to track the priceschedules?

                                // This is a sample solution and probably not the best solution.
                                updatedVariant.xp.PriceSchedules = priceSchedules.Select(p => p.ID).ToList();
                                
                                //var prices = xcVariant.GetPolicy<ListPricingPolicy>().Prices;
                                //foreach (var price in prices)
                                //{
                                //    var priceSchedule = new PriceSchedule()
                                //    {
                                //        ID = matchingVariant.Id,
                                //        Name = matchingVariant.Id,
                                //        MaxQuantity = int.TryParse(context.GetPolicy<LineQuantityPolicy>().Maximum.ToString(), out int result) ? result : (int?)null,
                                //        UseCumulativeQuantity = context.GetPolicy<RollupCartLinesPolicy>().Rollup,
                                //        PriceBreaks = new List<PriceBreak>()
                                //        {
                                //            new PriceBreak()
                                //            {
                                //                Price = decimal.Parse(price.Amount.ToString()),
                                //                Quantity = 1
                                //            }
                                //        },
                                //        Currency = price.CurrencyCode
                                //    };

                                //    context.Logger.LogInformation($"Saving price schedule; Price Schedule ID: {priceSchedule.ID}");
                                //    await client.PriceSchedules.SaveAsync(priceSchedule.ID, priceSchedule);
                                //}
                            }
                            
                            context.Logger.LogInformation($"Patching variant; Updating inventory and pricing; Product ID: {product.ID}, Variant ID: {variant.ID}");
                            await client.Products.PatchVariantAsync(product.ID, variant.ID, updatedVariant);
                            exportResult.Variants.ItemsPatched++;
                        }
                        else
                        {
                            // 5c. Disable invalid variants
                            var updatedVariant = new PartialVariant
                            {
                                Active = false
                            };

                            context.Logger.LogInformation($"Patching variant; Disabling invalid variants; Product ID: {product.ID}, Variant ID: {variant.ID}");
                            await client.Products.PatchVariantAsync(product.ID, variant.ID, updatedVariant);
                            exportResult.Variants.ItemsPatched++;
                        }
                    }
                } while (pagedVariants != null && pagedVariants.Meta.Page < pagedVariants.Meta.TotalPages);

                return variantList;
            }
            catch (Exception ex)
            {
                exportResult.Variants.ItemsErrored++;

                context.Abort(
                    await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        OrderCloudConstants.Errors.CreateVariantsFailed,
                        new object[]
                        {
                            Name,
                            product.ID,
                            ex.Message,
                            ex
                        },
                        $"{Name}: Ok| Creating variants '{product.ID}' failed.\n{ex.Message}\n{ex}").ConfigureAwait(false),
                    context);

                return null;
            }
        }

        /// <summary>
        /// Gets the Inventory Information for the sellable item or variant, if variationId is provided.
        /// </summary>
        /// <param name="sellableItem"></param>
        /// <param name="variationId"></param>
        /// <param name="inventorySetId"></param>
        /// <param name="context"></param>
        /// <returns>The <see cref="InventoryInformation"/> entry for the sellable item (/variant)</returns>
        protected async Task<InventoryInformation> GetInventoryInformation(SellableItem sellableItem, string variationId, string inventorySetId, CommercePipelineExecutionContext context)
        {
            if (sellableItem == null || !sellableItem.HasComponent<InventoryComponent>(variationId))
            {
                return null;
            }

            var inventoryComponent = sellableItem.GetComponent<InventoryComponent>(variationId);

            var inventoryAssociation =
                inventoryComponent.InventoryAssociations.FirstOrDefault(x =>
                    x.InventorySet.EntityTarget.Equals(
                        inventorySetId.EnsurePrefix(CommerceEntity.IdPrefix<InventorySet>()),
                        StringComparison.OrdinalIgnoreCase));

            var inventoryInformation =
                await Commander.Pipeline<IFindEntityPipeline>()
                    .RunAsync(
                        new FindEntityArgument(typeof(InventoryInformation),
                            inventoryAssociation.InventoryInformation.EntityTarget,
                            inventoryAssociation.InventoryInformation.EntityTargetUniqueId),
                        context.CommerceContext.PipelineContextOptions)
                    .ConfigureAwait(false) as InventoryInformation;

            return inventoryInformation;
        }

        protected VariationSummary GetVariationSummary(VariationsSummary variationsSummary, Variant variant)
        {
            var found = true;
            foreach (var variation in variationsSummary.Variations)
            {
                found = true;
                foreach (var property in variation.VariationProperties)
                {
                    if (variant.Specs.FirstOrDefault(s => s.Name == property.Name && s.Value == property.Value) == null)
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return variation;
                }
            }

            return null;
        }

        protected async Task<List<PriceSchedule>> CreatePriceSchedules(OrderCloudClient client, string id, ListPricingPolicy pricingPolicy, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var priceSchedules = new List<PriceSchedule>();

            var prices = pricingPolicy.Prices;
            foreach (var price in prices)
            {
                var priceSchedule = new PriceSchedule()
                {
                    ID = $"{id}_{price.CurrencyCode}",
                    Name = $"{id}_{price.CurrencyCode}",
                    MaxQuantity = int.TryParse(context.GetPolicy<LineQuantityPolicy>().Maximum.ToString(), out int result) ? result : (int?)null,
                    UseCumulativeQuantity = context.GetPolicy<RollupCartLinesPolicy>().Rollup,
                    PriceBreaks = new List<PriceBreak>()
                        {
                            new PriceBreak()
                            {
                                Price = decimal.Parse(price.Amount.ToString()),
                                Quantity = 1
                            }
                        },
                    Currency = price.CurrencyCode
                };

                try
                {
                    context.Logger.LogInformation($"Saving price schedule; Price Schedule ID: {priceSchedule.ID}");
                    priceSchedule = await client.PriceSchedules.SaveAsync(priceSchedule.ID, priceSchedule);
                    exportResult.PriceSchedules.ItemsUpdated++;
                }
                catch (Exception ex)
                {
                    exportResult.PriceSchedules.ItemsErrored++;
                    context.Logger.LogError($"Saving price schedule failed; Catalog ID: {priceSchedule.ID}\n{ex.Message}\n{ex}");

                    continue;
                }

                priceSchedules.Add(priceSchedule);
            }

            return priceSchedules;
        }
    }
}