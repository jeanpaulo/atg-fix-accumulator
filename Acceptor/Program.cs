

using Acceptor;
using Acceptor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickFix;
using QuickFix.Fields;

class Program
{
    private const string HttpServerPrefix = "http://127.0.0.1:5080/";

    static void Main(string[] args)
    {

		try
		{
            var serviceProvider = new ServiceCollection()
                .AddDbContext<AcceptorContext>(options => options.UseInMemoryDatabase("FixDb"))
                .BuildServiceProvider();


            using (var context = serviceProvider.GetService<AcceptorContext>())
            {
                var configFile = new StreamReader("./server.cfg");

                SessionSettings settings = new SessionSettings(configFile);
                IApplication executorApp = new AcceptorApplication(context);
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                ILogFactory logFactory = new FileLogFactory(settings);
                ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(executorApp, storeFactory, settings, logFactory);
                HttpServer srv = new HttpServer(HttpServerPrefix, settings);

                acceptor.Start();
                srv.Start();

                Console.WriteLine(HttpServerPrefix);
                Console.WriteLine("Aperter <enter> para sair");
                Console.Read();

                srv.Stop();
                acceptor.Stop();
            }
        }
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}


    }
}