﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Metrics.Core
{
    /// <summary>
    /// Encapsulates common functionality for a metrics registry
    /// </summary>
    public abstract class BaseRegistry : MetricsRegistry
    {
        private class MetricMetaCatalog<TMetric, TValue, TMetricValue>
            where TValue : MetricValueSource<TMetricValue>
            where TMetricValue : struct
        {
            public class MetricMeta
            {
                public MetricMeta(TMetric metric, TValue valueUnit)
                {
                    this.Name = valueUnit.Name;
                    this.Metric = metric;
                    this.Value = valueUnit;
                }

                public string Name { get; private set; }
                public TMetric Metric { get; private set; }
                public TValue Value { get; private set; }
            }

            private readonly ConcurrentDictionary<string, MetricMeta> metrics =
                new ConcurrentDictionary<string, MetricMeta>();

            public IEnumerable<TValue> All
            {
                get
                {
                    return this.metrics.Values.OrderBy(m => m.Name).Select(v => v.Value);
                }
            }

            public TMetric GetOrAdd(string name, Func<Tuple<TMetric, TValue>> metricProvider)
            {
                return this.metrics.GetOrAdd(name, n =>
                {
                    var result = metricProvider();
                    return new MetricMeta(result.Item1, result.Item2);
                }).Metric;
            }

            public void Clear()
            {
                var values = this.metrics.Values;
                this.metrics.Clear();
                foreach (var value in values)
                {
                    using (value.Metric as IDisposable) { }
                }
            }
        }

        private readonly MetricMetaCatalog<Gauge, GaugeValueSource, double> gauges = new MetricMetaCatalog<Gauge, GaugeValueSource, double>();
        private readonly MetricMetaCatalog<Counter, CounterValueSource, long> counters = new MetricMetaCatalog<Counter, CounterValueSource, long>();
        private readonly MetricMetaCatalog<Meter, MeterValueSource, MeterValue> meters = new MetricMetaCatalog<Meter, MeterValueSource, MeterValue>();
        private readonly MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue> histograms =
            new MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue>();
        private readonly MetricMetaCatalog<Timer, TimerValueSource, TimerValue> timers = new MetricMetaCatalog<Timer, TimerValueSource, TimerValue>();

        public BaseRegistry(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        public IEnumerable<GaugeValueSource> Gauges { get { return this.gauges.All; } }
        public IEnumerable<CounterValueSource> Counters { get { return this.counters.All; } }
        public IEnumerable<MeterValueSource> Meters { get { return this.meters.All; } }
        public IEnumerable<HistogramValueSource> Histograms { get { return this.histograms.All; } }
        public IEnumerable<TimerValueSource> Timers { get { return this.timers.All; } }

        public Gauge Gauge(string name, Func<double> valueProvider, Unit unit)
        {
            return this.gauges.GetOrAdd(name, () => CreateGauge(name, valueProvider, unit));
        }

        public Gauge Gauge<T>(string name, Func<T> gauge, Unit unit)
             where T : GaugeMetric
        {
            return this.gauges.GetOrAdd(name, () => CreateGauge(name, gauge, unit));
        }

        public Counter Counter(string name, Unit unit)
        {
            return this.counters.GetOrAdd(name, () => CreateCounter(name, unit));
        }

        public Meter Meter(string name, Unit unit, TimeUnit rateUnit)
        {
            return this.meters.GetOrAdd(name, () => CreateMeter(name, unit, rateUnit));
        }

        public Histogram Histogram(string name, Unit unit, SamplingType samplingType)
        {
            return this.histograms.GetOrAdd(name, () => CreateHistogram(name, unit, samplingType));
        }

        public Timer Timer(string name, Unit unit, SamplingType samplingType, TimeUnit rateUnit, TimeUnit durationUnit)
        {
            return this.timers.GetOrAdd(name, () => CreateTimer(name, unit, samplingType, rateUnit, durationUnit));
        }

        protected abstract Tuple<Gauge, GaugeValueSource> CreateGauge(string name, Func<double> valueProvider, Unit unit);
        protected abstract Tuple<Gauge, GaugeValueSource> CreateGauge<T>(string name, Func<T> gauge, Unit unit) where T : GaugeMetric;
        protected abstract Tuple<Counter, CounterValueSource> CreateCounter(string name, Unit unit);
        protected abstract Tuple<Meter, MeterValueSource> CreateMeter(string name, Unit unit, TimeUnit rateUnit);
        protected abstract Tuple<Histogram, HistogramValueSource> CreateHistogram(string name, Unit unit, SamplingType samplingType);
        protected abstract Tuple<Timer, TimerValueSource> CreateTimer(string name, Unit unit, SamplingType samplingType, TimeUnit rateUnit, TimeUnit durationUnit);

        public void ClearAllMetrics()
        {
            this.gauges.Clear();
            this.counters.Clear();
            this.meters.Clear();
            this.histograms.Clear();
            this.timers.Clear();
        }
    }
}
