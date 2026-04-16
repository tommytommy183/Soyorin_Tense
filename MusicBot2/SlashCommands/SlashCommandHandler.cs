using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using InstagramApiSharp.Classes;
using MusicBot2.Models;
using MusicBot2.Service;

namespace MusicBot2.SlahCommands
{
    public class SlashCommandHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Program _program;
        WordGuessingService wordService;
        MineGameService _mineGameService;
        ElevenLabsService _elevenLabsService;
        private readonly RubiksCubeService _rubiksCubeService;

        public SlashCommandHandler(Program program, WordGuessingService wordService, MineGameService mineGameService, ElevenLabsService elevenLabsService, RubiksCubeService rubiksCubeService)
        {
            _program = program;
            this.wordService = wordService;
            _elevenLabsService = elevenLabsService;
            _mineGameService = mineGameService;
            _rubiksCubeService = rubiksCubeService;
        }

        [SlashCommand("play", "播放音樂")]
        public async Task PlayCommand([Summary("查詢", "YouTube URL 或搜尋關鍵字")] string query)
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.PlayMusicAsync(Context.Channel, user, query);
        }

        [SlashCommand("p", "播放音樂 (簡短版)")]
        public async Task PCommand([Summary("查詢", "YouTube URL 或搜尋關鍵字")] string query)
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.PlayMusicAsync(Context.Channel, user, query);
        }

        [SlashCommand("bilibili", "播放 Bilibili 音樂")]
        public async Task BilibiliCommand([Summary("網址", "Bilibili 影片網址")] string url)
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.PlayBiblibiliMusicAsync(Context.Channel, user, url);
        }

        [SlashCommand("skip", "跳過目前歌曲")]
        public async Task SkipCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.SkipMusic(Context.Channel, user);
        }

        [SlashCommand("s", "跳過目前歌曲 (簡短版)")]
        public async Task SCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.SkipMusic(Context.Channel, user);
        }

        [SlashCommand("loop", "循環播放目前歌曲")]
        public async Task LoopCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.LoopMusic(Context.Channel, user);
        }

        [SlashCommand("unloop", "取消循環播放")]
        public async Task UnloopCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.UnLoopMusic(Context.Channel, user);
        }

        [SlashCommand("related", "開啟/關閉推薦音樂")]
        public async Task RelatedCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.HandleRelatedMusicAsync(Context.Channel, user);
        }

        [SlashCommand("find", "搜尋並播放音樂")]
        public async Task FindCommand([Summary("關鍵字", "搜尋關鍵字")] string query)
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            string url = await _program.GetYoutubeUrlByNameAsync(Context.Channel, query);
            if (!string.IsNullOrEmpty(url))
            {
                await _program.PlayMusicAsync(Context.Channel, user, url);
            }
        }

        [SlashCommand("f", "搜尋並播放音樂 (簡短版)")]
        public async Task FCommand([Summary("關鍵字", "搜尋關鍵字")] string query)
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            string url = await _program.GetYoutubeUrlByNameAsync(Context.Channel, query);
            if (!string.IsNullOrEmpty(url))
            {
                await _program.PlayMusicAsync(Context.Channel, user, url);
            }
        }

        [SlashCommand("list", "顯示目前播放清單")]
        public async Task ListCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.CalledPlayListAsync(Context.Channel, user);
        }

        [SlashCommand("earrape", "開啟/關閉 Ear Rape 模式")]
        public async Task EarRapeCommand()
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            await _program.EarRapeAsync(Context.Channel, user);
        }

        [SlashCommand("skill", "查詢英雄技能")]
        public async Task SkillCommand([Summary("英雄名", "英雄名稱")] string champName)
        {
            await DeferAsync();
            var champService = new GetChampService(Context.Channel as IMessageChannel);
            await champService.GetChampSkillsAsync(champName);
            await FollowupAsync(" ", ephemeral: true);
        }

        [SlashCommand("guess", "猜測英雄技能")]
        public async Task GuessCommand(
            [Summary("英雄名", "英雄名稱")] string champName,
            [Summary("技能位置", "P, Q, W, E, 或 R")][Choice("P", "p"), Choice("Q", "q"), Choice("W", "w"), Choice("E", "e"), Choice("R", "r")] string skillPos,
            [Summary("猜測名稱", "你猜測的技能名稱")] string userGuess)
        {
            await DeferAsync();
            var champService = new GetChampService(Context.Channel as IMessageChannel);
            await champService.GuessChampSkillAsync(champName.ToLower(), skillPos.ToLower(), userGuess.ToLower());
            await FollowupAsync(" ", ephemeral: true);
        }

        [SlashCommand("words", "猜單字")]
        public async Task Guess(string word)
        {
            try
            {
                var user = Context.User as SocketGuildUser;
                string res = await wordService.Guess(word, user);
                if (!string.IsNullOrEmpty(res))
                {
                    await RespondAsync(res);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        [SlashCommand("mine", "開始踩地雷遊戲")]
        public async Task MineCommand()
        {
            await DeferAsync();

            var (component, embed) = await _mineGameService.StartGameAsync(Context.User.Id, 5, 5);

            await FollowupAsync(embed: embed, components: component.Build());
        }

        [SlashCommand("minebig", "超大踩地雷遊戲")]
        public async Task CustomizedMineCommand(
            [Summary("寬度", "地圖寬度")] int width,
            [Summary("高度", "地圖高度")] int height)
        {
            await DeferAsync();

            var (component, embed) = await _mineGameService.StartBiggerGameAsync(Context.User.Id, width, height);

            await FollowupAsync(embed: embed, components: component.Build());
        }

        [SlashCommand("mineopen", "超大踩地雷遊戲")]
        public async Task OpenBox(
            [Summary("x座標", "x座標")] int x,
            [Summary("y座標", "y座標")] int y)
        {
            await DeferAsync();

            var embed = await _mineGameService.HandleTextCoordinate(Context.User.Id, x, y);
            await FollowupAsync(embed: embed);
        }

        [SlashCommand("speak", "透過ElevenLabs說話")]
        public async Task ElevenLabsTalk(
            [Summary("text", "要讓他說的話")] string text,
            [Summary("model", "選擇需要使用的模型")][Choice("品質最好", "eleven_v3"), Choice("最穩定", "eleven_multilingual_v2"), Choice("最低延遲", "eleven_flash_v2_5"), Choice("平衡", "eleven_turbo_v2_5")] string model, 
            [Summary("voiceID", "請輸入要使用的voiceID，不填入則預設")] string voiceID = "pNInz6obpgDQGcFmaJgB")
        {
            await DeferAsync();
            var user = Context.User as SocketGuildUser;
            var voiceChannel = user.VoiceChannel;
            await _elevenLabsService.SpeakAsync(voiceChannel, text, model, voiceID);
            await FollowupAsync(" ", ephemeral: true);
        }

        [SlashCommand("rubikscube", "開始魔術方塊遊戲")]
        public async Task RubiksCubeCommand(
            [Summary("難度", "打亂步數 (預設20步)")] int scrambleMoves = 20)
        {
            await DeferAsync();

            if (scrambleMoves < 5 || scrambleMoves > 100)
            {
                await FollowupAsync("❌ 難度必須在 5-100 步之間！", ephemeral: true);
                return;
            }

            var (component, embed) = _rubiksCubeService.StartGame(Context.Channel.Id, scrambleMoves);
            await FollowupAsync(embed: embed, components: component.Build());
        }

        [SlashCommand("cube", "開始魔術方塊遊戲 (簡短版)")]
        public async Task CubeCommand()
        {
            await DeferAsync();
            var (component, embed) = _rubiksCubeService.StartGame(Context.Channel.Id, 20);
            await FollowupAsync(embed: embed, components: component.Build());
        }
    }
}