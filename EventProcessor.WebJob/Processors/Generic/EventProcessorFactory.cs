﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Applications.PredictiveMaintenance.Common.Configurations;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.Devices.Applications.PredictiveMaintenance.EventProcessor.WebJob.Processors.Generic
{
    public class EventProcessorFactory<TEventProcessor> : IEventProcessorFactory where TEventProcessor : EventProcessor
    {
        private readonly object[] _arguments;

        private readonly ConcurrentDictionary<string, TEventProcessor> _eventProcessors = new ConcurrentDictionary<string, TEventProcessor>();
        private readonly ConcurrentQueue<TEventProcessor> _closedProcessors = new ConcurrentQueue<TEventProcessor>();

        public EventProcessorFactory(params object[] arguments)
        {
            _arguments = arguments;
        }

        public int ActiveProcessors
        {
            get { return _eventProcessors.Count; }
        }

        public int TotalMessages
        {
            get
            {
                int amount = _eventProcessors.Select(p => p.Value.TotalMessages).Sum();
                amount += _closedProcessors.Select(p => p.TotalMessages).Sum();
                return amount;
            }
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            TEventProcessor processor = Activator.CreateInstance(typeof(TEventProcessor), _arguments) as TEventProcessor;

            processor.ProcessorClosed += this.ProcessorOnProcessorClosed;
            _eventProcessors.TryAdd(context.Lease.PartitionId, processor);
            return processor;
        }

        public Task WaitForAllProcessorsInitialized(TimeSpan timeout)
        {
            return this.WaitForAllProcessorsCondition(p => p.IsInitialized, timeout);
        }

        public Task WaitForAllProcessorsClosed(TimeSpan timeout)
        {
            return this.WaitForAllProcessorsCondition(p => p.IsClosed, timeout);
        }

        public async Task WaitForAllProcessorsCondition(Func<TEventProcessor, bool> predicate, TimeSpan timeout)
        {
            TimeSpan sleepInterval = TimeSpan.FromSeconds(2);
            while (!_eventProcessors.Values.All(predicate))
            {
                if (timeout > sleepInterval)
                {
                    timeout = timeout.Subtract(sleepInterval);
                }
                else
                {
                    throw new TimeoutException("Condition not satisfied within expected timeout.");
                }
                await Task.Delay(sleepInterval);
            }
        }

        public void ProcessorOnProcessorClosed(object sender, EventArgs eventArgs)
        {
            TEventProcessor processor = sender as TEventProcessor;
            if (processor != null)
            {
                _eventProcessors.TryRemove(processor.Context.Lease.PartitionId, out processor);
                _closedProcessors.Enqueue(processor);
            }
        }
    }
}
