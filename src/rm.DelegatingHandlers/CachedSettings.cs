using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace rm.Hacks
{
	public interface ISettings
	{
		string SentinelKey { get; }
	}

	public interface ISettingsProvider<ISettings>
	{
		Task<ISettings> GetSettingsAsync(CancellationToken cancellationToken);
	}

	public interface ICachedSettingsProvider<ISettings> : ISettingsProvider<ISettings>
	{
		void Stop();
	}

	public class CacheSettings
	{
		public TimeSpan Ttl { get; init; }
	}

	public abstract class CachedSettingsProvider<TSettings> : ICachedSettingsProvider<TSettings>, IDisposable
		where TSettings : ISettings
	{
		private readonly ISettingsProvider<TSettings> settingsProvider;
		private readonly CacheSettings cacheSettings;

		private readonly object locker = new object();

		private TSettings settingsLocked;
		private TSettings Settings
		{
			get { lock (locker) { return settingsLocked; } }
			set { lock (locker) { settingsLocked = value; } }
		}

		private readonly AsyncManualResetEvent setupEvent = new AsyncManualResetEvent(set: false);

		private readonly Timer timer;

		public CachedSettingsProvider(
			ISettingsProvider<TSettings> settingsProvider,
			CacheSettings cacheSettings)
		{
			this.settingsProvider = settingsProvider
				?? throw new ArgumentNullException(nameof(settingsProvider));
			this.cacheSettings = cacheSettings
				?? throw new ArgumentNullException(nameof(cacheSettings));

			// setup
			timer = new Timer(CallbackAsync, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
		}

		private async void CallbackAsync(object state)
		{
			try
			{
				var newSettings = await settingsProvider.GetSettingsAsync(default)
					.ConfigureAwait(false);
				if (newSettings == null)
				{
					throw new NullReferenceException(nameof(newSettings));
				}
				var currentSettings = Settings;
				if (currentSettings == null || IsSentinelUpdated(currentSettings, newSettings))
				{
					Settings = newSettings;
				}

				setupEvent.Set();
			}
			catch (Exception)
			{
				// log ex!
			}
			finally
			{
				Start();
			}
		}

		private void Start()
		{
			if (disposed)
			{
				return;
			}

			// note: time creep is fine
			timer.Change(cacheSettings.Ttl, Timeout.InfiniteTimeSpan);
		}

		private bool IsSentinelUpdated(TSettings currentSettings, TSettings newSettings)
		{
			var updated = currentSettings.SentinelKey != newSettings.SentinelKey;
#if DEBUG
			if (!updated)
			{
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] !{newSettings} Ignored!");
			}
#endif
			return updated;
		}

		public async Task<TSettings> GetSettingsAsync(CancellationToken cancellationToken)
		{
			// wait for the first call to finish
			await setupEvent.WaitAsync(cancellationToken)
				.ConfigureAwait(false);

			var settings = Settings;
			return settings;
		}

		public void Stop()
		{
			if (disposed)
			{
				return;
			}

			timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		private bool disposed = false;

		protected void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// stop to avoid race
					Stop();

					timer?.Dispose();

					disposed = true;
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
