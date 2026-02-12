using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public interface IResult
    {
        int Code { get; set; }
        string? Message { get; set; }
        object Data { get; set; }
    }
    public interface IResult<T>:IResult {
        T Data { get; set; }
    }
    public class Result : Result<object>
    {
        

        public static Result Success(object? data = default)
        {
            return new Result { Data = data, Code = OkNum };
        }
        public static Result<T> Success<T>(T data=default)
        {
            return new Result<T> { Data = data, Code = OkNum };
        }
        public static Result Bad(string msg)
        {
            return new Result { Message = msg, Code = BadNum };
        }
        public static Result<T> Bad<T>(string msg)
        {
            return new Result<T> { Message = msg, Code = BadNum };
        }
        public static Result<T> Error<T>(string msg)
        {
            return new Result<T> { Message = msg, Code = ErrorNum };
        }
        public static Result Error(string msg)
        {
            return new Result { Message = msg, Code = ErrorNum };
        }
    }
    public class Result<T> : IResult<T>
    {
        public const int BadNum = 400;
        public const int ErrorNum = 500;
        public const int OkNum = 200;
        public const int BadGateway = 502;
        public int Code { get; set; }
        public string? Message { get; set; }
        public bool IsSuccess()
        {
            return Code == OkNum;
        }
        public T Data { get; set; }
        object IResult.Data { get => Data; set => Data = (T)value; }
    }
}
