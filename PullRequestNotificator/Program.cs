
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Device.Gpio;
using System.Text;

using IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(app =>
    {
        app.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<Application>();
    })
    .Build();

host.Services.GetRequiredService<Application>().StartProgram();

class Application
{
    private const int _notificationsPin = 18;

    private readonly IConfiguration _configuration;

    private QueueClient _notificationsQueueClient;
    private GpioController _gpioController;

    public Application(IConfiguration configuration)
    {
        _configuration = configuration;
        _gpioController = new GpioController();
    }

    public void StartProgram()
    {
        CreateQueueClient();
        InitializeGPIO();

        while (true)
        {
            CheckMessages();

            Thread.Sleep(1000);
        }
    }

    public void CreateQueueClient()
    {
        _notificationsQueueClient = new QueueClient(_configuration["AppSettings:ConnectionStrings:Queue"], "notifications");
    }

    public void CheckMessages()
    {
        if (_notificationsQueueClient.Exists())
        {
            QueueMessage[] retrievedMessages = _notificationsQueueClient.ReceiveMessages();

            if (retrievedMessages.Length > 0)
            {
                var notification = Encoding.UTF8.GetString(Convert.FromBase64String(retrievedMessages[0].Body.ToString()));

                switch (notification)
                {
                    case "pull_request":
                        NotifyPullRequest();
                        break;
                    case "clear_notifications":
                        ClearNotifications();
                        break;
                }


                _notificationsQueueClient.DeleteMessage(retrievedMessages[0].MessageId, retrievedMessages[0].PopReceipt);
            }
        }
    }

    public void InitializeGPIO()
    {
        _gpioController.OpenPin(_notificationsPin, PinMode.Output);

        _gpioController.Write(_notificationsPin, PinValue.High);
    }

    public void NotifyPullRequest()
    {
        Console.WriteLine("New pull request");

        _gpioController.Write(_notificationsPin, PinValue.Low);
    }

    public void ClearNotifications()
    {
        Console.WriteLine("Clear notifications");

        _gpioController.Write(_notificationsPin, PinValue.High);
    }
}
