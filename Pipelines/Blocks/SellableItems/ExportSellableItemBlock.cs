// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportSellableItemBlock.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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
            var problemObjects = context.CommerceContext.GetObject<ProblemObjects>();

            var variationPropertyPolicy = context.GetPolicy<VariationPropertyPolicy>();
            // Use reflection to identify property names? It's probably more practical just to code in the variation comparisons
            // variationPropertyPolicy.PropertyNames

            var requiresVariants = sellableItem.RequiresVariantsForOrderCloud();
            var productSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>().ProductSettings;

            // 1. Create Product
            var product = await GetOrCreateProduct(client, sellableItem, requiresVariants, productSettings, context, exportResult, problemObjects);
            if (product == null)
            {
                return null;
            }

            if (!requiresVariants)
            {
                if (productSettings.MultiInventory)
                {
                    // 1a. Update Inventory
                    await CreateInventoryRecords(client, sellableItem, product, context, exportResult, problemObjects);
                }
                
                return sellableItem;
            }

            // 2. Create/Update Variants
            var variants = await GetOrCreateVariants(client, sellableItem, product, productSettings, context, exportResult);
            if (variants == null)
            {
                return null;
            }

            if (productSettings.MultiInventory)
            {
                // 2a. Create/Update Variant Inventory
                await CreateVariantsInventoryRecords(client, sellableItem, product, variants, context, exportResult, problemObjects);
            }

            return sellableItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sellableItem"></param>
        /// <param name="requiresVariants"></param>
        /// <param name="productSettings"></param>
        /// <param name="context"></param>
        /// <param name="exportResult"></param>
        /// <param name="problemObjects"></param>
        /// <returns></returns>
        protected async Task<Product> GetOrCreateProduct(
            OrderCloudClient client,
            SellableItem sellableItem,
            bool requiresVariants,
            SellableItemExportPolicy productSettings,
            CommercePipelineExecutionContext context,
            ExportResult exportResult,
            ProblemObjects problemObjects)
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
                        product.xp.Tags = sellableItem.Tags.Select(t => t.Name);

                        if (sellableItem.HasComponent<ItemSpecificationsComponent>())
                        {
                            var specifications = sellableItem.GetComponent<ItemSpecificationsComponent>();

                            product.ShipWeight = Convert.ToDecimal(specifications.Weight);
                            product.ShipHeight = Convert.ToDecimal(specifications.Height);
                            product.ShipWidth = Convert.ToDecimal(specifications.Width);
                            product.ShipLength = Convert.ToDecimal(specifications.Length);
                        }

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

                        if (requiresVariants)
                        {
                            // Only migrate inventory from variants (sellable items may have inventory if it was assigned prior to creating variants. We do not want to bring these inventory records across to OrderCloud).
                            product.Inventory = new Inventory
                            {
                                Enabled = sellableItem.IsPhysicalItem(context),
                                VariantLevelTracking = true
                            };

                            // Variant inventory will need to be created after variants have been generated.
                        }
                        else
                        {
                            if (!productSettings.MultiInventory)
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

                        await CreateProductAssignments(context, client, exportResult, product, priceSchedules);

                        // TODO: Swap condition for multi-currency boolean parameter?
                        if (priceSchedules.Any())
                        {
                            // TODO: Create remaining multi-currency price schedules here.
                            await CreateProductAssignments(context, client, exportResult, product, priceSchedules);
                        }

                        return product;
                    }
                    catch (Exception e)
                    {
                        exportResult.Catalogs.ItemsErrored++;
                        problemObjects.Products.Add(productId);

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
                    problemObjects.Products.Add(productId);

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

        protected async Task<List<InventoryRecord>> CreateInventoryRecords(
            OrderCloudClient client,
            SellableItem sellableItem,
            Product product,
            CommercePipelineExecutionContext context,
            ExportResult exportResult,
            ProblemObjects problemObjects)
        {
            var inventoryRecords = new List<InventoryRecord>();
            var inventoryList = await GetInventoryInformationList(sellableItem, null, context);

            foreach (var inventory in inventoryList)
            {
                var friendlyIdParts = inventory.FriendlyId.Split("-");
                var inventorySetId = friendlyIdParts[0];

                var address = await GetOrCreateAdminAddress(client, inventorySetId, context, exportResult);
                if (address == null)
                {
                    continue;
                }

                var inventoryRecord = await UpdateInventoryRecord(client, inventory, address, product.ID, context, exportResult);
                if (inventoryRecord == null)
                {
                    continue;
                }

                inventoryRecords.Add(inventoryRecord);
            }
            
            return inventoryRecords;
        }

        protected async Task CreateVariantsInventoryRecords(OrderCloudClient client, SellableItem sellableItem, Product product, List<Variant> variants, CommercePipelineExecutionContext context, ExportResult exportResult, ProblemObjects problemObjects)
        {
            foreach (var variant in variants)
            {
                // TODO: Need to get variation based on non-ordercloud friendly ID.
                var xcVariant = sellableItem.GetVariation(variant.ID);
                var inventoryList = await GetInventoryInformationList(sellableItem, xcVariant.Id, context);

                foreach (var inventory in inventoryList)
                {
                    var friendlyIdParts = inventory.FriendlyId.Split("-");
                    var inventorySetId = friendlyIdParts[0];

                    var address = await GetOrCreateAdminAddress(client, inventorySetId, context, exportResult);
                    if (address == null)
                    {
                        continue;
                    }

                    var inventoryRecord = await UpdateVariantInventoryRecord(client, inventory, address, product.ID, variant.ID, context, exportResult);
                    if (inventoryRecord == null)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates an admin address.
        /// </summary>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="inventorySetId">The inventory set identifier.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Buyer"/>.</returns>
        protected async Task<Address> GetOrCreateAdminAddress(OrderCloudClient client, string inventorySetId, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            var addressId = inventorySetId.ToValidOrderCloudId();
            try
            {
                var address = context.CommerceContext.GetObjects<Address>().FirstOrDefault(b => b.ID == addressId);

                if (address != null)
                {
                    return address;
                }

                exportResult.AdminAddresses.ItemsProcessed++;

                address = await client.AdminAddresses.GetAsync(addressId);
                exportResult.AdminAddresses.ItemsNotChanged++;

                context.CommerceContext.AddObject(address);

                return address;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    try
                    {
                        var inventorySet = await Commander.GetEntity<InventorySet>(context.CommerceContext, inventorySetId).ConfigureAwait(false);
                        if (inventorySet == null)
                        {
                            exportResult.AdminAddresses.ItemsErrored++;
                            context.Logger.LogError($"{Name}: Inventory set '{inventorySetId}' not found.");
                        }

                        var address = new Address
                        {
                            ID = addressId,
                            AddressName = inventorySet.DisplayName
                        };
                        address.xp.Description = inventorySet.Description;

                        address = await client.AdminAddresses.SaveAsync(addressId, address);
                        exportResult.AdminAddresses.ItemsCreated++;

                        return address;
                    }
                    catch (Exception e)
                    {
                        exportResult.AdminAddresses.ItemsErrored++;
                        context.Logger.LogError($"{Name}: Create admin address '{addressId}' failed.\n{e.Message}\n{e}");
                    }
                }
                else
                {
                    exportResult.AdminAddresses.ItemsErrored++;
                    context.Logger.LogError($"{Name}: Get admin address '{addressId}' failed.\n{ex.Message}\n{ex}");
                }
            }

            return null;
        }

        protected async Task<InventoryRecord> UpdateInventoryRecord(
            OrderCloudClient client,
            InventoryInformation inventory,
            Address address,
            string productId,
            CommercePipelineExecutionContext context,
            ExportResult exportResult)
        {
            var inventoryRecordId = inventory.FriendlyId.ToValidOrderCloudId();

            try
            {
                var inventoryRecord = new InventoryRecord
                {
                    ID = inventoryRecordId,
                    AddressID = address.ID,
                    QuantityAvailable = inventory.Quantity
                };

                exportResult.InventoryRecords.ItemsProcessed++;
                inventoryRecord = await client.InventoryRecords.SaveAsync(productId, inventoryRecord.ID, inventoryRecord);
                exportResult.InventoryRecords.ItemsUpdated++;

                return inventoryRecord;
            }
            catch (Exception ex)
            {
                exportResult.InventoryRecords.ItemsErrored++;
                context.Logger.LogError($"{Name}: Exporting inventory record '{inventoryRecordId}' failed.\n{ex.Message}\n{ex}");

                return null;
            }
        }

        protected async Task<InventoryRecord> UpdateVariantInventoryRecord(
            OrderCloudClient client,
            InventoryInformation inventory,
            Address address,
            string productId,
            string variantId,
            CommercePipelineExecutionContext context,
            ExportResult exportResult)
        {
            var inventoryRecordId = inventory.FriendlyId.ToValidOrderCloudId();

            try
            {
                var inventoryRecord = new InventoryRecord
                {
                    ID = inventoryRecordId,
                    AddressID = address.ID,
                    QuantityAvailable = inventory.Quantity
                };

                exportResult.InventoryRecords.ItemsProcessed++;
                inventoryRecord = await client.InventoryRecords.SaveVariantAsync(productId, variantId, inventoryRecord.ID, inventoryRecord);
                exportResult.InventoryRecords.ItemsUpdated++;

                return inventoryRecord;
            }
            catch (Exception ex)
            {
                exportResult.InventoryRecords.ItemsErrored++;
                context.Logger.LogError($"{Name}: Exporting inventory record '{inventoryRecordId}' failed.\n{ex.Message}\n{ex}");

                return null;
            }
        }

        /// <summary>
        /// Creates product assignments representing multi-currency.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="client"></param>
        /// <param name="exportResult"></param>
        /// <param name="product"></param>
        /// <param name="priceSchedules"></param>
        /// <returns></returns>
        protected async Task CreateProductAssignments(CommercePipelineExecutionContext context, OrderCloudClient client, ExportResult exportResult, Product product, List<PriceSchedule> priceSchedules)
        {
            // Note: Currently relies on all buyers and currencies being passed under the BuyerSettings
            // TODO: Replace buyers and user group ids with BuyerSettings
            var buyerIds = new List<string>() { "Storefront" };
            // We will have buyer groups per currency. All Buyer Users will be assigned to the buyer group of the default currency for Buyers?
            foreach (var priceSchedule in priceSchedules)
            {
                foreach (var buyerId in buyerIds)
                {
                    var userGroupId = $"{buyerId}_{priceSchedule.Currency}";
                    try
                    {
                        var productAssignment = new ProductAssignment
                        {
                            ProductID = product.ID,
                            BuyerID = buyerId,
                            UserGroupID = userGroupId
                        };

                        exportResult.ProductAssignments.ItemsProcessed++;

                        context.Logger.LogInformation($"Saving product assignment; Product ID: {product.ID}, Buyer ID: {buyerId}, User Group ID: {userGroupId}");
                        await client.Products.SaveAssignmentAsync(productAssignment);
                        exportResult.ProductAssignments.ItemsCreated++;
                    }
                    catch (Exception ex)
                    {
                        exportResult.ProductAssignments.ItemsErrored++;
                        context.Logger.LogError($"Saving catalog assignment failed; Product ID: {product.ID}, Buyer ID: {buyerId}, User Group ID: {userGroupId}\n{ex.Message}\n{ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Creates variants for a product.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sellableItem"></param>
        /// <param name="product"></param>
        /// <param name="productSettings"></param>
        /// <param name="context"></param>
        /// <param name="exportResult"></param>
        /// <returns></returns>
        protected async Task<List<Variant>> GetOrCreateVariants(OrderCloudClient client, SellableItem sellableItem, Product product, SellableItemExportPolicy productSettings, CommercePipelineExecutionContext context, ExportResult exportResult)
        {
            try
            {
                var variationsSummary = sellableItem.GetVariationsSummary();

                // 1. Create unique specs for product
                var specs = await ConstructAndSaveSpecs(context, client, product, variationsSummary, exportResult);

                // 2. Create spec options
                await ConstructAndSaveSpecOptions(context, client, specs, variationsSummary, exportResult);

                // 3. Create spec product assignments
                await ConstructAndSaveSpecProductAssignments(context, client, product, specs, exportResult);

                // 4. Generate variants
                await client.Products.GenerateVariantsAsync(product.ID, true);

                // 5. Update generated variants
                var variantList = await UpdateVariants(context, client, sellableItem, product, variationsSummary, productSettings,exportResult);
                
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
        /// Creates or updates product specs.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="product">The product that will be used for the Spec ID prefix.</param>
        /// <param name="variationsSummary">The XC variations summary that will be converted to specs.</param>
        /// <param name="exportResult"></param>
        /// <returns>The list of <see cref="Spec"/>s.</returns>
        protected async Task<List<Spec>> ConstructAndSaveSpecs(
            CommercePipelineExecutionContext context,
            OrderCloudClient client,
            Product product,
            VariationsSummary variationsSummary,
            ExportResult exportResult)
        {
            var specs = new List<Spec>();
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

            // await Throttler.RunAsync(specs, 100, 20, spec => client.Specs.SaveAsync(spec.ID, spec));

            return specs;
        }

        /// <summary>
        /// Creates or updates product spec options.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="specs">The list of specs that will have options created.</param>
        /// <param name="variationsSummary">The XC variations summary that will be converted to specs.</param>
        /// <param name="exportResult"></param>
        /// <returns></returns>
        protected async Task ConstructAndSaveSpecOptions(
            CommercePipelineExecutionContext context,
            OrderCloudClient client,
            List<Spec> specs,
            VariationsSummary variationsSummary,
            ExportResult exportResult)
        {
            foreach (var spec in specs)
            {
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

                // await Throttler.RunAsync(specOptions, 100, 20, spec => client.Specs.SaveOptionAsync(spec.ID, specOption.ID, specOption));
            }
        }

        /// <summary>
        /// Creates or updates product spec options.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="product">The product to be associated to the specs.</param>
        /// <param name="specs">The list of specs that will have options created.</param>
        /// <param name="exportResult"></param>
        /// <returns></returns>
        protected async Task ConstructAndSaveSpecProductAssignments(
            CommercePipelineExecutionContext context,
            OrderCloudClient client,
            Product product,
            List<Spec> specs,
            ExportResult exportResult)
        {
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

            // await Throttler.RunAsync(specOptions, 100, 20, specProductAssignments => client.Specs.SaveProductAssignmentAsync(specProductAssignment));
        }

        /// <summary>
        /// Updates variants with inventory, pricing, etc.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="client">The <see cref="OrderCloudClient"/>.</param>
        /// <param name="sellableItem"></param>
        /// <param name="product">The product to be associated to the specs.</param>
        /// <param name="variationsSummary"></param>
        /// <param name="productSettings"></param>
        /// <param name="exportResult"></param>
        /// <returns></returns>
        protected async Task<List<Variant>> UpdateVariants(
            CommercePipelineExecutionContext context,
            OrderCloudClient client,
            SellableItem sellableItem,
            Product product,
            VariationsSummary variationsSummary,
            SellableItemExportPolicy productSettings,
            ExportResult exportResult)
        {
            var variantList = new List<Variant>();
            var page = 1;
            ListPage<Variant> pagedVariants;

            do
            {
                pagedVariants = await client.Products.ListVariantsAsync(product.ID, page: page++);

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
                            ID = matchingVariant.Id.ToValidOrderCloudId(),
                            Active = !xcVariant.Disabled,
                            Description = displayProperties.DisambiguatingDescription
                        };

                        updatedVariant.xp = new ExpandoObject();
                        updatedVariant.xp.Tags = xcVariant.Tags.Select(t => t.Name);

                        if (xcVariant.HasChildComponent<ItemSpecificationsComponent>())
                        {
                            var specifications = xcVariant.GetChildComponent<ItemSpecificationsComponent>();

                            updatedVariant.ShipWeight = Convert.ToDecimal(specifications.Weight);
                            updatedVariant.ShipHeight = Convert.ToDecimal(specifications.Height);
                            updatedVariant.ShipWidth = Convert.ToDecimal(specifications.Width);
                            updatedVariant.ShipLength = Convert.ToDecimal(specifications.Length);
                        }

                        // 5a. Update variant inventory
                        if (!productSettings.MultiInventory)
                        {
                            var inventory = await GetInventoryInformation(sellableItem, matchingVariant.Id, productSettings.InventorySetId, context);
                            if (inventory != null)
                            {
                                updatedVariant.Inventory = new VariantInventory
                                {
                                    QuantityAvailable = inventory.Quantity
                                };
                            }
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
                            ID = variant.ID,
                            Active = false
                        };

                        context.Logger.LogInformation($"Patching variant; Disabling invalid variants; Product ID: {product.ID}, Variant ID: {variant.ID}");
                        await client.Products.PatchVariantAsync(product.ID, variant.ID, updatedVariant);
                        exportResult.Variants.ItemsPatched++;
                    }

                    // await Throttler.RunAsync(updatedVariants, 100, 20, updatedVariant => client.Products.PatchVariantAsync(product.ID, updatedVariant.ID, updatedVariant));
                }
            } while (pagedVariants != null && pagedVariants.Meta.Page < pagedVariants.Meta.TotalPages);

            return variantList;
        }

        /// <summary>
        /// Gets the Inventory Information for the sellable item or variant, if variationId is provided.
        /// </summary>
        /// <param name="sellableItem"></param>
        /// <param name="variationId"></param>
        /// <param name="inventorySetId"></param>
        /// <param name="context"></param>
        /// <returns>The <see cref="InventoryInformation"/> entry for the sellable item / variant.</returns>
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

        /// <summary>
        /// Gets the Inventory Information for the sellable item or variant, if variationId is provided.
        /// </summary>
        /// <param name="sellableItem"></param>
        /// <param name="variationId"></param>
        /// <param name="inventorySetId"></param>
        /// <param name="context"></param>
        /// <returns>The list of <see cref=InventoryInformation"/> entries for the sellable item / variant.</returns>
        protected async Task<List<InventoryInformation>> GetInventoryInformationList(SellableItem sellableItem, string variationId, CommercePipelineExecutionContext context)
        {
            var inventoryList = new List<InventoryInformation>();

            if (sellableItem == null || !sellableItem.HasComponent<InventoryComponent>(variationId))
            {
                return inventoryList;
            }

            var inventoryComponent = sellableItem.GetComponent<InventoryComponent>(variationId);

            var inventoryInformationIds = inventoryComponent.InventoryAssociations.Select(x => x.InventorySet.EntityTarget);

            foreach (var inventoryInformationId in inventoryInformationIds)
            {
                var inventoryInformation =
                    await Commander.Pipeline<IFindEntityPipeline>()
                        .RunAsync(
                            new FindEntityArgument(typeof(InventoryInformation),
                                inventoryInformationId,
                                null),
                            context.CommerceContext.PipelineContextOptions)
                        .ConfigureAwait(false) as InventoryInformation;

                if (inventoryInformation == null)
                {
                    context.Logger.LogError($"Could not retrieve inventory information for Inventory Information ID: {inventoryInformationId}");
                    continue;
                }

                inventoryList.Add(inventoryInformation);
            }

            return inventoryList;
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
                    exportResult.PriceSchedules.ItemsProcessed++;

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