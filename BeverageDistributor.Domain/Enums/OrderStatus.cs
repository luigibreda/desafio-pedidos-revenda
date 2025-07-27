namespace BeverageDistributor.Domain.Enums
{
    public enum OrderStatus
    {
        Pending,    // Pedido criado, aguardando processamento
        Processing, // Pedido em processamento
        Completed,  // Pedido concluído com sucesso
        Cancelled   // Pedido cancelado
    }
}
