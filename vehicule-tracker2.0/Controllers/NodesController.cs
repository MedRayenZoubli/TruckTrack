using Microsoft.AspNetCore.Mvc;
using vehicule_tracker2._0.Services;
using vehicule_tracker2._0.Models;

namespace vehicule_tracker2._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NodesController : ControllerBase
    {
        private readonly DeliveryManager _manager;

        public NodesController(DeliveryManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        public IActionResult GetAllNodes()
        {
            try
            {
                var nodes = _manager.GetAllNodes();
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetNodeById(string id)
        {
            try
            {
                var nodes = _manager.GetAllNodes();
                var node = nodes.FirstOrDefault(n => n.Id == id);

                if (node == null)
                    return NotFound(new { error = $"Node {id} not found" });

                return Ok(node);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("info")]
        public IActionResult GetNodesInfo()
        {
            try
            {
                var nodes = _manager.GetAllNodes();
                var info = new
                {
                    totalNodes = nodes.Count,
                    nodes = nodes.Select(n => new
                    {
                        n.Id,
                        n.Name,
                        n.Latitude,
                        n.Longitude,
                        AlertRadiusKm = n.AlertRadius,
                        StopRadiusKm = n.StopRadius
                    })
                };
                return Ok(info);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

}


