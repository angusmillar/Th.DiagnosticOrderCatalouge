namespace Th.Fhir.Terminology.Domain.OrderCatalogueCsv;

public interface IOrderCatalogueCsvReader
{
    Task Read(
        OrderCatalogueCsvReaderInput input);
}