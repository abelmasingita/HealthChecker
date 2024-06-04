using System;

namespace HealthChecker.Models
{
    public class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string HealthCheckUri { get; set; }
        public string Status { get; set; } = "DOWN";
        public ServerError Error { get; set; } = null;
        public DateTime LastTimeUp { get; set; } = DateTime.MinValue;
    }

    public class ServerError
    {
        public int Status { get; set; }
        public string Body { get; set; }
    }

    public class ServerFilterInput
    {
        public string Status { get; set; }
    }

}
