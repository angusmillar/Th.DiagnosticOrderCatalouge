using Hl7.Fhir.Model;

namespace Th.Fhir.Terminology.Domain.FhirOrderCatalogue;

/// <summary>
/// The set of FHIR terminology resources generated from an Order Catalogue.
/// </summary>
/// <param name="LocalCodeSystem">The complete CodeSystem defining every local Catalogue code.</param>
/// <param name="SynonymSupplementCodeSystem">
/// A supplement CodeSystem adding localised synonym designations to the international
/// CodeSystem (SNOMED CT or LOINC). Null when the Catalogue holds no international codes,
/// or when none of the international codes carry synonyms.
/// </param>
/// <param name="ValueSet">The ValueSet drawing together the local and international codes.</param>
public sealed record TerminologyResourceSet(
    CodeSystem LocalCodeSystem,
    CodeSystem? SynonymSupplementCodeSystem,
    ValueSet ValueSet);
