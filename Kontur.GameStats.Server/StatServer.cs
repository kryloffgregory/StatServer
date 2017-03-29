using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Kontur.GameStats.Server
{
    internal class StatServer : IDisposable
    {
        public StatServer()
        {
            listener = new HttpListener();
            storage = new Storage();
        }

        public void Start(string prefix)
        {
            Console.WriteLine("Server started");
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
                }
            }
            listenerThread.Join();

        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else
                        Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    Console.WriteLine("INTERNAL ERROR:");
                    Console.WriteLine(error.Message);
                    Console.WriteLine(error.StackTrace);
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            try
            {
                string url = listenerContext.Request.RawUrl;
                string[] urlSegments =
                    listenerContext.Request.RawUrl
                        .Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                string bodyHTTP;
                using (StreamReader bodyReader =
                           new StreamReader(listenerContext.Request.InputStream))
                {
                    bodyHTTP = bodyReader.ReadToEnd();
                }
                                
                switch (listenerContext.Request.HttpMethod.ToUpper())
                {
                    
                    case "PUT":
                        //cases of various query types
                        if (Regex.IsMatch(url, serverInfoUrlPattern))
                        {
                            ServerInfo data = new ServerInfo();
                            data = JsonConvert
                                .DeserializeAnonymousType<ServerInfo>(bodyHTTP, data);
                            
                            storage.AddGameServer(urlSegments[1], data);
                            sendResponse(
                                listenerContext, 
                                (int)HttpStatusCode.OK, 
                                ""
                            );//New server added
                        }
                        else if (Regex.IsMatch(url, matchInfoUrlPattern))
                        {
                            if (storage.containsGameServer(urlSegments[1]))
                            {
                                MatchInfo data = new MatchInfo();
                                data = JsonConvert
                                    .DeserializeAnonymousType<MatchInfo>(bodyHTTP, data);
                                storage.AddMatch(urlSegments[1], urlSegments[3], data);
                                sendResponse(
                                    listenerContext,
                                    (int)HttpStatusCode.OK,
                                    ""//New match added
                                );
                            }
                            else
                                sendResponse
                                (
                                    listenerContext, 
                                    (int)HttpStatusCode.BadRequest, 
                                    ""
                                );//No advertise from this server
                        
                        }
                        else
                            sendResponse
                            (
                                listenerContext, 
                                (int)HttpStatusCode.NotFound, 
                                "Resource not found"
                            );
                
                        break;

                    case "GET":
                        //cases of various query types
                        if (Regex.IsMatch(url, serverInfoUrlPattern))
                        {
                            
                            if (storage.containsGameServer(urlSegments[1]))
                            {
                                string response = storage.GetServerInfo(urlSegments[1]);
                                sendResponse
                                (
                                    listenerContext, 
                                    (int)HttpStatusCode.OK, 
                                    response
                                );
                            }
                            else
                                sendResponse(
                                    listenerContext, 
                                    (int)HttpStatusCode.BadRequest, 
                                    ""
                                );//No advertise from this server
                        
                        }

                        else if (Regex.IsMatch(url, matchInfoUrlPattern))
                        {
                            if (storage.containsGameServer(urlSegments[1]))
                            {
                                if (storage.containsMatch(urlSegments[1], urlSegments[3]))
                                {
                                    string response = storage.GetMatchInfo(urlSegments[1], urlSegments[3]);
                                    sendResponse(listenerContext, (int)HttpStatusCode.OK, response);
                                }
                                else
                                    sendResponse(
                                        listenerContext, 
                                        (int)HttpStatusCode.BadRequest, 
                                        "No such match"
                                    );
                
                            }
                            else
                                sendResponse(
                                    listenerContext, 
                                    (int)HttpStatusCode.BadRequest, 
                                    ""
                                );//No advertise from this server
                        
                        }

                        else if (Regex.IsMatch(url, allServersInfoUrlPattern))
                        {
                            string response = storage.GetAllServersInfo();
                            sendResponse(listenerContext, (int)HttpStatusCode.OK, response);
                        }
                        else if (Regex.IsMatch(url, serverStatsUrlPattern))
                        {
                            if (storage.containsGameServer(urlSegments[1]))
                            {
                                string response = storage.GetServerStats(urlSegments[1]);
                                sendResponse(listenerContext, (int)HttpStatusCode.OK, response);
                            }
                            else
                                sendResponse(
                                    listenerContext, 
                                    (int)HttpStatusCode.BadRequest, 
                                    ""
                                );//No advertise from this server
                        }

                        else if (Regex.IsMatch(url, playerStatsUrlPattern))
                        {
                            if (storage.containsPlayer(urlSegments[1]))
                            {
                                string response = storage.GetPlayerStats(urlSegments[1]);
                                sendResponse(listenerContext, (int)HttpStatusCode.OK, response);
                            }
                            else
                                sendResponse(
                                    listenerContext, 
                                    (int)HttpStatusCode.NotFound, 
                                    "No such player"
                                );
                        }

                        else if (Regex.IsMatch(url, bestPlayersUrlPattern))
                        {
                            int count = 5;
                            if (urlSegments.Length == 3)
                            {
                                count = Int32.Parse(urlSegments[2]);
                                if (count > 50)
                                    count = 50;
                                if (count < 0)
                                    count = 0;
                            }
                            string response = storage.GetBestPlayers(count);
                            sendResponse(listenerContext, (int)HttpStatusCode.OK, response);
                        }

                        else if (Regex.IsMatch(url, popularServersUrlPattern))
                        {
                            int count = 5;
                            if (urlSegments.Length == 3)
                            {
                                count = Int32.Parse(urlSegments[2]);
                                if (count > 50)
                                    count = 50;
                                if (count < 0)
                                    count = 0;
                            }
                            string response = storage.GetPopularServers(count);
                            sendResponse
                            (
                                listenerContext, 
                                (int)HttpStatusCode.OK, 
                                response
                            );
                
                        }

                        else if (Regex.IsMatch(url, recentMatchesUrlPattern))
                        {
                            int count = 5;
                            if (urlSegments.Length == 3)
                            {
                                count = Int32.Parse(urlSegments[2]);
                                if (count > 50)
                                    count = 50;
                                if (count < 0)
                                    count = 0;
                            }
                            string response = storage.GetRecentMatches(count);
                            sendResponse
                            (
                                listenerContext, 
                                (int)HttpStatusCode.OK, 
                                response
                            );
                        }

                        else
                            sendResponse
                            (
                                listenerContext, 
                                (int)HttpStatusCode.NotFound, 
                                ""
                            );
                        break;

                    default:
                        sendResponse
                        (
                            listenerContext, 
                            (int)HttpStatusCode.MethodNotAllowed, 
                            ""
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                sendResponse
                (
                    listenerContext, 
                    (int)HttpStatusCode.InternalServerError, 
                    "Internal server error occured. Please, try later."
                );
                Console.WriteLine("INTERNAL ERROR:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw ex;
            }
        }

        private static void sendResponse(HttpListenerContext listenerContext, int statusCode, string response)
        {
            listenerContext.Response.StatusCode = statusCode;
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                writer.WriteLine(response);
        }

        private static bool isCorrectEndpoint(string endpoint)
        {
            return Regex.IsMatch(endpoint, correctEndpointPattern);
        }

        private static bool isCorrectTimestamp(string timestamp)
        {
            return Regex.IsMatch(timestamp, correctTimestampPattern);
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private Storage storage;


        //regular expressions for parsing the url string
        private static readonly string correctEndpointPattern = @".*";
        private static readonly string correctTimestampPattern =
            @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z";
        private static readonly string correctPlayerNamePattern =
            @".*";

        private static readonly string serverInfoUrlPattern =
            @"^/servers/" + correctEndpointPattern + @"/info$";
        private static readonly string matchInfoUrlPattern =
            @"^/servers/" + correctEndpointPattern + @"/matches/" + correctTimestampPattern + @"$";
        private static readonly string allServersInfoUrlPattern =
            @"^/servers/info$";
        private static readonly string serverStatsUrlPattern =
            @"^/servers/" + correctEndpointPattern + @"/stats$";
        private static readonly string playerStatsUrlPattern =
            @"^/players/" + correctPlayerNamePattern + @"/stats$";
        private static readonly string recentMatchesUrlPattern =
            @"^/reports/recent-matches(/\d+)?$";
        private static readonly string bestPlayersUrlPattern =
            @"^/reports/best-players(/\d+)?$";
        private static readonly string popularServersUrlPattern =
            @"^/reports/popular-servers(/\d+)?$";
    }
}
