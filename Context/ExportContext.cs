using Ajsuth.Sample.OrderCloud.Engine.FrameworkExtensions;
using Ajsuth.Sample.OrderCloud.Engine.Models;
using Ajsuth.Sample.OrderCloud.Engine.Pipelines.Arguments;
using Ajsuth.Sample.OrderCloud.Engine.Policies;
using Microsoft.Extensions.Logging;
using OrderCloud.SDK;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;
using System.Linq;

namespace Ajsuth.Sample.OrderCloud.Engine.Context
{
    public class ExportContext : CommercePipelineExecutionContext
    {
        public ExportContext(IPipelineExecutionContextOptions options, ILogger pipelineLogger) : base(options, pipelineLogger)
        {

        }

        private OrderCloudClient _client;

        public OrderCloudClient GetClient()
        {
            if (_client != null)
            {
                return _client;
            }

            _client = this.CommerceContext.GetObject<OrderCloudClient>();
            return _client;
        }

        private ExportResult _exportResult;

        public ExportResult GetExportResult()
        {
            if (_exportResult != null)
            {
                return _exportResult;
            }

            _exportResult = this.CommerceContext.GetObject<ExportResult>();
            return _exportResult;
        }

        public CustomerExportPolicy GetBuyerSettings(string buyerId)
        {
            var exportSettings = this.CommerceContext.GetObject<ExportEntitiesArgument>();
            var buyerSettings = exportSettings.BuyerSettings.FirstOrDefault(c => c.Id.ToValidOrderCloudId() == buyerId);

            return buyerSettings;
        }

        public CatalogExportPolicy GetCatalogSettings(string catalogId)
        {
            var exportSettings = this.CommerceContext.GetObject<ExportEntitiesArgument>();
            var catalogSettings = exportSettings.CatalogSettings.FirstOrDefault(c => c.CatalogName == catalogId);

            return catalogSettings;
        }
    }
}
