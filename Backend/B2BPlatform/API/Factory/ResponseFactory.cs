using API.DTO.GeneralResponse;
using API.DTO.Users;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace API.Factory
{
    public static class ResponseFactory
    {
        public static GeneralSingleResponse<T> CreateGeneralSingleResponse<T>(T data)
        {
            return new GeneralSingleResponse<T> { Data = data };
        }
        public static GeneralResponse<T> CreateGeneralResponse<T>(List<T> data)
        {
            return new GeneralResponse<T>() { Data = data};
        }
        public static GeneralResponsePagination<T> CreatePaginationResponse<T>(List<T> data, Meta meta)
        {
            return new GeneralResponsePagination<T>() { Data =data ,Meta = meta};
        }
        public static GeneralResponseError CreateMessageResponse( string message)
        {
            return new GeneralResponseError() { Data = new Data() { Message = message} };
        }
        public static GeneralResponseValidationError CreateValidationErrorResponse(Dictionary<string,string> error)
        {
            return new GeneralResponseValidationError() { Data = new ErrorData() { Message = "Validation Failed", details = error } };
        }
        public static GeneralResponseLogin CreateLoginResponse(string token ,UserDataDTO user)
        {
            return new GeneralResponseLogin() { Token = token , User= user };
        }
        public static GeneralResponsePagination<object> CreatePaginationEmptyResponse(int pageSize,int currentPage)
        {
            return new GeneralResponsePagination<object>() { Data = new List<object>(), Meta = new Meta
            {
                PageSize = pageSize,
                CurrentPage = currentPage,
                TotalPages = 0,
                TotalItems = 0
            }
            };
        }
    }
}
