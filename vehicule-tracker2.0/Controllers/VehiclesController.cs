using Microsoft.AspNetCore.Mvc;
using vehicule_tracker2._0.Models;
using vehicule_tracker2._0.Services;

namespace vehicule_tracker2._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly DeliveryManager _manager;

        public VehiclesController(DeliveryManager manager)
        {
            _manager = manager;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateTruck([FromBody] VehicleUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id))
            {
                Console.WriteLine(" Bad request: Vehicle ID required");
                return BadRequest(new { error = "Vehicle ID required" });
            }

            try
            {
                Console.WriteLine(
                    $" HTTP POST received: {request.Id} at ({request.Latitude:F4}, {request.Longitude:F4})"
                );

                var existingTruck = _manager
                    .GetAllTrucks()
                    .FirstOrDefault(t => t.Id == request.Id);

                Vehicle vehicle;

                if (existingTruck == null)
                {
                    vehicle = new Vehicle
                    {
                        Id = request.Id,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Status = "OK",
                        LastUpdate = DateTime.UtcNow,
                        StatusChangedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($" Creating new truck: {request.Id}");
                }
                else
                {
                    vehicle = existingTruck;

                    if (vehicle.Status == "STOP")
                    {
           
                        vehicle.LastUpdate = DateTime.UtcNow;

                        await _manager.UpdateTruckAsync(vehicle);

                        return Ok(new
                        {
                            message = "Truck STOPPED — no movement applied",
                            id = vehicle.Id,
                            status = vehicle.Status,
                            distance = vehicle.DistanceToNearestNode,
                            nearestNode = vehicle.NearestNodeName
                        });
                    }

                    vehicle.Latitude = request.Latitude;
                    vehicle.Longitude = request.Longitude;
                    vehicle.LastUpdate = DateTime.UtcNow;
                }

                await _manager.UpdateTruckAsync(vehicle);

                return Ok(new
                {
                    message = "Truck updated",
                    id = vehicle.Id,
                    status = vehicle.Status,
                    distance = vehicle.DistanceToNearestNode,
                    nearestNode = vehicle.NearestNodeName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error updating truck: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllTrucks()
        {
            try
            {
                var trucks = _manager.GetAllTrucks();
                return Ok(trucks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetTruckById(string id)
        {
            try
            {
                var trucks = _manager.GetAllTrucks();
                var truck = trucks.FirstOrDefault(t => t.Id == id);

                if (truck == null)
                    return NotFound(new { error = $"Truck {id} not found" });

                return Ok(truck);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        public IActionResult GetTrucksByStatus(string status)
        {
            try
            {
                var trucks = _manager
                    .GetAllTrucks()
                    .Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(trucks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
