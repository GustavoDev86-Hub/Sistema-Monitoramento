using System.Net.Http;
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
        RabbitMqService rabbitMqService) 
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
            var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sites = await _context.Ativos.ToListAsync();

            foreach (var site in sites)
            {
                try 
                {
                    using var client = new HttpClient();
                    // TESTA A URL DO SITE ESPECÍFICO
                    var response = await client.GetAsync(site.Url); 

                    site.EstaOnline = response.IsSuccessStatusCode;
                    site.UltimaVerificacao = DateTime.Now;

                    _context.Ativos.Update(site);
                    
                    Console.WriteLine($"[VERIFICADOR] Site: {site.Url} | Status: {site.EstaOnline} | Hora: {site.UltimaVerificacao}");
                }
                catch (Exception ex)
                {
                    site.EstaOnline = false;
                    site.UltimaVerificacao = DateTime.Now;
                    Console.WriteLine($"[ERRO] Falha ao testar {site.Url}: {ex.Message}");
                }
            }
            // SALVA TUDO NO BANCO DEPOIS DO LOOP
            await _context.SaveChangesAsync();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
        
        ativo.UltimaVerificacao = DateTime.Now;
    }
}