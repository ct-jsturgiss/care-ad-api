namespace CareAdApi
{
    public static class Constants
    {
        public const string ServiceName = "careapp-ad-api";
        public const string Copyright = "Copyright (c) 2025 - Caretaker Landscape and Tree Management";
        public const string LogTemplateName = "app_.log";
        public const string ArgLogLevel = "--loglevel";
        public const string DomainTarget = "caretaker.local";

        public const string LocalConfigFile = "app-local-cfg.json";
        public const string KeyFileName = "api.key";
        public const int LocalPort = 7078;
        public const string Endpoint = "10.10.40.7:9901";

        public static class LoggingConstants
        {
            public const string ConsoleFormat = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            public const string FileFormat = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        }

        public static class AttrConstants
        {
            public const string UserPrincipal = "userPrincipal";
            public const string EmployeeId = "employeeId";
            public const string ManagerPrincipal = "manager";
        }

        public static class Headers
        {
            public const string HeaderKey = "capp-key";
        }
    }
}
