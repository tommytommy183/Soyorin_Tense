using Discord;
using Discord.WebSocket;
using MusicBot2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBot2.RIOTService
{
    public class GetChampService
    {
        public string version { get; set; }
        public ChampVM? allChampionData;
        private const string versionFilePath = "league_version.txt";
        private const string champDataFilePath = "AllChamp.json";
        private readonly SocketMessage _message;
        private readonly IMessageChannel _channel;

        public GetChampService(SocketMessage message) 
        {
            _message = message;
            _channel = message.Channel as IMessageChannel;
            // 檢查並更新版本資訊
            CheckAndUpdateVersion();

            // 載入所有英雄基本資料
            LoadAllChampions();
        }

        /// <summary>
        /// 檢查並更新版本資訊
        /// </summary>
        private void CheckAndUpdateVersion()
        {
            bool shouldUpdateVersion = false;
            
            try
            {
                if (File.Exists(versionFilePath))
                {
                    var lines = File.ReadAllLines(versionFilePath);
                    if (lines.Length >= 2)
                    {
                        string savedVersion = lines[0];
                        string savedDateStr = lines[1];
                        
                        if (string.IsNullOrWhiteSpace(savedDateStr) || 
                            !DateTime.TryParse(savedDateStr, out DateTime savedDate))
                        {
                            shouldUpdateVersion = true;
                        }
                        else if ((DateTime.Now - savedDate).TotalDays >= 30)
                        {
                            shouldUpdateVersion = true;
                        }
                        else
                        {
                            version = savedVersion;
                        }
                    }
                    else
                    {
                        shouldUpdateVersion = true;
                    }
                }
                else
                {
                    shouldUpdateVersion = true;
                }
                
                if (shouldUpdateVersion)
                {
                    UpdateVersionFromApi();
                }
            }
            catch
            {
                UpdateVersionFromApi();
            }
        }

        /// <summary>
        /// 從 API 更新版本並儲存到檔案
        /// </summary>
        private void UpdateVersionFromApi()
        {
            string versionUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(versionUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var versions = JsonConvert.DeserializeObject<List<string>>(content);
                    version = versions[0];
                    
                    // 儲存版本和日期到檔案
                    File.WriteAllLines(versionFilePath, new[] 
                    { 
                        version, 
                        DateTime.Now.ToString("yyyy-MM-dd") 
                    });
                }
            }
        }

        /// <summary>
        /// 載入所有英雄的基本資料
        /// </summary>
        private void LoadAllChampions()
        {
            bool shouldUpdateChampData = false;
            
            // 檢查本地檔案是否存在
            if (File.Exists(champDataFilePath))
            {
                try
                {
                    // 讀取本地檔案
                    string jsonContent = File.ReadAllText(champDataFilePath);
                    var localData = JsonConvert.DeserializeObject<ChampVM>(jsonContent);
                    
                    // 比較版本是否一致
                    if (localData?.version != version)
                    {
                        shouldUpdateChampData = true;
                    }
                    else
                    {
                        // 版本相同,直接使用本地資料
                        allChampionData = localData;
                        return;
                    }
                }
                catch
                {
                    // 讀取失敗,需要重新從 API 取得
                    shouldUpdateChampData = true;
                }
            }
            else
            {
                // 檔案不存在,需要從 API 取得
                shouldUpdateChampData = true;
            }
            
            // 需要更新時才呼叫 API
            if (shouldUpdateChampData)
            {
                UpdateChampDataFromApi();
            }
        }

        /// <summary>
        /// 從 API 更新英雄資料並儲存到檔案
        /// </summary>
        private void UpdateChampDataFromApi()
        {
            string champListUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/data/zh_TW/champion.json";
            
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(champListUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    allChampionData = JsonConvert.DeserializeObject<ChampVM>(content);
                    
                    // 儲存到本地檔案
                    File.WriteAllText(champDataFilePath, content);
                }
            }
        }

        /// <summary>
        /// 智能搜尋英雄 ID (支援英文ID、中文名、稱號)
        /// </summary>
        /// <param name="searchTerm">搜尋關鍵字</param>
        /// <returns>英雄 ID,找不到則返回 null</returns>
        public string? FindChampionId(string searchTerm)
        {
            if (allChampionData?.data == null)
                return null;

            searchTerm = searchTerm.Trim();

            // 1. 先嘗試直接用 ID 匹配 (例如: "Aatrox")
            if (allChampionData.data.ContainsKey(searchTerm))
            {
                return searchTerm;
            }

            // 2. 不區分大小寫的 ID 匹配
            var championByIdIgnoreCase = allChampionData.data
                .FirstOrDefault(kvp => kvp.Key.Equals(searchTerm, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(championByIdIgnoreCase.Key))
            {
                return championByIdIgnoreCase.Value.id;
            }

            // 3. 用中文名稱或稱號搜尋
            var champion = allChampionData.data.Values
                .FirstOrDefault(c => 
                    c.name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                    c.title.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.name.Contains(searchTerm) || 
                    c.title.Contains(searchTerm));

            return champion?.id;
        }

        /// <summary>
        /// 取得特定英雄的詳細資料(包含技能)
        /// </summary>
        /// <param name="searchTerm">英雄名稱、ID 或稱號</param>
        /// <returns>英雄詳細資料的 JSON 字串</returns>
        public async Task GetChampSkillsAsync(string searchTerm)
        {
            try
            {
                // 先透過智能搜尋找到正確的英雄 ID
                var championId = FindChampionId(searchTerm);
                
                if (string.IsNullOrEmpty(championId))
                {
                    await _channel.SendMessageAsync($"找不到英雄: {searchTerm}");
                }

                // 建立詳細資料的 URL
                string champDetailUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/data/zh_TW/champion/{championId}.json";
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(champDetailUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var champDetail = JsonConvert.DeserializeObject<OnlyChampVM>(content);
                        
                        // 取得該英雄的資料(data 字典只會有一個英雄)
                        var champion = champDetail?.data?.Values.FirstOrDefault();
                        
                        if (champion != null)
                        {
                            StringBuilder result = new StringBuilder();
                            result.AppendLine($"英雄: {champion.name} - {champion.title}");
                            result.AppendLine($"簡介: {champion.blurb}");
                            result.AppendLine("\n技能列表:");
                            
                            // 被動技能
                            if (champion.passive != null)
                            {
                                result.AppendLine($"\n【被動 - {champion.passive.name}】");
                                result.AppendLine($"描述: {champion.passive.description}");
                            }
                            
                            // 列出所有技能
                            if (champion.spells != null)
                            {
                                for (int i = 0; i < champion.spells.Count; i++)
                                {
                                    var spell = champion.spells[i];
                                    string skillKey = i switch
                                    {
                                        0 => "Q",
                                        1 => "W",
                                        2 => "E",
                                        3 => "R",
                                        _ => $"{i + 1}"
                                    };
                                    
                                    result.AppendLine($"\n【{skillKey} - {spell.name}】");
                                    result.AppendLine($"描述: {spell.description}");
                                }
                            }
                            await _channel.SendMessageAsync(result.ToString());
                        }
                        else
                        {
                            await _channel.SendMessageAsync($"找不到英雄詳細資料: {searchTerm}");
                        }
                    }
                    else
                    {
                        await _channel.SendMessageAsync($"API 請求失敗: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _channel.SendMessageAsync(ex.Message); 
            }
        }

        /// <summary>
        /// 列出所有英雄名稱
        /// </summary>
        /// <returns>所有英雄名稱列表</returns>
        public async Task<List<string>> GetAllChampionNames()
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
        /// <returns>英雄 ID(用於 API 查詢)</returns>
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
