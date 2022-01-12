// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SellableItemExportPolicy.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Policies
{
    /// <summary>Defines the sellable item export policy.</summary>
    /// <seealso cref="Policy" />
    public class SellableItemExportPolicy : Policy
    {
        /// <summary>
        /// Gets or sets the multi-inventory flag.
        /// </summary>
        /// <value>
        /// The multi-inventory flag.
        /// </value>
        public bool MultiInventory { get; set; } = false;

        /// <summary>
        /// Gets or sets the inventory set identifier used for single inventory migrations.
        /// </summary>
        /// <value>
        /// The inventory set identifier.
        /// </value>
        public string InventorySetId { get; set; }

        /// <summary>
        /// Gets or sets the default currency for assigning DefaultPriceSchedules to products.
        /// </summary>
        /// <value>
        /// The default currency.
        /// </value>
        public string DefaultCurrency { get; set; }
    }
}
