using MusicBot2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBot2.RIOTService
{
    public class GetChampService
    {
        public string version { get; set; }
        public ChampVM? allChampionData;
        
        public GetChampService() 
        {
            // 先查詢目前最新的版本
            string versionUrl = $"https://ddragon.leagueoflegends.com/api/versions.json";
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(versionUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var versions = JsonConvert.DeserializeObject<List<string>>(content);
                    version = versions[0];
                }
            }

            // 載入所有英雄基本資料
            LoadAllChampions();
        }

        /// <summary>
        /// 載入所有英雄的基本資料
        /// </summary>
        private void LoadAllChampions()
        {
            string champListUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/data/zh_TW/champion.json";
            
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(champListUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    allChampionData = JsonConvert.DeserializeObject<ChampVM>(content);
                    
                }
            }
        }

        /// <summary>
        /// 取得特定英雄的詳細資料（包含技能）
        /// </summary>
        /// <param name="champName">英雄名稱（例如："Ahri", "Yasuo"）</param>
        /// <returns>英雄詳細資料的 JSON 字串</returns>
        public string GetChampSkills(string champName)
        {
            try
            {
                // 建立詳細資料的 URL
                string champDetailUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/data/zh_TW/champion/{champName}.json";
                
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(champDetailUrl).Result;
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        var champDetail = JsonConvert.DeserializeObject<OnlyChampVM>(content);
                        
                        // 取得該英雄的資料（data 字典只會有一個英雄）
                        var champion = champDetail?.data?.Values.FirstOrDefault();
                        
                        if (champion != null)
                        {
                            StringBuilder result = new StringBuilder();
                            result.AppendLine($"英雄：{champion.name} - {champion.title}");
                            result.AppendLine($"簡介：{champion.blurb}");
                            result.AppendLine("\n技能列表：");
                            
                            // 列出所有技能
                            foreach (var spell in champion.spells)
                            {
                                result.AppendLine($"\n【{spell.name}】");
                                result.AppendLine($"描述：{spell.description}");
                            }
                            
                            return result.ToString();
                        }
                        else
                        {
                            return "找不到該英雄資料";
                        }
                    }
                    else
                    {
                        return $"API 請求失敗：{response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"取得英雄技能時發生錯誤：{ex.Message}";
            }
        }

        /// <summary>
        /// 列出所有英雄名稱
        /// </summary>
        /// <returns>所有英雄名稱列表</returns>
        public List<string> GetAllChampionNames()
        {
            if (allChampionData?.data == null)
                return new List<string>();

            return allChampionData.data.Values
                .Select(champ => $"{champ.name} ({champ.id})")
                .ToList();
        }

        /// <summary>
        /// 根據中文名稱搜尋英雄 ID
        /// </summary>
        /// <param name="chineseName">中文名稱</param>
        /// <returns>英雄 ID（用於 API 查詢）</returns>
        public string? FindChampionIdByName(string chineseName)
        {
            if (allChampionData?.data == null)
                return null;

            var champion = allChampionData.data.Values
                .FirstOrDefault(c => c.name.Contains(chineseName) || c.title.Contains(chineseName));

            return champion?.id;
        }
    }
}
