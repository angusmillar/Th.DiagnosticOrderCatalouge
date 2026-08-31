using CsvHelper.Configuration;

namespace Th.Fhir.Terminology.Domain.OrderCatalogueCsv;

public class TerminologyCsvRecordMap : ClassMap<TerminologyCsvRecord>
{
    public TerminologyCsvRecordMap()
    {
        Map(x => x.Code).Name("Code");
        Map(x => x.DisplayName).Name("DisplayName");
        Map(x => x.CodeSystemUri).Name("SystemURI");
        Map(x => x.Synonyms).Name("Synonyms");
    }
}