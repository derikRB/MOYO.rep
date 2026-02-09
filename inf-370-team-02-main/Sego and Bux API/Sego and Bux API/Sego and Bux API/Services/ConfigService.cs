//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Sego_and__Bux.Data;
//using Sego_and__Bux.DTOs;
//using Sego_and__Bux.Interfaces;

//namespace Sego_and__Bux.Services
//{
//    public class AppConfigService : IAppConfigService
//    {
//        public const string OtpKey = "OtpExpiryMinutes";
//        public const string SessKey = "SessionTimeoutMinutes";

//        private readonly ApplicationDbContext _db;
//        public AppConfigService(ApplicationDbContext db) { _db = db; }

//        public async Task<int> GetIntAsync(string key, int @default)
//        {
//            var val = await _db.SystemConfigs.AsNoTracking()
//                .Where(x => x.Key == key)
//                .Select(x => x.Value)
//                .FirstOrDefaultAsync();

//            return int.TryParse(val, out var n) && n > 0 ? n : @default;
//        }

//        public async Task<TimerPolicyDto> GetTimerPolicyAsync(CancellationToken ct = default)
//        {
//            var otp = await GetIntAsync(OtpKey, 10);
//            var sess = await GetIntAsync(SessKey, 60);

//            // clamp to sensible bounds used everywhere
//            otp = Math.Clamp(otp, 1, 30);
//            sess = Math.Clamp(sess, 5, 240);

//            return new TimerPolicyDto
//            {
//                OtpExpiryMinutes = otp,
//                SessionTimeoutMinutes = sess,
//                UpdatedAtUtc = DateTime.UtcNow.ToString("O")
//            };
//        }

//        public async Task UpdateTimerPolicyAsync(int otpMinutes, int sessionMinutes, string changedBy, CancellationToken ct = default)
//        {
//            otpMinutes = Math.Clamp(otpMinutes, 1, 30);
//            sessionMinutes = Math.Clamp(sessionMinutes, 5, 240);

//            await Upsert(OtpKey, otpMinutes.ToString(), changedBy, ct);
//            await Upsert(SessKey, sessionMinutes.ToString(), changedBy, ct);
//            await _db.SaveChangesAsync(ct);
//        }

//        private async Task Upsert(string key, string value, string updatedBy, CancellationToken ct)
//        {
//            var row = await _db.SystemConfigs.FirstOrDefaultAsync(x => x.Key == key, ct);
//            if (row == null)
//            {
//                _db.SystemConfigs.Add(new Models.SystemConfig
//                {
//                    Key = key,
//                    Value = value,
//                    UpdatedBy = updatedBy,
//                    UpdatedAtUtc = DateTime.UtcNow
//                });
//            }
//            else
//            {
//                row.Value = value;
//                row.UpdatedBy = updatedBy;
//                row.UpdatedAtUtc = DateTime.UtcNow;
//            }
//        }
//    }
//}
