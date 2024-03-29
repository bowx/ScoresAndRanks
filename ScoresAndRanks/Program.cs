using ScoresAndRanks.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IScoresAndRanksService, SkipListService>();
//builder.Services.AddSingleton<IScoresAndRanksService, SortedListService>();

//Exception Handler
builder.Services.AddExceptionHandler<ScoresAndRanks.ExceptionHandler.ScoresAndRanksExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
