using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;

public sealed class AppSettings
{
    public string FrontendUrl { get; set; } = "https://lzdetailingstore.com";
}

public sealed class CreateCheckoutCommandHandler(
    DbContext dbContext,
    IMercadoPagoService mercadoPagoService,
    IUnitOfWork unitOfWork,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<CreateCheckoutCommand, Result<CheckoutResultDto>>
{
    public async Task<Result<CheckoutResultDto>> Handle(CreateCheckoutCommand request, CancellationToken ct)
    {
        // Load cart with items and variant/product data
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null || cart.Items.Count == 0)
            return Result<CheckoutResultDto>.Failure(
                new Error("EMPTY_CART", "El carrito está vacío."));

        // Find default shipping address
        var address = await dbContext.Set<ShippingAddress>()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.EsPrincipal && a.IsActive, ct);

        if (address is null)
            return Result<CheckoutResultDto>.Failure(
                new Error("NO_ADDRESS", "No tienes una dirección de envío principal configurada."));

        // Validate stock for all items
        foreach (var item in cart.Items)
        {
            if (!item.Variant.IsActive)
                return Result<CheckoutResultDto>.Failure(
                    new Error("VARIANT_UNAVAILABLE",
                        $"La variante '{item.Variant.Nombre}' ya no está disponible."));

            if (item.Quantity > item.Variant.Stock)
                return Result<CheckoutResultDto>.Failure(
                    new Error("INSUFFICIENT_STOCK",
                        $"Stock insuficiente para '{item.Variant.Nombre}'. Disponible: {item.Variant.Stock}."));
        }

        // Create order with snapshot data
        var order = new Order
        {
            UserId = request.UserId,
            ShippingAddressId = address.Id,
            Subtotal = cart.Items.Sum(i => i.Variant.PrecioVenta * i.Quantity),
            ShippingCost = 0, // TODO: Calculate shipping based on address/weight
            Items = cart.Items.Select(i => new OrderItem
            {
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                UnitPrice = i.Variant.PrecioVenta,
                Subtotal = i.Variant.PrecioVenta * i.Quantity,
                ProductName = i.Variant.Product.Name,
                VariantName = i.Variant.Nombre,
                Sku = i.Variant.Sku,
                ImageUrl = i.Variant.Product.ImageUrl
            }).ToList()
        };

        order.Total = order.Subtotal + order.ShippingCost;

        await dbContext.Set<Order>().AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Create MercadoPago preference
        var baseUrl = appSettings.Value.FrontendUrl;

        var mpItems = cart.Items.Select(i => new MercadoPagoItem(
            $"{i.Variant.Product.Name} - {i.Variant.Nombre}",
            i.Quantity,
            i.Variant.PrecioVenta
        )).ToList();

        var backUrls = new MercadoPagoBackUrls(
            Success: $"{baseUrl}/pedidos/{order.Id}/confirmado",
            Failure: $"{baseUrl}/pedidos/{order.Id}/error",
            Pending: $"{baseUrl}/pedidos/{order.Id}/pendiente"
        );

        var preferenceResult = await mercadoPagoService.CreatePreferenceAsync(
            order.Id.ToString(), mpItems, backUrls, ct);

        if (preferenceResult.IsFailure)
            return Result<CheckoutResultDto>.Failure(preferenceResult.Error!,
                preferenceResult.StatusCode ?? 500);

        // Store the real MercadoPago preference ID (not the order ID)
        order.MercadoPagoPreferenceId = preferenceResult.Value!.PreferenceId;
        await unitOfWork.SaveChangesAsync(ct);

        return new CheckoutResultDto(order.Id, preferenceResult.Value!.InitPoint);
    }
}
