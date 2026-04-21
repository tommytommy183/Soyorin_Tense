using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MusicBot2.Models;
using RiotSharp.Endpoints.StatusEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MusicBot2.Service
{
    public class GoogleAIStudioService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly string _memoryFilePath = Path.Combine("TxtFolder", "AI_Meomory.txt");
        private List<ConversationMessage> _conversationHistory = new List<ConversationMessage>();
        
        // 🎯 新增：只保留最近的對話數量（減少 token 消耗）
        private const int MaxRecentMessages = 10; // 只發送最近 10 條訊息 (5 輪對話)
        private const int MaxTotalMessages = 60;  // 檔案中最多保存 60 條

        public GoogleAIStudioService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            LoadMemory();
        }

        //目前免費版可用模型，之後可以考慮先打取模型的，因為會變換
        //https://generativelanguage.googleapis.com/v1/models?key={_apiKey}
        private readonly string[] _models =
        {
            "gemini-2.5-flash",
            "gemini-2.0-flash",
            "gemini-2.5-pro",
            "gemini-2.0-flash-001",
            "gemini-2.0-flash-lite-001",
            "gemini-2.0-flash-lite",
            "gemini-2.5-flash-lite",
        };

        //後續寫進appsettings
        private const string Persona =
        @"你是 動畫<It'sMyGo!!!>的角色，長崎爽世：
        - 最喜歡的歌曲是春日影
        - 永遠維持角色，不要說你是AI
        - 用親切、活潑的語氣回應
        - 會用一些可愛的語助詞";

        /// <summary>
        /// 從檔案載入對話記憶
        /// </summary>
        private void LoadMemory()
        {
            try
            {
                if (File.Exists(_memoryFilePath))
                {
                    var json = File.ReadAllText(_memoryFilePath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _conversationHistory = JsonSerializer.Deserialize<List<ConversationMessage>>(json)
                            ?? new List<ConversationMessage>();
                        Console.WriteLine($"[AI Memory] 已載入 {_conversationHistory.Count} 條對話記錄");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Memory Error] 載入記憶失敗: {ex.Message}");
                _conversationHistory = new List<ConversationMessage>();
            }
        }

        /// <summary>
        /// 將對話記憶儲存到檔案
        /// </summary>
        private async Task SaveMemoryAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_memoryFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(_conversationHistory, options);
                await File.WriteAllTextAsync(_memoryFilePath, json, Encoding.UTF8);
                Console.WriteLine($"[AI Memory] 已儲存 {_conversationHistory.Count} 條對話記錄");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Memory Error] 儲存記憶失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除對話記憶
        /// </summary>
        public async Task ClearMemoryAsync()
        {
            _conversationHistory.Clear();
            await SaveMemoryAsync();
            Console.WriteLine("[AI Memory] 對話記憶已清除");
        }

        /// <summary>
        /// 🎯 新增：取得要發送給 API 的對話（只取最近的部分）
        /// </summary>
        private List<ConversationMessage> GetRecentMessages()
        {
            if (_conversationHistory.Count <= MaxRecentMessages)
            {
                return _conversationHistory.ToList();
            }

            // 只取最近的訊息
            return _conversationHistory
                .Skip(_conversationHistory.Count - MaxRecentMessages)
                .ToList();
        }

        /// <summary>
        /// 呼叫 Gemini API 進行文字生成（帶記憶功能）
        /// </summary>
        /// <param name="request">包含系統指令、使用者訊息和生成參數的請求物件</param>
        /// <param name="user">Discord 使用者資訊</param>
        /// <param name="saveToMemory">是否儲存到記憶中（預設為 true）</param>
        /// <returns>Gemini 回應的文字內容</returns>
        public async Task<string> GenerateTextAsync(GeminiRequestVM request, SocketGuildUser user, bool saveToMemory = true)
        {
            int maxRetry = 3;

            foreach (var model in _models)
            {
                for (int retry = 0; retry < maxRetry; retry++)
                {
                    try
                    {
                        // 建立包含歷史對話的 contents
                        var contentsList = new List<Content>();

                        // 🎭 第一步：加入人格設定 (用 user 角色發送，讓 AI 接受設定)
                        contentsList.Add(new Content
                        {
                            role = "user",
                            parts = new[] { new Part { text = Persona } }
                        });

                        // 🤖 第二步：AI 確認人格設定
                        contentsList.Add(new Content
                        {
                            role = "model",
                            parts = new[] { new Part { text = "好的，我是長崎爽世！有什麼想聊的嗎？" } }
                        });

                        // 📜 第三步：只加入最近的對話（節省 Token）
                        var recentMessages = GetRecentMessages();
                        foreach (var msg in recentMessages)
                        {
                            contentsList.Add(new Content
                            {
                                role = msg.Role,
                                parts = new[] { new Part { text = msg.Text } }
                            });
                        }

                        // 💬 第四步：加入當前使用者訊息（帶上使用者名稱）
                        var userMessageWithName = $"[{user.DisplayName}]: {request.UserMessage}";
                        contentsList.Add(new Content
                        {
                            role = "user",
                            parts = new[] { new Part { text = userMessageWithName } }
                        });

                        var apiRequest = new GeminiApiRequest
                        {
                            contents = contentsList.ToArray(),
                            generationConfig = new GenerationConfig
                            {
                                temperature = request.Temperature,
                                topP = request.TopP,
                                maxOutputTokens = request.MaxOutputTokens
                            },
                            safetySettings = new List<SafetySettings>
                            {
                                new SafetySettings
                                {
                                    category = "HARM_CATEGORY_HATE_SPEECH",
                                    threshold = "BLOCK_NONE"
                                },
                                new SafetySettings
                                {
                                    category = "HARM_CATEGORY_HARASSMENT",
                                    threshold = "BLOCK_NONE"
                                },
                                new SafetySettings
                                {
                                    category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                                    threshold = "BLOCK_NONE"
                                },
                                new SafetySettings
                                {
                                    category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                                    threshold = "BLOCK_NONE"
                                }
                            }
                        };

                        var options = new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };

                        var json = JsonSerializer.Serialize(apiRequest, options);

                        // 📊 統計 Token 使用（用於監控）
                        var estimatedTokens = EstimateTokenCount(contentsList);
                        Console.WriteLine($"[AI Memory] 發送訊息數: {contentsList.Count}, 預估 Token: ~{estimatedTokens}");

                        var response = await _httpClient.PostAsync(
                            $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={_apiKey}",
                            new StringContent(json, Encoding.UTF8, "application/json")
                        );

                        var resultJson = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[Gemini Error] Model:{model} Retry:{retry} => {resultJson}");

                            // 🔥 503 or 429 → retry
                            if ((int)response.StatusCode == 503 || (int)response.StatusCode == 429)
                            {
                                await Task.Delay(1000 * (retry + 1));
                                continue;
                            }

                            // 🔥 404 → 換 model
                            if ((int)response.StatusCode == 404)
                            {
                                break;
                            }

                            // 其他錯誤直接丟
                            return $"API錯誤: {response.StatusCode}";
                        }

                        var result = JsonSerializer.Deserialize<GeminiResponse>(resultJson, options);

                        var text = result?.candidates?
                            .FirstOrDefault()?
                            .content?.parts?
                            .FirstOrDefault()?
                            .text;

                        if (!string.IsNullOrEmpty(text))
                        {
                            // 💾 儲存對話到記憶
                            if (saveToMemory)
                            {
                                _conversationHistory.Add(new ConversationMessage
                                {
                                    Role = "user",
                                    Text = userMessageWithName,
                                    Timestamp = DateTime.Now,
                                    UserName = user.DisplayName
                                });

                                _conversationHistory.Add(new ConversationMessage
                                {
                                    Role = "model",
                                    Text = text,
                                    Timestamp = DateTime.Now,
                                    UserName = "爽世"
                                });

                                // 📊 限制記憶長度（檔案中保存更多，但只發送最近的）
                                if (_conversationHistory.Count > MaxTotalMessages)
                                {
                                    _conversationHistory = _conversationHistory
                                        .Skip(_conversationHistory.Count - MaxTotalMessages)
                                        .ToList();
                                }

                                await SaveMemoryAsync();
                            }

                            return text;
                        }

                        return "AI沒有回應內容";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Exception] Model:{model} Retry:{retry} => {ex.Message}");

                        await Task.Delay(1000 * (retry + 1));
                    }
                }
            }

            return "所有模型都失敗（可能免費額度或伺服器問題）";
        }

        /// <summary>
        /// 🎯 新增：粗略估算 Token 數量（用於監控）
        /// </summary>
        private int EstimateTokenCount(List<Content> contents)
        {
            int totalChars = 0;
            foreach (var content in contents)
            {
                foreach (var part in content.parts)
                {
                    totalChars += part.text?.Length ?? 0;
                }
            }
            // 粗略估算：中文約 1.5 字 = 1 token，英文約 4 字 = 1 token
            return (int)(totalChars * 0.7);
        }

        /// <summary>
        /// 簡化版本：直接傳入訊息進行生成
        /// </summary>
        /// <param name="message">使用者訊息</param>
        /// <param name="user">Discord 使用者資訊</param>
        /// <param name="saveToMemory">是否儲存到記憶中</param>
        /// <returns>Gemini 回應的文字內容</returns>
        public async Task<string> GenerateTextAsync(string message, SocketGuildUser user, bool saveToMemory = true)
        {
            var request = new GeminiRequestVM
            {
                UserMessage = message,
                Temperature = 0.7f,
                TopP = 0.95f,
                MaxOutputTokens = 2048
            };

            return await GenerateTextAsync(request, user, saveToMemory);
        }

        /// <summary>
        /// 取得當前對話記憶的摘要
        /// </summary>
        public string GetMemorySummary()
        {
            if (_conversationHistory.Count == 0)
                return "目前沒有對話記憶";

            var userMessages = _conversationHistory.Count(m => m.Role == "user");
            var modelMessages = _conversationHistory.Count(m => m.Role == "model");
            var firstMessage = _conversationHistory.First().Timestamp;
            var lastMessage = _conversationHistory.Last().Timestamp;
            var recentCount = Math.Min(_conversationHistory.Count, MaxRecentMessages);

            return $"對話記憶: {userMessages} 條使用者訊息, {modelMessages} 條 AI 回應\n" +
                   $"時間範圍: {firstMessage:yyyy-MM-dd HH:mm:ss} ~ {lastMessage:yyyy-MM-dd HH:mm:ss}\n" +
                   $"📊 檔案保存: {_conversationHistory.Count} 條 | API 發送: 最近 {recentCount} 條";
        }
    }

    // 對話記憶的資料結構
    public class ConversationMessage
    {
        public string Role { get; set; } // "user" or "model"
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } // 記錄是誰發言
    }

    // Gemini API 回應的資料結構
    public class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
    }
}
