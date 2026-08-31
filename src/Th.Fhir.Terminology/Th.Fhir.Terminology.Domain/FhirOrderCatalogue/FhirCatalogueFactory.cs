using System.Text;
using Hl7.Fhir.Model;

namespace Th.Fhir.Terminology.Domain.FhirOrderCatalogue;

public class FhirCatalogueFactory : IFhirCatalogueFactory
{
    private static readonly Uri SnomedCtSystemUri = new("http://snomed.info/sct");
    private static readonly Uri LoincSystemUri = new("http://loinc.org");

    private const string SupplementUrlSuffix = "synonym-supplement";
    private const string CodeSystemPathSegment = "/CodeSystem/";
    private const string ValueSetPathSegment = "/ValueSet/";
    private const string DesignationLanguage = "en-AU";
    private const char SynonymDelimiter = ',';
    private DateTime Now;
    
    /// <summary>
    /// Declares that the ValueSet depends on a CodeSystem supplement and must not be used in its absence.
    /// </summary>
    private const string ValueSetSupplementExtensionUrl = "http://hl7.org/fhir/StructureDefinition/valueset-supplement";

    public TerminologyResourceSet GetFhirTerminology(string businessCode,
        List<TerminologyRecord> terminologyRecordList,
        string? version = null)
    {
        businessCode = TrimAndValidateBusinessCode(businessCode);
        Now = DateTime.Now;
        
        if (terminologyRecordList.Count == 0)
        {
            throw new ApplicationException("The Catalogue must contain at least one terminology record.");
        }

        var systemUriList = terminologyRecordList.Select(x => x.CodeSystems).Distinct().ToList();
        if (systemUriList.Count > 2)
        {
            throw new ApplicationException("Codes in the Catalogue must only come from two separate CodeSystems, " +
                                           "typically (SNOMED & Local) or (LOINC & Local)");
        }

        if (systemUriList.Contains(SnomedCtSystemUri) && systemUriList.Contains(LoincSystemUri))
        {
            throw new ApplicationException("Codes in the Catalogue must only come from a Local CodeSystems and Only one " +
                                           "other international CodeSystem such as (SNOMED or LOINC)");
        }

        List<Uri> localSystemUriList = systemUriList.Where(x => !IsInternationalSystem(x)).ToList();
        if (localSystemUriList.Count > 1)
        {
            throw new ApplicationException("Codes in the Catalogue must only come from a single Local CodeSystems, " +
                                           $"yet the following were found: {string.Join(", ", localSystemUriList.Select(x => x.OriginalString))}");
        }

        if (localSystemUriList.Count == 0)
        {
            throw new ApplicationException("Codes in the Catalogue must include a Local CodeSystems, as the Local " +
                                           "CodeSystems URL is the basis for the generated resource URLs.");
        }

        Uri localSystemUri = localSystemUriList[0];
        Uri? internationalSystemUri = systemUriList.FirstOrDefault(IsInternationalSystem);

        List<TerminologyRecord> localRecordList = GetRecordListForSystem(terminologyRecordList, localSystemUri);
        List<TerminologyRecord> internationalRecordList = internationalSystemUri is null
            ? []
            : GetRecordListForSystem(terminologyRecordList, internationalSystemUri);
        
        string localUrl = TrimUrl(localSystemUri);

        CodeSystem localCodeSystem = BuildLocalCodeSystem(
            businessCode, 
            localUrl, 
            version,
            Now,
            localRecordList);

        CodeSystem? supplementCodeSystem = BuildSupplementCodeSystem(
            businessCode,
            localUrl,
            version,
            Now,
            internationalSystemUri,
            internationalRecordList);

        ValueSet valueSet = BuildValueSet(
            businessCode,
            localUrl,
            version,
            Now,
            localSystemUri,
            internationalSystemUri,
            internationalRecordList,
            supplementCodeSystem);

        return new TerminologyResourceSet(localCodeSystem, supplementCodeSystem, valueSet);
    }

    private CodeSystem BuildLocalCodeSystem(
        string businessName,
        string localUrl,
        string? version,
        DateTime Date,
        List<TerminologyRecord> localRecordList)
    {
        
        var codeSystem = new CodeSystem()
        {
            Id = $"{businessName}-local-order-codes",
            Url = localUrl,
            Name = $"{GetName(businessName)} Local Order Codes",
            Version = version,
            DateElement = GetFhirDateTime(Date),
            Status = PublicationStatus.Active,
            Content = CodeSystemContentMode.Complete,
            CaseSensitive = true
        };

        foreach (TerminologyRecord terminologyRecord in localRecordList)
        {
            codeSystem.Concept.Add(GetLocalConcept(terminologyRecord));
        }

        return codeSystem;
    }

    /// <summary>
    /// Builds the supplement CodeSystem carrying the localised synonyms for the international codes.
    /// Records without synonyms are skipped, as they would add nothing to the supplemented system.
    /// Returns null when there is nothing to supplement.
    /// </summary>
    private static CodeSystem? BuildSupplementCodeSystem(
        string businessName,
        string localUrl,
        string? version,
        DateTime Date,
        Uri? internationalSystemUri,
        List<TerminologyRecord> internationalRecordList)
    {
        if (internationalSystemUri is null || internationalRecordList.Count == 0)
        {
            return null;
        }

        var conceptList = new List<CodeSystem.ConceptDefinitionComponent>();
        foreach (TerminologyRecord terminologyRecord in internationalRecordList)
        {
            string[] synonymList = SplitSynonyms(terminologyRecord.Synonyms);
            if (synonymList.Length == 0)
            {
                continue;
            }

            conceptList.Add(GetSupplementConcept(terminologyRecord, synonymList));
        }

        if (conceptList.Count == 0)
        {
            return null;
        }

        return new CodeSystem()
        {
            Id = $"{businessName}-order-code-" + SupplementUrlSuffix,
            Url = localUrl + "-" + SupplementUrlSuffix,
            Name = $"{GetName(businessName)} Order Code " + GetName(SupplementUrlSuffix),
            Version = version,
            DateElement = GetFhirDateTime(Date),
            Status = PublicationStatus.Active,
            Content = CodeSystemContentMode.Supplement,
            Supplements = TrimUrl(internationalSystemUri),
            Concept = conceptList
        };
    }

    private static FhirDateTime GetFhirDateTime(
        DateTime date)
    {
        return new FhirDateTime(year: date.Year, month: date.Month, day: date.Day);
    }

    /// <summary>
    /// Builds the ValueSet. The local system is included by system only, drawing in all of its
    /// codes, while the international codes must be enumerated as no filter can select them.
    /// </summary>
    private static ValueSet BuildValueSet(
        string businessName,
        string localUrl,
        string? version,
        DateTime Date,
        Uri localSystemUri,
        Uri? internationalSystemUri,
        List<TerminologyRecord> internationalRecordList,
        CodeSystem? supplementCodeSystem)
    {
        var valueSet = new ValueSet()
        { 
            Id = $"{businessName}-order-catalogue",
            Url = GetValueSetUrl(localUrl),
            Name = $"{GetName(businessName)} Order Catalogue",
            Version = version,
            DateElement = GetFhirDateTime(Date),
            Status = PublicationStatus.Active,
            Compose = new ValueSet.ComposeComponent()
        };

        if (supplementCodeSystem is not null)
        {
            valueSet.Extension.Add(new Extension(
                ValueSetSupplementExtensionUrl,
                new Canonical(supplementCodeSystem.Url)));
        }

        valueSet.Compose.Include.Add(new ValueSet.ConceptSetComponent()
        {
            System = TrimUrl(localSystemUri),
            Version = version
        });

        if (internationalSystemUri is null || internationalRecordList.Count == 0)
        {
            return valueSet;
        }

        // No version is set for the international system as the Catalogue does not carry one.
        var internationalInclude = new ValueSet.ConceptSetComponent()
        {
            System = TrimUrl(internationalSystemUri)
        };

        foreach (TerminologyRecord terminologyRecord in internationalRecordList)
        {
            internationalInclude.Concept.Add(new ValueSet.ConceptReferenceComponent()
            {
                Code = terminologyRecord.Code,
                Display = terminologyRecord.DisplayName
            });
        }

        valueSet.Compose.Include.Add(internationalInclude);

        return valueSet;
    }

    private static string TrimAndValidateBusinessCode(
        string businessCode)
    {
        if (string.IsNullOrWhiteSpace(businessCode))
        {
            throw new ApplicationException($"{nameof(businessCode)} must not be empty.");
        }

        businessCode = businessCode.Replace(" ",  string.Empty).Trim();
        return businessCode;
    }

    private static bool IsInternationalSystem(Uri systemUri) =>
        systemUri.Equals(SnomedCtSystemUri) || systemUri.Equals(LoincSystemUri);

    /// <summary>
    /// Returns every record for the given system, throwing when a code appears more than once.
    /// </summary>
    private static List<TerminologyRecord> GetRecordListForSystem(
        List<TerminologyRecord> terminologyRecordList,
        Uri systemUri)
    {
        List<TerminologyRecord> recordList = terminologyRecordList
            .Where(x => x.CodeSystems.Equals(systemUri))
            .ToList();

        List<string> duplicateCodeList = recordList
            .GroupBy(x => x.Code, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateCodeList.Count > 0)
        {
            throw new ApplicationException($"The CodeSystems {systemUri.OriginalString} contains duplicate codes in " +
                                           $"the Catalogue: {string.Join(", ", duplicateCodeList)}");
        }

        return recordList;
    }

    private static string[] SplitSynonyms(
        string? synonyms)
    {
        if (string.IsNullOrWhiteSpace(synonyms))
        {
            return [];
        }

        return synonyms.Split(SynonymDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static CodeSystem.ConceptDefinitionComponent GetLocalConcept(
        TerminologyRecord terminologyRecord)
    {
        var concept = new CodeSystem.ConceptDefinitionComponent()
        {
            Code = terminologyRecord.Code,
            Display = terminologyRecord.DisplayName
        };

        List<CodeSystem.DesignationComponent> designationList =
            GetDesignationList(SplitSynonyms(terminologyRecord.Synonyms));

        if (designationList.Count > 0)
        {
            concept.Designation = designationList;
        }

        return concept;
    }

    /// <summary>
    /// A supplement concept carries no display, as the display is owned by the supplemented
    /// CodeSystems. Only the localised synonyms are contributed, as designations.
    /// </summary>
    private static CodeSystem.ConceptDefinitionComponent GetSupplementConcept(
        TerminologyRecord terminologyRecord,
        string[] synonymList)
    {
        return new CodeSystem.ConceptDefinitionComponent()
        {
            Code = terminologyRecord.Code,
            Designation = GetDesignationList(synonymList)
        };
    }

    private static List<CodeSystem.DesignationComponent> GetDesignationList(string[] synonymList)
    {
        var designationList = new List<CodeSystem.DesignationComponent>();
        foreach (string synonym in synonymList)
        {
            designationList.Add(new CodeSystem.DesignationComponent()
            {
                Language = DesignationLanguage,
                Use = new Coding(system: "http://snomed.info/sct", code: "900000000000013009", display: "Synonym"),
                Value = synonym
            });
        }

        return designationList;
    }

    private static string TrimUrl(Uri systemUri) => systemUri.OriginalString.TrimEnd('/');

    /// <summary>
    /// Derives the ValueSet canonical from the Local CodeSystems URL, swapping the last
    /// /CodeSystem/ path segment for /ValueSet/ so the canonical does not sit under a CodeSystem
    /// path. The URL is used unchanged when it carries no such segment.
    /// </summary>
    private static string GetValueSetUrl(string localUrl)
    {
        int segmentIndex = localUrl.LastIndexOf(CodeSystemPathSegment, StringComparison.Ordinal);
        if (segmentIndex < 0)
        {
            return localUrl;
        }

        return string.Concat(
            localUrl.AsSpan(0, segmentIndex),
            ValueSetPathSegment,
            localUrl.AsSpan(segmentIndex + CodeSystemPathSegment.Length));
    }

    /// <summary>
    /// Builds a display name from a code by swapping the '-' and '_' separators for spaces and
    /// upper-casing the first letter of each word. The remaining letters are left untouched so
    /// acronyms and mixed-case codes survive.
    /// </summary>
    private static string GetName(
        string code)
    {
        string spacedCode = code.Replace("-", " ").Replace("_", " ").Trim();

        var nameBuilder = new StringBuilder(spacedCode.Length);
        bool atWordStart = true;
        foreach (char character in spacedCode)
        {
            if (char.IsWhiteSpace(character))
            {
                nameBuilder.Append(character);
                atWordStart = true;
                continue;
            }

            nameBuilder.Append(atWordStart ? char.ToUpperInvariant(character) : character);
            atWordStart = false;
        }

        return nameBuilder.ToString();
    }
    
}
