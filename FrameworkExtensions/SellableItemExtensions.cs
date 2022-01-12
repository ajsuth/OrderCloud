// <copyright file="SellableItemExtensions.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Models;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Composer;
using Sitecore.Commerce.Plugin.Views;
using System.Collections.Generic;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions
{
    /// <summary>
    /// Defines extensions for <see cref="SellableItem"/>
    /// </summary>
    public static class SellableItemExtensions
    {
        /// <summary>
        /// Determines if Variants will need to be created for the OrderCloud product. Sellable items with a single variation may be folded into a standalone product
        /// if the variation does not have any variation properties configured.
        /// </summary>
        /// <param name="sellableItem">The <see cref="SellableItem"/>.</param>
        /// <returns>True if there is more than 1 variant or if the sole variant does not have any variation properties configured.</returns>
        public static bool RequiresVariantsForOrderCloud(this SellableItem sellableItem)
        {
            if (sellableItem == null || !sellableItem.HasComponent<ItemVariationsComponent>())
            {
                return false;
            }

            var variationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
            var variations = variationsComponent.GetChildComponents<ItemVariationComponent>();
            if (variations.Count > 1)
            {
                return true;
            }

            var variation = variations[0];
            if (!variation.HasChildComponent<DisplayPropertiesComponent>())
            {
                return false;
            }

            var displayProperties = variation.GetChildComponent<DisplayPropertiesComponent>();

            return !string.IsNullOrWhiteSpace(displayProperties.Color) || !string.IsNullOrWhiteSpace(displayProperties.Size);
        }

        /// <summary>
        /// Determines if the sellable items with a single variation will be folded into a standalone product
        /// if the variation does not have any variation properties configured.
        /// </summary>
        /// <param name="sellableItem">The <see cref="SellableItem"/>.</param>
        /// <returns>True if the sole variant does not have any variation properties configured.</returns>
        public static bool WillFoldIntoStandloneProductForOrderCloud(this SellableItem sellableItem)
        {
            if (sellableItem == null || !sellableItem.HasComponent<ItemVariationsComponent>())
            {
                return false;
            }

            var variationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
            var variations = variationsComponent.GetChildComponents<ItemVariationComponent>();
            if (variations.Count > 1)
            {
                return false;
            }

            var variation = variations[0];
            if (!variation.HasChildComponent<DisplayPropertiesComponent>())
            {
                return true;
            }

            var displayProperties = variation.GetChildComponent<DisplayPropertiesComponent>();

            return string.IsNullOrWhiteSpace(displayProperties.Color) && string.IsNullOrWhiteSpace(displayProperties.Size);
        }

        /// <summary>
        /// Determines which variation properties are being used on the current sellable item.
        /// </summary>
        /// <param name="sellableItem">The <see cref="SellableItem"/>.</param>
        /// <returns>The list of active variation properties.</returns>
        public static List<VariationProperty> GetVariationProperties(this SellableItem sellableItem)
        {
            var variationProperties = new List<VariationProperty>();
            if (sellableItem == null || !sellableItem.HasComponent<ItemVariationsComponent>())
            {
                return new List<VariationProperty>();
            }

            var variationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
            var variations = variationsComponent.GetChildComponents<ItemVariationComponent>();
            foreach (var variation in variations)
            {
                if (!variation.HasChildComponent<DisplayPropertiesComponent>())
                {
                    return new List<VariationProperty>();
                }

                var displayProperties = variation.GetChildComponent<DisplayPropertiesComponent>();

                if (!string.IsNullOrWhiteSpace(displayProperties.Color))
                {
                    variationProperties.Add(new VariationProperty
                    {
                        Name = nameof(displayProperties.Color),
                        Value = displayProperties.Color
                    });
                }

                if (!string.IsNullOrWhiteSpace(displayProperties.Size))
                {
                    variationProperties.Add(new VariationProperty
                    {
                        Name = nameof(displayProperties.Size),
                        Value = displayProperties.Size
                    });
                }
            }

            return variationProperties;
        }

        /// <summary>
        /// Determines which variation properties are being used on the current sellable item.
        /// </summary>
        /// <param name="sellableItem">The <see cref="SellableItem"/>.</param>
        /// <returns>The list of active variation properties.</returns>
        public static VariationsSummary GetVariationsSummary(this SellableItem sellableItem)
        {
            var variationsSummary = new VariationsSummary();
            if (sellableItem == null || !sellableItem.HasComponent<ItemVariationsComponent>())
            {
                return variationsSummary;
            }

            var variationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
            var variations = variationsComponent.GetChildComponents<ItemVariationComponent>();
            foreach (var variation in variations)
            {
                if (!variation.HasChildComponent<DisplayPropertiesComponent>())
                {
                    return new VariationsSummary();
                }

                var displayProperties = variation.GetChildComponent<DisplayPropertiesComponent>();
                var variationSummary = new VariationSummary
                {
                    Id = variation.Id
                };

                if (!string.IsNullOrWhiteSpace(displayProperties.Color))
                {
                    variationsSummary.UniqueProperties.Add(nameof(displayProperties.Color));
                    variationSummary.VariationProperties.Add(new VariationProperty
                    {
                        Name = nameof(displayProperties.Color),
                        Value = displayProperties.Color
                    });
                }

                if (!string.IsNullOrWhiteSpace(displayProperties.Size))
                {
                    variationsSummary.UniqueProperties.Add(nameof(displayProperties.Size));
                    variationSummary.VariationProperties.Add(new VariationProperty
                    {
                        Name = nameof(displayProperties.Size),
                        Value = displayProperties.Size
                    });
                }

                if (variationSummary.VariationProperties.Count > 0)
                {
                    variationsSummary.Variations.Add(variationSummary);
                }
            }

            return variationsSummary;
        }
    }
}
