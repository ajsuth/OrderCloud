// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrderCloudConstants.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Ajsuth.Sample.OrderCloud.Engine
{
    /// <summary>The OrderCloud constants.</summary>
    public class OrderCloudConstants
    {
        /// <summary>
        /// The names of the errors.
        /// </summary>
        public static class Errors
        {
            /// <summary>
            /// The create admin address failed error name.
            /// </summary>
            public const string CreateAdminAddressFailed = nameof(CreateAdminAddressFailed);

            /// <summary>
            /// The create buyer failed error name.
            /// </summary>
            public const string CreateBuyerFailed = nameof(CreateBuyerFailed);

            /// <summary>
            /// The create catalog failed error name.
            /// </summary>
            public const string CreateCatalogFailed = nameof(CreateCatalogFailed);

            /// <summary>
            /// The create catalog product assignment failed error name.
            /// </summary>
            public const string CreateCatalogProductAssignmentFailed = nameof(CreateCatalogProductAssignmentFailed);

            /// <summary>
            /// The create category failed error name.
            /// </summary>
            public const string CreateCategoryFailed = nameof(CreateCategoryFailed);

            /// <summary>
            /// The create category product assignment failed error name.
            /// </summary>
            public const string CreateCategoryProductAssignmentFailed = nameof(CreateCategoryProductAssignmentFailed);
            
            /// <summary>
            /// The create product failed error name.
            /// </summary>
            public const string CreateProductFailed = nameof(CreateProductFailed);

            /// <summary>
            /// The create variants failed error name.
            /// </summary>
            public const string CreateVariantsFailed = nameof(CreateVariantsFailed);
            
            /// <summary>
            /// The export all catalog assignments failed error name.
            /// </summary>
            public const string ExportAllCatalogAssignmentsFailed = nameof(ExportAllCatalogAssignmentsFailed);

            /// <summary>
            /// The export all category assignments failed error name.
            /// </summary>
            public const string ExportAllCategoryAssignmentsFailed = nameof(ExportAllCategoryAssignmentsFailed);

            /// <summary>
            /// The export catalogs failed error name.
            /// </summary>
            public const string ExportCatalogsFailed = nameof(ExportCatalogsFailed);

            /// <summary>
            /// The export categories failed error name.
            /// </summary>
            public const string ExportCategoriesFailed = nameof(ExportCategoriesFailed);

            /// <summary>
            /// The export customers failed error name.
            /// </summary>
            public const string ExportCustomersFailed = nameof(ExportCustomersFailed);

            /// <summary>
            /// The export sellable items failed error name.
            /// </summary>
            public const string ExportSellableItemsFailed = nameof(ExportSellableItemsFailed);
            
            /// <summary>
            /// The get admin address failed error name.
            /// </summary>
            public const string GetAdminAddressFailed = nameof(GetAdminAddressFailed);

            /// <summary>
            /// The get buyer failed error name.
            /// </summary>
            public const string GetBuyerFailed = nameof(GetBuyerFailed);

            /// <summary>
            /// The get catalog failed error name.
            /// </summary>
            public const string GetCatalogFailed = nameof(GetCatalogFailed);

            /// <summary>
            /// The get category failed error name.
            /// </summary>
            public const string GetCategoryFailed = nameof(GetCategoryFailed);

            /// <summary>
            /// The get product failed error name.
            /// </summary>
            public const string GetProductFailed = nameof(GetProductFailed);

            /// <summary>
            /// The invalid OrderCloud client policy error name.
            /// </summary>
            public const string InvalidOrderCloudClientPolicy = nameof(InvalidOrderCloudClientPolicy);
            
            /// <summary>
            /// The shop not found error name.
            /// </summary>
            public const string ShopNotFound = nameof(ShopNotFound);

            /// <summary>
            /// The update buyer user failed error name.
            /// </summary>
            public const string UpdateBuyerUserFailed = nameof(UpdateBuyerUserFailed);

            /// <summary>
            /// The update category parent id failed error name.
            /// </summary>
            public const string UpdateCategoryParentIdFailed = nameof(UpdateCategoryParentIdFailed);
        }

        /// <summary>
        /// The names of the lists.
        /// </summary>
        public static class Lists
        {
            /// <summary>
            /// The catalogs list name.
            /// </summary>
            public const string Catalogs = nameof(Catalogs);

            /// <summary>
            /// The categories list name.
            /// </summary>
            public const string Categories = nameof(Categories);

            /// <summary>
            /// The customers list name.
            /// </summary>
            public const string Customers = nameof(Customers);

            /// <summary>
            /// The sellable items list name.
            /// </summary>
            public const string SellableItems = nameof(SellableItems);
        }

        /// <summary>
        /// The names of the pipelines.
        /// </summary>
        public static class Pipelines
        {
            /// <summary>
            /// The export catalog assignments pipeline name.
            /// </summary>
            public const string ExportCatalogAssignments = "OrderCloud.Pipeline.ExportCatalogAssignments";

            /// <summary>
            /// The export catalogs pipeline name.
            /// </summary>
            public const string ExportCatalogs = "OrderCloud.Pipeline.ExportCatalogs";

            /// <summary>
            /// The export categoriespipeline name.
            /// </summary>
            public const string ExportCategories = "OrderCloud.Pipeline.ExportCategories";

            /// <summary>
            /// The export category assignments pipeline name.
            /// </summary>
            public const string ExportCategoryAssignments = "OrderCloud.Pipeline.ExportCategoryAssignments";
            
            /// <summary>
            /// The export customerspipeline name.
            /// </summary>
            public const string ExportCustomers = "OrderCloud.Pipeline.ExportCustomers";

            /// <summary>
            /// The export sellable itemspipeline name.
            /// </summary>
            public const string ExportSellableItems = "OrderCloud.Pipeline.ExportSellableItems";

            /// <summary>
            /// The export to OrderCloud pipeline name.
            /// </summary>
            public const string ExportToOrderCloud = "OrderCloud.Pipeline.ExportToOrderCloud";

            /// <summary>
            /// The names of the pipeline blocks.
            /// </summary>
            public static class Blocks
            {
                /// <summary>
                /// The configure ops service api pipeline block name.
                /// </summary>
                public const string ConfigureOpsServiceApi = "OrderCloud.Block.ConfigureOpsServiceApi";

                /// <summary>
                /// The export all catalog assignments pipeline block name.
                /// </summary>
                public const string ExportAllCatalogAssignments = "OrderCloud.Block.ExportAllCatalogAssignments";

                /// <summary>
                /// The export all category assignments pipeline block name.
                /// </summary>
                public const string ExportAllCategoryAssignments = "OrderCloud.Block.ExportAllCategoryAssignments";

                /// <summary>
                /// The export catalogs pipeline block name.
                /// </summary>
                public const string ExportCatalogs = "OrderCloud.Block.ExportCatalogs";

                /// <summary>
                /// The export catalog pipeline block name.
                /// </summary>
                public const string ExportCatalog = "OrderCloud.Block.ExportCatalog";

                /// <summary>
                /// The export categories pipeline block name.
                /// </summary>
                public const string ExportCategories = "OrderCloud.Block.ExportCategories";

                /// <summary>
                /// The export category pipeline block name.
                /// </summary>
                public const string ExportCategory = "OrderCloud.Block.ExportCategory";
                
                /// <summary>
                /// The export catalog assignments pipeline block name.
                /// </summary>
                public const string ExportCatalogAssignments = "OrderCloud.Block.ExportCatalogAssignments";

                /// <summary>
                /// The export category assignments pipeline block name.
                /// </summary>
                public const string ExportCategoryAssignments = "OrderCloud.Block.ExportCategoryAssignments";

                /// <summary>
                /// The export customer pipeline block name.
                /// </summary>
                public const string ExportCustomer = "OrderCloud.Block.ExportCustomer";

                /// <summary>
                /// The export customers pipeline block name.
                /// </summary>
                public const string ExportCustomers = "OrderCloud.Block.ExportCustomers";

                /// <summary>
                /// The export sellable item pipeline block name.
                /// </summary>
                public const string ExportSellableItem = "OrderCloud.Block.ExportSellableItem";

                /// <summary>
                /// The export sellable items pipeline block name.
                /// </summary>
                public const string ExportSellableItems = "OrderCloud.Block.ExportSellableItems";

                /// <summary>
                /// The prepare export pipeline block name.
                /// </summary>
                public const string PrepareExport = "OrderCloud.Block.PrepareExport";

                /// <summary>
                /// The validate catalog pipeline block name.
                /// </summary>
                public const string ValidateCatalog = "OrderCloud.Block.ValidateCatalog";

                /// <summary>
                /// The validate category pipeline block name.
                /// </summary>
                public const string ValidateCategory = "OrderCloud.Block.ValidateCategory";

                /// <summary>
                /// The validate customer pipeline block name.
                /// </summary>
                public const string ValidateCustomer = "OrderCloud.Block.ValidateCustomer";

                /// <summary>
                /// The validate sellable item pipeline block name.
                /// </summary>
                public const string ValidateSellableItem = "OrderCloud.Block.ValidateSellableItem";
            }
        }
    }
}