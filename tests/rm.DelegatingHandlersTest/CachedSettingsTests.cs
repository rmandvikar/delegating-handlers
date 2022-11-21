using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.Hacks;

namespace rm.HacksTest
{
	[TestFixture]
	public class CachedSettingsTests
	{
		[Explicit]
		[Test]
		public async Task Verify()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var cacheSettings = new CacheSettings { Ttl = TimeSpan.FromMilliseconds(100) };
			fixture.Register(() => cacheSettings);
			fixture.Register<ISettingsProvider<Settings>>(() => new SettingsProvider());

			using var cachedSettingsProvider = fixture.Create<InMemoryCachedSettingsProvider>();

			Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]  start!");

			for (int i = 0; i < 100; i++)
			{
				var settings = await cachedSettingsProvider.GetSettingsAsync(default);
				Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}]  {settings}");

				await Task.Delay(5);
			}

			// showcase
			cachedSettingsProvider.Stop();
		}

		public class SettingsProvider : ISettingsProvider<Settings>
		{
			private int i = 0;

			public async Task<Settings> GetSettingsAsync(CancellationToken cancellationToken)
			{
				// simulate http call delay
				await Task.Delay(100)
					.ConfigureAwait(false);

				var settings =
					new Settings
					{
						Property1 = i.ToString(),
						// change sentinel every other time (by clearing LSB)
						SentinelProperty = (i & ~1).ToString(),
					};

				i++;

				return settings;
			}
		}

		[DebuggerDisplay("{Stringify}")]
		public class Settings : ISettings
		{
			public string SentinelKey => SentinelProperty!;

			public string? SentinelProperty { get; init; }
			public string? Property1 { get; init; }

			public override string ToString() => Stringify;

			private string Stringify => $"{nameof(Property1)}:{Property1,4}, {nameof(SentinelKey)}:{SentinelKey,4}";
		}

		public class InMemoryCachedSettingsProvider : CachedSettingsProvider<Settings>
		{
			public InMemoryCachedSettingsProvider(
				ISettingsProvider<Settings> settingsProvider,
				CacheSettings cacheSettings)
				: base(settingsProvider, cacheSettings)
			{ }
		}
	}
}
