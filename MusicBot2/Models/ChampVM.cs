using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBot2.Models
{
    public class OnlyChampVM
    {
        public string type { get; set; }
        public string format { get; set; }
        public string version { get; set; }
        public Dictionary<string, OnlyChampion> data { get; set; }
    }

    public class OnlyChampion
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string blurb { get; set; }
        public List<string> allytips { get; set; }
        public List<string> enemytips { get; set; }
        public List<spells> spells { get; set; }
    }

    public class spells
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string tooltip { get; set; }
    }

    public class passive
    {
        public string name { get; set; }
        public string description { get; set; }
    }



    public class ChampVM
    {
        public string type { get; set; }
        public string format { get; set; }
        public string version { get; set; }

        public Dictionary<string, AllChampion> data { get; set; }
        public info info { get; set; }
        public stats stats { get; set; }
    }

    public class AllChampion
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string blurb { get; set; }

    }

    public class info
    {
        public int attack { get; set; }
        public int defense { get; set; }
        public int magic { get; set; }
        public int difficulty { get; set; }
    }

    public class stats
    {
        public int hp { get; set; }
        public int hpperlevel { get; set; }
        public int mp { get; set; }
        public int mpperlevel { get; set; }
        public int movespeed { get; set; }
        public int armor { get; set; }
        public int armorperlevel { get; set; }
        public int spellblock { get; set; }
        public int spellblockperlevel { get; set; }
        public int attackrange { get; set; }
        public int hpregen { get; set; }
        public int hpregenperlevel { get; set; }
        public int mpregen { get; set; }
        public int mpregenperlevel { get; set; }
        public int crit { get; set; }
        public int critperlevel { get; set; }
        public int attackdamage { get; set; }
        public int attackdamageperlevel { get; set; }
        public int attackspeedperlevel { get; set; }
        public int attackspeed { get; set; }
    }
}
