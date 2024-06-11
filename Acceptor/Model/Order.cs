using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acceptor.Model;
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Symbol { get; set; }
    public char Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}