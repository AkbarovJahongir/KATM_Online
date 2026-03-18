using CreditBureau;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Adding Dependency
        services.AddCreditBureau(context);
        // Adding Dependency
        services.AddHostedService<Asoki>();
        services.AddHostedService<AsokiXml>();
        services.AddHostedService<CreditBureauReportWorker>();

    })
    .UseWindowsService()
    .Build();

await host.RunAsync();