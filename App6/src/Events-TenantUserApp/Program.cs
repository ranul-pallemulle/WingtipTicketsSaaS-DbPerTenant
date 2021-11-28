using Events_Tenant.Common.Utilities;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

public class Program
{
    public static DatabaseConfig DatabaseConfig { get; set; }
    public static CatalogConfig CatalogConfig { get; set; }
    public static TenantServerConfig TenantServerConfig { get; set; }
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        DatabaseConfig = new DatabaseConfig
        {
            DatabasePassword = builder.Configuration["DatabasePassword"],
            DatabaseUser = builder.Configuration["DatabaseUser"],
            DatabaseServerPort = Convert.ToInt32(builder.Configuration["DatabaseServerPort"]),
            SqlProtocol = SqlProtocol.Tcp,
            ConnectionTimeOut = Convert.ToInt32(builder.Configuration["ConnectionTimeOut"]),
            LearnHowFooterUrl = builder.Configuration["LearnHowFooterUrl"]
        };

        CatalogConfig = new CatalogConfig
        {
            ServicePlan = builder.Configuration["ServicePlan"],
            CatalogDatabase = builder.Configuration["CatalogDatabase"],
            CatalogServer = builder.Configuration["CatalogServer"] + ".database.windows.net",
            CatalogLocation = builder.Configuration["APP_REGION"]
        };

        TenantServerConfig = new TenantServerConfig
        {
            TenantServer = builder.Configuration["TenantServer"] + ".database.windows.net"               
        };

        bool isResetEventDatesEnabled = false;
        if (bool.TryParse(builder.Configuration["ResetEventDates"], out isResetEventDatesEnabled))
        {
            TenantServerConfig.ResetEventDates = isResetEventDatesEnabled;
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}


