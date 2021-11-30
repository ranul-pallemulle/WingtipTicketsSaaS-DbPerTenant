using System.Globalization;
using DnsClient;
using Events_Tenant.Common.Interfaces;
using Events_Tenant.Common.Repositories;
using Events_Tenant.Common.Utilities;
using Events_TenantUserApp.EF.CatalogDB;
using Events_TenantUserApp.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

ReadAppConfig(builder.Configuration);

// Authentication settings
builder.Services.AddAuthentication("CookieAuthentication")
    .AddCookie("CookieAuthentication");

//Localisation settings
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add framework services.
builder.Services.AddMvc()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// Adds a default in-memory implementation of IDistributedCache.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

//register catalog DB
builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(GetCatalogConnectionString(CatalogConfig, DatabaseConfig)));

//Add Application services
builder.Services.AddTransient<ICatalogRepository, CatalogRepository>();
builder.Services.AddSingleton<ITenantRepository>(p => new TenantRepository(GetBasicSqlConnectionString()));
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<ILookupClient>(p => new LookupClient());

//create instance of utilities class
builder.Services.AddTransient<IUtilities, Utilities>();
var provider = builder.Services.BuildServiceProvider();
_utilities = provider.GetService<IUtilities>();
_catalogRepository = provider.GetService<ICatalogRepository>();
_tenantRepository = provider.GetService<ITenantRepository>();
_client = provider.GetService<ILookupClient>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

#region Localisation settings

//get the list of supported cultures from the appsettings.json
var allSupportedCultures = app.Configuration.GetSection("SupportedCultures").Get<SupportedCultures>();
var defaultCulture = app.Configuration["DefaultRequestCulture"];

if (allSupportedCultures != null && defaultCulture != null)
{
    List<CultureInfo> supportedCultures = allSupportedCultures.SupportedCulture.Select(t => new CultureInfo(t)).ToList();

    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(defaultCulture),
        //get the default culture from appsettings.json
        SupportedCultures = supportedCultures, // UI strings that we have localized.
        SupportedUICultures = supportedCultures,
        RequestCultureProviders = new List<IRequestCultureProvider>()
    });
}
else
{
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("en-US"),
        RequestCultureProviders = new List<IRequestCultureProvider>()
    });
}

#endregion

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

InitialiseShardMapManager();

app.Run();

partial class Program
{
    #region Private fields
    private static IUtilities _utilities;
    private static ICatalogRepository _catalogRepository;
    private static ITenantRepository _tenantRepository;
    private static ILookupClient _client;
    #endregion
    #region Public Properties
    public static DatabaseConfig DatabaseConfig { get; set; }
    public static CatalogConfig CatalogConfig { get; set; }
    public static TenantServerConfig TenantServerConfig { get; set; }
    #endregion

    #region Private methods

    /// <summary>
    ///  Gets the catalog connection string using the app settings
    /// </summary>
    /// <param name="catalogConfig">The catalog configuration.</param>
    /// <param name="databaseConfig">The database configuration.</param>
    /// <returns></returns>
    private static string GetCatalogConnectionString(CatalogConfig catalogConfig, DatabaseConfig databaseConfig)
    {
        return
            $"Server=tcp:{catalogConfig.CatalogServer},1433;Database={catalogConfig.CatalogDatabase};User ID={databaseConfig.DatabaseUser};Password={databaseConfig.DatabasePassword};Trusted_Connection=False;Encrypt=True;";
    }

    /// <summary>
    /// Reads the application settings from appsettings.json
    /// </summary>
    private static void ReadAppConfig(IConfiguration configuration)
    {
        DatabaseConfig = new DatabaseConfig
        {
            DatabasePassword = configuration["DatabasePassword"],
            DatabaseUser = configuration["DatabaseUser"],
            DatabaseServerPort = Convert.ToInt32(configuration["DatabaseServerPort"]),
            SqlProtocol = SqlProtocol.Tcp,
            ConnectionTimeOut = Convert.ToInt32(configuration["ConnectionTimeOut"]),
            LearnHowFooterUrl = configuration["LearnHowFooterUrl"]
        };

        CatalogConfig = new CatalogConfig
        {
            ServicePlan = configuration["ServicePlan"],
            CatalogDatabase = configuration["CatalogDatabase"],
            CatalogServer = configuration["CatalogServer"] + ".database.windows.net",
            CatalogLocation = configuration["APP_REGION"]
        };

        TenantServerConfig = new TenantServerConfig
        {
            TenantServer = configuration["TenantServer"] + ".database.windows.net"               
        };

        bool isResetEventDatesEnabled = false;
        if (bool.TryParse(configuration["ResetEventDates"], out isResetEventDatesEnabled))
        {
            TenantServerConfig.ResetEventDates = isResetEventDatesEnabled;
        }
    }

    /// <summary>
    /// Initialises the shard map manager and shard map 
    /// <para>Also does all tasks related to sharding</para>
    /// </summary>
    private static void InitialiseShardMapManager()
    {
        var basicConnectionString = GetBasicSqlConnectionString();
        SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder(basicConnectionString)
        {
            DataSource = DatabaseConfig.SqlProtocol + ":" + CatalogConfig.CatalogServer + "," + DatabaseConfig.DatabaseServerPort,
            InitialCatalog = CatalogConfig.CatalogDatabase
        };

        var sharding = new Sharding(CatalogConfig.CatalogDatabase, connectionString.ConnectionString, _catalogRepository, _tenantRepository, _utilities);
    }

    /// <summary>
    /// Gets the basic SQL connection string.
    /// </summary>
    /// <returns></returns>
    private static string GetBasicSqlConnectionString()
    {
        var connStrBldr = new SqlConnectionStringBuilder
        {
            UserID = DatabaseConfig.DatabaseUser,
            Password = DatabaseConfig.DatabasePassword,
            ApplicationName = "EntityFramework",
            ConnectTimeout = DatabaseConfig.ConnectionTimeOut,
            LoadBalanceTimeout = 15
        };

        return connStrBldr.ConnectionString;
    }
    #endregion
}

class SqlConnectionStringBuilder
{
    public string UserID { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApplicationName { get; set; }
    public int ConnectTimeout { get; set; }
    public int LoadBalanceTimeout { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string InitialCatalog { get; set; } = string.Empty;

    private string _connectionString;

    public string ConnectionString => _connectionString;

    public SqlConnectionStringBuilder() 
    {
        _connectionString = $"Server=tcp:{DataSource},1433;Database={InitialCatalog};User ID={UserID};Password={Password};Trusted_Connection=False;Encrypt=True;";
    }
    public SqlConnectionStringBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }
}
