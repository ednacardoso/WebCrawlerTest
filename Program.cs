using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using cloudscribe.HtmlAgilityPack;
using Microsoft.Data.Sqlite;
using TesteWebCrawler;
using System.Data.SQLite;

class Program
{
    private static readonly HttpClient client = new HttpClient();

   
    static List<Task> tasks = new List<Task>();

    
    static async Task ProcessarPaginas(string urlBase, int totalPages)
    {
        
        for (int i = 1; i <= totalPages; i++)
        {
           
            tasks.Add(Task.Run(() => GetPageContent(urlBase, i)));
        }

        
        await Task.WhenAll(tasks);
    }

    static async Task Main(string[] args)
    {
        // banco de dados
        InitializeDatabase();

        string baseUrl = "https://proxyservers.pro/proxy/list/order/updated/order_dir/desc";
        string outputPath = @"C:\Users\Edna01\DadosExtraidos";
        int totalPages = 5;

        // gerenciador de webcrawler
        var webCrawlerManager = new WebCrawlerManager(GetPageContent, ExtractData, SaveResults);

        Console.WriteLine("Iniciando webcrawler multithread...");
        await webCrawlerManager.StartCrawlers(baseUrl, totalPages, outputPath);

        Console.WriteLine("Webcrawler finalizado.");
    }

    static void InitializeDatabase()
    {
        using (var connection = new SQLiteConnection("Data Source=data.db;Version=3;"))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Execucao (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DataInicio DATETIME,
                    DataFim DATETIME,
                    QuantidadePaginas INTEGER,
                    QuantidadeLinhas INTEGER,
                    ArquivoJson TEXT
                );
            ";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    static async Task<string> GetPageContent(string urlBase, int pageNumber)
    {
        try
        {
            // Verifica se a URL base já contém parâmetros
            string separator = urlBase.Contains("?") ? "&" : "?";

            // Monta a URL completa para a requisição
            string fullUrl = $"{urlBase}{separator}page={pageNumber}";

            Console.WriteLine($"Acessando URL: {fullUrl}");

            // Realiza a requisição HTTP
            HttpResponseMessage response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                // Se a resposta for bem-sucedida, obtém o conteúdo HTML
                string htmlContent = await response.Content.ReadAsStringAsync();
                // Salva o conteúdo HTML em um arquivo
                SaveHtmlToFile(htmlContent, pageNumber);
                return htmlContent;
            }
            else
            {
                Console.WriteLine($"Erro ao acessar a página: {response.StatusCode}");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exceção ao acessar a página: {ex.Message}");
            return string.Empty;
        }
    }




    static void SaveHtmlToFile(string htmlContent, int pageNumber)
    {
        string path = $@"C:\Users\Edna01\DadosExtraidos\pagina_{pageNumber}.html";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, htmlContent);
            Console.WriteLine($"Página {pageNumber} salva com sucesso em: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar o HTML: {ex.Message}");
        }
    }

    static List<DadosExtraidos> ExtractData(string htmlContent)
    {
        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        var resultados = new List<DadosExtraidos>();

        var rows = document.DocumentNode.SelectNodes("//tr[@valign='top']");
        if (rows != null)
        {
            foreach (var row in rows)
            {
                var time = row.SelectSingleNode(".//td[1]")?.InnerText.Trim() ?? string.Empty;
                var ip = row.SelectSingleNode(".//td[2]//a")?.InnerText.Trim() ?? string.Empty;
                var port = row.SelectSingleNode(".//td[3]")?.InnerText.Trim() ?? string.Empty;
                var country = row.SelectSingleNode(".//td[4]")?.InnerText.Trim() ?? string.Empty;
                var responseTime = row.SelectSingleNode(".//td[5]//div[@class='progress-spec-value']")?.InnerText.Trim() ?? string.Empty;
                var uptime = row.SelectSingleNode(".//td[6]")?.InnerText.Trim() ?? string.Empty;
                var protocol = row.SelectSingleNode(".//td[7]")?.InnerText.Trim() ?? string.Empty;

                resultados.Add(new DadosExtraidos
                {
                    Time = time,
                    IP = ip,
                    Port = port,
                    Country = country,
                    ResponseTime = responseTime,
                    Uptime = uptime,
                    Protocol = protocol
                });
            }
        }

        return resultados;
    }

    static void SaveResults(List<DadosExtraidos> dados, string filePath)
    {
        try
        {
            // Salvar os dados como JSON
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            string json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Dados salvos em: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar os dados: {ex.Message}");
        }
    }

    static void SaveExecutionToDatabase(int lineCount, string jsonPath, int pageCount)
    {
        using (var connection = new SQLiteConnection("Data Source=data.db;Version=3;"))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Execucao (DataInicio, DataFim, QuantidadePaginas, QuantidadeLinhas, ArquivoJson)
                VALUES (@DataInicio, @DataFim, @QuantidadePaginas, @QuantidadeLinhas, @ArquivoJson);
            ";

            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@DataInicio", DateTime.Now.AddMinutes(-pageCount));
                command.Parameters.AddWithValue("@DataFim", DateTime.Now);
                command.Parameters.AddWithValue("@QuantidadePaginas", pageCount);
                command.Parameters.AddWithValue("@QuantidadeLinhas", lineCount);
                command.Parameters.AddWithValue("@ArquivoJson", jsonPath);

                command.ExecuteNonQuery();
            }
        }
    }
}
