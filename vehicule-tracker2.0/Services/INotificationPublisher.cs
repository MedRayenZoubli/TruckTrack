using System.Threading.Tasks;
namespace vehicule_tracker2._0.Services
{
    
    
    public interface INotificationPublisher
    {
        Task PublishStatusChangeAsync(string truckId, string status, double distance);
    }
    

}
