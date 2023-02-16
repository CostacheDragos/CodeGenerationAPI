using CodeGenerationAPI.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ICodeGeneratorService, CodeGenerationService>();
builder.Services.AddSingleton<IFirestoreService>(sp =>
{
    string projectId = builder.Configuration.GetSection("FirebaseProjectId").Value;
    return new FirestoreService(projectId);
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow our frontend app to communicate
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "CorsPolicy",
                              policy =>
                              {
                                  policy.AllowAnyOrigin()
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                              });
});

if (builder.Environment.IsProduction())
{
    builder.WebHost.UseKestrel(options =>
    {
        int port = 16261;
        options.ListenAnyIP(port);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");


//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
