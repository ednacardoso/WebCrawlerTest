using System;

namespace TesteWebCrawler
{
    public class DadosExtraidos
    {
        public string Time { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public string Country { get; set; }
        public string ResponseTime { get; set; }
        public string Uptime { get; set; }
        public string Protocol { get; set; }

        public DadosExtraidos()
        {
            Time = string.Empty;
            IP = string.Empty;
            Port = string.Empty;
            Country = string.Empty;
            ResponseTime = string.Empty;
            Uptime = string.Empty;
            Protocol = string.Empty;
        }
    }
}

