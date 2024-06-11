

using Acceptor;
using QuickFix;
using QuickFix.Fields;

class Program
{
    private const string HttpServerPrefix = "http://127.0.0.1:5080/";

    static void Main(string[] args)
    {

		try
		{
			var configFile = new StreamReader("./server.cfg");


            SessionSettings settings = new SessionSettings(configFile);
            IApplication executorApp = new AcceptorApplication();
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
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}


    }
}