using System;
using System.Collections.Generic;
using LiteDB;
using System.Linq;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    internal class PlayerStatsReport
    {
        public PlayerStatsReport(){}
        public int totalMatchesPlayed{ get; set;}
        public int totalMatchesWon{ get; set;}
        public string favoriteServer{ get; set;}
        public int uniqueServers{ get; set;}
        public string favoriteGameMode{ get; set;}
        public double averageScoreboardPercent{ get; set;}
        public int maximumMatchesPerDay{ get; set;}
        public double averageMatchesPerDay{ get; set;}
        public string lastMatchPlayed{ get; set;}
        public double killToDeathRatio{ get; set;}
    }

    internal class PlayerGoodness
    {
        public PlayerGoodness(){}
        public string name{ get; set;}
        public double killToDeathRatio{ get; set;}
    }

    internal class PlayerStats
    {
        public PlayerStats(string name)
        {

            this.name = name;
            totalMatchesPlayed = 0;
            totalMatchesWon = 0;
            serversPopularity = new Dictionary<string, int> ();
            gameModePopularity = new Dictionary<string, int> ();
            averageScoreboardPercent = 0;
            matchesPerDay = new Dictionary<string, int> ();
            lastMatchPlayed = "0001-01-01T00:00:01Z";
            killsNumber = 0;
            deathsNumber = 0;
            killToDeathRatioDBIndex = -1;
        }

        public PlayerStats()
        {

            this.name = "Default";
            totalMatchesPlayed = 0;
            totalMatchesWon = 0;
            serversPopularity = new Dictionary<string, int> ();
            gameModePopularity = new Dictionary<string, int> ();
            averageScoreboardPercent = 0;
            matchesPerDay = new Dictionary<string, int> ();
            lastMatchPlayed ="0001-01-01T00:00:01Z";
            killsNumber = 0;
            deathsNumber = 0;
            killToDeathRatioDBIndex = -1;
        }

        public PlayerGoodness GetPlayerGoodness()
        {
            PlayerGoodness answer = new PlayerGoodness();

            answer.name = this.name;

            answer.killToDeathRatio = this.killToDeathRatioDBIndex;

            return answer;
        }

        public void Update(
            string endpoint, 
            string timestamp, 
            string gameMode, 
            double scoreboardPercent, 
            PlayerBehavior behavior
        )
        {
            string serverKey = endpoint;

            if (serversPopularity.ContainsKey(serverKey))
                serversPopularity[serverKey]++;
            else
                serversPopularity.Add(serverKey, 1);

            totalMatchesPlayed++;

            if (scoreboardPercent == 100)
                totalMatchesWon++;

            if (gameModePopularity.ContainsKey(gameMode))
                gameModePopularity[gameMode]++;
            else
                gameModePopularity.Add(gameMode, 1);

            averageScoreboardPercent = 
                (averageScoreboardPercent * (totalMatchesPlayed - 1) + scoreboardPercent) 
                / totalMatchesPlayed;

            string day = TimestampStrategy.GetDay(timestamp);
            if (matchesPerDay.ContainsKey(day))
                matchesPerDay[day]++;
            else
                matchesPerDay.Add(day, 1);

            lastMatchPlayed = TimestampStrategy.GetMax(lastMatchPlayed, timestamp);

            killsNumber += behavior.kills;
            deathsNumber += behavior.deaths;

            killToDeathRatioDBIndex = 
                (deathsNumber == 0 || totalMatchesPlayed < 10) 
                ? -1 
                : killsNumber / deathsNumber;

        }

        public string GetReport(string lastMatch)
        {
            PlayerStatsReport report = new PlayerStatsReport();

            report.totalMatchesPlayed = totalMatchesPlayed;

            report.totalMatchesWon = totalMatchesWon;

            string favoriteServer = serversPopularity.Keys.ElementAt(0);
            foreach(string server in serversPopularity.Keys)
            {
                if (serversPopularity[server] > serversPopularity[favoriteServer])
                {
                    favoriteServer = server;
                }
            }
            report.favoriteServer = favoriteServer;

            report.uniqueServers = serversPopularity.Count();

            string favoriteGameMode = gameModePopularity.Keys.ElementAt(0);
            foreach(string gameMode in gameModePopularity.Keys)
            {
                if (gameModePopularity[gameMode] > gameModePopularity[favoriteGameMode])
                {
                    favoriteGameMode = gameMode;
                }
            }
            report.favoriteGameMode = favoriteGameMode;

            report.averageScoreboardPercent = averageScoreboardPercent;

            report.maximumMatchesPerDay = matchesPerDay.Values.Max();  

            report.averageMatchesPerDay = (double)matchesPerDay.Values.Sum() / 
                TimestampStrategy.GetDayDifference(matchesPerDay.Keys.Min() + "T01:00:00Z", lastMatch);

            report.lastMatchPlayed = lastMatchPlayed;

            report.killToDeathRatio = killsNumber / deathsNumber;
            return JsonConvert.SerializeObject(report);

        }



        [BsonId][BsonIndex]
        public string name{ get; set;}
        [BsonIndex]
        public int totalMatchesPlayed{ get; set;}
        public int totalMatchesWon{ get; set;}
        public string firstMatch{ get; set;}
        public Dictionary<string, int> serversPopularity{ get; set;}
        public Dictionary<string, int> gameModePopularity{ get; set;}
        public double averageScoreboardPercent{ get; set;}
        public Dictionary<string, int> matchesPerDay{ get; set;}
        public string lastMatchPlayed{ get; set;}
        public int killsNumber{ get; set;}
        [BsonIndex]
        public int deathsNumber{ get; set;}
        [BsonIndex]
        public int killToDeathRatioDBIndex{ get; set;}
        /*{
            "totalMatchesPlayed": 100500,
            "totalMatchesWon": 1000,
            "favoriteServer": "62.210.26.88-1337",
            "uniqueServers": 2,
            "favoriteGameMode": "DM",
            "averageScoreboardPercent": 76.145693,
            "maximumMatchesPerDay": 33,
            "averageMatchesPerDay": 24.456240,
            "lastMatchPlayed": "2017-01-22T15:11:12Z",
            "killToDeathRatio": 3.124333
        }*/
    }
}

