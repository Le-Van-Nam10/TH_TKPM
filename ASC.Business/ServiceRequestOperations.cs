using ASC.Model.Queries;
using ASC.Business.Interfaces;
using ASC.DataAccess.Interface;
using ASC.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace ASC.Business
{
    public class ServiceRequestOperations : IServiceRequestOperations
    {
        private readonly IUnitOfWork _uniOfWork;

        public ServiceRequestOperations(IUnitOfWork uniOfWork)
        {
            _uniOfWork = uniOfWork;
        }
        public async Task CreateServiceRequestAsync(ServiceRequest request)
        {
            using (_uniOfWork)
            {
                await _uniOfWork.Repository<ServiceRequest>().AddAsync(request);
                _uniOfWork.CommitTransaction();

            }
        }
        public async Task<List<ServiceRequest>> GetServiceRequestsByRequestedDateAndStatus
(
    DateTime? requestedDate, List<string> status = null, string email = "", string serviceEngineerEmail = ""
)
        {
            var query = Queries.GetDashboardQuery(requestedDate, status, email, serviceEngineerEmail);
            var serviceRequests = await _uniOfWork.Repository<ServiceRequest>().FindAllByQuery(query);
            return serviceRequests.ToList();
        }

        public ServiceRequest UpdateServiceRequest(ServiceRequest request)
        {
            using (_uniOfWork)
            {
                _uniOfWork.Repository<ServiceRequest>().Update(request);
                _uniOfWork.CommitTransaction();
                return request;
            }
        }

        public async Task<ServiceRequest> UpdateServiceRequestStatusAsync(string rowKey, string partitionKey, string status)
        {
            using (_uniOfWork)
            {
                var serviceRequest = await _uniOfWork.Repository<ServiceRequest>().FindAsync(partitionKey, rowKey);
                if (serviceRequest == null)
                    throw new NullReferenceException();
                serviceRequest.Status = status;
                _uniOfWork.Repository<ServiceRequest>().Update(serviceRequest);
                _uniOfWork.CommitTransaction();
                return serviceRequest;
            }
        }
    }
}