// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariationsSummary.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;
using System.Collections.Generic;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the VariationsSummary model.</summary>
    /// <seealso cref="Model" />
    public class VariationsSummary : Model
    {
        /// <summary>
        /// Gets or sets the variation summaries.
        /// </summary>
        public List<VariationSummary> Variations { get; set; } = new List<VariationSummary>();

        /// <summary>
        /// Gets or sets the unique variation properties.
        /// </summary>
        public List<string> UniqueProperties { get; set; } = new List<string>();

        public List<string> GetDistinctValues(string name)
        {
            var values = new List<string>();
            foreach (var variationSummary in Variations)
            {
                values.AddRange(variationSummary.VariationProperties.Where(v => v.Name == name).Select(g => g.Value));
            }

            return values.Distinct().ToList();
        }
    }
}