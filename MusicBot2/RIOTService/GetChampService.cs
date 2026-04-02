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
                    await _channel.SendMessageAsync($"找不到，這她媽是誰: {searchTerm}");
                    return;
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
                            var random = RandomChampColor();

                            // 英雄頭像圖片 URL
                            string championImageUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{championId}.png";

                            // 创建一个新的 EmbedBuilder
                            var embedBuilder = new EmbedBuilder()
                            {
                                Title = $"⚔️ {AddSomeFuckingWords(champion.name)}",
                                Description = $"*{AddSomeFuckingWords(champion.title)}*",
                                Color = random,
                                ThumbnailUrl = championImageUrl,
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"裡狗李 - 遊戲版本 {version}"
                                },
                                Timestamp = DateTimeOffset.Now
                            };

                            // 英雄簡介
                            embedBuilder.AddField("📖 英雄簡介", AddSomeFuckingWords(champion.blurb), false);

                            embedBuilder.AddField("━━━━━━━━━━━━━━━━━━━━━━", "** **", false);

                            // 被動技能
                            if (champion.passive != null)
                            {
                                string passiveIconUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/img/passive/{champion.passive.image.full}";

                                embedBuilder.AddField(
                                    $"🔮 被動 - {champion.passive.name}",
                                    $"{CleanHtmlTags(AddSomeFuckingWords(champion.passive.description))}\n** **",
                                    false
                                );
                            }

                            // 列出所有技能
                            if (champion.spells != null)
                            {
                                for (int i = 0; i < champion.spells.Count; i++)
                                {
                                    var spell = champion.spells[i];
                                    string skillKey = i switch
                                    {
                                        0 => "🇶 Q",
                                        1 => "🇼 W",
                                        2 => "🇪 E",
                                        3 => "🇷 R",
                                        _ => $"{i + 1}"
                                    };

                                    //// 技能冷卻時間
                                    //string cooldown = spell.cooldown != null && spell.cooldown.Count > 0
                                    //    ? $"⏱️ 冷卻: {string.Join("/", spell.cooldown)} 秒"
                                    //    : "";

                                    //// 技能消耗
                                    //string cost = spell.cost != null && spell.cost.Count > 0
                                    //    ? $"💧 消耗: {string.Join("/", spell.cost)} {spell.costType}"
                                    //    : "";

                                    //string additionalInfo = !string.IsNullOrEmpty(cooldown) || !string.IsNullOrEmpty(cost)
                                    //    ? $"\n{cooldown} {cost}"
                                    //    : "";

                                    embedBuilder.AddField(
                                        $"{skillKey} - {spell.name}",
                                        $"{CleanHtmlTags(AddSomeFuckingWords(spell.description))}\n** **",
                                        false
                                    );
                                }
                            }

                            await _channel.SendMessageAsync(embed: embedBuilder.Build());
                        }
                        else
                        {
                            await _channel.SendMessageAsync($"白癡riot不給資料怪我瞜: {searchTerm}");
                        }
                    }
                    else
                    {
                        await _channel.SendMessageAsync($"白癡riot不給資料怪我瞜: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _channel.SendMessageAsync($"白癡riot不給資料怪我瞜: {ex.Message}");
            }
        }

        public async Task GuessChampSkillAsync(string champName, string skillPos, string userGuess)
        {
            string correctSkillName = string.Empty;

            // 先透過智能搜尋找到正確的英雄 ID
            var championId = FindChampionId(champName);

            if (string.IsNullOrEmpty(championId))
            {
                await _channel.SendMessageAsync($"找不到，這她媽是誰: {champName}");
                return;
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
                        if (skillPos.ToLower() == "p")
                        {
                            correctSkillName = champion.passive.name;
                        }
                        else
                        {
                            int index = skillPos.ToLower() switch
                            {
                                "q" => 0,
                                "w" => 1,
                                "e" => 2,
                                "r" => 3,
                                _ => -1
                            };

                            if (index >= 0 && index < champion.spells.Count)
                            {
                                correctSkillName = champion.spells[index].name;
                            }
                        }

                        if(correctSkillName.ToLower().Trim().Contains(userGuess))
                        {
                            if (userGuess == userGuess.ToLower().Trim())
                            {
                                await _channel.SendMessageAsync($"完全被你賽到瞜，獎勵你{GetRandomRewards()}");
                            }
                            else
                            {
                                await _channel.SendMessageAsync($"被你賽到一半，獎勵你{GetRandomRewards()}");
                            }
                        }
                        else
                        {
                            await _channel.SendMessageAsync($"傻逼，亂猜一通，你不知道弒君知道: {correctSkillName}");
                        }
                    }
                    else
                    {
                        await _channel.SendMessageAsync($"白癡riot不給資料怪我瞜: {champName}");
                    }
                }
            }
        }

        /// <summary>
        /// 清除 HTML 標籤並格式化技能描述
        /// </summary>
        private string CleanHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 移除 HTML 標籤
            text = System.Text.RegularExpressions.Regex.Replace(text, "<br>", "\n");
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");

            // 替換遊戲內特殊標記
            text = text.Replace("@Effect1Amount@", "X")
                        .Replace("@Effect2Amount@", "Y")
                        .Replace("@Effect3Amount@", "Z");

            return text;
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

        private Color RandomChampColor()
        {
            var colors = new List<Color>
            {
                new Color(0, 149, 255),      // 藍色
                new Color(255, 77, 77),      // 紅色
                new Color(255, 184, 77),     // 橘色
                new Color(153, 0, 204),      // 紫色
                new Color(0, 204, 102),      // 綠色
                new Color(255, 204, 0),      // 金色
                new Color(230, 0, 126),      // 粉紅色
                new Color(0, 204, 204),      // 青色
            };

            var random = new Random();
            return colors[random.Next(colors.Count)];
        }

        private string GetRandomRewards()
        {
            var rewards = new List<string>
            {
                "一首戰神阿基里斯",
                "和聽葳葳夢夢一起去日本",
                "去偷一台停在地下室還沒鎖的腳踏車",
                "去花蓮找綠開心",
                "被小魚告上法庭",
                "一把雙刀流柔伊",
                "和初華睡一晚",
                "被傻屌愛音摳",
                "一首夏夜晚風 芒果醬ver.",
                "玩一小時 furry shades of gay",
                "沒有墨水存檔，直接重頭開始",
                "一份免費的discord硝基",
                "一張陳俊佑的一日份屁眼使用卷",
                "一張劉宗漢的一日份屁眼使用卷",
                "一張陳偉鵬的一日份屁眼使用卷",
                "一張吳霈辰的一日份屁眼使用卷",
                "和外星人一起遨遊桃子腳",
                "和王議員一起參選",
                "邊洗澡邊尿尿",
                "和柯比勞大一起搭直升機",
                "和買狗成員一起演唱"
            };

            var random = new Random();
            return rewards[random.Next(rewards.Count)];
        }

        private string AddSomeFuckingWords(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var fuckingWords = new List<string>
            {
                "他媽的",
                "羅傑的",
                "芒果醬的",
                "fucking",
                "偷腳踏車的",
                "外星人的",
                "雙刀流的",
                "你媽的",
                "桃子腳的",
                "少一顆腎的"
            };

            var random = new Random();
            int ranCount = 0;

            if (input.Length < 10)
            {
                ranCount = random.Next(1, 2);
            }
            else if (input.Length < 30)
            {
                ranCount = random.Next(1, 3);
            }
            else if (input.Length >= 30)
            {
                ranCount = random.Next(1, 6);
            }

            int originalLength = input.Length;

            var insertions = new Dictionary<int, string>();

            for (int i = 0; i < ranCount; i++)
            {
                int position = random.Next(originalLength);

                while (insertions.ContainsKey(position))
                {
                    position = random.Next(originalLength);
                }

                insertions[position] = fuckingWords[random.Next(fuckingWords.Count)];
            }

            // 按位置排序後從後往前插入，避免位置偏移
            var result = new StringBuilder(input);
            foreach (var kvp in insertions.OrderByDescending(x => x.Key))
            {
                result.Insert(kvp.Key, kvp.Value);
            }

            return result.ToString();
        }
    }
}
