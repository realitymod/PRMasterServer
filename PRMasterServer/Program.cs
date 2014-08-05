using PRMasterServer.Data;
using PRMasterServer.Servers;
using System;
using System.Globalization;
using System.Net;
using System.Threading;

namespace PRMasterServer
{
	class Program
	{
		private static readonly object _lock = new object();

		static void Main(string[] args)
		{
			Action<string, string> log = (category, message) => {
				lock (_lock) {
					Log(String.Format("[{0}] {1}", category, message));
				}
			};

			Action<string, string> logError = (category, message) => {
				lock (_lock) {
					LogError(String.Format("[{0}] {1}", category, message));
				}
			};

            bool useCommAddress = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("+useCommunicationAddress"))
                {
                    useCommAddress = true;
                }
            }

            IPAddress bind = IPAddress.Any;
            if (args.Length >= 1)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("+bind"))
                    {
                        if ((i >= args.Length - 1) || !IPAddress.TryParse(args[i + 1], out bind))
                        {
                            LogError("+bind value must be a valid IP Address to bind to!");
                        }
                    }
                    //else if (args[i].Equals("+db"))
                    //{
                    //    if ((i >= args.Length - 1))
                    //    {
                    //        LogError("+db value must be a path to the database");
                    //    }
                    //    else
                    //    {
                    //        LoginDatabase.Initialize(args[i + 1], log, logError);
                    //    }
                    //}
                }
            }

            //if (!LoginDatabase.IsInitialized()) {
            //    LogError("Error initializing database, please confirm parameter +db is valid");
            //    LogError("Press any key to continue");
            //    Console.ReadKey();
            //    return;
            //}

            //CDKeyServer cdKeyServer = new CDKeyServer(bind, 29910, log, logError);
            ServerListReport serverListReport = new ServerListReport(bind, 27900, log, logError);
            //ServerListRetrieve serverListRetrieve = new ServerListRetrieve(bind, 28910, serverListReport, log, logError);
            ServerNatNeg serverNatNeg = new ServerNatNeg(bind, 27901, log, logError, useCommAddress);
            //LoginServer loginServer = new LoginServer(bind, 29900, 29901, log, logError);

			while (true) {
				Thread.Sleep(1000);
			}
		}

		private static void Log(string message)
		{
			Console.WriteLine(String.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), message));
		}

		private static void LogError(string message)
		{
			ConsoleColor c = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine(String.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), message));
			Console.ForegroundColor = c;
		}
	}
}
