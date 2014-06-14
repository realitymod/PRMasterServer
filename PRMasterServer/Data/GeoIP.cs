using MaxMind.GeoIP2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PRMasterServer.Data
{
	public class GeoIP : IDisposable
	{
		public readonly DatabaseReader Reader;

		private static GeoIP _instance;

		public GeoIP(DatabaseReader reader)
		{
			Reader = reader;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			try {
				if (disposing) {
					if (Reader != null) {
						Reader.Dispose();
					}
					_instance = null;

					if (_instance != null) {
						_instance.Dispose();
						_instance = null;
					}
				}
			} catch (Exception) {
			}
		}

		~GeoIP()
		{
			Dispose(false);
		}

		public static void Initialize(Action<string, string> log, string category)
		{
			DatabaseReader reader;

			if (File.Exists("GeoIP2-Country.mmdb")) {
				reader = new DatabaseReader("GeoIP2-Country.mmdb");
				log(category, "Loaded GeoIP2-Country.mmdb");
			} else if (File.Exists("GeoLite2-Country.mmdb")) {
				reader = new DatabaseReader("GeoLite2-Country.mmdb");
				log(category, "Loaded GeoLite2-Country.mmdb");
			} else {
				reader = null;
			}

			_instance = new GeoIP(reader);
		}

		public static GeoIP Instance
		{
			get
			{
				if (_instance == null) {
					throw new ArgumentNullException("Instance", "Initialize() must be called first");
				}

				return _instance;
			}
		}
	}
}
