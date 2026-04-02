using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using InstagramApiSharp.Classes;
using MusicBot2.RIOTService;

namespace MusicBot2.SlahCommands
{
    public class SlashCommandHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Program _program;

        public SlashCommandHandler(Program program)
        {
            _program = program;
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
    }
}