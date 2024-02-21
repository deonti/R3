﻿namespace R3;

public static partial class ObservableExtensions
{
    public static Observable<T> ThrottleLatest<T>(this Observable<T> source, TimeSpan timeSpan)
    {
        return new ThrottleLatest<T>(source, timeSpan, ObservableSystem.DefaultTimeProvider);
    }

    public static Observable<T> ThrottleLatest<T>(this Observable<T> source, TimeSpan timeSpan, TimeProvider timeProvider)
    {
        return new ThrottleLatest<T>(source, timeSpan, timeProvider);
    }

    public static Observable<T> ThrottleLatest<T, TSample>(this Observable<T> source, Observable<TSample> sampler)
    {
        return new ThrottleLatestObservableSampler<T, TSample>(source, sampler);
    }

    public static Observable<T> ThrottleLatest<T>(this Observable<T> source, Func<T, CancellationToken, ValueTask> sampler, bool configureAwait = true)
    {
        return new ThrottleLatestAsyncSampler<T>(source, sampler, configureAwait);
    }
}

internal sealed class ThrottleLatest<T>(Observable<T> source, TimeSpan interval, TimeProvider timeProvider) : Observable<T>
{
    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        return source.Subscribe(new _ThrottleLatest(observer, interval.Normalize(), timeProvider));
    }

    sealed class _ThrottleLatest : Observer<T>
    {
        static readonly TimerCallback timerCallback = RaiseOnNext;

        readonly Observer<T> observer;
        readonly TimeSpan interval;
        readonly ITimer timer;
        readonly object gate = new object();
        T? lastValue;
        bool hasValue;
        bool timerIsRunning;

        public _ThrottleLatest(Observer<T> observer, TimeSpan interval, TimeProvider timeProvider)
        {
            this.observer = observer;
            this.interval = interval;
            this.timer = timeProvider.CreateStoppedTimer(timerCallback, this);
        }

        protected override void OnNextCore(T value)
        {
            lock (gate)
            {
                if (!timerIsRunning) // timer is stopping
                {
                    timerIsRunning = true;
                    timer.InvokeOnce(interval); // timer start before OnNext
                    observer.OnNext(value);     // call OnNext in lock
                    return;
                }
                else
                {
                    hasValue = true;
                    lastValue = value;
                }
            }
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            observer.OnCompleted(result);
        }

        protected override void DisposeCore()
        {
            timer.Dispose();
        }

        static void RaiseOnNext(object? state)
        {
            var self = (_ThrottleLatest)state!;
            lock (self.gate)
            {
                self.timerIsRunning = false;
                if (self.hasValue)
                {
                    self.observer.OnNext(self.lastValue!);
                    self.hasValue = false;
                    self.lastValue = default;
                }
            }
        }
    }
}

internal sealed class ThrottleLatestAsyncSampler<T>(Observable<T> source, Func<T, CancellationToken, ValueTask> sampler, bool configureAwait) : Observable<T>
{
    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        return source.Subscribe(new _ThrottleLatest(observer, sampler, configureAwait));
    }

    sealed class _ThrottleLatest(Observer<T> observer, Func<T, CancellationToken, ValueTask> sampler, bool configureAwait) : Observer<T>
    {
        readonly object gate = new object();
        readonly CancellationTokenSource cancellationTokenSource = new();
        T? lastValue;
        bool hasValue;
        Task? taskRunner;

        protected override void OnNextCore(T value)
        {
            lock (gate)
            {
                if (taskRunner == null)
                {
                    taskRunner = RaiseOnNextAsync(value);
                    observer.OnNext(value);
                }
                else
                {
                    hasValue = true;
                    lastValue = value;
                }
            }
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            cancellationTokenSource.Cancel(); // cancel executing async process first
            observer.OnCompleted(result);
        }

        protected override void DisposeCore()
        {
            cancellationTokenSource.Cancel();
        }

        async Task RaiseOnNextAsync(T value)
        {
            try
            {
                await sampler(value, cancellationTokenSource.Token).ConfigureAwait(configureAwait);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException oce && oce.CancellationToken == cancellationTokenSource.Token)
                {
                    return;
                }
                OnErrorResume(ex);
            }
            finally
            {
                lock (gate)
                {
                    if (hasValue)
                    {
                        observer.OnNext(lastValue!);
                        lastValue = default;
                        hasValue = false;
                        taskRunner = null;
                    }
                }
            }
        }
    }
}

internal sealed class ThrottleLatestObservableSampler<T, TSample>(Observable<T> source, Observable<TSample> sampler) : Observable<T>
{
    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        return source.Subscribe(new _ThrottleLatest(observer, sampler));
    }

    sealed class _ThrottleLatest : Observer<T>
    {
        readonly Observer<T> observer;
        readonly object gate = new object();
        readonly IDisposable samplerSubscription;
        T? lastValue;
        bool hasValue;
        bool closing;

        public _ThrottleLatest(Observer<T> observer, Observable<TSample> sampler)
        {
            this.observer = observer;
            var sampleObserver = new SamplerObserver(this);
            this.samplerSubscription = sampler.Subscribe(sampleObserver);
        }

        protected override void OnNextCore(T value)
        {
            lock (gate)
            {
                if (!closing)
                {
                    closing = true;
                    observer.OnNext(value);
                }
                else
                {
                    lastValue = value;
                    hasValue = true;
                }
            }
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            observer.OnCompleted(result);
        }

        protected override void DisposeCore()
        {
            samplerSubscription.Dispose();
        }

        void PublishOnNext()
        {
            lock (gate)
            {
                closing = false;
                if (hasValue)
                {
                    observer.OnNext(lastValue!);
                    hasValue = false;
                    lastValue = default;
                }
            }
        }

        sealed class SamplerObserver(_ThrottleLatest parent) : Observer<TSample>
        {
            protected override void OnNextCore(TSample value)
            {
                parent.PublishOnNext();
            }

            protected override void OnErrorResumeCore(Exception error)
            {
                parent.OnErrorResume(error);
            }

            protected override void OnCompletedCore(Result result)
            {
                parent.OnCompleted(result);
            }
        }
    }
}
