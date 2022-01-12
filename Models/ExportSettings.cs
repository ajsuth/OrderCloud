// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportSettings.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the export settings model.</summary>
    /// <seealso cref="Model" />
    public class ExportSettings : Model
    {
        /// <summary>
        /// The import type. Can be CREATE (POST), UPDATE (PATCH), REPLACE (PUT)
        /// </summary>
        public string ImportType { get; set; } = "CREATE";

        /// <summary>
        /// Flag to export customer data to OrderCloud
        /// </summary>
        public bool ProcessCustomers { get; set; } = false;

        /// <summary>
        /// Flag to export catalog data to OrderCloud
        /// </summary>
        public bool ProcessCatalogs { get; set; } = false;

        /// <summary>
        /// Flag to export category data to OrderCloud
        /// </summary>
        public bool ProcessCategories { get; set; } = false;

        /// <summary>
        /// Flag to export product data to OrderCloud
        /// </summary>
        public bool ProcessProducts { get; set; } = false;

        /// <summary>
        /// Flag to export catalog assignments data to OrderCloud
        /// </summary>
        public bool ProcessCatalogAssignments { get; set; } = false;

        /// <summary>
        /// Flag to export category assignments data to OrderCloud
        /// </summary>
        public bool ProcessCategoryAssignments { get; set; } = false;

        /// <summary>
        /// Flag to export product relationship data to OrderCloud
        /// </summary>
        public bool ProcessProductRelationships { get; set; } = false;
    }
}