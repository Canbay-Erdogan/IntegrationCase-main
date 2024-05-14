using Integration.Common;
using Integration.Service;

namespace Integration;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var service = new ItemIntegrationService();

        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));

        //Thread.Sleep(500);

        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
        //ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));

        //Thread.Sleep(5000);

        for (int j = 0; j < 3; j++)
        {
            for (char i = 'A'; i <= 'z'; i++)
            {
                string s = i.ToString();
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var response = service.SaveItem(s);
                    if (response.Message is not null)
                    {
                        Console.WriteLine(response.Message);
                    }
                });
                Thread.Sleep(30);
            }
        }

        Console.WriteLine("Everything recorded:");

        service.GetAllItems().Result.ForEach(Console.WriteLine);

        Console.ReadLine();
    }
}