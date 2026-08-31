namespace Th.Fhir.Terminology.Domain.OrderCatalogueCsv;

public record OrderCatalogueCsvReaderInput(
    string BusinessCode, 
    string CatalogueVersion, 
    FileInfo InputCatalogueCsvFile, 
    DirectoryInfo OutputDirectory);