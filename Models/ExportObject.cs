// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportObject.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2022
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Ajsuth.Sample.OrderCloud.Engine.Models
{
    /// <summary>Defines the ExportObject model.</summary>
    public class ExportObject
    {
        public long ItemsProcessed { get; set; } = 0;
        public long ItemsNotChanged { get; set; } = 0;
        public long ItemsCreated { get; set; } = 0;
        public long ItemsPatched { get; set; } = 0;
        public long ItemsUpdated { get; set; } = 0;
        public long ItemsSkipped { get; set; } = 0;
        public long ItemsErrored { get; set; } = 0;

    }
}