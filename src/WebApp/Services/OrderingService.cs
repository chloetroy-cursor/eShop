namespace eShop.WebApp.Services;

public class OrderingService(HttpClient httpClient)
{
    private readonly string remoteServiceBaseUrl = "/api/Orders/";

    public Task<OrderRecord[]> GetOrders()
    {
        return httpClient.GetFromJsonAsync<OrderRecord[]>(remoteServiceBaseUrl)!;
    }

    public Task<DelayedOrdersResult> GetDelayedOrders(int limit = 100)
    {
        return httpClient.GetFromJsonAsync<DelayedOrdersResult>($"{remoteServiceBaseUrl}delayed?limit={limit}")!;
    }

    public Task CreateOrder(CreateOrderRequest request, Guid requestId)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, remoteServiceBaseUrl);
        requestMessage.Headers.Add("x-requestid", requestId.ToString());
        requestMessage.Content = JsonContent.Create(request);
        return httpClient.SendAsync(requestMessage);
    }
}

public record OrderRecord(
    int OrderNumber,
    DateTime Date,
    string Status,
    decimal Total);

public record DelayedOrdersResult(DateTimeOffset AsOf, int ThresholdMinutes, DelayedOrderRecord[] Orders);

public record DelayedOrderRecord(int OrderNumber, DateTime Date, string Status, decimal Total, int AgeMinutes);
