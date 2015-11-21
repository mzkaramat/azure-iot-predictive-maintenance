﻿// ---------------------------------------------------------------
//  Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

namespace Microsoft.Azure.Devices.Applications.PredictiveMaintenance.Web.Controllers
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Contracts;
    using Services;

    [Authorize]
    public sealed class DataController : ApiController
    {
        readonly ITelemetryService telemetryService;

        public DataController(ITelemetryService telemetryService)
        {
            this.telemetryService = telemetryService;
        }

        [HttpGet]
        [Route("api/telemetry")]
        public async Task<EnginesTelemetry> GetEnginesTelemetry()
        {
            var source = await this.telemetryService.GetLatestTelemetryData();

            var deviceGroup = new Dictionary<string, Collection<Telemetry>>();

            foreach (var telemetry in source)
            {
                Collection<Telemetry> collection;

                if (deviceGroup.ContainsKey(telemetry.DeviceId))
                {
                    collection = deviceGroup[telemetry.DeviceId];
                }
                else
                {
                    collection = new Collection<Telemetry>();
                    deviceGroup[telemetry.DeviceId] = collection;
                }

                collection.Add(telemetry);
            }

            var enginesTelemetry = new EnginesTelemetry
            {
                Engine1Telemetry = deviceGroup.ElementAt(0).Value,
                Engine2Telemetry = deviceGroup.ElementAt(1).Value
            };

            return enginesTelemetry;
        }

        [HttpGet]
        [Route("api/prediction")]
        public async Task<EnginesPrediction> GetEnginesPrediction()
        {
            var source = await this.telemetryService.GetLatestPredictionData();

            var deviceGroup = new Dictionary<string, Collection<Prediction>>();

            foreach (var telemetry in source)
            {
                Collection<Prediction> collection;

                if (deviceGroup.ContainsKey(telemetry.DeviceId))
                {
                    collection = deviceGroup[telemetry.DeviceId];
                }
                else
                {
                    collection = new Collection<Prediction>();
                    deviceGroup[telemetry.DeviceId] = collection;
                }

                collection.Add(telemetry);
            }

            var enginesTelemetry = new EnginesPrediction
            {
                Engine1Prediction = deviceGroup.ElementAt(0).Value,
                Engine2Prediction = deviceGroup.ElementAt(1).Value
            };

            return enginesTelemetry;
        }
    }
}