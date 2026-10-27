namespace eShop.WebApp.UnitTests;

[TestClass]
public class DelayedOrdersViewTest
{
    private static readonly DelayedOrderRecord[] Orders =
    [
        new(1, DateTime.UtcNow, "Submitted", 10m, 300),
        new(2, DateTime.UtcNow, "Paid", 20m, 250),
        new(3, DateTime.UtcNow, "Paid", 40m, 241),
    ];

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("Shipped")]
    [DataRow("bogus")]
    public void FilterReturnsAllOrdersForMissingOrUnknownStatus(string? status)
        => CollectionAssert.AreEqual(Orders, DelayedOrdersView.Filter(Orders, status).ToArray());

    [TestMethod]
    [DataRow("paid")]
    [DataRow("PAID")]
    public void FilterMatchesStatusCaseInsensitively(string status)
    {
        var filtered = DelayedOrdersView.Filter(Orders, status);

        CollectionAssert.AreEqual(new[] { 2, 3 }, filtered.Select(o => o.OrderNumber).ToArray());
    }

    [TestMethod]
    public void FilterOfEmptyInputIsEmpty()
        => Assert.IsEmpty(DelayedOrdersView.Filter([], "Paid"));

    [TestMethod]
    [DataRow(0, "0m")]
    [DataRow(59, "59m")]
    [DataRow(240, "4h 00m")]
    [DataRow(337, "5h 37m")]
    [DataRow(1500, "25h 00m")]
    public void FormatAge(int ageMinutes, string expected)
        => Assert.AreEqual(expected, DelayedOrdersView.FormatAge(ageMinutes));
}
