﻿using Microsoft.EntityFrameworkCore;
using TyresShopAPI.Application.Interfaces;
using TyresShopAPI.Domain.Entities;
using TyresShopAPI.Domain.Entities.Cart;
using TyresShopAPI.Domain.Exceptions;
using TyresShopAPI.Domain.Models.Cart;
using TyresShopAPI.Infrastructure.Persistance;

namespace TyresShopAPI.Application.Services
{
    public class CustomerCartService : BaseService, ICustomerCartService
    {
        public CustomerCartService(Context context) : base(context)
        {
        }

        public async Task AddOrUpdateCartItem(CartItemModel model)
        {
            var isTyreExist = _context.Tyres.Any(t => t.Id == model.TyreId);

            if (!isTyreExist)
            {
                throw new TyreNotFoundException(model.TyreId);
            }

            var customer = await _context
                .Users.Include(u => u.CustomerCart)
                .ThenInclude(ct => ct.Items)
                .FirstOrDefaultAsync(x => x.Id == model.CustomerId);

            if (customer is null)
            {
                throw new CustomerNotFoundException(model.CustomerId);
            }

            var isCartItemExist = customer.CustomerCart.Items.Any(x => x.TyreId == model.TyreId);

            var cartItem = new CartItem()
            {
                TyreId = model.TyreId,
                Quantity = model.Quantity,
                TotalValue = await CalculateTotalValue(model.TyreId, model.Quantity),
                CustomerCart = customer.CustomerCart
            };

            if (!isCartItemExist)
            {
                customer.CustomerCart.Items.Add(cartItem);
            }
            else
            {
                var dbCartItem = customer.CustomerCart.Items.FirstOrDefault(x => x.TyreId == model.TyreId);

                dbCartItem.TotalValue = await CalculateTotalValue(model.TyreId, model.Quantity);
                dbCartItem.Quantity = model.Quantity;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RegisterCustomerCart(string customerEmail)
        {
            var customer = await _context.Users
                .Where(x => x.Email!.Equals(customerEmail))
                .Include(c => c.CustomerCart)
                .SingleOrDefaultAsync();

            customer!.CustomerCart = new CustomerCart()
            {
                Customer = customer,
                CustomerId = customer.Id
            };

            await _context.SaveChangesAsync();
        }

        public async Task RemoveCartItem(CartItemModel model)
        {
            var customer = await _context
                .Users.Include(u => u.CustomerCart)
                .ThenInclude(ct => ct.Items)
                .FirstOrDefaultAsync(x => x.Id == model.CustomerId);

            if (customer is null)
            {
                throw new CustomerNotFoundException(model.CustomerId);
            }

            var isCartItemExist = customer.CustomerCart.Items.Any(x => x.TyreId == model.TyreId);

            if (isCartItemExist)
            {
                customer.CustomerCart.Items.RemoveAll(x => x.TyreId == model.TyreId);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveCartItemByIds(List<int> cartItemsIds)
        {
            foreach (var cartItemId in cartItemsIds)
            {
                var cartItem = await _context
               .CartItems.Where(x => x.Id == cartItemId).SingleOrDefaultAsync();

                if (cartItem is null)
                {
                    throw new Exception();
                }

                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CartView>> ReturnAllCustomerCartItems(string customerId)
        {
            List<CartView> customerCartItems = new();

            var customerCart = await _context.CustomerCarts.Where(x => x.CustomerId.Equals(customerId)).SingleOrDefaultAsync();

            if (customerCart is null)
            {
                throw new Exception();
            }

            var allCustomerCartItems = await _context.CartItems.Where(x => x.CustomerCartId == customerCart.Id).ToListAsync();

            foreach (var cartItem in allCustomerCartItems)
            {
                var cart = new CartView()
                {
                    Id = cartItem.Id,
                    TotalValue = cartItem.TotalValue,
                    TyreId = cartItem.TyreId,
                    Quantity = cartItem.Quantity
                };

                customerCartItems.Add(cart);
            }

            return customerCartItems;
        }

        private async Task<decimal> CalculateTotalValue(int tyreId, int quantity)
        {
            var tyre = await _context.Tyres.FindAsync(tyreId);

            return tyre!.Price * quantity;
        }
    }
}
