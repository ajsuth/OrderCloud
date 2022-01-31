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
        /// <summary>Gets or sets the commerce commander.</summary>
        protected CommerceCommander Commander { get; set; }

        /// <summary>The OrderCloud client.</summary>
        protected OrderCloudClient Client { get; set; }

        /// <summary>The export result model.</summary>
        protected ExportResult Result { get; set; }

        /// <summary>The problem objects model.</summary>
        protected ProblemObjects ProblemObjects { get; set; }

        /// <summary>The buyer settings.</summary>
        protected List<CustomerExportPolicy> BuyerSettings { get; set; }

        /// <summary>The product settings.</summary>
        protected SellableItemExportPolicy ProductSettings { get; set; }

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

            Client = context.CommerceContext.GetObject<OrderCloudClient>();
            Result = context.CommerceContext.GetObject<ExportResult>();
            ProblemObjects = context.CommerceContext.GetObject<ProblemObjects>();
            BuyerSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>().BuyerSettings;
            ProductSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>().ProductSettings;
            var processSettings = context.CommerceContext.GetObject<ExportEntitiesArgument>().ProcessSettings;

            var requiresVariants = sellableItem.RequiresVariantsForOrderCloud();

            // 1. Create Product
            Product product;
            if (processSettings.ImportType == "CREATE")
            {
                product = await GetOrCreateProduct(context, sellableItem, requiresVariants);
            }
            else
            {
                product = await CreateOrUpdateProduct(context, sellableItem, requiresVariants);
            }

            if (product == null)
            {
                return null;
            }

            if (!requiresVariants)
            {
                if (ProductSettings.MultiInventory)
                {
                    // 1a. Update Inventory
                    await CreateOrUpdateProductInventoryRecords(context, sellableItem, product);
                }
                
                return sellableItem;
            }

            // 2. Create/Update Variants
            var variants = await GetOrCreateVariants(context, sellableItem, product);
            if (variants == null)
            {
                return null;
            }

            if (ProductSettings.MultiInventory)
            {
                // 2a. Create/Update Variant Inventory
                await CreateOrUpdateVariantsInventoryRecords(context, sellableItem, product, variants);
            }

            return sellableItem;
        }

        /// <summary>
        /// Gets or creates the product.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sellableItem">The XC sellable item.</param>
        /// <param name="requiresVariants">The flag identfying if the OC Product will require variants.</param>
        /// <returns>The OC <see cref="Product"/>.</returns>
        protected async Task<Product> GetOrCreateProduct(CommercePipelineExecutionContext context, SellableItem sellableItem, bool requiresVariants)
        {
            var productId = sellableItem.FriendlyId.ToValidOrderCloudId();
            try
            {
                var product = await Client.Products.GetAsync(productId);
                Result.Products.ItemsNotChanged++;

                return product;
            }
            catch (OrderCloudException ex)
            {
                if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                {
                    return await CreateOrUpdateProduct(context, sellableItem, requiresVariants);
                }
                else
                {
                    Result.Products.ItemsErrored++;
                    ProblemObjects.Products.Add(productId);

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

        protected async Task<Product> CreateOrUpdateProduct(CommercePipelineExecutionContext context, SellableItem sellableItem, bool requiresVariants)
        {
            var productId = sellableItem.FriendlyId.ToValidOrderCloudId();
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
                    priceSchedules = await CreateOrUpdatePriceSchedules(context, productId, sellableItem.GetPolicy<ListPricingPolicy>());

                    var defaultPriceSchedule = priceSchedules?.FirstOrDefault(p => p.ID.EndsWith($"_{ProductSettings.DefaultCurrency}"));
                    product.DefaultPriceScheduleID = defaultPriceSchedule?.ID;
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
                    if (!ProductSettings.MultiInventory)
                    {
                        // sellable.ItemVariations will either contain the sole variant where we want to retrieve its inventory information or null which will return the inventory of the sellable item directly.
                        var inventory = await GetInventoryInformation(context, sellableItem, sellableItem.ItemVariations, ProductSettings.InventorySetId);

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
                product = await Client.Products.SaveAsync(productId, product);
                Result.Products.ItemsUpdated++;

                // Cannot create product assignments here because products have not yet been assigned to a catalog/category
                //await CreateProductAssignments(context, product, priceSchedules);

                return product;
            }
            catch (Exception e)
            {
                Result.Products.ItemsErrored++;
                ProblemObjects.Products.Add(productId);

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

        /// <summary>
        /// Creates or updates product inventory records.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sellableItem">The XC <see cref="SellableItem"/> to retrieve XC inventory information from.</param>
        /// <param name="product">The OC <see cref="Product"/> to create OC inventory records for.</param>
        /// <returns>The list of OC <see cref="InventoryRecord"/>'s to be explicitly assigned to the product.</returns>
        protected async Task<List<InventoryRecord>> CreateOrUpdateProductInventoryRecords(CommercePipelineExecutionContext context, SellableItem sellableItem, Product product)
        {
            var inventoryRecords = new List<InventoryRecord>();
            var inventoryList = await GetInventoryInformationList(context, sellableItem, sellableItem.ItemVariations);

            foreach (var inventory in inventoryList)
            {
                var friendlyIdParts = inventory.FriendlyId.Split("-");
                var inventorySetId = friendlyIdParts[0];

                var address = await GetOrCreateAdminAddress(context, inventorySetId);
                if (address == null)
                {
                    continue;
                }

                var inventoryRecord = await CreateOrUpdateProductInventoryRecord(context, inventory, address, product.ID);
                if (inventoryRecord == null)
                {
                    continue;
                }

                inventoryRecords.Add(inventoryRecord);
            }
            
            return inventoryRecords;
        }

        /// <summary>
        /// Creates or updates variants inventory records.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sellableItem">The XC <see cref="SellableItem"/> to retrieve XC inventory information from.</param>
        /// <param name="product">The OC <see cref="Product"/> to identify the resource.</param>
        /// <param name="variants">The list of OC <see cref="Variant"/>'s to create OC inventory records for.</param>
        /// <returns>The list of OC <see cref="InventoryRecord"/>'s to be explicitly assigned to the variants.</returns>
        protected async Task CreateOrUpdateVariantsInventoryRecords(CommercePipelineExecutionContext context, SellableItem sellableItem, Product product, List<Variant> variants2)
        {
            var variants = sellableItem.GetVariations();
            foreach (var variant in variants)
            {
                // Cannot use OC Variants 
                //var xcVariant = sellableItem.GetVariation(variant.ID);
                var inventoryList = await GetInventoryInformationList(context, sellableItem, variant.Id);
                var variantId = variant.Id.ToValidOrderCloudId();
                foreach (var inventory in inventoryList)
                {
                    var friendlyIdParts = inventory.FriendlyId.Split("-");
                    var inventorySetId = friendlyIdParts[0];

                    var address = await GetOrCreateAdminAddress(context, inventorySetId);
                    if (address == null)
                    {
                        continue;
                    }

                    var inventoryRecord = await CreateOrUpdateVariantInventoryRecord(context, inventory, address, product.ID, variantId);
                    if (inventoryRecord == null)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates an admin address to represent an inventory location.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="inventorySetId">The inventory set identifier.</param>
        /// <returns>The OC <see cref="Address"/> representing the inventory location.</returns>
        protected async Task<Address> GetOrCreateAdminAddress(CommercePipelineExecutionContext context, string inventorySetId)
        {
            var addressId = inventorySetId.ToValidOrderCloudId();
            try
            {
                var address = context.CommerceContext.GetObjects<Address>().FirstOrDefault(b => b.ID == addressId);

                if (address != null)
                {
                    return address;
                }

                Result.AdminAddresses.ItemsProcessed++;

                address = await Client.AdminAddresses.GetAsync(addressId);
                Result.AdminAddresses.ItemsNotChanged++;

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
                            Result.AdminAddresses.ItemsErrored++;
                            context.Logger.LogError($"{Name}: Inventory set '{inventorySetId}' not found.");
                        }

                        var address = new Address
                        {
                            ID = addressId,
                            AddressName = inventorySet.DisplayName,
                            Street1 = "dummy value",
                            City = "dummy value",
                            State = "dummy value",
                            Zip = "dummy value",
                            Country = "NA" // 2 character max
                        };
                        address.xp.Description = inventorySet.Description;

                        context.Logger.LogInformation($"Saving admin address; Address ID: {addressId}");
                        address = await Client.AdminAddresses.SaveAsync(addressId, address);
                        Result.AdminAddresses.ItemsCreated++;

                        return address;
                    }
                    catch (Exception e)
                    {
                        Result.AdminAddresses.ItemsErrored++;
                        context.Logger.LogError($"{Name}: Create admin address '{addressId}' failed.\n{e.Message}\n{e}");
                    }
                }
                else
                {
                    Result.AdminAddresses.ItemsErrored++;
                    context.Logger.LogError($"{Name}: Get admin address '{addressId}' failed.\n{ex.Message}\n{ex}");
                }
            }

            return null;
        }

        /// <summary>
        /// Creates or updates the product inventory record.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="inventory">The XC inventory information.</param>
        /// <param name="address">The OC address (inventory set).</param>
        /// <param name="productId">The OC product identifier.</param>
        /// <returns>The OC <see cref="InventoryRecord"/>.</returns>
        protected async Task<InventoryRecord> CreateOrUpdateProductInventoryRecord(CommercePipelineExecutionContext context, InventoryInformation inventory, Address address, string productId)
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

                Result.InventoryRecords.ItemsProcessed++;

                context.Logger.LogInformation($"Saving product inventory record; Product ID: {productId}, Inventory Record ID: {inventoryRecord.ID}");
                inventoryRecord = await Client.InventoryRecords.SaveAsync(productId, inventoryRecord.ID, inventoryRecord);
                Result.InventoryRecords.ItemsUpdated++;

                return inventoryRecord;
            }
            catch (Exception ex)
            {
                Result.InventoryRecords.ItemsErrored++;
                context.Logger.LogError($"{Name}: Exporting inventory record '{inventoryRecordId}' failed.\n{ex.Message}\n{ex}");

                return null;
            }
        }

        /// <summary>
        /// Creates or updates the variant inventory record.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="inventory">The XC inventory information.</param>
        /// <param name="address">The OC address (inventory set).</param>
        /// <param name="productId">The OC product identifier.</param>
        /// <param name="variantId">The OC variant identifier.</param>
        /// <returns>The OC <see cref="InventoryRecord"/>.</returns>
        protected async Task<InventoryRecord> CreateOrUpdateVariantInventoryRecord(
            CommercePipelineExecutionContext context,
            InventoryInformation inventory,
            Address address,
            string productId,
            string variantId)
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

                Result.InventoryRecords.ItemsProcessed++;

                context.Logger.LogInformation($"Saving variant inventory record; Product ID: {productId}, Variant ID: {variantId}, Inventory Record ID: {inventoryRecord.ID}");
                inventoryRecord = await Client.InventoryRecords.SaveVariantAsync(productId, variantId, inventoryRecord.ID, inventoryRecord);
                Result.InventoryRecords.ItemsUpdated++;

                return inventoryRecord;
            }
            catch (Exception ex)
            {
                Result.InventoryRecords.ItemsErrored++;
                context.Logger.LogError($"{Name}: Exporting inventory record failed. Product ID: {productId}, Variant ID: {variantId}, Inventory Record ID: {inventoryRecordId}\n{ex.Message}\n{ex}");

                return null;
            }
        }

        /// <summary>
        /// Creates product assignments representing multi-currency.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="product">The OC product.</param>
        /// <param name="priceSchedules">The OC price schedules created for the product.</param>
        /// <returns></returns>
        protected async Task CreateProductAssignments(CommercePipelineExecutionContext context, Product product, List<PriceSchedule> priceSchedules)
        {
            foreach (var buyer in BuyerSettings)
            {
                foreach (var currency in buyer.Currencies)
                {
                    var priceSchedule = priceSchedules.FirstOrDefault(p => p.Currency.EqualsOrdinalIgnoreCase(currency));
                    if (priceSchedule == null) {
                        continue;
                    }

                    var userGroupId = $"{buyer.Id}_{currency}";
                    try
                    {
                        var productAssignment = new ProductAssignment
                        {
                            ProductID = product.ID,
                            BuyerID = buyer.Id,
                            UserGroupID = userGroupId,
                            PriceScheduleID = priceSchedule.ID
                        };

                        Result.ProductAssignments.ItemsProcessed++;

                        context.Logger.LogInformation($"Saving product assignment; Product ID: {product.ID}, Buyer ID: {buyer.Id}, User Group ID: {userGroupId}");
                        await Client.Products.SaveAssignmentAsync(productAssignment);
                        Result.ProductAssignments.ItemsCreated++;
                    }
                    catch (Exception ex)
                    {
                        Result.ProductAssignments.ItemsErrored++;
                        context.Logger.LogError($"Saving product assignment failed; Product ID: {product.ID}, Buyer ID: {buyer.Id}, User Group ID: {userGroupId}\n{ex.Message}\n{ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Creates variants for a product.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sellableItem">The XC sellable item used to enrich OC variant data and disable invalid variants.</param>
        /// <param name="product">The OC product the variants will be created for.</param>
        /// <returns>The list of OC <see cref="Variant"/>s created/</returns>
        protected async Task<List<Variant>> GetOrCreateVariants(CommercePipelineExecutionContext context, SellableItem sellableItem, Product product)
        {
            try
            {
                var variationsSummary = sellableItem.GetVariationsSummary();

                // 1. Create unique specs for product
                var specs = await ConstructAndSaveSpecs(context, product, variationsSummary);

                // 2. Create spec options
                await ConstructAndSaveSpecOptions(context, specs, variationsSummary);

                // 3. Create spec product assignments
                await ConstructAndSaveSpecProductAssignments(context, product, specs);

                // 4. Generate variants
                context.Logger.LogInformation($"Generating variants; Product ID: {product.ID}");
                await Client.Products.GenerateVariantsAsync(product.ID, true);

                // 5. Update generated variants
                var variantList = await UpdateVariants(context, sellableItem, product, variationsSummary);
                
                return variantList;
            }
            catch (Exception ex)
            {
                Result.Variants.ItemsErrored++;

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
        /// <param name="product">The product that will be used for the Spec ID prefix.</param>
        /// <param name="variationsSummary">The XC variations summary that will be converted to specs.</param>
        /// <returns>The list of <see cref="Spec"/>s.</returns>
        protected async Task<List<Spec>> ConstructAndSaveSpecs(CommercePipelineExecutionContext context, Product product, VariationsSummary variationsSummary)
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

                Result.Specs.ItemsProcessed++;

                context.Logger.LogInformation($"Saving spec; Spec ID: {spec.ID}");
                spec = await Client.Specs.SaveAsync(spec.ID, spec);
                Result.Specs.ItemsUpdated++;

                specs.Add(spec);
            }

            // await Throttler.RunAsync(specs, 100, 20, spec => client.Specs.SaveAsync(spec.ID, spec));

            return specs;
        }

        /// <summary>
        /// Creates or updates product spec options.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="specs">The list of specs that will have options created.</param>
        /// <param name="variationsSummary">The XC variations summary that will be converted to specs.</param>
        /// <returns></returns>
        protected async Task ConstructAndSaveSpecOptions(CommercePipelineExecutionContext context, List<Spec> specs, VariationsSummary variationsSummary)
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

                    Result.SpecOptions.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving spec option; Spec ID: {spec.ID}, Option ID: {option.ID}");
                    await Client.Specs.SaveOptionAsync(spec.ID, option.ID, option);
                    Result.SpecOptions.ItemsUpdated++;
                }

                // await Throttler.RunAsync(specOptions, 100, 20, spec => client.Specs.SaveOptionAsync(spec.ID, specOption.ID, specOption));
            }
        }

        /// <summary>
        /// Creates or updates product spec options.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="product">The product to be associated to the specs.</param>
        /// <param name="specs">The list of specs that will have options created.</param>
        /// <returns></returns>
        protected async Task ConstructAndSaveSpecProductAssignments(CommercePipelineExecutionContext context, Product product, List<Spec> specs)
        {
            foreach (var spec in specs)
            {
                var specProductAssignment = new SpecProductAssignment
                {
                    SpecID = spec.ID,
                    ProductID = product.ID
                };

                Result.SpecProductAssignments.ItemsProcessed++;

                context.Logger.LogInformation($"Saving spec product assignment; Spec ID: {specProductAssignment.SpecID}, Product ID: {specProductAssignment.ProductID}");
                await Client.Specs.SaveProductAssignmentAsync(specProductAssignment);
                Result.SpecProductAssignments.ItemsUpdated++;
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
        protected async Task<List<Variant>> UpdateVariants(CommercePipelineExecutionContext context, SellableItem sellableItem, Product product, VariationsSummary variationsSummary)
        {
            var variantList = new List<Variant>();
            var page = 1;
            ListPage<Variant> pagedVariants;

            do
            {
                pagedVariants = await Client.Products.ListVariantsAsync(product.ID, page: page++);

                foreach (var variant in pagedVariants.Items)
                {
                    Result.Variants.ItemsProcessed++;

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
                        if (!ProductSettings.MultiInventory)
                        {
                            var inventory = await GetInventoryInformation(context, sellableItem, matchingVariant.Id, ProductSettings.InventorySetId);
                            if (inventory != null)
                            {
                                updatedVariant.Inventory = new VariantInventory
                                {
                                    QuantityAvailable = inventory.Quantity
                                };
                            }
                        }

                        // 5b. Update variant pricing
                        updatedVariant.xp.PriceSchedules = null;
                        if (xcVariant.HasPolicy<ListPricingPolicy>())
                        {
                            // TODO: create price markups

                            // How would we apply variant specific pricing for multi-currency?
                            // It appears price markups can only support single currency as different currencies may require different results.
                            // Use xp to track the priceschedules?

                            // This is a sample workaround solution and probably not the best solution.
                            var priceSchedules = await CreateOrUpdatePriceSchedules(context, matchingVariant.Id, xcVariant.GetPolicy<ListPricingPolicy>());
                            updatedVariant.xp.PriceSchedules = priceSchedules.Select(p => p.ID).ToList();
                        }

                        context.Logger.LogInformation($"Patching variant; Updating inventory and pricing; Product ID: {product.ID}, Variant ID: {variant.ID}");
                        await Client.Products.PatchVariantAsync(product.ID, variant.ID, updatedVariant);
                        Result.Variants.ItemsPatched++;
                    }
                    else
                    {
                        // 5c. Disable invalid variants
                        var updatedVariant = new PartialVariant
                        {
                            ID = variant.ID,
                            Active = false
                        };
                        updatedVariant.xp = new ExpandoObject();
                        updatedVariant.xp.Tags = null;
                        updatedVariant.xp.PriceSchedules = null;

                        context.Logger.LogInformation($"Patching variant; Disabling invalid variants; Product ID: {product.ID}, Variant ID: {variant.ID}");
                        await Client.Products.PatchVariantAsync(product.ID, variant.ID, updatedVariant);
                        Result.Variants.ItemsPatched++;
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
        protected async Task<InventoryInformation> GetInventoryInformation(CommercePipelineExecutionContext context, SellableItem sellableItem, string variationId, string inventorySetId)
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
        protected async Task<List<InventoryInformation>> GetInventoryInformationList(CommercePipelineExecutionContext context, SellableItem sellableItem, string variationId = null)
        {
            var inventoryList = new List<InventoryInformation>();

            if (sellableItem == null || !sellableItem.HasComponent<InventoryComponent>(variationId))
            {
                return inventoryList;
            }

            var inventoryComponent = sellableItem.GetComponent<InventoryComponent>(variationId);

            var inventoryInformationIds = inventoryComponent.InventoryAssociations.Select(x => x.InventoryInformation.EntityTarget);

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

        /// <summary>
        /// Get the variation summary for a the variant.
        /// </summary>
        /// <param name="variationsSummary"></param>
        /// <param name="variant"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates or updates price schedules from XC sellable item's <see cref="ListPricePolicy"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="productId">The OC product identifier.</param>
        /// <param name="pricingPolicy">The XC sellable item's <see cref="ListPricePolicy"/>.</param>
        /// <returns>The list of OC <see cref="PriceSchedule"/>'s created for the product.</returns>
        protected async Task<List<PriceSchedule>> CreateOrUpdatePriceSchedules(CommercePipelineExecutionContext context, string productId, ListPricingPolicy pricingPolicy)
        {
            var priceSchedules = new List<PriceSchedule>();

            var prices = pricingPolicy.Prices;
            foreach (var price in prices)
            {
                var priceSchedule = new PriceSchedule()
                {
                    ID = $"{productId}_{price.CurrencyCode}",
                    Name = $"{productId}_{price.CurrencyCode}",
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
                    Result.PriceSchedules.ItemsProcessed++;

                    context.Logger.LogInformation($"Saving price schedule; Price Schedule ID: {priceSchedule.ID}");
                    priceSchedule = await Client.PriceSchedules.SaveAsync(priceSchedule.ID, priceSchedule);
                    Result.PriceSchedules.ItemsUpdated++;
                }
                catch (Exception ex)
                {
                    Result.PriceSchedules.ItemsErrored++;
                    context.Logger.LogError($"Saving price schedule failed; Catalog ID: {priceSchedule.ID}\n{ex.Message}\n{ex}");

                    continue;
                }

                priceSchedules.Add(priceSchedule);
            }

            return priceSchedules;
        }
    }
}