using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using LiteDB;
namespace Kontur.GameStats.Server
{
    
   
    internal class PlayerBehavior 
    {
        public PlayerBehavior(){}
        public string name{ get; set;}
        public int frags{ get; set;}
        public int kills{ get; set;}
        public int deaths{ get; set;}
    }

    internal class MatchInfo
    {
        public MatchInfo(){}
        public string map{ get; set;}
        public string gameMode{ get; set;}
        public int fragLimit{ get; set;}
        public int timeLimit{ get; set;}
        public double timeElapsed{ get; set;}
        public PlayerBehavior[] scoreboard{ get; set;}

    }



    internal class MatchData
    {      
        public MatchData(string _endpoint, string _timestamp, MatchInfo _info)
        {
            endpoint = _endpoint;
            timestamp = _timestamp;
            info = _info;
        }
        public MatchData()
        {
            info = new MatchInfo();
        }
        [BsonIndex]
        public string endpoint{ get; set;}
        [BsonIndex]
        public string timestamp{ get; set;}

        public MatchInfo info{ get; set;}
    }


    

  

}