using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Address;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserAddresses(Guid userId)
    {
        var addresses = await _addressService.GetUserAddressesAsync(userId);
        return Ok(addresses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var address = await _addressService.GetByIdAsync(id);

        if (address == null)
            return NotFound();

        return Ok(address);
    }

    [HttpPost("{userId:guid}")]
    public async Task<IActionResult> Create(
        Guid userId,
        [FromBody] CreateAddressDto dto)
    {
        var address = await _addressService.CreateAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = address.Id },
            address);
    }

    [HttpPut("{userId:guid}/{addressId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        Guid addressId,
        [FromBody] UpdateAddressDto dto)
    {
        await _addressService.UpdateAsync(userId, addressId, dto);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _addressService.DeleteAsync(id);

        return NoContent();
    }

    [HttpPatch("{userId:guid}/{addressId:guid}/default")]
    public async Task<IActionResult> SetDefault(
        Guid userId,
        Guid addressId)
    {
        await _addressService.SetDefaultAsync(userId, addressId);

        return NoContent();
    }
}