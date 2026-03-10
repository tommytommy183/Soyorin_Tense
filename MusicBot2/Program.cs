using Discord.Commands;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Exceptions;
using Discord;
using System.Collections;
using AngleSharp.Dom;
using YoutubeExplode.Search;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;

//ear rape之後會死掉?? 還是本來就會死掉? 死掉原因尚不明
//10/15 11:34  又好像不會了? 電腦剛重啟效能不足?
//待處理 looping & relate同時開啟時 
public class Program
{
    #region 變數
    private DiscordSocketClient? _client;
    private CommandService? _commands;
    private IAudioClient? _audioClient = null;
    private Queue<string> _songQueue = new Queue<string>();
    private bool _isPlaying = false;
    private String _NowPlayingSongUrl = "";
    private String _NowPlayingSongID = "";
    private String _NowPlayingSongName = "";
    private bool _isSkipRequest = false;
    private string _LoopingSongUrl = "";
    private List<string> _SongBeenPlayedList = new List<string>();
    private bool _isRelatedOn = false;
    private SocketGuildUser? _uuser;
    private bool _RelateSwitch = true;
    private string _LastPlayingName = "";
    private bool _isEarRapeOn = false;
    string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;";
    #endregion

    #region 基礎設定
    public static Task Main(string[] args) => new Program().RunBotAsync();
    public async Task RunBotAsync()
    {
        var config = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            EnableVoiceDaveEncryption = true,
            GatewayIntents = GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent |
                             GatewayIntents.Guilds |
                             GatewayIntents.GuildVoiceStates |
                             GatewayIntents.GuildMessageReactions |
                             GatewayIntents.GuildMembers
        };

        _client = new DiscordSocketClient(config);
        _commands = new CommandService();
        _client.MessageReceived += MessageReceivedHandler;
        _client.Log += Log;
        _ = SetBotStatusAsync(_client);
        await _client.LoginAsync(TokenType.Bot, "");
        await _client.StartAsync();
        await Task.Delay(-1);

    }

    private Task Log(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }

    private static async Task SetBotStatusAsync(DiscordSocketClient _client)
    {
        while (true)
        {
            await _client.SetGameAsync("小祥辛酸打工畫面流出", "https://www.youtube.com/watch?v=_1xcBdtwEE4&ab_channel=supanasu", ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("正在重組CRYCHIC", null, ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("CRYCHIC新成員演唱", "https://www.youtube.com/watch?v=f9p0HWDQHxs&ab_channel=nlnl", ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("有考慮當貝斯手嗎 我當然有考慮當貝斯手啊，那是我的夢想耶。我跟你說：當貝斯手比當工程師……我當……我當貝斯手，是……最想當的", null, ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("寫程式真的很莫名其妙", null, ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("那大家得多注意健康才行了", null, ActivityType.CustomStatus);
            await Task.Delay(20000);
            await _client.SetGameAsync("知ってたら止めたし😭セトリはもう終わってたのに急に演奏しだして😭みんなを止められなくてごめんね😭祥ちゃん、怒ってるよね😭怒るのも当然だと思う😭でも信じて欲しいの。春日影、本当に演奏する予定じゃなかったの😭本当にごめんね😭もう勝手に演奏したりしないって約束するよ😭ほかの子たちにも絶対にしないって約束させるから😭少しだけ話せないかな😭私、CRYCHICのこと本当に大切に思ってる😭だから、勝手に春日影演奏されたの祥ちゃんと同じくらい辛くて😭私の気持ちわかってほしいの😭お願い。どこても行くから😭バンドやらなきゃいけなかった理由もちゃんと話すから😭会って話せたら、きっとわかってもらえると思う😭私は祥ちゃんの味方だから😭会いたいの😭", null, ActivityType.CustomStatus);
            await Task.Delay(20000);
        }
    }
    #endregion

    #region MSreceive
    private async Task MessageReceivedHandler(SocketMessage message)
    {
        if (message is not SocketUserMessage userMessage || message.Author.IsBot || !message.Content.StartsWith("$$")) return;
        string cmd = message.Content.Substring(2);
        var channel = message.Channel as IMessageChannel;
        var user = message.Author as SocketGuildUser;
        _uuser = user;

        if (user == null)
            return;

        //撥放
        if (cmd.StartsWith("play"))
        {
            var query = cmd.Substring(4).Trim();
            await PlayMusicAsync(channel, user, query);
        }
        else if (cmd.ToLower().StartsWith("p"))
        {
            var query = cmd.Substring(1).Trim();
            await PlayMusicAsync(channel, user, query);
        }
        //bilibili
        else if (cmd.StartsWith("b"))
        {
            var url = cmd.Substring(1).Trim();
            await PlayBiblibiliMusicAsync(channel, user, url);
        }
        //跳過
        else if (cmd.ToLower().StartsWith("s") || cmd.StartsWith("skip"))
        {
            await SkipMusic(channel, user);
        }
        //循環和解除
        else if (cmd.ToLower().StartsWith("loop") || cmd.ToLower().StartsWith("lo"))
        {
            await LoopMusic(channel, user);
        }
        else if (cmd.ToLower().StartsWith("unloop") || cmd.ToLower().StartsWith("u"))
        {
            await UnLoopMusic(channel, user);
        }
        //推薦
        else if (cmd.ToLower().StartsWith("r"))
        {
            if (_RelateSwitch)
            {
                _RelateSwitch = false;
                await RelatedMusicAsync(channel, user);
            }
            else
            {
                _RelateSwitch = true;
                _isRelatedOn = false;
                _SongBeenPlayedList.Clear();
                await channel.SendMessageAsync("取消推薦");
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=1400&episode=13");
            }
        }
        //查詢
        else if (cmd.ToLower().StartsWith("find"))
        {
            var query = cmd.Substring(4).Trim();
            string url = await GetYoutubeUrlByNameAsync(channel, query);
            if (url == "")
            {
                Console.WriteLine("空");
                return;
            }
            else
            {
                await PlayMusicAsync(channel, user, url);
            }
        }
        else if (cmd.ToLower().StartsWith("f"))
        {
            var query = cmd.Substring(1).Trim();
            string url = await GetYoutubeUrlByNameAsync(channel, query);
            if (url == "")
            {
                Console.WriteLine("空");
                return;
            }
            else
            {
                await PlayMusicAsync(channel, user, url);
            }
        }
        //列出清單
        else if (cmd.ToLower().StartsWith("li"))
        {
            await CalledPlayListAsync(channel, user);
        }
        //爆
        else if (cmd.ToLower().StartsWith("e") || cmd.StartsWith("爆"))
        {
            await EarRapeAsync(channel, user);
        }
        else if(cmd.ToLower().StartsWith("qry"))
        {
            await qryData(channel, user);
        }
        else
        {
            await channel.SendMessageAsync("亂打一通");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=50472&episode=1-3");
        }
    }
    #endregion

    #region 撥放音樂事件
    private async Task PlayMusicAsync(IMessageChannel channel, SocketGuildUser user, string query)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("不進語音房是要撥個ㄐ8? 我去妳房間撥你衣服比較快 ");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=19672&episode=9");
            return;
        }

        if (!await CheckYoutubeUrlAliveAsync(query) && !_isRelatedOn)
        {
            await channel.SendMessageAsync("連結");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=1528&episode=13");
            return;
        }


        var voiceChannel = user.VoiceChannel;
        _songQueue.Enqueue(query);



        if (!_isPlaying)
        {
            _isPlaying = true;
            await CalledPlayListAsync(channel, user);
            _ = Task.Run(async () =>
            {
                await PlayNextSongAsync(channel, voiceChannel);
            });
        }
        else
        {
            if (!_isRelatedOn)
            {
                await CalledPlayListAsync(channel, user);
            }

        }
    }
    private async Task PlayBiblibiliMusicAsync(IMessageChannel channel, SocketGuildUser user, string url)
    {
        try
        {
            if (user?.VoiceChannel == null)
            {
                await channel.SendMessageAsync("不進語音房是要撥個ㄐ8? 我去妳房間撥你衣服比較快 ");
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=19672&episode=9");
                return;
            }

            var voiceChannel = user.VoiceChannel;
            _songQueue.Enqueue(url);

            if (!_isPlaying)
            {
                _isPlaying = true;
                await CalledPlayListForBBAsync(channel, user);
                await PlayNextSongAsync(channel, voiceChannel);
            }
            else
            {
                if (!_isRelatedOn)
                {
                    await CalledPlayListAsync(channel, user);
                }

            }
        }
        catch (Exception ex)
        {
            await channel.SendMessageAsync("下載失敗摟");
            await channel.SendMessageAsync(ex.ToString());
        }
    }
    private async Task SkipMusic(IMessageChannel channel, SocketGuildUser user)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("不進語音房是要跳ㄐㄐ");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=23200&episode=10");
            return;
        }
        if (_isPlaying)
        {
            await channel.SendMessageAsync($"你這個人滿腦子都只想到自己呢 ");
            await channel.SendMessageAsync($"https://anon-tokyo.com/image?frame=23864&episode=10");
            _isSkipRequest = true;
        }
        else
        {
            await channel.SendMessageAsync("沒歌了是要跳什麼");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=62208&episode=1-3");
        }
    }
    private async Task LoopMusic(IMessageChannel channel, SocketGuildUser user)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=15088&episode=9");
            return;
        }
        if (_isPlaying)
        {
            await channel.SendMessageAsync($"組一輩子Crychic");
            await channel.SendMessageAsync($"https://anon-tokyo.com/image?frame=8752&episode=13");
            _LoopingSongUrl = _NowPlayingSongUrl;
        }
        else
        {
            await channel.SendMessageAsync("沒歌了是要循環甚麼 戀愛嗎");
        }
    }
    private async Task UnLoopMusic(IMessageChannel channel, SocketGuildUser user)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("你不進語音是結束不掉的");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=28840&episode=4");
            return;
        }
        if (_isPlaying)
        {
            await channel.SendMessageAsync($"要持續一輩子是很困難的");
            await channel.SendMessageAsync($"https://anon-tokyo.com/image?frame=29160&episode=11");
            _LoopingSongUrl = "";
        }
        else
        {
            await channel.SendMessageAsync("沒歌了 已經維持不下去了..");
        }
    }
    private async Task CalledPlayListAsync(IMessageChannel channel, SocketGuildUser user)
    {

        if (_songQueue.Count == 0)
        {
            await channel.SendMessageAsync("沒歌你還想要清單?");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=86048&episode=1-3");
            return;
        }

        var random = RandomColor();

        // 创建一个新的 EmbedBuilder
        var embedBuilder = new EmbedBuilder()
        {
            Title = "目前歌單資訊",
            Color = random
        };

        if (_songQueue.Count != 0)
        {
            embedBuilder.AddField("目前歌單數量", $"{_songQueue.Count.ToString()}", true);
        }


        if (!string.IsNullOrEmpty(_NowPlayingSongUrl))
        {
            embedBuilder.AddField("目前正在撥放名稱", await GetVideoIDAsync(_NowPlayingSongUrl), true);
        }
        if (!string.IsNullOrEmpty(_NowPlayingSongUrl))
        {
            embedBuilder.AddField("歌曲網址", _NowPlayingSongUrl, true);
            embedBuilder.AddField($"目前待撥清單", "=======================================================================", true);
        }
        int count = 0;
        // 添加待播放的歌曲列表
        foreach (var song in _songQueue)
        {
            count++;
            string a = song;
            string b = await GetVideoIDAsync(a);
            embedBuilder.AddField($"第 {count} 首", b, false);

        }

        // 发送 Embed 消息
        await channel.SendMessageAsync(embed: embedBuilder.Build());

    }
    private async Task CalledPlayListForBBAsync(IMessageChannel channel, SocketGuildUser user)
    {

        if (_songQueue.Count == 0)
        {
            await channel.SendMessageAsync("沒歌你還想要清單?");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=86048&episode=1-3");
            return;
        }

        var random = RandomColor();

        // 创建一个新的 EmbedBuilder
        var embedBuilder = new EmbedBuilder()
        {
            Title = "目前歌單資訊",
            Color = random
        };

        if (_songQueue.Count != 0)
        {
            embedBuilder.AddField("目前歌單數量", $"{_songQueue.Count.ToString()}", true);
        }


        if (!string.IsNullOrEmpty(_NowPlayingSongUrl))
        {
            embedBuilder.AddField("目前正在撥放名稱", await GetVideoIDAsync(_NowPlayingSongUrl), true);
        }
        if (!string.IsNullOrEmpty(_NowPlayingSongUrl))
        {
            embedBuilder.AddField("歌曲網址", _NowPlayingSongUrl, true);
            embedBuilder.AddField($"目前待撥清單", "=======================================================================", true);
        }
        int count = 0;
        // 添加待播放的歌曲列表
        foreach (var song in _songQueue)
        {
            count++;
            string a = song;
            string b = await GetBilibiliTitleAsync(a);
            embedBuilder.AddField($"第 {count} 首", b, false);

        }

        // 发送 Embed 消息
        await channel.SendMessageAsync(embed: embedBuilder.Build());

    }
    private async Task RelatedMusicAsync(IMessageChannel channel, SocketGuildUser user)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=15088&episode=9");
            return;
        }
        string url;
        if (_isPlaying)
        {
            if (_SongBeenPlayedList.Count == 0)
            {
                _SongBeenPlayedList.Add(_NowPlayingSongID);
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=8368&episode=6");
            }
            url = await SearchRelateVideoAsync(channel, _NowPlayingSongName);
            if (string.IsNullOrEmpty(url))
            {
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=27448&episode=1-3");
                return;
            }
        }
        else
        {
            await channel.SendMessageAsync("沒點歌還想要推薦 那就聽春日影吧");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=184&episode=4");
            _NowPlayingSongID = "-kZBuzsZ7Ho";
            url = "https://www.youtube.com/watch?v=-kZBuzsZ7Ho&ab_channel=MyGO%21%21%21%21%21-Topic";
            _SongBeenPlayedList.Add(_NowPlayingSongID);
        }
        _isRelatedOn = true;

        await PlayMusicAsync(channel, user, url);
    }
    private async Task EarRapeAsync(IMessageChannel channel, SocketGuildUser user)
    {
        if (user?.VoiceChannel == null)
        {
            await channel.SendMessageAsync("要進語音诶 還是你想不進語音偷偷ear rape別人？ 想要的話跟我講 我改");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=16696&episode=1-3");
            return;
        }
        _isEarRapeOn = !_isEarRapeOn;
        if (_isEarRapeOn) await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=18288&episode=4");
        else await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=22448&episode=7");
    }
    private async Task qryData(IMessageChannel channel, SocketGuildUser user)
    {
        string query = "exec prGetTable1";
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                SqlCommand command = new SqlCommand(query, conn);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    // 假設 table1 有 id, name 欄位
                    await channel.SendMessageAsync($"欄1: {reader["Table_Column1"]}, 欄2: {reader["Table_Column2"]}");

                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("錯誤：" + ex.Message);
            }
        }
    }
    #endregion

    #region 撥放音樂
    //1可2可3不可   why??  =====> delay時間不夠長 貌似取決於電腦效能&網路
    private async Task PlayNextSongAsync(IMessageChannel channel, SocketVoiceChannel voiceChannel)
    {
        //songqueue為空 ／loop沒啟動／沒有開推薦
        if (_songQueue.Count == 0 && _LoopingSongUrl == "" && _isRelatedOn == false)
        {
            _isPlaying = false;
            await channel.SendMessageAsync("沒歌ㄌ");
            _NowPlayingSongUrl = "";
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=18976&episode=10");
            return;
        }
        //推薦開啟／且歌單只剩一首歌時
        if (_isRelatedOn && _songQueue.Count == 1)
        {
            await RelatedMusicAsync(channel, _uuser);
        }
        _isPlaying = true;
        string songUrl;
        //正常情況
        if (_LoopingSongUrl == "")
        {
            songUrl = _songQueue.Dequeue(); // 取出下一首歌
        }
        //開啟loop時
        else
        {
            songUrl = _LoopingSongUrl;
        }
        _NowPlayingSongUrl = songUrl;
        string filepath = "";
        if (_isRelatedOn)
        {
            await CalledPlayListAsync(channel, _uuser);
        }

        try
        {
            if(songUrl.Contains("bili"))
            {
                filepath = await DownloadBilibiliAudioAsync(songUrl);
            }
            else if(songUrl.Contains("youtube"))
            {
                filepath = await DownloadAudioAsync(songUrl);
            }
            await Task.Delay(2000);

            if (_audioClient == null)
            {
                _audioClient = await voiceChannel.ConnectAsync(selfDeaf: false, selfMute: false);
            }

            // _是異步 反正就是不在同個程式時間內run
            _ = Task.Run(async () =>
            {
                IAudioClient audioClient = _audioClient;

                var output = audioClient.CreatePCMStream(AudioApplication.Mixed);
                using (var audioFile = new AudioFileReader(filepath))
                {
                    var sampleRate = audioFile.WaveFormat.SampleRate;
                    var channels = audioFile.WaveFormat.Channels;
                    //新增爆
                    var modifiedSampleRate = _isEarRapeOn ? sampleRate / 10 : sampleRate;
                    using (var resampler = new MediaFoundationResampler(audioFile, new WaveFormat(sampleRate, channels)))
                    {
                        resampler.ResamplerQuality = _isEarRapeOn ? 1 : 60; // 設置重取樣品質
                        byte[] buffer = new byte[4096];
                        int bytesRead;

                        // 播放音樂
                        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            audioFile.Volume = _isEarRapeOn ? 10.0f : 1.0f;
                            if (_isSkipRequest)
                            {
                                await output.FlushAsync();
                                _isSkipRequest = false;
                                break;
                            }
                            await output.WriteAsync(buffer, 0, bytesRead);
                        }
                        await output.FlushAsync(); // 確保所有數據已發送
                    }
                }
                // 播放完成後的清理
                File.Delete(filepath);
                // 停止音頻播放
                //await audioClient.StopAsync();
                output.Dispose();
                // 播放下一首歌
                await PlayNextSongAsync(channel, voiceChannel);

            });
        }
        catch (Exception ex)
        {
            await channel.SendMessageAsync($"我從來不覺得寫程式開心過:PlayNextSongAsync {ex.Message} {ex}");
            await channel.SendMessageAsync($"https://anon-tokyo.com/image?frame=20704&episode=6");
        }
    }
    #endregion

    #region yt相關
    private async Task<string> GetVideoIDAsync(string url)
    {
        var youtube = new YoutubeClient();
        var videoId = YoutubeExplode.Videos.VideoId.TryParse(url);
        var video = await youtube.Videos.GetAsync(videoId.Value);
        var videoTitle = video.Title;
        if (videoId == null)
        {
            return "";
        }
        else
        {
            return videoTitle;
        }
    }
    private async Task<string> DownloadAudioAsync(string url)
    {

        var youtube = new YoutubeClient();
        var videoId = YoutubeExplode.Videos.VideoId.TryParse(url);
        if (!videoId.HasValue)
        {
            throw new Exception("連結無效");
        }
        _NowPlayingSongID = videoId.Value;
        var video = await youtube.Videos.GetAsync(videoId.Value);
        _NowPlayingSongName = video.Title.ToString();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        var tempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        Directory.CreateDirectory(tempDirectory);

        var filePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.mp3");
        await youtube.Videos.Streams.DownloadAsync(streamInfo, filePath);

        return filePath;
    }
    private async Task<string> GetYoutubeUrlByNameAsync(IMessageChannel channel, string query)
    {
        try
        {
            // 使用 YoutubeExplode 搜索视频
            var youtube = new YoutubeClient();
            var searchResults = await youtube.Search.GetResultsAsync(query);

            if (!searchResults.Any())
            {
                await channel.SendMessageAsync("找不到歌曲");
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=5280&episode=13");
                return "";
            }
            // 获取第一个搜索结果
            var video = searchResults.First();
            var videoUrl = video.Url;

            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=89608&episode=1-3");
            await channel.SendMessageAsync($"{video.Url}");
            return $"{videoUrl}";
        }
        catch (Exception ex)
        {
            await channel.SendMessageAsync($"我從來不覺得寫程式開心過:GetYoutubeUrlByName {ex.Message}");
            await channel.SendMessageAsync($" https://anon-tokyo.com/image?frame=20704&episode=6");
            return "";
        }
    }
    private async Task<string> SearchRelateVideoAsync(IMessageChannel channel, string name)
    {
        string url = "";
        try
        {
            var modifiedTitle = GetRandomizedTitle(name, channel);
            var youtube = new YoutubeClient();
            var searchResults = await youtube.Search.GetResultsAsync(modifiedTitle);
            var top10Results = searchResults.Take(10);
            //打亂
            var random = new Random();
            var shuffledResults = top10Results.OrderBy(x => random.Next()).ToList();

            // 输出结果
            foreach (var result in shuffledResults)
            {
                // 检查结果类型是否为视频
                if (result is VideoSearchResult videoResult)
                {
                    if (videoResult.Duration < TimeSpan.FromMinutes(10))
                    {
                        if (!_SongBeenPlayedList.Contains(videoResult.Id))
                        {
                            url = videoResult.Url;
                            _SongBeenPlayedList.Add(videoResult.Id);
                            break;
                        }
                    }

                }
            }//真查不到就變20筆 再查不到就return空值回去判斷
            if (string.IsNullOrEmpty(url))
            {
                var top20Results = searchResults.Take(20);
                foreach (var result in top20Results)
                {
                    if (result is VideoSearchResult videoResult)
                    {
                        if (videoResult.Duration < TimeSpan.FromMinutes(10))
                        {
                            if (!_SongBeenPlayedList.Contains(videoResult.Id))
                            {
                                url = videoResult.Url;
                                _SongBeenPlayedList.Add(videoResult.Id);
                                break;
                            }
                        }
                    }
                }
            }
            return url;
        }
        catch (Exception ex)
        {
            await channel.SendMessageAsync($"我從來不覺得寫程式開心過:SearchRelateVideoAsync {ex.Message}");
            await channel.SendMessageAsync($" https://anon-tokyo.com/image?frame=20704&episode=6");
            return "";
        }
    }
    #endregion

    #region
    private async Task<string> DownloadBilibiliAudioAsync(string url)
    {
        var tempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
        Directory.CreateDirectory(tempDirectory);

        // 用 Guid 當做「檔名前綴」，但不指定副檔名
        var filePrefix = Guid.NewGuid().ToString();
        var outputTemplate = Path.Combine(tempDirectory, $"{filePrefix}.%(ext)s");

        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = $"-f ba -x --audio-format mp3 -o \"{outputTemplate}\" {url}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            // 找出實際的 MP3 檔案
            var downloadedFile = Directory
                .EnumerateFiles(tempDirectory, $"{filePrefix}.*")
                .FirstOrDefault(f => Path.GetExtension(f).Equals(".mp3", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(downloadedFile))
                return downloadedFile;
        }

        throw new Exception("Bilibili 下載失敗搂 OB一串字母女士非常不開心！");

    }

    private async Task<string> GetBilibiliTitleAsync(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = $"--get-title {url}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            return output.Trim();
        }
        else
        {
            return "[取得 Bilibili 標題失敗]";
        }
    }



    #endregion
    #region 自訂func
    private string GetRandomizedTitle(string title, IMessageChannel channel)
    {
        var _ignoreKeywords = new List<string>
    {
        "official", "video", "mv", "lyrics", "audio", "remastered", "hd", "live", "version", "ft.", "feat", "featuring","歌詞","拼音","ver" ,"music","movie","tv","高画質","amv","mad","1k","2k","3k","4k"
        ,"弾き語り","fps" ,"hdr" ,"ultra","實況","精華","アニメ"
    };
        StringBuilder sb = new StringBuilder();
        string ai = "";
        string pattern = string.Join("|", _ignoreKeywords.Select(Regex.Escape));
        string cleanTitle = Regex.Replace(title.ToLower(), $@"({pattern})", ",", RegexOptions.IgnoreCase).Trim();
        sb.AppendLine($"移除贅字後的title：{cleanTitle}");
        sb.AppendLine("=========================");
        var parts = Regex.Split(cleanTitle, @"[-|/【】『』「」，:：《》〈〉＜＞<>‧．·，、。＊＆＃※§′‵〞〝”“’!！()（）｛｝｜  \-.,#〔＋〕@的 ‘'[\]]").Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        foreach (var p in parts)
        {
            if (p.StartsWith("ai"))
            {
                string s = RandomAI();
                ai = $"【Ai {s}唱】";
                channel.SendMessageAsync($"查詢條件:{ai}");
                return ai;
            }
            sb.Append($" {p}     ");
        }
        sb.Remove((sb.Length - 1), 1);
        sb.Append('\n');
        sb.AppendLine("=========================");
        if (parts.Count == 0)
        {
            return cleanTitle;
        }

        var random = new Random();
        int index = random.Next(parts.Count);
        sb.AppendLine($"最後選中的：{parts[index]}");
        channel.SendMessageAsync(sb.ToString());
        return (parts[index]);
    }
    private async Task<bool> CheckYoutubeUrlAliveAsync(string url)
    {
        try
        {
            var videoId = YoutubeExplode.Videos.VideoId.TryParse(url);
            if (videoId == null)
            {
                return false;
            }

            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videoId.Value);
            _NowPlayingSongName = video.Title;

            return video != null; // 如果成功获取到视频信息，则视为有效
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查视频有效性时发生错误: {ex.Message}");
            return false; // 发生异常，视为无效
        }
    }
    private Color RandomColor()
    {
        var colors = new List<Color>
{
    Color.Blue,
    Color.Purple,
    Color.DarkBlue,
    Color.DarkerGrey,
    Color.DarkPurple,
    Color.DarkMagenta
};

        // 创建一个随机数生成器
        var random = new Random();

        // 随机选择一个颜色
        var randomColor = colors[random.Next(colors.Count)];
        return randomColor;
    }
    private string RandomAI()
    {
        var random = new Random();
        List<string> singer = new List<string>();
        singer.Add("NL");
        singer.Add("Roger");
        singer.Add("羅傑");
        singer.Add("統神");
        singer.Add("toyz");
        singer.Add("RB");

        string s = "";
        s = singer[random.Next(singer.Count)];

        return s;
    }

    #endregion
}
