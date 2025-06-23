using Telegram.Bot;
using DotNetEnv;
using TelegramInsuranceBot.Services;
using TelegramInsuranceBot.Configuration;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var botConfig = BotConfiguration.LoadFromEnv();
builder.Services.AddSingleton(botConfig);

builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient =>
        new TelegramBotClient(botConfig.BotToken, httpClient));

builder.Services.AddSingleton<MindeeService>();

builder.Services.AddSingleton<UpdateHandler>();

builder.Services.AddSingleton<OpenAiChatService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TelegramInsuranceBot API");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
