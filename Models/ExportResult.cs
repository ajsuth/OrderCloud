// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportResult.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the ExportResult model.</summary>
    /// <seealso cref="Model" />
    public class ExportResult : Model
    {
        public ExportObject AdminAddresses { get; set; } = new ExportObject();
        public ExportObject BuyerAddressAssignments { get; set; } = new ExportObject();
        public ExportObject BuyerAddresses { get; set; } = new ExportObject();
        public ExportObject Buyers { get; set; } = new ExportObject();
        public ExportObject BuyerGroupAssignments { get; set; } = new ExportObject();
        public ExportObject BuyerGroups { get; set; } = new ExportObject();
        public ExportObject BuyerUsers { get; set; } = new ExportObject();
        public ExportObject Catalogs { get; set; } = new ExportObject();
        public ExportObject CatalogAssignments { get; set; } = new ExportObject();
        public ExportObject CatalogProductAssignments { get; set; } = new ExportObject();
        public ExportObject Categories { get; set; } = new ExportObject();
        public ExportObject CategoryAssignments { get; set; } = new ExportObject();
        public ExportObject CategoryProductAssignments { get; set; } = new ExportObject();
        public ExportObject Locales { get; set; } = new ExportObject();
        public ExportObject LocaleAssignments { get; set; } = new ExportObject();
        public ExportObject ProductAssignments { get; set; } = new ExportObject();
        public ExportObject Products { get; set; } = new ExportObject();
        public ExportObject SecurityProfileAssignments { get; set; } = new ExportObject();
        public ExportObject SecurityProfiles { get; set; } = new ExportObject();
        public ExportObject Specs { get; set; } = new ExportObject();
        public ExportObject SpecOptions { get; set; } = new ExportObject();
        public ExportObject SpecProductAssignments { get; set; } = new ExportObject();
        public ExportObject Variants { get; set; } = new ExportObject();
        public ExportObject PriceSchedules { get; set; } = new ExportObject();
        public ExportObject InventoryRecords { get; set; } = new ExportObject();
    }
}