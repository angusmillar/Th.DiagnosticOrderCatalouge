namespace Th.Fhir.Terminology.Domain.FhirOrderCatalogue;

public class TerminologyRecord
{
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public required Uri CodeSystems { get; set; }
    public string? Synonyms { get; set; }

}