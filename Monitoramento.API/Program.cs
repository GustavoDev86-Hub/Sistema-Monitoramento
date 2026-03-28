using Microsoft.EntityFrameworkCore;
using Monitoramento.Infrastructure.Data;
using Monitoramento.API.Services;
using Monitoramento.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHostedService<MonitoramentoWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options => {
    options.AddPolicy("FrontEnd", policy => 
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 2. CRIAÇÃO DO APP (Bater o bolo - APENAS UMA VEZ!)
var app = builder.Build();

// 3. BLOCO DE INSERÇÃO AUTOMÁTICA
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); 

        // Se o banco estiver vazio, insere o Google
        if (!context.Ativos.Any())
        {
            context.Ativos.Add(new Monitoramento.Domain.Entities.Ativo 
            { 
                Url = "https://www.google.com", 
                EstaOnline = true, 
                UltimaVerificacao = DateTime.UtcNow 
            });
            context.SaveChanges();
            Console.WriteLine("✅ Banco populado com sucesso!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao popular banco: {ex.Message}");
    }
}

// 4. PIPELINE DO APP (O que o servidor faz)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontEnd"); // Ativa o CORS 
app.UseHttpsRedirection();

app.MapGet("/", () => "Sistema de Monitoramento Online rodando no Debian!");

// ROTA PARA O FRONT-END 
app.MapGet("/api/monitoramento/status", async (AppDbContext context) => 
{
    return await context.Ativos.ToListAsync();
});

app.Run();