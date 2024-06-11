using Acceptor.Data;
using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acceptor;
public class AcceptorApplication : QuickFix.MessageCracker, QuickFix.IApplication
{
    private static readonly HttpClient client = new HttpClient();
    private readonly AcceptorContext _context;

    int orderID = 0;
    int execID = 0;


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
        Console.WriteLine("\nMensagem chegando...");

        try
        {
            Symbol symbol = n.Symbol;
            Side side = n.Side;
            OrdType ordType = n.OrdType;
            OrderQty orderQty = n.OrderQty;
            Price price = new Price(n.Price.Obj);
            ClOrdID clOrdID = n.ClOrdID;

            Console.WriteLine($"Simbolo: {symbol.Obj}");
            Console.WriteLine($"Lado: {side.Obj}");
            Console.WriteLine($"Quantidade: {orderQty.Obj}");
            Console.WriteLine($"Preço: {price.Obj}");

            QuickFix.FIX44.ExecutionReport exReport = new QuickFix.FIX44.ExecutionReport();

            exReport.Set(clOrdID);
            exReport.Set(symbol);
            exReport.Set(orderQty);
            exReport.Set(new LastQty(orderQty.getValue()));
            exReport.Set(new LastPx(price.getValue()));

            Session.SendToTarget(exReport, s);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AcceptorApplication/OnMessage: {ex.Message}");
            throw;
        }
    }


}
