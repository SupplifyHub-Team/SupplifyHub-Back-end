using Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using System.Numerics;



namespace API.Services
{
    public class CleanupService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CleanupService(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }
        public async Task CleanExpiredAdvertisementForAdminAsync()
        {
            var adminAdvertisements = await _unitOfWork.SupplierAdvertisements.FindAllAsync(ads=> ads.SupplierId == null && ads.EndDate < DateTime.UtcNow );
            if (adminAdvertisements.Any())
            {
                _unitOfWork.SupplierAdvertisements.DeleteRange(adminAdvertisements);
                await _unitOfWork.SaveAsync();
            }
        }
        public async Task CleanExpiredTokensAsync()
        {
            var userTokensToRemove = await _unitOfWork.UserTokens.GetAllUserTokenThatNotValidateAsync();
            if (userTokensToRemove.Count != 0)
            {
                _unitOfWork.UserTokens.DeleteRange(userTokensToRemove);
                await _unitOfWork.SaveAsync();
            }
        }
    }


}