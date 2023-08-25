﻿using Microsoft.AspNetCore.Identity;
using TyresShopAPI.Domain.Entities.Cart;

namespace TyresShopAPI.Domain.Entities.Customers
{
    public class Customer : IdentityUser
    {
        public CustomerCart? CustomerCart { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public CustomerAddress? Address { get; set; }
    }
}
