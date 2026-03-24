using Microsoft.EntityFrameworkCore;
using Monitoramento.Infrastructure.Data;
using Monitoramento.Infrastructure.Messaging;

namespace Monitoramento.API.Services;

public class MonitoramentoWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoramentoWorker> _logger;
    private readonly RabbitMqService _rabbitMqService; // Injetando o serviço de Rabbit

    public MonitoramentoWorker(
        IServiceProvider serviceProvider, 
        ILogger<MonitoramentoWorker> logger, 
        RabbitMqService rabbitMqService) // Construtor atualizado
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMqService = rabbitMqService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Monitoramento Iniciado...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ativos = await context.Ativos.ToListAsync();

                if (ativos.Any())
                {
                    _logger.LogInformation("Verificando {Count} ativos...", ativos.Count);
                    
                    // Executa as verificações em paralelo
                    var tarefas = ativos.Select(a => VerificarStatus(a));
                    await Task.WhenAll(tarefas);
                    
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("Nenhum ativo cadastrado para monitorar.");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task VerificarStatus(Monitoramento.Domain.Entities.Ativo ativo)
    {
        using var client = new HttpClient();
        try 
        {
            var response = await client.GetAsync(ativo.Url);
            ativo.EstaOnline = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Status de {Url}: {Status}", ativo.Url, ativo.EstaOnline ? "ONLINE" : "OFFLINE");

            if (!ativo.EstaOnline)
            {
                await _rabbitMqService.EnviarAlerta($"O site {ativo.Url} está OFFLINE!");
            }
        }
        catch 
        {
            ativo.EstaOnline = false;
            _logger.LogError("FALHA CRÍTICA: {Url} está inacessível!", ativo.Url);
            await _rabbitMqService.EnviarAlerta($"O site {ativo.Url} caiu totalmente!");
        }
        
        ativo.UltimaVerificacao = DateTime.UtcNow;
    }
}