# Sitecore Commerce to OrderCloud Migration
This is sample plugin to migrate Sitecore Commerce data over into OrderCloud.

## Supported Sitecore Experience Commerce Versions
- XC 10.1

## Features

* Customer Migration
* Catalog Migration
* Category Migration
  * Data
  * Category Assignments
* Product Migration
  * Data
  * Catalog Assignments
  * Category Assignments
* List Price Migration
  * Pseudo-multi-currency support
* Inventory Migration

## Global

* IDs will be updated to replace spaces with '\_'s._

## Customers

* An overarching Buyer will be created from the customer domain, e.g. Storefront\\test@test.com will create Buyer - Storefront, and Buyer User - test@test.com.
* Customers will be converted into Buyer Users

## Catalogs

* Catalogs to be migrated will need a CatalogSetting configured in the export request.

### CatalogSettings
* **CatalogName:** The catalog name (FriendlyID) to be exported.
* **DefaultBuyerId:** The buyer Id the catalog will be assigned to in OrderCloud. Assumes the Buyer has already been created. See Buyers.
* **ShopName:** The name of the storefront item from Sitecore's Content Editor - _/sitecore/Commerce/Commerce Control Panel/Storefront Settings/Storefronts/\<storefront item\>__
* **MultiCurrency:** If true, will create currency-specific buyer user groups, currency-specific price schedules, and price schedule assignments between the price schedules and user groups.

## Categories

* One-to-one relationship support for category to category associations. If any one-to-many associations still exist within XC, the last association processed will be persisted in OrderCloud. _XC category-to-category associations previously supported a one-to-many relationship, but has since removed support and is now supports a one-to-one relationship._
* XC orders categories by entity id, whereas OC has a specific ListOrder property, which is generated/incremented per category imported. Manual re-ordering of categories may be required.

## Products

* Static and Dynamic bundles are not supported.
* Sellable items with a single variant that does not contain any variation property values will be folded into a standalone product in OrderCloud.
* Sellable items with inconsistent or duplicate variation property values will be flagged as invalid and will not be migrated.

## Pricing

* Sellable item List Prices are migrated to Price Schedules.
  * Price Schedule IDs will use the associated product ID for easier identification.
  * For multi-currency implementations, currency-specific price schedules will be created with Price Schedule IDs using the convention '_\<product ID\>\_\<currency code\>_'.
    * Price schedules will be assigned to currency-specific buyer groups. Buyer users will be assigned to the default currency user group and the middleware will need to be implement functionality to switch the user to the respective user groups to utilise alternate currencies.
* Price Books and Price Cards are not supported.

## Inventory

* Product and variant inventory is supported.
* Multi-inventory not currently supported.

## Promotions

* Not supported

## Orders

* Not supported

## Manual changes required

### Variation Properties

In order to create variants correctly, variation property validation and comparisons may need updating if your implementation uses properties other than the standard 'color' and 'size'.
Areas that will need to be updated include:
* SellableItemExtensions.RequiresVariantsForOrderCloud()
* SellableItemExtensions.GetVariationSummary()
* ValidateSellableItemBlock.ValidateVariants()
* ExportSellableItemBlock.cs

## Installation Instructions
1. Download the repository.
2. Add the **Ajsuth.Sample.OrderCloud.Engine.csproj** to the _**Sitecore Commerce Engine**_ solution.
3. In the _**Sitecore Commerce Engine**_ project, add a reference to the **Ajsuth.Sample.OrderCloud.Engine** project.
4. Run the _**Sitecore Commerce Engine**_ from Visual Studio or deploy the solution and run from IIS.

## Known Issues
| Feature                 | Description | Issue |
| ----------------------- | ----------- | ----- |
|                         |             |       |

## Disclaimer
The code provided in this repository is sample code only. It is not intended for production usage and not endorsed by Sitecore.
Both Sitecore and the code author do not take responsibility for any issues caused as a result of using this code.
No guarantee or warranty is provided and code must be used at own risk.
