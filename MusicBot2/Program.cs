using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicBot2.Service;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

public class Program
{
    #region 變數
    private DiscordClient? _client;
    private VoiceNextExtension? _voiceNext;
    private VoiceNextConnection? _voiceConnection = null;
    private Queue<string> _songQueue = new Queue<string>();
    private bool _isPlaying = false;
    private String _NowPlayingSongUrl = "";
    private String _NowPlayingSongID = "";
    private String _NowPlayingSongName = "";
    private bool _isSkipRequest = false;
    private string _LoopingSongUrl = "";
    private List<string> _SongBeenPlayedList = new List<string>();
    GetChampService champService;
    private bool _isRelatedOn = false;
    private DiscordMember? _umember;
    private bool _RelateSwitch = true;
    private bool _isEarRapeOn = false;
    private IServiceProvider? _services;
    private DiscordGuild? _currentGuild;
    #endregion

    #region 基礎設定
    public static Task Main(string[] args) => new Program().RunBotAsync();
    public async Task RunBotAsync()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configer = builder.Build();
        string token = configer["Discord:Token"];
        string elevenLabsApiKey = configer["ElevenLabs:ApiKey"];

        var config = new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMembers,
            AutoReconnect = true,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
        };

        _client = new DiscordClient(config);
        _voiceNext = _client.UseVoiceNext();

        // 設置依賴注入 (暫時移除遊戲功能和 Slash Commands，只保留音樂)
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(this)
            .AddSingleton<GetChampService>()
            .BuildServiceProvider();

        _client.MessageCreated += MessageReceivedHandler;
        _client.Ready += ClientReady;

        _ = SetBotStatusAsync(_client);

        await _client.ConnectAsync();
        await Task.Delay(-1);
    }


    #endregion

    #region 額外的handler
    private async Task ClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        await Task.CompletedTask;
    }

    public static async Task SetBotStatusAsync(DiscordClient _client)
    {
        while (true)
        {
            await _client.UpdateStatusAsync(new DiscordActivity("搜幽林轉生☆大★爆☆誕★", ActivityType.Custom));
            await Task.Delay(20000);
            await _client.UpdateStatusAsync(new DiscordActivity("傻逼DISCORD加密 不如我苦來溪苦一根", ActivityType.Custom));
            await Task.Delay(20000);
            await _client.UpdateStatusAsync(new DiscordActivity("小祥辛酸打工畫面流出", ActivityType.Watching));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("正在重組CRYCHIC", ActivityType.Custom));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("CRYCHIC新成員演唱", ActivityType.Custom));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("有考慮當貝斯手嗎 我當然有考慮當貝斯手啊，那是我的夢想耶。我跟你說：當貝斯手比當工程師……我當……我當貝斯手，是……最想當的", ActivityType.Custom));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("寫程式真的很莫名其妙", ActivityType.Custom));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("那大家得多注意健康才行了", ActivityType.Custom));
            await Task.Delay(10000);
            await _client.UpdateStatusAsync(new DiscordActivity("知ってたら止めたし😭セトリはもう終わってたのに急に演奏しだして😭みんなを止められなくてごめんね😭祥ちゃん、怒ってるよね😭怒るのも当然だと思う😭でも信じて欲しいの。春日影、本当に演奏する予定じゃなかったの😭本当にごめんね😭もう勝手に演奏したりしないって約束するよ😭ほかの子たちにも絶対にしないって約束させるから😭少しだけ話せないかな😭私、CRYCHICのこと本当に大切に思ってる😭だから、勝手に春日影演奏されたの祥ちゃんと同じくらい辛くて😭私の気持ちわかってほしいの😭お願い。どこても行くから😭バンドやらなきゃいけなかった理由もちゃんと話すから😭会って話せたら、きっとわかってもらえると思う😭私は祥ちゃんの味方だから😭会いたいの😭", ActivityType.Custom));
            await Task.Delay(10000);
        }
    }
    #endregion

    #region MSreceive
    public async Task MessageReceivedHandler(DiscordClient sender, MessageCreateEventArgs e)
    {
        if (e.Author.IsBot || !e.Message.Content.StartsWith("$$")) return;
        
        string cmd = e.Message.Content.Substring(2);
        var channel = e.Channel;
        
        if (e.Guild == null) return;
        
        var member = await e.Guild.GetMemberAsync(e.Author.Id);
        _umember = member;
        _currentGuild = e.Guild;
        champService = new GetChampService();

        //撥放
        if (cmd.StartsWith("play"))
        {
            var query = cmd.Substring(4).Trim();
            await PlayMusicAsync(channel, member, query);
        }
        else if (cmd.ToLower().StartsWith("p"))
        {
            var query = cmd.Substring(1).Trim();
            await PlayMusicAsync(channel, member, query);
        }
        //bilibili
        else if (cmd.StartsWith("b"))
        {
            var url = cmd.Substring(1).Trim();
            await PlayBiblibiliMusicAsync(channel, member, url);
        }
        //跳過
        else if (cmd.ToLower().StartsWith("s") || cmd.StartsWith("skip"))
        {
            await SkipMusic(channel, member);
        }
        //循環和解除
        else if (cmd.ToLower().StartsWith("loop") || cmd.ToLower().StartsWith("lo"))
        {
            await LoopMusic(channel, member);
        }
        else if (cmd.ToLower().StartsWith("unloop") || cmd.ToLower().StartsWith("u"))
        {
            await UnLoopMusic(channel, member);
        }
        //推薦
        else if (cmd.ToLower().StartsWith("r"))
        {
            if (_RelateSwitch)
            {
                _RelateSwitch = false;
                await RelatedMusicAsync(channel, member);
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
                await PlayMusicAsync(channel, member, url);
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
                await PlayMusicAsync(channel, member, url);
            }
        }
        //列出清單
        else if (cmd.ToLower().StartsWith("li"))
        {
            await CalledPlayListAsync(channel, member);
        }
        //爆
        else if (cmd.ToLower().StartsWith("e") || cmd.StartsWith("爆"))
        {
            await EarRapeAsync(channel, member);
        }
        else
        {
            await channel.SendMessageAsync("亂打一通");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=50472&episode=1-3");
        }
    }
    #endregion

    #region 撥放音樂事件
    public async Task PlayMusicAsync(DiscordChannel channel, DiscordMember user, string query)
    {
        if (user?.VoiceState?.Channel == null)
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

        var voiceChannel = user.VoiceState.Channel;
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
    
    public async Task PlayBiblibiliMusicAsync(DiscordChannel channel, DiscordMember user, string url)
    {
        try
        {
            if (user?.VoiceState?.Channel == null)
            {
                await channel.SendMessageAsync("不進語音房是要撥個ㄐ8? 我去妳房間撥你衣服比較快 ");
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=19672&episode=9");
                return;
            }

            var voiceChannel = user.VoiceState.Channel;
            _songQueue.Enqueue(url);

            if (!_isPlaying)
            {
                _isPlaying = true;
                await CalledPlayListForBBAsync(channel, user);
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
        catch (Exception ex)
        {
            await channel.SendMessageAsync("下載失敗摟");
            await channel.SendMessageAsync(ex.ToString());
        }
    }
    
    public async Task SkipMusic(DiscordChannel channel, DiscordMember user)
    {
        if (user?.VoiceState?.Channel == null)
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
    
    public async Task LoopMusic(DiscordChannel channel, DiscordMember user)
    {
        if (user?.VoiceState?.Channel == null)
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
    
    public async Task UnLoopMusic(DiscordChannel channel, DiscordMember user)
    {
        if (user?.VoiceState?.Channel == null)
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
    public async Task CalledPlayListAsync(DiscordChannel channel, DiscordMember user)
    {
        if (_songQueue.Count == 0)
        {
            await channel.SendMessageAsync("沒歌你還想要清單?");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=86048&episode=1-3");
            return;
        }

        var random = RandomColor();

        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle("目前歌單資訊")
            .WithColor(random);

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
        foreach (var song in _songQueue)
        {
            count++;
            string a = song;
            string b = await GetVideoIDAsync(a);
            embedBuilder.AddField($"第 {count} 首", b, false);
        }

        await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embedBuilder.Build()));
    }
    
    public async Task CalledPlayListForBBAsync(DiscordChannel channel, DiscordMember user)
    {
        if (_songQueue.Count == 0)
        {
            await channel.SendMessageAsync("沒歌你還想要清單?");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=86048&episode=1-3");
            return;
        }

        var random = RandomColor();

        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle("目前歌單資訊")
            .WithColor(random);

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
        foreach (var song in _songQueue)
        {
            count++;
            string a = song;
            string b = await GetBilibiliTitleAsync(a);
            embedBuilder.AddField($"第 {count} 首", b, false);
        }

        await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embedBuilder.Build()));
    }
    
    public async Task RelatedMusicAsync(DiscordChannel channel, DiscordMember user)
    {
        if (user?.VoiceState?.Channel == null)
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

    public async Task HandleRelatedMusicAsync(DiscordChannel channel, DiscordMember user)
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
    
    public async Task EarRapeAsync(DiscordChannel channel, DiscordMember user)
    {
        if (user?.VoiceState?.Channel == null)
        {
            await channel.SendMessageAsync("要進語音诶 還是你想不進語音偷偷ear rape別人？ 想要的話跟我講 我改");
            await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=16696&episode=1-3");
            return;
        }
        _isEarRapeOn = !_isEarRapeOn;
        if (_isEarRapeOn) await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=18288&episode=4");
        else await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=22448&episode=7");
    }
    #endregion

    #region 撥放音樂
    //1可2可3不可   why??  =====> delay時間不夠長 貌似取決於電腦效能&網路
    public async Task PlayNextSongAsync(DiscordChannel channel, DiscordChannel voiceChannel)
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
            await RelatedMusicAsync(channel, _umember);
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
            await CalledPlayListAsync(channel, _umember);
        }

        try
        {
            if (songUrl.Contains("bili"))
                filepath = await DownloadBilibiliAudioAsync(songUrl);
            else
                filepath = await DownloadAudioAsync(songUrl);

            await Task.Delay(2000);

            if (_voiceConnection == null || !_voiceConnection.IsPlaying)
                _voiceConnection = await _voiceNext.ConnectAsync(voiceChannel);

            var transmitSink = _voiceConnection.GetTransmitSink();
            
            using (var audioFile = new AudioFileReader(filepath))
            {
                var sampleRate = audioFile.WaveFormat.SampleRate;
                var channels = audioFile.WaveFormat.Channels;
                
                using (var resampler = new MediaFoundationResampler(audioFile, new WaveFormat(48000, 2)))
                {
                    resampler.ResamplerQuality = _isEarRapeOn ? 1 : 60;
                    byte[] buffer = new byte[3840]; // 20ms of 48kHz 16-bit stereo PCM
                    int bytesRead;

                    // 播放音樂
                    while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        audioFile.Volume = _isEarRapeOn ? 10.0f : 1.0f;
                        if (_isSkipRequest)
                        {
                            _isSkipRequest = false;
                            break;
                        }
                        await transmitSink.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }

            File.Delete(filepath);
            await PlayNextSongAsync(channel, voiceChannel);
        }
        catch (Exception ex)
        {
            await channel.SendMessageAsync($"我從來不覺得寫程式開心過:PlayNextSongAsync {ex.Message} {ex}");
            await channel.SendMessageAsync($"https://anon-tokyo.com/image?frame=20704&episode=6");
        }
    }
    #endregion

    #region yt相關
    public async Task<string> GetVideoIDAsync(string url)
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
    public async Task<string> DownloadAudioAsync(string url)
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
    public async Task<string> GetYoutubeUrlByNameAsync(DiscordChannel channel, string query)
    {
        try
        {
            var youtube = new YoutubeClient();
            var videos = new List<VideoSearchResult>();
            
            await foreach (var result in youtube.Search.GetVideosAsync(query))
            {
                videos.Add(result);
                if (videos.Count >= 1) break;
            }

            if (!videos.Any())
            {
                await channel.SendMessageAsync("找不到歌曲");
                await channel.SendMessageAsync("https://anon-tokyo.com/image?frame=5280&episode=13");
                return "";
            }
            
            var video = videos.First();
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
    public async Task<string> SearchRelateVideoAsync(DiscordChannel channel, string name)
    {
        string url = "";
        try
        {
            var modifiedTitle = GetRandomizedTitle(name, channel);
            var youtube = new YoutubeClient();
            var videos = new List<VideoSearchResult>();
            
            await foreach (var result in youtube.Search.GetVideosAsync(modifiedTitle))
            {
                videos.Add(result);
                if (videos.Count >= 20) break;
            }
            
            var top10Results = videos.Take(10);
            var random = new Random();
            var shuffledResults = top10Results.OrderBy(x => random.Next()).ToList();

            foreach (var videoResult in shuffledResults)
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
            
            if (string.IsNullOrEmpty(url))
            {
                var top20Results = videos.Take(20);
                foreach (var videoResult in top20Results)
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
    public async Task<string> DownloadBilibiliAudioAsync(string url)
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

    public async Task<string> GetBilibiliTitleAsync(string url)
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

    public Process CreatePcmStreamProcess(string path)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string ffmpegPath = Path.Combine(projectRoot, "ffmpeg-master-latest-win64-gpl-shared", "bin", "ffmpeg.exe");

        return Process.Start(new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
    }
    public string GetRandomizedTitle(string title, DiscordChannel channel)
    {
        var _ignoreKeywords = new List<string>
    {
        "official", "video", "mv", "lyrics", "audio", "remastered", "hd", "live", "version", "ft.", "feat", "featuring","歌詞","拼音","ver" ,"music","movie","tv","高画質","amv","mad","1k","2k","3k","4k"
        ,"弾き語り","fps" ,"hdr" ,"ultra","實況","精華","アニメ","official youTube channel"
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
    public async Task<bool> CheckYoutubeUrlAliveAsync(string url)
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
    public DiscordColor RandomColor()
    {
        var colors = new List<DiscordColor>
        {
            new DiscordColor(0, 0, 255),      // Blue
            new DiscordColor(128, 0, 128),    // Purple
            new DiscordColor(0, 0, 139),      // DarkBlue
            new DiscordColor(47, 49, 54),     // DarkerGrey
            new DiscordColor(114, 137, 218),  // DarkPurple (Blurple)
            new DiscordColor(139, 0, 139)     // DarkMagenta
        };

        var random = new Random();
        var randomColor = colors[random.Next(colors.Count)];
        return randomColor;
    }
    public string RandomAI()
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
