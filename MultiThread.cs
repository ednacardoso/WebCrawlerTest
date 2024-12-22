using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TesteWebCrawler;

public class WebCrawlerManager
{
    private const int MaxThreads = 3; // Limite de threads simultâneas
    private SemaphoreSlim semaphore = new SemaphoreSlim(MaxThreads);

    private readonly Func<string, int, Task<string>> _getPageContent; 
    private readonly Func<string, List<DadosExtraidos>> _extractData; 
    private readonly Action<List<DadosExtraidos>, string> _saveData; 

    public WebCrawlerManager(
        Func<string, int, Task<string>> getPageContent,
        Func<string, List<DadosExtraidos>> extractData,
        Action<List<DadosExtraidos>, string> saveData)
    {
        _getPageContent = getPageContent;
        _extractData = extractData;
        _saveData = saveData;
    }

    public async Task StartCrawlers(string baseUrl, int totalPages, string outputPath)
    {
        List<Task> tasks = new List<Task>();

        for (int i = 1; i <= totalPages; i++)
        {
            string url = $"{baseUrl}?page={i}";
            int pageIndex = i;
            tasks.Add(ProcessPage(url, pageIndex, outputPath));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessPage(string url, int pageIndex, string outputPath)
    {
        await semaphore.WaitAsync(); 

        try
        {
            Console.WriteLine($"Iniciando processamento para a página {pageIndex}");

           
            string pageHtmlContent = await _getPageContent(url, pageIndex);
            if (string.IsNullOrEmpty(pageHtmlContent)) return;

            
            var resultados = _extractData(pageHtmlContent);

           
            string filePath = $"{outputPath}\\resultados_{pageIndex}.json";
            _saveData(resultados, filePath);

            Console.WriteLine($"Página {pageIndex} processada com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar a página {pageIndex}: {ex.Message}");
        }
        finally
        {
            semaphore.Release(); 
        }
    }
}
