using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using LiteDB;
using System.Linq;

namespace Kontur.GameStats.Server
{
    internal class Storage
    {
        public Storage()
        {
            try
            {
                db = new LiteDatabase(dbPath);

                matchesCollection = db.GetCollection<MatchData>("matches");
                serversCollection = db.GetCollection<ServerData>("servers");
                playerStatsCollection = db.GetCollection<PlayerStats>("playerStats");
                serverStatsCollection = db.GetCollection<ServerStats>("serverStats");

                if(matchesCollection.Count() > 0)//recovering last match timestamp from db
                {
                    lastMatch = matchesCollection
                        .Find(Query.All("timestamp",Query.Descending), limit: 1)
                        .First()
                        .timestamp;
                }
                else
                    lastMatch = "0001-01-01T01:01:01Z";

                lastRecalc = "0001-01-01T01:01:01Z";

                Console.WriteLine("Storage was launched");
                Console.WriteLine("Last match time: " 
                    + (lastMatch == "0001-01-01T01:01:01Z" ? "No matches were" : lastMatch));
            }
            catch (Exception ex)
            {
                Console.WriteLine("STORAGE INIT ERROR:");
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }

        }

        public bool containsGameServer(string endpoint)
        {
            return serversCollection.Exists(x => x.endpoint == endpoint);
        }

        public bool containsMatch(string endpoint, string timestamp)
        {
            return matchesCollection
                .Exists(x => x.endpoint == endpoint && x.timestamp == timestamp);
        }

        public bool containsPlayer(string name)
        {
            return playerStatsCollection
                .Exists(x => x.name == name);
        }
        //PUT

        public void AddGameServer(string endpoint, ServerInfo serverInfo)
        {
            //servers
            var serverRecord = serversCollection.FindById(endpoint);

            if(serverRecord == null)
            {
                serversCollection.Insert(new ServerData(endpoint, serverInfo));
            }
            else
            {
                serverRecord.info = serverInfo;
                serversCollection.Update(serverRecord);
            }
            

            //servers' stats
            var serverStatsRecord = serverStatsCollection.FindById(endpoint);

            if(serverStatsRecord == null)
            {
                serverStatsCollection.Insert(new ServerStats(endpoint, serverInfo.name));
            }
            else
            {
                serverStatsRecord.name = serverInfo.name;
                serverStatsCollection.Update(serverStatsRecord);
            }
                         

        }

        public void AddMatch(string endpoint, string timestamp, MatchInfo matchInfo)
        {
            lastMatch = TimestampStrategy.GetMax(lastMatch, timestamp);
            matchesCollection
                .Insert(new  MatchData(endpoint, timestamp, matchInfo));
            
            //Updating players' stats
            for(int place = 0; place < matchInfo.scoreboard.Count(); ++place)
            {
                PlayerStats playerStats = 
                    playerStatsCollection.FindById(matchInfo.scoreboard[place].name);
                if (playerStats == null)
                {
                    playerStats = new PlayerStats(matchInfo.scoreboard[place].name);
                }

                double scoreboardPercent = 
                    (matchInfo.scoreboard.Count() - place - 1) / 
                    (matchInfo.scoreboard.Count() - 1) * 100;
                
                playerStats.Update(endpoint, timestamp, matchInfo.gameMode,
                    scoreboardPercent, matchInfo.scoreboard[place]);
                
                playerStatsCollection.Upsert(playerStats);
            }

            //Updating servers' stats
            ServerStats serverStats =
                serverStatsCollection.FindById(endpoint);
            serverStats.Update(new  MatchData(endpoint, timestamp, matchInfo), lastMatch);

            serverStatsCollection.Upsert(serverStats);

        }

        //GET

        public string GetServerInfo(string endpoint)
        {
            ServerInfo answer = serversCollection.FindById(endpoint).info;
            return JsonConvert.SerializeObject(answer);
        }

        public string GetMatchInfo(string endpoint, string timestamp)
        {
            MatchInfo answer = matchesCollection
                .FindOne(x => 
                    (x.endpoint.Equals(endpoint) && x.timestamp.Equals(timestamp))).info;
            return JsonConvert.SerializeObject(answer);
        }

        public string GetAllServersInfo()
        {
            var answer = serversCollection.FindAll();
            return JsonConvert.SerializeObject(answer);
        }



        public string GetServerStats(string endpoint)
        {
            string answer = serverStatsCollection
                .FindById(endpoint)
                .GetReport(lastMatch);
            return answer;
        }

        public string GetPlayerStats(string playerName)
        {
            string answer = playerStatsCollection
                .FindById(playerName)
                .GetReport(lastMatch);
            return answer;
        }

        //reports
        public string GetRecentMatches(int count)
        {

            var answer = matchesCollection
                .Find(Query.All("timestamp", Query.Descending), limit:count);
            return JsonConvert.SerializeObject(answer);
        }

        public string GetBestPlayers(int count)
        {

            var bestPlayers = playerStatsCollection
                .Find(Query.All("killToDeathRatio", Query.Descending), limit:count);
            ArrayList answer = new ArrayList();
            foreach(PlayerStats playerStats in bestPlayers)
            {
                if(playerStats.killToDeathRatioDBIndex >= 0)
                    answer.Add(playerStats.GetPlayerGoodness());
            }
            return JsonConvert.SerializeObject(answer);

        }

        public string GetPopularServers(int count)
        {
            RecalcPopularity();

            var popularServers = serverStatsCollection
                .Find(Query.All("averageMatchesPerDay", Query.Descending), limit:count);
            ArrayList answer = new ArrayList();
            foreach(ServerStats serverStats in popularServers)
            {
                answer.Add(serverStats.GetServerPopularity(lastMatch));
            }
            return JsonConvert.SerializeObject(answer);
        
        }

        private void RecalcPopularity()
        {
            if (TimestampStrategy.GetDayDifference(lastRecalc, lastMatch) <= 1)
                return;
            
            foreach (ServerStats stats in serverStatsCollection.FindAll())
            {
                stats.Update(lastMatch);
                serverStatsCollection.Update(stats);
            }
        }

        private LiteDatabase db;
        private LiteCollection<MatchData> matchesCollection;
        private LiteCollection<ServerData> serversCollection;
        private LiteCollection<PlayerStats> playerStatsCollection;
        private LiteCollection<ServerStats> serverStatsCollection;

        private string lastMatch;
        private string lastRecalc;

        private readonly string dbPath = "storage.db";


    }
}

