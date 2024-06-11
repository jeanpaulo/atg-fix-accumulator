using Acceptor.Data;
using Acceptor.Model;
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
    
    const int LIMITE_EXPOSICAO = 1_000_000;

    int orderID = 0;

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
        string signalUrl = "http://localhost:5112/broadcast?";
        string finalSignalUrl = "";

        try
        {
            Console.WriteLine("\nMensagem chegando...");

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

            var somatorio_ordem = price.Obj * Convert.ToInt32(orderQty.Obj);

            if (somatorio_ordem > LIMITE_EXPOSICAO)
            {
                var reject = new QuickFix.FIX44.OrderCancelReject(
                    new OrderID("1"),
                    new ClOrdID(GenOrderID()),
                    new OrigClOrdID(n.ClOrdID.Obj),
                    new OrdStatus(OrdStatus.CANCELED),
                    new CxlRejResponseTo('1')
                );

                Session.SendToTarget(reject, s);

                finalSignalUrl = signalUrl + "message=OrderReject";

                Console.WriteLine(finalSignalUrl);
            }
            else
            {
                var order = new Order();

                order.CreatedAt = DateTime.UtcNow;
                order.Price = price.Obj;
                order.Quantity = Convert.ToInt32(orderQty.Obj);
                order.Side = side.Obj;
                order.Symbol = symbol.Obj;

                _context.OrderItems.Add(order);
                await _context.SaveChangesAsync();

                var listaOrdens = _context.OrderItems.ToList();
                var compra = listaOrdens.Where(x => x.Side == '1').Select(y => y.Price * y.Quantity).Sum();
                var venda = listaOrdens.Where(x => x.Side == '2').Select(y => y.Price * y.Quantity).Sum();

                var exposicao = compra - venda;

                QuickFix.FIX44.ExecutionReport exReport = new QuickFix.FIX44.ExecutionReport();

                exReport.Set(clOrdID);
                exReport.Set(symbol);
                exReport.Set(orderQty);
                exReport.Set(new LastQty(orderQty.getValue()));
                exReport.Set(new LastPx(price.getValue()));

                if (n.IsSetAccount())
                    exReport.SetField(n.Account);

                Session.SendToTarget(exReport, s);

                finalSignalUrl = signalUrl + "message=ExecutionReport";

                Console.WriteLine(finalSignalUrl);
            }
            
            try
            {
                var response = await client.PostAsync(finalSignalUrl, null);
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
            }
            catch (HttpRequestException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AcceptorApplication/OnMessage: {ex.Message}");
            throw;
        }
    }

    private string GenOrderID() { return (++orderID).ToString(); }

}
