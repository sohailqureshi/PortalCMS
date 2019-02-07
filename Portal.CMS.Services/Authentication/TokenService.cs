﻿using Portal.CMS.Entities;
using Portal.CMS.Entities.Entities;
using Portal.CMS.Entities.Enumerators;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Portal.CMS.Services.Authentication
{
    public interface ITokenService
    {
        Task<string> AddAsync(string emailAddress, UserTokenType userTokenType);

        Task<string> RedeemPasswordTokenAsync(string token, string emailAddress, string password);

        Task<string> RedeemSSOTokenAsync(string userId, string token);
    }

    public class TokenService : ITokenService
    {
        #region Dependencies

        readonly PortalDbContext _context;
        readonly IUserService _userService;
        readonly IRegistrationService _registrationService;

        public TokenService(PortalDbContext context, IUserService userService, IRegistrationService registrationService)
        {
            _context = context;
            _userService = userService;
            _registrationService = registrationService;
        }

        #endregion Dependencies

        public async Task<string> AddAsync(string emailAddress, UserTokenType userTokenType)
        {
            var user = await _userService.GetByEmailAsync(emailAddress);
            if (user == null) return string.Empty;

            var userToken = new UserToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                UserTokenType = userTokenType,
                DateAdded = DateTime.Now
            };

            _context.UserTokens.Add(userToken);

            await _context.SaveChangesAsync();

            return userToken.Token;
        }

        public async Task<string> RedeemPasswordTokenAsync(string token, string emailAddress, string password)
        {
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase));

            if (userToken == null)
                return "Invalid Token. Please Request Reset Password Token Again...";

            if (!userToken.User.Email.Equals(emailAddress, StringComparison.OrdinalIgnoreCase))
                return "Invalid Token. This Token does not match the Email Address you entered...";

            if (userToken.DateRedeemed.HasValue)
                return "Invalid Token. This Token has already been used";

            await _registrationService.ChangePasswordAsync(userToken.User.Id, password);

            userToken.DateRedeemed = DateTime.Now;
            await _context.SaveChangesAsync();

            return string.Empty;
        }

        public async Task<string> RedeemSSOTokenAsync(string userId, string token)
        {
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase) && x.UserId == userId);

            if (userToken == null)
                return "Invalid Token.";

            if (userToken.DateRedeemed.HasValue)
                return "Invalid Token. This Token has already been used.";

            userToken.DateRedeemed = DateTime.Now;

            await _context.SaveChangesAsync();

            return string.Empty;
        }
    }
}