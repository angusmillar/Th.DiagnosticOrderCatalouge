using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Th.Fhir.Terminology.Domain.FhirOrderCatalogue;
using Th.Fhir.Terminology.Domain.OrderCatalogueCsv;

Console.WriteLine("Running: Th.Fhir.Terminology.Console");

var builder = Host.CreateApplicationBuilder(args);

//Use Serilog for logging — clear default providers (console, debug) to prevent duplicate output
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger(), dispose: true);

// Add services/tools as required.
builder.Services.AddScoped<IFhirCatalogueFactory, FhirCatalogueFactory>();
builder.Services.AddScoped<IOrderCatalogueCsvReader, OrderCatalogueCsvReader>();

//Build the host and resolve Application via a scope
using var host = builder.Build();

//Create a new scope
await using var scope = host.Services.CreateAsyncScope();

//Choose the tool to be run:
var input = new OrderCatalogueCsvReaderInput(
    BusinessCode: "acme-pathology",
    InputCatalogueCsvFile: new FileInfo(
        @"C:\Temp\Th.Fhir.Terminology\OrderCatalogue\BPP23174_2184_QML_August - Copy.csv"),
    OutputDirectory: new DirectoryInfo(@"C:\Temp\Th.Fhir.Terminology\OrderCatalogue\Output"),
    CatalogueVersion: "1.0.0");
    
await scope.ServiceProvider.GetRequiredService<IOrderCatalogueCsvReader>().Read(input);
