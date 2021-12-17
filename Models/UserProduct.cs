using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataContextContainer.Models;

namespace DataContextContainer
{
    public class UserProduct
    {
        public int UserId { get; set; }

        public int ProductId { get; set; }

        public virtual User User { get; set; }

        public virtual Product Product { get; set; }

    }
}
