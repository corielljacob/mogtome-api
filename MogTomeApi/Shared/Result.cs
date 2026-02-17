using System.Net;

namespace MogTomeApi.Shared
{
    public class Result<T>
    {
        protected internal Result(T value, HttpStatusCode httpStatusCode, bool isSuccess, string error)
        {
            Value = value;
            StatusCode = httpStatusCode;
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Value { get; set; }
        public string Error { get; set; }
    }
}
