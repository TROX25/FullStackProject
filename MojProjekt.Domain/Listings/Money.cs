namespace MojProjekt.Domain.Listings;

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public override string ToString() => $"{Amount:N0} {Currency}";
}
