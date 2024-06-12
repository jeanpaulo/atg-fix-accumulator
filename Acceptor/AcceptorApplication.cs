using Acceptor.Data;
using Acceptor.Model;
using QuickFix;
using QuickFix.Fields;
using System.Net.Http;

namespace Acceptor;
public class AcceptorApplication : QuickFix.MessageCracker, QuickFix.IApplication
{
    private static readonly HttpClient client = new HttpClient();
    private readonly AcceptorContext _context;
    
    private const int LIMITE_EXPOSICAO = 1_000_000;
    private const string signalUrl = "http://localhost:5074/broadcast";
    private int orderID = 0;

    public AcceptorApplication()
    {
    }

    public AcceptorApplication(AcceptorContext context)
    {
        _context = context;
    }


    #region QuickFix.Application Methods
    public void FromApp(Message message, SessionID sessionID)
    {
        Console.WriteLine("IN:  " + message);
        Crack(message, sessionID);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        Console.WriteLine("OUT: " + message);
    }

    public void FromAdmin(Message message, SessionID sessionID) { }
    public void OnCreate(SessionID sessionID) { }
    public void OnLogout(SessionID sessionID) { }
    public void OnLogon(SessionID sessionID) { }
    public void ToAdmin(Message message, SessionID sessionID) { }
    #endregion


    public async void OnMessage(QuickFix.FIX44.NewOrderSingle n, SessionID s)
    {
        string finalSignalUrl = "";

        try
        {
            Console.WriteLine("\nMensagem chegando...");

            var symbol = n.Symbol.Obj;
            var side = n.Side.Obj;
            //OrdType ordType = n.OrdType;
            var orderQty = n.OrderQty.Obj;
            var price = n.Price.Obj;
            var clOrdID = n.ClOrdID.Obj;

            Console.WriteLine($"Simbolo: {symbol}");
            Console.WriteLine($"Lado: {side}");
            Console.WriteLine($"Quantidade: {orderQty}");
            Console.WriteLine($"Preço: {price}");

            var orders = _context.OrderItems.Where(x => x.Symbol.Equals(symbol)).ToList();
            var newOrder = CreateOrder(symbol, side, orderQty, price);
            orders.Add(newOrder);

            var exposicao = CalculateExposure(orders);

            Console.WriteLine($"Exposição: {exposicao}");

            if (Math.Abs(exposicao) > LIMITE_EXPOSICAO)
            {
                var reject = CreateOrderCancelReject(clOrdID);
                Session.SendToTarget(reject, s);
                await BroadcastMessage("OrderReject");
                _context.ChangeTracker.Clear();
            } 
            else
            {
                _context.OrderItems.Add(newOrder);
                await _context.SaveChangesAsync();

                var exReport = CreateExecutionReport(clOrdID, symbol, orderQty, price);
                Session.SendToTarget(exReport, s);
                await BroadcastMessage("ExecutionReport");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AcceptorApplication/OnMessage: {ex.Message}");
            throw;
        }
    }

    private enum SIDE
    {
        COMPRA = '1' ,
        VENDA = '2'
    }

    private decimal CalculateExposure(List<Order> orders)
    {
        var compra = orders.Where(x => x.Side == (char)SIDE.COMPRA).Select(y => y.Price * y.Quantity).Sum();
        var venda = orders.Where(x => x.Side == (char)SIDE.VENDA).Select(y => y.Price * y.Quantity).Sum();
        return compra - venda;
    }

    private Order CreateOrder(string symbol, char side, decimal orderQty, decimal price)
    {
        return new Order
        {
            CreatedAt = DateTime.UtcNow,
            Price = price,
            Quantity = Convert.ToInt32(orderQty),
            Side = side,
            Symbol = symbol
        };
    }

    private QuickFix.FIX44.ExecutionReport CreateExecutionReport(string clOrdID, string symbol, decimal orderQty, decimal price)
    {
        var exReport = new QuickFix.FIX44.ExecutionReport();
        exReport.Set(new ClOrdID(clOrdID));
        exReport.Set(new Symbol(symbol));
        exReport.Set(new OrderQty(orderQty));
        //exReport.Set(new LastQty(orderQty));
        exReport.Set(new LastPx(price));
        exReport.Set(new OrderID(GenOrderID()));
        return exReport;
    }

    private QuickFix.FIX44.OrderCancelReject CreateOrderCancelReject(string clOrdID)
    {
        return new QuickFix.FIX44.OrderCancelReject(
            new OrderID(GenOrderID()),
            new ClOrdID(clOrdID),
            new OrigClOrdID(clOrdID),
            new OrdStatus(OrdStatus.CANCELED),
            new CxlRejResponseTo('1')
        );
    }


    private async Task BroadcastMessage(string message)
    {
        var finalSignalUrl = $"{signalUrl}?message={message}";

        Console.WriteLine(finalSignalUrl);

        try
        {
            var response = await client.PostAsync(finalSignalUrl, null);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ao enviar messagem de broadcast: {ex.Message}");
            throw;
        }
    }

    private string GenOrderID() { return (++orderID).ToString(); }

}
