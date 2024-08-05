using MassTransit;

using MessageContracts;

var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host("localhost");
    cfg.ReceiveEndpoint("invoice-service", e =>
    {
        e.UseInMemoryOutbox();
        e.Consumer<EventConsumer>(c =>
        c.UseMessageRetry(m => m.Interval(5, new TimeSpan(0, 0, 10))));
    });
});

var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await busControl.StartAsync(source.Token);

Console.WriteLine("Invoice Microservice Now Listening");

try
{
    while (true)
    {
        await Task.Delay(100);
    }
}
finally
{
    await busControl.StopAsync();
}

public class EventConsumer : IConsumer<IInvoiceToCreate>
{
    public async Task Consume(ConsumeContext<IInvoiceToCreate> context)
    {

        var newInvoiceNymber = new Random().Next(10000,99999);
        Console.WriteLine($"Creating invoice {newInvoiceNymber} for customer: {context.Message.CustomerNumber}");

        context.Message.InvoiceItems.ForEach(i =>
        {
            Console.WriteLine($"with items: Price: {i.Price}, Decs: {i.Description}");
            Console.WriteLine($"Actual distance in miles: {i.ActualMileage}, Base Rate: { i.BaseRate}");
            Console.WriteLine($"Oversized: {i.IsOversized}, Refrigerated: { i.IsRefrigerated}, Haz Mat: { i.IsHazardousMaterial}");
        });

        await context.Publish<IInvoiceCreated>(new
        {
            InvoiceNumber = newInvoiceNymber,
            InvoiceData = new
            {
                context.Message.CustomerNumber,
                context.Message.InvoiceItems
            }
        });

    }

}
