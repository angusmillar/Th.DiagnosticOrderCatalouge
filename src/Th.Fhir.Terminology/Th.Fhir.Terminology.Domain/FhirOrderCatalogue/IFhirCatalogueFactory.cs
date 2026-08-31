namespace Th.Fhir.Terminology.Domain.FhirOrderCatalogue;

public interface IFhirCatalogueFactory
{
    /// <summary>
    /// Converts an Order Catalogue's terminology records into a local complete CodeSystem,
    /// a synonym supplement CodeSystem for the international codes, and a ValueSet spanning both.
    /// </summary>
    /// <param name="businessCode">A code friendly name of the business. Must not be empty.</param>
    /// <param name="terminologyRecordList">The Order Catalogue records. Must not be empty.</param>
    /// <param name="version">
    /// Optional business version stamped on each generated resource, and used for the local
    /// system's ValueSet.compose.include.version. When null no version is set anywhere.
    /// </param>
    TerminologyResourceSet GetFhirTerminology(string businessCode,
        List<TerminologyRecord> terminologyRecordList,
        string? version = null);
}
