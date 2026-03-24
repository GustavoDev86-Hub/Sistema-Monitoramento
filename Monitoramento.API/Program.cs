using Microsoft.EntityFrameworkCore;
using Monitoramento.Infrastructure.Data;
using Monitoramento.API.Services;
using Monitoramento.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurações de Serviços
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHostedService<MonitoramentoWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. BLOCO DE INSERÇÃO AUTOMÁTICA (Onde a mágica acontece)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); // Cria as tabelas se não existirem

        if (!context.Ativos.Any())
        {
            context.Ativos.Add(new Monitoramento.Domain.Entities.Ativo 
            { 
                Url = "https://www.google.com", 
                EstaOnline = true, 
                UltimaVerificacao = DateTime.UtcNow 
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao popular banco: {ex.Message}");
    }
}

// 3. Pipeline do App
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Sistema de Monitoramento Online rodando no Debian!");

app.Run();