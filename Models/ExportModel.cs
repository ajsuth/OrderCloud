// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportModel.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Sitecore.Commerce.Core;

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the Export model.</summary>
    /// <seealso cref="Model" />
    public class ExportModel : Model
    {
        public string Entity { get; set; }
        public int ItemsProcessed { get; set; }
        public int ItemsExported { get; set; }
        public int ItemsSkipped { get; set; }
    }
}