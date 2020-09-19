﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GpsManagerImpl : NotifyPropertyChanged, IGpsManager, IShinyStartupTask
    {
        readonly CLLocationManager locationManager;
        readonly GpsManagerDelegate gdelegate;


        public GpsManagerImpl()
        {
            this.gdelegate = new GpsManagerDelegate();
            this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
        }


        public async void Start()
        {
            if (this.CurrentListener != null)
            {
                try
                {
                    if (this.CurrentListener.UseBackground)
                        await this.StartListenerInternal(this.CurrentListener);
                    else
                        this.CurrentListener = null;
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => this.gdelegate.WhenAccessStatusChanged(request.UseBackground);
        public Task<AccessState> RequestAccess(GpsRequest request) => this.locationManager.RequestAccess(request.UseBackground);
        public AccessState GetCurrentStatus(GpsRequest request) => this.locationManager.GetCurrentStatus(request.UseBackground);

        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set => this.Set(ref this.request, value);
        }


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async ct =>
        {
            if (this.locationManager.Location != null)
                return new GpsReading(this.locationManager.Location);

            var task = this
                .WhenReading()
                .Timeout(TimeSpan.FromSeconds(20))
                .Take(1)
                .ToTask(ct);

            var listen = this.CurrentListener;
            try
            {
                if (listen == null)
                {
                    var access = await this.RequestAccess(new GpsRequest());
                    access.Assert();
                    this.locationManager.StartUpdatingLocation();
                }
                return await task.ConfigureAwait(false);
            }
            finally
            {
                if (listen == null)
                    this.locationManager.StopUpdatingLocation();
            }
        });


        public async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                throw new ArgumentException("There is already an active GPS listener");

            await this.StartListenerInternal(request);
        }


        public Task StopListener()
        {
            if (this.CurrentListener != null)
            {
#if __IOS__
                this.locationManager.AllowsBackgroundLocationUpdates = false;
#endif
                this.locationManager.StopUpdatingLocation();
                this.CurrentListener = null;
            }
            return Task.CompletedTask;
        }


        protected virtual async Task StartListenerInternal(GpsRequest request)
        {
            var access = await this.RequestAccess(request);
            access.Assert();
            this.gdelegate.Request = request;
#if __IOS__
            this.locationManager.AllowsBackgroundLocationUpdates = request.UseBackground;
            var throttledInterval = request.ThrottledInterval?.TotalSeconds ?? 0;
            var minDistance = request.MinimumDistance?.TotalMeters ?? 0;

            if (throttledInterval > 0 || minDistance > 0)
            {
                this.locationManager.AllowDeferredLocationUpdatesUntil(
                    minDistance,
                    throttledInterval
                );
            }
#endif
            switch (request.Priority)
            {
                case GpsPriority.Highest:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
                    break;

                case GpsPriority.Normal:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
                    break;

                case GpsPriority.Low:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
                    break;
            }

            //this.locationManager.ShouldDisplayHeadingCalibration
            //this.locationManager.ShowsBackgroundLocationIndicator
            //this.locationManager.PausesLocationUpdatesAutomatically = false;
            //this.locationManager.DisallowDeferredLocationUpdates
            //this.locationManager.ActivityType = CLActivityType.Airborne;
            //this.locationManager.LocationUpdatesPaused
            //this.locationManager.LocationUpdatesResumed
            //this.locationManager.Failed
            //this.locationManager.UpdatedHeading
            //if (CLLocationManager.HeadingAvailable)
            //    this.locationManager.StopUpdatingHeading();
            this.locationManager.StartUpdatingLocation();
            this.CurrentListener = request;
        }

        public IObservable<IGpsReading> WhenReading() => this.gdelegate.WhenGps();
    }
}
