using System;
using System.Collections.Generic;
using LiteDB;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;

namespace Kontur.GameStats.Server
{
    internal class ServerInfo
    {
        public ServerInfo()
        {

        }
        [BsonIndex][BsonId]
        public string name{ get; set;}
        public string[] gameModes{ get; set;}

    }

    internal class ServerData
    {
        public ServerData()
        {
            info = new ServerInfo();
        }
        public ServerData(string _endpoint, ServerInfo _info)
        {
            endpoint = _endpoint;
            info = _info;

        }
        [BsonId] [BsonIndex]
        public string endpoint{ get; set;}
        public ServerInfo info{ get; set;}

    }

    internal class ServerStatsReport
    {
        
        public int totalMatchesPlayed { get; set;}
        public int maximumMatchesPerDay{ get; set;}
        public double averageMatchesPerDay{ get; set;}
        public int maximumPopulation{ get; set;}
        public double averagePopulation{ get; set;}
        public string[] top5GameModes{ get; set;}
        public string[] top5Maps{ get; set;}

        public ServerStatsReport()
        {
        }
    }
    internal class ServerPopularity
    {
        public ServerPopularity()
        {
        }
        public string endpoint{ get; set; }
        public string name{ get; set; }
        public double averageMatchesPerDay{ get; set; }

    }

    internal class ServerStats
    {
        public ServerStats()
        {
            endpoint = "Default";
            name = "Default";
            totalMatchesPlayed = 0;
            matchesPerDay = new Dictionary<string, int>();
            totalPopulation = 0;
            maximumPopulation = 0;
            firstMatch = "5000-12-30T00:00:00Z"; //anyway, greater than any possible timestamp
            gameModePopularity = new Dictionary<string, int>();
            mapPopularity = new Dictionary<string, int>();
        }

        public ServerStats(string endpoint, string name)
        {
            this.endpoint = endpoint;
            this.name = name;
            totalMatchesPlayed = 0;
            firstMatch = "5000-12-30T00:00:00Z"; //anyway, greater than any possible timestamp
            matchesPerDay = new Dictionary<string, int>();
            totalPopulation = 0;
            maximumPopulation = 0;
            gameModePopularity = new Dictionary<string, int>();
            mapPopularity = new Dictionary<string, int>();
        }

        public ServerPopularity GetServerPopularity(string lastMatch)
        {
            ServerPopularity answer = new ServerPopularity();
            answer.endpoint = endpoint;
            answer.name = name;
            answer.averageMatchesPerDay = averageMatchesPerDay;
            return answer;
        }

        public string GetReport(string lastMatch) 
        {
            ServerStatsReport report = new ServerStatsReport();
            report.totalMatchesPlayed = totalMatchesPlayed;

            report.maximumMatchesPerDay = matchesPerDay.Values.Max();   

            report.averageMatchesPerDay = averageMatchesPerDay;

            report.maximumPopulation = maximumPopulation;
            report.averagePopulation = (double)totalPopulation 
                / totalMatchesPlayed;
            var gameModes = gameModePopularity
                .OrderByDescending(x => x.Value)
                .Take(5);
            
            report.top5GameModes = new string[gameModes.Count()];
            int counter = 0;
            foreach(var mode in gameModes)
                report.top5GameModes[counter++] = mode.Key;
                     
                

            var maps = mapPopularity
                .OrderByDescending(x => x.Value)
                .Take(5);

            report.top5Maps = new string[maps.Count()];
            counter = 0;
            foreach (var map in maps)
                report.top5Maps[counter++] = map.Key;

            return JsonConvert.SerializeObject(report);
                       

        }

        public void Update(MatchData match, string lastMatch)
        {
            totalMatchesPlayed ++;

            if (matchesPerDay.ContainsKey(match.timestamp))
                matchesPerDay[match.timestamp] ++;
            else
                matchesPerDay.Add(match.timestamp, 1);
            
            totalPopulation += match.info.scoreboard.Count();

            if (maximumPopulation <= match.info.scoreboard.Count())
                maximumPopulation = match.info.scoreboard.Count();

            firstMatch = TimestampStrategy
                .GetMin(firstMatch, match.timestamp);

            if (mapPopularity.ContainsKey(match.info.map))
                mapPopularity[match.info.map]++;
            else
                mapPopularity.Add(match.info.map, 1);

            if (mapPopularity.ContainsKey(match.info.map))
                mapPopularity[match.info.map]++;
            else
                mapPopularity.Add(match.info.map, 1);

            if (gameModePopularity.ContainsKey(match.info.gameMode))
                gameModePopularity[match.info.gameMode]++;
            else
                gameModePopularity.Add(match.info.gameMode, 1);

            Update(lastMatch);

        }

        public void Update(string lastMatch)
        {
            averageMatchesPerDay = (double) matchesPerDay.Values.Sum() / 
                TimestampStrategy
                .GetDayDifference
                (
                    matchesPerDay.Keys.Min() + "T01:00:00Z", 
                    lastMatch
                );
        }

        [BsonId][BsonIndex]
        public string endpoint{ get; set;}
        public string name{ get; set; }
        public string firstMatch{ get; set;}
        public int totalMatchesPlayed{ get; set;}
        public Dictionary<string, int> matchesPerDay{ get; set;}
        [BsonIndex]
        public double averageMatchesPerDay{ get; set;}
        public int totalPopulation{ get; set;}
        public int maximumPopulation{ get; set;}
        public Dictionary<string, int> gameModePopularity{ get; set;}
        public Dictionary<string, int> mapPopularity{ get; set;}
    }

}

