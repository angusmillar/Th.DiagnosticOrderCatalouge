using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Th.Fhir.Terminology.Domain.FhirOrderCatalogue;
using Task = System.Threading.Tasks.Task;

namespace Th.Fhir.Terminology.Domain.OrderCatalogueCsv;

public class OrderCatalogueCsvReader(ILogger<OrderCatalogueCsvReader> logger, IFhirCatalogueFactory fhirCatalogueFactory) : IOrderCatalogueCsvReader
{
    public async Task Read(OrderCatalogueCsvReaderInput input)
    {
        if (!IsInputValid(input))
        {
            return;
        }

        SetupEnvironment(input);
        
        List<TerminologyCsvRecord> terminologyCsvRecordList = ReadRawRecords(input.InputCatalogueCsvFile.FullName);
        
        List<TerminologyRecord> terminologyRecordList = new();
        foreach (TerminologyCsvRecord terminologyCsvRecord in terminologyCsvRecordList)
        {
            if (string.IsNullOrWhiteSpace(terminologyCsvRecord.Code))
            {
                logger.LogError("All rows must have a none empty Code value");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(terminologyCsvRecord.DisplayName))
            {
                logger.LogError("All rows must have a none empty Display Name for its Code");
                return;
            }
            
            if (terminologyCsvRecord.CodeSystemUri == null)
            {
                logger.LogError("All rows must have a CodeSystem URI");
                return;
            }
            
            if (!Uri.TryCreate(terminologyCsvRecord.CodeSystemUri, UriKind.Absolute, out var uriResult))
            {
                logger.LogError("The following CodeSystem URIs is an invalid URI: {CodeSystemUri}", 
                    terminologyCsvRecord.CodeSystemUri);
                return;
            }
            
            terminologyRecordList.Add(new TerminologyRecord()
            {
                Code = terminologyCsvRecord.Code,
                DisplayName = terminologyCsvRecord.DisplayName,
                CodeSystems = uriResult,
                Synonyms = terminologyCsvRecord.Synonyms,
            });
        }

        TerminologyResourceSet terminologyResourceSet = fhirCatalogueFactory.GetFhirTerminology(
            businessCode: input.BusinessCode,
            terminologyRecordList: terminologyRecordList,
            version: input.CatalogueVersion);
        
        await WriteOutFhirResource(
            filePath: new FileInfo(Path.Combine(input.OutputDirectory.FullName, $"{terminologyResourceSet.ValueSet.Id}-ValueSet.json")), 
            resource: terminologyResourceSet.ValueSet);
        
        await WriteOutFhirResource(
            filePath: new FileInfo(Path.Combine(input.OutputDirectory.FullName, $"{terminologyResourceSet.LocalCodeSystem.Id}-CodeSystems.json")),
            resource: terminologyResourceSet.LocalCodeSystem);
        
        if (terminologyResourceSet.SynonymSupplementCodeSystem is not null)
        {
            await WriteOutFhirResource(
                filePath: new FileInfo(Path.Combine(input.OutputDirectory.FullName, $"{terminologyResourceSet.SynonymSupplementCodeSystem.Id}-CodeSystems.json")),
                resource: terminologyResourceSet.SynonymSupplementCodeSystem);    
        }
        
    }

    private async Task WriteOutFhirResource(FileInfo filePath, Resource resource)
    {
        await File.WriteAllTextAsync(filePath.FullName, await resource.ToJsonAsync(new FhirJsonSerializationSettings() { Pretty = true }));
        logger.LogInformation("FHIR {ResourceType} written to: {FilePath}", resource.TypeName, filePath);
        
    }

    private static List<TerminologyCsvRecord> ReadRawRecords(string csvFilePath)
    {
        using var streamReader = new StreamReader(csvFilePath);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        csvReader.Context.RegisterClassMap<TerminologyCsvRecordMap>();
        return csvReader.GetRecords<TerminologyCsvRecord>().ToList();
    }

    private void SetupEnvironment(
        OrderCatalogueCsvReaderInput input)
    {
        if (!input.OutputDirectory.Exists)
        {
            input.OutputDirectory.Create();
            return;
        }

        foreach (FileInfo fileInfo in input.OutputDirectory.GetFiles(searchPattern: "*.json"))   
        {
            fileInfo.Delete();
        }
    }

    private bool IsInputValid(
        OrderCatalogueCsvReaderInput input)
    {
        if (!input.InputCatalogueCsvFile.Exists)
        {
            logger.LogError("Unable to locate the CSV input file at: {FilePath}", input.InputCatalogueCsvFile.FullName);
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(input.BusinessCode))
        {
            throw new ApplicationException($"{nameof(input.BusinessCode)} must not be empty.");
        }
        
        if (!string.IsNullOrWhiteSpace(input.CatalogueVersion) && !IsValidSemVer(input.CatalogueVersion))
        {
            throw new ApplicationException($"{nameof(input.CatalogueVersion)} must be a valid SemVer version string if provided (e.g. 1.0.0)");
        }
        
        return true;
    }
    
    private static bool IsValidSemVer(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        const string pattern =
            @"^(0|[1-9]\d*)\." +
            @"(0|[1-9]\d*)\." +
            @"(0|[1-9]\d*)" +
            @"(?:-((?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*))*))?" +
            @"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$";

        return Regex.IsMatch(version, pattern);
    }
}