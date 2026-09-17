using System.Runtime.Serialization;

namespace SmartAuditERP
{
    public class SystemResponse
    {
        [DataMember]
        public string Message { get; set; } = string.Empty;

        [DataMember]
        public bool IsSuccess { get; set; } = false;

        [DataMember]
        public object ResponseObject { get; set; }

        public SystemResponse()
        {
            Message = string.Empty;
            IsSuccess = false;
        }
    }

    public class SystemResponse<T>
    {
        public string Message { get; set; } = string.Empty;

        public bool IsSuccess { get; set; } = false;

        public T ResponseObject { get; set; }

        public SystemResponse()
        {
            Message = string.Empty;
            IsSuccess = false;
        }
    }
}