using AutoMapper;
using InventoryService.Application.DTOs;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mappings;

public class InventoryMappingProfile : Profile
{
    public InventoryMappingProfile()
    {
        CreateMap<Inventory, InventoryDto>();
        CreateMap<InventoryMovement, InventoryMovementDto>();
        CreateMap<Inventory, StockDto>();
    }
}

