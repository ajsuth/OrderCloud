// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommerceOpsController.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Ajsuth.Sample.OrderCloud.Engine.Commands;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ajsuth.Sample.OrderCloud.Engine.Controllers
{
    /// <summary>Defines the commerce ops controller</summary>
    /// <seealso cref="CommerceODataController" />
    public class CommerceOpsController : CommerceODataController
    {
        /// <summary>Initializes a new instance of the <see cref="CommerceOpsController" /> class.</summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="globalEnvironment">The global environment.</param>
        public CommerceOpsController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }

        /// <summary>
        /// Exports XC data to OrderCloud.
        /// </summary>
        /// <param name="value">The action parameters.</param>
        /// <returns>The action result.</returns>
        [HttpPost]
        [ODataRoute("ExportToOrderCloud", RouteName = CoreConstants.CommerceOpsApi)]
        public async Task<IActionResult> ExportToOrderCloud([FromBody] ODataActionParameters value)
        {
            Condition.Requires(value, nameof(value)).IsNotNull();

            if (!ModelState.IsValid || value == null)
            {
                return new BadRequestObjectResult(ModelState);
            }

            if (!value.ContainsKey("processSettings") || !(value["processSettings"] is ExportSettings processSettings))
            {
                return new BadRequestObjectResult(value);
            }

            var command = Command<ExportToOrderCloudCommand>();
            var result = await command.Process(CurrentContext, processSettings, (value["buyerSettings"] as IEnumerable<CustomerExportPolicy>)?.ToList(), (value["catalogSettings"] as IEnumerable<CatalogExportPolicy>)?.ToList(), value["productSettings"] as SellableItemExportPolicy).ConfigureAwait(false);

            return new ObjectResult(result);
        }
    }
}